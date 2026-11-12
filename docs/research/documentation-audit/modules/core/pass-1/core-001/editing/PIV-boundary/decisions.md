# PIV boundary edit decisions

This separate batch follows the coordinator's completed PIV release. It uses product source
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef` in the isolated worktree at
`da66f5d01ba594993974ddd26723e8c300c0a454`. Primary `boundary-Piv.md` and `boundaries/Piv/*`
are read-only final inputs; current primary Core product bytes are not a source baseline.
The OpenPGP patch and records delivered earlier remain separate and unchanged.

| Candidate | Documentation applied | Retain / hold / handoff |
| --- | --- | --- |
| CORE-PIV-X001 | Direct/hidden connection ownership, binding/readiness/failure cleanup and local card-state explanation in factories/interface/README; correct disposal-based PIN gotcha and historical Today-signs test prose. | PinPolicy.Once already correctly follows card state. Strict Exclusive/explicit sharing and requested UNPOWER do not prove physical power/PIN isolation. PIVCORE-001/#22/YESDK-1630 and physical/cross-process qualification remain separate. |
| CORE-PIV-X002 | Complete-task serialization and input/key/prompt lifetime, two-send authentication, callback/unpadding outside guard, admitted cancellation and reset's retained protocol/no handshake. PivTool guidance preserves task ownership despite outer waits. | Existing authentication/PIN-only/crypto helper comments and workflow tests remain accurate. No workflow lease, operation lock, transaction, rollback, callback policy or native deadline is added. PIVCORE-002/B5-C006 remains an engineering/policy handoff. |
| CORE-PIV-X003 | Internal X25519 return is raw 32-byte result, not a point. Data-only assertion scope is separated from full response-owner Clear, and README names separate response/TLV/transport/fragment/SCP/provider owners. | Existing mutable outputs, PRINTED ownership-taking decoder, import predecessor clearing and public metadata classification remain accurate. PIVIMP-007 output admission/provider/decompression work, PIVIMP-009/010 and underlying B6/C owners remain open. |
| CORE-PIV-X004 | Factory/version provenance, major-zero optimistic policy rather than physical 5.8 floor, detected reset fallback independent of override; attestation comment describes appended Le without guessed ISO case. | Existing SDK gates, AES 5.4.0/released 5.4.2 and serial 5.0.0/Python 5.0.3 distinctions are retained. Profile/compliance and deployed native/SCP/ISO qualification stay held with their owners. No catalog capability broadening. |
| CORE-PIV-X005 | GET-exchange concurrency rather than whole-session exclusion; pending-read assertion-failure gap; hotplug poll/teardown/reopen actual oracle and no native deadline; reset membership rather than complete sequence. | Runtime assertions, method names, timeout values and release expectations remain unchanged. Other truthful crypto/Always/X25519/attestation/cross-key comments remain retained. Harness cleanup, fixture floors and stronger oracles stay PIVIMP-008. |
| CORE-PIV-X006 | Add only complete task/prompt/input lifetime context to the existing sample limitations. | Preserve source-supported tool signing, read-only plaintext, result certificate, digit-filter/profile, callback and error-mapping caveats. PIVIMP-005 remains confirmed/unfixed; no tool behavior, helper, production credential policy or experimental API is introduced. |

## Complete path and example accounting

`path-decisions.json` gives all 121 original paths an explicit edit/retain decision, candidate
associations and a substantive reason. Its retained reasons reuse the completed investigator's
file observations rather than claiming 121 new full-file source investigations. All changed
files were read completely before editing. `changed-files.json` gives exact before/after hashes
and ranges; source identity is pinned Git, with BOM/CRLF normalization explicit for local retained
checkout representations. Configuration/API snapshots stay ineligible and unchanged.

`outside-owner-handoffs.json` retains all 31 exact Core/shared dependency paths with original
primary owner, local candidates and precise reason/trigger. Core/shared/other modules were not
edited. B1/B4 retain connection sharing/native release and producer/header scope; B5 retains
workflow/session admission; B6 retains Le/response producers/SCP contained owners/phases; C retains
reader/TLV/private/provider mechanisms; SHARED owns its assigned narratives. Migration and
Tests.Shared files require their coordinator-assigned owners. No duplicate Core finding is added.

`example-inventory.json` inventories every fenced Markdown and XML code body on the 121 paths,
including composable shared-context fragments, complete task bodies/methods, protocol/structure
fragments and commands. Every body is retained exactly with before/after ranges and hashes. No
new helper or C# example body is added, and no new compilation/runtime proof is claimed. Historical
archive examples remain excluded and untouched, with their original qualified evidence.

## Preserved finding states

All eleven routed keys retain source-run identity, including `piv-001:PIVCORE-001..003`
(dispatch's PIV-CORE spellings do not renumber them). All `piv-001:PIVIMP-001..010` remain at
their original resolved/open/confirmed/qualification scopes. In particular PIVIMP-005 remains
confirmed/unfixed: historical tool RSA failed original-message verification and Ed25519 verified
SHA256(message), while direct SDK controls passed. Updated docs do not repair that tool framing.

F9/private/attestation material and deliberately unread protected PRINTED restoration scope stay
protected; no device calls ran. Serial 125's two 6A88 results were INITIALIZE UPDATE failures before
EXTERNAL AUTHENTICATE or RESET. Plain PIV results supply no protected PIV acceptance. Physical
PIN/power/cross-process/touch/hotplug/cancellation-recovery lanes remain deferred/qualified. The
user's unready Windows concurrency delta requires its separate future handoff, pin and review.
