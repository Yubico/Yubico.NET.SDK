# YubiKey SDK v1 to YubiKit v2 Migration Guide

This document reflects the v2 SDK state on branch `yubikit` as of commit `e348013685d92a6a665cd0b8bd7e8b05850fddd5`. Later pull requests targeting `yubikit` update this guide incrementally.

This is an initial migration snapshot. It records high-confidence migration areas and marks low-level or behavior-sensitive cases for manual review.

## Package and Namespace Split

The v2 SDK uses `Yubico.YubiKit.*` packages and namespaces instead of the v1 `Yubico.YubiKey.*` and `Yubico.Core` shape.

High-level package guidance:

- Core device, transport, connection, APDU, logging, and platform infrastructure move to `Yubico.YubiKit.Core`.
- Management interfaces move to `Yubico.YubiKit.Management`.
- Application features are split by applet: `Yubico.YubiKit.Piv`, `Yubico.YubiKit.Fido2`, `Yubico.YubiKit.Oath`, `Yubico.YubiKit.YubiOtp`, `Yubico.YubiKit.OpenPgp`, `Yubico.YubiKit.SecurityDomain`, and `Yubico.YubiKit.YubiHsm`.

Treat package and namespace changes as assisted migrations until the specific v1 type or member mapping is present in `v1-to-v2-map.yml`.

## Device Discovery and Connections

V2 separates device discovery, device identity, connection factories, and protocol handling more explicitly than v1. Migration work usually starts by replacing direct v1 device enumeration or static discovery calls with the v2 device repository and connection abstractions in `Yubico.YubiKit.Core`.

Review code that assumes:

- One global device discovery entry point.
- A fixed transport for all operations.
- A device object that directly owns all applet operations.
- Synchronous connection setup for operations that are async in v2.

### HID Listener Callbacks

V1 low-level HID listeners used `Yubico.Core.Devices.Hid.HidDeviceListener.Arrived` and `Removed` events (`EventHandler<HidDeviceEventArgs>`) carrying the affected `IHidDevice`. V1 YubiKey-level monitoring used `YubiKeyDeviceListener.Arrived`/`Removed` and the `YubiKeyDevice.FindAll()` cache. In v2, the low-level `Yubico.YubiKit.Core.Transports.Hid.HidDeviceListener.DeviceEvent` callback is `Action<HidDeviceRescanHint>?`: a diagnostic rescan hint with `HidDeviceChangeKind` plus optional platform identifier/path. It is not authoritative physical-device state. Applications that need real YubiKey arrivals and removals should use `YubiKeyManager.DeviceChanges`, which is emitted after the device repository rescans and diffs the discovered device set.

Earlier v2 alphas could make Rx-style `Subscribe(Action<T>)` and query operators appear transitively. Current builds expose only the BCL `IObservable<T>` surface. If your migration code uses lambda subscriptions or operators such as `Where`, add a direct `System.Reactive` reference. `YubiKeyManager.WatchAsync(cancellationToken)` (`IAsyncEnumerable<DeviceEvent>`) is available as an ergonomic alternative to `DeviceChanges`; both are backed by the same dependency-free broadcaster. See `hid-listener-rescan-hints` in `v1-to-v2-map.yml`.

### HID Interface Type Classification

V1's `IHidDevice.UsagePage` (`HidUsagePage`: `Unknown`, `Fido`, `Keyboard`) classified a device from the HID UsagePage field alone; `Keyboard = 1` was actually the Generic Desktop usage page, not specifically a keyboard, so v1 code paired it with `Usage` to detect the YubiKey OTP interface. V2 classifies from the full UsagePage+Usage pair through `IHidDevice.InterfaceType` (`HidInterfaceType`: `Unknown`, `Fido`, `Otp`). Migrate `UsagePage == HidUsagePage.Fido` to `InterfaceType == HidInterfaceType.Fido`, and `UsagePage == HidUsagePage.Keyboard` (v1's OTP-interface check) to `InterfaceType == HidInterfaceType.Otp`. See `hid-usage-page-to-interface-type` in `v1-to-v2-map.yml`. Most applications should prefer `YubiKeyManager`/`IYubiKey` discovery and `ConnectionType` over raw `IHidDevice` interface classification.

### Secure Channel (SCP) Session Construction

`ProtocolFactory`, `ISmartCardProtocol`, and `PcscProtocolScp` are internal implementation machinery in v2.
Most applications should pass SCP key parameters to the applet's `Create{Applet}SessionAsync(...)` entry point.
Advanced callers that intentionally work below applet semantics can establish SCP through
`RawSmartCardSession`:

```csharp
using ScpKeyParameters scpParameters = LoadScpParametersFromSecureStorage();
await using RawSmartCardSession raw = await yubiKey.CreateRawSmartCardSessionAsync(
    scpParameters,
    firmwareVersion,
    protocolConfiguration,
    cancellationToken);
```

Raw SCP configuration is applied before channel establishment and cannot be changed afterward. The raw session
reuses Core's SCP processor and exchange guard. Do not log real keys or sensitive APDU payloads.
See [Raw Access Tiers](../architecture/raw-access-tiers.md) for ownership and recovery rules.

### Device Identity: Serial Number and Correlation

V1 device discovery read the hardware serial synchronously while building each returned device object (`IYubiKeyDeviceInfo.SerialNumber`), so it was reliably present immediately unless the device class does not report one. V2 now exposes `IYubiKey.SerialNumber` (`int?`) directly on the physical device object without a Management session, but it is populated by a background discovery read: it is `null` until that read succeeds, can remain `null` indefinitely (a failed read, an exhausted discovery read budget, or a serial-less device class such as the Security Key series), and once non-null never reverts to `null`. Do not assume `SerialNumber` is populated immediately after `FindAllAsync` returns; use a Management session (see the Device Info recipe below) when a serial is required synchronously and reliably. `IYubiKey.SameDeviceAs(IYubiKey)` returns a three-valued `DeviceCorrelation` (`Same`/`Different`/`Unknown`) for comparing two device references: `Unknown` means "cannot correlate," not "different," and must not be coerced to either outcome for collection equality or deduplication. See `device-serial-number-property` in `v1-to-v2-map.yml`.

## Session Lifecycle

V2 application sessions are applet-specific and commonly own connection/protocol state. Prefer the v2 session factory or constructor pattern documented by each applet package rather than carrying v1 session setup forward mechanically.

Migration review is required for code that:

- Constructs sessions directly from v1 device objects.
- Relies on synchronous disposal where v2 uses async cleanup.
- Reuses one connection across multiple applet sessions.
- Depends on implicit transport selection.

Every v2 applet package exposes an `IYubiKey.Create{Applet}SessionAsync(...)` extension method (for example `CreatePivSessionAsync`, `CreateFidoSessionAsync`, `CreateOathSessionAsync`, `CreateOpenPgpSessionAsync`, `CreateSecurityDomainSessionAsync`, `CreateHsmAuthSessionAsync`, `CreateYubiOtpSessionAsync`) and `Yubico.YubiKit.Management` exposes `CreateManagementSessionAsync`. These are the preferred v2 entry points: they open the correct connection/transport and construct the session in one call. Treat the underlying connection handling, transport selection defaults, and SCP key parameter plumbing as assisted rather than automatic until the specific v1 call site is reviewed.

## Common Migration Recipes

These examples show the shape of common v1 code and the closest v2 pattern. They are intentionally small and source-backed. Treat examples that write applet state, credential material, keys, PINs, PUKs, access codes, or slot configuration as human-reviewed migrations even when the session or member mapping is clear.

### Device Discovery

V1 discovery returned v1 device objects synchronously:

```csharp
using Yubico.YubiKey;

var devices = YubiKeyDevice.FindByTransport(Transport.All).ToList();
var device = devices.First();
```

V2 discovery is async and returns physical `IYubiKey` instances with explicit available connections:

```csharp
using Yubico.YubiKit.Core.Devices;

var devices = await YubiKeyManager.FindAllAsync(
    ConnectionType.All,
    forceRescan: true,
    cancellationToken: cancellationToken);
var device = devices.First();
```

Migration notes:

- Replace synchronous enumeration assumptions with async flow and cancellation support.
- V2 models one physical YubiKey with one or more available connections. Avoid assuming that the discovered object is a single transport handle.
- If v1 code filtered by `Transport.HidFido`, `Transport.UsbSmartCard`, or NFC-specific behavior, review the v2 `ConnectionType` choice rather than applying a mechanical enum rename.

### Device Info

V1 code often read device metadata directly from the discovered device:

```csharp
using Yubico.YubiKey;

var device = YubiKeyDevice.FindAll().First();
Console.WriteLine(device.SerialNumber);
Console.WriteLine(device.FirmwareVersion);
```

V2 exposes detailed device information through the Management package:

```csharp
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management;

var device = (await YubiKeyManager.FindAllAsync(
    ConnectionType.All,
    forceRescan: false,
    cancellationToken: cancellationToken)).First();
await using var session = await device.CreateManagementSessionAsync(
    cancellationToken: cancellationToken);

var info = await session.GetDeviceInfoAsync(cancellationToken);
Console.WriteLine(info.SerialNumber);
Console.WriteLine(info.VersionName);
```

Migration notes:

- Use a Management session when you need rich device metadata, capability flags, form factor, FIPS state, or configuration state.
- Some v1 metadata properties moved into richer v2 fields. For example, v1 `FirmwareVersion` often corresponds to `DeviceInfo.FirmwareVersion` for comparisons and `DeviceInfo.VersionName` for display.
- Reuse one session for multiple Management operations instead of creating repeated one-shot sessions.
- Configuration changes are persistent and may reboot the device; keep read-only device-info migrations separate from configuration migrations.

### Applet Session Creation

V1 applet sessions were commonly constructed directly from the v1 device object:

```csharp
using Yubico.YubiKey.Piv;

using var piv = new PivSession(device);
```

V2 uses async applet-specific extension methods on `IYubiKey`:

```csharp
using Yubico.YubiKit.Piv;

await using var piv = await device.CreatePivSessionAsync(
    cancellationToken: cancellationToken);
```

Migration notes:

- Prefer `Create{Applet}SessionAsync(...)` over manually opening a connection and constructing the session.
- Use `await using` because v2 sessions and owned connections are async-disposable.
- Review transport selection for applets that can use more than one connection type, especially FIDO2 and YubiOTP.

### PIV: Generate a Key Pair

V1 generated a PIV key pair through `PivSession.GenerateKeyPair(...)`:

```csharp
using Yubico.YubiKey.Piv;

using var piv = new PivSession(device);
piv.KeyCollector = keyCollector;

var publicKey = piv.GenerateKeyPair(
    PivSlot.Authentication,
    PivAlgorithm.EccP256,
    PivPinPolicy.Default,
    PivTouchPolicy.Default);
```

V2 uses async PIV operations and explicit caller-owned sensitive buffers:

```csharp
using System.Security.Cryptography;
using Yubico.YubiKit.Piv;

await using var piv = await device.CreatePivSessionAsync(
    cancellationToken: cancellationToken);

byte[] managementKey = GetManagementKeyBytes();
try
{
    await piv.AuthenticateAsync(managementKey, cancellationToken);

    var publicKey = await piv.GenerateKeyAsync(
        PivSlot.Authentication,
        PivAlgorithm.EccP256,
        PivPinPolicy.Default,
        PivTouchPolicy.Default,
        cancellationToken);
}
finally
{
    CryptographicOperations.ZeroMemory(managementKey);
}
```

Migration notes:

- This is a state-mutating operation and requires management-key authentication in v2.
- Replace v1 key-collector assumptions with explicit credential collection and zeroing around the v2 call site.
- Review algorithm, slot, PIN policy, and touch policy values manually; names and defaults are not guaranteed to be one-to-one.

### FIDO2: Read Authenticator Info

V1 exposed authenticator info from the constructed session:

```csharp
using Yubico.YubiKey.Fido2;

using var fido = new Fido2Session(device);
var info = fido.AuthenticatorInfo;
```

V2 reads fresh authenticator info asynchronously:

```csharp
using Yubico.YubiKit.Fido2;

var info = await device.GetFidoInfoAsync(cancellationToken);
```

For several FIDO2 operations, keep the session open:

```csharp
await using var fido = await device.CreateFidoSessionAsync(
    cancellationToken: cancellationToken);

var info = await fido.GetInfoAsync(cancellationToken);
```

Migration notes:

- V2 does not treat authenticator info as a cached session property; call `GetInfoAsync(...)` when fresh data matters.
- USB FIDO normally uses HID FIDO first. If v1 code depended on smart-card FIDO2 or SCP, review the v2 `preferredConnection` and SCP parameters explicitly.
- Operations such as make credential, get assertion, reset, and credential management still need flow-specific review for PIN/UV, user presence, and extension behavior.

### OATH: Add and Calculate Credentials

V1 OATH code used synchronous credential APIs and string-based secrets:

```csharp
using Yubico.YubiKey.Oath;

using var oath = new OathSession(device);
oath.KeyCollector = keyCollector;

var credential = new Credential
{
    Issuer = "Yubico",
    AccountName = "alice@example.com",
    Type = CredentialType.Totp,
    Secret = "JBSWY3DPEHPK3PXP",
    Digits = 6
};

oath.AddCredential(credential);
var codes = oath.CalculateAllCredentials();
```

V2 separates credential data, async storage, and async calculation:

```csharp
using Yubico.YubiKit.Oath;

await using var oath = await device.CreateOathSessionAsync(
    cancellationToken: cancellationToken);

using var credentialData = CredentialData.ParseUri(
    "otpauth://totp/Yubico:alice@example.com?secret=JBSWY3DPEHPK3PXP&issuer=Yubico&algorithm=SHA1&digits=6&period=30");

await oath.PutCredentialAsync(credentialData, cancellationToken: cancellationToken);
var codes = await oath.CalculateAllAsync(cancellationToken: cancellationToken);
```

Migration notes:

- `CredentialData.ParseUri(...)` decodes the OATH URI into v2 credential data. If you construct `CredentialData` manually, v2 expects decoded secret bytes in `CredentialData.Secret`; decode and zero caller-owned secret material when appropriate.
- If the OATH applet is password-protected, derive and validate the access key explicitly with byte buffers that can be zeroed.
- Adding, deleting, renaming, or setting keys mutates persistent OATH state and should be reviewed manually.

### YubiOTP: HMAC-SHA1 Challenge-Response

V1 used OTP operation builders for slot programming and calculation:

```csharp
using Yubico.YubiKey.Otp;

using var otp = new OtpSession(device);

otp.ConfigureChallengeResponse(Slot.LongPress)
    .UseHmacSha1()
    .UseKey(hmacKey)
    .UseSmallChallenge()
    .Execute();

var response = otp.CalculateChallengeResponse(Slot.LongPress)
    .UseYubiOtp(false)
    .UseChallenge(challenge)
    .GetDataBytes();
```

V2 uses explicit slot configuration objects and async session methods:

```csharp
using System.Security.Cryptography;
using Yubico.YubiKit.YubiOtp;

await using var otp = await device.CreateYubiOtpSessionAsync(
    cancellationToken: cancellationToken);

try
{
    using var config = new HmacSha1SlotConfiguration(hmacKey);
    config.UseShortChallenge();

    await otp.PutConfigurationAsync(
        Slot.Two,
        config,
        cancellationToken: cancellationToken);

    var response = await otp.CalculateHmacSha1Async(
        Slot.Two,
        challenge,
        cancellationToken);
}
finally
{
    CryptographicOperations.ZeroMemory(hmacKey);
    CryptographicOperations.ZeroMemory(challenge);
}
```

Migration notes:

- Slot mapping is semantic, not textual: v1 `ShortPress` and `LongPress` map to v2 `Slot.One` and `Slot.Two` respectively.
- Programming a slot overwrites persistent device state. Prefer slot 2 for examples and tests unless a human explicitly chooses otherwise.
- Review touch, access-code, short-challenge, update, and NDEF behavior manually before migrating production slot code.

### PIV: Enable PIN-Only Management Key Mode

V1 set PIN-only mode through the session, collecting the PIN and management key via `KeyCollector`:

```csharp
using Yubico.YubiKey.Piv;

using var piv = new PivSession(device);
piv.KeyCollector = keyCollector;

piv.SetPinOnlyMode(PivPinOnlyMode.PinProtected, PivAlgorithm.Aes192);
```

V2 requires the management key to already be authenticated, and takes the PIN and management key explicitly:

```csharp
using System.Security.Cryptography;
using Yubico.YubiKit.Piv;

await using var piv = await device.CreatePivSessionAsync(
    cancellationToken: cancellationToken);

byte[] managementKey = GetManagementKeyBytes();
byte[] pin = GetPinBytes();
try
{
    await piv.AuthenticateAsync(managementKey, cancellationToken);
    await piv.SetPinOnlyModeAsync(
        PivPinOnlyMode.PinProtected,
        pin,
        managementKey,
        cancellationToken);
}
finally
{
    CryptographicOperations.ZeroMemory(managementKey);
    CryptographicOperations.ZeroMemory(pin);
}
```

Migration notes:

- Only `PivPinOnlyMode.PinProtected` can be newly enabled in v2. V1's `PinDerived` mode is a deprecated, weaker mechanism; v2 can still detect and recover an existing PIN-derived configuration (`GetPinOnlyModeAsync`/`RecoverPinOnlyModeAsync`) but cannot enable a new one.
- The caller must authenticate the management key explicitly before calling `SetPinOnlyModeAsync`; v1's `KeyCollector` handled this implicitly.
- Enabling blocks the PUK and is state-mutating; treat this as a human-reviewed migration like other PIV write operations.

### OATH: Check Password Protection and Retry a Locked Operation

V1 exposed a persistent `IsPasswordProtected` flag and relied on `KeyCollector` for transparent retry:

```csharp
using Yubico.YubiKey.Oath;

using var oath = new OathSession(device);
oath.KeyCollector = keyCollector;

if (oath.IsPasswordProtected)
{
    var codes = oath.CalculateAllCredentials();
}
```

V2 exposes the same persistent signal and an explicit, module-appropriate retry helper instead of a global `KeyCollector`:

```csharp
using Yubico.YubiKit.Oath;

await using var oath = await device.CreateOathSessionAsync(
    cancellationToken: cancellationToken);

if (oath.IsPasswordProtected)
{
    var codes = await oath.AuthenticateAndRetryAsync(
        ct => oath.CalculateAllAsync(cancellationToken: ct),
        ct => GetPasswordBytesAsync(ct),
        cancellationToken);
}
```

Migration notes:

- `IsPasswordProtected` reflects whether the device has a password configured at all, independent of `IsLocked`'s per-session unlock state.
- `AuthenticateAndRetryAsync` retries the wrapped operation exactly once after a successful `ValidateAsync`; it is not an unbounded retry loop like v1's `KeyCollector`.
- Catch the dedicated `OathException` and branch on `OathException.Reason` (`Locked` or `WrongPassword`) instead of v1's `SecurityException`.

## Application Sections

### PIV

Use `Yubico.YubiKit.Piv` for PIV operations. Review authentication, PIN/PUK handling, key import/generation, certificate management, and APDU-level customization manually because lifecycle and security-sensitive buffer handling can differ between v1 and v2.

PIN-only management-key mode (`IPivSession.GetPinOnlyModeAsync`/`SetPinOnlyModeAsync`/`RecoverPinOnlyModeAsync`) and typed CHUID/CCC/AdminData/KeyHistory data objects (`Yubico.YubiKit.Piv.DataObjects`) were restored after an initial v2 gap; see `piv-pin-only-mode` and `piv-typed-data-objects` in `v1-to-v2-map.yml`. Enabling a new PIN-derived (as opposed to PIN-protected) management key is not supported in v2.

### FIDO2

Use `Yubico.YubiKit.Fido2` for FIDO2/WebAuthn operations. Review transport selection, PIN/UV flows, credential management, and authenticator state assumptions manually.

`AuthenticatorInfo.RawData`, `BioEnrollment.FingerprintSensorInfo.RawData`, `BioEnrollment.EnrollmentSampleResult.RawData`, `CredentialManagement.CredentialMetadata.RawData`, and `CredentialManagement.RelyingPartyInfo.RawData` each preserve the complete original CBOR-encoded authenticator response for that type. This is a new v2-only forward-compatibility escape hatch for authenticator response fields this SDK version does not yet model, not a substitute for the typed properties on the same objects; see `fido2-raw-response-envelopes` in `v1-to-v2-map.yml`.

### WebAuthn

Use `Yubico.YubiKit.WebAuthn` for the higher-level W3C WebAuthn API. It is a new package built on top of `Yubico.YubiKit.Fido2` (`IFidoSession`); v1 had no equivalent package. `WebAuthnClient` accepts an optional `ICredentialPrompt` (`Yubico.YubiKit.Core.Credentials`) that supplies a PIN on demand instead of requiring `pinBytes` up front, and owns a bounded retry loop instead of v1 FIDO2 code's global, unbounded `KeyCollector` pattern; see `webauthn-credential-prompt` in `v1-to-v2-map.yml`.

### OATH

Use `Yubico.YubiKit.Oath` for TOTP/HOTP credential management and code calculation. Review credential naming, secret handling, password flows, and time-source assumptions manually.

`IOathSession.IsPasswordProtected` (device-password state independent of session unlock state), `AuthenticateAndRetryAsync` (module-appropriate authenticate-and-retry), and the dedicated `OathException`/`OathFailureReason` type were restored after an initial v2 gap; see `oath-password-protection-state`, `oath-authenticate-and-retry`, and `oath-exception` in `v1-to-v2-map.yml`.

### YubiOTP

Use `Yubico.YubiKit.YubiOtp` for Yubico OTP configuration and slot operations. Review slot numbering, configuration flags, and write/update behavior manually.

A keyboard-layout-aware `StaticPasswordSlotConfiguration(string, KeyboardLayout)` constructor and Yubico-OTP-algorithm challenge-response (`YubicoOtpChallengeResponseSlotConfiguration`/`CalculateYubicoOtpAsync`) were restored after an initial v2 gap; see `yubiotp-static-password-keyboard` and `yubiotp-yubico-otp-challenge-response` in `v1-to-v2-map.yml`. HMAC-SHA1 and Yubico OTP key inputs of invalid length now fail before any device I/O instead of being silently hashed or padded.

### OpenPGP

Use `Yubico.YubiKit.OpenPgp` for OpenPGP card operations. Review key slots, PIN policy, management key behavior, and command-level assumptions manually.

PIN verification failures throw a dedicated `OpenPgpInvalidPinException` with a typed `RetriesRemaining`; see `openpgp-exception` in `v1-to-v2-map.yml`.

### Security Domain

Use `Yubico.YubiKit.SecurityDomain` for SCP03 and security domain key management. Treat all secure channel, cryptographic key, and diversification migrations as manual until a specific high-confidence mapping exists.

Secure-channel handshake/authentication failures (during session creation or post-reset reinitialization) throw a dedicated `SecureChannelException` that preserves the original failure as `InnerException`; see `securitydomain-exception` in `v1-to-v2-map.yml`. Per-operation failures after a channel is open are unchanged.

### YubiHSM

Use `Yubico.YubiKit.YubiHsm` for YubiHSM 2 workflows. Review connector/session creation, authentication, object identifiers, capabilities, and command behavior manually.

A dedicated `HsmAuthRetryException.RetriesRemaining` and an `HsmAuthSession.OnTouchRequired` callback were restored after an initial v2 gap; see `yubihsm-retry-exception` and `yubihsm-touch-notify` in `v1-to-v2-map.yml`. `HsmAuthCredential.Counter` was hardware-verified and renamed to `RetriesRemaining` to match v1's "retries remaining before deletion" semantics; see `yubihsm-credential-retries-remaining-rename`.

Credential passwords moved from `string` to UTF-8 `ReadOnlyMemory<byte>` across nine `IHsmAuthSession`/`HsmAuthSession` members (parameters renamed with the `...Utf8` suffix), closing a v1 regression rather than introducing one, since v1's equivalent path already used byte-based passwords; see `yubihsm-credential-password-bytes` in `v1-to-v2-map.yml`.

## Manual Low-Level Command Cases

Manual migration is required for code that builds APDUs, parses raw responses, relies on status-word behavior, preserves unknown protocol fields, or sends vendor-specific commands directly. These cases should be migrated against v2 command/session abstractions where possible. If direct command access remains necessary, verify byte-level behavior against both v1 and v2 sources.

## Automation Note

Migration documentation is maintained by automation on the `yubikit` branch. Pull requests targeting `yubikit` receive migration-impact preview comments. Pushes to `yubikit` open documentation update pull requests and advance `docs/migration/.state.yml` only after migration artifacts are updated.

Weekly scheduled reconciliation requires a wrapper workflow on the repository default branch, because GitHub only runs scheduled workflows from the default branch. The wrapper checks out `yubikit`, runs the same reconciliation logic, and opens documentation pull requests back into `yubikit`.

## How This Document Grows

This guide is intended to mature over the v2 development cycle rather than be written in one final pass.

1. Pull requests targeting `yubikit` get preview comments that identify migration impact without editing files.
2. Merges into `yubikit` trigger documentation update pull requests for the newly analyzed commit range.
3. Weekly reconciliation catches missed or stale migration guidance.
4. Monthly synthesis reorganizes accumulated notes into clearer release-ready sections.

The human review responsibility is intentionally narrow: review the generated migration documentation pull requests for truthfulness, confidence level, and usefulness. The automation should preserve uncertainty as manual-review items instead of inventing mappings.

## Release Readiness Tracker

This initial snapshot is not release-complete. Future automated updates should improve these areas:

- Core/device discovery: package, namespace, physical device, and transport model guidance.
- Session lifecycle: direct v1 constructors to v2 async factories and async disposal.
- Applet recipes: before/after examples for common PIV, FIDO2, OATH, YubiOTP, OpenPGP, Security Domain, and YubiHSM tasks.
- Manual migration cases: raw APDU, custom command classes, exception behavior, and security-sensitive credential flows.
- Tooling foundation: structured map entries precise enough to support a future scanner or analyzer.
