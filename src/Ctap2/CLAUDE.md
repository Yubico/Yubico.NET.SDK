# CLAUDE.md - Ctap2 Module

This file provides Claude-specific guidance for working with the CTAP2 module. **Read the root [CLAUDE.md](../../CLAUDE.md) first** for general repository patterns.

## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented here
> - **New features** (e.g., new CTAP commands, extensions) should include usage examples
> - **Breaking changes** require updates with migration guidance

## Module Context

### Naming boundaries

- `Yubico.YubiKit.Ctap2` is the namespace, assembly, and package; `Ctap2Session` and
  `ICtap2Session` expose CTAP2 operations. `CreateCtap2SessionAsync` opens a session and
  `GetCtap2InfoAsync` is the one-shot capability read.
- FIDO is the shared authenticator/application identity: keep `ApplicationIds.Fido`,
  `DeviceCapabilities.Fido2`, `UserPresenceApplications.Fido2`, and FIDO2 PIN prompts.
- CTAPHID is shared framing, not synonymous with CTAP2. Keep Core `Fido` connection/protocol
  names and `RawCtapHidSession`; backends call `IFidoHidProtocol.SendCommandAsync`.
- WebAuthn is the higher-level client/ceremony API. Keep standardized strings (`FIDO_2_0`,
  `FIDO_2_1_PRE`, `FIDO_2_1`, `FIDO_2_3`, `U2F_V2`, `fido-u2f`, `WebAuthn PRF`) unchanged.
- `CtapCommand`, `CtapStatus`, and `CtapException` belong directly to `Yubico.YubiKit.Ctap2`,
  even though their files remain in the module-local `Ctap/` source folder. Touch operation IDs use `UserPresenceOperations.Ctap2`.

The module exposes CTAP2 credential operations and selected CTAP 2.1/2.3 features. Revision advertisement,
optional capabilities, configured state, token permissions and firmware metadata are separate checks;
none alone proves per-device support or standards conformance. The session sends/parses protocol data,
not RP registration/sign-in verification. See the README for the complete protocol-demo/RP boundary.

**Transports Supported:**
- FIDO HID protocol over USB (primary transport)
- SmartCard (CCID) over NFC or USB when the connected authenticator exposes the FIDO AID
- USB SmartCard on firmware 5.8.0+; older USB-connected YubiKeys must use HID FIDO
- NFC SmartCard remains allowed when the current PC/SC connection reports `Transport.Nfc`; Core classifies PC/SC YubiKey transport from ATR evidence, not from model NFC capability

> **Important:** HID FIDO is the primary USB CTAP2 interface.
> USB SmartCard CTAP2 is also supported on firmware 5.8.0+ when the FIDO AID is exposed;
> older USB-connected YubiKeys must use `IFidoHidConnection`.
> The firmware gate runs after SmartCard FIDO AID selection because FIDO firmware is obtained from
> CTAP GetInfo on this transport.
> FIDO GetInfo can report a `0.x` sentinel firmware (for example `0.0.1`) on devices whose real
> firmware is available through Management; firmware gates use `Feature.IsSupportedByFirmware(...)`
> so `Major == 0` sentinel firmware is treated as modern.
> A grouped physical YubiKey admits one live SDK connection across all known interfaces. A second
> session is refused with `ConnectionInUseException` until the current session disposes its owned
> connection. CTAP2/WebAuthn selects HID FIDO when exposed, otherwise SmartCard, and never switches
> transport after a connection failure.

## Architecture Overview

```
Yubico.YubiKit.Ctap2/
├── src/
│   ├── Ctap2Session.cs              # Main session class (entry point)
│   ├── ICtap2Session.cs             # Session interface
│   ├── AuthenticatorInfo.cs        # GetInfo response parsing
│   ├── IYubiKeyExtensions.cs       # Extension methods for IYubiKey
│   ├── Backend/                    # Transport backends
│   │   ├── ICtap2Backend.cs
│   │   ├── HidBackend.cs           # HID transport
│   │   └── SmartCardBackend.cs     # SmartCard/CCID transport
│   ├── Cbor/                       # CBOR serialization
│   │   ├── CtapRequestBuilder.cs   # Fluent CBOR request builder
│   │   └── CtapResponseParser.cs   # CBOR response parsing utilities
│   ├── Cose/                       # Public-key carriers, including experimental ARKG seed keys
│   ├── Arkg/                       # Experimental offline derivation convenience
│   ├── Config/                     # authenticatorConfig; per-subcommand advertised support
│   │   ├── ConfigSubCommand.cs
│   │   └── AuthenticatorConfig.cs
│   ├── CredentialManagement/       # Credential enumeration/management
│   │   ├── CredManagementSubCommand.cs
│   │   ├── CredentialEnumerationResult.cs
│   │   ├── CredentialManagement.cs
│   │   ├── CredentialManagementModels.cs
│   │   ├── RelyingPartyEnumerationResult.cs
│   │   └── StoredCredentialInfo.cs
│   ├── Credentials/                # MakeCredential/GetAssertion types
│   │   ├── AuthenticatorData.cs
│   │   ├── AttestedCredentialData.cs
│   │   ├── GetAssertionOptions.cs
│   │   ├── MakeCredentialOptions.cs
│   │   ├── MakeCredentialResponse.cs
│   │   ├── GetAssertionResponse.cs
│   │   └── PublicKeyCredentialTypes.cs
│   ├── Crypto/                     # Offline encrypted GetInfo metadata helpers
│   │   └── EncryptedMetadataDecryptor.cs
│   ├── Ctap/                       # CTAP protocol types
│   │   ├── CtapCommand.cs
│   │   ├── CtapStatus.cs
│   │   └── CtapException.cs
│   ├── Extensions/                 # WebAuthn/CTAP extension types
│   │   ├── Shared/                 # ExtensionBuilder, identifiers, output parser
│   │   ├── CredProtect/            # credProtect policy
│   │   ├── CredBlob/               # credBlob inputs and outputs
│   │   ├── HmacSecret/             # hmac-secret inputs and outputs
│   │   ├── LargeBlob/              # Legacy largeBlob convenience output decoder
│   │   ├── MinPinLength/           # minPinLength inputs and outputs
│   │   └── PreviewSign/            # previewSign types and canonical CBOR encoder
│   ├── BioEnrollment/              # Fingerprint APIs; known standard wire/schema limitations
│   │   ├── BioEnrollmentSubCommand.cs
│   │   ├── BioEnrollmentModels.cs
│   │   └── FingerprintBioEnrollment.cs
│   ├── LargeBlobs/                 # Large blob storage
│   │   ├── LargeBlobData.cs        # Entry/Array types
│   │   └── LargeBlobStorage.cs     # Storage API
│   └── Pin/                        # PIN/UV authentication
│       ├── IPinUvAuthProtocol.cs
│       ├── PinUvAuthProtocolV1.cs
│       ├── PinUvAuthProtocolV2.cs
│       ├── ClientPin.cs
│       └── ClientPinSubCommand.cs   # Includes PinUvAuthTokenPermissions
└── tests/
    ├── Yubico.YubiKit.Ctap2.UnitTests/
    └── Yubico.YubiKit.Ctap2.IntegrationTests/
```

## Key Patterns

### Session Creation

`ApplicationSession` prepares the HID channel before GetInfo. `Ctap2Session` calls `SelectApplicationAsync`
only on `SmartCardBackend`; neither `ICtap2Backend` nor `HidBackend` exposes selection. SCP on HID is rejected
before channel initialization or GetInfo; SmartCard selection and firmware discovery still precede SCP establishment.

```csharp
// With IYubiKey yubiKey in an async method; this factory owns the opened connection.
await using var session = await yubiKey.CreateCtap2SessionAsync();
```

Alternatively, pass an existing supported `IConnection connection` to
`Ctap2Session.CreateAsync(connection)`. That session borrows the connection; keep the caller's disposal
scope outside the session's. Do not append the direct factory or a one-shot GetInfo helper while another
connection-owning session is live on the same key. SCP03 requires an authorized SmartCard key configured
with the supplied keys; own/dispose `Scp03KeyParameters` outside the secure session scope.

### GetInfo (No User Presence Required)

```csharp
// Get authenticator capabilities
var info = await session.GetInfoAsync();

// Check versions
bool ctap2_1 = info.Versions.Contains("FIDO_2_1");

// Check supported extensions
bool hasHmacSecret = info.Extensions.Contains("hmac-secret");

// Check options
bool hasRk = info.Options.TryGetValue("rk", out var rk) && rk;
```

Do not generalize optional-field null preservation. Optional numeric, Boolean and text fields retain
null when absent, but decoded `EncIdentifier`/`EncCredStoreState` convert absent byte arrays to present
empty nullable memory. `HasValue` is not a wire-presence/capability check, and nullable presence/length
does not distinguish missing fields from encoded empty byte strings. Use the complete `RawData` map
to inspect encoded presence. Missing required AAGUID separately defaults to empty, not a valid response.

### PIN/UV Auth Protocols

There is no separate initialization step. The protocol object is stateless with respect to the
authenticator; `ClientPin` performs the `getKeyAgreement` round trip and ECDH encapsulation
internally on each token request.

```csharp
// Fragment: session, byte[] pin, string rpId, byte[32] clientDataHash,
// MakeCredentialOptions options, RP/user entities and CancellationToken cancellationToken are supplied.
// Requires mutually supported protocol 2; this constructor does not negotiate it.
using var protocol = new PinUvAuthProtocolV2();
using var clientPin = new ClientPin(session, protocol);

// Get PIN token with permissions. rpId is required for MakeCredential/GetAssertion permissions.
byte[]? pinToken = null;
byte[]? authParam = null;
try
{
    pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
        pin,
        PinUvAuthTokenPermissions.MakeCredential,
        rpId,
        cancellationToken);

    // Authenticate command
    authParam = protocol.Authenticate(pinToken, clientDataHash);
    options.WithPinUvAuth(authParam, protocol.Version);
    // Keep options.UserVerification null; even uv:false is mutually exclusive with token auth.
    await session.MakeCredentialAsync(
        clientDataHash, rp, user, [PublicKeyCredentialParameters.CreateES256()], options,
        cancellationToken);
}
finally
{
    if (pinToken is not null) CryptographicOperations.ZeroMemory(pinToken);
    if (authParam is not null) CryptographicOperations.ZeroMemory(authParam);
}
```

This composable fragment sends a creation command but does not verify RP registration. Supply a fresh
exact client-data hash and RP/account context as described in the README, or demo inputs on the dedicated
evaluation key. The PIN is borrowed; the caller clears it after the final await.
`ClientPin.Dispose` disposes its supplied protocol, not the session. The returned token is caller-owned;
zero it when done (see
`examples/FidoTool/FidoExamples/MakeCredential.cs`). On authenticators that do not advertise the
`pinUvAuthToken` option, `GetPinUvAuthTokenUsingPinAsync` falls back to the legacy
`getPinToken` subcommand with the same protocol; requested permissions/RP do not apply. This is not V1
negotiation or general administrative authorization. Built-in UV token acquisition needs configured UV,
token capability and any permission-specific gates, and can require biometric interaction without an
SDK presence notification. Token issuance/lifetime and host buffer lifetime are different boundaries.

### Extensions (WebAuthn)

Compose CTAP extension bytes with `ExtensionBuilder`; some overloads take typed inputs such as
`HmacSecretInput` and `PreviewSignRegistrationInput`. Building bytes does not negotiate support, enforce
RP policy or perform a ceremony. Client-side WebAuthn transformations belong in the WebAuthn package.

#### Available Extensions

| Extension | Builder Method | Description | Availability check |
|-----------|---------------|-------------|------------|
| credProtect | `.WithCredProtect(...)` | Requests policy; `enforcePolicy` is currently not honored | Extension support and applicable UV policy |
| hmac-secret | `.WithHmacSecret()` / `.WithHmacSecret(...)` | Registration enablement / assertion encrypted salts | Advertised extension, protocol and UP |
| hmac-secret-mc | `.WithHmacSecretMakeCredential(input)` | Encrypted evaluation during creation; also emits hmac-secret=true | Separate advertisement; firmware metadata alone is insufficient |
| credBlob | `.WithCredBlob(blobData)` / `.WithGetCredBlob()` | Store at creation / retrieve at assertion | Advertised extension and maxCredBlobLength |
| largeBlobKey | `.WithLargeBlobKey()` | Key in response envelope; `LargeBlobStorage` reads/writes shared array | Extension plus largeBlobs option and discoverable credential |
| minPinLength | `.WithMinPinLength()` | Reports existing code-point minimum during creation, only to authorized RP IDs | Extension plus configured disclosure RP list |
| previewSign | `.WithPreviewSign(...)` | Generated-key signing in experimental vendor draft | Advertised previewSign and algorithm support |

`previewSign` follows the experimental [Yubico sign-extension draft v4](https://yubicolabs.github.io/webauthn-sign-extension/4/#sctn-sign-extension)
(Editor's Draft, 2025-08-26), not CTAP v4 or an adopted W3C signing standard. CTAP overloads take
`PreviewSignRegistrationInput` or `PreviewSignSigningParams`; `PreviewSignAuthenticationInput` is a
WebAuthn-layer type. Generic `tbs` and optional `additionalArgs` are encoded unchanged; callers must
provide algorithm-appropriate TBS/prehashing and a CBOR COSE_Sign_Args map when required. Inner attestation
is parsed, not trust-verified. **WARNING -- EXPERIMENTAL --** Public ARKG conveniences are not ready for
production and must not be treated as production cryptographic guidance. Convert arguments with
`PreviewSignCbor.EncodeAdditionalArgs(...)`. The seed algorithm placeholder -65700 and BL=-1/KEM=-2
labels agree with ARKG Internet-Draft -10, as does the example's derived ESP256=-9. The SDK's outer kty=2
and byte-string-wrapped nested maps differ from the draft's kty=-65537/direct maps; the SDK also requires
an integer derived algorithm where the draft permits an optional int/text value. This partial agreement
is not full-profile conformance; the draft's stated expiry is 2026-08-31. Signing and derived-key
identifiers remain distinct; the convenience verifier handles raw
messages with P-256/SHA-256 and limited DER parsing, not arbitrary generic TBS algorithms.

#### Example Usage

```csharp
// Builder fragment: ReadOnlyMemory<byte> blobData and the hmac-secret inputs below are caller-supplied.
// Build extensions for MakeCredential
var extensions = new ExtensionBuilder()
    .WithCredProtect(CredProtectPolicy.UserVerificationRequired)
    .WithCredBlob(blobData)
    .Build();

var options = new MakeCredentialOptions
{
    Extensions = extensions,
    ResidentKey = true
};

// For GetAssertion: encrypted salts under the shared secret from key agreement
// protocol/sharedSecret/keyAgreement and 32-byte salt1/salt2 belong to the caller.
var assertionExtensions = new ExtensionBuilder()
    .WithHmacSecret(protocol, sharedSecret, keyAgreement, salt1, salt2)
    .Build();
```

Keep borrowed extension inputs unchanged through encoding/transmission and clear caller-owned secrets.
For sensitive/PII credBlob content the RP must request level-3 credProtect with enforcement; the current
builder does not enforce that policy, so this composition alone is not a safe sensitive-storage recipe.
Use nonsensitive demo blobData unless support/policy prevention is independently handled.

#### hmac-secret vs WebAuthn PRF

`hmac-secret` is the CTAP2 extension (2.1 §12.5; 2.3 §12.7) and lives here. Its credential-scoped random
secrets have separate verified/unverified variants; repeatability depends on credential, salt and UV
mode, not on the PIN/biometric value. Salt authentication under the shared secret differs from command
token authentication over clientDataHash. WebAuthn's `prf` extension (L3 §10.1.4) is a
*client* extension built on top of it: the WebAuthn package hashes the relying party's inputs into salts
(`SHA-256("WebAuthn PRF" || 0x00 || input)`), performs key agreement, encrypts and authenticates the salts
with `WithHmacSecret(...)`, and decrypts the result. There is no `"prf"` key on the CTAP wire, so this
package has no PRF types; see `Yubico.YubiKit.WebAuthn.Extensions.Inputs.PrfInput` and
docs/architecture/webauthn-extension-processing.md ("Where code belongs").

The current WebAuthn adapter does not enforce PRF's required verified-variant UV override, and result/
intermediate copies lack an owned cleanup guarantee; retain these limitations when linking the transform.
For large blobs, the SDK uses largeBlobKey/storage rather than the standard direct largeBlob extension.
The direct route does define makeCredential support input; its absence here is an SDK-route limitation,
not a claim that CTAP forbids it or that every current YubiKey lacks it.

### Credential Management

```csharp
// With live session/protocol, authorized borrowed pinToken, returned RP hash and descriptor credentialId.
using var credMgmt = new CredentialManagement(session, protocol, pinToken);

// Get credential metadata
var metadata = await credMgmt.GetCredentialsMetadataAsync();

// Enumerate RPs. ReportedTotal is copied from the first response and can be null.
var rpResult = await credMgmt.EnumerateRelyingPartiesAsync();
var rps = rpResult.RelyingParties;

// Enumerate credentials for an RP
using var credentialResult = await credMgmt.EnumerateCredentialsAsync(rpIdHash);
var creds = credentialResult.Credentials;

// Delete credential
await credMgmt.DeleteCredentialAsync(credentialId);
```

Credential-management enumeration results own read-only collection storage. A
`CredentialEnumerationResult` also owns its `StoredCredentialInfo` entries and must be disposed after their
raw or secret-bearing views are no longer needed. `StoredCredentialInfo.Decode` borrows its input and returns
an independently disposable owner for direct decoder callers. The reported total is not recomputed from the
collection count. `ClientPin.GetPinRetriesAsync` and `GetUvRetriesAsync` return the property-based
`PinRetryStatus` and `UserVerificationRetryStatus` models; their response parsers retain the existing protocol
behavior.

Use the returned RP hash unchanged: RP display text can be truncated. Enumeration eagerly sends begin
and the reported number of next calls; missing total yields one result, and malformed totals are not
fully validated. Do not interleave other device operations. A continuation failure throws rather than
returning partial results. `CredentialManagement.Dispose` currently only marks a field that operations
do not check; it neither owns/clears the borrowed token nor ends device authorization. Config/bio/storage
helpers also borrow these dependencies. Toggle alwaysUv only after reading current state; after an
uncertain outcome re-read before repeating. minPinLength disclosure is not setting policy.

Bio helpers have known standard subcommand/authentication/timeout and template-array decoding limitations.
Do not claim interoperable enrollment from fake fixtures or sensor metadata. Retain the begin template
ID for continuation; sample `IsComplete` is only decoded remaining-count equality, including unsafe
missing-field defaults. .NET cancellation does not automatically send enrollment cancel. Blob updates
are shared-array read-modify-write operations without whole-sequence conflict protection or rollback;
the SDK requires protocol/token authorization for writes, stricter than CTAP's conditional rule.

### User-presence notifications

Supply `SessionCreationOptions.UserPresencePrompt` when creating a `Ctap2Session` to receive touch
notifications. On HID, `FidoHidProtocol` requests the operation's shared notification handle with
`DeviceWaiting` when CTAPHID keep-alive status `0x02` is first observed. `HidBackend` owns terminal
resolution after the response is fully drained and the CTAP status is known. SmartCard has no
equivalent in-flight signal, so `SmartCardBackend` owns both the policy request and resolution;
MakeCredential and GetAssertion emit `PolicyRequires` immediately before the APDU unless their
effective `UserPresence` option is `false`. Their scope is the RP ID.
Successful response ownership transfers only after terminal resolution succeeds; a callback failure clears
the untransferred backend response, including secret-derived extension output.

Selection and Reset carry null-scope context for an authoritative HID keep-alive, but do not predict
touch on SmartCard. Generic `SendCborRequestAsync` calls do not infer touch semantics. Bio enrollment
and other helpers that currently route through that generic API remain silent until their individual
user-presence behavior is modeled explicitly.

## CBOR Encoding Pattern

Use `CtapRequestBuilder` for all CTAP commands:

```csharp
var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
    .WithInt(1, protocol.Version)
    .WithInt(2, ClientPinSubCommand.GetRetries)
    .Build();

var response = await session.SendCborRequestAsync(request);
```

Key rules:
- Parameters are **integer keys** (not strings) for CTAP
- Use **Ctap2Canonical** conformance mode (map keys sorted by encoded type/length/bytes, not numeric or alphabetical order; the current writer sorts at map completion)
- Build request returns command byte + CBOR payload
- Use `.WithValue(key, writer => ...)` for complex values or sensitive byte spans that should be written directly instead of copied through `.WithBytes(...)`
- Keep operation-level request builders as pure encoding helpers; do not introduce operation-specific CTAP command classes
- WithBytes/encoded inputs retain private copies with no disposal owner; WithValue callbacks run at Build.
  Keep callback inputs valid until Build. Clear caller-owned encoded request arrays after the awaited send.
  Canonical mode alone does not enforce command schema, CTAP depth, device maxMsgSize or full input exhaustion.

## Forward compatibility and escape hatches

Valid authenticator response data that is not yet modeled must remain usable without relaxing malformed or security-critical data checks. The CTAP2 and WebAuthn response escape hatches are:

- `RawData` on response owners such as `AuthenticatorInfo`, credential metadata, relying-party information, `StoredCredentialInfo`, `FingerprintSensorInfo`, and `EnrollmentSampleResult` preserves the complete response envelope. Dispose `StoredCredentialInfo` directly, or its owning `CredentialEnumerationResult`, because its envelope can contain secret fields.
- `AttestedCredentialData.CredentialPublicKey`, with `CoseOtherKey.RawCbor` when an unmodeled key type is not claimed by a modeled algorithm. COSE keys must still report an algorithm.
- AuthenticatorData raw extension bytes and `WebAuthnAuthenticatorData.ParsedExtensions` expose encoded
  extension data. `ExtensionOutput.TryGetRawExtension` is a limited reconstruction, not byte-exact
  preservation of every CBOR type; its reader overload stores identifiers with empty values only.
- `ICtap2Session.SendCborRequestAsync` for explicitly constructed raw CTAP CBOR requests and responses.

PIN/UV key agreement is a separate security-critical boundary: do not relax required P-256 key type,
curve, coordinates or protocol structure for forward compatibility. Current helper checks coordinates
but omits required peer-label checks; that is a validation gap, not a sanctioned bypass. Unknown response
fields must not be blindly copied into requests. Credential/GetInfo decoders also have known required-
field/envelope-validation gaps; preserve valid unknown data without promising universal malformed rejection.

Add `RawData` to models that own a complete, non-secret authenticator response. A response that can contain
secret material instead needs explicit disposable ownership. `StoredCredentialInfo` owns one private complete
response clone; `LargeBlobKey` is a view into it, and disposal clears that clone, including unknown fields.
The public decoder never clears its borrowed input, and the session's read-only response contract is unchanged,
so this is not an end-to-end cleanup guarantee. Do not duplicate the same buffer on nested entities.
Use `RawCbor` only for an encoded nested CBOR structure, such as an attestation statement or unsupported COSE key; it may be a slice of its parent buffer rather than a separately owned copy.

## Security Requirements

### Memory Handling

This synchronous fragment illustrates cleanup of a caller-owned PIN and its derived hash, not the
complete ClientPin protocol. Small synchronous buffers use stack allocation; bytes crossing an await
need Memory/owned storage instead. Never log the PIN, hash, tokens or secret-derived extension output.

```csharp
// With byte[] pinBytes supplied by the caller, in a synchronous method.
Span<byte> pinHash = stackalloc byte[32];
try
{
    SHA256.HashData(pinBytes, pinHash);
}
finally
{
    CryptographicOperations.ZeroMemory(pinHash);
    CryptographicOperations.ZeroMemory(pinBytes);
}
```

### Protocol Disposal

```csharp
using var protocol = new PinUvAuthProtocolV2();
// Dispose() only marks the instance disposed; it holds no key material to release.
```

The ephemeral ECDH key pair lives entirely inside a single `Encapsulate` call:
`PinUvAuthHelpers.PerformEcdhKeyAgreement` creates it with `using var ecdh = ...`; after the helper
returns, `Encapsulate` zeroes raw `z` in its `finally`. This does not cover the helper's post-derivation
allocation-failure gap or all provider/internal copies. `Encapsulate` hands the derived shared secret
to the caller, who owns it — `ClientPin` zeroes it in its own `finally`. Still use `using` on the
protocol so its crypto methods reject disposed-instance use; Version/AuthenticationTagLength remain readable.

## Test Patterns

### Unit Tests (No Hardware)

`Ctap2SessionLinuxCrossLayerTests` complements the macOS HID cross-layer suite using actual Linux
interfaces/slots, `YubiKeyDevice` and production native owners with report/readiness fakes underneath.
It pins held creation/GetInfo, a multi-report MakeCredential request followed by exactly one CANCEL,
terminal presence only after response drain, correlated GetInfo reuse, borrowed idle sessions, and
owned pending-exchange disposal with claim retention through true quiescence. It does not fake
`IFidoHidConnection`, operate a device, or attest native Linux/AOT behavior. See
[L6a managed evidence](../../docs/research/linux-managed-acceptance.md).

```csharp
[Fact]
public void AuthenticatorInfo_Decode_ParsesVersions()
{
    // Partial parser fixture, not a conforming GetInfo envelope: the required AAGUID is absent.
    var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
    writer.WriteStartMap(1);
    writer.WriteInt32(1);
    writer.WriteStartArray(1);
    writer.WriteTextString("FIDO_2_0");
    writer.WriteEndArray();
    writer.WriteEndMap();
    byte[] cborData = writer.Encode();
    
    // Act
    var info = AuthenticatorInfo.Decode(cborData);
    
    // Assert
    Assert.Contains("FIDO_2_0", info.Versions);
}
```

### CBOR Test Mock Patterns

When testing classes that send CBOR commands via `ICtap2Session`:

The following complete method belongs in the CTAP UnitTests project with xUnit v3, NSubstitute,
System.Formats.Cbor and PIN namespaces imported. It checks a real helper request, not biometric or
cryptographic interoperability. Mock `ICtap2Session`, not the sealed `Ctap2Session`.

```csharp
[Fact]
public async Task PinRetries_EncodesProtocolAndSubcommand()
{
    var session = Substitute.For<ICtap2Session>();
    ReadOnlyMemory<byte> mockResponse = new byte[] { 0xA1, 0x03, 0x08 }; // {pinRetries: 8}
    ReadOnlyMemory<byte> capturedRequest = default;
    session.SendCborRequestAsync(
        Arg.Do<ReadOnlyMemory<byte>>(value => capturedRequest = value),
        Arg.Any<CancellationToken>()).Returns(mockResponse);
    using var protocol = new PinUvAuthProtocolV2();
    using var clientPin = new ClientPin(session, protocol);

    var retries = await clientPin.GetPinRetriesAsync(TestContext.Current.CancellationToken);

    Assert.Equal(8, retries.RetriesRemaining);
    Assert.Equal(CtapCommand.ClientPin, capturedRequest.Span[0]);
    var reader = new CborReader(capturedRequest.Slice(1), CborConformanceMode.Ctap2Canonical);
    Assert.Equal(2, reader.ReadStartMap());
    Assert.Equal(1, reader.ReadInt32());
    Assert.Equal(protocol.Version, reader.ReadInt32());
    Assert.Equal(2, reader.ReadInt32());
    Assert.Equal(ClientPinSubCommand.GetRetries, reader.ReadInt32());
    reader.ReadEndMap();
}
```

**Common gotcha:** Tests may fail if they parse `capturedRequest` directly without skipping the command byte prefix.

### Hardware Integration Tests

Tests requiring user presence must be marked and excluded from unattended runs:

Use the xUnit v2 IntegrationTests project with Tests.Shared, its Infrastructure namespace and Core
Devices/Sessions namespaces imported. This direct-session example has no PIN-normalizing helper setup:

```csharp
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.HidFido)]
public async Task GetInfoAsync_Returns_Aaguid(YubiKeyTestState state)
{
    await using var session = await state.Device.CreateCtap2SessionAsync(
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidFido });
    var info = await session.GetInfoAsync();
    Assert.Equal(16, info.Aaguid.Length);
}
```

For a meaningful marked creation example, use [tests/CLAUDE.md](tests/CLAUDE.md#user-interaction-and-coordination-lanes).
Keep its actual setup, arguments, assertion and cleanup in the real test-project context. Most existing
`WithCtap2SessionAsync` calls default to PIN normalization before the callback; even a final GetInfo can
therefore set/change a PIN or consume a retry. Smoke filters do not make those paths read-only. Preserve
the existing module human-coordination policy; GetInfo itself needs no new approval gate.

From the repository root, the first command selects units only; the second includes unit/integration
projects then filters to integration smoke. The latter still requires inspecting actual setup for PIN/
state work under the existing coordination policy; it is not an unattended safety guarantee.
```bash
dotnet toolchain.cs -- test --project Ctap2
dotnet toolchain.cs -- test --integration --project Ctap2 --smoke --filter "FullyQualifiedName~Yubico.YubiKit.Ctap2.IntegrationTests"
```

## Feature Flags

The following schematic declarations summarize actual `Ctap2Session` firmware metadata, not a new class
to paste into the library. They do not check model hardware, advertised command/extension or permission:

```csharp
public static readonly Feature FeatureFido2 = new("FIDO2", 5, 0, 0);
public static readonly Feature FeatureBioEnrollment = new("Bio Enrollment", 5, 2, 0);
public static readonly Feature FeatureCredentialManagement = new("Credential Management", 5, 2, 0);
public static readonly Feature FeatureHmacSecretMc = new("hmac-secret-mc", 5, 4, 0);
public static readonly Feature FeatureAuthenticatorConfig = new("Authenticator Config", 5, 4, 0);
public static readonly Feature FeatureCredBlob = new("credBlob", 5, 5, 0);
public static readonly Feature FeatureEncIdentifier = new("Encrypted Identifier", 5, 7, 0);
```

With a `FirmwareVersion firmwareVersion`, a threshold comparison can rule out known older firmware.
Major-zero sentinel/default metadata passes `IsSupportedByFirmware`; this is not qualified feature support:
```csharp
if (!Ctap2Session.FeatureBioEnrollment.IsSupportedByFirmware(firmwareVersion))
{
    throw new NotSupportedException("Bio enrollment requires firmware 5.2+");
}
```

## Common Pitfalls

1. **CBOR Key Order**: CTAP2 canonical keys sort by encoded type/length/bytes, not numeric value.
   Use `CborConformanceMode.Ctap2Canonical`; the current writer sorts completed maps automatically.

2. **PIN Padding**: ClientPin right-zero-pads a new PIN to 64 bytes before encryption. AES uses no padding,
   not PKCS#7. Caller input must meet UTF-8/NFC/code-point/device-policy requirements; byte count alone is insufficient.

3. **Protocol Reuse**: PIN/UV auth protocols are stateless with respect to the authenticator; token requests perform key agreement internally. Dispose the protocol when done.

4. **User Presence**: Configure `SessionCreationOptions.UserPresencePrompt` for SDK notifications; do not infer touch from generic CBOR traffic. Hardware tests that actually require touch must still use the `RequiresUserPresence` trait.

5. **Extension Order**: Canonical output uses encoded-key order, not numeric/alphabetical insertion;
   the current Ctap2Canonical writer sorts completed maps. Do not infer broken update-user wire from insertion order.

## References

### Shared key metadata

Use Core `Cryptography.Cose.CoseKeyType` for readable known family constants, while retaining open `int`
wire families in active `CoseKey` and `CoseOtherKey` raw preservation. Do not cast Core concrete `KeyType`
values to wire IDs. `CoseAlgorithm` and unknown conversions remain open. Core `CoseAlgorithmIdentifier`
names are PascalCase; `CryptographicDefinitions.Keys` exposes optional `Core.Cryptography.Cose.CoseKeyInfo`
with kty/crv and no intrinsic algorithm. `Algorithms.FindForKey` is structural applicability, not CTAP2
capability or a default. Never force decoded keys through the finite Core registry. Preserve unknown numeric
kty/alg and opaque bytes, and strict PIN/UV P-256/-25 checks. See [Core catalog](../Core/README.md#cryptographic-metadata).

- [CTAP 2.1 Specification](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html)
- [WebAuthn Level 2](https://www.w3.org/TR/webauthn-2/)
- [COSE Key Registry](https://www.iana.org/assignments/cose/cose.xhtml)
