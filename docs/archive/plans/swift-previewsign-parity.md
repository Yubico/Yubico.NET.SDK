# yubikit-swift previewSign Parity Report (retroactive)

**Date:** 2026-04-23 (retroactively closing the original Phase 9.2 Step 1 deliverable)
**Investigated:** yubikit-swift release/1.3.0
**Verdict:** **CODE-PRESENT-UNTESTED** — registration **and** authentication code paths exist; only registration is exercised by the test suite. Hardware test for authentication is absent.

## Findings

**Code paths:** Both `MakeCredential` (with `previewSign.generateKey`) and `GetAssertion` (with `signByCredential`) are implemented. Wire-format encoding for authentication produces the same CBOR shape as the C# port: a flat map under the string key `"previewSign"` containing keyHandle and TBS payload (per the diagnostic comparison at `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:105` — *"Swift reference (PreviewSign.swift:193-206) produces identical structure"*).

**Hardware tests:** Registration only.
- `PreviewSignTests.swift` covers `MakeCredential` flows that produce a generated signing key.
- **No** test method exercises the authentication path (`GetAssertion` with `signByCredential`).
- Diagnostic note in this repo at `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:106` records: *"yubikit-swift's PreviewSignTests.swift has NO authentication test — only registration."*

**Documentation / release notes:** Not separately surveyed for this retroactive report; the diagnostic comparison already confirmed the wire-format identity between the Swift and C# encoders.

## Citations

- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:105-106` — original C#-side diagnostic that synthesizes the Swift parity finding
- `yubikit-swift release/1.3.0` `PreviewSign.swift:193-206` — referenced wire-format encoder (per the diagnostic above)
- `yubikit-swift release/1.3.0` `PreviewSignTests.swift` — registration-only test surface

## Recommendation for Phase 9.2

**Supports path 2A** alongside the Rust report. Swift confirms the wire-format identity hypothesis: C# and Swift produce structurally-identical CBOR, yet C# fails on hardware. This means the bug is **not** at the abstract structural level (where Swift, Rust, and C# all agree) but at a lower-layer encoding detail (byte-string length headers, ordering, or a missing `additional_args` for ARKG payloads). The Rust hardware test is the disambiguator.

**Not supported by Swift:** the multi-credential probe path. Same DEFER to Phase 10 as the other parity reports.
