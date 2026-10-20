// Part of toolchain.cs, pulled in through #:include. See TOOLCHAIN.md.

/// <summary>Checks the command line before anything runs, and suggests the closest valid name for a typo.</summary>
static class Usage
{
    // Bullseye's own long options; everything else starting with "--" must be one of ours.
    static readonly string[] s_bullseyeOptions =
    [
        "--clear", "--dry-run", "--host", "--list-dependencies", "--list-inputs", "--list-targets",
        "--list-tree", "--no-color", "--no-extended-chars", "--parallel", "--skip-dependencies", "--verbose",
    ];

    // Commands answered without running a target, so they must not be silenced or saved.
    public static readonly string[] ListingOptions =
        ["--list-dependencies", "-D", "--list-inputs", "-I", "--list-targets", "-T", "--list-tree", "-t"];

    /// <summary>The first problem with the command line, or null when it is valid.</summary>
    public static string? Validate(string[] args, IReadOnlyCollection<string> targets)
    {
        string[] options = [.. Options.All.Select(o => o.Name), .. s_bullseyeOptions];

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var option = Options.All.FirstOrDefault(o => o.Name == arg);
            if (option is not null)
            {
                if (option.Value is not null)
                {
                    if (i + 1 >= args.Length)
                        return $"{arg} needs a value: {arg} {option.Value}";
                    i++;
                }

                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                if (!options.Contains(arg))
                    return $"Unknown option {arg}.{Suggest(arg, options)}";
            }
            else if (!arg.StartsWith('-') && !targets.Contains(arg))
            {
                return $"Unknown target '{arg}'.{Suggest(arg, targets)}";
            }
        }

        return null;
    }

    static string Suggest(string input, IEnumerable<string> candidates)
    {
        var best = candidates
            .Select(c => (Candidate: c, Distance: Distance(input.ToLowerInvariant(), c.ToLowerInvariant())))
            .OrderBy(c => c.Distance)
            .FirstOrDefault();

        return best.Candidate is not null && best.Distance <= Math.Max(2, input.Length / 3)
            ? $" Did you mean '{best.Candidate}'?"
            : "";
    }

    // Levenshtein distance; inputs are short command-line words.
    static int Distance(string a, string b)
    {
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1];
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var substitution = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                current[j] = Math.Min(substitution, Math.Min(previous[j] + 1, current[j - 1] + 1));
            }

            previous = current;
        }

        return previous[b.Length];
    }
}
