# Focused R1/R2 re-review

**PASS — the full successor 39-path documentation batch is accepted for content/citation review
at the exact identities below.** R1 and R2 are resolved. No mandatory correction remains from this
review. This is not compiler/runtime acceptance or authorization to publish before coordinator gates.

Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-applet-boundaries`.
Baseline HEAD: `da66f5d01ba594993974ddd26723e8c300c0a454`.
Runtime/product pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
Accepted full successor `product.diff` SHA-256:
`e15e4afcc7c2ae932858408db0f50793f5174d415b84ea10fe22dfc3fe438df8`.
Accepted 233-path ledger SHA-256:
`2414b45a57381ee489ffde0843ee9a8e4ec1d040d1c13b8ed26c4ddccb58b067`.

## R1 — Resolved: explicit coordinate convention and scoped source meaning

I independently checked all five `source-anchors.json` excerpts against the retained original bytes,
their SHA-256 values, Python `str.splitlines()` coordinates, LF-only coordinates and half-open UTF-8
byte offsets. They agree exactly.

| Passage | `str.splitlines()` inclusive lines | LF-only inclusive lines | UTF-8 bytes, half-open |
| --- | --- | --- | --- |
| SCP03 §6.2.6, page 29 | 1177–1194 | 1149–1166 | [76417, 77722) |
| SCP03 §6.2.5 error/EXTERNAL AUTHENTICATE exceptions | 1048–1055 | 1023–1030 | [68963, 69606) |
| SCP03 host R-MAC verification | 1084–1086 | 1059–1061 | [70640, 70859) |
| SCP03 optional/switchable R-MAC context | 1158–1164 | 1131–1137 | [75412, 75921) |
| SCP11 §4.3, page 24 | 1108–1134 | 1085–1111 | [88509, 90742) |

Form-feed boundaries explain the different numbering. **The earlier review's locator criticism must
be read as a convention ambiguity, not proof that the original LF-based locator selected a different
source or contradicted the substantive claim.** The successor now states the convention and supplies
both coordinate systems and exact bytes; no historical source/register/review was rewritten.

The successor citation wording preserves SCP03 §6.2.6's **both C-DECRYPTION and C-MAC** applicability,
selected/optional R-MAC rather than a universal response requirement, and the error/EXTERNAL
AUTHENTICATE exceptions. SCP11 keeps a/c OCE-to-SD certificate plus first-correct-MAC authentication
separate from SD-to-OCE certificate/receipt authentication and b's device-only direction. It does not
turn the descriptive §4.3 paragraph into a capitalized MUST or the factory flag into administrative
authority. SCP03 1.1.2 Public Release remains distinct from uninspected 1.1.2.6 Public Review; SCP11
1.3.0.13 Public Review remains distinct from final 1.4 and the unverified complete vendor profile.

## R2 — Resolved: attempted reset precondition, not verified restoration

I re-read both complete affected test methods, their finally blocks, provisioning/assertion context,
the reset helper and independently timed Track teardown.

- `SecurityDomainSession_Scp03Tests.cs:114–117` now describes requesting reset to attempt the
  default-key precondition needed by later tests/suites and explicitly disclaims a restored-content
  assertion. The reset call and existing class token are unchanged.
- `SecurityDomainSession_Scp03KeyLifecycleTests.cs:211–214` correctly says Session 2 replaces the
  original custom set imported in Session 1, not defaults. It gives the same attempted-precondition
  and no-readback qualification. The reset call and `ct` are unchanged.
- `candidate-decisions.md` and the two RETAIN → EDIT path decisions correctly apply the correction
  to CORE-SD-X008 and related X004. Original IDs/context and SD-IMP-001/005/007/008, SD-DEP-004 and
  B5-C006 holds remain intact. Track teardown is another request, not a restored-state oracle.

The comments preserve the reason later tests need public default-key authentication without promising
cleanup success. No assertion, lifecycle, token, helper or runtime remedy was introduced.

## Preservation and acceptance scope

Fresh independent read-only checks established:

- All **233** current before/after hashes match the successor ledger: **39 edited / 194 retained**,
  comprising **29 edited C# / 10 Markdown** paths.
- Exactly the two R2 paths differ from the prior reviewed ledger. The other **231 entire path records**
  are exact, and all **37 original product-diff sections** remain byte-exact in the successor patch.
  The full actual diff, with explicit `c/` and `w/` prefixes, matches the accepted successor hash.
- The focused actual two-file delta equals `R2-comments.diff`. Changes are full-line comments and
  recorded terminal newlines. Non-comment lines compare exactly across all 29 edited C# paths.
  The new comments are outside executable literals. No new import/directive/signature/default/
  attribute/assertion/runtime-help/API change is present.
- All **107** prior example bodies, hashes and ranges still match actual source and the successor
  preservation witnesses; no example changed in revision 2. All **15** original editor/reviewer
  history records match their preserved hashes. `git diff --check` returns zero.

These preservation checks carry forward the earlier full content review for unchanged material; this
focused re-review does not claim another broad applet audit. `review-identities.json` binds the
accepted successor records, two source hashes and original standards derivatives.

Final compiler/analyzer/cref, token/API and meaningful consumer/example coordinator gates remain
pending; static non-comment comparison is not compiler acceptance. All prior engineering/policy,
source-edition/profile, native/device and HSM peer/host/connector qualifications remain open at their
original scopes. OATH's fourteen historical resolutions/terminal reset, HSM's retry-message retraction
and HSM-IMP-008, the unready Windows delta, PIV physical deferrals and historical hardware limits are
unchanged. No build/test/formatter/native/device/crypto/connector operation, Git mutation or subagent
ran. Only this successor review and its identity manifest were written.

The coordinator completes the remaining gates, publishes Core Pass 1 and its deferred-finding
handoff, then stops. This PASS authorizes no implementation, Pass-2 project or further device work.
