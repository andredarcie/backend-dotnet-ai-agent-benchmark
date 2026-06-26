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
}
