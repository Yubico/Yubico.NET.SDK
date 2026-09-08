## Device discovery

![h:520](assets/L4-discovery.svg)

---

## Discovery API

```csharp
// Cached — the repository's current view
var keys = await YubiKeyManager.FindAllAsync();

// Force a fresh scan, or narrow by transport
var scan = await YubiKeyManager.FindAllAsync(
    ConnectionType.SmartCard, forceRescan: true); (REVIEW: Consider if this does not belong in the SDK at all any longer, now replaced by event-driven discovery)
```

Discovery is **publish-first and degraded-state tolerant** — a key is published as soon
as it is enumerated, even if its metadata read has not succeeded.

Canonical Rust instead withholds publication until metadata is read. That difference is
deliberate, and it is why `SerialNumber` can arrive late (next slide).

<!-- Anchors: PublicAPI.Unshipped.txt:984-985;
     docs/architecture/device-identity.md:93-96 -->
