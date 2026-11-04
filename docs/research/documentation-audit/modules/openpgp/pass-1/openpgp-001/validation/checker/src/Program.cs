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
            ["tokens-import-removals", var root, var manifest] => await CheckImportRemovalsAsync(Path.GetFullPath(root), manifest),
            ["api", var root] => await CheckApiAsync(Path.GetFullPath(root)),
            ["compile", var source, var root, .. var modules] => Compile(source, Path.GetFullPath(root), modules),
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
    Console.Error.WriteLine("Usage: DocCheck tokens <repo> | tokens-import-removals <repo> <run-local import-removals.json> | api <repo> | compile <source.cs> <repo> [SDK module ...] [--reference <assembly.dll> ...] [--identity-comparer default|desktop]");
    return 2;
}

static async Task<int> CheckTokensAsync(string root)
{
    const string baseline = "fe180282a2d77b15eaaa5868d6c8b00e251d1f05";
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

static async Task<int> CheckImportRemovalsAsync(string root, string manifestArgument)
{
    const string baseline = "fe180282a2d77b15eaaa5868d6c8b00e251d1f05";
    string manifestPath = Path.GetFullPath(manifestArgument, root);
    byte[] manifestBytes = ReadRunEvidence(root, Path.GetRelativePath(root, manifestPath));
    if (Path.GetFileName(manifestPath) != "import-removals.json")
    {
        throw new InvalidOperationException("Require a run-local import-removals.json, not an arbitrary exemption file.");
    }
    using var document = JsonDocument.Parse(manifestBytes);
    JsonElement manifest = document.RootElement;
    RequireProperties(manifest, "schema_version", "baseline", "formatter", "files");
    if (manifest.GetProperty("schema_version").GetInt32() != 1 || RequiredText(manifest, "baseline") != baseline)
    {
        throw new InvalidOperationException("Import-removal schema/baseline does not match this run.");
    }
    string changed = await GitTextAsync(root, "diff", "--name-only", "--no-renames", "-z", baseline, "--", "*.cs");
    string untracked = await GitTextAsync(root, "ls-files", "--others", "--exclude-standard", "-z", "--", "*.cs");
    string[] paths = [.. SplitPaths(changed).Concat(SplitPaths(untracked)).Where(IsExecutableSource).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    var approvals = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
    foreach (JsonElement approval in manifest.GetProperty("files").EnumerateArray())
    {
        RequireProperties(approval, "path", "baseline_sha256", "before", "after", "removals");
        string path = RequiredText(approval, "path");
        if (!IsExecutableSource(path) || Path.GetRelativePath(root, Path.GetFullPath(path, root)) != path ||
            path.Contains('\\') || !paths.Contains(path, StringComparer.Ordinal) || !approvals.TryAdd(path, approval))
        {
            throw new InvalidOperationException($"Invalid, duplicate or unchanged import-exception path: {path}");
        }
    }
    if (paths.Length == 0 || approvals.Count == 0)
    {
        throw new InvalidOperationException("Require nonempty changed source and import-removal approvals; otherwise use strict tokens.");
    }
    Console.WriteLine($"IMPORT EXCEPTION MANIFEST: {manifestPath}; SHA256={Hash(manifestBytes)}; baseline={baseline}");
    int failures = 0;
    int removals = 0;
    foreach (string path in paths)
    {
        byte[] baselineBytes = await GitAsync(root, "show", $"{baseline}:{path}");
        string before = Encoding.UTF8.GetString(baselineBytes);
        string afterPath = Path.Combine(root, path);
        string after = File.ReadAllText(afterPath);
        if (approvals.TryGetValue(path, out JsonElement approval))
        {
            var validated = ValidateImportRemoval(root, path, approval, manifest.GetProperty("formatter"), baselineBytes, before, after);
            before = validated.NormalizedBaseline;
            removals += validated.Count;
        }
        var oldShape = Shape(before);
        var newShape = Shape(after);
        bool identical = oldShape.Tokens.SequenceEqual(newShape.Tokens) && oldShape.Trivia.SequenceEqual(newShape.Trivia);
        Console.WriteLine($"{(identical ? "PASS" : "FAIL")}: {(approvals.ContainsKey(path) ? "listed-import-normalized" : "strict")} tokens/directives {path}; SHA256={Hash(File.ReadAllBytes(afterPath))}");
        failures += identical ? 0 : 1;
    }
    Console.WriteLine($"Import-accounted comparison against {baseline}: files={paths.Length}, exception-files={approvals.Count}, explicitly-listed-removals={removals}, failures={failures}; syntax is NOT identical: only listed ordinary namespace imports plus the existing directive-free using order allowance were normalized.");
    return failures == 0 ? 0 : 1;
}

static (string NormalizedBaseline, int Count) ValidateImportRemoval(string root, string path, JsonElement approval, JsonElement provenance,
    byte[] baselineBytes, string baselineText, string currentText)
{
    RequireHash(baselineBytes, RequiredText(approval, "baseline_sha256"), "baseline " + path);
    string capturedBefore = ReadSourceCapture(root, path, approval.GetProperty("before"), true);
    string capturedAfter = ReadSourceCapture(root, path, approval.GetProperty("after"), false);
    byte[] currentBytes = ReadRunOrSourceBytes(root, path);
    RequireHash(currentBytes, RequiredText(approval.GetProperty("after"), "sha256"), "current " + path);
    if (capturedAfter != currentText)
    {
        throw new InvalidOperationException($"Current source does not match approved post-format capture: {path}");
    }
    // The pre-format source itself must pass the ORIGINAL strict baseline comparison.
    var originalShape = Shape(baselineText);
    var capturedShape = Shape(capturedBefore);
    if (!originalShape.Tokens.SequenceEqual(capturedShape.Tokens) || !originalShape.Trivia.SequenceEqual(capturedShape.Trivia))
    {
        throw new InvalidOperationException($"Pre-format capture already changes baseline tokens/directives: {path}");
    }
    RequireProperties(provenance, "diagnostic", "command_log", "command_log_sha256", "log", "log_sha256");
    if (RequiredText(provenance, "diagnostic") != "IDE0005")
    {
        throw new InvalidOperationException("Only actual compiler/formatter unused-import provenance (IDE0005) is permitted.");
    }
    byte[] commandLog = ReadRunEvidence(root, RequiredText(provenance, "command_log"));
    RequireHash(commandLog, RequiredText(provenance, "command_log_sha256"), "formatter command log");
    string command = Encoding.UTF8.GetString(commandLog).Split('\n')[0];
    if (!command.StartsWith("$ dotnet format ", StringComparison.Ordinal) || !command.Contains("--include", StringComparison.Ordinal) ||
        !command.Contains(path, StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Retain the actual scoped dotnet format command, not an arbitrary removal justification.");
    }
    byte[] log = ReadRunEvidence(root, RequiredText(provenance, "log"));
    RequireHash(log, RequiredText(provenance, "log_sha256"), "formatter provenance");
    if (!Encoding.UTF8.GetString(log).Contains("IDE0005", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Retained diagnostic log contains no IDE0005 evidence.");
    }
    var original = ParseRemovalSource(baselineText);
    var captured = ParseRemovalSource(capturedBefore);
    var current = ParseRemovalSource(currentText);
    var names = new HashSet<string>(StringComparer.Ordinal);
    var originalNodes = new List<UsingDirectiveSyntax>();
    var capturedNodes = new List<UsingDirectiveSyntax>();
    foreach (JsonElement removal in approval.GetProperty("removals").EnumerateArray())
    {
        RequireProperties(removal, "namespace");
        string name = RequiredText(removal, "namespace");
        if (!names.Add(name) || current.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(node => ImportName(node) == name))
        {
            throw new InvalidOperationException($"Duplicate removal or import still present: {path}: {name}");
        }
        originalNodes.Add(RequireOrdinaryImport(original, name));
        capturedNodes.Add(RequireOrdinaryImport(captured, name));
        Console.WriteLine($"IMPORT REMOVAL: {path}: using {name}; IDE0005 provenance={RequiredText(provenance, "log")}; command-log={RequiredText(provenance, "command_log")}");
    }
    if (names.Count == 0)
    {
        throw new InvalidOperationException("An exception file must list actual import removals.");
    }
    using var transform = JsonDocument.Parse(ReadRunEvidence(root, RequiredText(approval.GetProperty("after"), "path")));
    JsonElement transformedFile = RequireCaptureEntry(transform.RootElement, path);
    string[] recordedNames = [.. transformedFile.GetProperty("removed_namespaces").EnumerateArray().Select(item => item.GetString() ?? "")];
    if (!names.Order(StringComparer.Ordinal).SequenceEqual(recordedNames.Order(StringComparer.Ordinal)) ||
        RequiredText(transformedFile, "before_sha256") != RequiredText(approval.GetProperty("before"), "sha256"))
    {
        throw new InvalidOperationException("Approved removals/before hash disagree with the actual formatter transform.");
    }
    var normalizedOriginal = original.WithUsings(SyntaxFactory.List(original.Usings.Where(node => !originalNodes.Contains(node))));
    var normalizedCaptured = captured.WithUsings(SyntaxFactory.List(captured.Usings.Where(node => !capturedNodes.Contains(node))));
    var capturedAfterShape = Shape(normalizedCaptured.ToFullString());
    var actualAfterShape = Shape(currentText);
    if (!capturedAfterShape.Tokens.SequenceEqual(actualAfterShape.Tokens) || !capturedAfterShape.Trivia.SequenceEqual(actualAfterShape.Trivia))
    {
        throw new InvalidOperationException($"Formatter transform changes unlisted imports or other tokens/directives: {path}");
    }
    return (normalizedOriginal.ToFullString(), names.Count);
}

static CompilationUnitSyntax ParseRemovalSource(string text)
{
    var syntax = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.CSharp14));
    var root = (CompilationUnitSyntax)syntax.GetRoot();
    // Conservative scope: no removal exception in ANY directive/disabled/malformed-source file.
    if (syntax.GetDiagnostics().Any(item => item.Severity == DiagnosticSeverity.Error) ||
        root.DescendantTrivia().Any(IsSignificantTrivia))
    {
        throw new InvalidOperationException("Import exceptions require valid, directive-free source; use strict mode for other files.");
    }
    return root;
}

static string ImportName(UsingDirectiveSyntax node) => node.Name is null ? "" : string.Concat(node.Name.DescendantTokens().Select(token => token.Text));

static UsingDirectiveSyntax RequireOrdinaryImport(CompilationUnitSyntax root, string name)
{
    UsingDirectiveSyntax[] matches = [.. root.DescendantNodes().OfType<UsingDirectiveSyntax>().Where(node => ImportName(node) == name)];
    if (matches.Length != 1 || !root.Usings.Contains(matches[0]))
    {
        throw new InvalidOperationException($"Require exactly one compilation-unit import in baseline/before source: {name}");
    }
    UsingDirectiveSyntax node = matches[0];
    if (node.Alias is not null || node.StaticKeyword.RawKind != 0 || node.GlobalKeyword.RawKind != 0 ||
        node.UnsafeKeyword.RawKind != 0 || node.Name is null ||
        node.Name.DescendantTokens().Any(token => !token.IsKind(SyntaxKind.IdentifierToken) && !token.IsKind(SyntaxKind.DotToken)) ||
        node.DescendantTrivia().Any(IsSignificantTrivia))
    {
        throw new InvalidOperationException($"Only ordinary namespace imports may be removed (not alias/static/global/unsafe/conditional): {name}");
    }
    return node;
}

static string ReadSourceCapture(string root, string sourcePath, JsonElement capture, bool before)
{
    RequireProperties(capture, "path", "bundle_sha256", "sha256");
    string path = RequiredText(capture, "path");
    byte[] bytes = ReadRunEvidence(root, path);
    RequireHash(bytes, RequiredText(capture, "bundle_sha256"), path);
    using var bundle = JsonDocument.Parse(bytes);
    JsonElement entry = RequireCaptureEntry(bundle.RootElement, sourcePath);
    string expected = RequiredText(capture, "sha256");
    if (before)
    {
        if (RequiredText(bundle.RootElement, "baseline") != "fe180282a2d77b15eaaa5868d6c8b00e251d1f05" || RequiredText(entry, "sha256") != expected)
        {
            throw new InvalidOperationException("Pre-format capture baseline/source hash changed.");
        }
        string text = entry.GetProperty("content").GetString() ?? throw new InvalidOperationException("Missing full pre-format source capture.");
        RequireHash(Encoding.UTF8.GetBytes(text), expected, "captured before " + sourcePath);
        return text;
    }
    if (bundle.RootElement.GetProperty("formatter_exit").GetInt32() != 0 || RequiredText(entry, "after_sha256") != expected ||
        entry.GetProperty("added_namespaces").GetArrayLength() != 0 || !entry.GetProperty("non_using_non_whitespace_identical").GetBoolean())
    {
        throw new InvalidOperationException("Formatter transform records additions, collateral changes or a failed formatter.");
    }
    RequireHash(ReadRunOrSourceBytes(root, sourcePath), expected, "approved after " + sourcePath);
    return File.ReadAllText(Path.GetFullPath(sourcePath, root));
}

static JsonElement RequireCaptureEntry(JsonElement bundle, string sourcePath)
{
    JsonElement[] entries = [.. bundle.GetProperty("files").EnumerateArray().Where(item => RequiredText(item, "path") == sourcePath)];
    return entries.Length == 1 ? entries[0] : throw new InvalidOperationException("Require exactly one source entry in the capture bundle.");
}

static byte[] ReadRunEvidence(string root, string relativePath)
{
    const string run = "docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/";
    if (!relativePath.StartsWith(run, StringComparison.Ordinal) ||
        Path.GetRelativePath(root, Path.GetFullPath(relativePath, root)) != relativePath || relativePath.Contains('\\'))
    {
        throw new InvalidOperationException("Manifest, captures and diagnostic evidence must be canonical paths in this run.");
    }
    return ReadRunOrSourceBytes(root, relativePath);
}

static byte[] ReadRunOrSourceBytes(string root, string relativePath)
{
    string fullPath = Path.GetFullPath(relativePath, root);
    string prefix = root;
    foreach (string part in relativePath.Split('/'))
    {
        prefix = Path.Combine(prefix, part);
        if ((File.GetAttributes(prefix) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException("Symlinked evidence/source paths are not allowed.");
        }
    }
    return File.ReadAllBytes(fullPath);
}

static void RequireHash(byte[] bytes, string expected, string role)
{
    if (expected.Length != 64 || expected.Any(character => !"0123456789abcdefABCDEF".Contains(character)) ||
        !string.Equals(Hash(bytes), expected, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"Approved SHA-256 mismatch: {role}");
    }
}

static string RequiredText(JsonElement value, string name) => value.GetProperty(name).GetString() is { Length: > 0 } text
    ? text : throw new InvalidOperationException($"Require nonempty text: {name}");

static void RequireProperties(JsonElement value, params string[] names)
{
    string[] actual = [.. value.EnumerateObject().Select(item => item.Name)];
    if (actual.Length != names.Length || !actual.Order(StringComparer.Ordinal).SequenceEqual(names.Order(StringComparer.Ordinal)))
    {
        throw new InvalidOperationException("Manifest object has missing, duplicate or unknown fields.");
    }
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
    const string baseline = "fe180282a2d77b15eaaa5868d6c8b00e251d1f05";
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
    string[] modules = [.. new[] { "Core", "Ctap2" }.Concat(additionalModules).Distinct(StringComparer.Ordinal)];
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
    // Match the evaluated preview warning-wave coverage; never copy SDK NoWarn into raw Roslyn.
    var options = new CSharpCompilationOptions(kind, nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: false, warningLevel: 9999);
    if (comparerName == "desktop")
    {
        options = options.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);
    }
    Console.WriteLine($"POLICY: identity-comparer={options.AssemblyIdentityComparer.GetType().FullName}; warning-level={options.WarningLevel}; specific-diagnostic-overrides={options.SpecificDiagnosticOptions.Count}; warnings/errors remain failures.");
    var compilation = CSharpCompilation.Create("Ctap2DocumentationExamples", [tree], references, options);
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
        if (usings.Any(item => item.DescendantTrivia().Any(trivia => trivia.IsDirective ||
                trivia.IsKind(SyntaxKind.DisabledTextTrivia) || trivia.IsKind(SyntaxKind.SkippedTokensTrivia))))
        {
            return usings;
        }
        return SyntaxFactory.List(usings.OrderBy(item => string.Join("|", item.DescendantTokens()
            .Select(token => $"{token.RawKind}:{JsonSerializer.Serialize(token.Text)}")), StringComparer.Ordinal));
    }
}
