using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Roslyn-based structural analyzer for a .NET submission.
// Parses C# syntactically (no compilation needed) so it correctly understands primary
// constructors, comments, attributes, partial classes, etc. — things regex gets wrong.
// Prints a single JSON object to stdout. On failure prints {"ok":false,...} so the caller
// can fall back to its regex heuristics.

try
{
    var dir = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
    if (!Directory.Exists(dir))
    {
        Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = $"directory not found: {dir}" }));
        return;
    }

    static bool Skip(string p)
    {
        var s = Path.DirectorySeparatorChar;
        return p.Contains($"{s}bin{s}") || p.Contains($"{s}obj{s}") || p.Contains($"{s}node_modules{s}");
    }

    var csFiles = Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories).Where(p => !Skip(p)).ToList();
    var csprojFiles = Directory.EnumerateFiles(dir, "*.csproj", SearchOption.AllDirectories).Where(p => !Skip(p)).ToList();

    // --- target frameworks (from csproj) ---
    var targetFrameworks = new List<string>();
    foreach (var pf in csprojFiles)
    {
        var text = File.ReadAllText(pf);
        foreach (Match m in Regex.Matches(text, @"<TargetFrameworks?>([^<]+)</TargetFrameworks?>"))
            targetFrameworks.AddRange(m.Groups[1].Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    // --- parse all syntax trees ---
    var classes = new List<(ClassDeclarationSyntax decl, string file)>();
    var interfaces = new List<InterfaceDeclarationSyntax>();
    var records = new List<RecordDeclarationSyntax>();
    var usings = new HashSet<string>();
    var invocationNames = new HashSet<string>();
    var genericNames = new HashSet<string>();
    var allNames = new HashSet<string>();
    var memberAccesses = new HashSet<string>();
    var mapEndpointRoutes = new List<string>();
    var produceOrPublishInvocations = new List<InvocationExpressionSyntax>();

    foreach (var f in csFiles)
    {
        SyntaxNode root;
        try { root = CSharpSyntaxTree.ParseText(File.ReadAllText(f)).GetRoot(); }
        catch { continue; }

        foreach (var c in root.DescendantNodes().OfType<ClassDeclarationSyntax>()) classes.Add((c, f));
        foreach (var i in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>()) interfaces.Add(i);
        foreach (var r in root.DescendantNodes().OfType<RecordDeclarationSyntax>()) records.Add(r);
        foreach (var u in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
            if (u.Name != null) usings.Add(u.Name.ToString());
        foreach (var g in root.DescendantNodes().OfType<GenericNameSyntax>()) genericNames.Add(g.Identifier.Text);
        foreach (var sn in root.DescendantNodes().OfType<SimpleNameSyntax>()) allNames.Add(sn.Identifier.Text);
        foreach (var ma in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>()) memberAccesses.Add(ma.Expression + "." + ma.Name.Identifier.Text);

        foreach (var inv in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (inv.Expression is MemberAccessExpressionSyntax ma)
            {
                invocationNames.Add(ma.Name.Identifier.Text);
                var name = ma.Name.Identifier.Text;
                if (name is "MapGet" or "MapPost" or "MapPut" or "MapDelete" && inv.ArgumentList.Arguments.Count > 0)
                    mapEndpointRoutes.Add(inv.ArgumentList.Arguments[0].ToString());
                if (name is "Produce" or "ProduceAsync" || name.StartsWith("Publish"))
                    produceOrPublishInvocations.Add(inv);
            }
            else if (inv.Expression is IdentifierNameSyntax idn)
            {
                invocationNames.Add(idn.Identifier.Text);
            }
        }
    }

    var declaredClassNames = classes.Select(c => c.decl.Identifier.Text).ToHashSet();

    bool BaseListContains(BaseListSyntax? bl, Func<string, bool> pred) =>
        bl != null && bl.Types.Any(t => pred(t.Type.ToString()));

    // referenced simple names (identifiers + generic names) inside a node, ignoring comments
    static HashSet<string> RefNames(SyntaxNode n) =>
        n.DescendantNodes().OfType<SimpleNameSyntax>().Select(x => x.Identifier.Text).ToHashSet();

    // --- controllers ---
    var controllers = classes.Where(c =>
            c.decl.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString().Contains("ApiController"))
            || BaseListContains(c.decl.BaseList, s => s == "Controller" || s.EndsWith("ControllerBase") || s.EndsWith(".Controller")))
        .Select(c => c.decl.Identifier.Text).Distinct().ToList();

    // --- DbContext subclasses (handles `: DbContext`, `: DbContext(o)`, `: IdentityDbContext<..>`) ---
    var dbContextClasses = classes.Where(c => BaseListContains(c.decl.BaseList, s => s == "DbContext" || s.EndsWith("DbContext") || s.Contains("DbContext<") || s.Contains("DbContext("))).ToList();
    var dbContextNames = dbContextClasses.Select(c => c.decl.Identifier.Text).Distinct().ToList();

    // --- entities: DbSet<T> property type args that are declared classes ---
    var dbSetTypes = new List<string>();
    foreach (var (decl, _) in classes)
        foreach (var p in decl.Members.OfType<PropertyDeclarationSyntax>())
            if (p.Type is GenericNameSyntax g && g.Identifier.Text == "DbSet" && g.TypeArgumentList.Arguments.Count == 1)
                dbSetTypes.Add(g.TypeArgumentList.Arguments[0].ToString());
    var entities = dbSetTypes.Distinct().Where(t => declaredClassNames.Contains(t)).ToList();

    // --- EF / Npgsql wiring ---
    var usesEfNamespace = usings.Any(u => u.StartsWith("Microsoft.EntityFrameworkCore"));
    var useNpgsql = invocationNames.Contains("UseNpgsql");

    // --- 1:N relationship: navigation prop to another entity, or an int FK property ---
    var entitySet = entities.ToHashSet();
    var entityDecls = classes.Where(c => entitySet.Contains(c.decl.Identifier.Text)).Select(c => c.decl).ToList();
    bool hasNavigation = entityDecls.Any(c => c.Members.OfType<PropertyDeclarationSyntax>().Any(p =>
    {
        var ts = p.Type.ToString();
        return entitySet.Any(e => ts == e || ts.Contains($"<{e}>"));
    }));
    bool hasFkProperty = entityDecls.Any(c => c.Members.OfType<PropertyDeclarationSyntax>().Any(p =>
        p.Identifier.Text.EndsWith("Id") && p.Type.ToString() is "int" or "int?" or "long" or "long?" or "Guid" or "Guid?"));
    bool relationship = hasNavigation || hasFkProperty || invocationNames.Contains("HasForeignKey");

    // --- Kafka ---
    var kafkaClient = usings.Any(u => u.Contains("Confluent.Kafka")) || genericNames.Contains("IProducer") || genericNames.Contains("ProducerBuilder");
    var kafkaProduce = invocationNames.Contains("Produce") || invocationNames.Contains("ProduceAsync") || genericNames.Contains("IProducer");

    // --- repositories ---
    var repoInterfaces = interfaces.Where(i => i.Identifier.Text.Contains("Repository")).Select(i => i.Identifier.Text).ToList();
    var repoClasses = classes.Where(c => c.decl.Identifier.Text.Contains("Repository")).ToList();
    var hasRepositoryInterface = repoInterfaces.Count > 0;
    var hasRepositoryImpl = repoClasses.Any(c => !c.decl.Modifiers.Any(m => m.Text == "abstract"));
    // base repository: an abstract or generic repository class, or a repo that inherits another repo (non-interface) base
    var baseRepository = repoClasses.Any(c => c.decl.Modifiers.Any(m => m.Text == "abstract") || c.decl.TypeParameterList != null)
        || repoClasses.Any(c => BaseListContains(c.decl.BaseList, s => s.Contains("Repository") && !Regex.IsMatch(s, @"(^|\.)I[A-Z]")));

    // --- use cases ---
    var useCaseClasses = classes.Where(c => c.decl.Identifier.Text.EndsWith("UseCase") || c.decl.Identifier.Text.EndsWith("Interactor")).ToList();
    var useCases = useCaseClasses.Select(c => c.decl.Identifier.Text).ToList();
    var useCasesPerFile = useCaseClasses.GroupBy(c => c.file).ToDictionary(g => g.Key, g => g.Count());
    var oneFilePerUseCase = useCaseClasses.Count > 0 && useCasesPerFile.Values.All(v => v == 1);

    // --- layering relationships (comments ignored — this is the syntax tree) ---
    var dbTokens = new HashSet<string>(dbContextNames) { "DbContext", "DbSet" };
    var controllerDecls = classes.Where(c => controllers.Contains(c.decl.Identifier.Text)).Select(c => c.decl).ToList();
    var controllerTouchesDbContext = controllerDecls.Any(c => RefNames(c).Overlaps(dbTokens));
    var controllerUsesUseCase = controllerDecls.Any(c => RefNames(c).Any(n => n.EndsWith("UseCase") || n.EndsWith("Interactor")));
    var useCaseTouchesDbContext = useCaseClasses.Any(c => RefNames(c.decl).Overlaps(dbTokens));
    var repoUsesEf = repoClasses.Any(c => RefNames(c.decl).Overlaps(dbTokens) || RefNames(c.decl).Contains("Set"));

    // --- minimal-API resource endpoints (informational) ---
    var minimalApiResourceEndpoints = mapEndpointRoutes.Count(r => r.Contains("api/") || r.Contains("api\\u002F"));

    // --- best-practice signals ---
    bool MethodsUseCancellation(IEnumerable<ClassDeclarationSyntax> decls) =>
        decls.Any(c => c.Members.OfType<MethodDeclarationSyntax>()
            .Any(m => m.ParameterList.Parameters.Any(p => p.Type != null && p.Type.ToString().Contains("CancellationToken"))));
    var controllersUseCancellation = MethodsUseCancellation(controllerDecls);
    var reposUseCancellation = MethodsUseCancellation(repoClasses.Select(c => c.decl));

    var typeNames = classes.Select(c => c.decl.Identifier.Text).Concat(records.Select(r => r.Identifier.Text)).ToList();
    var responseDtoTypes = typeNames.Where(n => n.EndsWith("Response") || n.EndsWith("Dto") || n.EndsWith("DTO")).Distinct().ToList();
    var controllersUseDtos = responseDtoTypes.Count > 0 && controllerDecls.Any(c => RefNames(c).Overlaps(responseDtoTypes));
    // Use cases that RETURN a response DTO (controllers then surface that via result.Value) —
    // this means responses are DTOs even when the controller never names the type.
    var useCasesReturnDtos = responseDtoTypes.Count > 0 && useCaseClasses.Any(c =>
        c.decl.Members.OfType<MethodDeclarationSyntax>().Any(m => responseDtoTypes.Any(d => m.ReturnType.ToString().Contains(d))));

    var usesExceptionHandler = classes.Any(c => BaseListContains(c.decl.BaseList, s => s.Contains("IExceptionHandler"))) || allNames.Contains("IExceptionHandler");
    var usesProblemDetails = invocationNames.Contains("AddProblemDetails") || allNames.Contains("ProblemDetails");
    var usesResultPattern = typeNames.Any(n => n.EndsWith("Result"));

    var databaseMigrate = invocationNames.Contains("Migrate") || invocationNames.Contains("MigrateAsync");

    // Kafka producer durability: Acks.All (or idempotence) + retries configured.
    var kafkaDurable = memberAccesses.Contains("Acks.All") || allNames.Contains("EnableIdempotence");

    // Resilient publish: the Produce/Publish call sits in a try whose catch does NOT rethrow,
    // so a messaging hiccup doesn't fail (500) the HTTP request after the data is persisted.
    static bool InResilientTry(InvocationExpressionSyntax inv)
    {
        foreach (var t in inv.Ancestors().OfType<TryStatementSyntax>())
        {
            if (!t.Block.Span.Contains(inv.Span)) continue; // inv is in a catch/finally, not the try block
            if (t.Catches.Count == 0) return false;
            return !t.Catches.Any(c => c.Block.DescendantNodes().OfType<ThrowStatementSyntax>().Any());
        }
        return false;
    }
    var kafkaPublishResilient = produceOrPublishInvocations.Any(InResilientTry);

    var result = new
    {
        ok = true,
        targetFrameworks,
        controllers,
        dbContexts = dbContextNames,
        entities,
        usesEfNamespace,
        useNpgsql,
        relationship,
        kafkaClient,
        kafkaProduce,
        hasRepositoryInterface,
        hasRepositoryImpl,
        baseRepository,
        useCases,
        oneFilePerUseCase,
        controllerUsesUseCase,
        controllerTouchesDbContext,
        useCaseTouchesDbContext,
        repoUsesEf,
        minimalApiResourceEndpoints,
        controllersUseCancellation,
        reposUseCancellation,
        responseDtoTypes,
        controllersUseDtos,
        useCasesReturnDtos,
        usesExceptionHandler,
        usesProblemDetails,
        usesResultPattern,
        databaseMigrate,
        kafkaDurable,
        kafkaPublishResilient,
    };

    Console.WriteLine(JsonSerializer.Serialize(result));
}
catch (Exception ex)
{
    Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
}
