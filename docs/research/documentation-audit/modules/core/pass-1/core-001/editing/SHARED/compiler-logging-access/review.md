# Independent focused review — root logging accessibility bullet

**PASS for the exact one-bullet patch.** No mandatory correction remains in this bounded return.

- Accepted patch SHA-256: `b43c9a1da1416b45971f25e81b25bc46705158066e474d214e506f7909b452bd`.
- Accepted path: root `CLAUDE.md`, logging bullet at line 114.
- Before SHA-256: `6e4708a941776bbfd20aa84beae02a3f07e9e114a463d488d4ca22306b61c383`.
- Accepted after SHA-256: `48c7ffbeede39f50234212025f6b8ae809a7717a1c386a44aae2f185acf7d264`.

This is the focused SHARED instruction correction for compiler SAMPLE-WA002/FULL032. The WebAuthn
body/example correction and its compilation review remain separately owned by FIDO; this review
neither edits nor accepts that separate patch. Only this review and its identity record were written.

## Accessibility and policy — PASS

Actual current source supports both routes and their stated boundary:

- `src/Core/src/YubiKitLogging.cs:20–39` declares the class and `LoggerFactory` property public,
  with property type `Microsoft.Extensions.Logging.ILoggerFactory`. Both Core CreateLogger overloads
  are internal. The generic internal wrapper already delegates to the same factory extension with
  `Microsoft.Extensions.Logging` imported; the new instruction does not add a different logging mechanism.
- `src/Core/src/Yubico.YubiKit.Core.csproj:27–60` explicitly grants selected applet/test assemblies
  access but does not name WebAuthn or its integration tests. `Directory.Build.targets:44–50` adds
  Core's own assembly-named test friends and legacy friends, not WebAuthn friendship. The root bullet
  now restricts the internal helper to actual Core/friend access instead of treating it as universal.
- Public `LoggerFactoryExtensions.CreateLogger<T>(this ILoggerFactory)` returns `ILogger<T>` in
  `Microsoft.Extensions.Logging`. Independent Context7 `/dotnet/docs` checks and the namespace-specific
  Microsoft Learn API reference support the extension route; the logging overview includes the
  `using Microsoft.Extensions.Logging` / `loggerFactory.CreateLogger<Program>()` pattern. The
  namespace-specific API was checked independently rather than treating the editor's MTP architecture-
  services reference as sufficient namespace proof. No package/provider version or installed-runtime
  claim is inferred from current API documentation.
- The bullet preserves static logger use, configuration before first use, providers alive through
  captured logger use and the existing **NEVER inject ILogger** preference. It does not claim logger
  replacement, provider ownership transfer, global redaction, deep isolation or a new IVT/public API.

No source, project, targets, API snapshot, friendship or runtime body changed. The valid public route
does not imply that the separate WebAuthn caller has already been corrected or compiler-verified.

## Independent static verification

- The complete before snapshot freshly matches current PRIMARY root bytes and the recorded accepted
  predecessor hash. The complete after snapshot matches the actual guide file and accepted hash.
- Reconstructing the conventional before-to-after diff gives the exact patch. There is exactly one
  removed bullet and one replacement bullet; no other root line changes.
- All **17** root fence bodies, languages, coordinates and SHA-256 identities are unchanged and match
  independently extracted before/after receipts. No new example-compilation credit is granted.
- All **26** other current product paths and all **100** prior artifact preservation entries remain
  byte-identical. Earlier full-guide, sampling, lifecycle, type-guidance and map acceptance identities
  remain immutable predecessors, not relabeled seals for the new root bytes.
- All three actual source/config evidence files match their captured complete text and SHA-256.
  `git diff --check` and read-only focused reverse-application check each exit **0**. HEAD remains
  `da66f5d01ba594993974ddd26723e8c300c0a454`, and the index is unchanged.

The exact identities and API-reference scope are bound in [review-identities.json](review-identities.json).
No product/prior-record/PRIMARY write, SDK build/test/compiler/formatter, provider/native/device/crypto
operation, Git mutation or subagent occurred. This PASS accepts only the root bullet. Separate FIDO
body/compiler checks, final reconciliation/validation, Core Pass-1 publication and the precise deferred
handoff remain coordinator gates. Finish Core Pass 1, then stop.
