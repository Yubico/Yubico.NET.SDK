# SAMPLE-SD-001: Security Domain related-module namespaces

**The two Related Modules bullets are corrected for focused review.** This change addresses Astra's
selected unit `core-001:unit:ed3e115f81fd126c` in `src/SecurityDomain/CLAUDE.md`, lines 241–242.
It changes no policy, API, runtime behavior, test, example or other narrative section.

Before editing, the file matched the accepted primary checkout byte-for-byte and its entry in the
39-file successor ledger. Its SHA-256 was
`76408f4a4ce1995712731ca7201b390836b9d4ded236268c619424a460d3915a`.
The exact file is preserved as [before.md](before.md), with comparison/history identities in
[before-identities.json](before-identities.json).

The verified declarations in [source-evidence.json](source-evidence.json) place `StaticKeys` in
`Yubico.YubiKit.Core.Protocols.SmartCard.Scp`, and `ECPrivateKey`/`ECPublicKey` in
`Yubico.YubiKit.Core.Cryptography`. The bullets use those namespace suffixes under the common
`Yubico.YubiKit` prefix: **Core.Protocols.SmartCard.Scp** and **Core.Cryptography**. `StaticKeys`
is now listed with SCP rather than the EC key wrappers.

[focused.diff](focused.diff) contains only this two-line delta against the accepted before file.
[verification.json](verification.json) records exact before/after hashes, changed ranges, source
declaration identities and preservation checks. Existing 39-file patches, source/review records and
all 107 example bodies remain unchanged as historical artifacts; this correction is a separate delta.

Focused re-review covers these two bullets and their three namespace declarations. The sampler owns
edited-unit credit and any Security Domain narrative sampling expansion. This record neither claims
sampling completion nor expands the audit.

No build, tests, formatter, cryptographic/native/device calls, Git mutations or subagents ran. PIV
physical deferrals, future user Windows handoff, HSM profile holds and OATH's fourteen historical
resolutions remain intact. The coordinator completes Core Pass 1 and its deferred handoff, then stops.
