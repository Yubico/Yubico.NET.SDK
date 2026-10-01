using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

internal sealed record BoundarySite(string Id, string Path, string Owner, string Kind, string Target, bool UnsafeAsync);

internal static class BoundaryScanner
{
    private static readonly SymbolDisplayFormat Format = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly HashSet<string> DefinedSymbols = ["NET", "NET10_0", "NET10_0_OR_GREATER", "NETCOREAPP", "NETCOREAPP3_0_OR_GREATER", "DEBUG", "NETFRAMEWORK"];

    internal static string CoreSourceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "toolchain.cs")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found"), "src", "Core", "src");
    }

    internal static IReadOnlyList<BoundarySite> ScanDirectory(string root) => Scan(
        Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"))
            .Select(path => (Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path))));

    internal static IReadOnlyList<BoundarySite> Scan(IEnumerable<(string Path, string Source)> sources)
    {
        var input = sources.ToArray();
        foreach (var source in input)
        {
            var directiveTree = CSharpSyntaxTree.ParseText(source.Source, new CSharpParseOptions(LanguageVersion.Preview), source.Path);
            foreach (var identifier in directiveTree.GetRoot().DescendantTrivia(descendIntoTrivia: true)
                .Select(trivia => trivia.GetStructure()).OfType<DirectiveTriviaSyntax>()
                .Where(directive => directive is IfDirectiveTriviaSyntax or ElifDirectiveTriviaSyntax)
                .SelectMany(directive => directive.DescendantNodes().OfType<IdentifierNameSyntax>()))
            {
                if (!DefinedSymbols.Contains(identifier.Identifier.ValueText))
                    throw new InvalidOperationException($"Unreviewed preprocessor symbol {identifier.Identifier.ValueText} in {source.Path}");
            }
        }
        var trees = input.Select(source => CSharpSyntaxTree.ParseText(source.Source,
            new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: ["NET", "NET10_0", "NET10_0_OR_GREATER", "NETCOREAPP", "NETCOREAPP3_0_OR_GREATER"]), source.Path)).ToArray();
        var usings = CSharpSyntaxTree.ParseText("""
            global using System;
            global using System.Collections.Generic;
            global using System.IO;
            global using System.Linq;
            global using System.Net.Http;
            global using System.Threading;
            global using System.Threading.Tasks;
            """, new CSharpParseOptions(LanguageVersion.Preview), "ImplicitUsings.g.cs");
        var refs = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException("Missing TPA"))
            .Split(Path.PathSeparator).Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
            .Where(path => !Path.GetFileName(path).StartsWith("Yubico.YubiKit.Core", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        var compilation = CSharpCompilation.Create("BoundaryAudit", [.. trees, usings], refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        // RuntimeEnvironment returns <dotnet-root>/shared/Microsoft.NETCore.App/<version>/.
        var dotnetRoot = Directory.GetParent(RuntimeEnvironment.GetRuntimeDirectory())?.Parent?.Parent?.Parent;
        var refPack = Path.Combine(dotnetRoot?.FullName ?? throw new InvalidOperationException("Runtime root missing"),
            "packs", "Microsoft.NETCore.App.Ref");
        var analyzerPath = Path.Combine(Directory.EnumerateDirectories(refPack)
            .Where(path => Path.GetFileName(path).StartsWith("10.0.", StringComparison.Ordinal) &&
                !Path.GetFileName(path).Contains('-', StringComparison.Ordinal))
            .OrderByDescending(path => Version.Parse(Path.GetFileName(path))).First(), "analyzers", "dotnet", "cs", "Microsoft.Interop.LibraryImportGenerator.dll");
        var loader = new NativeAnalyzerLoader();
        var generator = new AnalyzerFileReference(analyzerPath, loader).GetGenerators(LanguageNames.CSharp);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator, parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var generated, out var generatorDiagnostics);
        compilation = (CSharpCompilation)generated;
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)
            .Concat(generatorDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).ToArray();
        // Match the SDK's source-generated native stubs. Reject missing types and unresolved calls.
        if (errors.Length > 0)
        {
            throw new InvalidOperationException("Semantic compilation failed: " + string.Join("; ", errors.Take(8)));
        }

        var found = new List<(string Path, string Owner, string Kind, string Target, int Position, bool UnsafeAsync)>();
        var blockingOwners = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        var nativeOwners = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        var callbackForwarders = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(method) is { } declared && declared.GetAttributes().Any(attribute =>
                    attribute.AttributeClass?.ToDisplayString() is "System.Runtime.InteropServices.LibraryImportAttribute" or
                        "System.Runtime.InteropServices.DllImportAttribute"))
                {
                    nativeOwners.Add(declared.OriginalDefinition);
                }
            }
        }
        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetOperation(node) is IInvocationOperation registration && nativeOwners.Contains(registration.TargetMethod.OriginalDefinition) &&
                    registration.Arguments.Any(argument => argument.Parameter?.Type.TypeKind is TypeKind.Delegate or TypeKind.FunctionPointer &&
                        !argument.Value.ConstantValue.HasValue) && EnclosingMethod(model, node) is IMethodSymbol enclosing)
                {
                    callbackForwarders.Add(enclosing.OriginalDefinition);
                }
                if (model.GetOperation(node) is IInvocationOperation call && IsBlocking(call.TargetMethod) &&
                    model.GetEnclosingSymbol(node.SpanStart) is IMethodSymbol owner)
                {
                    blockingOwners.Add(owner.OriginalDefinition);
                }
            }
            foreach (var node in tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                if (model.GetOperation(node) is IPropertyReferenceOperation property && IsBlockingResult(property.Property) &&
                    model.GetEnclosingSymbol(node.SpanStart) is IMethodSymbol owner)
                {
                    blockingOwners.Add(owner.OriginalDefinition);
                }
            }
        }
        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                if (node is MethodDeclarationSyntax method && model.GetDeclaredSymbol(method) is { } declared)
                {
                    if (declared.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() is
                        "System.Runtime.InteropServices.LibraryImportAttribute" or "System.Runtime.InteropServices.DllImportAttribute"))
                    {
                        found.Add((tree.FilePath, declared.ToDisplayString(Format), "native-import",
                            declared.ToDisplayString(Format), method.SpanStart, false));
                    }
                    if (declared.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() ==
                        "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute") &&
                        tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Any(address =>
                            address.IsKind(SyntaxKind.AddressOfExpression) &&
                            model.GetSymbolInfo(address.Operand).Symbol is IMethodSymbol referenced &&
                            SymbolEqualityComparer.Default.Equals(referenced.OriginalDefinition, declared.OriginalDefinition)))
                    {
                        found.Add((tree.FilePath, declared.ToDisplayString(Format), "callback-address", declared.ToDisplayString(Format), method.SpanStart, false));
                    }
                }

                if (node is InvocationExpressionSyntax invocation && model.GetOperation(invocation) is IInvocationOperation call)
                {
                    var target = call.TargetMethod.OriginalDefinition;
                    var type = target.ContainingType.ToDisplayString();
                    var kind = type == "System.Runtime.InteropServices.NativeLibrary" && target.Name is ("Load" or "GetExport" or "TryGetExport")
                        ? "native-export"
                        : type is ("System.Threading.Tasks.Task" or "System.Threading.Tasks.Task<TResult>" or
                            "System.Threading.Tasks.TaskFactory" or "System.Threading.Tasks.TaskFactory<TResult>") && target.Name is ("Wait" or "Run" or "StartNew")
                            ? target.Name == "Wait" ? "blocking-wait" : "scheduling"
                            : IsBlocking(target) ? "blocking-wait"
                            : type == "System.Threading.Thread" && target.Name == "Start" ? "scheduling" : null;
                    if (kind is not null)
                    {
                        found.Add((tree.FilePath, Owner(model, node), kind, target.ToDisplayString(Format), node.SpanStart,
                            kind == "blocking-wait" && (InAsyncMethod(node) || IsTaskReturningEntry(model, node))));
                    }
                    if (IsTaskReturningEntry(model, node) &&
                        (target.ContainingType.TypeKind == TypeKind.Interface || blockingOwners.Contains(target) || nativeOwners.Contains(target)))
                    {
                        found.Add((tree.FilePath, Owner(model, node), "dispatch-gap", target.ToDisplayString(Format), node.SpanStart,
                            blockingOwners.Contains(target) || nativeOwners.Contains(target)));
                    }
                    if (target.ContainingType.ToDisplayString() == "System.Runtime.InteropServices.Marshal" &&
                        target.Name == "GetFunctionPointerForDelegate")
                    {
                        found.Add((tree.FilePath, Owner(model, node), "callback-conversion", target.ToDisplayString(Format), node.SpanStart, false));
                    }
                    if (nativeOwners.Contains(target) && call.Arguments.Any(argument =>
                        argument.Parameter?.Type.TypeKind is (TypeKind.Delegate or TypeKind.FunctionPointer) &&
                        argument.Value.ConstantValue.HasValue is false))
                    {
                        found.Add((tree.FilePath, Owner(model, node), "callback-registration", target.ToDisplayString(Format), node.SpanStart, false));
                    }
                    if (callbackForwarders.Contains(target) && call.Arguments.Any(argument =>
                        argument.Parameter?.Type.TypeKind is (TypeKind.Delegate or TypeKind.FunctionPointer) &&
                        !argument.Value.ConstantValue.HasValue))
                    {
                        found.Add((tree.FilePath, Owner(model, node), "callback-registration", target.ToDisplayString(Format), node.SpanStart, false));
                    }
                }

                if (node is MemberAccessExpressionSyntax member && model.GetOperation(member) is IPropertyReferenceOperation property &&
                    property.Property.Name == "Result" && property.Property.ContainingType.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.Task<TResult>")
                {
                    found.Add((tree.FilePath, Owner(model, node), "blocking-wait", property.Property.OriginalDefinition.ToDisplayString(Format), node.SpanStart,
                        InAsyncMethod(node) || IsTaskReturningEntry(model, node)));
                }

                if (node is MemberAccessExpressionSyntax option && model.GetOperation(option) is IFieldReferenceOperation field &&
                    field.Field.ContainingType.ToDisplayString() == "System.Threading.Tasks.TaskCreationOptions" && field.Field.Name == "LongRunning")
                {
                    found.Add((tree.FilePath, Owner(model, node), "scheduling", field.Field.ToDisplayString(Format), node.SpanStart, false));
                }

                if (node is InvocationExpressionSyntax registration && model.GetOperation(registration) is IInvocationOperation callback &&
                    callback.TargetMethod.Name.StartsWith("IOHID", StringComparison.Ordinal) &&
                    callback.TargetMethod.Name.Contains("Register", StringComparison.Ordinal) &&
                    callback.TargetMethod.Name.EndsWith("Callback", StringComparison.Ordinal) &&
                    callback.TargetMethod.ContainingNamespace.ToDisplayString().StartsWith("Yubico.YubiKit.Core.Native.MacOS", StringComparison.Ordinal))
                {
                    found.Add((tree.FilePath, Owner(model, node), "callback-registration", callback.TargetMethod.ToDisplayString(Format), node.SpanStart, false));
                }
            }
        }

        return found.GroupBy(s => (s.Path, s.Owner, s.Kind, s.Target))
            .SelectMany(group => group.OrderBy(s => s.Position).Select((site, index) =>
                new BoundarySite($"{site.Path}|{site.Owner}|{site.Kind}|{site.Target}|{index + 1}",
                    site.Path, site.Owner, site.Kind, site.Target, site.UnsafeAsync)))
            .OrderBy(s => s.Id, StringComparer.Ordinal).ToArray();
    }

    private static string Owner(SemanticModel model, SyntaxNode node) =>
        (EnclosingMethod(model, node) ?? throw new InvalidOperationException("Missing owner"))
        .ToDisplayString(Format);

    private static ISymbol? EnclosingMethod(SemanticModel model, SyntaxNode node)
    {
        var symbol = model.GetEnclosingSymbol(node.SpanStart);
        while (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction })
        {
            symbol = symbol.ContainingSymbol;
        }
        return symbol;
    }

    private static bool InAsyncMethod(SyntaxNode node) => node.Ancestors()
        .OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Modifiers.Any(SyntaxKind.AsyncKeyword) == true;

    private static bool IsTaskReturningEntry(SemanticModel model, SyntaxNode node)
    {
        var method = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method is null || method.Modifiers.Any(SyntaxKind.AsyncKeyword) ||
            model.GetDeclaredSymbol(method)?.ReturnType is not INamedTypeSymbol returnType ||
            returnType.ContainingNamespace.ToDisplayString() != "System.Threading.Tasks" ||
            returnType.OriginalDefinition.MetadataName is not ("Task" or "Task`1" or "ValueTask" or "ValueTask`1"))
        {
            return false;
        }

        return !node.Ancestors().TakeWhile(ancestor => ancestor != method).Any(ancestor => ancestor is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax);
    }

    private static bool IsBlocking(IMethodSymbol method) =>
        (method.Name == "Wait" && method.ContainingType.ToDisplayString() is "System.Threading.Tasks.Task" or "System.Threading.Tasks.Task<TResult>" or
            "System.Threading.ManualResetEventSlim" or "System.Threading.SemaphoreSlim") ||
        (method.Name == "WaitOne" && method.ContainingType.ToDisplayString() == "System.Threading.WaitHandle") ||
        (method.Name is ("WaitAll" or "WaitAny") && method.ContainingType.ToDisplayString() == "System.Threading.WaitHandle") ||
        (method.Name == "Wait" && method.ContainingType.ToDisplayString() == "System.Threading.Monitor") ||
        (method.Name == "Sleep" && method.ContainingType.ToDisplayString() == "System.Threading.Thread") ||
        (method.Name == "Join" && method.ContainingType.ToDisplayString() == "System.Threading.Thread") ||
        (method.Name == "GetResult" && method.ContainingType.ContainingNamespace.ToDisplayString() == "System.Runtime.CompilerServices" &&
            (method.ContainingType.OriginalDefinition.MetadataName is ("TaskAwaiter" or "TaskAwaiter`1" or "ValueTaskAwaiter" or "ValueTaskAwaiter`1") ||
             method.ContainingType.Name is ("ConfiguredTaskAwaiter" or "ConfiguredValueTaskAwaiter") && method.ContainingType.ContainingType?.OriginalDefinition.MetadataName is
                 ("ConfiguredTaskAwaitable" or "ConfiguredTaskAwaitable`1" or "ConfiguredValueTaskAwaitable" or "ConfiguredValueTaskAwaitable`1")));

    private static bool IsBlockingResult(IPropertySymbol property) => property.Name == "Result" &&
        property.ContainingType.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.Task<TResult>";

    private sealed class NativeAnalyzerLoader : IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath) { }

        public System.Reflection.Assembly LoadFromPath(string fullPath) => System.Reflection.Assembly.LoadFrom(fullPath);
    }
}
