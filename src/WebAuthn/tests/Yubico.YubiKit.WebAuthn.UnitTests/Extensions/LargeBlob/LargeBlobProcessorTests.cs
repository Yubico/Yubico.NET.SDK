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
using NSubstitute.ExceptionExtensions;
using System.Buffers.Binary;
using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.LargeBlobs;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Extensions.Outputs;
using Yubico.YubiKit.WebAuthn.Extensions.Processing;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions.LargeBlob;

/// <summary>
///     WebAuthn largeBlob (L3 §10.1.5) over CTAP largeBlobKey (§12.3) and authenticatorLargeBlobs (§6.10), through the
///     extension sessions. These mocked storage tests cover the SDK's supported route, not every
///     WebAuthn input constraint or authenticator capability.
/// </summary>
public class LargeBlobProcessorTests
{
    [Theory]
    [InlineData(true, null)]
    [InlineData(false, 1u)]
    [InlineData(true, 2u)]
    [InlineData(false, 2u)]
    public async Task Authentication_WriteUsesTheSelectedAssertionsStorageKey(bool multiEntry, uint? assertionCount)
    {
        var storage = Context().Backend;
        storage.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(new Yubico.YubiKit.Ctap2.AuthenticatorInfo
        {
            Extensions = ["largeBlobKey"],
            Options = new Dictionary<string, bool> { ["largeBlobs"] = true, ["clientPin"] = true, ["pinUvAuthToken"] = true }
        });
        var permissions = PinUvAuthTokenPermissions.GetAssertion | PinUvAuthTokenPermissions.LargeBlobWrite;
        var token = new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), new byte[32]);
        storage.GetPinUvTokenAsync(PinUvAuthMethod.Pin, permissions, "example.com", Arg.Any<ReadOnlyMemory<byte>?>(), Arg.Any<CancellationToken>())
            .Returns(token);
        byte[] first = [1];
        byte[] selected = [2];
        byte[] payload = [0x33];
        var assertion = MockCtap2Responses.CreateMockGetAssertionResponse(selected, numberOfCredentials: assertionCount, largeBlobKey: LargeBlobKey);
        storage.GetNextAssertionAsync(Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("A singleton request must not continue."));
        storage.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<BackendGetAssertionRequest>();
            var allowed = Assert.Single(request.AllowList ?? []);
            if (request.Options?.GetValueOrDefault("up") == false)
            {
                if (allowed.Id.Span.SequenceEqual(first))
                    throw new Yubico.YubiKit.Ctap2.CtapException(Yubico.YubiKit.Ctap2.CtapStatus.NoCredentials);
                return MockCtap2Responses.CreateMockGetAssertionResponse(selected, largeBlobKey: new byte[32]);
            }
            Assert.Equal(selected, allowed.Id.Span);
            return assertion;
        });
        _storage.SetBlobAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                Assert.Equal(LargeBlobKey, call.ArgAt<ReadOnlyMemory<byte>>(0).Span);
                Assert.Equal(payload, call.ArgAt<ReadOnlyMemory<byte>>(1).Span);
                return Task.CompletedTask;
            });
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        await using var client = new WebAuthnClient(storage, origin, new() { RpIdValidator = (_, rpId) => rpId == "example.com" });
        var ct = TestContext.Current.CancellationToken;
        var options = new WebAuthn.Client.Authentication.PublicKeyCredentialRequestOptions
        {
            Challenge = new byte[32],
            RpId = "example.com",
            AllowCredentials = multiEntry ? [new(first), new(selected)] : [new(selected)],
            Extensions = new AuthenticationExtensionInputs(LargeBlob: new LargeBlobInput { Write = payload })
        };
        Task<IReadOnlyList<WebAuthn.Client.Authentication.MatchedCredential>> AssertAsync() => client.GetAssertionAsync(options, "123456"u8.ToArray(), ct);
        if (assertionCount == 2)
        {
            var error = await Assert.ThrowsAsync<WebAuthnClientError>(AssertAsync);
            Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code);
            Assert.False(assertion.AuthenticatorDataRaw.Span.ContainsAnyExcept((byte)0));
            Assert.False(assertion.LargeBlobKey.GetValueOrDefault().Span.ContainsAnyExcept((byte)0));
        }
        else
        {
            var result = await Assert.Single(await AssertAsync()).SelectAsync(ct);
            Assert.Equal(selected, result.CredentialId.Span);
            Assert.True(result.ClientExtensionResults?.LargeBlob?.Written);
            storage.Received(1).CreateLargeBlobStorage(token);
        }
        await storage.DidNotReceive().GetNextAssertionAsync(Arg.Any<CancellationToken>());
        await _storage.Received(assertionCount == 2 ? 0 : 1).SetBlobAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Authentication_WriteWithoutOriginalAllowEntriesFailsBeforeIo(bool absent, bool supported)
    {
        var info = Context(supported).Info;
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(info);
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockGetAssertionResponse(largeBlobKey: LargeBlobKey));
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        await using var client = new WebAuthnClient(_backend, origin, new() { RpIdValidator = (_, rpId) => rpId == "example.com" });
        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(new()
        {
            Challenge = new byte[32],
            RpId = "example.com",
            AllowCredentials = absent ? null : [],
            Extensions = new AuthenticationExtensionInputs(LargeBlob: new LargeBlobInput { Write = new byte[] { 1 } })
        }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(WebAuthnClientErrorCode.NotSupported, error.Code);
        await _backend.DidNotReceive().GetCachedInfoAsync(Arg.Any<CancellationToken>());
        await _backend.DidNotReceive().GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>());
        await _storage.DidNotReceive().SetBlobAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
    }

    private static readonly byte[] LargeBlobKey = Enumerable.Repeat((byte)0x4B, 32).ToArray();
    private static readonly byte[] StoredBlob = "stored blob"u8.ToArray();

    private readonly IWebAuthnBackend _backend = Substitute.For<IWebAuthnBackend>();
    private readonly ILargeBlobStorage _storage = Substitute.For<ILargeBlobStorage>();

    private ExtensionContext Context(bool largeBlobsSupported = true)
    {
        _backend.CreateLargeBlobStorage(Arg.Any<PinUvAuthTokenSession?>()).Returns(_storage);
        return new ExtensionContext(
            MockCtap2Responses.CreateMockAuthenticatorInfo(
                extensions: largeBlobsSupported ? ["largeBlobKey", "credBlob"] : [], largeBlobsSupported: largeBlobsSupported),
            new TestPinUvAuthProtocol(), _backend);
    }

    private static bool RequestsLargeBlobKey(ReadOnlyMemory<byte>? cbor)
    {
        if (cbor is null) return false;
        var reader = new CborReader(cbor.Value, CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap();
        return reader.ReadTextString() == "largeBlobKey" && reader.ReadBoolean();
    }

    // ---- registration ----

    /// <summary>CTAP §12.3 requires rk:true for a key request; this Preferred non-discoverable fixture sends none and reports false.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1613")]
    public async Task Registration_PreferredWithoutResidentKey_SendsNothingAndReportsUnsupported()
    {
        using var session = RegistrationExtensionSession.Create(
            new RegistrationExtensionInputs(LargeBlob: new LargeBlobInput { Support = LargeBlobSupport.Preferred }),
            ExtensionSessionTests.CreateMockOptions(), requireResidentKey: false, Context());

        Assert.Null(session.PrepareInputs(token: null));
        var result = await session.PrepareOutputsAsync(MockCtap2Responses.CreateMockMakeCredentialResponse(),
            WebAuthnAuthenticatorData.Decode(MockCtap2Responses.BuildAuthData()), token: null, TestContext.Current.CancellationToken);
        Assert.False(result?.LargeBlob?.Supported);
    }

    /// <summary>CTAP §12.3: the key comes back in response key 0x05, "not in the extensions field of the authenticator data"; L3 §10.1.5: supported iff the credential supports large blobs.</summary>
    [Theory]
    [Trait("Audit", "YESDK-1613")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Registration_WithResidentKey_RequestsKeyAndReportsSupportedFromResponseKey5(bool keyReturned)
    {
        using var session = RegistrationExtensionSession.Create(
            new RegistrationExtensionInputs(LargeBlob: new LargeBlobInput { Support = LargeBlobSupport.Preferred }),
            ExtensionSessionTests.CreateMockOptions(), requireResidentKey: true, Context());

        Assert.True(RequestsLargeBlobKey(session.PrepareInputs(token: null)));
        var response = MockCtap2Responses.CreateMockMakeCredentialResponse(largeBlobKey: keyReturned ? LargeBlobKey : null);
        var result = await session.PrepareOutputsAsync(response,
            WebAuthnAuthenticatorData.Decode(response.AuthenticatorDataRaw), token: null, TestContext.Current.CancellationToken);

        Assert.Equal(keyReturned, result?.LargeBlob?.Supported);
    }

    /// <summary>The SDK rejects Required when this authenticator lacks its supported storage route; this does not cover every Required registration path.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1613")]
    public void Registration_RequiredOnUnsupportingAuthenticator_IsNotSupported()
    {
        var error = Assert.Throws<WebAuthnClientError>(() => RegistrationExtensionSession.Create(
            new RegistrationExtensionInputs(LargeBlob: new LargeBlobInput { Support = LargeBlobSupport.Required }),
            ExtensionSessionTests.CreateMockOptions(), requireResidentKey: true, Context(largeBlobsSupported: false)));

        Assert.Equal(WebAuthnClientErrorCode.NotSupported, error.Code);
    }

    /// <summary>L3 §10.1.5 registration: "If read or write is present, return a DOMException whose name is NotSupportedError".</summary>
    [Fact]
    [Trait("Audit", "YESDK-1613")]
    public void Registration_ReadOrWrite_IsNotSupported()
    {
        var error = Assert.Throws<WebAuthnClientError>(() => RegistrationExtensionSession.Create(
            new RegistrationExtensionInputs(LargeBlob: new LargeBlobInput { Read = true }),
            ExtensionSessionTests.CreateMockOptions(), requireResidentKey: true, Context()));

        Assert.Equal(WebAuthnClientErrorCode.NotSupported, error.Code);
    }

    // ---- authentication ----

    /// <summary>L3 §10.1.5 read: the blob is read with the assertion's largeBlobKey and returned as blob.</summary>
    [Fact]
    public async Task Authentication_Read_ReturnsTheBlobForTheAssertionsKey()
    {
        _storage.GetBlobAsync(Arg.Is<ReadOnlyMemory<byte>>(k => k.ToArray().SequenceEqual(LargeBlobKey)), Arg.Any<CancellationToken>())
            .Returns(StoredBlob);
        using var session = AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(LargeBlob: new LargeBlobInput { Read = true }), allowCredentials: null, Context());

        Assert.True(RequestsLargeBlobKey(session.PrepareInputs(selected: null, token: null)));
        Assert.Equal(PinUvAuthTokenPermissions.None, session.Permissions);
        var response = MockCtap2Responses.CreateMockGetAssertionResponse(largeBlobKey: LargeBlobKey);
        var result = await session.PrepareOutputsAsync(response,
            WebAuthnAuthenticatorData.Decode(response.AuthenticatorDataRaw), token: null, TestContext.Current.CancellationToken);

        Assert.Equal(StoredBlob, result?.LargeBlob?.Blob?.ToArray());
        Assert.Null(result?.LargeBlob?.Written);
    }

    /// <summary>The SDK requests LargeBlobWrite for a write and reports true when mocked storage succeeds.</summary>
    /// <remarks>
    /// The null allow list exercises a current validation gap, not WebAuthn permission to write
    /// without exactly one original allowed credential. CTAP itself also permits pinless writes in
    /// unconfigured-device cases that the current SDK storage path does not support.
    /// </remarks>
    [Fact]
    public async Task Authentication_Write_NeedsLargeBlobWritePermissionAndReportsWritten()
    {
        byte[] data = "new blob"u8.ToArray();
        using var session = AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(LargeBlob: new LargeBlobInput { Write = data }), allowCredentials: null, Context());
        var token = new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), new byte[32]);

        Assert.Equal(PinUvAuthTokenPermissions.LargeBlobWrite, session.Permissions);
        var response = MockCtap2Responses.CreateMockGetAssertionResponse(largeBlobKey: LargeBlobKey);
        var result = await session.PrepareOutputsAsync(response,
            WebAuthnAuthenticatorData.Decode(response.AuthenticatorDataRaw), token, TestContext.Current.CancellationToken);

        _backend.Received(1).CreateLargeBlobStorage(token);
        await _storage.Received(1).SetBlobAsync(
            Arg.Is<ReadOnlyMemory<byte>>(k => k.ToArray().SequenceEqual(LargeBlobKey)),
            Arg.Is<ReadOnlyMemory<byte>>(d => d.ToArray().SequenceEqual(data)), Arg.Any<CancellationToken>());
        Assert.True(result?.LargeBlob?.Written);
    }

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

    [Theory]
    [InlineData(0, false)] // no key
    [InlineData(1, false)] // no stored blob
    [InlineData(2, true)] // stored empty blob
    [InlineData(3, false)] // storage failure
    [InlineData(4, false)] // malformed returned key, no storage I/O
    [InlineData(5, false)] // unsupported storage route, even with an unsolicited key
    [InlineData(6, false)] // transport I/O failure
    public async Task Authentication_ReadKeepsTheExtensionPresent(int scenario, bool hasBlob)
    {
        var info = Context(largeBlobsSupported: scenario != 5).Info;
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(info);
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<BackendGetAssertionRequest>();
                Assert.Equal(scenario != 5, RequestsLargeBlobKey(request.Extensions));
                return MockCtap2Responses.CreateMockGetAssertionResponse(largeBlobKey: scenario switch
                {
                    0 => null,
                    4 => [0x42],
                    _ => LargeBlobKey
                });
            });
        _storage.GetBlobAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>()).Returns(_ => scenario switch
        {
            3 => throw new Yubico.YubiKit.Ctap2.CtapException(Yubico.YubiKit.Ctap2.CtapStatus.InvalidCbor),
            6 => throw new IOException("HID read failed"),
            2 => Array.Empty<byte>(),
            _ => (byte[]?)null
        });
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        await using var client = new WebAuthnClient(_backend, origin, new() { RpIdValidator = (_, rpId) => rpId == "example.com" });
        var ct = TestContext.Current.CancellationToken;
        var matches = await client.GetAssertionAsync(new()
        {
            Challenge = new byte[32],
            RpId = "example.com",
            Extensions = new AuthenticationExtensionInputs(LargeBlob: new LargeBlobInput { Read = true })
        }, cancellationToken: ct);
        var response = await Assert.Single(matches).SelectAsync(ct);
        var result = Assert.IsType<LargeBlobAuthenticationOutput>(response.ClientExtensionResults?.LargeBlob);
        Assert.Equal(hasBlob, result.Blob.HasValue);
        if (result.Blob is { } blob) Assert.True(blob.IsEmpty);
        await _storage.Received(scenario is 0 or 4 or 5 ? 0 : 1).GetBlobAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Authentication_ReadKeepsAssertionWhenDecryptedEntryHasWrongInflatedSize(int declaredSize)
    {
        var ctap = Substitute.For<ICtap2Session>();
        var info = Context().Info;
        ctap.GetInfoAsync(Arg.Any<CancellationToken>()).Returns(info);
        var writer = new CborWriter();
        writer.WriteStartMap(1);
        writer.WriteInt32(1); // authenticatorLargeBlobs response config
        writer.WriteByteString(BuildStoredEntry(declaredSize));
        writer.WriteEndMap();
        ctap.SendCborRequestAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns((ReadOnlyMemory<byte>)writer.Encode());
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(info);
        _backend.CreateLargeBlobStorage(null).Returns(new LargeBlobStorage(ctap));
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockGetAssertionResponse(credentialId: [0x44], largeBlobKey: LargeBlobKey));

        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        await using var client = new WebAuthnClient(_backend, origin, new() { RpIdValidator = (_, rpId) => rpId == "example.com" });
        var ct = TestContext.Current.CancellationToken;
        var matches = await client.GetAssertionAsync(new()
        {
            Challenge = new byte[32],
            RpId = "example.com",
            Extensions = new AuthenticationExtensionInputs(LargeBlob: new LargeBlobInput { Read = true })
        }, cancellationToken: ct);
        var response = await Assert.Single(matches).SelectAsync(ct);
        Assert.Equal(new byte[] { 0x44 }, response.CredentialId.ToArray());
        var output = Assert.IsType<LargeBlobAuthenticationOutput>(response.ClientExtensionResults?.LargeBlob);
        if (declaredSize == 1)
            Assert.Equal(new byte[] { 0x41 }, Assert.IsType<Memory<byte>>(output.Blob).ToArray());
        else
            Assert.Null(output.Blob);
        await ctap.Received(1).SendCborRequestAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
    }

    private static byte[] BuildStoredEntry(int declaredSize)
    {
        ReadOnlySpan<byte> compressed = [0x73, 0x04, 0x00]; // raw DEFLATE for one byte, 0x41
        Span<byte> aad = stackalloc byte[12];
        "blob"u8.CopyTo(aad);
        BinaryPrimitives.WriteUInt64LittleEndian(aad[4..], (ulong)declaredSize);
        byte[] nonce = new byte[12];
        byte[] ciphertext = new byte[compressed.Length + 16];
        using var aes = new AesGcm(LargeBlobKey, 16);
        aes.Encrypt(nonce, compressed, ciphertext.AsSpan(0, compressed.Length), ciphertext.AsSpan(compressed.Length), aad);
        return LargeBlobArray.CreateEmpty().WithEntry(new LargeBlobEntry(ciphertext, nonce, (ulong)declaredSize)).Serialize();
    }

    [Theory]
    [InlineData("A16863726564426C6F62416B", true)]
    [InlineData("A16863726564426C6F625F416BFF", false)]
    public async Task Authentication_ReadTransfersPlaintextWithoutClearingVerificationBytes(string signedExtensions, bool definite)
    {
        var info = Context().Info;
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(info);
        var assertion = MockCtap2Responses.CreateMockGetAssertionResponse(largeBlobKey: LargeBlobKey,
            authDataExtensionsCbor: Convert.FromHexString(signedExtensions));
        byte[] raw = assertion.AuthenticatorDataRaw.ToArray();
        byte[] plaintext = [0x6B];
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(assertion);
        _storage.GetBlobAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>()).Returns(plaintext);
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        var ct = TestContext.Current.CancellationToken;
        var client = new WebAuthnClient(_backend, origin, new() { RpIdValidator = (_, rpId) => rpId == "example.com" });
        LargeBlobAuthenticationOutput output;
        ReadOnlyMemory<byte> credentialBlob;
        await using (client)
        {
            var matches = await client.GetAssertionAsync(new()
            {
                Challenge = new byte[32],
                RpId = "example.com",
                Extensions = new AuthenticationExtensionInputs(GetCredBlob: true, LargeBlob: new LargeBlobInput { Read = true })
            }, cancellationToken: ct);
            var response = await Assert.Single(matches).SelectAsync(ct);
            output = Assert.IsType<LargeBlobAuthenticationOutput>(response.ClientExtensionResults?.LargeBlob);
            credentialBlob = Assert.IsType<Yubico.YubiKit.Ctap2.Extensions.CredBlobAssertionOutput>(response.ClientExtensionResults?.CredBlob).Blob;
            Assert.Equal(definite, credentialBlob.Equals(response.AuthenticatorData.ParsedExtensions["credBlob"][1..]));
            Assert.Equal(raw, response.RawAuthenticatorData.Span);
            Assert.True(assertion.AuthenticatorData.RawData.Equals(response.AuthenticatorData.Raw));
            Assert.False(assertion.LargeBlobKey.GetValueOrDefault().Span.ContainsAnyExcept((byte)0));
        }

        var copy = output with { };
        Assert.Equal(new byte[] { 0x6B }, credentialBlob.ToArray());
        Memory<byte> actual = Assert.IsType<Memory<byte>>(output.Blob);
        Assert.Equal(0x6B, actual.Span[0]);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(actual.Span);
        Assert.Equal(0, copy.Blob.GetValueOrDefault().Span[0]);
        Assert.Equal(0, plaintext[0]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Authentication_ReadCancellationClearsUnreturnedPlaintext(bool laterAssertion)
    {
        var info = Context().Info;
        _backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(info);
        // A signed raw extension slice must share the same owned backing that the ceremony clears.
        var first = MockCtap2Responses.CreateMockGetAssertionResponse(numberOfCredentials: laterAssertion ? 2u : 1u, largeBlobKey: LargeBlobKey,
            authDataExtensionsCbor: Convert.FromHexString("A16863726564426C6F62416B")); // {credBlob: h'6B'}
        var parsed = WebAuthnAuthenticatorData.FromOwned(first.AuthenticatorData);
        var extensionSlice = parsed.ParsedExtensions["credBlob"];
        Assert.Equal(new byte[] { 0x41, 0x6B }, extensionSlice.ToArray());
        var credentialBlob = Assert.IsType<Yubico.YubiKit.Ctap2.Extensions.CredBlobAssertionOutput>(
            WebAuthn.Extensions.Codec.CredBlobCodec.ParseAuthenticationOutput(parsed.ParsedExtensions));
        Assert.True(credentialBlob.Blob.Equals(extensionSlice[1..]));
        var second = MockCtap2Responses.CreateMockGetAssertionResponse(largeBlobKey: LargeBlobKey);
        _backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(first);
        _backend.GetNextAssertionAsync(Arg.Any<CancellationToken>()).Returns(second);
        byte[] plaintext = [0x6B];
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        int reads = 0;
        _storage.GetBlobAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (++reads == 1 && laterAssertion)
                return Task.FromResult<byte[]?>(plaintext);
            caller.Cancel();
            return Task.FromCanceled<byte[]?>(caller.Token);
        });
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        await using var client = new WebAuthnClient(_backend, origin, new() { RpIdValidator = (_, rpId) => rpId == "example.com" });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAssertionAsync(new()
        {
            Challenge = new byte[32],
            RpId = "example.com",
            Extensions = new AuthenticationExtensionInputs(GetCredBlob: true, LargeBlob: new LargeBlobInput { Read = true })
        }, cancellationToken: caller.Token));

        Assert.Equal(laterAssertion ? 2 : 1, reads);
        if (laterAssertion)
        {
            Assert.Equal(0, plaintext[0]);
            Assert.False(second.AuthenticatorDataRaw.Span.ContainsAnyExcept((byte)0));
        }
        Assert.False(first.AuthenticatorDataRaw.Span.ContainsAnyExcept((byte)0));
        Assert.False(parsed.Raw.Span.ContainsAnyExcept((byte)0));
        Assert.False(extensionSlice.Span.ContainsAnyExcept((byte)0));
        Assert.Equal(0, credentialBlob.Blob.Span[0]);
    }

    /// <summary>L3 §10.1.5 authentication: "If support is present, or if both read and write are present, return ... NotSupportedError".</summary>
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    public void Authentication_InvalidInputCombination_IsNotSupported(bool support, bool read, bool write)
    {
        var input = new LargeBlobInput
        {
            Support = support ? LargeBlobSupport.Preferred : null,
            Read = read,
            Write = write ? new byte[] { 1 } : null
        };

        var error = Assert.Throws<WebAuthnClientError>(() => AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(LargeBlob: input), allowCredentials: null, Context()));

        Assert.Equal(WebAuthnClientErrorCode.NotSupported, error.Code);
    }
}