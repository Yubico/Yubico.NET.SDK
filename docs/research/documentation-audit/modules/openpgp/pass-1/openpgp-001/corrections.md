# OpenPGP corrections and evidence progression

## Initial vendor-client certificate inventory

The original read-only vendor-state helper completed metadata reads but explicitly failed three
certificate reads (ATT/DEC/SIG) with NotSupportedError. Direct Python OpenPgpSession reports version
**0.0.1**, whereas ykman CLI displays app version **5.8.0** and device metadata **5.8.0.beta.0**.
Installed get_certificate/_select_certificate require version >=5.2 for those paths; AUT uses the
legacy default-certificate read and reported empty data. This is a client-side version gate, with
actual certificate state still unknown at that checkpoint. Original baseline JSON and failure log
are preserved; no mutation has run.

Coordinator introspection first attempted `ykman.cli.openpgp` and failed ModuleNotFoundError; direct
inspection of installed yubikit.openpgp succeeded and exposed the two relevant gates. Focused helper
correction will read public certificate data using explicit vendor command/framing provenance and the
freshly observed development metadata, retaining both original applet version and exact chosen path.
Shipping SDK/Python installation and device firmware are unchanged.

V2 baseline succeeded using installed vendor GET DATA/SELECT DATA shapes selected by fresh guarded
beta metadata. Every public APDU retains command/status/length, while raw version remains 0.0.1.
It also reads UIF objects directly because the low-version vendor branch synthesizes Off. Those
reads confirm Off on all four slots. The original helper/source snapshot and unsuccessful JSON remain
unaltered. Att certificate is present (793 bytes; DER SHA256
94afb703099edb4aaaee443c6e197c379f7f909d23f717f0868ef73057e4a16a); all normal certificates are absent
through successful zero-length responses. Subsequent cleanup binds to vendor-baseline-v2.json.

## Interrupted first integration batch

Coordinator initially supplied a 120-second shell timeout for the 48-method batch including both slow
RSA generation cases. The shell terminated the invocation after 24 reported passes (RSA4096 56 seconds,
RSA3072 30 seconds); no final RESULT or wrapper exit was reached. Original wrapper log and full available
toolchain text are retained in validation/integration-nonkdf-interrupted with exact hashes/counts.
Process inspection found no surviving toolchain/testhost/VSTest process. Independent vendor reset,
fresh full state/factory PIN verification and attestation identity preservation passed afterward.
The full batch is repeated with a 600-second outer allowance; the interrupted attempt remains incomplete.

## Probe and documentation corrections

KDF v1 generated the control key before F9 and received6985. V2 changed only that order: F9/fresh
Admin verification preceded generation, and ordinary signing then passed before extended VERIFY
exposed the distinct-salt/typed-counter defect. V1/v2 sources, statuses and cleanup remain retained.
Attestation v2 used extended-only User verification and received6982 before its successful ceremony;
v3 used the ordinary mode supported by existing tests/vendor examples and passed the full certificate
oracle. The original failure remains evidence of the measured authorization boundary.

Initial human-ready Sign/Decrypt touch attempts ended6600/Failed. User reconfirmed immediate readiness
and retries passed their original crypto/lifecycle assertions. These are timing/ceremony attempts,
not an SDK fix. No-touch cancellation separately measured6600/Failed and fresh-session cryptographic
recovery before reset. ATT On persistence through reset was observed and explicitly restored to Off.

Independent review found five residual documentation problems after the first edit: typed-zero blocking
implication, host-session PIN-policy wording, mislabeled fake6A82 statuses, guaranteed-reset summary,
and sampled generation-time zero/signed-carrier semantics. Fresh editor corrected seven paths; original
58-path report and review remain immutable. Re-review accepted59 paths. Expanded sampling excludes
the corrected timestamp unit and passes11 of32 remaining units, with additional API checks after misses.
Final formatting was whitespace-only and required no import exception or relaxed checker rule.

## Consumer compiler-capture analyzer assumption

All17 consumer groups compiled with zero warnings. The first immediate compiler capture rejected
`EnableNETAnalyzers=false` as if analyzers had been skipped. Actual capture contains ten analyzer
arguments, including both canonical Microsoft.CodeAnalysis.NetAnalyzers9.0.0 assemblies; warnings-as-
errors and RunAnalyzersDuringBuild are enabled and no skip flag is present. Root targets explicitly
reference the analyzer package, which may disable duplicate SDK-bundled analyzers. The tooling owner
is tracing the imported package property and correcting this evidence assumption with exact provenance.
Original failure, policy, compiler arguments and v1 tool/manifest identities remain retained. Shipping
code and analyzer settings are unchanged; a successful build alone is not substituted for full capture.

The exact pinned package import proved that interpretation. Versioned analyzer-package-1 tools preserve
the original110 preparation dependencies and require both canonical analyzer DLLs in actual Csc, with
all warning/no-skip/membership checks retained. Corrected consumer second capture reused the successful
compilation; full19-source shipping build/first capture and final live verification then passed.
