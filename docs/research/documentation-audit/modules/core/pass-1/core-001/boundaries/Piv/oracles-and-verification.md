# Oracles and meaningful unexecuted verification

## Evidence read, with no new execution credit

The packet read all assigned test methods with the real local fakes, raw response constructors,
recording connections, reflection seams and cleanup. `RecordingSmartCardConnection` copies
outgoing APDUs, returns queued raw arrays and increments both disposal paths. Its copies are
test-owned storage; inspecting those copies cannot prove product send-buffer erasure.
CryptoCapturingBackend and DataObjectProtocol's capturing backend instead retain the actual
encoded command/`ApduResponse` owner arrays. Their assertions reach the intended product owner.

| Oracle | Useful evidence from actual assertions | Limit |
| --- | --- | --- |
| PivAuditReproTests | Full expected short APDU for 10/64-byte Ed25519; short EC left padding; oversized RSA no-command refusal | Request encoding, not device signature verification |
| PinOnlyWorkflowTests | Exact command sequences; failure stopping; type-appropriate default bytes and same static view preserved; live encoded views cleared | Fake accepts submitted witness without validating it; reflected management auth bypasses actual setup |
| PivPinOnlyModeSessionTests | Mixed recovery fake decrypts real witness, rejects stale candidate at second stage, restores earlier protected candidate; exact attempts; cancellation/unrelated error skip restoration; actual secret views clear | Managed synthetic device semantics; no OS/power/real-key result |
| PrivateKeyOperationsTests | Successful result same-array transfer; actual command/response zeroing; direct parsed-result zeroing on resolution failure | Transport staging/chained/SCP/provider copies are outside observer scope |
| TouchNotificationTests | Command count before/after request/resolution; same context; None terminal token; intent after metadata; no duplicate metadata; pre-wire cancellation; malformed-success Failed; primary error retained | No physical touch, native timeout or post-dispatch cancellation test |
| CatalogCapabilityBoundaryTests | Catalog-backed unsupported keys reject generation/import/agreement before encoding/clearing/transmission | Protocol request subset, not provider or hardware curve support |
| SessionConsistencyTests | Required metadata/gates; narrow fallback; cancellation and malformed metadata propagate; override does not redefine reset key default | Fake fixtures establish policy/source behavior only |
| PIV integration crypto/import/move/overwrite | Host RSA/ECDSA verification and selected raw ECDH equality bind exact generated/imported key/message in those methods | Ed25519 differential tests and X25519 generation-only method remain weaker; no inherited suite-wide crypto proof |
| Integration PIN-only round trip | Random key enable -> owned connection close -> fresh recover -> disable -> default auth, plus independent cleanup scope | Historical recorded execution is OS/firmware-specific; token budget gives no native deadline |
| Contention tests | Known-member acquisition refusal and sequential borrowed PIV/Management/PIV serial reads; keygen overlap and cross-key completion checks | Nonempty signatures do not prove key binding; count/timing assertions do not prove complete topology census or every scheduling interleaving |
| AuditV2 PIN isolation | Sequential same-process fresh handles; baseline refusal; desired rejection after verifier disposal | No simultaneous shared clients, separate process, observed power or all-OS qualification; stale Today wording reopened |
| Hotplug test | Polls serial until failure; disposes connection; checks later exception differs from ConnectionInUse | Token is not native-call bound; any failure can satisfy poll; unproven release can correctly quarantine; physical lane deferred |
| Certificate/attestation | Certificate storage compares thumbprint/fields; selected attestation tests check issuer/validity/serial; metadata SPKI test compares exact public encoding | Storage is not slot binding; issuer strings are not chain trust. Stronger historical supplemental proof remains a separate oracle |

## Coordinator-only later checks

No command below was executed or authorized by this source assignment. Use the existing toolchain
and validation foundation, not a new experiment/helper project. Read the actual discovered project
selection and final RESULT block, require nonzero executed counts, and preserve warnings/failures.
Use the current build/test skills before later SDK runs. Native-capable unit files remain native
work even when the project name says UnitTests.

1. **Compile real consumer scopes.** Reuse the retained PIV example groups and actual PivTool project
   with current exact references/analyzers. Validate a directly borrowed SmartCard connection with
   sequential PIV/Management sessions, an owned convenience PIV session, and caller-owned SCP
   parameters/prompt/private-key/certificate scopes. Track current-source versus historical-output
   identity; compilation grants no device/profile execution.
2. **Run existing managed behavioral classes.** Select the real PIV unit project and complete
   `PivSessionTests`, `SessionConsistencyTests`, `TouchNotificationTests`, `PinOnlyWorkflowTests`,
   `PivPinOnlyModeSessionTests`, `PrivateKeyOperationsTests`, `DataObjectProtocolTests`, Bio and
   catalog-boundary classes. Select Core `SmartCardSharingTests`, `PcscProtocolConcurrencyTests`,
   session/connection ownership and response/TLV ownership classes separately. Keep selected test
   host and actual native participation explicit.
3. **Propose a whole-workflow lifetime regression only in authorized implementation scope.** Hold
   the real PIV request callback before the crypto APDU, or resolution callback after parsed success;
   start session disposal and record admission/terminal/guard-drain/public-task order separately.
   A second vector holds the gap between management witness and submitted challenge or between
   default-key replacement and object deletion. Assert no invented rollback or universal operation
   lease. Release all gates and await actual tasks in finally, even after assertion failure.
4. **Strengthen existing test oracles separately.** Retain/read responsive tasks after WaitAsync
   timeout, drain readTask on refusal-assertion failure, and preserve primary plus cleanup failures.
   Fix metadata-dependent fixture floors and dispose actual exported private buffers/certificates.
   Independent RSA/Ed25519 original-message checks must continue to distinguish PivTool from direct
   SDK; successful helper result alone cannot close PIVIMP-005.
5. **Qualify protected PIV with exact setup phases later.** Identify authorized device, transport,
   OS/resource manager, firmware/raw/effective versions, key reference, SCP variant and native artifact.
   Record INITIALIZE UPDATE, EXTERNAL/INTERNAL AUTHENTICATE, protected PIV operation, reset/post-reset
   behavior, actual teardown and fresh handshake as separate observations. Serial 125's two 6A88
   results reached neither EXTERNAL AUTHENTICATE nor RESET. Do not repair/reset SD keys to force a pass.
6. **Keep physical and platform lanes deferred.** Touch, hotplug and touch-dependent cancellation/
   recovery require the user's separate release. A controlled two-process/handle PIN-state lane needs
   explicit share mode, selected-application transitions, observed state, checked release and fresh
   independent state inspection. Preserve F9/private material and compare against a fresh baseline;
   historical serial 103/125 captures are not current state evidence. Review the user's Windows
   CCID/HID source/evidence delta only after readiness, under its own pin and review scope.

## Completion gates

The local record checks verify ID sets, raw source/range identities, pin equality with explicit
BOM/newline treatment, authored file-note completeness, candidate associations and linked source
hashes. They are separate from semantic investigator judgment, independent content/citation review,
stratified clean sampling and any SDK/native/device execution. Nothing here changes the central
coverage, inherited registry or editing release. Finish Core Pass 1 publication and the deferred
finding handoff through the coordinator, then stop; no separate planning/implementation project starts.
