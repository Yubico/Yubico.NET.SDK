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
using Yubico.YubiKit.Fido2.Credentials;
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
public class PreviewSignProcessorTests
{
    private const int KeyHandleKey = 2;  // draft CDDL: key-handle = 2, tbs = 6, additional-args = 7
    private const int TbsKey = 6;

    private static readonly PublicKeyCredentialDescriptor CredentialA = new(new byte[] { 0x01, 0x02 });
    private static readonly PublicKeyCredentialDescriptor CredentialB = new(new byte[] { 0x03, 0x04 });

    private static WebAuthnPreviewSign.PreviewSignSigningParams Params(byte marker) =>
        new(keyHandle: new byte[] { 0xA0, marker }, tbs: new byte[] { 0xB0, marker });

    private static WebAuthnPreviewSign.PreviewSignAuthenticationInput SignBy(params (PublicKeyCredentialDescriptor Credential, byte Marker)[] entries) =>
        WebAuthnPreviewSign.PreviewSignAuthenticationInput.CreateSignByCredential(
            entries.ToDictionary(e => e.Credential.Id, e => Params(e.Marker), ByteArrayKeyComparer.Instance));

    private static AuthenticationExtensionSession Session(
        WebAuthnPreviewSign.PreviewSignAuthenticationInput input,
        IReadOnlyList<PublicKeyCredentialDescriptor>? allowCredentials) =>
        AuthenticationExtensionSession.Create(
            new AuthenticationExtensionInputs(PreviewSign: input), allowCredentials, TestExtensionContext.SupportingAll());

    private static (byte[] KeyHandle, byte[] Tbs) ReadPreviewSignInput(ReadOnlyMemory<byte> cbor)
    {
        var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
        Assert.Equal(1, reader.ReadStartMap());
        Assert.Equal("previewSign", reader.ReadTextString());
        var count = reader.ReadStartMap();
        byte[]? keyHandle = null, tbs = null;
        for (var i = 0; i < count; i++)
        {
            var key = reader.ReadInt32();
            if (key == KeyHandleKey) keyHandle = reader.ReadByteString();
            else if (key == TbsKey) tbs = reader.ReadByteString();
            else reader.SkipValue();
        }
        return (keyHandle!, tbs!);
    }

    /// <summary>Draft v4 §10.2.1: "Let signInputs be extSign.signByCredential[chosenCredentialIdB64]" — the selected credential, not the first entry.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void Authentication_SendsTheSelectedCredentialsEntry_NotTheFirst()
    {
        using var session = Session(SignBy((CredentialA, 0xAA), (CredentialB, 0xBB)), [CredentialA, CredentialB]);

        var cbor = session.PrepareInputs(selected: CredentialB, token: null);

        var (keyHandle, tbs) = ReadPreviewSignInput(cbor!.Value);
        Assert.Equal(new byte[] { 0xA0, 0xBB }, keyHandle);
        Assert.Equal(new byte[] { 0xB0, 0xBB }, tbs);
    }

    /// <summary>Nothing selected (no allowed credential on the key): nothing to sign, no input.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void Authentication_NothingSelected_SendsNoInput()
    {
        using var session = Session(SignBy((CredentialA, 0xAA)), [CredentialA]);

        Assert.Null(session.PrepareInputs(selected: null, token: null));
    }

    /// <summary>Draft v4 §10.2.1: "If the size of extSign.signByCredential does not equal the size of pkOptions.allowCredentials, return ... NotSupportedError".</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void Authentication_MapSizeDiffersFromAllowList_IsNotSupported()
    {
        var error = Assert.Throws<WebAuthnClientError>(() => Session(SignBy((CredentialA, 0xAA)), [CredentialA, CredentialB]));

        Assert.Equal(WebAuthnClientErrorCode.NotSupported, error.Code);
    }

    /// <summary>No allow list at all: the same NotSupportedError (sizes 1 and 0 differ; python-fido2 "sign requires allowCredentials").</summary>
    [Theory]
    [Trait("Audit", "YESDK-1634")]
    [InlineData(false)]
    [InlineData(true)]
    public void Authentication_WithoutAllowList_IsNotSupported(bool emptyList)
    {
        var error = Assert.Throws<WebAuthnClientError>(() => Session(SignBy((CredentialA, 0xAA)), emptyList ? [] : null));

        Assert.Equal(WebAuthnClientErrorCode.NotSupported, error.Code);
    }

    /// <summary>Draft v4 §10.2.1: "If signInputs is undefined, return ... SyntaxError" — an allowed credential without an entry.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void Authentication_AllowedCredentialWithoutEntry_IsInvalidRequest()
    {
        var unlisted = new PublicKeyCredentialDescriptor(new byte[] { 0x09 });

        var error = Assert.Throws<WebAuthnClientError>(() => Session(SignBy((CredentialA, 0xAA), (unlisted, 0xCC)), [CredentialA, CredentialB]));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, error.Code);
    }
}