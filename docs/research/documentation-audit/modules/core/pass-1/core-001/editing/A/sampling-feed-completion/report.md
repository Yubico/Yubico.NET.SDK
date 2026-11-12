# SAMPLE-A003 — separate feed-completion documentation addendum

## Outcome and boundary

Only the XML on `DeviceChangeFeed.Complete` in `src/Core/src/Devices/DeviceChangeFeed.cs` changes.
The before-source matched current accepted primary byte-for-byte, SHA-256
`68d779ac0a709ad45daf74ce4b75938f55f8c7a22151eac57afed59b6d091ee3`.
The corrected method XML requests normal completion for remaining healthy subscriptions, preserves
existing subscription errors, keeps the producer-stopped precondition, and explains that replay/buffered
changes may drain. Later subscriptions remain empty. No runtime, assertion, signature or logging change.
The patch tool also adds a final newline to the previously unterminated file; the manifest records this
incidental whitespace normalization separately from exact non-comment-line preservation.

This addendum is separate from the completed SAMPLE-A001/A002 return in
[`../sampling-correction/report.md`](../sampling-correction/report.md). That return's complete file
population and bytes remain unchanged, including focused patch
`c00893c28c7b215f129b6b68e36a8240b2b34d5eec511da7fc4df8a082e29651` and output-identity manifest
`841c02582dad5183cce16f0a850044ed331329a3ba85abd7b09104271fb2b417`.
No cumulative A patch, accepted revision-2 record or prior review is regenerated.

## Honest sample provenance and exact A unit

**SAMPLE-A003 is an additional enclosing-context finding, not a new selected Complete sample.**
The actual inventory/investigation unit for the corrected method is
**`core-001:unit:3c7f0585447bd6d1`**, original lines **109–122**, characters **4547..4923 exclusive**,
range SHA-256 `d7f1c6b9c06c224812e040370f55d96b0c7dee3dcb6f50b453055dbc8bc5ae44`.
Its source pin remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; its original READ-CLEAN disposition
and empty candidate list remain historical, unmodified records. The before file has 172 lines; the
corrected XML occupies current **111–117**, followed by the unchanged method body at **118–128**.

The related selected guide unit is **`core-001:unit:cf1e2081af8b8ac4`**, assigned to **SHARED**:
`docs/architecture/event-driven-device-discovery.md`, original lines **124–149**, characters
**8469..10216 exclusive**, range SHA-256
`54530cfe088e7bdfd9ccc6f78c181be3b03c88df9f002a9b602de34866ffa547`.
These are original pinned ranges, not mislabeled current ranges. Current primary guide lines **132–156**
contain the lifecycle narrative, including successful all-listener cleanup at 145 and every-active-stream
normal termination at 153–155; startup text at 127–129 has the related absolute. SHARED owns those
corrections separately. No guide was edited here, and the Complete unit is not retroactively called a
selected guide/class/field sample.

Actual enclosing-source context IDs are recorded as context only in `annotation.json`: the feed class
`core-001:unit:a6706ccc51c5b8b2`, subscription class `core-001:unit:25aa4af785e881de`, and relevant
subscription/channel fields and methods. They provide no invented additional sample selections or
coverage credit. Coordinator-owned denominator/expansion decisions and central ledgers remain untouched.

## Complete source verification

The full DeviceChangeFeed file was read, together with the retained runtime's complete TryComplete
implementation and the complete relevant existing test methods. The source establishes:

| Source before correction | Actual flow and qualification |
| --- | --- |
| `DeviceChangeFeed.Fault`, 97–109 | Records the first feed fault and passes a wrapped error to each current subscription's Complete. |
| `Subscription.Deliver`, 154–162 | A full bounded buffer calls subscription Complete with an overflow error. This subscriber's error is separate from the feed-level fault field. |
| `Subscription.Complete`, 164–168 | Sets the terminated flag and calls writer TryComplete with the supplied error. A later normal-completion request cannot replace an already-established channel outcome. |
| `DeviceChangeFeed.Complete`, 111–122 | Under the publication lock, sets `_completed` once and requests subscription Complete with no error. Healthy remaining channels receive normal completion; prior channel errors remain. It does not itself stop the producer. |
| `Subscribe`, 64–95 | Checks `_completed` before checking `_fault`, so newly started subscriptions after Complete end empty even if an earlier feed fault exists. Previously captured replay and queued records remain distinct from that new-subscription path. |

The exact retained dependency is **dotnet/runtime tag v10.0.0**,
`System.Threading.Channels/BoundedChannel.cs`, complete **TryComplete, lines 305–361**, SHA-256
`4798673d97ee37741287c70b7f017b5f962b956b1a9e214b9089e0940a4ee406`.
Lines 319–326 return false if writing is already completed, rather than replacing the stored error;
buffered items delay completion. This is pinned managed-runtime implementation evidence, not a new
experiment, device requirement, API-version upgrade or physical qualification.

Existing `DeviceChangeFeedTests` methods were inspected without execution:
`Apply_Overflow_FaultsOnlySlowSubscriberAndAllowsReplayRecovery` (118–142),
`Complete_EndsCurrentAndLaterSubscriptionsNormally_AndRejectsApply` (144–157),
`Fault_EndsCurrentAndLaterSubscriptionsWithFirstFailure_AndRejectsApply` (159–175), and
`Subscribe_CancellationDuringReplay_DoesNotAffectOtherSubscribers` (177–203). They separately assert
overflow isolation/recovery, healthy completion/later empty behavior, first-fault delivery, and replay/
buffer drain. The fault test calls Complete after its error assertions; it does not newly measure a
pending-reader fault-then-Complete case. No assertion was changed, added, run or credited as that experiment.

## Owner and existing-mechanism crosslinks

- **A / SAMPLE-A003:** this Core Complete XML correction is the entire new source delta; related existing
  **CORE-A-C007 / FPT-007** feed/subscription semantics and **CORE-A-C008 / FPT-008** monitor lifetime
  qualifications retain their identities. No new runtime finding, policy or candidate is allocated.
- **A / SAMPLE-A001:** the completed HotplugSignals constructor/cleanup qualification stays exclusively in
  the preserved `sampling-correction/` return; selected unit `core-001:unit:711e47d18bab0742` is unchanged
  as historical provenance. Native release and logger-failure experiments remain held.
- **A / SAMPLE-A002:** the completed discovery-lease/connection/waiter cause qualification stays in that
  same preserved return; selected units `core-001:unit:c085f1cd3632e7c2` and
  `core-001:unit:076b4d4328825902` retain their exact provenance. Registry code is unchanged.
- **SHARED:** selected guide unit `core-001:unit:cf1e2081af8b8ac4` and its current lifecycle/startup
  narrative need the corresponding best-effort-cleanup and healthy-stream qualifiers. This editor neither
  edits that narrative nor claims its reconciliation is complete.

## Focused artifacts, review and stop

`product.patch` is a **one-file** delta from the exact current-primary before file, not a replacement
for either prior sampling correction or the accepted 37-file A transfer. `file-identities.json` contains
before/primary/current hashes, comment boundaries and exact non-comment-line preservation.
`annotation.json` identifies the real Complete unit, related original context/guide units and their range
hashes. The before-source snapshot is byte-exact. `predecessor-identities.json` hashes 35 existing editor/
review files without copying or changing them. `verification.json` supplies the focused patch hash and
fresh static-check results; `output-identities.json` binds the addendum artifacts except itself.

Focused review should check the one XML block against Fault, both Complete methods, overflow delivery,
Subscribe's completed-before-fault ordering, and the retained TryComplete passage. The 37 accepted
revision-2 source identities and the completed two-file sampling return remain unchanged. No source
example occurs in DeviceChangeFeed and no prior example identity changes. Pending primary transfer and
content acceptance belong to the coordinator/reviewer, not this source correction.

No SDK build/test, formatter, compiler, native/device/crypto execution, Git mutation or subagent ran.
All experiments retain NOT-RUN status, including FPX-01/A-E01 and A-E02 through A-E06. Finish Core Pass 1
and its agreed handoff, then stop; no new pass, broad re-audit or implementation project is introduced.
