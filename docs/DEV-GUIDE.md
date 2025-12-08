# Developer Guide

## Analyzer and Formatting Workflow

- Roslyn analyzers are enabled via `Directory.Build.props` for every project. The solution defaults to `AnalysisLevel=latest` with `AnalysisMode=AllEnabledByDefault` so new rules surface automatically.
- Test projects run with `AnalysisMode=Recommended`. If a test project needs a stricter or more relaxed profile, add scoped overrides in `.editorconfig` or adjust the conditional property group in `Directory.Build.props`.
- The analyzer package set lives in `Directory.Build.targets`. Add new analyzer dependencies there so every project picks them up consistently.
- Run analyzers locally with either `dotnet build Yubico.YubiKit.sln` or `dotnet format analyzers`. Both commands respect the analyzer configuration and report diagnostics.

## .editorconfig Ownership

- `.editorconfig` is the single source of truth for formatting and style. Update it when you need to change indentation, brace layout, or rule severities.
- Use additional `.editorconfig` files or folder-scoped sections when tests or legacy code paths require a different configuration. Roslyn will pick the closest match automatically.
- Do not rely on IDE-specific settings to express shared conventions. If Rider or Visual Studio disagrees, the build wins.

## IDE Configuration Notes

- Rider: enable **Editor → Code Style → General → Read settings from .editorconfig**. Use the shared `.DotSettings` file only for features that `.editorconfig` cannot express (e.g., custom cleanup profiles that invoke "Apply .editorconfig rules").
- Visual Studio: ensure **Tools → Options → Text Editor → C# → Code Style → General → Enable .editorconfig support** is turned on.
- Avoid IDE cleanup profiles that perform conflicting actions (e.g., rearranging members). Prefer tooling that consumes `.editorconfig` directly.

## Making Rule Changes

1. Update `.editorconfig` (or add a scoped file) with the new rule or severity.
2. If the change introduces a new analyzer package, add the dependency in `Directory.Build.targets`.
3. Run `dotnet format` to apply any required code fixes.
4. Capture the resulting changes and update this guide or other docs if the workflow shifts.

## Continuous Integration Expectations

- CI should run `dotnet build.cs test` at minimum. Add a dedicated formatting or analyzer step (`dotnet format --verify-no-changes`) if you want the pipeline to enforce style automatically.
- Suppress diagnostics only with justification. Prefer targeted `.editorconfig` overrides or `[SuppressMessage]` attributes over global disables.
