# Bounded discovery lifecycle sampling correction

This documentation-only successor addresses SAMPLE-A001/A003 in the current accepted
`docs/architecture/event-driven-device-discovery.md`, including sampled unit
`core-001:unit:cf1e2081af8b8ac4`. Fresh focused review remains required; the coordinator excludes changed
units from clean sampling credit and owns any sampling expansion.

The before-file was compared byte-for-byte with the primary checkout and matched SHA-256
`4c841d4992e626de8d504ca8c776d696c3145fc818d1cc93e9082cd98bd2bc60`.
Only this one product Markdown path is edited. All original SHARED editor/review records, revision-2
records and full successor patch remain byte-exact. No broader guide audit or source edit is included.

## Checked whole-source control flow

The complete current `HotplugSignals`, `YubiKeyMonitor` and `DeviceChangeFeed` files were read in the
guide worktree and primary checkout. Their XML revision can differ; the relevant implementation order
is the same. `source-evidence.json` retains exact current file identities, complete source text and
the observations below. These are source conclusions, not fault-injection or runtime execution.

| Source / complete relevant methods | Observation and narrative correction |
| --- | --- |
| `HotplugSignals` constructor, TryStart, Dispose, BestEffort | Startup iterates listeners and handles ordinary start/status failures with logging and callback-clear/stop/dispose attempts. BestEffort catches cleanup exceptions but its warning logger is unguarded. A logger throw can interrupt startup, later listener attempts, disposal loops, or channel completion. Guide now describes attempts and the logging condition rather than guaranteed independent startup/cleanup. |
| `YubiKeyMonitor.CreateAsync` | The constructed monitor starts its loop and awaits first publication. Its catch awaits DisposeAsync before rethrow. Cleanup failure can replace the earlier exception; the guide no longer promises original-error preservation or unconditional partial cleanup for every failure. |
| `YubiKeyMonitor.DisposeCoreAsync` | `_signals.Dispose()` precedes lifetime cancellation, loop await, token-source disposal, discovery retirement and feed completion, without a finally advancing later stages. Ordered completion depends on earlier calls returning. Final diagnostic logging occurs after feed completion and can still fault disposal. Guide distinguishes the ordered path from guaranteed completion. |
| `DeviceChangeFeed.Fault`, Complete, Subscribe, Subscription.Deliver/Complete | Fault stores the feed error and TryComplete(error) faults existing subscription channels; overflow separately faults one subscription. Later TryComplete(null) does not erase the existing channel outcome. Complete sets `_completed`, so later enumerations end empty before checking `_fault`; existing faulted tails retain errors, while healthy uncanceled tails can drain and end normally. |
| Retained dotnet/runtime v10.0.0 `BoundedChannel.TryComplete`, lines 305–360 | Existing `_doneWriting` returns false before replacement, preserving an earlier error. Initial completion can wait for buffered items to drain. This pins the channel-source detail separately from SDK source ordering; no installed runtime or new concurrency result is inferred. |

## Scope and transfer

- `before.md` / `after.md`: exact complete guide bytes for focused review.
- `product.patch`: conventional `a/`/`b/` focused before-to-after narrative patch.
- `changed-path-sha256.json`: exact before/after hashes and source of the accepted before identity.
- `source-evidence.json`: complete source/retained-runtime identities and text.
- `preserved-record-identities.json`: all prior SHARED records and original report, checked unchanged.
- `verification.json`: single-path scope, previous 25 product paths unchanged, fence/body/local-target
  checks, patch reverse check and diff whitespace result. Anchors/external URLs are outside this check.
- `output-identities.json`: new correction artifacts, excluding itself.

No C# XML, runtime/config/product script, central sampling record, other checkout or original editor/
review artifact is changed. No build/test/formatter/compiler/native/crypto/device operation, Git
index/ref/commit/push or subagent runs. A's Hotplug/SkipCause/Feed XML corrections remain separately
owned. Existing metadata/native/provider/SCP/physical qualifications and deferred Windows/PIV lanes
remain unchanged. Complete focused review, Core Pass-1 publication and the deferred handoff, then stop.
