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
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

[Trait("Audit", "YESDK-1608")]
public class RegistrationTransportTests
{
    [Fact]
    public async Task MakeCredential_AdvertisedKnownAndFutureTransports_UsesCachedInfoWithoutExtraQuery()
    {
        var info = DecodeInfo(["usb", "nfc", "ble", "smart-card", "hybrid", "internal", "future-link"]);
        var session = CreateSession(info);
        await using var client = CreateClient(session);

        var first = await client.MakeCredentialAsync(CreateOptions(), cancellationToken: TestContext.Current.CancellationToken);
        var second = await client.MakeCredentialAsync(CreateOptions(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(first.Transports);
        Assert.Equal(["ble", "future-link", "hybrid", "internal", "nfc", "smart-card", "usb"],
            first.Transports.Select(transport => transport.Value));
        Assert.Contains(WebAuthnTransport.Usb, first.Transports);
        Assert.Contains(WebAuthnTransport.Nfc, first.Transports);
        Assert.Contains(WebAuthnTransport.Ble, first.Transports);
        Assert.Contains(WebAuthnTransport.SmartCard, first.Transports);
        Assert.Contains(WebAuthnTransport.Hybrid, first.Transports);
        Assert.Contains(WebAuthnTransport.Internal, first.Transports);
        Assert.Equal(first.Transports, second.Transports);
        await session.Received(1).GetInfoAsync(Arg.Any<CancellationToken>());
        await session.Received(2).MakeCredentialAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<PublicKeyCredentialRpEntity>(),
            Arg.Any<PublicKeyCredentialUserEntity>(), Arg.Any<IReadOnlyList<PublicKeyCredentialParameters>>(),
            Arg.Any<MakeCredentialOptions>(), Arg.Any<CancellationToken>());
        await session.DidNotReceive().SendCborRequestAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MakeCredential_MissingOrEmptyTransportMetadata_ReturnsEmptyList(bool includeEmptyMetadata)
    {
        var session = CreateSession(DecodeInfo(includeEmptyMetadata ? [] : null));
        await using var client = CreateClient(session);

        var response = await client.MakeCredentialAsync(CreateOptions(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(response.Transports);
        Assert.Empty(response.Transports);
        await session.Received(1).GetInfoAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MakeCredential_TransportSnapshot_IsOrdinalSortedUniqueAndIndependentOfSource()
    {
        List<string> advertised = ["usb", "z-link", "nfc", "USB", "ä-link", "usb", "a-link", "nfc", "Z-link"];
        var session = CreateSession(new AuthenticatorInfo { Versions = ["FIDO_2_1"], Transports = advertised });
        await using var client = CreateClient(session);

        var response = await client.MakeCredentialAsync(CreateOptions(), cancellationToken: TestContext.Current.CancellationToken);
        advertised.Clear();
        advertised.Add("later-link");

        Assert.NotNull(response.Transports);
        Assert.Equal(["USB", "Z-link", "a-link", "nfc", "usb", "z-link", "ä-link"],
            response.Transports.Select(transport => transport.Value));
        var collection = Assert.IsAssignableFrom<IList<WebAuthnTransport>>(response.Transports);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection[0] = WebAuthnTransport.Internal);
    }

    [Fact]
    public async Task RegistrationResponse_WithoutTransportInitializer_DefaultsToEmptyList()
    {
        var session = CreateSession(DecodeInfo(null));
        await using var client = CreateClient(session);
        var response = await client.MakeCredentialAsync(CreateOptions(), cancellationToken: TestContext.Current.CancellationToken);

        var callerResponse = new RegistrationResponse
        {
            CredentialId = response.CredentialId,
            AttestationObject = response.AttestationObject,
            RawAttestationObject = response.RawAttestationObject,
            AuthenticatorData = response.AuthenticatorData,
            RawAuthenticatorData = response.RawAuthenticatorData,
            AttestationStatement = response.AttestationStatement,
            PublicKey = response.PublicKey,
            Aaguid = response.Aaguid,
            SignCount = response.SignCount,
            ClientData = response.ClientData
        };

        Assert.NotNull(callerResponse.Transports);
        Assert.Empty(callerResponse.Transports);
    }

    [Theory]
    [InlineData("usb")]
    [InlineData("future-link")]
    [InlineData("USB")]
    public void FromValue_PreservesExactTransportNameAndValueEquality(string value)
    {
        var transport = WebAuthnTransport.FromValue(value);
        var copy = WebAuthnTransport.FromValue(value);

        Assert.Equal(value, transport.Value);
        Assert.Equal(value, transport.ToString());
        Assert.Equal(transport, copy);
        Assert.Equal(value == "usb", transport == WebAuthnTransport.Usb);
    }

    private static ICtap2Session CreateSession(AuthenticatorInfo info)
    {
        var session = Substitute.For<ICtap2Session>();
        session.GetInfoAsync(Arg.Any<CancellationToken>()).Returns(info);
        session.MakeCredentialAsync(
                Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<PublicKeyCredentialRpEntity>(),
                Arg.Any<PublicKeyCredentialUserEntity>(), Arg.Any<IReadOnlyList<PublicKeyCredentialParameters>>(),
                Arg.Any<MakeCredentialOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => MockCtap2Responses.CreateMockMakeCredentialResponse());
        return session;
    }

    private static WebAuthnClient CreateClient(ICtap2Session session)
    {
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        Assert.NotNull(origin);
        return new WebAuthnClient(session, origin,
            new WebAuthnClientOptions { PublicSuffixChecker = domain => domain == "com" });
    }

    private static PublicKeyCredentialCreationOptions CreateOptions() => new()
    {
        Challenge = new byte[32],
        Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
        User = new PublicKeyCredentialUserEntity(new byte[16], "user@example.com", "User"),
        PubKeyCredParams = [CoseAlgorithm.Es256]
    };

    private static AuthenticatorInfo DecodeInfo(IReadOnlyList<string>? transports)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(transports is null ? 2 : 3);
        writer.WriteInt32(0x01);
        writer.WriteStartArray(1);
        writer.WriteTextString("FIDO_2_1");
        writer.WriteEndArray();
        writer.WriteInt32(0x03);
        writer.WriteByteString(new byte[16]);
        if (transports is not null)
        {
            writer.WriteInt32(0x09);
            writer.WriteStartArray(transports.Count);
            foreach (string transport in transports)
            {
                writer.WriteTextString(transport);
            }
            writer.WriteEndArray();
        }
        writer.WriteEndMap();
        return AuthenticatorInfo.Decode(writer.Encode());
    }
}