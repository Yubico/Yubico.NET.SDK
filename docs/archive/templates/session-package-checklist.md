# Session Package Checklist (Template)

Use this checklist when introducing a new `Yubico.YubiKit.<Module>` session package (e.g., PIV/OATH/OTP/OpenPGP/YubiHsm/FIDO/Management/SecurityDomain).

## API surface + DX

- [ ] Public entry point(s): `IYubiKey` extension(s) for the most common flow
- [ ] Explicit transport expectations in XML docs (e.g., SmartCard-only)
- [ ] 3-tier docs examples in README:
  - [ ] one-liner convenience
  - [ ] session usage (recommended)
  - [ ] manual connection usage
- [ ] Public model type guidelines applied:
  - [ ] small immutable data: `readonly record struct`
  - [ ] larger immutable data: `sealed record`
  - [ ] resource-backed: `sealed class : IDisposable`
- [ ] Avoid re-exports/aliases in the surface area; document where to find types instead

## Session implementation

- [ ] Session derives from `ApplicationSession`
- [ ] Initialization uses `ApplicationSession.InitializeCoreAsync(...)`
- [ ] Protocol created via protocol factory `.Create()` (no logger factory threading)
- [ ] Session state owned by base (`Protocol`, `FirmwareVersion`, `IsInitialized`, `IsAuthenticated`)
- [ ] Disposal works via base; derived does not null/dispose protocol out-of-band

## Dependency Injection

- [ ] DI registration in `DependencyInjection.cs` (factory delegate or typed factory)
- [ ] DI docs snippet included (typical lifetimes + disposal patterns)

## Logging

- [ ] README includes how to configure `YubiKitLogging.LoggerFactory`
- [ ] No API requires passing an `ILoggerFactory`/`ILoggingFactory` to instantiate sessions
