# AGENTS.md

## First Reads
- Root `CLAUDE.md` is the canonical detailed agent guide; this file is only the compact OpenCode ramp-up sheet.
- If touching `src/<Module>/`, read that module's `CLAUDE.md` first when present. Tests subfolders may also have their own `CLAUDE.md`.
- Prefer executable truth over prose: `toolchain.cs`, `Directory.Build.props`, `Directory.Packages.props`, `.github/workflows/build.yml`, and test `.csproj` files settle command and runner questions.

## Toolchain Commands
- Use .NET 10 SDK. The repo defaults to `net10.0`, C# 14, nullable enabled, central package management, and warnings-as-errors for non-test projects.
- Do not run `dotnet build` directly. Use `dotnet toolchain.cs build` or `dotnet toolchain.cs build --project Piv`.
- Do not run `dotnet test` directly. Unit tests mix xUnit v3/Microsoft Testing Platform and xUnit v2; `dotnet toolchain.cs test` detects and invokes the right runner.
- Focus tests with both module and filter when possible: `dotnet toolchain.cs test --project Fido2 --filter "Method~Sign"`.
- `--integration` requires `--project`: `dotnet toolchain.cs -- test --integration --project Piv --smoke`.
- `--smoke` skips `Slow` and `RequiresUserPresence`; agents should not run touch/insert/remove tests unless a human explicitly coordinates hardware.
- Use `dotnet toolchain.cs -- --help` when arguments act strangely; `--help` and some options need the `--` separator.
- CI runs `dotnet toolchain.cs build`, `dotnet toolchain.cs test`, then `dotnet toolchain.cs pack --package-version 2.0.0-preview.<run>`.
- Run `dotnet format` or `dotnet format --verify-no-changes` before claiming formatting is clean.

## Project Shape
- Modules live under `src/` with directory names stripped of the `Yubico.YubiKit.` prefix; assembly, namespace, and package names keep the prefix.
- `Core` owns device discovery, connection abstractions, APDU processing, SCP support, platform interop, crypto utilities, and TLV utilities.
- App modules are `Management`, `Piv`, `Fido2`, `WebAuthn`, `Oath`, `YubiOtp`, `OpenPgp`, `SecurityDomain`, and `YubiHsm`.
- `WebAuthn` is a higher-level surface over `Fido2`; do not duplicate CTAP/FIDO behavior there.
- Broad exploration shortcut: run `codemapper .` to generate gitignored API maps in `codebase_ast/`.
- DI entry point for Core is `AddYubiKeyManagerCore()` in `src/Core/src/DependencyInjection.cs`.

## Architecture Gotchas
- APDU flow is a decorator pipeline: command chaining, short/extended APDU formatting, `ISmartCardConnection.TransmitAsync()`, then chained response reassembly.
- `ApplicationSession` centralizes firmware, initialization, authentication, and protocol ownership; prefer `IsSupported(...)` / `EnsureSupports(...)` over duplicating firmware gates.
- Platform interop belongs under `src/Core/src/PlatformInterop/{Windows,MacOS,Linux,Desktop}` with safe handles and `SdkPlatformInfo` platform selection.
- FIDO2 over USB primarily uses HID FIDO. SmartCard/CCID FIDO2 is supported over NFC and over USB when the FIDO2 AID is exposed; USB SmartCard FIDO2 requires firmware 5.8.0+.
- `TlvBuilder` and `DisposableTlvList` must be disposed.

## Hardware And Integration Tests
- Integration tests require authorized YubiKey hardware. The shared test infrastructure reads `YubiKeyTests:AllowedSerialNumbers` from `appsettings.json`; an empty/missing allow list can hard-fail with `Environment.Exit(-1)`, and unauthorized devices are filtered out.
- Linux hardware tests need PC/SC pieces like `pcscd`, `libpcsclite-dev`, and `libudev-dev`; CI starts `pcscd` manually before build/test.
- `[WithYubiKey]`/`YubiKeyTestState` tests discover devices lazily at execution, not discovery. Devices must already be connected before the test runner starts.
- Security Domain integration helpers reset the Security Domain by default before each test; pass `resetBeforeUse: false` only when preserving state is intentional.
- PIV tests commonly need manual application reset to defaults at the start of the test unless a helper documents otherwise.

## Security And Memory Rules
- Small synchronous byte buffers: `Span<byte>` with `stackalloc` up to 512 bytes. Larger temporary buffers: `ArrayPool<byte>.Shared.Rent()` with `try/finally` return.
- Async byte data crossing awaits should use `Memory<byte>`, `ReadOnlyMemory<byte>`, or `IMemoryOwner<byte>`, not `Span<byte>`.
- Avoid `.ToArray()` and LINQ on byte data unless data must escape the current scope.
- Always zero PINs, PUKs, keys, SCP material, and secret-derived buffers with `CryptographicOperations.ZeroMemory()`.
- Use `CryptographicOperations.FixedTimeEquals` for secret-derived comparisons; do not use `SequenceEqual` on secrets.
- Never store a privately cloned sensitive `byte[]` in a struct; struct copies keep references you cannot reliably zero. Use a sealed disposable class for owned sensitive buffers.
- Logging is static via `YubiKitLogging.CreateLogger<T>()`; do not add SDK constructors that require `ILogger`/`ILoggerFactory`.
- Never log PINs, keys, credentials, challenge/response secrets, or sensitive payloads. Log lengths, algorithm IDs, status, and non-secret metadata only.

## Tests Worth Writing
- Do not add validation-only tests that just prove `ArgumentNullException.ThrowIfNull` or framework type checks work.
- Do not add skipped placeholder tests. If behavior cannot be unit-tested, document the limitation and point to an integration path.
- Use fake connections, queued APDU responses, and byte/vector assertions for protocol behavior; use integration tests only for real hardware behavior.

## Git And Release Notes
- The workflow file currently triggers on `yubikit` and `yubikit-applets`; verify the active plan/branch before assuming a PR base.
- Only stage files you changed explicitly; never use `git add .`, `git add -A`, or `git commit -a`.
- Package signing is `sign.cs` and is Windows-only because it depends on `signtool.exe`, Windows certificate store, NuGet CLI, GitHub CLI, and a signing smart card.
