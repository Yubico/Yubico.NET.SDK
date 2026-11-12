# Independent review — Linux listener sampling correction

## Decision

**PASS — the exact two-file additive documentation patch.** No mandatory correction remains.

Accepted `source.patch` SHA-256:
`a994f3081f0bc7439221d2a6073b5bc4f0dec9149d9dfa4c3e254799cbab1049` (3,124 bytes).
Apply after the accepted transport revision-2 full patch,
`109bd7833b71d69f9b25b2a021ea27e460334535fe28e8fcdc8cd6bd160404de`.
This acceptance concerns only the sampling correction, not a replacement full patch or a new
whole-transport audit. Prior content acceptance and all qualification limits remain intact.

## Source and assertion review

### SAMPLE-B2-001 — attempted shutdown wake

Accepted complete interface/type explanation and SignalShutdown contract at
`src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs:57–97`.

The new wording accurately separates a signaling attempt from acknowledgment:

- `WaitForEvent:194–241` retains the shutdown/monitor handles around Poll with timeout `-1`.
  Cancellation is not passed into that native wait. The listener checks its session flag around
  WaitForEvent (`LinuxHidDeviceListener.cs:171–205`), so setting the flag alone does not interrupt poll.
- `SignalShutdown:265–298` returns for a missing/invalid shutdown handle and catches
  ObjectDisposedException during signaling. It returns no outcome that acknowledges a wake.
- `WriteShutdownSignal:356–394` returns after a full eight-byte write, reports then returns on a
  nonnegative short write, retries EINTR at most four times, reports exhausted EINTR or other errors,
  and returns without failure reporting on EAGAIN. EAGAIN can reflect an already pending signal;
  it is not itself proof of failure to wake. None of these void returns observes the waiting thread,
  loop exit, drain or native release. A failure-reporting logger can also throw; the corrected text
  promises neither universal successful signaling nor blanket exception suppression.
- Listener Stop (`119–149`) cancels, attempts signaling, and joins with its existing budget. A timeout
  retains the live session's resource ownership rather than disposing its source on the stopping
  thread. The listener-thread finally (`151–169`) disposes the source on actual loop exit. This is
  source control flow, not a measured native shutdown deadline.

The test evidence is correctly bounded. In
`src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxHidDeviceListenerFaultInjectionTests.cs`,
the failure cases (`33–68`) supply an outcome and assert Error, eventual fake disposal and one wait
call. The blocked-wait case (`70–92`) requests Stop and asserts elapsed time under four seconds,
Stopped status, fake disposal and one wait. The fake (`278–356`) uses ManualResetEventSlim and Set,
not libc poll/eventfd. Those meaningful managed-loop assertions do not qualify production native
poll completion, all signal-failure paths or a shutdown deadline. Tests were read, not executed.

Original selected units remain `core-001:unit:2ab3cc1db2ed4269` (original 86–92) and
`core-001:unit:7a90c15f7fb4a767` (original 55–66). Their original ranges/hashes were independently
matched to B2's ledger; their former clean statuses are provenance, not renewed sampling credit.

### SAMPLE-B2-002 — factory invocation follows the Start guard

Accepted constructor documentation and corresponding session explanation at
`src/Core/src/Transports/Hid/Linux/LinuxHidDeviceListener.cs:19–67`.

`Start:70–116` checks Started and returns at 74–77 before invoking the factory at 86. A new
session attempt invokes the factory once to create its event source; Initialize and ListenerSession
construction follow that call. Failed initialization can therefore invoke the factory without a
successful session. The new wording does not imply otherwise, and explicitly excludes already-started
no-op calls. The type remarks now describe each new session instead of every Start invocation.
Existing injection purpose, session ownership and late-exit resource-safety rationale are retained.

Original selected constructor unit: `core-001:unit:5c0f333d06ef9126` (54–67).
Related same-file consistency unit: `core-001:unit:cf330b6a639970cd` (1–34).
Both identities and range hashes match the original B2 ledger. The consistency correction creates
no extra finding or sampling selection.

## Independent static preservation

Fresh read-only Python/text/hash comparisons found zero discrepancies:

- Both saved before texts match the primary checkout byte-for-byte and the accepted revision-2
  current-path hashes. Both corrected files match their recorded after hashes.
- An independently regenerated unified diff from those before texts to the current files exactly
  equals the accepted focused patch. There are exactly two changed files and only ordinary/XML
  comment changes.
- Verbatim nonblank lines excluding only full-line `//` comments are unchanged; inline comments,
  block comments, executable lines, assertions, imports, directives, declarations, defaults,
  attributes, strings and API identities remain in that bounded comparison. BOM and final-newline
  state are preserved. This is not a compiler or tokenizer proof.
- The other **211 eligible source files** match their accepted revision-2 raw hashes. All **27 prior
  editor/review records** match their protected hashes, including prior patches, decisions, holds,
  example inventory and independent reviews. No example was introduced or altered; the existing
  three-body inventory carries forward unchanged.
- All eight editor records listed by this correction's `return-identities.json` match their hashes.
  This reviewer changed none of those inputs.

Accepted after identities:

| File | Raw SHA-256 |
| --- | --- |
| `LinuxHidEventSource.cs` | `676d31c9d87d67a9b481082caed6cb06a91576f3120aa3c336d2dd9ded1f2f29` |
| `LinuxHidDeviceListener.cs` | `3bad0ebd9f43e81ae610b38cd18b998aa8e678494e5f0a7c9d693834e9fbd9c6` |

Only `sampling-correction/review.md` and `review-identities.json` were written. No product/editor
record edits, broad audit, new tooling, build/tests, native/device/SDK-crypto execution, formatter,
Git command/mutation or subagent occurred in this focused review. Source and assertion reading
establish documentation accuracy, not new runtime/native/physical acceptance. Existing native,
physical, deployment and producer gaps remain explicit and unchanged.

The sampler owns edited-overlap exclusion, zero-credit accounting and XML-category expansion; this
PASS allocates no sampling or coverage credit. Coordinator validation remains separate. Complete
Core Pass 1 publication and the deferred finding handoff, then stop; no new campaign is authorized.
