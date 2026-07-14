using BackendEvaluator.Core;
using Xunit;

namespace Evaluator.Tests;

public class ProjectInspectorTests
{
    // An inspector over an empty dir: no .csproj test roots, so IsTestFile exercises the path fallback.
    private static ProjectInspector OnEmptyDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "evtest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return new ProjectInspector(dir);
    }

    [Theory]
    [InlineData("/repo/tests/Foo.cs", true)]
    [InlineData("/repo/test/Foo.cs", true)]
    [InlineData("/repo/CreditCardApi.Tests/Foo.cs", true)]
    [InlineData("/repo/CreditCardApi.IntegrationTests/SomeTest.cs", true)]
    [InlineData("/repo/src/CreditCardApi.Api/Program.cs", false)]
    [InlineData("/repo/src/Domain/CreditCard.cs", false)]
    [InlineData("/repo/src/Latest/Greatest.cs", false)] // "Latest"/"Greatest" must NOT read as tests
    public void IsTestFile_detects_test_paths_via_fallback(string path, bool expected)
    {
        var p = OnEmptyDir();
        Assert.Equal(expected, p.IsTestFile(path.Replace('/', Path.DirectorySeparatorChar)));
    }

    // AnyDir must recognize the PROJECT-PER-LAYER convention (CreditCardApi.Application), not just a
    // folder literally named "Application". Otherwise the rubric rewards the weaker layout (folders in
    // one assembly) and fails the stronger one (a real assembly boundary per layer) — grading a naming
    // convention instead of the architecture. Regression: sonnet-5/run1 scored "no application layer"
    // while shipping an entire CreditCardApi.Application project.
    [Theory]
    [InlineData("src/CreditCardApi.Application/CreditCards/CreditCardService.cs", "Application", true)]
    [InlineData("src/CreditCardApi.Domain/Entities/CreditCard.cs", "Domain", true)]
    [InlineData("src/CreditCardApi.Infrastructure/Data/AppDbContext.cs", "Infrastructure", true)]
    [InlineData("src/Application/Services/Foo.cs", "Application", true)]        // the folder layout still counts
    [InlineData("src/CreditCardApi.Api/Program.cs", "Application", false)]      // unrelated project: no match
    [InlineData("src/MyApplication/Foo.cs", "Application", false)]              // suffix must follow a dot
    public void AnyDir_matches_a_segment_or_the_dotted_project_per_layer_suffix(string relPath, string layer, bool expected)
    {
        var dir = Path.Combine(Path.GetTempPath(), "evtest-" + Guid.NewGuid().ToString("N"));
        var file = Path.Combine(dir, relPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "// fixture");
        try
        {
            Assert.Equal(expected, new ProjectInspector(dir).AnyDir(layer));
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }
}
