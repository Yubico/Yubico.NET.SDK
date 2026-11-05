# PIV corrections and evidence progression

## Planning and setup observations

The initial helper-preparation packet guessed AES256 as a possible recent default. Fresh CLI and
public metadata establish AES192/raw0A on both captured development keys. The coordinator corrected
the hypothesis before implementation of authentication/cleanup. The helper uses actual metadata.

The harness allowlist was initially assumed ignored, following existing guidance. A `git ls-files`
assertion showed the shared file is tracked; the failed assertion preceded any write. Original bytes
and hash were then retained with the correct tracked classification. Its source is still unchanged.

## Initial cleanup control — chained F9 metadata

`hardware/cleanup-control-103.json` failed its F9 comparison **before PIN/PUK blocking or RESET**.
Its independent fresh before-capture matched the baseline exactly. The cleanup helper requested full
metadata through the vendor protocol, which handles GET RESPONSE chaining. It incorrectly compared
that result with a baseline trace filtered to a single F7/9000 transport response.

The actual baseline F9 metadata response is256 bytes/SW6117 followed by23 bytes from C0/SW9000.
The trace retained public F7 payload bytes but no C0 payload; the parsed baseline metadata contains
the full public-key digest/SPKI, policy and origin after vendor protocol assembly. Consequently the
single-F7/9000 list was empty and the helper stopped with “Complete live F9 public metadata changed.”
This establishes a helper assumption failure, not a changed attestation key or failed device reset.

Versioned correction is assigned to compare all **captured parsed F9 fields** with the corresponding
fresh decoded metadata and retain the independent certificate identity guard. The original baseline,
public-state helper, failed receipt and original cleanup source remain preserved. Unknown raw fields
absent from the original capture cannot be represented as independently baseline-compared.

V2 control passed: bounded blocking/reset completed, fresh post-reset public capture matched every
captured baseline field, factory User PIN and metadata-selected AES192 management-key authentication
succeeded, and another fresh capture matched baseline. F9 identity/policies were preserved. This
qualifies the independent cleanup path on serial103; original V1 failure remains retained.

## Supplemental preparation and first compiler capture

Coordinator source review caught two preparation issues before any probe ran: the actual shared
configuration contains full-line JSONC comments, and OpenSSL public fixture files should remain outside
the UTF-8/registered-primary-PDF archive. Versioned preparation now strips only those comment lines
in memory and records configuration identity; fixture files live in a unique external directory with
public path/size/hash records. Desired-contract failures remain failures; P07 is blocked following
the user's physical-test deferral. Original preparation sources are retained.

P01's first wrapper invocation built the audit host successfully, then failed compiler capture because
its initial capture script still required `EnableNETAnalyzers=true`. The repository's explicit pinned
analyzer package selects its own canonical DLLs and disables duplicate SDK analyzers, as established
in earlier captures. **No P01 test or pre-test reset ran.** The successful build and failed capture
remain under hardware-probe/evidence/p01-x25519-v2. Correction must prove actual Csc membership and
both canonical analyzers, preserve strict warnings/no-skip policy, and retain full source/config/assets;
relaxing the false property equality alone is insufficient. New versioned tooling is being prepared.

The next fresh build passed with the audit host's explicitly selected AllEnabledByDefault policy.
Its capture then stopped on a second assumption: late MSBuild ProjectReference items contain the
four declared direct projects **and eight transitive projects**, rather than only the direct four.
All preceding actual Csc/source/analyzer/policy checks passed, and no probe/reset ran. Versioned
correction must reconcile the full reachable project graph and bind each actual compiler reference
to its real project output. Original direct-reference declarations and failed captures remain retained.

## Independent content corrections and sampling

Three required review groups corrected six existing product paths: actual F9 result property/JSON
shape, one-or-both default credentials after partial rotation, and SHA256/differential Ed25519 test
oracle descriptions. Re-review accepted all57 paths, with no optional rewrites. The original61 bodies
and current60 bodies reconcile; only the intended F9 example changed during correction. Runtime
code/assertions remain unchanged. Internal/test sampling expanded byten after the oracle misses;
final current sample is61 of385 eligible regions, above the39 minimum. Scoped formatting preserved
all non-whitespace characters and imports in48 C# paths.

## Actual annotated-example analyzer failure

The v2 consumer's44 groups compiled warning-free. Its first capture hit the same direct-only project
reference assumption; additive project-graph tools preserved175 original identities and successfully
captured the intact consumer build assecond, proving its exact declared/closure/Csc bindings.

The separate annotation build then failed **xUnit2002** on the CLAUDE example's `Assert.NotNull`
applied to the nonnullable PinMetadata record struct. This is a genuine documentation-example defect,
not an SDK build failure or a capture-tool assumption. Coordinator verified the actual record fields
and replaced that assertion with a remaining-retries range check, adding the operation's5.3 firmware
prerequisite to its WithYubiKey attribute. Only two Markdown code-body lines changed; shipping test
assertions and all48 edited C# paths remain unchanged.

The failed v2 source/build evidence stays immutable. Prior reconciliation outputs/tool and exact
hash-matched pre-correction CLAUDE bytes are retained under validation/reconciliation-v1. Independent
review of the complete corrected example and a new accepted source/example context precede recompilation.
No analyzer suppression or synthetic SDK substitute is used to bypass the real compiler finding.
