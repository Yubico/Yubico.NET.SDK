# SHARED narrative decisions

This is the fresh guide editor's documentation return at base `da66f5d01ba594993974ddd26723e8c300c0a454`,
product pin `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. It is not independent acceptance, compilation,
platform qualification, or publication. Only the released D facets are edited. Existing instruction
policy is preserved except for source-backed factual corrections (notably shared-array alias rationale).

## Applied task corrections

| Paths | Edit rationale / exact candidate families |
| --- | --- |
| `README.md`, `src/Core/README.md` | Discover once or own/share tracking; physical grouping and known-member bounds; borrowed/owned session lifetimes; useful metadata and SCP tasks. A C002/C003/C007/C008/C011/C015/C017/C021; B1 C001/C006/C007; B2 C001/C004; B5 C001–C006; B6 C001/C002/C003/C007/C009/C015; C C005/C008–C010/C015/C020. |
| `docs/usage/device-discovery.md`, `docs/architecture/{device-discovery-guarantees,physical-device-model,device-identity,event-driven-device-discovery}.md`, `src/Core/src/Devices/README.md` | Last-known scalar snapshots versus `ChallengeResponseTimeout` TLV alias; independent serial latch; serial-less-success caching and metadata PID gap; complementary-partials serial-to-slot risk; no forced convergence or OS-impossibility inference; current monitor replay/retention. A C002/C003/C007–C013/C015–C021 plus B1 enumeration/listener and B3 nullable native-context limits. Historical comparison/rig paragraphs remain verbatim with surrounding current-vs-historical qualification. |
| `docs/architecture/{raw-access-tiers,connection-ownership-and-contention}.md` | Applet/raw/connection task choices, actual completion/borrow/release, Windows compatibility limits, per-exchange versus whole-workflow lifetime, callbacks and transaction outcome, OTP deadlines, no-response CANCEL/original drain, APDU/SCP profile/copy/recovery limits. B1 C002/C005–C008/C012; B2 C002/C006; B3 C001/C007/C008; B4 C001/C004; B5 C001–C008; B6 C001/C003/C007/C015. |
| `docs/usage/user-interaction.md` | Synchronous Core reader versus async prompt; UTF-16/code-point/byte units; redirected immutable strings; confirmation owner lifetime; hex/empty/cancellation limitations; actual borrowed PIV method; non-reentrant callback disposal and exception provenance. C C001–C004/C008/C017; B5 C005/C006. Existing applet-specific heuristic/terminal descriptions retained pending final boundary cross-check. |
| `docs/usage/tlv-processing.md` | Static helper versus Tlv/list owners; view/copy/result/intermediate lifetimes; bounded definite parser; sensitive search caveat. C C005–C007. Existing full nested cleanup example is preserved byte-for-byte. |
| `docs/{MEMORY-MANAGEMENT,CRYPTO-APIS}.md`, root `CLAUDE.md` | Current span async-segment rules; shared struct-array references versus ambiguous ownership; borrowed ApduCommand and copied response; provider/writer/native/caller copies; finite parser/profile checks; AES-CBC pre-try allocation caveat. C C009–C018, B5 C005, B6 C002/C007. C019 has no D facet and remains held. |
| `docs/LOGGING.md` | Caller-owned factory before first use, retained static loggers, global temporary scopes, no central redactor, headers/lengths versus SELECT bytes and exception provenance. C C008; B6 C014; B4 C007 only for general carrier/provenance distinction. Actual toolchain argv persistence is outside scope. |
| `docs/linux-setup.md`, `docs/NATIVE-AOT.md`, `docs/v2-highlights.md` | Scoped group permissions with desktop/service distinction; libudev unversioned-loader prerequisite; package/source/producer/ABI versus installed runtime; historical execution versus current metadata; AOT static shim versus non-AOT shared deployment. B2 C001/C004; B3 C008; B4 C006/C008/C009. |
| `docs/v1-to-v2-comparison.md` | Current discovery/ownership/version explanations; historical parity classification qualified rather than promoted to runtime/conformance evidence. B5 C001, A C011 and B4 sharing/error provenance. Automation-owned migration files remain held. |
| `docs/SDK-HOUSE-STYLE.md` | Clarify intended method sketch and actual token argument; readiness/version and whole-workflow limits. B5 C001/C006. No architectural policy redesign. |
| `docs/TESTING.md`, `src/Core/tests/CLAUDE.md` | Current paths, translator behavior without unrun repeated-switch semantics, live identification before fixture allow-list, lazy binding, actual fake API names, API-correct and explicitly illustrative cleanup. A C013/C018/C019; B1 C011; B2 C005; B4 C010; B6 C013. |
| `src/Core/CLAUDE.md` | Align current shared contracts, actual static macOS callbacks, sizes, fake APIs, ApduException.SW, credential containment, cached alias, logging and workflow responsibilities. B1/B3/B4/B5/B6/C source-backed contributor corrections, not runtime fixes. |
| `docs/architecture/sdk-architecture-diagrams.md` | Correct prose teaching claims about type owners, evidence, and all-copy clearing. Mermaid bodies, rendered assets, manifest and map remain unchanged; their labels require the separate diagram owner. B4 C010 plus A/B6 dependent claims. |
| `docs/architecture/webauthn-extension-processing.md` | Preserve deliberate expired ARKG draft-09 target and structural/domain-separation evidence without injectivity/conformance/native-erase claims. C C011/C018; actual WebAuthn actor/serializer/result policy remains at its boundary. |

`path-dispositions.json` is the exact edit/retain/hold list, including every candidate-associated
in-scope document. `candidate-map.json` preserves all 103 local IDs / 102 FPT groups with exact original
unit references, facets, inherited associations, related guide actions and outside-owner referrals.
An edited path can still contain a held facet; no whole candidate is closed by that path's edit.

## Retains and holds

- `src/Core/PACKAGE_README.md`: retain accurate short Find/watch ownership and Management pointer; no
  added claim of fresh data, full grouping, or native qualification.
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/README.md`: retain explicit source-site,
  link-registry and diagnostic-sentinel bounds; it already separates native acceptance and SELECT/exception provenance.
- `docs/TESTING_PLATFORM_FINDINGS.md`: retain runner-migration direction; B4 C004 reaches this as dependency
  context, not a document containing the Windows access claim corrected elsewhere.
- Other inventory-listed active Markdown without a candidate is retained without claiming a new whole-file
  semantic audit. Candidate-associated external C#, applet guides, migration subtree, TOOLCHAIN, YAML,
  diagrams/assets, skills and historical/frozen records are held for their named owners. Exact paths,
  local IDs and original reopening triggers are in `outside-scope-followups.json`.
- Preserve metadata alias/serial-cache policy, FIPS mapping/width, form-factor normalization and unknown-field
  representation. FPX-01–04 and all other proposed experiments remain unrun; only the stronger claims
  depending on them are held. No metadata cleanup was simulated by disposing page lists or casting memory.
- Native completion, reference/callback drain, physical release, deployed producer/ABI, cross-process PIN
  state, provider erasure and exact standards/vendor profile stay separate. Requested UNPOWER is not an
  observed power cycle or PIN reset. Source compilation proves none of these dimensions.
- C019/FPT-101 remains P/E/Q/B with no D release; C020/FPT-102 is a B6/SHARED/applet contained-owner referral.
  No allocation/conversion/native-erasure fix is made. SCP03 release/review and SCP11 review/final/vendor
  gaps, synthetic signed errors, empty-command behavior and physical SCP-reset gaps remain open.

## Consumed late returns and final cross-check

The completed discovery, C and protocol editor returns were read only; their changes are independently
reviewed elsewhere and not copied as accepted C# work. A review R1/R2 is reflected in the no-expiry/
consumed-evidence convergence wording and implementation-only null-topology statement. The AES-CBC
allocation caveat is source-backed and does not infer a runtime allocation failure.

The subsequent completed transport and FIDO-boundary editor referrals are consumed read-only too.
They add the actual internal library/factory examples, discovery-first acquisition precedence,
transaction error reporting, Mac input normalization, and complete FIDO ceremony/callback lifetime
qualifications. The user's later A/C successor PASS report is attributed input, not this guide batch's
independent acceptance. Transport/protocol/FIDO editor claims remain scoped to their exact returned bytes.

Completed Management, CTAP2 (including its published follow-ups), WebAuthn, Security Domain, OATH and
YubiOTP returns inform applicability only. Shared wording remains subject to their final cross-check:
metadata/live-read provenance; setup versus first protected-command/app authorization; response aliases;
complete ceremonies/multi-command/reset workflows; actual prompt adoption. HSM/OpenPGP/PIV were active
at assignment, and no mutable partial return is treated as accepted. Finish those exact boundary inputs
before final guide acceptance. Do not poll, wait, or inspect unfinished Windows work.

The user-ready Windows CCID/HID delta needs a separately pinned source/evidence and affected-claim review.
PIV physical touch/hotplug and touch-dependent cancellation remain deferred. Historical serial 9681620
and serial 125's two 6A88 INITIALIZE UPDATE failures remain unexplained/unexecuted in their original
dimensions; no secure reset occurred in those setup failures. Finish publication and the deferred-finding
handoff, then stop; this return creates no Pass-2 project.
