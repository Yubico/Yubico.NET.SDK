# Packet A sampling corrections — independent focused review

## PASS — both additive documentation patches

Accepted at these exact hashes:

| Focused patch | SHA-256 |
| --- | --- |
| `sampling-correction/product.patch` — HotplugSignals and discovery skip-cause XML | `c00893c28c7b215f129b6b68e36a8240b2b34d5eec511da7fc4df8a082e29651` |
| `sampling-feed-completion/product.patch` — DeviceChangeFeed.Complete XML | `f50744c7d3c81a515ca0f43c84e5dd22e84f87c368260308d61034cc0ecc3aae` |

No further content correction is required. These are disjoint, additive documentation deltas from
byte-verified before files, not replacements for the accepted revision-2 37-file patch or reviews.
That acceptance and its qualification limits remain unchanged. Compiler/token/API/cref and final
validation remain coordinator gates; this PASS supplies no execution or native-cleanup proof.

Checkout: `/home/rupicola/Code/y/yubikit-core-docs-edit-discovery`.
HEAD/base: `da66f5d01ba594993974ddd26723e8c300c0a454`.
Original source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.

## Content decisions against complete relevant source

### SAMPLE-A001 — ordered best-effort listener work

Selected original unit: **`core-001:unit:711e47d18bab0742`**.

The complete current and captured-before `HotplugSignals` source was checked, including the constructor,
`TryStart`, all three disposal passes, `BestEffort` and channel construction. Current constructor XML
at **35–39** and disposal XML at **86–96** accurately describe attempts and their limits:

- The constructor attempts listeners in input order. `TryStart` catches factory/callback/start/status
  failures, but its catch-path warning logger is not itself contained. Logging can escape before partial
  cleanup or the next iteration. The all-listeners-unavailable warning is also outside that containment.
- Disposal sets `_disposed` first, then attempts all callback detachments, all stops and all disposals,
  in that order. `BestEffort` catches an action failure and logs it; its warning logger can throw.
  Neither the caught action failure nor the log establishes successful cleanup.
- Channel `TryComplete` is reached only after the passes return. A warning-logger exception can interrupt
  subsequent work and prevent that call. The final debug log occurs **after** TryComplete: an exception
  there does not undo a completion already requested. Later Dispose calls return on the set flag and
  do not retry failed work. Channel completion is not proof of listener shutdown or native release.

The corrected text retains useful order/rationale without promising successful cleanup or logger-failure
resilience. No listener/native implementation or error policy changed; A-E03 remains unexecuted.

### SAMPLE-A002 — discovery ownership is also a refusal cause

Selected original units: **`core-001:unit:c085f1cd3632e7c2`** and
**`core-001:unit:076b4d4328825902`**. Exception-type unit
`core-001:unit:79340850e7d0f3c2` is adjacent context, not another selected sample.

The complete cause/exception file was checked against the registry implementation and shared-read
call path. The corrected enum preamble (**17–22**), InterfaceLeaseHeld XML (**28–31**) and exception
summary (**48–55**) correctly include another discovery lease, an active connection and a waiting
connection. `IsDiscoveryAvailable` checks the complement of the atomic acquisition refusal predicate:

- `DeviceConnectionRegistry.cs:144–145,181–191`: an unregistered interface is available; otherwise,
  under the lock, unrecovered ownership is checked before testing discovery/connection/waiter state.
- `DeviceConnectionRegistry.cs:193–231,281–304`: connection waiters are tracked separately;
  TryAcquireDiscovery calls ThrowIfUnrecovered first, then returns null for active discovery, a live
  connection or a waiting connection. Unrecovered ownership therefore throws its separate exception,
  rather than becoming InterfaceLeaseHeld.
- `SharedDeviceInfoRead.cs:98–132,134–243,245–290`: the PID/cohort precheck can translate false to
  InterfaceLeaseHeld; atomic TryAcquireDiscovery can independently return null and produce the same
  cause before open. A successful precheck reserves nothing. Admission/lease/native-owner transfer
  and existing failure cleanup remain unchanged.

This corrects cause vocabulary, not runtime admission behavior, rollback policy or an engineering finding.

### SAMPLE-A003 — additional enclosing-context Complete XML

Corrected method unit: **`core-001:unit:3c7f0585447bd6d1`**. It is an **additional context finding**, not
a selected Complete sample. No selected status is assigned to enclosing feed/subscription context.

The complete current and captured-before feed were read, including `Fault`, `Subscribe`, both Complete
methods, overflow delivery and channel construction. Current method XML at **111–117** matches:

- Complete requests normal completion for remaining healthy subscriptions; it does not stop the producer.
  The producer-stopped precondition remains explicit.
- Feed Fault records the first feed error and requests fault completion for current subscribers. Overflow
  establishes a subscriber-local error without setting the feed fault. Later normal requests do not
  replace either channel's already-established error.
- Subscribe checks `_completed` before `_fault`, so later subscriptions after Complete end empty even
  after a preceding feed fault. Existing captured replay and queued changes can still drain.

The retained **dotnet/runtime v10.0.0** `BoundedChannel.cs` complete TryComplete passage **305–361** was
rechecked. Its first-terminal check at 319–326 rejects replacement of an existing completion outcome;
buffered items delay channel completion. Exact retained source SHA-256:
`4798673d97ee37741287c70b7f017b5f962b956b1a9e214b9089e0940a4ee406`.
This is pinned managed-runtime implementation evidence, not a device standard or fresh experiment.

Existing `DeviceChangeFeedTests.cs:118–203` methods were read without execution. They separately assert
overflow isolation/recovery, healthy completion and later-empty behavior, first-fault delivery, and
replay/buffer drain. The fault test calls Complete only after its error assertions; it is not credited
as a new pending-reader fault-then-Complete experiment. The editor's report preserves this distinction.

## Identity, provenance and executable-preservation checks

Fresh independent read-only checks confirmed:

1. All three captured before files equal the current primary-checkout files, original source pin,
   worktree base and accepted revision-2 retained-path identities byte for byte. Current corrected
   file hashes match the focused manifests. Exact before/current hashes are in the companion review
   identities. Actual diff hunks change only the described XML and recorded final newlines.
2. Both focused patches equal the actual diffs for their listed files using explicit `a/` and `b/`
   prefixes. Their path sets are disjoint from each other and from the accepted 37 edited paths.
   Exact filtered non-comment lines match for all three files. No imports, directives, signatures,
   defaults, enum values/order, assertions, logging strings, runtime help or executable bodies changed.
3. All **37 accepted revision-2 source hashes** are unchanged; their scoped full diff still equals the
   immutable accepted patch `028c2a45a04d8522cf6211a6d76724e8ce0c84b3dc6a32dfdc128f584dcf7228`.
   Both earlier reviews, revision-2 records and frozen predecessors match their exact prior identities.
   The first sampling return's 25 frozen predecessor files and the feed addendum's 35-file predecessor
   manifest were checked. The completed two-file return and its artifacts remain intact.
4. Both focused output manifests and their listed artifact hashes match. The annotations' **19 original
   ranges** (7 first-return units, 11 feed-context units and 1 related guide range) were independently
   checked against exact original file/character-range hashes. These are provenance checks, not 19
   selected samples or new coverage credit. The three selected A IDs above remain distinct from the
   additional Complete unit and other adjacent/enclosing context. The original READ-CLEAN records and
   empty candidate lists remain historical; separate corrective annotations do not overwrite them.
5. Registry/shared-read/test/runtime dependency hashes match their recorded identities. **16 XML blocks**
   across the three corrected files parsed as well-formed XML; scoped `git diff --check` exited **0**.
   The files contain no example bodies, and no existing accepted example identity was changed.

The static comparison is not full compiler/token/API proof. No source-supported assertion is promoted
to an executed result. The accepted wording is calm, explicit about actors and local conditions, and
retains necessary rationale without new safety or conformance promises.

## Coordinator handoff and stop

The event-driven guide's selected unit `core-001:unit:cf1e2081af8b8ac4` is a **SHARED-owned** related
narrative correction; only its original provenance was hash-checked here. This review neither edits it
nor accepts its pending correction. Sampling misses, denominator/expansion decisions, source credit,
central ledgers, transfer and final publication remain coordinator-owned. No candidate or engineering
behavior is added by this return.

FPX-01/A-E01 and A-E02 through A-E06 remain NOT-RUN. Prior native/physical/profile/cross-process,
historical-failure and applet-boundary qualifications remain unchanged. No product edits, builds,
tests, formatter/compiler, crypto/native/device execution, Git mutations or subagents occurred.
Only `editing/A/sampling-review.md` and `editing/A/sampling-review-identities.json` were created.
Finish the existing Core Pass-1 validation and agreed handoff, then stop; no new framework or pass is requested.
