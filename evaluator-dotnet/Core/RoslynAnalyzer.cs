using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BackendEvaluator.Core;

/// <summary>
/// Parses the target's C# files with Roslyn (syntactically — no compilation needed) and produces a
/// <see cref="CodeFacts"/>. Using the real compiler front-end makes detection robust to comments,
/// string literals, formatting, primary constructors and partial classes — unlike regex.
/// </summary>
public static class RoslynAnalyzer
{
    public static CodeFacts Analyze(ProjectInspector project)
    {
        var f = new CodeFacts();
        var classDecls = new List<(ClassDeclarationSyntax decl, string file)>();
        var baseTypeRefs = new List<string>();

        foreach (var file in project.SourceFiles)
        {
            string text;
            try { text = File.ReadAllText(file); } catch { continue; }

            SyntaxNode root;
            try { root = CSharpSyntaxTree.ParseText(text).GetRoot(); }
            catch { f.ParseErrors++; continue; }
            f.FilesParsed++;
            bool isTestFile = project.IsTestFile(file);

            foreach (var u in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
                if (u.Name != null) f.Usings.Add(u.Name.ToString());

            foreach (var inv in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                switch (inv.Expression)
                {
                    case MemberAccessExpressionSyntax ma: f.InvocationNames.Add(ma.Name.Identifier.Text); break;
                    case IdentifierNameSyntax idn: f.InvocationNames.Add(idn.Identifier.Text); break;
                }
            }

            foreach (var sn in root.DescendantNodes().OfType<SimpleNameSyntax>()) f.IdentifierNames.Add(sn.Identifier.Text);
            foreach (var g in root.DescendantNodes().OfType<GenericNameSyntax>()) f.GenericNames.Add(g.Identifier.Text);
            foreach (var ma in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
                f.MemberAccesses.Add(ma.Expression + "." + ma.Name.Identifier.Text);
            foreach (var oc in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                f.ObjectCreationTypes.Add(oc.Type.ToString());

            foreach (var a in root.DescendantNodes().OfType<AttributeSyntax>())
            {
                var name = a.Name.ToString();
                var simple = name.Contains('.') ? name[(name.LastIndexOf('.') + 1)..] : name;
                if (simple.EndsWith("Attribute", StringComparison.Ordinal)) simple = simple[..^"Attribute".Length];
                f.AttributeNames.Add(simple);
            }

            foreach (var lit in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
                if (lit.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    f.StringLiterals.Add(lit.Token.ValueText);
                    if (!isTestFile) f.ProductionStringLiterals.Add(lit.Token.ValueText);
                }

            foreach (var c in root.DescendantNodes().OfType<CatchClauseSyntax>())
                if (c.Block.Statements.Count == 0) f.EmptyCatchCount++;

            foreach (var m in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                if (m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword))) f.AsyncMethodCount++;

            // Blocking sync-over-async: `.Wait()` / `.GetResult()` invocations (AST-scoped).
            foreach (var inv in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                if (inv.Expression is MemberAccessExpressionSyntax m2 && m2.Name.Identifier.Text is "Wait" or "GetResult")
                    f.HasBlockingCalls = true;

            if (root.DescendantNodes().OfType<StackAllocArrayCreationExpressionSyntax>().Any()) f.HasUnsafeOrStackalloc = true;
            if (root.DescendantTokens().Any(t => t.IsKind(SyntaxKind.UnsafeKeyword))) f.HasUnsafeOrStackalloc = true;

            foreach (var fld in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                var mods = fld.Modifiers.Select(x => x.Text).ToHashSet();
                if (mods.Contains("static") && !mods.Contains("const") && !mods.Contains("readonly")) f.StaticMutableFieldCount++;
            }

            foreach (var tr in root.DescendantTrivia())
            {
                if (tr.IsKind(SyntaxKind.SingleLineCommentTrivia) || tr.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    if (Regex.IsMatch(tr.ToString(), @"\b(TODO|FIXME|HACK)\b")) f.TodoCommentCount++;
                }
                else if (tr.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || tr.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    f.DocCommentCount++;
                }
            }

            foreach (var c in root.DescendantNodes().OfType<ClassDeclarationSyntax>()) { classDecls.Add((c, file)); f.TypeNames.Add(c.Identifier.Text); }
            foreach (var rec in root.DescendantNodes().OfType<RecordDeclarationSyntax>()) f.TypeNames.Add(rec.Identifier.Text);
            foreach (var i in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>()) f.InterfaceNames.Add(i.Identifier.Text);

            foreach (var bl in root.DescendantNodes().OfType<BaseListSyntax>())
                foreach (var t in bl.Types) baseTypeRefs.Add(t.Type.ToString());

            foreach (var p in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                if (p.Type is GenericNameSyntax gg && gg.Identifier.Text == "DbSet" && gg.TypeArgumentList.Arguments.Count == 1)
                    f.DbSetTypes.Add(gg.TypeArgumentList.Arguments[0].ToString());

            foreach (var td in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                var span = td.GetLocation().GetLineSpan();
                int lines = span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
                if (lines > f.LargestTypeLines) { f.LargestTypeLines = lines; f.LargestTypeName = td.Identifier.Text; }
            }

            if (Regex.IsMatch(file, @"[\\/](Domain|Entities)[\\/]", RegexOptions.IgnoreCase))
            {
                bool leak = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(u =>
                    u.Name != null && Regex.IsMatch(u.Name.ToString(), "EntityFrameworkCore|Npgsql|Confluent\\.Kafka"));
                if (leak) f.DomainInfraLeakFiles++;
            }
        }

        foreach (var iface in f.InterfaceNames.Distinct())
        {
            int impls = baseTypeRefs.Count(b => b == iface || b.EndsWith("." + iface, StringComparison.Ordinal) || b.StartsWith(iface + "<", StringComparison.Ordinal));
            f.InterfaceImplementers[iface] = impls;
        }

        // 1:N relationship: a navigation property to another entity, an "<Other>Id" FK, a [ForeignKey],
        // or a fluent HasForeignKey/HasOne/WithMany call.
        var entitySet = f.DbSetTypes.Where(t => f.TypeNames.Contains(t)).ToHashSet();
        var entityDecls = classDecls.Where(c => entitySet.Contains(c.decl.Identifier.Text)).Select(c => c.decl).ToList();
        bool hasNav = entityDecls.Any(c => c.Members.OfType<PropertyDeclarationSyntax>().Any(p =>
        {
            var ts = p.Type.ToString();
            return entitySet.Any(e => ts == e || ts.Contains($"<{e}>"));
        }));
        static bool IsFkType(string ts) => ts is "int" or "int?" or "long" or "long?" or "Guid" or "Guid?";
        bool hasFk = entityDecls.Any(c => c.Members.OfType<PropertyDeclarationSyntax>().Any(p =>
        {
            var pn = p.Identifier.Text;
            if (pn == "Id" || !pn.EndsWith("Id", StringComparison.Ordinal) || !IsFkType(p.Type.ToString())) return false;
            var prefix = pn[..^2];
            return entitySet.Any(e => e != c.Identifier.Text && (prefix == e || e.EndsWith(prefix, StringComparison.Ordinal) || prefix.EndsWith(e, StringComparison.Ordinal)));
        }));
        bool hasFkAttr = entityDecls.Any(c => c.Members.OfType<PropertyDeclarationSyntax>()
            .Any(p => p.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString().Contains("ForeignKey"))));
        f.Relationship = hasNav || hasFk || hasFkAttr || f.Invokes("HasForeignKey", "HasOne", "WithMany");

        f.HasOutboxType = f.TypeNames.Any(n => n.Contains("Outbox", StringComparison.OrdinalIgnoreCase))
                          || f.DbSetTypes.Any(t => t.Contains("Outbox", StringComparison.OrdinalIgnoreCase));

        f.Available = f.FilesParsed > 0;
        return f;
    }
}
