# Device discovery and monitoring design

manager watch creates a producer; monitor watch subscribes to an existing one.

## Why these boundaries exist

- Listeners see interfaces, not physical keys. Hints coalesce or drop; `SnapshotDiff` decides membership
  from observed interface-path sets, connection flags, and contradictory known serials, never hint payloads.
- `DiscoveryBudgets` is the single source of discovery wait budgets: enumeration 5 s,
  identity 2 s per attempt, metadata 3 s. There is no separate managed-loop disposal deadline: disposal
  awaits the actual loop before retiring discovery and completing the feed. Budgets bound waits, not
  native work; identity retries and finder scan-lock queueing preclude a hard total wall-clock bound.
- The lifetime is one-way. Dispose and create another monitor; no pause/restart generations, abandoned
  managed loops, or late-publication guards are needed. Disposal may overlap active enumerations (they end
normally, which the hosted-singleton pattern relies on) but not another disposal.
- `CurrentDevices` was removed. Replay plus `SnapshotComplete` distinguishes
  loading from loaded-empty without a snapshot/subscription gap. Build a projection; use `FindAllAsync`
  for independent one-off discovery. Retained metadata is last-known data, not live configuration.
- The one lock in the feed, `_gate`, protects snapshot/revision publication, complete-diff enqueue,
  capture plus subscriber registration, unregistration, and terminal state. Diffing, logging, and replay
  stay outside it; one producer supplies ordering. Finder/native ownership locks remain separate.

## Public surface justification

Each manager operation has one signature with optional arguments, not token-only convenience overloads.
Generated record equality, copying, printing, and deconstruction are standard record value operations.

Change records carry references to published device objects; their generated value operations do not
freeze metadata at `Revision`. Snapshot replacement preserves earlier scalar values, while
`DeviceInfo.ChallengeResponseTimeout` currently retains an input-TLV memory alias. The finder also distinguishes
failed reads from cached successful serial-less metadata, and identity-cache PID validation from the
metadata cache's member-ID keys. Keep these storage/freshness limits beside consumer snapshot claims.

- `YubiKeyManager.FindAllAsync(interfaces, cancellationToken)`: fresh scoped discovery with no tracking.
- `YubiKeyManager.FindFirstAsync(predicate, interfaces, cancellationToken)`: selection when absence is exceptional.
- `YubiKeyManager.FindFirstOrDefaultAsync(predicate, interfaces, cancellationToken)`: selection when absence is expected.
- `YubiKeyManager.WatchAsync(cancellationToken)`: one consumer owns a producer without managing a monitor explicitly.
- `YubiKeyManager.CreateMonitorAsync(cancellationToken)`: several consumers share one initialized producer.
- `YubiKeyMonitor.WatchAsync(cancellationToken)`: independent replay and live tail on that producer.
- `YubiKeyMonitor.DisposeAsync()`: explicit, idempotent sequential release of the single tracking lifetime.
- `DeviceChange(Revision)`: common record contract; revision groups changes from one applied scan.
- `DeviceAdded(Revision, Device)`: membership addition or initial replay of the published object.
- `DeviceRemoved(Revision, Device)`: correlates removal to the originally published object.
- `DeviceUpdated(Revision, Device)`: makes changed last-known metadata on a retained object observable.
- `SnapshotComplete(Revision)`: marks replay completion, including empty membership, without a device payload.
- `DeviceChange.Revision`: readable/initializable scan grouping on every notification, including replay completion.
- `DeviceAdded.Device`: readable/initializable reference to the published member for a consumer projection.
- `DeviceRemoved.Device`: readable/initializable reference to the original member so removal correlates.
- `DeviceUpdated.Device`: readable/initializable reference to the retained member whose metadata changed.
- `IDeviceChangeListener.Changed`: payload-less reconciliation hint shared by all transports.
- `IDeviceChangeListener.Status`: diagnostic check that listener startup actually succeeded.
- `IDeviceChangeListener.Start()`: establishes the baseline and begins hints after callback assignment.
- `IDeviceChangeListener.Stop()`: stops hints without disposing the listener; never call inside its callback.
- `IDeviceChangeListener.Dispose()`: releases owned native listener resources.
