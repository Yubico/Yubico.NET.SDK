# Project-context evidence collector correction

The first real SDK-context host build (`20261103-200410-051`) passed with zero compiler/analyzer warnings.
Its evidence collector then rejected `EnableNETAnalyzers=false`. That was an incorrect collector
assumption, not a compiler failure or analyzer disablement introduced by this run.

Astra inspected actual Csc inputs: the canonical NuGet `Microsoft.CodeAnalysis.NetAnalyzers` 9.0.0
analyzer DLLs and xUnit analyzers were passed; `RunAnalyzersDuringBuild=true`, warnings-as-errors true.
The installed package's `buildTransitive/DisableNETAnalyzersForNuGetPackage.props` explicitly sets the
SDK-bundled analyzer flag false because the package supplies the identical CA rules. No SDK/package
file or analyzer setting was edited.

Independent evaluation of the actual Management integration project:

```text
dotnet msbuild src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj -getProperty:EnableNETAnalyzers,RunAnalyzersDuringBuild,TreatWarningsAsErrors,AnalysisMode -nologo
EnableNETAnalyzers=false
RunAnalyzersDuringBuild=true
TreatWarningsAsErrors=true
AnalysisMode=Recommended
```

Corrected only the collector: still require warnings-as-errors, actual analyzer execution, actual
xUnit/NET analyzer inputs and all source/reference hashes. If the SDK flag is false, specifically
require the canonical package analyzer replacement. Compiler/reference/warning policy remains unchanged.
The collector source/manifest and actual first-build Csc inputs were preserved before correction under
`validation/context-collection-failure/`; its failed output is `validation/final-context-collection.log`.
The prepared context will be regenerated and freshly built so final evidence also matches the corrected
collector hash. This does not turn the preserved raw CS1701 diagnostics into a warning-free raw result.
