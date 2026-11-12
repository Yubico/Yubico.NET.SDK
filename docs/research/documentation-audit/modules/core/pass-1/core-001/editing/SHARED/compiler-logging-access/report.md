# Compiler SAMPLE-WA002 / FULL032: root logging-access qualification

This bounded instruction correction changes only the root `CLAUDE.md` logging bullet. It propagates
the actual accessibility boundary behind compiler SAMPLE-WA002/FULL032; the FIDO owner separately
corrects WebAuthn module examples. No public API, IVT, runtime behavior or logging policy is added.

The before-file matched primary current accepted B4-corrected root bytes. All root fenced bodies and
all prior accepted/editor/review/sampling/architecture-map patches remain byte-exact. The focused patch
and exact before/after hashes accompany this report; independent review is pending.

## Checked evidence

- `src/Core/src/YubiKitLogging.cs:20–39`: YubiKitLogging and LoggerFactory are public, while both
  CreateLogger helpers are internal. The generic helper already delegates to the factory extension.
- `src/Core/src/Yubico.YubiKit.Core.csproj:27–60`: explicit Core friends include selected applet/test
  assemblies, but not WebAuthn or WebAuthn integration tests.
- `Directory.Build.targets:44–50`: shared friend entries use the current assembly's own UnitTests/
  IntegrationTests names and legacy names. For Core they do not grant WebAuthn assemblies access.
- Context7 `/dotnet/docs` confirms the public generic `LoggerFactoryExtensions.CreateLogger<T>`
  extension on ILoggerFactory. Its call site uses `Microsoft.Extensions.Logging`; this API access
  route is distinct from the inaccessible internal Core wrapper. No compiler or provider was invoked.

The single bullet now uses the internal helper only where Core/friend access exists, and otherwise
the public `YubiKitLogging.LoggerFactory.CreateLogger<T>()` extension. It keeps static logger use,
configure-before-first-use, provider lifetime through captured logger use and the existing no-injected-
ILogger preference. This makes house guidance usable at the actual assembly boundary rather than
inventing a runtime exception or another friendship.

`source-evidence.json` binds exact current declarations/targets and the Context7 support used here.
`before.md`, `after.md`, `product.patch`, `changed-path-sha256.json`, unchanged complete fence-body
records and static verification are scoped to this bullet. All other current product paths and prior
SHARED records are checked unchanged. No primary write, source/config/script/API change, build/test/
formatter/compiler/native/crypto/device operation, Git index/ref/commit/push or subagent runs.
Complete focused review and Core Pass-1 publication/handoff, then stop.
