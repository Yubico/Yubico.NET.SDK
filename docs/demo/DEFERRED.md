# Deferred findings

Bugs, incorrectness, and inconsistencies encountered while building the v2 SDK
demo deck. **Nothing here has been fixed.** Per standing instruction, this
workload defers all repairs to a separate effort.

Repository pinned at `d04d59aae63981588f6dc047eb0d788b681a8b8d`.

| # | Severity | Location | Finding |
|---|---|---|---|
| 1 | medium | `benchmarks/Yubico.YubiKit.PerformanceBenchmarks/Program.cs:161,188,198,225,235` | The benchmark project **does not compile** at this SHA. Five call sites pass `CreateManagementSessionAsync(preferredConnection: ConnectionType.X)`, but that named parameter no longer exists. The current signature is `CreateManagementSessionAsync(SessionCreationOptions? options, CancellationToken)` (`src/Management/src/IYubiKeyExtensions.cs:101`); transport preference now travels via `SessionCreationOptions.PreferredConnectionType`. Error: `CS1739`. |
| 2 | low | `Yubico.YubiKit.sln` | `benchmarks/Yubico.YubiKit.PerformanceBenchmarks` is **not referenced by the solution** and is not built by `build.yml`. This is the root cause of finding 1: nothing compiles the project, so it bit-rotted silently when the applet factory signatures were consolidated. Consider adding it to a CI build-only lane. |
| 3 | low | `docs/usage/device-discovery.md:62-63` | Reads "both surfaces below use BCL types only", but only one surface now exists. Leftover wording from when `DeviceChanges` (`IObservable<DeviceEvent>`) sat alongside `WatchAsync`. The same file at line 126 correctly states "`WatchAsync` is the only device-change stream". |
| 4 | low | `docs/usage/device-discovery.md:80` | The sample `switch` has a `_ => "changed"` arm, implying a third `DeviceAction`. `DeviceAction` has exactly two members, `Added` and `Removed` (`src/Core/src/DeviceEvent.cs:19-23`, `src/Core/src/PublicAPI.Unshipped.txt:203-204`). The arm is unreachable and misleads readers into expecting a `Changed` event. Note the rust peer SDK *does* have a third `Changed` variant, which makes the confusion more likely, not less. |
| 5 | **high** | `src/Core/src/Transports/SmartCard/PscsConnectionKind.cs` | Public type name is misspelled: **`PscsConnectionKind`** — the `s` and `c` are transposed. It should be `PcscConnectionKind` (PC/SC, Personal Computer / Smart Card). Every neighbouring type spells it correctly: `PcscDevice`, `IPcscDevice`, `FindPcscDevices`, `PcscConnectionKindDetector`. Rated high because it is **shipped public API surface** — `src/Core/src/PublicAPI.Unshipped.txt:651,668,673-675` — reachable via `IPcscDevice.Kind`. It is still in `Unshipped.txt`, so renaming is cheap **now** and expensive after 2.0 GA. |

| 6 | medium | `docs/v2-highlights.md:40-41` | Claims "Every public API in v2 is `async`/`await`. There are **no synchronous wrappers anywhere**, and that's deliberate rather than half-finished." This is **false against shipped public API**. `YubiKeyManager.Shutdown()` is exactly a synchronous wrapper — `ShutdownAsync().GetAwaiter().GetResult()` (`src/Core/src/Devices/YubiKeyManager.cs:257`), public at `src/Core/src/PublicAPI.Unshipped.txt:987`. `ISmartCardConnection.BeginTransaction(CancellationToken)` (`:653`) is a synchronous public transport method that blocks on a `Task` (`src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs:102`). `docs/architecture/raw-access-tiers.md:158-159` contradicts the highlights doc directly, describing sync `Dispose` as blocking to drain. The sync members look deliberate; the documentation asserting they do not exist is what is wrong. |

## Severity scale

- **high** — actively misleads a reader or consumer of the API.
- **medium** — incorrect but self-evident, or confined to internal documentation.
- **low** — cosmetic, stale wording, or a minor inconsistency.

## Open API questions raised during deck review (2026-09-08)

Not defects. Design questions worth a decision, recorded so they are not lost.

| # | Area | Question |
|---|---|---|
| Q-A | `YubiKeyManager.FindAllAsync(forceRescan:)` | While monitoring is active the monitor keeps the cache fresh, so `forceRescan` is redundant; without monitoring it is the *only* refresh path (`src/Core/src/Devices/YubiKeyManager.cs:286-300`). Should `FindAllAsync` start monitoring implicitly, or should `forceRescan` be marked obsolete in favour of an explicit `RescanAsync`? Today a caller who never monitors and never passes `forceRescan` silently reads a cache that can be arbitrarily stale. |
| Q-B | `IYubiKey.DeviceId` | Raised: should this leave the public API? Current position is keep-as-diagnostic: the prefix encodes evidence tier (`pcsc:*`/`hid:*` for a lone interface, `ykphysical:*` only once grouping proved a physical key — `docs/architecture/device-identity.md:179-184`). D1 (`:61-65`) already rejects exposing the interface-set string. If it stays, the XML doc should state "diagnostic, not durable identity" on the member itself, not only in the architecture doc. |
| Q-C | `ISecurityDomainSession.GetKeyInfoAsync` | .NET abbreviates where every peer spells it out: Python `get_key_information()`, Swift `getKeyInformation()`, Android `getKeyInformation()`. Gratuitous naming divergence on a cross-SDK-visible operation. Still in `PublicAPI.Unshipped.txt:24`, so renaming is cheap now. |

## Async-strategy findings (2026-09-08)

Found while analysing the sync-vs-async trade for the deck. Not fixed.

| # | Severity | Location | Finding |
|---|---|---|---|
| 7 | **medium** | `src/Core/src/Protocols/Fido/Hid/FidoHidConnection.cs:32,52`; `src/Core/src/Protocols/Otp/Hid/OtpHidConnection.cs:32,53` | **`Task`-returning methods that are entirely synchronous and ignore their `CancellationToken`.** All four accept `CancellationToken cancellationToken = default` and never reference it; they call blocking `SetReport`/`GetReport` and return `Task.CompletedTask` / `Task.FromResult`. A caller who `await`s expecting to yield does not, and on a thread with a `SynchronizationContext` the UI blocks anyway. Cancellation *is* handled correctly one layer up (`FidoHidProtocol.cs:250-267` uses CTAP KEEPALIVE frames to poll the caller token and send `CTAPHID_CANCEL`), so the behaviour is safe — but the leaf signatures advertise a contract they do not implement. Either honour the token, or drop the parameter and return `ValueTask`. |
| 8 | low | `src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs:267` | `Task.Run(() => SCardTransmit(...), ct)` offloads a blocking native call per APDU exchange. Correct for keeping a UI thread free, but it is a thread-pool hop on every exchange, and the token can only cancel *before* the delegate starts — `SCardTransmit` itself is not interruptible. Worth documenting as a known cost rather than leaving readers to infer real async I/O. |
| 9 | low | `src/Core/src/Devices/YubiKeyManager.cs:257`; `src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs:102` | Two sync-over-async wrappers in public API: `Shutdown() => ShutdownAsync().GetAwaiter().GetResult()` and `BeginTransaction`'s `beginTask.GetAwaiter().GetResult()`. Both are the classic deadlock shape when a `SynchronizationContext` is present. Related to finding 6: `docs/v2-highlights.md:40` claims no synchronous wrappers exist anywhere. |
