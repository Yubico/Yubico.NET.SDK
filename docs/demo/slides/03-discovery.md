## Device discovery

![h:520](assets/L4-discovery.svg)

---

## Two operation models

**A — One-shot.** Ask, act, exit. No monitoring.

```csharp
var keys = await YubiKeyManager.FindAllAsync();
await using var piv = await keys[0].CreatePivSessionAsync();
// ... do the work, then exit
```

**B — Long-lived.** Start monitoring, react to arrivals for the process lifetime.

```csharp
YubiKeyManager.StartMonitoring();

await foreach (var e in YubiKeyManager.WatchAsync(ct))
{
    if (e.Action == DeviceAction.Added)
        await OnKeyInserted(e.Device);
}
```

Same `IYubiKey`, same sessions. The models differ only in **who keeps the cache fresh.**

---

## Which model serves whom

| | **A — One-shot** | **B — Monitored** |
|---|---|---|
| Who | CLI tools, scripts, CI, installers | Desktop apps, daemons, services, tray UIs |
| Lifetime | seconds | hours |
| Cache kept fresh by | **you**, via `forceRescan` | the monitor |
| `forceRescan: true` | needed on every re-poll | **redundant — drop it** |
| Cost when idle | none | one listener, no polling |

```csharp
// A — polling loop without monitoring: MUST force, or you re-read a stale cache
while (!ct.IsCancellationRequested)
{
    var keys = await YubiKeyManager.FindAllAsync(forceRescan: true);
    ...
    await Task.Delay(1000, ct);
}
```

> **The trap.** Model A without `forceRescan` scans once, then returns that first
> snapshot **forever**. It looks like it works, because the first call is correct.
> Either pass `forceRescan: true`, or switch to model B.

`ykman`'s CLI is model A. `yubikit-android` and `yubikit-swift` apps are model B.
Most .NET desktop consumers want B; most .NET tooling wants A.

---

## Discovery guarantees

Discovery is **publish-first and degraded-state tolerant** — a key is published as soon
as it is enumerated, even if its metadata read has not succeeded. Canonical Rust
instead withholds publication until metadata is read, which is why `SerialNumber` can
arrive late here and not there.

Both models inherit this. Neither model can promise a key that arrives *during* a scan
appears in that scan's result.

<!-- Anchors: FindAllAsync src/Core/src/PublicAPI.Unshipped.txt:984-985;
     caching + "monitoring keeps cache fresh" src/Core/src/Devices/YubiKeyManager.cs:286-300;
     monitoring surface PublicAPI.Unshipped.txt:986-992;
     race conditions docs/usage/device-discovery.md:202-209;
     publish-first docs/architecture/device-identity.md:93-96 -->
