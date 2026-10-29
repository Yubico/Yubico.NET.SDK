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
using System.Formats.Cbor;
using Yubico.YubiKit.Fido2.LargeBlobs;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Extensions.Processing;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions.LargeBlob;

/// <summary>
///     WebAuthn largeBlob (L3 §10.1.5) over CTAP largeBlobKey (§12.3) and authenticatorLargeBlobs (§6.10), through the
///     extension sessions: the shape of python-fido2's LargeBlobExtension and yubikit-android's LargeBlobExtension.
/// </summary>
public class LargeBlobProcessorTests
{
    private static readonly byte[] LargeBlobKey = Enumerable.Repeat((byte)0x4B, 32).ToArray();
    private static readonly byte[] StoredBlob = "stored blob"u8.ToArray();

    private readonly IWebAuthnBackend _backend = Substitute.For<IWebAuthnBackend>();
    private readonly ILargeBlobStorage _storage = Substitute.For<ILargeBlobStorage>();

    private ExtensionContext Context(bool largeBlobsSupported = true)
    {
        _backend.CreateLargeBlobStorage(Arg.Any<PinUvAuthTokenSession?>()).Returns(_storage);
        return new ExtensionContext(
            MockFido2Responses.CreateMockAuthenticatorInfo(
                extensions: largeBlobsSupported ? ["largeBlobKey"] : [], largeBlobsSupported: largeBlobsSupported),
            new TestPinUvAuthProtocol(), _backend);
    }

    private static bool RequestsLargeBlobKey(ReadOnlyMemory<byte>? cbor)
    {
        if (cbor is null) return false;
        var reader = new CborReader(cbor.Value, CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap();
        return reader.ReadTextString() == "largeBlobKey" && reader.ReadBoolean();
    }

    // ---- registration (#5) ----

    /// <summary>CTAP §12.3: without rk the authenticator answers INVALID_OPTION, so a non-discoverable registration asks for nothing and reports supported: false (L3 §10.1.5).</summary>
    [Fact]
    [Trait("Audit", "YESDK-1613")]
    public async Task Registration_PreferredWithoutResidentKey_SendsNothingAndReportsUnsupported()
    {
        using var session = RegistrationExtensionSession.Create(
            new RegistrationExtensionInputs(LargeBlob: new LargeBlobInput { Support = LargeBlobSupport.Preferred }),
            ExtensionSessionTests.CreateMockOptions(), requireResidentKey: false, Context());

        Assert.Null(session.PrepareInputs(token: null));
        var result = await session.PrepareOutputsAsync(MockFido2Responses.CreateMockMakeCredentialResponse(),
            WebAuthnAuthenticatorData.Decode(MockFido2Responses.BuildAuthData()), token: null, TestContext.Current.CancellationToken);
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
        var response = MockFido2Responses.CreateMockMakeCredentialResponse(largeBlobKey: keyReturned ? LargeBlobKey : null);
        var result = await session.PrepareOutputsAsync(response,
            WebAuthnAuthenticatorData.Decode(response.AuthenticatorDataRaw), token: null, TestContext.Current.CancellationToken);

        Assert.Equal(keyReturned, result?.LargeBlob?.Supported);
    }

    /// <summary>L3 §10.1.5: "If support is required and the authenticator does not support large blobs, return ... NotSupportedError".</summary>
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
        var response = MockFido2Responses.CreateMockGetAssertionResponse(largeBlobKey: LargeBlobKey);
        var result = await session.PrepareOutputsAsync(response,
            WebAuthnAuthenticatorData.Decode(response.AuthenticatorDataRaw), token: null, TestContext.Current.CancellationToken);

        Assert.Equal(StoredBlob, result?.LargeBlob?.Blob?.ToArray());
        Assert.Null(result?.LargeBlob?.Written);
    }

    /// <summary>L3 §10.1.5 write: needs the LargeBlobWrite token permission (CTAP §6.10.2); written is true on success.</summary>
    [Fact]
    public async Task Authentication_Write_NeedsLargeBlobWritePermissionAndReportsWritten()
    {
        byte[] data = "new blob"u8.ToArray();
        using var session = AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(LargeBlob: new LargeBlobInput { Write = data }), allowCredentials: null, Context());
        var token = new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), new byte[32]);

        Assert.Equal(PinUvAuthTokenPermissions.LargeBlobWrite, session.Permissions);
        var response = MockFido2Responses.CreateMockGetAssertionResponse(largeBlobKey: LargeBlobKey);
        var result = await session.PrepareOutputsAsync(response,
            WebAuthnAuthenticatorData.Decode(response.AuthenticatorDataRaw), token, TestContext.Current.CancellationToken);

        _backend.Received(1).CreateLargeBlobStorage(token);
        await _storage.Received(1).SetBlobAsync(
            Arg.Is<ReadOnlyMemory<byte>>(k => k.ToArray().SequenceEqual(LargeBlobKey)),
            Arg.Is<ReadOnlyMemory<byte>>(d => d.ToArray().SequenceEqual(data)), Arg.Any<CancellationToken>());
        Assert.True(result?.LargeBlob?.Written);
    }

    /// <summary>L3 §10.1.5: "set written to true if the write succeeded and false otherwise" — a storage failure is reported, not thrown.</summary>
    [Fact]
    public async Task Authentication_WriteFails_ReportsWrittenFalse()
    {
        _storage.SetBlobAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Fido2.Ctap.CtapException(Fido2.Ctap.CtapStatus.LargeBlobStorageFull));
        using var session = AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(LargeBlob: new LargeBlobInput { Write = new byte[] { 1 } }), allowCredentials: null, Context());
        var response = MockFido2Responses.CreateMockGetAssertionResponse(largeBlobKey: LargeBlobKey);

        var result = await session.PrepareOutputsAsync(response,
            WebAuthnAuthenticatorData.Decode(response.AuthenticatorDataRaw),
            new PinUvAuthTokenSession(new TestPinUvAuthProtocol(), new byte[32]), TestContext.Current.CancellationToken);

        Assert.False(result?.LargeBlob?.Written);
    }

    /// <summary>No largeBlobKey in the assertion (credential made without it): no storage access, no output.</summary>
    [Fact]
    public async Task Authentication_NoKeyInAssertion_ProducesNoOutput()
    {
        using var session = AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(LargeBlob: new LargeBlobInput { Read = true }), allowCredentials: null, Context());
        var response = MockFido2Responses.CreateMockGetAssertionResponse();

        var result = await session.PrepareOutputsAsync(response,
            WebAuthnAuthenticatorData.Decode(response.AuthenticatorDataRaw), token: null, TestContext.Current.CancellationToken);

        Assert.Null(result?.LargeBlob);
        await _storage.DidNotReceive().GetBlobAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
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