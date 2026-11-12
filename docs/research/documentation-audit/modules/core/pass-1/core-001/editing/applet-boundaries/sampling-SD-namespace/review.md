# SAMPLE-SD-001 focused namespace review

**PASS — accept only the focused two-bullet patch** with SHA-256
`b1cb5c00f569c94499060cd1539edf2bafa67057763126e3786c3f0e83c8ab65`.
No correction remains within this scope.

The bullets in `src/SecurityDomain/CLAUDE.md:241–242` match the actual declarations:

- `StaticKeys.cs:19,24` declares `StaticKeys` in
  `Yubico.YubiKit.Core.Protocols.SmartCard.Scp`.
- `ECPrivateKey.cs:17,27` and `ECPublicKey.cs:17,27` declare the EC key wrappers in
  `Yubico.YubiKit.Core.Cryptography`.

The correction uses those namespace suffixes consistently with the Related Modules list. It moves
`StaticKeys` to its actual SCP namespace without changing documentation policy, executable code,
API, examples or any other narrative section.

## Independent preservation checks

Read-only checks verified:

- Preserved `before.md` equals the current accepted primary file and the accepted 39-file ledger's
  Security Domain entry: SHA-256
  `76408f4a4ce1995712731ca7201b390836b9d4ded236268c619424a460d3915a`.
- The current file is SHA-256
  `dd24dbcc43606abd77888fecdf9368b0c103b74b860fba1c46d3c10ccdab9022`.
  Reconstructing its unified diff against that before file gives the exact focused patch above;
  only lines 241–242 differ.
- All other **232** accepted source-path hashes and all **30** preserved editor/reviewer records
  remain exact. The three namespace-source file hashes match the supplied source evidence.
- All **107** example bodies, hashes and source ranges remain unchanged.

This accepts a separate delta atop the previously reviewed 39-path batch; it does not silently
replace that historical batch's patch or ledger. The sampler owns reconciliation and any sampling
expansion for `core-001:unit:ed3e115f81fd126c`. **Edited-unit sampling credit is zero.** This review
does not claim sampling completion or authorize a broader audit.

Only this review and its identity manifest were written. No SDK build/test, formatter, native,
device, cryptographic or connector operation, Git command/mutation or subagent ran. Existing
coordinator/compiler/example/publication gates and all deferred findings remain unchanged.
