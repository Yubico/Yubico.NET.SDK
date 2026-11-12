PROPOSED SAMPLE — no verdict: core-001:unit:b4125585d8579c68 / WebAuthn / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/src/Client/Registration/RegistrationResponse.cs:1–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.WebAuthn.Attestation;
using Yubico.YubiKit.WebAuthn.Cose;

namespace Yubico.YubiKit.WebAuthn.Client.Registration;

/// <summary>
/// Response from WebAuthn credential registration (MakeCredential).
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:7d926f1edba94468 / WebAuthn / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/src/Extensions/Codec/CredProtectCodec.cs:1–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Extensions.Outputs;

namespace Yubico.YubiKit.WebAuthn.Extensions.Codec;

/// <summary>
/// Translation between the WebAuthn credProtect extension inputs and outputs and their CTAP encoding.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:06d5faa281acff8e / WebAuthn / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/src/IYubiKeyExtensions.cs:27–84
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    extension(IYubiKey yubiKey)
    {
        /// <summary>
        /// Creates a WebAuthn client for the YubiKey asynchronously.
        /// </summary>
        /// <param name="origin">The WebAuthn origin for client data JSON.</param>
        /// <param name="clientOptions">
        /// Required configuration for the client itself (application RP validity and authorization,
        /// credential prompt, prompt-attempt limit), forwarded to the created
        /// <see cref="WebAuthnClient"/>. Unrelated to <paramref name="sessionOptions"/>, which
        /// configures the session rather than the client.
        /// </param>
        /// <param name="sessionOptions">
        /// Optional settings for the underlying CTAP2 session (SCP key parameters, protocol
        /// configuration, preferred transport, firmware override). When no preferred connection is
        /// specified, the CTAP2 default order applies (<see cref="ConnectionType.HidFido"/>, then
        /// <see cref="ConnectionType.SmartCard"/>), except that SCP key parameters select SmartCard
        /// automatically. Explicit SmartCard selection is also supported; explicit HID with SCP is not.
        /// CTAP2 requires the FIDO AID for SmartCard and applies its USB firmware gate (5.8.0+);
        /// NFC can use SmartCard without that USB restriction. SCP is available only on SmartCard.
        /// Transport selection does not retry another connection after an opening failure.
        /// </param>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        /// <returns>A <see cref="WebAuthnClient"/> that owns the underlying CTAP2 session.</returns>
        /// <remarks>
        /// <see cref="WebAuthnClientOptions.RpIdValidator"/> owns RP syntax validity and origin/RP authorization and is invoked
        /// before each ceremony's device I/O, not during session creation (which has no requested RP ID).
        /// The SDK checks only nonempty, faithfully UTF-8 encodable RP text, not domain/URI syntax or binding policy.
        /// This method adds no independent session-creation logic; <paramref name="sessionOptions"/> is
        /// validated and applied by the underlying CTAP2 <c>CreateCtap2SessionAsync</c>, while
        /// <paramref name="clientOptions"/> is forwarded to the returned <see cref="WebAuthnClient"/>.
        /// </remarks>
        public async Task<WebAuthnClient> CreateWebAuthnClientAsync(
            WebAuthnOrigin origin,
            WebAuthnClientOptions clientOptions,
            SessionCreationOptions? sessionOptions = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(origin);
            ArgumentNullException.ThrowIfNull(clientOptions);
            // Force the runtime guard before session creation can open a device connection.
            _ = clientOptions.RpIdValidator;

            var ctap2Session = await yubiKey.CreateCtap2SessionAsync(sessionOptions, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                return new WebAuthnClient(ctap2Session, origin, clientOptions);
            }
            catch
            {
                await ctap2Session.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:ec91cffcba17a31e / WebAuthn / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/src/Yubico.YubiKit.WebAuthn.csproj:1–25
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- TargetFramework inherited from Directory.Build.props -->
    <RootNamespace>Yubico.YubiKit.WebAuthn</RootNamespace>
    <AssemblyName>Yubico.YubiKit.WebAuthn</AssemblyName>

    <!-- Package Metadata -->
    <Description>Client-side WebAuthn registration and authentication for YubiKey authenticators over CTAP2, with typed extension processing and the experimental Yubico previewSign draft v4.</Description>
    <PackageTags>yubikey;yubico;webauthn;fido2;ctap2;passkeys;authentication</PackageTags>

    <!-- Opts this published library into the shared Native AOT analyzer and metadata properties. -->
    <IsYubiKitSdkLibrary>true</IsYubiKitSdkLibrary>
  </PropertyGroup>

  <ItemGroup>
    <!-- For NSubstitute to mock the internal IWebAuthnBackend test seam -->
    <InternalsVisibleTo Include="DynamicProxyGenAssembly2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Ctap2\src\Yubico.YubiKit.Ctap2.csproj" />
  </ItemGroup>

</Project>



PROPOSED SAMPLE — no verdict: core-001:unit:c8377e0a58dac11c / WebAuthn / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/CLAUDE.md:6–13
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented in both CLAUDE.md and README.md
> - **New features** (e.g., new extensions, credential types) should include usage examples
> - **Breaking changes** require updates to both files with migration guidance
> - **Test infrastructure changes** should be reflected in the test pattern sections below




PROPOSED SAMPLE — no verdict: core-001:unit:4886f819fa316d28 / WebAuthn / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/CLAUDE.md:70–87
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Logging

Use Core's `YubiKitLogging.CreateLogger<T>()` when adding logging; there is no `LoggingFactory`.

The WebAuthn source currently contains no logger calls. This does not guarantee that the underlying
CTAP2/Core layers or application logging are silent, and it is not a sensitive-data logging audit.

When adding logs:
```csharp
private static readonly ILogger Logger = YubiKitLogging.CreateLogger<PreviewSignProcessor>();

Logger.LogDebug("Selected algorithm: {Algorithm}", selectedAlgorithm);
```

**Security:** Never log PINs, derived secrets, keys, or sensitive `tbs`/argument payloads. Prefer lengths
and algorithm IDs. Public protocol identifiers can still identify or correlate accounts; apply
redaction/access policy rather than asserting that credential IDs are safe to log.




PROPOSED SAMPLE — no verdict: core-001:unit:546f13cbc632a4a1 / WebAuthn / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/README.md:260–278
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Cancellation and failures

Pass the ceremony's third argument by name when omitting the PIN:
`client.GetAssertionAsync(requestOptions, cancellationToken: cancellationToken)`.
`Timeout` on either options record has no SDK default or clamping and must be positive and within the
framework-supported delay range. It starts after local validation and the RP callback, and signals
cooperative cancellation, not a hard completion deadline. HID can send `CTAPHID_CANCEL` during a
keep-alive and drain a terminal response; admitted SmartCard exchanges finish before returning.
A completed success can win the cancellation race. Cancellation does not roll back credential creation
or extension writes.

An empty match list is normal. Excluded registration credentials map to `InvalidState`; unavailable
required verification and an observed ceremony timeout map to `NotAllowed`. Caller-token cancellation
propagates `OperationCanceledException`, not the client's `Cancelled` error code. Authenticator timeout
statuses instead map to `Constraint`. Capability reads, token acquisition, transport/setup, and extension
output processing can propagate lower-level exceptions: not every failure is `WebAuthnClientError`.
A failure does not trigger a retry on another transport. See
[`WebAuthnClientError`](src/WebAuthnClientError.cs) for the error vocabulary.




PROPOSED SAMPLE — no verdict: core-001:unit:bf53e9ad2aa19caf / WebAuthn / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/README.md:308–328
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Evaluate PRF inputs deliberately

The WebAuthn PRF maps application input to domain-separated `hmac-secret` salts. `First` and optional
`Second` supply the default evaluation inputs; `Second` alone is not evaluated. Authentication can
override these per credential with `EvalByCredential`, whose keys are unpadded base64url credential
IDs and whose nonempty map requires an allow list. Registration does not accept that map when the PRF
processor runs; registration-time evaluation additionally needs advertised `hmac-secret-mc` support.
Check enablement and evaluation results separately.

**Current limitation:** [WebAuthn L3 §10.1.4](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#prf-extension)
requires the hmac-secret-backed PRF to use the verified variant, overriding the UV preference if needed.
This client does not enforce that override. Request `UserVerification = UserVerificationPreference.Required`
for evaluation; otherwise actual UV changes can change the output for the same credential and input.
Selecting Required alone does **not** establish full PRF conformance or solve result cleanup.

PRF outputs are derived secrets: never log them. SDK-produced results expose read-only memory without
a disposal/zeroing guarantee for returned arrays or intermediate copies; disposing the client does not
clear them. Clearing a copy you made does not clear the SDK's originals. Treat PRF inputs according to
their application sensitivity as well. This lifetime limitation matters when evaluating suitability
for secret-handling applications; there is no writable-result-span API to use for guaranteed cleanup.




PROPOSED SAMPLE — no verdict: core-001:unit:a78b316e99da06d2 / WebAuthn / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs:253–276
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    [Trait("Audit", "YESDK-1615")]
    public async Task YESDK1615_PreferredResidentKeyOnCapableAuthenticatorRequestsDiscoverable()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(new AuthenticatorInfo { Options = new Dictionary<string, bool> { ["rk"] = true } });
        BackendMakeCredentialRequest? sent = null;
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.Arg<BackendMakeCredentialRequest>();
                return MockCtap2Responses.CreateMockMakeCredentialResponse();
            });
        await using var client = CreateClient(backend);
        _ = await client.MakeCredentialAsync(Registration() with { ResidentKey = ResidentKeyPreference.Preferred },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(sent);
        Assert.True(sent.Options?.TryGetValue("rk", out var rk) == true && rk,
            "a capable authenticator must receive rk:true when residentKey is preferred");
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7fe08c46f10afee5 / WebAuthn / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientGetAssertionTests.cs:1–29
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using NSubstitute;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;




PROPOSED SAMPLE — no verdict: core-001:unit:e29522f04d40349a / WebAuthn / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientGetAssertionTests.cs:130–163
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task GetAssertion_MatchedCredential_SelectAsync_IsIdempotent()
    {
        // Arrange
        var credentialId = RandomNumberGenerator.GetBytes(32);
        var options = new PublicKeyCredentialRequestOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            RpId = "example.com"
        };

        _mockBackend.GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockGetAssertionResponse(credentialId));

        // Act
        var result = await _client.GetAssertionAsync(options, pinBytes: null, TestContext.Current.CancellationToken);
        var matched = result[0];

        var response1 = await matched.SelectAsync(TestContext.Current.CancellationToken);
        var response2 = await matched.SelectAsync(TestContext.Current.CancellationToken);

        // Assert - same instance or value-equal
        Assert.Equal(response1.CredentialId.ToArray(), response2.CredentialId.ToArray());
        Assert.Equal(response1.Signature.ToArray(), response2.Signature.ToArray());

        // Backend GetAssertion should have been called only once during GetAssertionAsync
        await _mockBackend.Received(1).GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<CancellationToken>());
    }


PROPOSED SAMPLE — no verdict: core-001:unit:2f2513b5ffd4c56b / WebAuthn / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientOptionsTests.cs:28–29
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class NullPrompt : ICredentialPrompt
    {


PROPOSED SAMPLE — no verdict: core-001:unit:1b932c40537768a9 / WebAuthn / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/ExtensionSessionTests.cs:151–190
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact(Timeout = 5000)]
    public void PrepareInputs_PreviewSignWithMultipleStandardExtensions_ProducesCtap2CanonicalOrder()
    {
        // Arrange - combine previewSign with standard extensions that have different lengths
        var inputs = new RegistrationExtensionInputs(
            CredProtect: new CredProtectInput(CredProtectPolicy.UserVerificationRequired), // length 11
            CredBlob: new Ctap2.Extensions.CredBlobInput { Blob = new byte[] { 0x01, 0x02 } }, // length 8
            PreviewSign: new WebAuthn.Extensions.PreviewSign.PreviewSignRegistrationInput(
                algorithms: new[] { CoseAlgorithm.Es256 })); // length 11
        var options = CreateMockOptions();


        // Act
        var cborBytes = RegistrationExtensionSession.Create(inputs, options, requireResidentKey: false, TestExtensionContext.SupportingAll()).PrepareInputs(token: null);

        // Assert - Verify CBOR doesn't throw when encoding (canonical mode validates order)
        Assert.NotNull(cborBytes);

        // Decode and verify CTAP2 canonical order: length-ascending, then lex within same-length
        var reader = new CborReader(cborBytes.Value, CborConformanceMode.Ctap2Canonical);
        int? mapLength = reader.ReadStartMap();
        Assert.NotNull(mapLength);
        Assert.Equal(3, mapLength.Value); // credBlob + credProtect + previewSign

        // Expected order: credBlob(8), credProtect(11), previewSign(11)
        var key1 = reader.ReadTextString();
        Assert.Equal("credBlob", key1); // length 8 comes first

        reader.SkipValue(); // skip credBlob value

        var key2 = reader.ReadTextString();
        Assert.Equal("credProtect", key2); // length 11, lex before previewSign

        reader.SkipValue(); // skip credProtect value

        var key3 = reader.ReadTextString();
        Assert.Equal("previewSign", key3); // length 11, lex after credProtect
    }


PROPOSED SAMPLE — no verdict: core-001:unit:651df80c5416339e / WebAuthn / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/LargeBlob/LargeBlobProcessorTests.cs:163–180
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>This CTAP storage-write failure becomes Written=false; the assertion does not cover read errors or cancellation.</summary>
    [Fact]
    public async Task Authentication_WriteFails_ReportsWrittenFalse()
    {
        _storage.SetBlobAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Ctap2.CtapException(Ctap2.CtapStatus.LargeBlobStorageFull));
        using var session = AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(LargeBlob: new LargeBlobInput { Write = new byte[] { 1 } }), allowCredentials: null, Context());
        var response = MockCtap2Responses.CreateMockGetAssertionResponse(largeBlobKey: LargeBlobKey);

        var result = await session.PrepareOutputsAsync(response,
            WebAuthnAuthenticatorData.Decode(response.AuthenticatorDataRaw),
            new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), new byte[32]), TestContext.Current.CancellationToken);

        Assert.False(result?.LargeBlob?.Written);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:35e46ef0a6a3636d / WebAuthn / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignCodecTests.cs:87–137
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact(Timeout = 5000)]
    public void PreviewSign_Registration_OutputPopulatedFromUnsignedAttObj()
    {
        // Arrange - mock authenticator data with previewSign extension containing algorithm + flags in authData
        // and the inner generated-key attestation object in unsignedExtensionOutputs.
        var innerCredentialId = RandomNumberGenerator.GetBytes(32);
        var publicKeyBytes = BuildCoseEc2PublicKey(CoseAlgorithm.Es256);
        var attestationObject = BuildAttestationObject(publicKeyBytes, WebAuthnPreviewSign.PreviewSignFlags.RequireUserVerification, innerCredentialId);

        // Build authData.extensions["previewSign"] = {3: alg, 4: flags}
        var authDataExtension = new CborWriter(CborConformanceMode.Ctap2Canonical);
        authDataExtension.WriteStartMap(2);
        authDataExtension.WriteInt32(3);  // alg key
        authDataExtension.WriteInt32(CoseAlgorithm.Es256.Value);
        authDataExtension.WriteInt32(4);  // flags key
        authDataExtension.WriteInt32((byte)WebAuthnPreviewSign.PreviewSignFlags.RequireUserVerification);
        authDataExtension.WriteEndMap();

        // Build unsignedExtensionOutputs["previewSign"] = {7: att-obj}
        var unsignedExtension = new CborWriter(CborConformanceMode.Ctap2Canonical);
        unsignedExtension.WriteStartMap(1);
        unsignedExtension.WriteInt32(7);  // att-obj key
        unsignedExtension.WriteByteString(attestationObject);
        unsignedExtension.WriteEndMap();

        var authData = BuildAuthDataWithExtensions(new Dictionary<string, byte[]>
        {
            ["previewSign"] = authDataExtension.Encode()
        });

        var parsedAuthData = WebAuthnAuthenticatorData.Decode(authData);

        var unsignedExtensionOutputs = new Dictionary<string, ReadOnlyMemory<byte>>
        {
            ["previewSign"] = unsignedExtension.Encode()
        };

        // Act - parse registration output
        var output = PreviewSignCodec.ParseRegistrationOutput(parsedAuthData, unsignedExtensionOutputs);

        // Assert - GeneratedKey comes from the inner attestation object (previewSign draft v4 §10.2.1)
        Assert.NotNull(output);
        Assert.NotNull(output.GeneratedKey);
        Assert.NotNull(output.GeneratedKey.PublicKey);
        Assert.Equal(CoseAlgorithm.Es256, output.GeneratedKey.Algorithm);
        Assert.Equal(innerCredentialId, output.GeneratedKey.KeyHandle.ToArray());
        Assert.NotNull(output.GeneratedKey.AttestationObject);
        Assert.Equal(innerCredentialId, output.GeneratedKey.AttestationObject.AuthenticatorData.AttestedCredentialData!.CredentialId.ToArray());
    }


PROPOSED SAMPLE — no verdict: core-001:unit:4e15fa5e2d4d3425 / WebAuthn / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignProcessorTests.cs:1–28
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
using Yubico.YubiKit.WebAuthn.Extensions.Processing;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;
using WebAuthnPreviewSign = Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions.PreviewSign;

/// <summary>
///     previewSign authentication through the extension session (Yubico previewSign draft v4 §10.2.1): the client
///     validates signByCredential against allowCredentials and sends the entry of the credential it selected.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:8705722ec03b98c1 / WebAuthn / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/IYubiKeyExtensionsTransportTests.cs:140–160
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // Non-concrete override -> ArgumentException from the shared CTAP2 validation, no connect attempt.
    [Theory]
    [InlineData(ConnectionType.Hid)]
    [InlineData(ConnectionType.All)]
    [InlineData(ConnectionType.Unknown)]
    [InlineData(ConnectionType.HidFido | ConnectionType.SmartCard)]
    public async Task CreateWebAuthnClientAsync_NonConcreteOverride_ThrowsArgumentException(ConnectionType invalid)
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.SmartCard);

        await Assert.ThrowsAsync<ArgumentException>(
            () => device.CreateWebAuthnClientAsync(
                Origin,
                ClientOptions,
                sessionOptions: new SessionCreationOptions { PreferredConnectionType = invalid },
                cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:dd7a8c8d3beb3ad5 / WebAuthn / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/IYubiKeyExtensionsTransportTests.cs:192–193
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class SelectionProbeYubiKey(ConnectionType available) : IYubiKey
    {


PROPOSED SAMPLE — no verdict: core-001:unit:c6719030f9881ef4 / WebAuthn / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/TestSupport/TestPinUvAuthProtocol.cs:1–18
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Ctap2.Pin;

namespace Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;


