// Shared by crap.cs and complexity.cs through #:include. See TOOLCHAIN.md.

using System.Xml.Linq;

sealed record CrapRow
{
    public required SourceMethod Method { get; init; }
    public required int CoveredLines { get; init; }
    public required int TotalLines { get; init; }

    /// <summary>True when no coverage report mentioned this member at all.</summary>
    public bool NeverObserved => TotalLines == 0;

    public double Coverage => TotalLines == 0 ? 0 : (double)CoveredLines / TotalLines;

    public double Crap
    {
        get
        {
            var cc = (double)Method.Cyclomatic;
            var uncovered = 1 - Coverage;
            return (cc * cc * uncovered * uncovered * uncovered) + cc;
        }
    }
}

/// <summary>Cobertura ingest and span-based correlation of covered lines to source methods.</summary>
static class CoverageData
{
    public static List<string> DiscoverReports(string repoRoot, string coverageDir)
    {
        var dir = Path.IsPathRooted(coverageDir)
            ? coverageDir
            : Path.Combine(repoRoot, coverageDir);

        return Directory.Exists(dir)
            ? [.. Directory.EnumerateFiles(dir, "coverage.cobertura.xml", SearchOption.AllDirectories).Order()]
            : [];
    }

    /// <summary>
    /// Reads every Cobertura report into a merged (absolute path, line) -> hits map.
    /// </summary>
    /// <remarks>
    /// Each report's <c>&lt;sources&gt;</c> root must be applied to that report's own
    /// <c>filename</c> values. coverlet derives the root from the common prefix of the
    /// assemblies it instrumented, so a project that only touches Core emits
    /// <c>Protocols/.../SWConstants.cs</c> while every other project emits
    /// <c>Core/src/Protocols/.../SWConstants.cs</c> for the same file. Keying on the raw
    /// relative path therefore double-counts shared code.
    ///
    /// Hits are accumulated rather than overwritten so a line executed by any one test
    /// project counts as covered overall.
    /// </remarks>
    public static Dictionary<(string Path, int Line), int> LoadCoverage(List<string> reports)
    {
        var hits = new Dictionary<(string, int), int>();

        foreach (var report in reports)
        {
            var doc = XDocument.Load(report);
            var roots = doc.Descendants("source")
                .Select(s => s.Value.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            foreach (var cls in doc.Descendants("class"))
            {
                var filename = cls.Attribute("filename")?.Value;
                if (string.IsNullOrEmpty(filename))
                    continue;

                var resolved = ResolveAgainstRoots(roots, filename);
                if (resolved is null)
                    continue;

                foreach (var line in cls.Descendants("line"))
                {
                    if (!int.TryParse(line.Attribute("number")?.Value, out var number))
                        continue;
                    if (!int.TryParse(line.Attribute("hits")?.Value, out var count))
                        continue;

                    var key = (resolved, number);
                    hits[key] = hits.TryGetValue(key, out var existing) ? existing + count : count;
                }
            }
        }

        return hits;
    }

    static string? ResolveAgainstRoots(List<string> roots, string filename)
    {
        foreach (var root in roots)
        {
            var candidate = Path.GetFullPath(Path.Combine(root, filename));
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Assigns each covered line to the innermost source method whose span contains it.
    /// </summary>
    /// <remarks>
    /// Matching on spans rather than names is what makes async work. coverlet records an
    /// async body under the compiler-generated <c>&lt;Name&gt;d__N::MoveNext</c>, but it
    /// keeps the original file and line numbers, so the lines fall inside the source
    /// method's span. Name-based matching misses this and scores every async method 0%.
    /// </remarks>
    public static List<CrapRow> Correlate(
        List<SourceMethod> methods,
        Dictionary<(string Path, int Line), int> hits,
        out int unmatchedCoverage,
        out int uninstrumented)
    {
        var byFile = methods
            .GroupBy(m => m.FilePath)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.EndLine - m.StartLine).ToList());

        var covered = new Dictionary<SourceMethod, (int Covered, int Total)>();
        unmatchedCoverage = 0;

        foreach (var ((path, line), count) in hits)
        {
            if (!byFile.TryGetValue(path, out var candidates))
            {
                unmatchedCoverage++;
                continue;
            }

            // Candidates are ordered narrowest-first, so the first containing span is innermost.
            var owner = candidates.FirstOrDefault(m => line >= m.StartLine && line <= m.EndLine);
            if (owner is null)
            {
                unmatchedCoverage++;
                continue;
            }

            var entry = covered.TryGetValue(owner, out var e) ? e : (0, 0);
            covered[owner] = (entry.Item1 + (count > 0 ? 1 : 0), entry.Item2 + 1);
        }

        var rows = covered.Select(kv => new CrapRow
        {
            Method = kv.Key,
            CoveredLines = kv.Value.Covered,
            TotalLines = kv.Value.Total,
        }).ToList();

        // A member with a body that never appeared in any coverage report was not exercised
        // at all — most often because its assembly has no unit test project. That is the
        // most dangerous code in the repo, so it must be reported as 0% covered rather than
        // dropped. Only members that emit no code are genuinely unmeasurable.
        uninstrumented = 0;
        foreach (var method in methods)
        {
            if (covered.ContainsKey(method))
                continue;

            if (!method.HasImplementation)
            {
                uninstrumented++;
                continue;
            }

            rows.Add(new CrapRow { Method = method, CoveredLines = 0, TotalLines = 0 });
        }

        return rows;
    }
}
