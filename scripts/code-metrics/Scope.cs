// Shared by crap.cs and complexity.cs through #:include. See TOOLCHAIN.md.

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// The scope flags every metric command accepts: --module, --changed and --base.
/// </summary>
sealed class ScopeArgs
{
    public List<string> Modules { get; } = [];
    public bool Changed { get; set; }
    public string? BaseRef { get; set; }

    /// <summary>
    /// Consumes a scope flag at <paramref name="i"/>, advancing past its value. Returns false
    /// for anything else, so the caller can handle its own options.
    /// </summary>
    public bool TryConsume(string[] args, ref int i)
    {
        switch (args[i])
        {
            case "--module" when i + 1 < args.Length:
                Modules.Add(args[++i]);
                return true;
            case "--changed":
                Changed = true;
                return true;
            case "--base" when i + 1 < args.Length:
                // A base only makes sense for a changed-lines scope, so it implies one.
                BaseRef = args[++i];
                Changed = true;
                return true;
            default:
                return false;
        }
    }

    public const string Usage = """
          --module <Name>            Only src/<Name>/src/, repeatable
          --changed                  Only methods overlapping lines changed vs HEAD (staged, unstaged, untracked)
          --base <ref>               Compare with <ref> instead of HEAD; implies --changed
        """;
}

/// <summary>
/// A resolved scope: which shipping files and which method spans a metric looks at.
/// </summary>
sealed class MetricScope
{
    public required string RepoRoot { get; init; }

    /// <summary>Canonical module names, or empty for every module.</summary>
    public required IReadOnlyList<string> Modules { get; init; }

    /// <summary>Changed lines per repo-relative path, or null when the scope is not changed-lines.</summary>
    public required IReadOnlyDictionary<string, FileChanges>? Changes { get; init; }

    public required string BaseRef { get; init; }

    public bool IsChanged => Changes is not null;
    public bool IsRestricted => IsChanged || Modules.Count > 0;

    public static MetricScope Everything(string repoRoot) => new()
    {
        RepoRoot = repoRoot,
        Modules = [],
        Changes = null,
        BaseRef = "HEAD",
    };

    /// <summary>Validates module names and, for a changed scope, reads the diff from git.</summary>
    /// <returns>Null after printing an error.</returns>
    public static MetricScope? Resolve(string repoRoot, ScopeArgs args)
    {
        var modules = new List<string>();
        if (args.Modules.Count > 0)
        {
            var known = KnownModules(repoRoot);
            foreach (var requested in args.Modules)
            {
                var match = known.FirstOrDefault(m => string.Equals(m, requested, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    Console.Error.WriteLine(
                        $"error: unknown module '{requested}'. Known modules: {string.Join(", ", known)}");
                    return null;
                }

                if (!modules.Contains(match, StringComparer.Ordinal))
                    modules.Add(match);
            }
        }

        var baseRef = args.BaseRef ?? "HEAD";
        IReadOnlyDictionary<string, FileChanges>? changes = null;
        if (args.Changed)
        {
            changes = ReadChanges(repoRoot, baseRef);
            if (changes is null)
                return null;
        }

        return new MetricScope { RepoRoot = repoRoot, Modules = modules, Changes = changes, BaseRef = baseRef };
    }

    public bool IncludesFile(string absolutePath) => IncludesRelative(Repo.Relative(RepoRoot, absolutePath));

    public bool IncludesRelative(string repoRelativePath)
    {
        var path = repoRelativePath.Replace('\\', '/');

        if (Modules.Count > 0 && !Modules.Any(m => path.StartsWith($"src/{m}/src/", StringComparison.Ordinal)))
            return false;

        return Changes is null || Changes.ContainsKey(path);
    }

    public bool Includes(SourceMethod method)
    {
        var path = Repo.Relative(RepoRoot, method.FilePath);
        if (!IncludesRelative(path))
            return false;

        return Changes is null || Changes[path].Touches(method.StartLine, method.EndLine);
    }

    public string Describe()
    {
        var modules = Modules.Count switch
        {
            0 => null,
            1 => $"module {Modules[0]}",
            _ => $"modules {string.Join(", ", Modules)}",
        };

        if (IsChanged)
            return modules is null ? $"changed lines vs {BaseRef}" : $"changed lines vs {BaseRef} in {modules}";

        return modules ?? "whole shipping SDK";
    }

    /// <summary>Module names that have a shipping src/ folder, i.e. src/&lt;Name&gt;/src/.</summary>
    static List<string> KnownModules(string repoRoot)
    {
        var src = Path.Combine(repoRoot, "src");
        if (!Directory.Exists(src))
            return [];

        return [.. Directory.EnumerateDirectories(src)
            .Where(d => Directory.Exists(Path.Combine(d, "src")))
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Working-tree changes against <paramref name="baseRef"/>, staged and unstaged, plus
    /// untracked C# files in full.
    /// </summary>
    static Dictionary<string, FileChanges>? ReadChanges(string repoRoot, string baseRef)
    {
        // Explicit prefixes override diff.noprefix and diff.mnemonicPrefix, which would
        // otherwise change the "+++ b/" header the parser relies on. --relative keeps paths
        // relative to the repo root even when it is not the git top level.
        var diff = Repo.Git(repoRoot,
            "diff", "--no-color", "--no-ext-diff", "--no-textconv", "--unified=0",
            "--src-prefix=a/", "--dst-prefix=b/", "--relative", baseRef, "--", "*.cs");

        if (diff.ExitCode != 0)
        {
            Console.Error.WriteLine($"error: git diff against '{baseRef}' failed: {diff.StdErr.Trim()}");
            return null;
        }

        var changes = DiffParser.Parse(diff.StdOut);

        // -z prints paths verbatim, without git's C-style quoting.
        var untracked = Repo.Git(repoRoot, "ls-files", "-z", "--others", "--exclude-standard", "--", "*.cs");
        if (untracked.ExitCode != 0)
        {
            Console.Error.WriteLine($"error: git ls-files failed: {untracked.StdErr.Trim()}");
            return null;
        }

        foreach (var path in untracked.StdOut.Split('\0', StringSplitOptions.RemoveEmptyEntries))
            changes[path] = FileChanges.WholeFile();

        return changes;
    }
}

/// <summary>Which lines of one file changed, in new-file line numbers.</summary>
sealed class FileChanges
{
    readonly List<(int Start, int End)> _lines = [];

    // A pure deletion sits between line p and p + 1 of the new file.
    readonly List<int> _deletionPoints = [];

    bool _wholeFile;

    public static FileChanges WholeFile() => new() { _wholeFile = true };

    public void AddLines(int start, int end) => _lines.Add((start, end));

    public void AddDeletionAfter(int line) => _deletionPoints.Add(line);

    /// <summary>
    /// True when a changed line falls within [start, end], or a deletion falls strictly inside
    /// it. A deletion on a method's boundary more likely removed the neighbouring member, so it
    /// does not count.
    /// </summary>
    public bool Touches(int start, int end) =>
        _wholeFile
        || _lines.Any(l => l.Start <= end && l.End >= start)
        || _deletionPoints.Any(p => p >= start && p + 1 <= end);
}

/// <summary>Parses `git diff --unified=0` output into changed line ranges per file.</summary>
static class DiffParser
{
    static readonly Regex HunkHeader = new(@"^@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@", RegexOptions.Compiled);

    public static Dictionary<string, FileChanges> Parse(string diff)
    {
        var result = new Dictionary<string, FileChanges>(StringComparer.Ordinal);
        FileChanges? current = null;

        // File headers only appear between "diff --git" and the first hunk. Tracking that
        // keeps an added source line such as "++ b/x" from being read as a new file header.
        var inHeader = false;

        foreach (var raw in diff.Split('\n'))
        {
            var line = raw.TrimEnd('\r');

            if (line.StartsWith("diff --git ", StringComparison.Ordinal))
            {
                inHeader = true;
                current = null;
                continue;
            }

            if (inHeader && line.StartsWith("+++ ", StringComparison.Ordinal))
            {
                current = ParseNewPath(line[4..]) is { } path
                    ? result.TryGetValue(path, out var existing) ? existing : result[path] = new FileChanges()
                    : null;
                continue;
            }

            if (current is null || !line.StartsWith("@@ ", StringComparison.Ordinal))
                continue;

            inHeader = false;
            var match = HunkHeader.Match(line);
            if (!match.Success)
                continue;

            var start = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var count = match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : 1;

            if (count == 0)
                current.AddDeletionAfter(start);
            else
                current.AddLines(start, start + count - 1);
        }

        return result;
    }

    /// <summary>"b/src/x.cs" -> "src/x.cs"; "/dev/null" (a deleted file) -> null.</summary>
    static string? ParseNewPath(string header)
    {
        // git appends a tab after a path that contains spaces.
        var path = header.TrimEnd('\t');
        if (path == "/dev/null")
            return null;

        if (path.StartsWith('"'))
            path = Unquote(path);

        return path.StartsWith("b/", StringComparison.Ordinal) ? path[2..] : path;
    }

    /// <summary>
    /// Decodes git's C-style quoted path, used for names containing a quote, backslash, or
    /// control character: <c>"b/src/a\"b.cs"</c> -> <c>b/src/a"b.cs</c>. Octal escapes are
    /// UTF-8 bytes.
    /// </summary>
    public static string Unquote(string quoted)
    {
        var bytes = new List<byte>();
        var inner = quoted.AsSpan(1, quoted.Length - 2);

        for (var i = 0; i < inner.Length; i++)
        {
            if (inner[i] != '\\' || i + 1 >= inner.Length)
            {
                // Keep surrogate pairs together; with core.quotepath=off, non-ASCII is raw.
                var width = char.IsHighSurrogate(inner[i]) && i + 1 < inner.Length ? 2 : 1;
                bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(inner.Slice(i, width).ToString()));
                i += width - 1;
                continue;
            }

            var next = inner[++i];
            if (next is >= '0' and <= '7' && i + 2 < inner.Length)
            {
                bytes.Add(Convert.ToByte(inner.Slice(i, 3).ToString(), 8));
                i += 2;
                continue;
            }

            bytes.Add(next switch
            {
                'a' => (byte)'\a',
                'b' => (byte)'\b',
                'f' => (byte)'\f',
                'n' => (byte)'\n',
                'r' => (byte)'\r',
                't' => (byte)'\t',
                'v' => (byte)'\v',
                _ => (byte)next,
            });
        }

        return System.Text.Encoding.UTF8.GetString([.. bytes]);
    }
}
