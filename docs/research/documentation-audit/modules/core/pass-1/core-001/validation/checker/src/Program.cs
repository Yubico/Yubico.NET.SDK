using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

return await RunAsync(args);

static async Task<int> RunAsync(string[] arguments)
{
    try
    {
        return arguments switch
        {
            ["tokens", var root] => await CheckTokensAsync(Path.GetFullPath(root)),
            ["api", var root] => await CheckApiAsync(Path.GetFullPath(root)),
            ["selfcheck"] => Selfcheck(),
            _ => Usage()
        };
    }
    catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException or JsonException)
    {
        Console.Error.WriteLine($"FAIL: {exception.Message}");
        return 2;
    }
}

static int Usage()
{
    Console.Error.WriteLine("Usage: Audit20261111.DocCheck tokens <repo> | api <repo> | selfcheck");
    return 2;
}

static async Task<int> CheckTokensAsync(string root)
{
    const string baseline = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef";
    string oldListing = await GitTextAsync(root, "ls-tree", "-r", "--name-only", "-z", baseline);
    string currentListing = await GitTextAsync(root, "ls-files", "--cached", "--others", "--exclude-standard", "-z");
    string[] paths = [.. SplitPaths(oldListing).Where(IsExecutableSource).Order(StringComparer.Ordinal)];
    string[] current = [.. SplitPaths(currentListing).Where(IsExecutableSource).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    if (paths.Length == 0 || !paths.SequenceEqual(current))
    {
        throw new InvalidOperationException("Executable C# membership changed or baseline has no source.");
    }
    int failures = 0;
    foreach (string path in paths)
    {
        string before = await GitTextAsync(root, "show", $"{baseline}:{path}");
        string after = File.ReadAllText(Path.Combine(root, path));
        bool identical = Shape(before).SequenceEqual(Shape(after));
        Console.WriteLine($"{(identical ? "PASS" : "FAIL")}: tokens/directives {path}; SHA256={Hash(File.ReadAllBytes(Path.Combine(root, path)))}");
        failures += identical ? 0 : 1;
    }
    Console.WriteLine($"Token comparison against {baseline}: files={paths.Length}, failures={failures}; exact import order and all executable structure retained.");
    return failures == 0 ? 0 : 1;
}

static bool IsExecutableSource(string path)
{
    string[] frozenRoots = ["docs/research/", "docs/archive/", "docs/completed/", ".claude/"];
    return path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
        !frozenRoots.Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal));
}

static string[] Shape(string text)
{
    var parsed = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.CSharp14)).GetRoot();
    // The full-fidelity parser retains directives, disabled branches and malformed skipped text.
    // Imports, signatures, modifiers, defaults, attributes and literals receive the same treatment.
    var result = new List<string>();
    int tokenIndex = 0;
    foreach (SyntaxToken token in parsed.DescendantTokens())
    {
        AddSignificantTrivia(token.LeadingTrivia, tokenIndex, "leading", result);
        result.Add($"{token.RawKind}:{JsonSerializer.Serialize(token.Text)}");
        AddSignificantTrivia(token.TrailingTrivia, tokenIndex, "trailing", result);
        tokenIndex++;
    }
    return [.. result];
}

static void AddSignificantTrivia(SyntaxTriviaList items, int tokenIndex, string side, List<string> result)
{
    foreach (SyntaxTrivia item in items.Where(item => item.IsDirective ||
                 item.IsKind(SyntaxKind.DisabledTextTrivia) || item.IsKind(SyntaxKind.SkippedTokensTrivia)))
    {
        result.Add($"{tokenIndex}:{side}:{item.RawKind}:{JsonSerializer.Serialize(item.ToFullString())}");
    }
}

static int Selfcheck()
{
    const string before = "using A; using B; public class C { public const string S = \"value\"; public int M(int p = 1) => p; }";
    if (!Shape(before).SequenceEqual(Shape("// documentation\n" + before)))
    {
        throw new InvalidOperationException("Ordinary comments changed executable shape.");
    }
    string[] changed = [before.Replace("using A; using B;", "using B; using A;", StringComparison.Ordinal),
        before.Replace("public class", "internal class", StringComparison.Ordinal),
        before.Replace("p = 1", "p = 2", StringComparison.Ordinal),
        before.Replace("value", "changed", StringComparison.Ordinal)];
    if (changed.Any(text => Shape(before).SequenceEqual(Shape(text))) ||
        Shape("#if X\nclass A {}\n#endif\n").SequenceEqual(Shape("#if X\nclass B {}\n#endif\n")))
    {
        throw new InvalidOperationException("Executable/directive/disabled-source mutation escaped detection.");
    }
    Console.WriteLine("PASS: comments accepted; import, modifier, default, literal and disabled-source changes rejected.");
    return 0;
}

static async Task<int> CheckApiAsync(string root)
{
    const string baseline = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef";
    string oldListing = await GitTextAsync(root, "ls-tree", "-r", "--name-only", "-z", baseline, "--", "src");
    string currentListing = await GitTextAsync(root, "ls-files", "--cached", "--others", "--exclude-standard", "-z", "--", "src");
    static bool IsSnapshot(string path) => Path.GetFileName(path) is "PublicAPI.Shipped.txt" or "PublicAPI.Unshipped.txt";
    string[] oldPaths = [.. SplitPaths(oldListing).Where(IsSnapshot).Order(StringComparer.Ordinal)];
    string[] currentPaths = [.. SplitPaths(currentListing).Where(IsSnapshot).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    if (oldPaths.Length == 0 || !oldPaths.SequenceEqual(currentPaths))
    {
        throw new InvalidOperationException("Public API snapshot path set changed or baseline has no snapshots.");
    }
    int failures = 0;
    foreach (string path in oldPaths)
    {
        byte[] before = await GitAsync(root, "show", $"{baseline}:{path}");
        byte[] after = File.ReadAllBytes(Path.Combine(root, path));
        bool identical = before.AsSpan().SequenceEqual(after);
        Console.WriteLine($"{(identical ? "PASS" : "FAIL")}: API snapshot {path}; SHA256={Hash(after)}");
        failures += identical ? 0 : 1;
    }
    Console.WriteLine($"API snapshot comparison against {baseline}: files={oldPaths.Length}, failures={failures}.");
    return failures == 0 ? 0 : 1;
}

static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
static string[] SplitPaths(string listing) => listing.Split('\0', StringSplitOptions.RemoveEmptyEntries);
static async Task<string> GitTextAsync(string root, params string[] arguments) => Encoding.UTF8.GetString(await GitAsync(root, arguments));

static async Task<byte[]> GitAsync(string root, params string[] arguments)
{
    var start = new ProcessStartInfo("git")
    {
        WorkingDirectory = root,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    foreach (string argument in arguments)
    {
        start.ArgumentList.Add(argument);
    }
    using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start git.");
    using var output = new MemoryStream();
    Task<string> errorTask = process.StandardError.ReadToEndAsync();
    await process.StandardOutput.BaseStream.CopyToAsync(output);
    await process.WaitForExitAsync();
    string error = await errorTask;
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"git {string.Join(' ', arguments)}: {error}");
    }
    return output.ToArray();
}
