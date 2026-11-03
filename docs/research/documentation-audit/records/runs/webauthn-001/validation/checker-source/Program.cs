using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args[0] == "tokens")
{
    string root = args[1];
    string baseline = args[2];
    string[] paths = Git(root, "diff", "--name-only", baseline, "--", "*.cs")
        .Split('\n', StringSplitOptions.RemoveEmptyEntries);
    foreach (string path in paths)
    {
        var before = Parse(Git(root, "show", $"{baseline}:{path}"));
        var after = Parse(File.ReadAllText(Path.Combine(root, path)));
        if (!Tokens(before).SequenceEqual(Tokens(after)) || !Directives(before).SequenceEqual(Directives(after)))
        {
            throw new InvalidOperationException($"Executable tokens or directives changed: {path}");
        }
    }
    Console.WriteLine($"PASS: {paths.Length} edited C# files have identical executable tokens and directives to {baseline}, allowing only top-level using-directive ordering.");
}
else if (args[0] == "compile")
{
    string source = args[1];
    string root = args[2];
    var runtime = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException())
        .Split(Path.PathSeparator);
    string[] sdk = ["Core", "Ctap2", "WebAuthn"];
    var references = runtime.Concat(sdk.Select(module => Path.Combine(root, "src", module, "src", "bin", "Release", "net10.0", $"Yubico.YubiKit.{module}.dll")))
        .Distinct().Select(path => MetadataReference.CreateFromFile(path));
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(source), new CSharpParseOptions(LanguageVersion.Preview), source);
    var kind = tree.GetRoot().DescendantNodes().Any(node => node is GlobalStatementSyntax)
        ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary;
    var compilation = CSharpCompilation.Create("WebAuthnDocumentationExamples", [tree], references,
        new CSharpCompilationOptions(kind, nullableContextOptions: NullableContextOptions.Enable));
    using var output = new MemoryStream();
    var result = compilation.Emit(output);
    foreach (var diagnostic in result.Diagnostics.Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error))
    {
        Console.WriteLine(diagnostic);
    }
    if (!result.Success || result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning))
    {
        Environment.ExitCode = 1;
    }
    else
    {
        Console.WriteLine($"PASS: {source} compiles against the current SDK assemblies with no compiler warnings. Compilation only; no device or RP calls executed.");
    }
}
else
{
    throw new ArgumentException("Expected tokens or compile mode.");
}

static SyntaxNode Parse(string text) => CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Preview)).GetRoot();
static IEnumerable<(int Kind, string Text)> Tokens(SyntaxNode node)
{
    if (node is CompilationUnitSyntax unit)
    {
        node = unit.WithUsings(SyntaxFactory.List(unit.Usings.OrderBy(directive => directive.ToString(), StringComparer.Ordinal)));
    }
    return node.DescendantTokens().Select(token => (token.RawKind, token.Text));
}
static IEnumerable<string> Directives(SyntaxNode node) => node.DescendantTrivia().Where(trivia => trivia.GetStructure() is DirectiveTriviaSyntax).Select(trivia => trivia.ToFullString().Trim());

static string Git(string root, params string[] arguments)
{
    var start = new ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (string argument in arguments)
    {
        start.ArgumentList.Add(argument);
    }
    using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start git.");
    string result = process.StandardOutput.ReadToEnd();
    string error = process.StandardError.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(error);
    }
    return result;
}
