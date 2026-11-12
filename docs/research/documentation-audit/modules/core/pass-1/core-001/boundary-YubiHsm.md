# YubiHSM Auth/Core dedicated boundary investigation

**Outcome:** all **160 assigned semantic units across 33 selected paths** received complete
source/assertion dispositions. All **53 candidate files / 6,325 lines** were read with caller, helper,
XML, example and enclosing cleanup context. Seven local candidate groups are ready for coordinator
triage. This completes the assigned static boundary scope, with explicit external qualifications;
product editing, independent review and runtime qualification remain separate gates.

Source remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Record serialization observed metadata HEAD
`da66f5d01ba594993974ddd26723e8c300c0a454`. Every original assigned file/range SHA-256 matches inventory;
all 53 candidate paths match pinned source after explicit UTF-8 BOM/CRLF normalization. Metadata HEAD
can advance without changing this source pin. Narrow dependency reads carry zero additional assigned
coverage and retain their family owners.

## Record map

| Record | Purpose / limit |
| --- | --- |
| [Analysis](boundaries/YubiHsm/analysis.md) | Actual factory/backend/APDU call graph; resource, state, reset, cancellation, version and crypto-actor tables; platform/late-delta map |
| [Candidates](boundaries/YubiHsm/candidates.md), [index](boundaries/YubiHsm/candidate-index.json) | CORE-HSM-X001–X007; separate documentation/runtime/decision/qualification facets and precise XML/guide/sample/test/migration reopening triggers |
| [Unit dispositions](boundaries/YubiHsm/unit-dispositions.json) | All original IDs, exact coordinates, source/range hashes, localized candidate/clean/context and pending review/validation |
| [File dispositions](boundaries/YubiHsm/file-dispositions.json) | All 53 complete candidate files, distinct authored observations and explicit unchanged decisions, including caller-only files with no new assigned unit |
| [Dependency dispositions](boundaries/YubiHsm/dependency-dispositions.json) | Exact read-only Core/shared caller/helper ranges and identities, primary owners and zero duplicate coverage credit |
| [Inherited outcomes](boundaries/YubiHsm/inherited-dispositions.json) | All 15 assigned provenance keys/four nonadditive groups, unchanged authoritative registry states, plus each original HSM-IMP-001–008 alias |
| [Sources](boundaries/YubiHsm/sources.md), [identities](boundaries/YubiHsm/source-identities.json) | Original revision/status/section/actor/strength/applicability, reused shared Python/family originals and bounded applet/profile gaps |
| [Oracles and proposals](boundaries/YubiHsm/oracles-and-proposals.md) | Complete test assertion limits, historical failed/successor probe distinctions, bounded unrun checks and a typed UNCOMPILED lifetime example |
| [Coverage](boundaries/YubiHsm/coverage-summary.json), [clean nominees](boundaries/YubiHsm/clean-nominees.json) | 79 read-clean, eight read-candidate, 73 read-context, zero unread; 17 meaningful unsampled nominees |
| [Static verification](boundaries/YubiHsm/verification.json), [separate receipt](boundaries/YubiHsm/verification-receipt.json), [manifest](boundaries/YubiHsm/record-manifest.json) | Fresh exact ID/raw-range/pin/Python/inherited-key and 30 dependency-range checks; links/fences and owned-record hashes, separate from independent content review and SDK execution |

Root/Core/YubiHSM/nested test instructions, Core README, campaign workflow/style/SDK house style,
run plan/dispatch, inherited registry/routing/seeds, completed A/B1–B6/C reports and original YubiHSM
handoff/findings/cryptogram/hardware records were consumed. Relevant shared test/CLI helper source was
read with its actual call sites. There is no module CLAUDE file under Cli.Shared. Frozen archives and
other workers' files were left unchanged.

## Principal conclusions

1. **Creation selects SmartCard once and awaits complete HSM setup.** The convenience extension opens
   one hidden connection and transfers ownership only after direct factory success. The direct factory
   borrows its connection, binds before SELECT, registers protocol disposal, reads applet version,
   configures optional Core SCP, rebinds backend and then publishes readiness. Failure cleanup preserves
   original error provenance; native release proof remains separate from a disposal task's outcome.
2. **Core channel state and HSM authorization have different actors.** Inherited `IsAuthenticated`
   records successful Core SCP setup. Management keys and credential passwords authorize their own
   applet operations, and returned HSM session keys still require an external HSM library to complete
   enrollment, peer verification, host authentication and protected command state. HSM CALCULATE does
   its key computation on the YubiKey; generic Core EC/CMAC helpers are not called for these HSM result
   keys. Core point/catalog agreement supplies no missing HSM profile validation.
3. **Reset preserves local policy but performs no fresh SCP handshake.** The current backend sends
   RESET and re-SELECT over the same protocol; the secure wrapper passes SELECT through unprotected
   while retaining the old graph. HSM clears no readiness/auth flag and retains no parameters for
   reauthentication. Its transaction-retention comment exceeds the actual no-transaction protocol path.
   X001 proposes truthful local state/reset guidance and a separately decided runtime policy. No actual
   SCP reset or post-reset protected operation was executed here.
4. **Cleanup reaches specific owners, rather than every pipeline copy.** TLV/request/padded-password
   and returned-key cleanup is explicit. `ZeroApduResponse` reaches the current final response array;
   the supported Core owner API is now `Clear()`. Earlier transport results, fragments, failed writer
   staging and a decrypted temporary remain independent. Successful key ownership transfers only after
   terminal resolution; a failing success callback disposes the produced keys. X002 applies C/B6's copy
   findings at these actual HSM callers without claiming a new failure to clear that final owner.
5. **Full public workflows extend beyond guarded exchanges.** Optional LIST, CALCULATE, parsing and
   callbacks have distinct phases; RESET and re-SELECT are separate exchanges. Core's admitted exchange
   receives token None and drains, while no session-wide operation lease covers subsequent callbacks or
   finally cleanup. X003 asks callers to await/serialize complete tasks and retain their input/service
   lifetimes. Whole-method exclusion/disposal admission remains a separate maintainer decision.
6. **Raw, effective and metadata versions stay separate.** HSM reads SELECT tag 79/three bytes, falls
   back to 5.4.3 for missing/wrong-width values, and applies an optional caller override only afterward.
   Malformed framing can throw. Major-zero acceptance is optimistic SDK policy. Reset discards its new
   version read and retains original policy. X004 preserves A's version provenance and the historical
   SDK/direct-Python 0.0.1, ykman 5.8.0 and Management 5.8 beta observations.

X005 retains the already documented **HSM-IMP-008** supplied-cryptogram 56-versus-48 parser uncertainty.
The unretained response's key/tail regions, production-firmware applicability and key/receipt result
contract remain open. Vendor Python slices three keys; vendor C suppresses an eight-byte symmetric
cryptogram and expects 48 bytes. Their different request/parser choices and generic Core agreement do
not resolve the applet profile. Omitted-cryptogram three-key equality is YubiKey-side calculation evidence,
separate from successful HSM/connector authentication and live interoperability qualification.

X006 retains `HSM-IMP-001/003/007` error/parser/profile decisions: GET_CHALLENGE general APDU errors,
short/unknown LIST record policy, partial context/point validation and externally trusted HSM material.
X007 retains diagnostic tool/input/output and test harness/oracle aliases `HSM-IMP-002/004/005/006`.
The current warnings and many assertion-accurate comments already carry the right qualifications.

The first final record checker caught an investigator misreading of the credential retry message.
The helper and asymmetric retry test both use `Invalid credential password`; there is no mismatch
finding. The [correction](boundaries/YubiHsm/corrections.md) and original failed checker output are
preserved. Historical passing-run evidence remains unchanged, and no SDK test ran here.

## Inherited status and qualifications

* **HSM-DEP-001:** actual SELECT/fallback/override/reset/gate mechanism is now traced. Documentation
  clarification and independent acceptance remain pending; older-firmware/NFC/other-OS qualification stays open.
* **HSM-DEP-002:** actual connection/prompt borrowing, admitted cancellation/drain, resolution-before-
  transfer and whole-workflow limits are traced. The historical 6982/Failed plus fresh-session recovery
  remains measured scope, without native interruption or a proven natural touch-timeout cause.
* **HSM-DEP-003:** actual source/TLV/request/formatter/transport/response/fragment/decrypt/key-result
  owners are traced. Final-owner cleanup is source-functional; lower copies and scoped exception cleanup
  retain their implementation/qualification debt. No end-to-end zeroing guarantee is accepted.
* **HSM-IMP-001–008:** all original IDs and deferred states remain. The ledger records each current
  source outcome; related Core/family aliases contribute zero additional independent bugs.
* The other 11 assigned provenance keys receive a shared-mechanism comparison and explicit return to
  their own OATH/SD/OTP/PGP/PIV boundary owners. HSM's local result does not close their reset/key/cache/
  workflow or physical questions. The 21 PGP documentation labels remain outside new bug counts.

Original applet manuals are descriptive, unnumbered vendor guidance. Reused SCP03 final 1.1.2 and
SCP11 public-review 1.3.0.13 keep their distinct status/sections/actors; code-cited SCP03 1.1.2.6,
final SCP11/YubiKey profile and exact ISO clauses retain B6's unavailable-source qualifications.
Prior initial research plus focused follow-up remains bounded: a versioned CALCULATE response profile
and raw 56-byte region identification are precise missing evidence, not an indefinite search queue.

## Next gates and preservation

The 17 clean nominees span substantial narrative/complete task blocks, actual backend/version methods,
exact byte/count tests, final-owner zeroing witnesses and prompt primary/secondary failure behavior.
They are nominations, not completed independent sampling. The coordinator removes any later edited
overlap and applies the stratified sampling requirement. Product edits and independent content/citation
review need fresh ownership. The complete typed ownership proposal remains `VALID-NOT-CHECKED`.

No product or central record edit, assertion/API change, build/test/formatter, crypto/native/device/
connector command, subagent, commit, push or Git index/ref change occurred. Only this report and
`boundaries/YubiHsm/*` were written; Python performed static joins/hash/source checks only. Any later
scratch/output belongs under the approved home-backed `BOUNDARY-YubiHsm` runtime directory; existing
archives/user files and exhausted temporary storage remain untouched.

Live HSM/connector, NFC, older firmware, other operating systems and Native AOT runtime lanes remain separately
unqualified. Serial 125's two 6A88 setup failures preceded EXTERNAL AUTHENTICATE/RESET; plain or synthetic
success supplies no missing secure-reset proof. The user-ready Windows CCID/HID delta is still pending;
the analysis supplies its precise reopening map without inspecting unfinished work. PIV physical
deferrals and the unexplained historical serial 9681620 failure remain unchanged.
