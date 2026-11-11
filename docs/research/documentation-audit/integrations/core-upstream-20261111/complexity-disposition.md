# Integration complexity disposition

**Retain both methods with the two commit-message justifications below. No required source correction.**

Sol adjudicated only the two flagged methods in the active checkout
`/home/rupicola/Code/y/yubikit-core-docs-integration`. The inactive `/tmp` evidence copy was neither
used for Git nor modified. Recovery routing and independent acceptance are recorded in
`recovery/handoff.md`, `recovery/independent-review.md` and the original source `review.md`.
This task changed only this record; it ran no build, test, formatter, device operation, index/ref
mutation, commit or subagent. Whole-Core investigation remains gated on integration publication.

## Actual gate and baseline

The retained main run is
`validation/commands-v2/main-gates-home-first/toolchain/20261111-195127-739-build+test+docs-qa+docs-architecture+complexity+resilience+pack/`.
Its `run.log:120–142` reports 318 changed methods in 73 files against campaign
`f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa`, with exactly two new threshold findings:

| Method / current source lines | Cyclomatic | Cognitive | Disposition |
| --- | ---: | ---: | --- |
| `YubiKeyMonitor.RunAsync`, `src/Core/src/Devices/YubiKeyMonitor.cs:127–171` | 11 | 12 | Retain explicit single-producer trigger and failure classification. |
| `DeviceInfo.Equals`, `src/Core/src/Devices/DeviceInfo.cs:77–99` | 22 | 1 | Retain flat value comparison of all 22 stored properties. |

**Both are new source methods relative to the campaign baseline.** The campaign has no YubiKeyMonitor
file and no explicit `DeviceInfo.Equals` declaration. Its record type already had compiler-generated
equality; the incoming explicit implementation changes the memory-content comparison. Neither finding
is described as unchanged campaign debt. The tool's successful process/RESULT still requires these
explicit policy dispositions under root `CLAUDE.md` § Pre-Commit Checklist.

The retained `result.txt` reports zero warnings/failures, 4,442 ordinary unit executions with five
skips, and 197 separate resilience executions with zero skips. Its 4,639 total includes those repeated
resilience executions rather than 4,639 unique tests. Core's ordinary row is 2,065 executed / four
skipped; the older 1,787 figure belongs to an earlier source/run. These are unit-suite results, not
hardware qualification or a measure of whole-Core semantic coverage. This adjudication read retained
results and relevant test bodies; it executed no tests.

## Exact source and upstream provenance

Fresh read-only Git/file comparison found **whole-file byte equality** between each current file and
incoming `5746e72ed9a848d08b5782ddde025aa4a4b522ef`. That stronger comparison establishes identical
method bodies and tokens without relying on whitespace normalization or a new lexer/compiler run.
HEAD remains the campaign pin and MERGE_HEAD remains the incoming pin.

| Source | Current and incoming file SHA-256 | Incoming Git blob |
| --- | --- | --- |
| `Devices/YubiKeyMonitor.cs` | `1145218451f56567275d848d9bdfd04082a557fd49e88f2d2c536c08d01c83f1` | `be4dbf6dc61a4793d019d78519a334e484ff9311` |
| `Devices/DeviceInfo.cs` | `ee39c986928191e044c9869ca6cc787212e425cdd40396fdba208ea7d5dd4b00` | `47c998d5b72b18e5d2e31dd261958098b3a8a2bb` |

Method-span hashes cover the declaration through the method's closing brace/semicolon, preserving
indentation and normalizing only the terminal blank separator to one LF:

- `RunAsync`: 1,789 bytes, SHA-256
  `6ef8ec3e87f261352f58467337fbe92d2c8f68e6c0580bbeddd61967a45766c6`.
- `Equals`: 1,108 bytes, SHA-256
  `f9e561bc72b9469101ef6f26559d7ad30c14f8e9cb331223b3993e8815cde9d8`.

Those spans also match their upstream introducing commits:

- `8754f075b0054773a3f2b8f278ce5f7412439bce`, “rewrite YubiKeyMonitor as a single-lifetime producer and
  delete the worker,” contains the same `RunAsync` span. Its change removes the older worker/lifecycle
  structure and supplies the single owned producer and associated deterministic tests.
- `b590d44b85372952bb3a75a9c8c5309834921cdc`, “compare DeviceInfo by value and reject partial
  enumeration in monitored scans,” contains the same `Equals` span. Its message explicitly identifies
  timeout-byte content equality and suppression of spurious DeviceUpdated events from fresh buffers.

Both commits are verified ancestors of the incoming pin. The same spans occur at upstream
`c9ce183fb9ac60a82ab5455cd5a87d1e005e19c8`. The later merge
`820c323a815b8f7395a6222da9ef3b5c29bd7333`, also an incoming ancestor, already carries these exact
method-specific justification subjects: the inherited single-producer trigger/cancellation/failure
flow and flat field equality for metadata change detection. Its earlier justification is corroborating
history; the present retention decision is based on the actual methods below. The integration resolver
introduced neither method nor any additional branch in them.

## Independent retention rationale

`RunAsync` owns one sequential scan/publication loop. Hotplug triggers invalidate evidence before
scanning. The initial trigger must publish successfully before completing creation; later scan or
publication errors log and await another trigger while preserving the previous publication. Its inner
filter distinguishes an unrelated cancellation exception from cancellation of this monitor's own
lifetime. The outer handlers settle pending creation on lifetime cancellation or terminal failure,
fault the feed on terminal failure, and keep a failing error logger from obscuring that observable
failure. These are consequential lifecycle distinctions within one 45-line owner method. The measured
11/12 is reasonable for keeping those paths visible together; extracting handlers merely to lower the
count would obscure the classification during this merge.

The read test bodies establish useful regression intent: `YubiKeyMonitorTests` checks awaited first
publication and partial-lifetime cleanup (`:223–310`), later scan/publication/enumeration-timeout
failure followed by retry (`:351–387`), and invalidation failure faulting active and future watchers
(`:504–534`). These source assertions support retaining the explicit branches; suite success alone is
not treated as proof of every exceptional/logging path.

`Equals` consists of **22 property comparisons joined by 21 `&&` operators**, explaining cyclomatic
22 and the flat expression's cognitive 1. The properties include flags/enums, scalars, nullable text,
version values and timeout bytes; they are not 22 enum members. The derived `VersionName` adds no
stored state. `ChallengeResponseTimeout.Span.SequenceEqual` compares metadata bytes by content;
the other comparisons retain each property's value semantics. The adjacent hash implementation covers
the same properties and adds those bytes by content. This keeps independent decodes of equivalent
metadata equal while detecting a changed timeout. `SnapshotDiff.UpdateDeviceInfo` (`:70–78`) uses that
inequality to decide whether to update the retained object and emit DeviceUpdated.

`DeviceInfoTests.Equals_SnapshotsDecodedFromSeparateBuffers_CompareByContent` (`:95–111`) checks two
independent decodes, equality/operator agreement, equal hashes and changed timeout inequality.
`SnapshotDiffTests.Apply_EqualMetadataFromSeparateBuffers_DoesNotEmitUpdate` checks the consumer
effect. A flat, explicit comparison preserves this inspectable equality/hash contract; replacing it
with generic/reflection machinery or grouping helpers would add merge scope without a demonstrated
integration need. Future added stored properties must keep equality and hashing aligned.

## Exact commit-message lines

Include both lines in the eventual integration commit message:

```text
Complexity-Justification: YubiKeyMonitor.RunAsync: Retain the incoming single-producer loop with explicit hotplug invalidation, required initial publication, retryable later failures, lifetime cancellation and terminal feed faulting; its 11/12 complexity keeps those lifecycle outcomes visible in one owner method, byte-identical to 5746e72ed9a848d08b5782ddde025aa4a4b522ef.
Complexity-Justification: DeviceInfo.Equals: Retain the incoming flat comparison of all 22 stored metadata properties, including timeout bytes by content, so equivalent independent decodes do not emit DeviceUpdated; 21 conjunctions explain cyclomatic 22 with cognitive 1, and the method is byte-identical to 5746e72ed9a848d08b5782ddde025aa4a4b522ef.
```

These are explicit justifications for newly integrated source relative to the campaign, not an
exemption based solely on upstream origin. No threshold, assertion or runtime code is changed.
