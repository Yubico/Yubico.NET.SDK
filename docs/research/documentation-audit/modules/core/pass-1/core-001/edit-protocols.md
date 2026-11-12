# Core B5/B6 documentation editor return

The bounded C# documentation edit is ready for independent content/citation review and the coordinator's
existing preservation/compilation gates. **56 primary C# files changed; executable behavior and assertions
were left unchanged.** This is an editor return, not publication of all Core Pass 1 or runtime qualification.

## Identity and scope

| Identity | Exact value |
| --- | --- |
| Worktree | `/home/rupicola/Code/y/yubikit-core-docs-edit-protocols` |
| Branch | `yubikit-core-docs-edit-protocols` |
| Base and current HEAD | `da66f5d01ba594993974ddd26723e8c300c0a454` |
| Product source | `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef` |
| Authorized primary population | B5-session-framing and B6-wire-scp packet `.cs` paths beneath `src/Core/` |

The worktree was clean at entry. Writes are limited to those primary C# paths, this report, and new
`editing/protocols/*` records. No shared guide, applet/external source, API snapshot, configuration,
central audit ledger, historical record, archive, or primary-checkout file was edited. Patch application
added final newlines to edited files that previously lacked them; the full diff and raw hashes include this.

## Exact return records

| Record | Content |
| --- | --- |
| [Full primary diff](editing/protocols/full-product.diff) | Entire uncommitted product documentation patch, with full Git blob identities; no omitted hunks |
| [Path decisions and hashes](editing/protocols/path-decisions.json) | All **91 eligible primary C# paths**: **56 edit, 23 retain, 12 hold**; includes all **74 candidate-associated eligible paths**, not only changed files |
| [Candidate/proposal decisions](editing/protocols/candidate-decisions.json) | Original `CORE-B5-C001`–`C009`, `CORE-B6-C001`–`C016`, proposal `FPT-058`–`FPT-082`, inherited association keys, original indices and exact follow-up triggers |
| [Examples](editing/protocols/examples.json) | Complete original/current XML code bodies and body hashes, classification and explicit unrun gates; removed internal prototype classification |
| [Outside-owner handoffs](editing/protocols/outside-owner-handoffs.json) | Every directly indexed outside-owner path plus byte-preserved inherited source-run dispositions and applet referrals |
| [Citation decisions](editing/protocols/citations.md) | Exact retained editions, clauses, actors, normative strength and application limits |
| [Static receipt](editing/protocols/receipt.json) | Current hashes/counts, limited noncomment-line comparison, XML well-formedness and unrun gates |
| [Artifact hashes](editing/protocols/artifact-manifest.json) | Exact SHA-256 for the complete new editor records/report; manifest excludes itself |

Each path record contains exact base/current raw SHA-256 and Git blob hashes, the product-source hash,
normalization comparison, all original file unit IDs and the original units overlapping changed lines.
Candidate associations include retained context as well as direct anchors; they are not new findings or
an assertion that every associated unit changed. The candidate records preserve the source identities
without renumbering historical proposals or allocating accepted Core IDs.

`write-return.py` only serializes this bounded return and performs explicitly limited static checks. It
does not add an SDK validation framework, invoke a compiler, or modify source, the index, or refs.

## Corrections made

### Sessions and HID — FPT-058–066

- Distinguish application initialization from raw attachment and lazy channel/status initialization.
  Effective firmware can be detected, supplied, assumed, or unknown. Raw HID retains inherited 0.0.0;
  Security Domain assumes 5.3.0 without an override. Firmware compatibility queries remain available
  after disposal and major-zero acceptance remains permissive policy, not hardware evidence.
- Separate historical protocol-authentication setup from applet PIN/UV/management authority and channel
  liveness. Explain retained Security Domain parameters, shallow contained references, and the ignored
  Management/Security Domain presence service.
- State that guards/drains cover a logical exchange, not every public workflow, parser, cache update or
  detached task. Keep both synchronous and asynchronous disposal outside the operation's callbacks.
- Describe ordinary CTAPHID request/response grammar, no-response CANCEL, keep-alive-triggered signaling,
  original-response drain and indefinite pending-read possibility. Raced CBOR success is SDK policy,
  rather than a host mandate inferred from a standard's silence. Vendor scripts are not normative CBOR
  cancellation or physical re-plug evidence.
- Scope device-managed exclusion to in-process known-member admission. Explain incremental payload
  borrowing through actual task termination and the distinct returned/accumulation/incoming-report owners.
- Correct OTP status-report offsets, command-specific inbound length/CRC, outbound CRC coverage, local
  polling deadlines, the broad NEO refresh catch, and dummy-report cleanup versus persistent reset.
- Replace unsupported Rust/Python authority and historical gap narration with the current C# guard/test
  purpose. Explain that the held first FIDO send is lazy INIT and that fake/native seams and same-helper
  CRC assertions have narrower oracles than physical qualification or independent vectors.

### APDU and SCP — FPT-067–081

- Document always-emitted Le, short/extended zero encodings, complete-command ceilings versus 255-byte
  data chunks, fixed-zero 0x61 follow-ups, absent 0x6C retry and the exact non-0x9000 throwing predicate.
  The diagnostic status string remains unchanged; its unknown signed fallback is qualified rather than
  treated as a success classifier.
- Explain response.Clear's private copied owner, including status and aliases, separately from transport,
  fragment, writer and decrypted-temporary copies. A plain first-fragment failure can remain uncertain
  without local recovery notification. Plain SELECT after SCP retains the local graph and is not a
  reauthentication recipe.
- Identify pre-diversified SCP03 inputs, AES-128 derivation, unchanged DEK copy, minimum parsed response
  prefix and pre-return versus post-return cleanup. Remove the nonexistent SessionKeys/ScpState finalizer
  claim. Explain exact contained parameter owners, shared record clones and encryptor/session lifetime.
- Correct SCP11a/b versus c returned SD-key roles, fixed P-256/AES-128 input path, optional OCE-reference
  fallback, trusted SD-key selection, device chain verification and the draft-specific first protected
  command phase. Constructor admission is broader than verified profile support.
- Preserve implementation discrepancies rather than making them intended guarantees: empty-command
  encryption, pre-return counter mutation, instruction-only INS 82 exception, response copying and
  protected chaining. Stack declarations alone are not portable zero-initialization guarantees; no
  current garbage-IV/provider failure is claimed.
- Narrow test prose to exact full-value, length, equivalence, disposed-refusal, receipt, fixture-parsing
  and scripted response assertions. Preserve fixture strings while identifying the PEM root-serial
  mismatch and absent original receipt-capture identity. Skipped completed-task bodies remain plans.

FPT-082 / CORE-B6-C016 has no released D facet. `AnswerToReset.cs` is explicitly **held**, with its
content-equality/identity-hash/null-operator issue left for a separately selected decision and implementation.

## Complete example decisions

The complete 91-file C# scan finds one XML `<code>` body and no other XML code/example or fenced bodies:
`ApduCommand.cs`. Its original body comprises context-dependent PIN/SELECT fragments with undeclared
inputs and success-only cleanup. It is replaced with one complete composable `SendAndClearAsync` helper:
the caller supplies an open borrowed connection, applet-specific header and owned payload. The helper
scopes its raw session, requests a response even on non-0x9000 status, reads status before Clear, clears
the response in finally, and clears payload even if session creation or transmission fails. The caller
continues to own the connection and selected-applet/authorization prerequisites. This is not a complete
device-discovery/app-authentication recipe or evidence that arbitrary supplied APDUs are valid.

The obsolete commented-out provider experiment in `Scp11X963Kdf.cs` is removed as an incomplete internal
prototype, not executable code. Its complete original text remains in the full diff and base object.
All example binding/analyzer/consumer compilation is **NOT RUN**. Shared-guide and investigator-prepared
examples remain unchanged with their original classifications; they require the later guide owner's
complete-body treatment. No claimed example execution or device result is inferred from XML parsing.

## Holds and precise follow-ups

The machine candidate/path records are the exhaustive decision list. In particular:

1. **B5 command/recovery policy:** decide encoded CBOR 0x90 normalization, control-command admission,
   reserved assigned CID/control lengths and failed-tail recovery before stronger claims. If selected,
   use the existing bounded B5-E03/E05 proposals, not a new framework or unbounded stress run.
2. **B5 memory/lifetime policy:** choose incoming-report transfer/clearing ownership before adding actual
   retained-report oracles; never write-cast arbitrary borrowed read-only storage. Choose whole-workflow,
   callback self-disposal and reinitialization policy separately. Await real tasks, release held fake
   gates and drain in finally before any future harness teardown.
3. **OTP:** decide tight-poll cadence and NEO exception classification separately from byte compatibility.
   Local three-/one-/fourteen-second conditions do not bound native calls; vendor backoff is a comparator,
   not a timing mandate. No polling, NEO, touch or reset experiment ran here.
4. **APDU/SCP implementation:** independently review absent-Le/profile rules, below-4 reconfiguration,
   major-zero ForceShort behavior, first-fragment recovery, copy cleanup, counter mutation/notification,
   CMAC failure offset, INS 82 phase gating, pre-state-return SCP03 cleanup, optional DEK and KeyReference
   encoding/equality. Source observations stand independently of a later selected probe.
5. **SCP qualification:** select the target profile before an independent empty-command transcript,
   status-only error, split ciphertext/R-MAC or remaining-command vector. The local model's signed
   0x61xx/0x6A82 and padded empty command are not the inspected release's independent wire oracle.
   Final SCP11 1.4, exact vendor-profile clauses and code-cited SCP03 1.1.2.6 clauses remain gaps.
6. **Test/fixture work:** later authorized owners can fix held gate/drain cleanup, fixture association,
   disposed key/certificate owners, nonproduction fake formatting and names. Existing assertions,
   runtime strings, literals, directives and test identifiers remain frozen in this edit.

### Shared-guide and applet owners

Do not silently integrate C# corrections while leaving the mapped guide claims as verified current truth.
The next explicit guide owner must reconcile `src/Core/README.md`, `src/Core/CLAUDE.md`, root `CLAUDE.md`,
`src/Core/tests/CLAUDE.md`, `docs/SDK-HOUSE-STYLE.md`, raw-access architecture, user-interaction/memory
guidance and migration/comparison/highlights sections. Specific changes include extension-as-standard
owned entry versus public direct borrowing factories, ignored/retained options, exchange versus workflow
lifetime, deferred cancellation, report/response owners, size units and useful complete SCP tasks. Fix
stale fake API names (`EnqueueResponse`/`TransmittedCommands`), `ApduException.SW` and source-buffer finally
examples in those guides. Dated decisions/changelogs retain historical status and bytes.

The outside-owner JSON retains exact applet `.cs` and shared path referrals from both candidate indices
and inherited source-run keys. Management owns detected/effective metadata and parameter examples;
Security Domain owns retained reset parameters, omitted reset config/version, trust/authority and chain
tasks. CTAP2/WebAuthn own ceremony status/result/cancellation semantics; OATH/OpenPGP/PIV/YubiOTP/HSM own
their full operation/authorization/cache/reset and output-buffer scopes. `IOtpHidConnection` and platform
producer/native release documentation stay with their assigned transport owners. None was edited here.

Serial **1256A88** remains two setup failures returning **0x6A88 before EXTERNAL AUTHENTICATE and RESET**;
plain reset success supplies no SCP-reset proof. Historical Linux serial **9681620** remains unexplained.
Native producer/runtime/deployed binary, physical release/reopen, PIV touch/PIN/hotplug and cancellation
deferrals remain qualified. The new Windows concurrency handoff is future work after explicit readiness;
this editor inspected no unfinished Windows branch or worktree.

## Verification and stopping boundary

The fresh static receipt confirms all changed product paths are eligible, all 91 paths have hashes and
decisions, all 56 changed files match a deliberately limited noncomment-line comparison, and extracted
XML comment blocks are well formed. `git diff --check` reports no whitespace errors. These checks are
not a C# lexer, compiler, cref binder, public API checker, test result or security/interoperability proof.

No build, test, formatter, cryptographic computation, provider/native/device execution, Git index/ref
write, commit, push or subagent ran. Independent full-unit content/citation review and the existing
coordinator compilation/token/API checks remain pending. The coordinator can integrate this bounded
editor return, finish and publish Core Pass 1 with the explicit deferred handoff, then stop as requested;
this return starts no Pass 2 or implementation project.
