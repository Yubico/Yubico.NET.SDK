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
using System.Formats.Cbor;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Extensions.Processing;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions.Prf;

/// <summary>
///     PRF through the extension sessions: WebAuthn L3 §10.1.4 client processing on top of CTAP 2.1-2.3 §12.7
///     hmac-secret, as python-fido2's HmacSecretExtension and yubikit-android's HmacSecretExtension do it.
///     The shared secret is the L3 §16.17.1.2 vector so the wire bytes are checkable.
/// </summary>
public class PrfProcessorTests
{
    private const int KeyAgreementKey = 1;   // CTAP §12.7 hmac-secret input map
    private const int SaltEncKey = 2;
    private const int SaltAuthKey = 3;
    private const int PinUvAuthProtocolKey = 4;

    private static readonly byte[] PrfEvalFirst = Convert.FromHexString("576562417574686e20505246207465737420766563746f727302");
    private static readonly byte[] PrfEvalSecond = Convert.FromHexString("576562417574686e20505246207465737420766563746f727303");
    private static readonly byte[] Salt1 = Convert.FromHexString("527413ebb48293772df30f031c5ac4650c7de14bf9498671ae163447b6a772b3");
    private static readonly byte[] Salt2 = Convert.FromHexString("d68ac03329a10ee5e0ec834492bb9a96a0e547baf563bf78ccbe8789b22e776b");
    private static readonly byte[] Output1 = Convert.FromHexString("3c33e07d202c3b029cc21f1722767021bf27d595933b3d2b6a1b9d5dddc77fae");
    private static readonly byte[] Output2 = Convert.FromHexString("a62a8773b19cda90d7ed4ef72a80a804320dbd3997e2f663805ad1fd3293d50b");
    private static readonly byte[] SharedSecretV2 = Convert.FromHexString(
        "0c63083de8170101d38bcf8bd72309568ddb4550867e23404b35d85712f7c20d8bc911ee23c06034cbc14290b9669bec07739053c5a416e313ef905c79955876");
    private static readonly byte[] OutputEncV2Two = Convert.FromHexString(
        "90ee52f739043bc17b3488a74306d7801debb5b61f18662c648a25b5b5678ede482cdaff99a537a44f064fcb10ce6e04dfd27619dc96a0daff8507e499296b1eecf0981f7c8518b277a7a3018f5ec6fb");

    private readonly PinUvAuthProtocolV2 _protocol = new();
    private readonly IWebAuthnBackend _backend = Substitute.For<IWebAuthnBackend>();
    private readonly Dictionary<int, object?> _platformKey = new() { [1] = 2, [3] = -25, [-1] = 1, [-2] = new byte[32], [-3] = new byte[32] };

    private ExtensionContext Context(params string[] extensions)
    {
        _backend.EncapsulateAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new PinUvSharedSecret(_platformKey, (byte[])SharedSecretV2.Clone()));
        return new ExtensionContext(
            MockFido2Responses.CreateMockAuthenticatorInfo(extensions: extensions), _protocol, _backend);
    }

    private static Dictionary<int, object?> ReadHmacSecretInput(ReadOnlyMemory<byte> extensionsCbor)
    {
        var reader = new CborReader(extensionsCbor, CborConformanceMode.Ctap2Canonical);
        Assert.Equal(1, reader.ReadStartMap());
        Assert.Equal("hmac-secret", reader.ReadTextString());
        var count = reader.ReadStartMap();
        var map = new Dictionary<int, object?>();
        for (var i = 0; i < count; i++)
        {
            var key = reader.ReadInt32();
            map[key] = reader.PeekState() switch
            {
                CborReaderState.ByteString => reader.ReadByteString(),
                CborReaderState.UnsignedInteger or CborReaderState.NegativeInteger => reader.ReadInt32(),
                CborReaderState.StartMap => ReadCoseKey(reader),
                _ => throw new InvalidOperationException(reader.PeekState().ToString())
            };
        }
        return map;
    }

    private static Dictionary<int, object?> ReadCoseKey(CborReader reader)
    {
        var count = reader.ReadStartMap();
        var key = new Dictionary<int, object?>();
        for (var i = 0; i < count; i++)
        {
            var k = reader.ReadInt32();
            key[k] = reader.PeekState() == CborReaderState.ByteString ? reader.ReadByteString() : reader.ReadInt32();
        }
        reader.ReadEndMap();
        return key;
    }

    /// <summary>L3 §10.1.4 registration: "Set hmac-secret to true in the authenticator extensions input." No key agreement needed.</summary>
    [Fact]
    public async Task Registration_RequestsHmacSecretTrue_WithoutKeyAgreement()
    {
        var context = Context("hmac-secret");
        var session = RegistrationExtensionSession.Create(
            new RegistrationExtensionInputs(Prf: new PrfInput()), ExtensionSessionTests.CreateMockOptions(), requireResidentKey: false, context);

        await session.PrepareAsync(TestContext.Current.CancellationToken);
        var cbor = session.PrepareInputs(token: null);

        var reader = new CborReader(cbor!.Value, CborConformanceMode.Ctap2Canonical);
        Assert.Equal(1, reader.ReadStartMap());
        Assert.Equal("hmac-secret", reader.ReadTextString());
        Assert.True(reader.ReadBoolean());
        await _backend.DidNotReceive().EncapsulateAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Registration output: hmac-secret boolean → enabled (L3 §10.1.4).</summary>
    [Fact]
    public async Task Registration_HmacSecretTrueOutput_ReportsEnabled()
    {
        var context = Context("hmac-secret");
        var session = RegistrationExtensionSession.Create(
            new RegistrationExtensionInputs(Prf: new PrfInput()), ExtensionSessionTests.CreateMockOptions(), requireResidentKey: false, context);
        var outputs = new CborWriter(CborConformanceMode.Ctap2Canonical);
        outputs.WriteStartMap(1);
        outputs.WriteTextString("hmac-secret");
        outputs.WriteBoolean(true);
        outputs.WriteEndMap();
        var authData = WebAuthnAuthenticatorData.Decode(MockFido2Responses.BuildAuthData(outputs.Encode()));

        var result = await session.PrepareOutputsAsync(
            MockFido2Responses.CreateMockMakeCredentialResponse(), authData, token: null, TestContext.Current.CancellationToken);

        Assert.True(result?.Prf?.Enabled);
    }

    /// <summary>
    ///     L3 §10.1.4 authentication: salts from eval, one key agreement, CTAP §12.7 map {1: keyAgreement, 2: saltEnc,
    ///     3: saltAuth, 4: protocol}; saltEnc decrypts to salt1 || salt2 and saltAuth authenticates saltEnc.
    /// </summary>
    [Fact]
    public async Task Authentication_SendsEncryptedAndAuthenticatedSalts()
    {
        var context = Context("hmac-secret");
        var session = AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(Prf: new PrfInput { First = PrfEvalFirst, Second = PrfEvalSecond }),
            allowCredentials: null, context);

        await session.PrepareAsync(TestContext.Current.CancellationToken);
        var cbor = session.PrepareInputs(selected: null, token: null);

        await _backend.Received(1).EncapsulateAsync(Arg.Any<CancellationToken>());
        var input = ReadHmacSecretInput(cbor!.Value);
        Assert.Equal(2, ((Dictionary<int, object?>)input[KeyAgreementKey]!)[1]);
        var saltEnc = (byte[])input[SaltEncKey]!;
        Assert.Equal([.. Salt1, .. Salt2], _protocol.Decrypt(SharedSecretV2, saltEnc));
        Assert.Equal(_protocol.Authenticate(SharedSecretV2, saltEnc), (byte[])input[SaltAuthKey]!);
        Assert.Equal(2, input[PinUvAuthProtocolKey]);
    }

    /// <summary>The assertion's hmac-secret output is decrypted with the same shared secret and split into results.</summary>
    [Fact]
    public async Task Authentication_DecryptsResultsWithTheSessionSharedSecret()
    {
        var context = Context("hmac-secret");
        var session = AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(Prf: new PrfInput { First = PrfEvalFirst, Second = PrfEvalSecond }),
            allowCredentials: null, context);
        await session.PrepareAsync(TestContext.Current.CancellationToken);
        _ = session.PrepareInputs(selected: null, token: null);
        var outputs = new CborWriter(CborConformanceMode.Ctap2Canonical);
        outputs.WriteStartMap(1);
        outputs.WriteTextString("hmac-secret");
        outputs.WriteByteString(OutputEncV2Two);
        outputs.WriteEndMap();
        var authData = WebAuthnAuthenticatorData.Decode(MockFido2Responses.BuildAuthData(outputs.Encode()));

        var result = await session.PrepareOutputsAsync(
            MockFido2Responses.CreateMockGetAssertionResponse(), authData, token: null, TestContext.Current.CancellationToken);

        Assert.Equal(Output1, result?.Prf?.Results.First.ToArray());
        Assert.Equal(Output2, result?.Prf?.Results.Second?.ToArray());
    }

    /// <summary>evalByCredential keyed on the selected credential wins over eval (L3 §10.1.4 "let ev be the value of that entry").</summary>
    [Fact]
    public async Task Authentication_EvalByCredential_UsesTheSelectedCredentialsInputs()
    {
        var context = Context("hmac-secret");
        var selected = new PublicKeyCredentialDescriptor(new byte[] { 0x01, 0x02 });
        var other = new PublicKeyCredentialDescriptor(new byte[] { 0x03 });
        var input = new PrfInput
        {
            First = "fallback"u8.ToArray(),
            EvalByCredential = new Dictionary<string, PrfInputValues>
            {
                ["AQI"] = new() { First = PrfEvalFirst },      // base64url(0x0102)
                ["Aw"] = new() { First = "other"u8.ToArray() } // base64url(0x03)
            }
        };
        var session = AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(Prf: input), [other, selected], context);

        await session.PrepareAsync(TestContext.Current.CancellationToken);
        var cbor = session.PrepareInputs(selected, token: null);

        var saltEnc = (byte[])ReadHmacSecretInput(cbor!.Value)[SaltEncKey]!;
        Assert.Equal(Salt1, _protocol.Decrypt(SharedSecretV2, saltEnc));
    }

    /// <summary>No evaluation requested at authentication: nothing to send, no key agreement.</summary>
    [Fact]
    public async Task Authentication_NoEval_SendsNothing()
    {
        var context = Context("hmac-secret");
        var session = AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(Prf: new PrfInput()), allowCredentials: null, context);

        await session.PrepareAsync(TestContext.Current.CancellationToken);

        Assert.Null(session.PrepareInputs(selected: null, token: null));
        await _backend.DidNotReceive().EncapsulateAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Undecryptable output: the PRF result is omitted, the assertion is not failed (android parity, L3 §9.4).</summary>
    [Fact]
    public async Task Authentication_UndecryptableOutput_OmitsPrfResult()
    {
        var context = Context("hmac-secret");
        var session = AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(Prf: new PrfInput { First = PrfEvalFirst }), allowCredentials: null, context);
        await session.PrepareAsync(TestContext.Current.CancellationToken);
        _ = session.PrepareInputs(selected: null, token: null);
        var outputs = new CborWriter(CborConformanceMode.Ctap2Canonical);
        outputs.WriteStartMap(1);
        outputs.WriteTextString("hmac-secret");
        outputs.WriteByteString(new byte[7]); // not even a whole AES block
        outputs.WriteEndMap();
        var authData = WebAuthnAuthenticatorData.Decode(MockFido2Responses.BuildAuthData(outputs.Encode()));

        var result = await session.PrepareOutputsAsync(
            MockFido2Responses.CreateMockGetAssertionResponse(), authData, token: null, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Null(result.Prf);
    }

    // ---- 26a: L3 §10.1.4 validation of evalByCredential ----

    /// <summary>L3 §10.1.4 registration: "If evalByCredential is present, return a DOMException whose name is NotSupportedError".</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void Registration_EvalByCredential_IsNotSupported()
    {
        var context = Context("hmac-secret");
        var input = new PrfInput { EvalByCredential = new Dictionary<string, PrfInputValues> { ["AQ"] = new() { First = PrfEvalFirst } } };

        var error = Assert.Throws<WebAuthnClientError>(() => RegistrationExtensionSession.Create(
            new RegistrationExtensionInputs(Prf: input), ExtensionSessionTests.CreateMockOptions(), requireResidentKey: false, context));

        Assert.Equal(WebAuthnClientErrorCode.NotSupported, error.Code);
    }

    /// <summary>L3 §10.1.4: "If evalByCredential is not empty but allowCredentials is empty, return ... NotSupportedError".</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void Authentication_EvalByCredentialWithoutAllowList_IsNotSupported()
    {
        var context = Context("hmac-secret");
        var input = new PrfInput { EvalByCredential = new Dictionary<string, PrfInputValues> { ["AQ"] = new() { First = PrfEvalFirst } } };

        var error = Assert.Throws<WebAuthnClientError>(() => AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(Prf: input), allowCredentials: null, context));

        Assert.Equal(WebAuthnClientErrorCode.NotSupported, error.Code);
    }

    /// <summary>
    ///     L3 §10.1.4: "If any key in evalByCredential is the empty string, or is not a valid base64url encoding, or does
    ///     not equal the id of some element of allowCredentials ... return a DOMException whose name is SyntaxError".
    /// </summary>
    [Theory]
    [Trait("Audit", "YESDK-1634")]
    [InlineData("")]
    [InlineData("!!")]
    [InlineData("AQID")] // valid base64url, but 0x010203 is not in the allow list
    public void Authentication_EvalByCredentialKeyNotAnAllowedCredential_IsInvalidRequest(string key)
    {
        var context = Context("hmac-secret");
        var input = new PrfInput { EvalByCredential = new Dictionary<string, PrfInputValues> { [key] = new() { First = PrfEvalFirst } } };

        var error = Assert.Throws<WebAuthnClientError>(() => AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(Prf: input), [new PublicKeyCredentialDescriptor(new byte[] { 0x01 })], context));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, error.Code);
    }

    /// <summary>L3 §10.1.4: "If ev is null and eval is present, then let ev be the value of eval."</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task Authentication_NoEntryForTheSelectedCredential_FallsBackToEval()
    {
        var context = Context("hmac-secret");
        var listed = new PublicKeyCredentialDescriptor(new byte[] { 0x01 });
        var selected = new PublicKeyCredentialDescriptor(new byte[] { 0x02 });
        var input = new PrfInput
        {
            First = PrfEvalFirst,
            EvalByCredential = new Dictionary<string, PrfInputValues> { ["AQ"] = new() { First = "listed"u8.ToArray() } }
        };
        var session = AuthenticationExtensionSession.Create(new AuthenticationExtensionInputs(Prf: input), [listed, selected], context);

        await session.PrepareAsync(TestContext.Current.CancellationToken);
        var cbor = session.PrepareInputs(selected, token: null);

        var saltEnc = (byte[])ReadHmacSecretInput(cbor!.Value)[SaltEncKey]!;
        Assert.Equal(Salt1, _protocol.Decrypt(SharedSecretV2, saltEnc));
    }

    // ---- 26b: enabled and registration-time evaluation ----

    /// <summary>L3 §10.1.4: enabled "If not present, set enabled to false" — the output is present, not null.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task Registration_NoHmacSecretOutput_ReportsEnabledFalse()
    {
        var context = Context("hmac-secret");
        var session = RegistrationExtensionSession.Create(
            new RegistrationExtensionInputs(Prf: new PrfInput()), ExtensionSessionTests.CreateMockOptions(), requireResidentKey: false, context);
        var authData = WebAuthnAuthenticatorData.Decode(MockFido2Responses.BuildAuthData());

        var result = await session.PrepareOutputsAsync(
            MockFido2Responses.CreateMockMakeCredentialResponse(), authData, token: null, TestContext.Current.CancellationToken);

        Assert.NotNull(result?.Prf);
        Assert.False(result.Prf.Enabled);
    }

    /// <summary>A non-boolean hmac-secret output reports disabled, never enabled, and does not fail the registration.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task Registration_NonBooleanHmacSecretOutput_ReportsEnabledFalse()
    {
        var context = Context("hmac-secret");
        var session = RegistrationExtensionSession.Create(
            new RegistrationExtensionInputs(Prf: new PrfInput()), ExtensionSessionTests.CreateMockOptions(), requireResidentKey: false, context);
        var outputs = new CborWriter(CborConformanceMode.Ctap2Canonical);
        outputs.WriteStartMap(1);
        outputs.WriteTextString("hmac-secret");
        outputs.WriteInt32(1);
        outputs.WriteEndMap();
        var authData = WebAuthnAuthenticatorData.Decode(MockFido2Responses.BuildAuthData(outputs.Encode()));

        var result = await session.PrepareOutputsAsync(
            MockFido2Responses.CreateMockMakeCredentialResponse(), authData, token: null, TestContext.Current.CancellationToken);

        Assert.NotNull(result?.Prf);
        Assert.False(result.Prf.Enabled);
    }

    /// <summary>
    ///     CTAP 2.2-2.3 §12.8 hmac-secret-mc: with eval at registration on an authenticator that advertises it, send
    ///     "hmac-secret": true plus "hmac-secret-mc" with the getAssertion input map, and decrypt the result.
    /// </summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task Registration_EvalOnAuthenticatorWithHmacSecretMc_SendsBothAndDecryptsResults()
    {
        var context = Context("hmac-secret", "hmac-secret-mc");
        var session = RegistrationExtensionSession.Create(
            new RegistrationExtensionInputs(Prf: new PrfInput { First = PrfEvalFirst, Second = PrfEvalSecond }),
            ExtensionSessionTests.CreateMockOptions(), requireResidentKey: false, context);

        await session.PrepareAsync(TestContext.Current.CancellationToken);
        var cbor = session.PrepareInputs(token: null);

        var reader = new CborReader(cbor!.Value, CborConformanceMode.Ctap2Canonical);
        Assert.Equal(2, reader.ReadStartMap());
        Assert.Equal("hmac-secret", reader.ReadTextString());
        Assert.True(reader.ReadBoolean());
        Assert.Equal("hmac-secret-mc", reader.ReadTextString());
        Assert.Equal(4, reader.ReadStartMap());

        var outputs = new CborWriter(CborConformanceMode.Ctap2Canonical);
        outputs.WriteStartMap(2);
        outputs.WriteTextString("hmac-secret"); outputs.WriteBoolean(true);
        outputs.WriteTextString("hmac-secret-mc"); outputs.WriteByteString(OutputEncV2Two);
        outputs.WriteEndMap();
        var authData = WebAuthnAuthenticatorData.Decode(MockFido2Responses.BuildAuthData(outputs.Encode()));
        var result = await session.PrepareOutputsAsync(
            MockFido2Responses.CreateMockMakeCredentialResponse(), authData, token: null, TestContext.Current.CancellationToken);

        Assert.NotNull(result?.Prf);
        Assert.True(result.Prf.Enabled);
        Assert.Equal(Output1, result.Prf.Results?.First.ToArray());
        Assert.Equal(Output2, result.Prf.Results?.Second?.ToArray());
    }

    /// <summary>Without hmac-secret-mc support, eval at registration sends only hmac-secret: true (L3: "a future extension ... permits").</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task Registration_EvalWithoutHmacSecretMc_SendsOnlyHmacSecretTrue()
    {
        var context = Context("hmac-secret");
        var session = RegistrationExtensionSession.Create(
            new RegistrationExtensionInputs(Prf: new PrfInput { First = PrfEvalFirst }),
            ExtensionSessionTests.CreateMockOptions(), requireResidentKey: false, context);

        await session.PrepareAsync(TestContext.Current.CancellationToken);
        var cbor = session.PrepareInputs(token: null);

        var reader = new CborReader(cbor!.Value, CborConformanceMode.Ctap2Canonical);
        Assert.Equal(1, reader.ReadStartMap());
        Assert.Equal("hmac-secret", reader.ReadTextString());
        Assert.True(reader.ReadBoolean());
        await _backend.DidNotReceive().EncapsulateAsync(Arg.Any<CancellationToken>());
    }
}