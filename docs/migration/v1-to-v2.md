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

## Application Sections

### PIV

Use `Yubico.YubiKit.Piv` for PIV operations. Review authentication, PIN/PUK handling, key import/generation, certificate management, and APDU-level customization manually because lifecycle and security-sensitive buffer handling can differ between v1 and v2.

### FIDO2

Use `Yubico.YubiKit.Fido2` for FIDO2/WebAuthn operations. Review transport selection, PIN/UV flows, credential management, and authenticator state assumptions manually.

### OATH

Use `Yubico.YubiKit.Oath` for TOTP/HOTP credential management and code calculation. Review credential naming, secret handling, password flows, and time-source assumptions manually.

### YubiOTP

Use `Yubico.YubiKit.YubiOtp` for Yubico OTP configuration and slot operations. Review slot numbering, configuration flags, and write/update behavior manually.

### OpenPGP

Use `Yubico.YubiKit.OpenPgp` for OpenPGP card operations. Review key slots, PIN policy, management key behavior, and command-level assumptions manually.

### Security Domain

Use `Yubico.YubiKit.SecurityDomain` for SCP03 and security domain key management. Treat all secure channel, cryptographic key, and diversification migrations as manual until a specific high-confidence mapping exists.

### YubiHSM

Use `Yubico.YubiKit.YubiHsm` for YubiHSM 2 workflows. Review connector/session creation, authentication, object identifiers, capabilities, and command behavior manually.

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
