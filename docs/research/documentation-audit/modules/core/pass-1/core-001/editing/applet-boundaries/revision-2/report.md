# Applet-boundary revision 2: R1/R2 correction return

**R1 and R2 are corrected in successor records and two Security Domain comment paths.** This is a
bounded correction return for fresh re-review, not independent acceptance or publication. All old
editor/reviewer/source-register bytes and the original 37-file patch are preserved.

Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-applet-boundaries`.
HEAD: `da66f5d01ba594993974ddd26723e8c300c0a454`.
Product source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
Review input: [review.md](../review.md) and [review identities](../review-identities.json).
The immutable first patch is [../product.diff](../product.diff), SHA-256
`6c3797572fc9dcc7cb7fff70f4f1122f2736101462a43289ff95f7085a71a568`.
The full successor [product.diff](product.diff) has SHA-256
`e15e4afcc7c2ae932858408db0f50793f5174d415b84ea10fe22dfc3fe438df8`.

## Verify the feedback against source

**R1:** both requested ranges match the retained originals when numbered using Python
`str.splitlines()`, including form-feed page boundaries. SCP03 §6.2.6 is at 1177–1194; its explicit
both-C-DECRYPTION-and-C-MAC condition is at 1179–1183 and counter/absent-data text at 1184–1194.
SCP11 §4.3 is at 1108–1134: a/c's certificate plus first correct-MAC APDU at 1111–1115, SD certificate
and receipt at 1123–1126, and mutual versus b-only distinction at 1127–1128.

The same retained bytes use LF-only lines 1149–1166 and 1085–1111 respectively. This explains why
LF-only readers located the passages differently: the first citation record did not identify its
line convention. There is no substantive protocol contradiction or changed source identity. The
successor [citation record](citations.md) explicitly states the convention and both locators;
[source-anchors.json](source-anchors.json) stores hashes, exact excerpts and UTF-8 byte offsets.
It preserves edition/status, actor and selected-R-MAC conditions rather than importing a broader
unqualified summary from a historical source register. No product claim changed for R1.

**R2:** actual code confirms the two local finally blocks only await `ResetSecurityDomainAsync`,
with the existing class token or `ct`, and do not read back/assert factory contents afterward.
In `PutKeyAsync_WithReplaceKvn_RotatesKey`, Session 1 imports custom KVN 01; Session 2 passes
`originalKeyRef.Kvn` to replace that earlier custom set with KVN 02. It does not replace defaults
at that second step. `ResetAsync` skips an empty listing and otherwise finishes raw attempts and
reinitializes without factory-content verification. `BlockKeyAsync` can finish its attempt limit
without throwing. Track teardown is an additional independent 60-second cleanup request, not
verified restoration. No contradiction with the proposed R2 correction was found.

## Product delta and current decisions

Only these two additional source paths changed:

- `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp03Tests.cs`:
  the finally comment describes a reset request to attempt the default-key precondition needed by
  later tests/suites, without asserting restored factory contents.
- `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp03KeyLifecycleTests.cs`:
  the finally comment identifies the earlier custom set and states the same attempted precondition
  and lack of restored-content assertions.

Calls, assertions, imports, declarations, attributes, test names, runtime strings, key bytes and
token/cleanup logic remain unchanged. The patch tool also adds terminal newlines to these two files;
this is recorded separately from changed comment ranges. No engineering finding is added.

The current 233-path ledger has **39 edit / 194 retain** decisions. The original 37 edited paths
remain byte-exact to the reviewed batch; only the two formerly retained paths transition to edit.
CORE-SD-X008 (related X004) now includes the two cleanup-comment corrections; all other candidate
decisions and held engineering/policy/qualification facets retain their prior scope.

## Successor deliverables

| Record | Purpose |
| --- | --- |
| [product.diff](product.diff) | Full current 39-file product patch against the same HEAD, with explicit `c/` and `w/` prefixes |
| [R2-comments.diff](R2-comments.diff) | Focused two-file comment delta, separate from the unchanged first 37 paths |
| [path-decisions.json](path-decisions.json) | Current full 233-path before/after hashes and changed ranges; original IDs/context preserved |
| [R2-path-decisions.json](R2-path-decisions.json) | Two corrected paths, previous reviewed hashes, retain-to-edit reasons and current ranges |
| [candidate-decisions.md](candidate-decisions.md) | Revised X008/X004 documentation disposition and unchanged outside-scope holds |
| [citations.md](citations.md), [source-anchors.json](source-anchors.json) | R1 successor citation rows and exact retained-original anchors |
| [example-preservation.json](example-preservation.json) | All 107 prior bodies, ranges and body hashes checked unchanged; zero changed/new example bodies |
| [history-preservation.json](history-preservation.json) | Before/after preservation of old editor report/records and review bytes |
| [static-checks.json](static-checks.json), [record-revision.py](record-revision.py) | Scope-specific static checks and reproducible successor generation; original collector never run |
| [output-identities.json](output-identities.json) | Successor record hashes, excluding this manifest itself |

## Focused re-review handoff

Re-review only the two comment units and their unchanged enclosing finally/assertion/provisioning
context; X008's revised disposition; the R1 source passages, line-convention explanation and
selected-R-MAC/edition/actor wording; and the successor diff/current hashes/example preservation.
The older full review remains the basis for unchanged content. Independent re-review remains pending.

No builds, tests, formatter, cryptographic/native/device/connector operations, Git index/ref/commit/push
mutations or subagents ran. Source/token/structural XML/diff checks do not substitute for compiler,
cref, example execution, native-release or device/profile qualification. All 107 examples retain their
prior classifications and VALID-NOT-CHECKED states in this edit.

PIV physical deferrals and the future unready/unreviewed user Windows handoff remain intact. HSM's
retry-message retraction, HSM-IMP-008 and external peer/host/connector-profile uncertainty remain open.
OATH's fourteen historical resolutions and terminal-reset contract remain intact. All original
source-run IDs, failed attempts, historical hardware limits and shared referrals are preserved.
This revision does not authorize implementation, a broader re-audit or another framework. The
coordinator publishes Core Pass 1 and its deferred handoff after the remaining gates, then stops.
