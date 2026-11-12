# Event-driven device discovery architecture

## Public ownership boundary

manager watch creates a producer; monitor watch subscribes to an existing one.

`YubiKeyManager.Find…Async` delegates to `YubiKeyDiscovery`, which creates a fresh scoped finder per
call and retires its read scope in `finally`. It neither starts tracking nor publishes into a monitor.
`YubiKeyManager.WatchAsync(ct)` creates and disposes a private monitor per enumeration, starting on the
first `MoveNextAsync`. `CreateMonitorAsync(ct)` returns an initialized monitor for the caller to share
and dispose. Every monitor scans all interfaces, even without subscribers. Build membership projections
from replay, or call `FindAllAsync` for a one-off. Live Management reads remain the configuration API.

## Components and scan flow

| Component | Responsibility |
|---|---|
| `HotplugSignals` | Starts optional `IDeviceChangeListener` implementations; owns their teardown and a capacity-one signal channel |
| `RescanScheduler` | Uses `TimeProvider` for initial, interval, and coalesced hotplug triggers |
| `YubiKeyMonitor` | Composes resources; runs the sole sequential discovery/publication loop; owns lifetime |
| `IMonitoredDiscovery` / `FindYubiKeys` | Invalidates evidence, scans with discovery budgets, retires read scopes |
| `DeviceChangeFeed` | Holds membership and revision; publishes diffs and manages isolated subscriptions |
| `SnapshotDiff` | Computes identity retention, replacements, and metadata updates |

```text
platform listener Changed callback
  → HotplugSignals: capacity-one Channel<ConnectionType>, DropWrite
  → RescanScheduler: Initial, Interval, or Hotplug(transports)
  → monitor loop: invalidate evidence for Hotplug, await discovery
  → DeviceChangeFeed.Apply: SnapshotDiff, then atomic publication
  → independent bounded subscriber buffers
  → Subscribe: captured replay → SnapshotComplete → live tail
```

The scheduler yields `Initial` immediately. It then waits for a signal or the five-second fallback
interval. Hotplug hints are coalesced with a 200 ms quiet window and a one-second cap. The cap bounds
coalescing, not scan completion or detection latency. Each consumed hint combines transport flags;
when the capacity-one channel is full, extra hints are dropped rather than queued. This is safe because
any hotplug trigger invalidates evidence globally and the next scan reconciles all interfaces. Hints
received during a scan can request a follow-up scan. Elapsed-time checks prevent a continuously refilled
channel from starving the coalescing deadline. Scheduler timing is tested with `FakeTimeProvider`.

The monitor awaits every scan and its publication before requesting the next trigger. Immediately
before a hotplug scan it calls `IMonitoredDiscovery.InvalidateEvidence(transports)`. Listener callbacks
only signal; they do not invalidate evidence themselves. The monitor has no lock or parallel producer.
`FindYubiKeys` still serializes its own scan callers with a semaphore.

## Discovery budgets and partial results

`DiscoveryBudgets` centralizes the enumeration budget (five seconds; Linux scans that overlap a `pcscd`
restart measured under 300 ms), identity-read
budget (two seconds per attempt), and metadata-read budget (three seconds). Identity reads can retry
up to three attempts; concurrent finder callers can queue on the scan lock. `ScanUpperBound` is a sum
of stage budgets, **not a hard end-to-end wall-clock guarantee**.

`EnumerateInterfacesAsync` starts both in-scope transport waits together. If enumeration exceeds its
budget, the behaviour depends on the caller. One-shot `Find…Async` logs a warning, omits that transport for
this call and returns the other transport's results. The monitor's path (`IMonitoredDiscovery.FindAllAsync`)
throws `TimeoutException` instead, because a partial scan diffed against the previous snapshot would read
as removals of every key on the stalled transport; the loop logs and retries on the next trigger, and the
previous snapshot stands. Caller cancellation abandons the wait. Late enumeration completion/failure is
observed and logged at Debug level; it does not add interfaces to an already published scan.

Budgets bound callers' waits, not native execution or ownership. Already-dispatched work completes
and cleans up independently. A returned initialized monitor is not proof that every transport or
metadata field was accessible.

The monitor can reuse successful serial-less metadata. Its identity cache checks Product ID changes;
its metadata cache currently does not perform that same check. Listener invalidation and observed
absence still evict both. Treat cached metadata as last-known evidence rather than a periodic live read.

## Snapshot diff rules

`SnapshotDiff.Apply(current, observed, revision)` builds a successor dictionary keyed by the physical
interface-path set (`YubiKeyDevice.PhysicalIdentityKeyFor`), not `DeviceId` or the current evidence tier.

- An absent identity produces `DeviceRemoved`; a new identity produces `DeviceAdded`.
- A changed connection set, or two known serials that contradict each other, produces removal of the
  predecessor followed by addition of the successor. Unknown serials do not prove substitution.
- Otherwise, the originally published object is retained, so its later removal correlates by reference.
- For retained `YubiKeyDevice` objects, changed non-null `DeviceInfo` replaces last-known metadata and
  produces `DeviceUpdated`. Equal metadata produces no change; null does not erase a successful value.
- Changes are ordered as all removals, then all additions, then all updates. Order within each group
  is not a public sorting guarantee.

The revision increases once per applied scan, even if its diff is empty. All changes from that scan
share the revision. Metadata lives on retained mutable device objects; a revision does not freeze it.
Diffing and metadata refresh occur outside the feed gate, so atomic publication is a membership/change
boundary, not transactional isolation of device properties.

`DeviceInfo` scalar fields in a returned value survive snapshot replacement. Its `ChallengeResponseTimeout`
memory currently aliases an input TLV, however, so read-only metadata is not a general independent-storage
or post-disposal stability guarantee. See [cached metadata](../usage/device-discovery.md#cached-device-metadata).

## Publication and subscription

Only the monitor loop calls `DeviceChangeFeed.Apply`. It computes the diff outside the gate, then
under one short lock replaces the membership dictionary, advances the revision, and enqueues the
complete ordered change list to every registered subscription. Published dictionaries are not mutated.
No consumer code or logging runs under the gate; channel continuations are asynchronous.

On the first `MoveNextAsync`, `Subscribe` captures the dictionary and revision and registers the
subscriber under that same gate. Outside it, the iterator emits one `DeviceAdded` per captured member,
one `SnapshotComplete` at the captured revision (even if empty), then reads its live tail. Capture and
registration cannot miss an intervening publication. Replay itself uses no buffer capacity, but changes
arriving during replay can fill the live buffer. Do not prepend an independent snapshot or Find call.

Each subscriber has a 256-change buffer. Publication uses `TryWrite`, never waits for a consumer, and
faults only the overflowing subscription with `InvalidOperationException` rather than dropping changes.
Recovery clears the consumer projection and re-enumerates the same monitor. Cancellation or disposing
an enumeration unregisters only that subscription. `Fault` records a terminal error and faults current
and later subscriptions with a wrapped `InvalidOperationException`. `Complete` ends healthy subscriptions
normally; later subscriptions end empty. Captured replay and buffered history can drain after termination.

## Why listeners are hints, not truth

All transports use `IDeviceChangeListener.Changed`, a payload-less callback. Set it before `Start` and
check `Status` afterward. Never stop or dispose a listener inside its callback: native shutdown may
wait for that callback. Platform-specific HID diagnostics remain internal Trace logging, not public
arrival/removal payloads. Unknown removals still request reconciliation.

One physical key can expose several interfaces, notifications can be duplicated/coalesced, and native
events need not identify the physical key. Only discovery plus the snapshot diff determines public
membership changes. Windows HID uses configuration-manager notifications, macOS uses IOKit callbacks,
Linux uses udev, and the desktop smart-card listener uses PC/SC status changes.

`HotplugSignals` starts each listener independently. A thrown startup or status other than `Started`
is logged and that listener is cleaned up without preventing the other from starting. With no listeners,
interval scans still run. Unavailable services may enumerate empty; this is best-effort partial discovery.

<a id="single-lifetime-worker-and-shutdown"></a>
## Monitor lifecycle and disposal

Creation starts listeners, launches the loop, and awaits its first publication. Empty success is ready.
First-scan failure fails fast with the original error; initialization failure or cancellation disposes
partial ownership. The creation token is independent of the monitor lifetime after return.

Later scan/publication exceptions are logged and retried on the next trigger. Discovery failures before
publication retain the last membership. Unexpected failures outside that retry path fault the feed before
best-effort diagnostic logging. A new subscription does not restart a failed monitor: dispose and create
a new instance. There is no pause, restart, manual rescan, interval replacement, or monitor generation.

Disposal is idempotent and strictly sequential:

1. Clear callbacks, stop all listeners, dispose all listeners, and complete the signal channel.
2. Cancel the lifetime token and await the actual scan loop, including any in-flight scan/publication.
3. Dispose the lifetime token source and retire discovery.
4. Complete the change feed.

There is no managed-loop abandonment or disposal timeout. In-flight scan waits are bounded by the
discovery budgets; listener cleanup and total disposal are not a hard wall-clock promise. No producer
can publish after feed completion because the loop has already ended. Caller-owned connections and
sessions are not disposed. Concurrent `WatchAsync` enumerations are supported, and `DisposeAsync` may
overlap them: it ends each active enumeration normally. `DisposeAsync` must not be called concurrently
with itself. Applications that want consumers to observe a final state before shutdown do so on their
own terms; disposal does not wait for consumers.

### Finder evidence epochs are not monitor generations

Hotplug invalidation atomically replaces the whole identity/metadata evidence epoch and retires its
read scope, even when only one transport signalled. Old native completion can write only into unreachable
old caches; retaining sibling evidence could mix two physical keys. Scan-observed absence also evicts
evidence. Without either hint or observed absence, same-interface substitution can remain unknowable.

Read-scope retirement cancels queued admission and prevents later opens. Already-open reads retain
actual worker admission and registry leases until native teardown. Sibling admission, same-product
serialization, and unknown-product atomic reservation remain; a pending unknown-product request
reserves no gates. Neither a wait budget nor scope retirement proves native release. See
[native discovery scheduling](device-discovery-guarantees.md#native-discovery-scheduling).

`SnapshotDiffTests` pins retention/replacement and metadata changes; `DeviceChangeFeedTests` pins replay,
ordering, revisions, overflow, and termination. Monitor, scheduler, and hotplug tests cover initialization,
sequential scans, timing, fallback, and disposal; enumeration-budget tests cover partial results and late work.
