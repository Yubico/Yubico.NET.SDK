## Going below the applet API: three tiers

| Tier | What it gives you | What you take on |
|---|---|---|
| **0 — Applet session** | applet semantics, feature gates, protocol state | nothing |
| **1 — Raw session** | APDU/CTAP framing, chaining, overlap refusal, SCP | payload semantics |
| **2 — Raw connection** | a pipe | *everything* |

```csharp
await using var piv  = await key.CreatePivSessionAsync();           // Tier 0
await using var raw  = await key.CreateRawSmartCardSessionAsync();  // Tier 1
await using var conn = await key.ConnectAsync<ISmartCardConnection>();
var rsp = await conn.TransmitAndReceiveAsync(rawBytes);             // Tier 2
```

Tier 1 does **not** select an applet or apply feature gates. At Tier 2 you own APDU
formatting, chaining, response correlation, CRC validation, keep-alive, sequencing, and
recovery from partial or cancelled exchanges.

Dropping a tier does **not** drop transport safety — raw sessions still obey
one-connection-per-key and one-session-per-connection.

> This is the supported answer for teammates hand-rolling APDUs in `ykman` today:
> there is a place to do that, and it is Tier 1, not Tier 2.

<!-- Anchors: docs/architecture/raw-access-tiers.md:6-16 (T0), :19-33 (T1),
     :136-145 (T2), :30-33 (safety still applies);
     src/Core/src/Devices/YubiKeyConnectionExtensions.cs:37,90,108;
     Tier 2 signature takes ReadOnlyMemory<byte> src/Core/src/PublicAPI.Unshipped.txt:655
     (Tier 1 takes ApduCommand, :535) -->
