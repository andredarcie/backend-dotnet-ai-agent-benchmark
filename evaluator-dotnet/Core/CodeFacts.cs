namespace BackendEvaluator.Core;

/// <summary>
/// Structural facts about the target's C# code, produced by <see cref="RoslynAnalyzer"/> from the
/// real syntax tree (AST) — not regex. Evaluators query this instead of grepping source text.
/// </summary>
public sealed class CodeFacts
{
    public bool Available { get; set; }
    public int FilesParsed { get; set; }
    public int ParseErrors { get; set; }

    // Raw token/name sets collected from the AST.
    public HashSet<string> Usings { get; } = new();
    public HashSet<string> InvocationNames { get; } = new();      // method names actually invoked
    public HashSet<string> IdentifierNames { get; } = new();      // every simple name referenced
    public HashSet<string> GenericNames { get; } = new();         // generic type names (DbSet, IProducer)
    public HashSet<string> MemberAccesses { get; } = new();       // e.g. "Acks.All"
    public HashSet<string> AttributeNames { get; } = new();       // normalized: "HttpGet", "Authorize"
    public HashSet<string> ObjectCreationTypes { get; } = new();  // e.g. "ProblemDetails"
    public HashSet<string> TypeNames { get; } = new();
    public List<string> InterfaceNames { get; } = new();
    public Dictionary<string, int> InterfaceImplementers { get; } = new();
    public List<string> StringLiterals { get; } = new();          // for PAN/secret scanning (all files)
    public List<string> ProductionStringLiterals { get; } = new();// excludes test projects (PCI scan)
    public List<string> DbSetTypes { get; } = new();

    // Computed signals.
    public int EmptyCatchCount { get; set; }
    public int AsyncMethodCount { get; set; }
    public bool HasBlockingCalls { get; set; }
    public int TodoCommentCount { get; set; }
    public int DocCommentCount { get; set; }
    public int LargestTypeLines { get; set; }
    public string? LargestTypeName { get; set; }
    public int StaticMutableFieldCount { get; set; }
    public int DomainInfraLeakFiles { get; set; }
    public bool HasUnsafeOrStackalloc { get; set; }
    public bool Relationship { get; set; }
    public bool HasOutboxType { get; set; }

    // Query helpers (AST-backed, so they ignore comments/strings/formatting).
    public bool Invokes(params string[] names) => names.Any(InvocationNames.Contains);
    public bool UsesGeneric(params string[] names) => names.Any(GenericNames.Contains);
    public bool UsesAttribute(params string[] names) => names.Any(AttributeNames.Contains);
    public bool UsesNamespace(params string[] prefixes) => Usings.Any(u => prefixes.Any(p => u.StartsWith(p, StringComparison.OrdinalIgnoreCase)));
    public bool HasMemberAccess(params string[] names) => names.Any(MemberAccesses.Contains);
    public bool IdentifierEquals(params string[] names) => names.Any(IdentifierNames.Contains);
    public bool IdentifierContains(params string[] subs) => IdentifierNames.Any(id => subs.Any(s => id.Contains(s, StringComparison.OrdinalIgnoreCase)));
    public bool TypeNameContains(params string[] subs) => TypeNames.Any(t => subs.Any(s => t.Contains(s, StringComparison.OrdinalIgnoreCase)));

    public int SingleImplementationInterfaces => InterfaceImplementers.Count(kv => kv.Value <= 1);
    public int InterfaceCount => InterfaceImplementers.Count;
}
