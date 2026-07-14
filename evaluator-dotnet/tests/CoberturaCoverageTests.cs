using BackendEvaluator.Core;
using Xunit;

namespace Evaluator.Tests;

/// <summary>
/// Coverage must be measured over <b>the code that matters</b> — the rubric's words. These tests pin the
/// two properties that make the number honest: the UNION across reports (so a multi-project suite isn't
/// understated), and the exclusion of lines no unit test can reach by construction.
/// </summary>
public class CoberturaCoverageTests
{
    private static string WriteReport(string dir, string name, params (string file, int line, int hits)[] lines)
    {
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);
        var byFile = lines.GroupBy(l => l.file);
        var classes = string.Join("", byFile.Select(g =>
            $"<class filename=\"{g.Key}\"><lines>" +
            string.Join("", g.Select(l => $"<line number=\"{l.line}\" hits=\"{l.hits}\"/>")) +
            "</lines></class>"));
        File.WriteAllText(path, $"<coverage><packages><package><classes>{classes}</classes></package></packages></coverage>");
        return path;
    }

    private static string TempDir() => Path.Combine(Path.GetTempPath(), "cov-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Generated_code_and_the_composition_root_are_excluded_from_the_denominator()
    {
        // THE REGRESSION THIS GUARDS: sonnet-5/run1 covered 786 of the 1086 lines it actually wrote (72%),
        // but was scored 32% — a Fail — because the denominator also carried ~1,350 uncoverable lines: EF
        // migration scaffolding (which the task REQUIRES), source-generated OpenAPI code under obj/, and
        // Program.cs (which the task's own no-WebApplicationFactory rule makes untestable). The evaluator
        // was grading the framework, not the model.
        var dir = TempDir();
        try
        {
            var report = WriteReport(dir, "coverage.cobertura.xml",
                ("src/App/Services/CardService.cs", 1, 1),          // real code, covered
                ("src/App/Services/CardService.cs", 2, 1),
                ("src/App/Services/CardService.cs", 3, 0),          // real code, NOT covered -> still counts
                ("src/App/Program.cs", 1, 0),                       // composition root -> excluded
                ("src/Infra/Migrations/InitialCreate.cs", 1, 0),    // EF scaffolding -> excluded
                ("obj/Debug/net10.0/Generated.g.cs", 1, 0));        // source-generated -> excluded

            var m = CoberturaCoverage.Merge(new[] { report });

            Assert.Equal(3, m.Coverable);   // only CardService.cs counts
            Assert.Equal(2, m.Covered);
            Assert.Equal(3, m.Excluded);
            Assert.Equal(2.0 / 3.0, m.LineRate, 3);
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void Untested_business_code_still_counts_against_the_submission()
    {
        // The exclusions must not become a loophole: everything that is NOT generated/composition-root
        // stays in the denominator, so leaving real code untested still costs — exactly as it should.
        var dir = TempDir();
        try
        {
            var report = WriteReport(dir, "coverage.cobertura.xml",
                ("src/Infra/Repositories/CardRepository.cs", 1, 0),
                ("src/Infra/Repositories/CardRepository.cs", 2, 0),
                ("src/Api/Controllers/CardsController.cs", 1, 1));

            var m = CoberturaCoverage.Merge(new[] { report });

            Assert.Equal(3, m.Coverable);
            Assert.Equal(1, m.Covered);
            Assert.Equal(0, m.Excluded);
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void Merge_is_the_union_across_reports_so_a_second_suite_cannot_understate_the_first()
    {
        var dir = TempDir();
        try
        {
            var a = WriteReport(dir, "a.cobertura.xml", ("src/App/S.cs", 1, 1), ("src/App/S.cs", 2, 0));
            var b = WriteReport(dir, "b.cobertura.xml", ("src/App/S.cs", 1, 0), ("src/App/S.cs", 2, 3));

            var m = CoberturaCoverage.Merge(new[] { a, b });

            Assert.Equal(2, m.Reports);
            Assert.Equal(2, m.Coverable);
            Assert.Equal(2, m.Covered);      // line 1 hit by A, line 2 hit by B -> both covered
            Assert.Equal(1.0, m.LineRate);
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }
}
