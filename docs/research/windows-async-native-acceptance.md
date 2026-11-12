# Windows async-boundary acceptance checkpoint

This record covers **`bdac2f4f4224fa0ccf92006971af8659736aa2b1`**, the Windows
implementation in draft PR #771. The cross-platform consistency follow-up has a
different source identity and must not inherit these results as its own execution.

## Native Windows and device results — 2026-11-12

Commands were issued from Linux through the `windows-sdk` runner, with explicit
source selection, one original lease per job, and serial **103** as the dedicated
device. Reports for the following jobs are authoritative with native exit **0**.

| Job | Selection | Executed / failed / skipped / warnings |
|---|---|---|
| `d49f259548214758af46d5385cc3422a` | Core unit suite in Windows | 2210 / 0 / 3 / 0 |
| `a6dda0ddfa0040cfbc833128881fe48c` | CCID, FIDO HID, OTP HID sanity | 3 / 0 / 0 / 0 |
| `39c1b4a57e514e0ea819fb241f5c559a` | Core ownership, PCSC lifetime, discovery | 28 / 0 / 0 / 0 |
| `65598364077449319525c1b596c74abe` | CTAP2 public factories/GetInfo, HID and CCID | 4 / 0 / 0 / 0 |
| `8f8fcffc534f4c2084e5c7d6f7b74fac` | OTP HID HMAC and metadata | 5 / 0 / 0 / 0 |
| `db7cca11e1ca4d91968ce664e41217ec` | HID overlapping calls and recovery | 2 / 0 / 0 / 0 |
| `c4bf3c308b694a53874fb2af678fb273` | Scoped discovery and live/cached metadata | 8 / 0 / 0 / 0 |
| `f09e5e034f9249b89d086d45cc844358` | Borrowed FIDO factory and unsupported reset | 2 / 0 / 0 / 0 |
| `d47781eef35643f39f895ee1fbf0f183` | Borrowed OTP HID slot-two configuration | 4 / 0 / 0 / 0 |
| `95af17d0ea7541dd864d4e4ec359e7cf` | Slot-two protection regression | 1 / 0 / 0 / 0 |
| `ff9910bba140416bbea4eb623e084d45` | Two slow monitor cases, without smoke | 2 / 0 / 0 / 0 |

The **59 device cases** ran elevated in the active Windows console: session ID 1
equaled active-console ID 1, WTS protocol 0, active/unlocked, direct USB. Each job
returned the key to Linux. Explicit SmartCard CTAP2 GetInfo passed without an
unsupported-device skip. The allowed OTP fixtures programmed and deleted slot two.
The selected suites avoided touch, unplug, PIN-normalizing and credential ceremonies.

The three unit skips are two SCP11 device methods and an isolated child-body method.
Eight listener methods reported passing, but their existing assertions/early-return
paths and retained normal-verbosity output do not establish that both native
listeners reached Started. The slow metadata-retention case can complete on initial
metadata; its pass does not prove it waited for a later interval scan.

## Unsealed JIT runtime diagnostic

Exec job **`79c73f7ed683458a97a70a25eed850c4`** freshly published the verifier as
self-contained win-x64 with `PublishAot=false` and isolated intermediates, then ran
`--native jit` under the same lease. Publication and verifier exits were zero.
All **10 child scenarios, 44 scenario checks and 21 coordinator checks** passed;
the stress scenario includes 100 owned-pipe close/reopen lifetimes. These are native
scenario/check counts, not xUnit tests.

All 11 collected evidence files matched the independent required check-name sets;
their published-file manifests matched all 207 collected files. The source remained
`bdac2f4f4`, snapshot `52ecb539bbb03151a1e8aa66eaabf1cfe3633f1ae9e1cb3b3b2e2189d66b7c8a`.
This is **unsealed JIT native evidence over synthetic pipes**. It does not replace
the combined publication-seal/JIT/AOT gate or attest physical HID-driver races.
Native AOT remains blocked by the unavailable C++ linker and was not retried.
See the [verifier contract and limitations](../../verification/WindowsHidRouteVerification/EVIDENCE.md).

## Source-bound redirected FIDO diagnostic

Diagnostic **`cc7c78c4790b4e1692f134a4c287826e`**, under original RDP owner
`87a3dcece70b45269a407ded62965dd6`, completed two GetInfo operations with native
exit zero. The actual actor was standard-token, active RDP session 1/protocol 2,
with explicit exclusive SmartCard access to serial 103 and no HID fallback.
Product-source/build-receipt/runtime hashes were checked independently of the
diagnostic tooling's source identity. There were no PIN/token/ceremony/touch requests.
This is not Windows WebAuthn API-channel or attended assertion evidence.

## Evidence custody and remaining acceptance

Runner records are retained under `~/.local/share/windows-sdk/jobs/<job>/` on the
test controller; the complete standalone JIT run is retained under
`~/.local/share/yubikit-work-evidence/pr771-console-003/`.
Exact commands, diagnostic receipts and named-window handoffs are in the controller's
append-only `windows-sdk/backplane/yubikit-windows-async.md`. These machine-local
records are not CI-hosted artifacts or signed attestations.

The first source-transfer attempt (`25fad7c4502b436abcebef6ed4e253b6`) failed before
native execution on a historical public-fixture policy restriction. Its evidence
remains failed/unconfirmed; maintenance reconciled its owner and corrected the
reviewed transfer policy before the fresh successful runs. It was not an SDK defect.

Outstanding evidence includes AOT/sealed publication, raw immediate-success versus
pending submission attribution, native allocator-failure reproduction, physical
removal/touch, two-key behavior and direct synchronous-facade hardware coverage.
**The macOS consistency changes require macOS native/hardware testing before merge.**
No result here advances the whole-program ISA acceptance count by itself.

On 2026-11-12 the user authorized finishing the Linux-verifiable work and PR review.
macOS native/hardware execution is planned for the Mac later; native AOT/linker
verification is planned for a separately prepared VM. These are future execution
plans, not completed gates or authorization to change the current shared guest.
The [macOS handoff](adapter-consistency-macos-handoff.md) lists source-derived
managed, production-route JIT and bounded hardware selections for that later run.
