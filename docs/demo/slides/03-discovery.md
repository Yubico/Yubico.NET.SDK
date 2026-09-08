## Device discovery

![h:520](assets/L4-discovery.svg)

---

## Discovery API

```csharp
// Cached — the repository's current view
var keys = await YubiKeyManager.FindAllAsync();

// Fresh scan, or narrowed by transport
var scan = await YubiKeyManager.FindAllAsync(
    ConnectionType.SmartCard, forceRescan: true);
```

**Is `forceRescan` obsolete now that discovery is event-driven?** No — but its job
has narrowed to exactly one case:

| | Cache freshness | Need `forceRescan`? |
|---|---|---|
| **Monitoring on** | the monitor keeps it fresh | **no** — it is redundant |
| **Monitoring off** | first call scans, then the cache never updates | **yes** — the only way to refresh |

So it is the escape hatch for callers who want a one-shot look and never call
`StartMonitoring()`. If you are monitoring, stop passing it.

Discovery is **publish-first and degraded-state tolerant** — a key is published as soon
as it is enumerated, even if its metadata read has not succeeded. Canonical Rust
instead withholds publication until metadata is read, which is why `SerialNumber` can
arrive late here and not there.

<!-- Anchors: PublicAPI.Unshipped.txt:984-985;
     caching + "monitoring keeps cache fresh" src/Core/src/Devices/YubiKeyManager.cs:286-300;
     race-condition guidance docs/usage/device-discovery.md:202-209;
     publish-first docs/architecture/device-identity.md:93-96 -->
