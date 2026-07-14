using System.Xml.Linq;

namespace BackendEvaluator.Core;

/// <summary>
/// Filesystem + project-file inspection (no code-regex). C# code analysis lives in
/// <see cref="RoslynAnalyzer"/>; here we only deal with files, .csproj XML and small text files.
/// </summary>
public sealed class ProjectInspector
{
    private static readonly string[] IgnoreDirs =
        { "bin", "obj", "node_modules", ".git", ".vs", ".idea", "TestResults", "dist", "packages" };

    public string Root { get; }
    private readonly List<string> _allFiles;
    private readonly List<string> _sourceFiles;
    private readonly List<string> _csprojFiles;
    private readonly HashSet<string> _testRoots = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Package ids referenced by any .csproj (parsed from XML).</summary>
    public HashSet<string> Packages { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>MSBuild properties found in any .csproj PropertyGroup (last value wins).</summary>
    public Dictionary<string, string> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);

    public ProjectInspector(string root)
    {
        Root = Path.GetFullPath(root);
        _allFiles = SafeEnumerate(Root).ToList();
        _sourceFiles = _allFiles.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                                            && !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                                            && !f.Contains("GlobalUsings", StringComparison.OrdinalIgnoreCase)).ToList();
        _csprojFiles = _allFiles.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).ToList();
        ParseProjects();
    }

    public IReadOnlyList<string> AllFiles => _allFiles;
    public IReadOnlyList<string> SourceFiles => _sourceFiles;
    public IReadOnlyList<string> CsprojFiles => _csprojFiles;

    private static IEnumerable<string> SafeEnumerate(string root)
    {
        if (!Directory.Exists(root)) yield break;
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            string[] subdirs = Array.Empty<string>();
            try { subdirs = Directory.GetDirectories(dir); } catch { }
            foreach (var sd in subdirs)
            {
                var name = Path.GetFileName(sd);
                if (IgnoreDirs.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                stack.Push(sd);
            }
            string[] files = Array.Empty<string>();
            try { files = Directory.GetFiles(dir); } catch { }
            foreach (var f in files) yield return f;
        }
    }

    private static readonly string[] TestPackageMarkers =
        { "xunit", "nunit", "MSTest", "Test.Sdk", "Testcontainers", "FluentAssertions", "Moq", "NSubstitute" };

    private void ParseProjects()
    {
        foreach (var proj in _csprojFiles)
        {
            XDocument doc;
            try { doc = XDocument.Load(proj); } catch { continue; }

            bool isTestProject = Path.GetFileNameWithoutExtension(proj).Contains("Test", StringComparison.OrdinalIgnoreCase)
                                 || doc.Descendants().Any(e => e.Name.LocalName == "IsTestProject"
                                                               && string.Equals(e.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase));

            foreach (var pr in doc.Descendants().Where(e => e.Name.LocalName == "PackageReference"))
            {
                var id = pr.Attribute("Include")?.Value ?? pr.Attribute("Update")?.Value;
                if (string.IsNullOrWhiteSpace(id)) continue;
                Packages.Add(id.Trim());
                if (TestPackageMarkers.Any(m => id.Contains(m, StringComparison.OrdinalIgnoreCase))) isTestProject = true;
            }

            foreach (var pg in doc.Descendants().Where(e => e.Name.LocalName == "PropertyGroup"))
                foreach (var prop in pg.Elements())
                    Properties[prop.Name.LocalName] = prop.Value.Trim();

            if (isTestProject)
            {
                var dir = Path.GetDirectoryName(Path.GetFullPath(proj));
                if (dir != null) _testRoots.Add(dir);
            }
        }
    }

    /// <summary>
    /// True when a file belongs to a test project (so it is excluded from production-only scans such
    /// as the PCI/PAN check — standard fake test card numbers are fixtures, not stored secrets).
    /// </summary>
    public bool IsTestFile(string path)
    {
        var full = Path.GetFullPath(path);
        foreach (var root in _testRoots)
            if (full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
                return true;
        // Fallback for tests not in a dedicated .csproj: a path segment that reads as a test folder/project.
        return full.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(seg => System.Text.RegularExpressions.Regex.IsMatch(seg, @"^(test|tests)$|(^|\.)(Unit|Integration|Acceptance|Functional|E2E)?Tests?$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>The C# files that are NOT part of a test project.</summary>
    public IReadOnlyList<string> ProductionSourceFiles => _sourceFiles.Where(f => !IsTestFile(f)).ToList();

    /// <summary>True when any referenced package id contains <paramref name="substring"/> (case-insensitive).</summary>
    public bool HasPackage(string substring) => Packages.Any(p => p.Contains(substring, StringComparison.OrdinalIgnoreCase));

    /// <summary>True when an MSBuild property equals the given value (case-insensitive).</summary>
    public bool PropertyIs(string name, string value)
        => Properties.TryGetValue(name, out var v) && string.Equals(v, value, StringComparison.OrdinalIgnoreCase);

    public IEnumerable<string> FindByName(params string[] names)
        => _allFiles.Where(f => names.Any(n => string.Equals(Path.GetFileName(f), n, StringComparison.OrdinalIgnoreCase)));

    public IEnumerable<string> FindByNamePattern(string regex)
    {
        var re = new System.Text.RegularExpressions.Regex(regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return _allFiles.Where(f => re.IsMatch(Path.GetFileName(f)));
    }

    public bool AnyFile(params string[] names) => FindByName(names).Any();

    /// <summary>
    /// True when any path segment is named <paramref name="name"/> — or <b>ends with <c>.name</c></b>.
    ///
    /// The suffix rule matters: the idiomatic .NET layered layout is a PROJECT PER LAYER
    /// (<c>CreditCardApi.Domain</c>, <c>CreditCardApi.Application</c>, <c>CreditCardApi.Infrastructure</c>),
    /// not a folder literally called "Application". Matching the exact segment only would reward the
    /// weaker layout (folders inside one project) and penalize the stronger one — a submission that gives
    /// the application layer its own assembly, with compiler-enforced dependency direction, would score
    /// "no application layer". That is the rubric grading a naming convention instead of the architecture.
    /// </summary>
    public bool AnyDir(string name) => _allFiles.Any(f => f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                                          .Any(seg => string.Equals(seg, name, StringComparison.OrdinalIgnoreCase)
                                                                      || seg.EndsWith("." + name, StringComparison.OrdinalIgnoreCase)));

    public bool AnyPathContains(string fragment) => _allFiles.Any(f => f.Replace('\\', '/').Contains(fragment, StringComparison.OrdinalIgnoreCase));

    public string? ReadFirst(params string[] names)
    {
        var file = FindByName(names).FirstOrDefault();
        if (file == null) return null;
        try { return File.ReadAllText(file); } catch { return null; }
    }

    public string Rel(string fullPath)
    {
        try { return Path.GetRelativePath(Root, fullPath); } catch { return fullPath; }
    }
}
