# PIV/Core dedicated source-boundary investigation

**Outcome:** all **513 assigned units across 93 selected paths**, and all **121 complete
candidate files / 22,213 lines**, received source/assertion dispositions. Six local proposals
are ready for coordinator triage. The bounded source investigation is complete; product editing,
independent content/citation review, clean sampling, compilation and hardware qualification remain
separate gates. No accepted Core finding IDs or duplicate bug counts were allocated.

Source remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; metadata checkpoint is
`da66f5d01ba594993974ddd26723e8c300c0a454`. Every assigned raw file/range identity matches its
inventory. All 121 candidate files match pinned source with BOM/CRLF treatment explicit. Metadata
publication does not rebase implementation evidence.

The coordinator extended live editing-release metadata during this packet. The first observed hash
and failed verification are preserved; the current record was reread and captured separately.
Intermediate metadata bytes were not retained at observation time and are explicitly unavailable;
this qualification affects no pinned product/range identity or frozen evidence archive.

## Record map

| Record | Purpose / limit |
| --- | --- |
| [Analysis](boundaries/Piv/analysis.md) | Actual factory/transport/SCP call graph and creation, ownership, authentication, failure/cancellation, memory, disposal/reset/reopen and version tables |
| [Candidates](boundaries/Piv/candidates.md), [index](boundaries/Piv/candidate-index.json), [anchors](boundaries/Piv/anchors.json) | CORE-PIV-X001–X006; separate documentation/implementation/policy/qualification facets and precise XML/guide/sample/comment reopening |
| [Unit dispositions](boundaries/Piv/unit-dispositions.json) | Every original unit ID, exact source/range/coordinates, actual file context, localized proposal or retained clean/context disposition |
| [File dispositions](boundaries/Piv/file-dispositions.json), [authored file notes](boundaries/Piv/file-notes.tsv) | All 121 candidate paths explicitly unchanged, including candidate-associated retained passages with useful rationale |
| [Inherited outcomes](boundaries/Piv/inherited-outcomes.md), [exact routed rows](boundaries/Piv/inherited-dispositions.json) | All 11 routed source-run keys in three nonadditive groups; all PIVIMP-001–010 retained |
| [Sources](boundaries/Piv/sources.md), [manifest](boundaries/Piv/source-manifest.json) | Exact original revision/status/section/actor/strength/application, reused source/Python evidence and native/profile/ISO gaps |
| [Oracles and unexecuted checks](boundaries/Piv/oracles-and-verification.md) | Actual assertion/helper limits, bounded later managed/compiler/native/device recommendations and separate gates |
| [Coverage](boundaries/Piv/coverage-summary.json), [clean nominees](boundaries/Piv/clean-nominees.json) | 384 read-clean, 46 localized read-candidate, 83 read-context; zero unread; 12 substantive unsampled nominees |
| [Dependency dispositions](boundaries/Piv/dependency-dispositions.json) | Unchanged Core/shared dependencies, their original owners and precise reopening conditions; no duplicate coverage credit |
| [Static verification](boundaries/Piv/verification.json) | Fresh ID/range/pin/record/link checks; no SDK, native or device execution credit |

Root/Core/PIV/nested Core test instructions, Tests.Shared instructions, campaign plan/workflow/style/
dispatch/editing release, SDK house style, inherited registry/routing/seeds, all eight completed Core
family reports and prior PIV publication/findings/source handoff were read. Every assigned PIV unit was
read with actual caller/helper/assertion/cleanup context. Shared/Core source reads retain their family
owners; other applet assignments were not duplicated. No subagent was spawned.

## Principal conclusions

1. **PIV selects SmartCard once and publishes readiness after complete setup.** The convenience
   extension opens a hidden connection, calls the direct borrowing factory, then transfers ownership.
   Core binds before SELECT, registers protocol disposal, PIV reads FD, applies effective Configure/SCP,
   rebinds the backend and resolves management type before readiness. Failure cleanup preserves the
   initializer exception; native release proof retains its separate registry/diagnostic outcome.
2. **Protocol authentication, management authentication and PIN state have different owners.**
   SCP success sets inherited `IsAuthenticated`; it grants no PIV PIN or management authorization.
   Management authentication is a local observation; card-controlled PIN/administration state can
   change through application selection or external actions. Direct-session disposal closes no borrowed
   connection. Strict Exclusive/explicit sharing and requested UNPOWER are source/assertion-backed;
   actual physical power, PIN isolation and separate-process behavior remain qualified.
3. **Per-exchange drain leaves whole-workflow boundaries visible.** Authentication, PIN-only
   enable/recover/disable/reset and metadata-plus-crypto span exchanges. PIV request/resolution callbacks
   sit outside the APDU exchange guard; resolution and local RSA unpadding can outlive that guard.
   Await and serialize complete workflows before disposing sessions, prompts or borrowed inputs.
   Whole-operation lifetime/overlap policy remains the inherited B5-C006/PIVCORE-002 handoff.
4. **Owned response clearing and result transfer are confirmed at actual PIV callers.** Crypto,
   authentication, raw GET, PRINTED, Bio and private import use distinct owned working/result buffers.
   Actual-array observers check response/command clearing; parsed output transfers to caller-owned
   mutable memory only after success resolution. Core transport/chained/SCP-decrypted/provider copies
   remain separate B6/C owners. Generic public metadata/certificate copies are not automatically secret.
5. **Cancellation and completion remain operation-specific.** Pre-admission cancel prevents an APDU;
   admitted constituent traffic uses None and can return success after token cancellation. The existing
   callback-cancel test is pre-wire. Malformed success resolves Failed; parsed success resolves Completed
   before local RSA unpadding, whose failure can follow. No PIV TimedOut/native deadline is established.
6. **Version provenance and old test prose need local corrections.** Raw PIV FD, Management metadata,
   effective override and detected-version reset fallback remain distinct. Major-zero fixtures implement
   optimistic compatibility, rather than proving physical firmware >=5.8. The no-data attestation comment
   should describe actual appended Le without guessing ISO clauses. Hotplug token/time-bound and
   unconditional-release narration also needs current source/actual-oracle qualifications.

X001–X006 localize these concerns and existing tool/test work. B1/B4/B5/B6/C mechanisms are mapped at
actual PIV call sites without duplicate bug credit. In particular, PIV reaches B4's common PC/SC native
producer/header boundary and B6's empty-command/chaining/SCP phases; it chooses no HID applet backend.
The user's new Windows CCID/HID concurrency work remains a separate future readiness/delta handoff,
outside pinned B4 and this packet.

## Inherited outcomes and qualification

- Preserve original `piv-001:PIVCORE-001..003` identities (also called PIV-CORE-001..003 in dispatch),
  `audit-v2-20261022-20261031:#22` / YESDK-1630, all other 11 routed keys and all PIVIMP-001–010.
  Shared applet-specific KDF/CRC/CALCULATE/reset semantics retain their assigned boundary owners.
- **PIVIMP-005 remains confirmed/unfixed.** Actual tool RSA failed original-message verification;
  Ed25519 verified SHA256(message); direct SDK controls passed. This is an applet/tool framing finding,
  not a generic Core defect. No new cryptographic probe was run.
- Earlier narrow source fixes remain resolved: retry fallback/count uncertainty, management-state
  invalidation/SET distinctions, detected default independent of overrides, active-key authentication,
  PIN-only deletion/restoration order, raw unknown metadata, strict wire subsets, mutable output/actual
  response-owner cleanup and private encoding cleanup, Ed25519 passthrough/EC padding/RSA oversize handling.
- Historical PIV 86/89 canonical methods and six supplemental outcomes retain their original scope.
  Serial 103/125 baselines are dated evidence, not current device state. F9/private material and
  deliberately unread protected PRINTED restoration scope remain preserved.
- Serial 125 returned 6A88 for both FF and 00 INITIALIZE UPDATE, before EXTERNAL AUTHENTICATE/RESET.
  Plain PIV success supplies no protected PIV acceptance. Retain B6's exact final/public-review SCP and
  unavailable ISO/profile clauses, B4's installed native-producer/header gap and provider erasure limits.
- **PIV physical touch, hotplug and touch-dependent cancellation/recovery are DEFERRED**, including
  the prerequisite printer in the historical physical lane. No devices, resets, PIN operations, native
  calls, crypto probes, builds/tests or formatter ran here. No source evidence establishes hardware
  conformance, broad FIPS/NFC/Bio support, cross-process isolation or verified power-off.

## Next gates and stopping point

The 12 clean nominees include exact APDU bytes, unsupported-catalog refusal before encoding,
actual response-owner clearing, mixed default-key object ordering, primary-error preservation,
detected-versus-effective reset policy, complete consumer cleanup tasks and public-only host-key
extraction. They are nominations, not independent stratified sampling; remove later edited overlaps.

Use the existing validation foundation for complete PIV examples/PivTool and meaningful existing
managed classes after separate assignment. Hold actual public workflow/callback gaps only in an
authorized future regression scope, release/drain gates in finally, and distinguish wait timeout from
original task completion. Any later device lane needs a fresh identity/state baseline and explicit
setup/operation/terminal/release/reopen observations while preserving F9/private material.

Only `boundaries/Piv/*` and this report were written. One-off Python assembled/checks records and
hashes; read-only Git identified pinned objects. No new helper/tooling project, product/central ledger
edit, Git index/ref change, commit or push occurred. Scratch is home-backed at
`/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/Piv/`; TMPDIR/TMP/TEMP were equal for
record generation. Existing archives and user/other-worker files were left untouched.

Return to the coordinator for editing and fresh independent review, then Core Pass-1 validation,
publication and deferred-finding handoff. After that agreed closeout, stop; no separate implementation
or planning project is started by this return.
