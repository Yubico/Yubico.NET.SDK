# Yubico.YubiKit.Core

Core is the foundation every other module builds on. It discovers YubiKeys, opens and owns connections
over SmartCard (PC/SC) and HID, runs the ISO 7816-4 APDU pipeline with automatic command chaining,
implements Secure Channel Protocol (SCP03 and SCP11), and supplies the shared device metadata,
cryptography, and TLV types. It has no application of its own; install an application module and Core
comes with it.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10 on Windows, macOS, or Linux; Linux also needs PC/SC and udev rules ([Linux setup](../../docs/linux-setup.md)).
- A YubiKey 4 series, YubiKey 5 series, or Security Key series device.
- SmartCard (USB CCID or NFC), HID FIDO, and HID OTP transports. Which of these a given application uses is
  documented in that application's README.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Core --prerelease
```

You only need this line for device discovery, raw sessions, or SCP key parameters without an application
module. Every application package installs Core transitively.

## Getting started

```csharp
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Piv;

IYubiKey device = await YubiKeyManager.FindFirstAsync();
Console.WriteLine($"Serial {device.SerialNumber}: {device.AvailableConnections}");

foreach (var each in await YubiKeyManager.FindAllAsync())
{
    Console.WriteLine($"  {each.SerialNumber}: {each.AvailableConnections}");
}
```

One `IYubiKey` is one physical YubiKey, even when it exposes several interfaces at once. `SerialNumber` is
the durable identity; it is `null` on devices that do not report one, such as the Security Key series.
Discovery needs no PIN, touch, or open session. Later snippets assume these directives and a `device` obtained the same way;
`Yubico.YubiKit.Piv` is referenced only to show an application session.

## Common operations

### Open an application session

Application sessions are the intended path. Each module adds a `Create<Application>SessionAsync`
extension on `IYubiKey` that selects a transport, opens a connection it owns, and selects the applet:

```csharp
await using var piv = await device.CreatePivSessionAsync();
```

Pass a `SessionCreationOptions` to override the transport, establish SCP, or receive touch notifications;
see [User interaction](../../docs/usage/user-interaction.md).

### Select one device

```csharp
IYubiKey first = await YubiKeyManager.FindFirstAsync();
IYubiKey? smartCard = await YubiKeyManager.FindFirstOrDefaultAsync(
    device => device.SupportsConnection(ConnectionType.SmartCard));
```

`FindFirstAsync` throws `InvalidOperationException` when nothing matches; `FindFirstOrDefaultAsync` returns
`null`. Both are one-shot queries over the same cached discovery as `FindAllAsync`: they do not wait for a
key to be inserted, and "first" is not a stable ordering.

### Watch for devices being added and removed

```csharp
YubiKeyManager.StartMonitoring();

await foreach (DeviceEvent change in YubiKeyManager.WatchAsync())
{
    Console.WriteLine($"{change.Action}: serial {change.Device.SerialNumber}");
}
```

`WatchAsync` yields `Added` and `Removed` events computed from a repository diff, not raw OS notifications.
Subscription starts on first enumeration; events raised before that are not replayed.

### Send raw APDUs

When no application module models what you need, a raw session gives you framing, ownership, and
sequencing without applet checks. You own every protocol concern.

```csharp
await using ISmartCardConnection connection = await device.ConnectAsync<ISmartCardConnection>();
await using RawSmartCardSession raw = await RawSmartCardSession.CreateAsync(connection);

// firmwareVersion is the device's firmware, read via Management; it sets APDU formatting. Configure first.
raw.Configure(firmwareVersion);
await raw.SelectAsync(ApplicationIds.Piv);
ApduResponse response = await raw.TransmitAndReceiveAsync(
    new ApduCommand(0x00, 0xCB, 0x3F, 0xFF, requestData),
    throwOnError: false);

Console.WriteLine($"SW={response.SW:X4}, {response.Data.Length} bytes");
```

`RawFidoHidSession` and `RawOtpHidSession` are the HID equivalents, created with
`device.CreateRawFidoHidSessionAsync()` and `device.CreateRawOtpHidSessionAsync()`.

For expert raw SmartCard transactions, `BeginTransactionAsync` returns `IDisposable`. The built-in
scope also implements `IAsyncDisposable` at runtime; the interface does not promise that for custom
connections. End the transaction before disposing the connection:

```csharp
await using ISmartCardConnection connection = await device.ConnectAsync<ISmartCardConnection>();
IDisposable transaction = await connection.BeginTransactionAsync();
try
{
    // Perform serialized raw operations on connection here.
}
finally
{
    if (transaction is IAsyncDisposable asyncTransaction)
        await asyncTransaction.DisposeAsync();
    else
        transaction.Dispose();
}
```

On built-in SmartCard connections, async begin does not block the caller; synchronous begin and
scope disposal can wait on native work. A custom connection using the interface's default async
begin calls synchronous begin and can block until it is overridden.

The public expert `IHidDevice.ConnectToIOReports()` / `ConnectToFeatureReports()` methods open
legacy synchronous report connections even on macOS; they are not the built-in async connections
used by `device.ConnectAsync<TConnection>()`. `IHidConnection.GetReport()` / `SetReport(byte[])`
offer no cancellation token or native-async guarantee. The caller owns the direct connection;
avoid concurrent report calls and disposal. Legacy macOS IO teardown frees callback handles without
an acknowledged in-flight callback drain, so concurrent disposal may be unsafe. Synchronous
`ISmartCardConnection.BeginTransaction()` and transaction-scope `Dispose()` can also block on
native work; synchronous connection `Dispose()` is not uniformly nonblocking. For
method-by-method execution, ownership and evidence gaps, see
[retained synchronous expert boundaries](../../docs/architecture/raw-access-tiers.md#retained-synchronous-expert-boundaries).

In the development worktree, the built-in macOS FIDO connection uses asynchronous open,
awaited channel initialization and persistent native input delivery. Blocking output and
checked shutdown have connection-owned execution; uncertain native close retains the
physical claim. This does not make the public lower-level `IHidConnection` interface or
OTP/Windows/Linux HID routes asynchronous. The development worktree's read-only macOS
FIDO open/init/getInfo/dispose/reopen path has run on one connected 5.7.4 YubiKey, but
touch, removal and interrupted shutdown are not verified. The current development pin is
**`Yubico.NativeShims` `1.18.1-async.2`**, an unsigned, unpublished local preview requiring
a local feed; the prior selected-key result used that preview. An actual-key pending-receive
dispose/reopen test is still pending, not a claimed pass; see the
[async-boundaries status](../../docs/plans/yubikit-async-boundaries/00-status.md).

### Use a secure channel

Core owns the SCP key-parameter types; session factories establish the channel from them.

```csharp
using var keys = new StaticKeys(encKey, macKey, dekKey);
using var scp = new Scp03KeyParameters(KeyReference.Default, keys);

await using var raw = await device.CreateRawSmartCardSessionAsync(scp, firmwareVersion);
```

`Scp03KeyParameters.Default` carries the well-known factory keys for test devices. Manage the keys on the
device with `Yubico.YubiKit.SecurityDomain`.

## Constraints

- Choose one interface per connection with the typed `ConnectAsync<TConnection>()`. The parameterless
  overload is only for single-interface devices and throws on a composite key.
- A physical YubiKey admits one live connection across the interfaces discovery grouped together. A
  second attempt in the same process throws `ConnectionInUseException` until the first is disposed. The
  guard is process-local.
- One live session per connection. Dispose it before creating another over the same connection.
- Whoever creates a connection disposes it; use `await using`. A session from a `device.Create...` factory
  owns the hidden connection it opened. Built-in SmartCard connections request safe shutdown from their
  finalizer, but finalization is nondeterministic and is not a substitute for disposal.
- Sessions refuse overlapping operations. An exchange already in flight runs to completion.
- Raw `IConnection` I/O bypasses session and exchange guards. The built-in SmartCard connection additionally
  refuses overlapping raw native operations instead of queuing them. Never interleave raw I/O with a live
  session, and dispose and reopen after an interrupted exchange.
- Built-in SmartCard cancellation prevents dispatch when observed first. Once a native PC/SC call starts, its
  task remains pending until the call returns so caller-owned input remains borrowed safely. Zero sensitive
  caller-owned input only after the task is terminal. Async disposal waits for accepted work, transaction end,
  disconnect, and context release without blocking the caller. Custom implementations must provide their own
  drain and borrowed-memory lifetime guarantees; these are not automatic for every SDK connection.
- On built-in macOS FIDO, canceling a pending raw read may leave it occupying the overlap slot until a report
  or terminal wake; drain it or dispose the connection before another operation. Cancellation between reads
  does not immediately abort the native device protocol. Built-in macOS OTP calls already active at cancellation
  may finish successfully. Do not assume cancellation releases native resources early; await the operation and
  connection disposal. Other HID implementations may have different cancellation behavior.
- If native release cannot be proven, the physical-interface claim remains quarantined and a later open throws
  `UnrecoveredConnectionException` rather than treating the key as ordinarily busy.
- `ProtocolFactory` and the `IProtocol` family are internal. Use `Raw*Session.CreateAsync(connection)`.

## Security notes

- SCP key material is caller-owned. `StaticKeys` and the `Scp*KeyParameters` types are `IDisposable` and
  zero their contents on disposal; zero any source buffers you supplied.
- Never embed or log real SCP keys, and disable trace logging in production; APDU traces can contain
  sensitive payloads.
- `Tlv` and the collection from `TlvHelper.DecodeList` own buffers and must be disposed. The encode helpers
  do not clear your source buffers or their own returned encodings; for sensitive data, zero both in a
  `finally`. Full examples: [TLV processing](../../docs/usage/tlv-processing.md).
- `ApduCommand` stores a reference to your data, not a copy. Zero the source after transmission.

## Related

- Application modules: [Management](../Management/README.md), [PIV](../Piv/README.md),
  [FIDO2](../Fido2/README.md), [WebAuthn](../WebAuthn/README.md), [OATH](../Oath/README.md),
  [YubiOTP](../YubiOtp/README.md), [OpenPGP](../OpenPgp/README.md),
  [Security Domain](../SecurityDomain/README.md), [YubiHSM Auth](../YubiHsm/README.md).
- [Physical device model](../../docs/architecture/physical-device-model.md): one `IYubiKey` per key, transport selection, ownership.
- [Device discovery guarantees](../../docs/architecture/device-discovery-guarantees.md): grouping guarantees per platform.
- [Raw access tiers](../../docs/architecture/raw-access-tiers.md): applet sessions, raw sessions, raw connections.
- [Device discovery](../../docs/usage/device-discovery.md), [User interaction](../../docs/usage/user-interaction.md),
  and [TLV processing](../../docs/usage/tlv-processing.md).
- [Logging](../../docs/LOGGING.md): configure `YubiKitLogging.LoggerFactory` once at startup.
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.
