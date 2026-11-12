# B2 clean-sampling documentation correction

## Outcome and patch

Verified both sampling findings against the actual source and applied a small comment-only correction
in the two assigned Core files. No contradiction was found. The correction is ready for independent
review; it supplies no new runtime, native or hardware qualification.

Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-transports`.
Both pre-edit files matched the current primary checkout **byte-for-byte** and matched the accepted
transport revision-2 identities. The primary checkout was read only for that two-file comparison.

**Additive focused patch:** `source.patch`, SHA-256
**`a994f3081f0bc7439221d2a6073b5bc4f0dec9149d9dfa4c3e254799cbab1049`**.
Apply it after the accepted transport revision 2; it is not a replacement full transport patch.
The accepted full patch remains byte-exact at `../revision-2/source.patch`, SHA-256
`109bd7833b71d69f9b25b2a021ea27e460334535fe28e8fcdc8cd6bd160404de`.
The original patch `86b0c6…`, all previous editor records and all review records are also preserved.

## SAMPLE-B2-001 — Shutdown signaling is an attempt, not an acknowledgment

`src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs` now describes SignalShutdown as attempting
a shutdown wake. The remarks preserve concurrent-Dispose tolerance and explain that a missing,
invalid or concurrently disposed handle, or a failed signal write, can leave the native wait unwoken.
A normal void return acknowledges neither wake, loop termination, drain nor release.

The source supports this distinction:

- WaitForEvent passes timeout `-1` to Poll. Once the listener loop is blocked in that wait, it can
  observe cancellation only after the wait returns; the flag alone does not interrupt native poll.
- SignalShutdown returns for a missing/invalid handle and catches ObjectDisposedException during
  signaling. WriteShutdownSignal also returns on short/nonnegative writes, EAGAIN, other errors and
  exhausted bounded EINTR retries. Some failures invoke the logging callback; normal return carries
  no outcome that acknowledges a wake or native release.
- Stop requests cancellation/signaling and joins with its existing budget. If the thread remains
  alive, its session retains ownership; actual thread exit drives source disposal.

The selected type prose's “bounded exit” wording is also corrected. The existing no-hardware tests
exercise supplied loop outcomes, error transitions, hints and caller-requested stop using a fake
ManualResetEventSlim wake. Those assertions do not establish native poll completion or a shutdown
deadline. Test source was read, not executed or changed.

Original selected anchors:

- `core-001:unit:2ab3cc1db2ed4269` — ILinuxHidEventSource.SignalShutdown, original lines 86–92.
- `core-001:unit:7a90c15f7fb4a767` — related source/type explanation, original lines 55–66.

## SAMPLE-B2-002 — The factory is not invoked by every Start call

`src/Core/src/Transports/Hid/Linux/LinuxHidDeviceListener.cs` now associates factory invocation with
event-source creation for a new session. Start calls returning at the already-started guard do not
invoke it. The constructor's no-hardware injected-fake purpose remains unchanged.

The source checks Started before reaching the factory call. The factory precedes initialization and
successful ListenerSession construction, so creating an event source is not proof that startup
succeeded. Matching file-level remarks now describe each **new session**, replacing the same
unconditional “Each Start” claim. The session ownership, retained-state and late-exit safety rationale
is otherwise unchanged.

Original selected anchor: `core-001:unit:5c0f333d06ef9126`, constructor documentation/implementation,
original lines 54–67. The same-file session explanation retains its separate original identity,
`core-001:unit:cf330b6a639970cd`; this is a consistency correction under the same finding, not a new
sampling selection or independent finding.

## Identities and bounded checks

| Source file | Accepted before SHA-256 | Corrected after SHA-256 |
| --- | --- | --- |
| `LinuxHidEventSource.cs` | `547a1b3236949e845c5dde57ec7bd254e9ae7dbbf62530a73e92cf64b64c94b0` | `676d31c9d87d67a9b481082caed6cb06a91576f3120aa3c336d2dd9ded1f2f29` |
| `LinuxHidDeviceListener.cs` | `d0ab57494728d55049b57238ec3a962b677f806a225df6c6441c5f72a699b01c` | `3bad0ebd9f43e81ae610b38cd18b998aa8e678494e5f0a7c9d693834e9fbd9c6` |

`inputs.json` records the accepted primary/worktree identities and immutable prior-record hashes.
`original-unit-anchors.json` preserves the original selected-unit ranges, hashes and investigator
records without rewriting their historical status. `file-dispositions.json` and
`finding-dispositions.json` record the two additive editing decisions and complete source rationale.
`xml-check.json` and `static-preservation.json` record the bounded checks and their limits.

Only ordinary/XML comments change. Executable lines, directives, imports, declarations, defaults,
attributes, runtime strings, assertions and APIs are preserved, along with BOM/final-newline policy.
No examples are added or changed. The accepted example inventory and its three body identities remain
unchanged. All previous hold decisions and Linux native/runtime qualification gaps remain in force.

No build, tests, formatter, native/device/SDK-crypto execution, Git mutation, subagent, new framework
or broad semantic re-audit was performed. Static text/hash/XML checks and read-only Git inspection
are the only verification here. New records are confined to `editing/transports/sampling-correction/`;
all previous transport records and reviews remain immutable.

## Independent-review and sampler handoff

Review the two corrected complete XML units and the related type/session explanations, then compare
the signal/write/poll and Start-guard/factory control flow identified in `finding-dispositions.json`.
Bind the focused patch hash above. This return claims neither independent content acceptance nor
compiler/runtime validation.

The sampler owns edited-overlap zero-credit accounting and the requested XML-category expansion.
No clean-sampling credit or campaign coverage status is allocated or amended by this editor.
Finish Core Pass 1 publication and the deferred finding handoff, then stop; no implementation or
new native/device campaign is authorized by these corrections.
