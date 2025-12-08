# Yubico.NET.SDK

[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Yubico/Yubico.NET.SDK/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Yubico/Yubico.NET.SDK)

## Test Runner Support in IDEs

- Unit test projects use xUnit v3 with the Microsoft Testing Platform (`<UseMicrosoftTestingPlatformRunner>true`). Run them via `dotnet run --project ... --no-build` or use the build script (`dotnet build.cs test`).
- Integration test projects remain on xUnit v2 with `Microsoft.NET.Test.Sdk`, so they will appear in VS Code’s Test Explorer.
- VS Code’s C# extensions do **not** yet discover xUnit v3 / Testing Platform projects. Until Microsoft ships support, the unit tests are invisible in the Testing tab even though they run fine from the CLI.
