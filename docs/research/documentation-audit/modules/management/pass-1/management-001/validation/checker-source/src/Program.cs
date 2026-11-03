using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

return await RunAsync(args);

static async Task<int> RunAsync(string[] arguments)
{
    try
    {
        return arguments switch
        {
            ["tokens", var root] => await CheckTokensAsync(Path.GetFullPath(root)),
            ["api", var root] => await CheckApiAsync(Path.GetFullPath(root)),
            ["compile", var source, var root, .. var modules] => Compile(source, Path.GetFullPath(root), modules),
            _ => Usage()
        };
    }
    catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
    {
        Console.Error.WriteLine($"FAIL: {exception.Message}");
        return 2;
    }
}

static int Usage()
{
    Console.Error.WriteLine("Usage: DocCheck tokens <repo> | api <repo> | compile <source.cs> <repo> [SDK module ...] [--reference <assembly.dll> ...] [--identity-comparer default|desktop]");
    return 2;
}

static async Task<int> CheckTokensAsync(string root)
{
    const string baseline = "da61d2e6";
    string changed = await GitTextAsync(root, "diff", "--name-only", "--no-renames", "-z", baseline, "--", "*.cs");
    string untracked = await GitTextAsync(root, "ls-files", "--others", "--exclude-standard", "-z", "--", "*.cs");
    string[] paths = [.. SplitPaths(changed).Concat(SplitPaths(untracked)).Where(IsExecutableSource).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    if (paths.Length == 0)
    {
        throw new InvalidOperationException("No changed shipping/test/example C# files found; refusing a vacuous pass.");
    }

    int failures = 0;
    foreach (string path in paths)
    {
        // A missing baseline or deleted file deliberately fails closed (including untracked source).
        string before = await GitTextAsync(root, "show", $"{baseline}:{path}");
        string after = File.ReadAllText(Path.Combine(root, path));
        var oldShape = Shape(before);
        var newShape = Shape(after);
        bool identical = oldShape.Tokens.SequenceEqual(newShape.Tokens) && oldShape.Trivia.SequenceEqual(newShape.Trivia);
        Console.WriteLine($"{(identical ? "PASS" : "FAIL")}: tokens/directives {path}; SHA256={Hash(File.ReadAllBytes(Path.Combine(root, path)))}");
        failures += identical ? 0 : 1;
    }
    Console.WriteLine($"Token comparison against {baseline}: files={paths.Length}, failures={failures}; only directive-free using-list order normalized.");
    return failures == 0 ? 0 : 1;
}

static bool IsExecutableSource(string path)
{
    string[] roots = ["src/", "tests/", "examples/", "benchmarks/", "verification/"];
    string[] excluded = ["bin", "obj", "codebase_ast", "generated"];
    return roots.Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal)) &&
        !path.Split('/').Any(part => excluded.Contains(part, StringComparer.OrdinalIgnoreCase)) &&
        !path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) &&
        !path.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase);
}

static (string[] Tokens, string[] Trivia) Shape(string text)
{
    var parsed = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.CSharp14)).GetRoot();
    // No semantic evaluation: preserve active tokens, directives, disabled branches and skipped text.
    SyntaxNode normalized = new UsingOrderNormalizer().Visit(parsed) ?? throw new InvalidOperationException("No syntax root.");
    var tokens = new List<string>();
    var trivia = new List<string>();
    foreach (SyntaxToken token in normalized.DescendantTokens())
    {
        AddSignificantTrivia(token.LeadingTrivia, tokens.Count, "leading", trivia);
        tokens.Add($"{token.RawKind}:{JsonSerializer.Serialize(token.Text)}");
        AddSignificantTrivia(token.TrailingTrivia, tokens.Count - 1, "trailing", trivia);
    }
    return ([.. tokens], [.. trivia]);
}

static void AddSignificantTrivia(SyntaxTriviaList items, int tokenIndex, string side, List<string> result)
{
    foreach (SyntaxTrivia item in items.Where(IsSignificantTrivia))
    {
        // Index anchors detect moving a directive across executable tokens, not just changing its text.
        result.Add($"{tokenIndex}:{side}:{item.RawKind}:{JsonSerializer.Serialize(item.ToFullString())}");
    }
}

static bool IsSignificantTrivia(SyntaxTrivia item) => item.IsDirective ||
    item.IsKind(SyntaxKind.DisabledTextTrivia) || item.IsKind(SyntaxKind.SkippedTokensTrivia);

static async Task<int> CheckApiAsync(string root)
{
    const string baseline = "da61d2e6";
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

static int Compile(string source, string root, string[] referenceArguments)
{
    string sourcePath = Path.GetFullPath(source);
    var additionalModules = new List<string>();
    var explicitReferences = new List<string>();
    string comparerName = "default";
    for (int index = 0; index < referenceArguments.Length; index++)
    {
        string argument = referenceArguments[index];
        if (argument == "--reference")
        {
            if (++index >= referenceArguments.Length)
            {
                throw new ArgumentException("--reference requires an assembly path.");
            }
            string path = Path.GetFullPath(referenceArguments[index], root);
            if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Explicit metadata references must be DLL files.");
            }
            explicitReferences.Add(path);
        }
        else if (argument == "--identity-comparer")
        {
            if (++index >= referenceArguments.Length || referenceArguments[index] is not ("default" or "desktop"))
            {
                throw new ArgumentException("--identity-comparer requires default or desktop.");
            }
            comparerName = referenceArguments[index];
        }
        else
        {
            additionalModules.Add(argument);
        }
    }
    string[] modules = [.. new[] { "Core", "Management" }.Concat(additionalModules).Distinct(StringComparer.Ordinal)];
    if (modules.Any(module => module.Length == 0 || module.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '.')))
    {
        throw new ArgumentException("Additional references must be SDK module names, not paths.");
    }
    string[] sdkPaths = [.. modules.Select(module => Path.Combine(root, "src", module, "src", "bin", "Release", "net10.0", $"Yubico.YubiKit.{module}.dll"))];
    string[] suppliedPaths = [.. sdkPaths.Concat(explicitReferences).Distinct(StringComparer.Ordinal)];
    foreach (string path in suppliedPaths)
    {
        Console.WriteLine($"REFERENCE: {path}; SHA256={Hash(File.ReadAllBytes(path))}");
    }
    string[] runtime = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException("No runtime reference list."))
        .Split(Path.PathSeparator);
    // Exclude the checker and its Roslyn dependencies from snippet binding, even if TPA lists them.
    var suppliedNames = suppliedPaths.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var references = runtime.Where(path => !Path.GetFileName(path).StartsWith("Yubico.", StringComparison.Ordinal) &&
            !Path.GetFileName(path).StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) &&
            !suppliedNames.Contains(Path.GetFileName(path)))
        .Concat(suppliedPaths).Distinct(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path));
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(sourcePath), new CSharpParseOptions(LanguageVersion.CSharp14), sourcePath);
    OutputKind kind = tree.GetRoot().DescendantNodes().Any(node => node is GlobalStatementSyntax)
        ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary;
    // The real project's AnalysisLevel=preview evaluates to WarningLevel=9999 on this SDK.
    // Match that warning-wave coverage instead of silently retaining Roslyn's default level 4.
    var options = new CSharpCompilationOptions(kind, nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: false, warningLevel: 9999);
    if (comparerName == "desktop")
    {
        // Match csc's comparer only; do not copy SDK NoWarn defaults or hide identity diagnostics.
        options = options.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);
    }
    Console.WriteLine($"POLICY: identity-comparer={options.AssemblyIdentityComparer.GetType().FullName}; warning-level={options.WarningLevel}; specific-diagnostic-overrides={options.SpecificDiagnosticOptions.Count}; warnings/errors remain failures.");
    var compilation = CSharpCompilation.Create("ManagementDocumentationExamples", [tree], references, options);
    // Request full binding diagnostics as well as Emit diagnostics; neither phase may hide a warning.
    Diagnostic[] bindingProblems = [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error)];
    using var output = new MemoryStream();
    var result = compilation.Emit(output);
    Diagnostic[] problems = [.. bindingProblems.Concat(result.Diagnostics)
        .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error)
        .DistinctBy(diagnostic => diagnostic.ToString())];
    Console.WriteLine($"DIAGNOSTIC PHASES: binding-warnings/errors={bindingProblems.Length}; emit-warnings/errors={result.Diagnostics.Count(diagnostic => diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error)}; unique-warnings/errors={problems.Length}.");
    foreach (Diagnostic diagnostic in problems)
    {
        Console.WriteLine(diagnostic);
    }
    bool success = result.Success && problems.Length == 0;
    Console.WriteLine($"{(success ? "PASS" : "FAIL")}: compile-only {sourcePath}; SHA256={Hash(File.ReadAllBytes(sourcePath))}; warnings/errors={problems.Length}; no assembly loaded or snippet executed.");
    return success ? 0 : 1;
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

sealed class UsingOrderNormalizer : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node) =>
        base.VisitCompilationUnit(node.WithUsings(Sort(node.Usings)));

    public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) =>
        base.VisitNamespaceDeclaration(node.WithUsings(Sort(node.Usings)));

    public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node) =>
        base.VisitFileScopedNamespaceDeclaration(node.WithUsings(Sort(node.Usings)));

    private static SyntaxList<UsingDirectiveSyntax> Sort(SyntaxList<UsingDirectiveSyntax> usings)
    {
        // Do not move conditional, disabled or malformed source across a using boundary.
        if (usings.Any(item => item.DescendantTrivia().Any(trivia => trivia.IsDirective ||
                trivia.IsKind(SyntaxKind.DisabledTextTrivia) || trivia.IsKind(SyntaxKind.SkippedTokensTrivia))))
        {
            return usings;
        }
        return SyntaxFactory.List(usings.OrderBy(item => string.Join("|", item.DescendantTokens()
            .Select(token => $"{token.RawKind}:{JsonSerializer.Serialize(token.Text)}")), StringComparer.Ordinal));
    }
}
