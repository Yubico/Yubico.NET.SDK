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
using System.Buffers.Binary;
using System.Formats.Cbor;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

public class TokenlessUvMessageSizeTests
{
    private const int MessageLimit = 1024;
    private const string RpId = "example.com";
    private static readonly byte[] Miss = [(byte)CtapStatus.NoCredentials, 0x90, 0x00];

    [Theory]
    [InlineData(500, false)]
    [InlineData(456, true)]
    public async Task TokenlessUvSize_AuthenticationRefinesAtRealSessionGuard(int idLength, bool extensionHeavier)
    {
        var (a, b) = Descriptors(idLength);
        var wire = new List<byte[]>();
        // With getCredBlob the silent pair fits but the real pair does not. Otherwise both pairs are oversized.
        byte[][] responses = extensionHeavier ? [Miss, Miss, AssertionSuccess()] : [Miss, Miss, Miss, AssertionSuccess()];
        using var connection = FakeConnection(wire, responses);
        var ct = TestContext.Current.CancellationToken;
        await using var session = await Ctap2Session.CreateAsync(connection, cancellationToken: ct);
        await using var client = CreateClient(session);

        var result = await client.GetAssertionAsync(new PublicKeyCredentialRequestOptions
        {
            Challenge = new byte[32],
            RpId = RpId,
            AllowCredentials = [a, b],
            UserVerification = UserVerificationPreference.Required,
            Extensions = extensionHeavier ? new AuthenticationExtensionInputs(GetCredBlob: true) : null
        }, cancellationToken: ct);

        var assertion = await Assert.Single(result).SelectAsync(ct);
        Assert.Equal(b.Id.ToArray(), assertion.CredentialId.ToArray());
        var requests = wire.Select(ReadAssertion).ToList();
        if (extensionHeavier)
            AssertChunk(requests[0], new byte[32], a, b);
        else
        {
            AssertChunk(requests[0], new byte[32], a);
            AssertChunk(requests[1], new byte[32], b);
        }
        AssertChunk(requests[^2], assertion.ClientData.Hash, a);
        AssertChunk(requests[^1], assertion.ClientData.Hash, b);
        Assert.Equal(extensionHeavier, requests[^1].HasExtensions);
        Assert.Equal(extensionHeavier ? 3 : 4, wire.Count);
    }

    [Fact]
    public async Task TokenlessUvSize_RegistrationRefinesAtRealSessionGuardBeforeOneCreation()
    {
        var (a, b) = Descriptors(500);
        var wire = new List<byte[]>();
        using var connection = FakeConnection(wire, Miss, AssertionSuccess(), [(byte)CtapStatus.CredentialExcluded, 0x90, 0x00]);
        var ct = TestContext.Current.CancellationToken;
        await using var session = await Ctap2Session.CreateAsync(connection, cancellationToken: ct);
        await using var client = CreateClient(session);

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(new PublicKeyCredentialCreationOptions
        {
            Challenge = new byte[32],
            Rp = new PublicKeyCredentialRpEntity(RpId, "Example"),
            User = new PublicKeyCredentialUserEntity(new byte[16], "user", "User"),
            PubKeyCredParams = [CoseAlgorithm.Es256],
            ExcludeCredentials = [a, b],
            UserVerification = UserVerificationPreference.Required
        }, cancellationToken: ct));

        Assert.Equal(WebAuthnClientErrorCode.InvalidState, error.Code);
        Assert.Equal(3, wire.Count);
        Assert.Equal(CtapCommand.MakeCredential, wire[2][0]);
        var creation = ReadCredentialCommand(wire[2], hashKey: 1, listKey: 5, extensionKey: 6);
        Assert.Contains(creation.Hash, value => value != 0);
        AssertChunk(creation, creation.Hash, b);
        AssertChunk(ReadAssertion(wire[0]), creation.Hash, a);
        AssertChunk(ReadAssertion(wire[1]), creation.Hash, b);
    }

    [Fact]
    public async Task TokenlessUvSize_OversizedSingletonFailsWithoutTransportWrite()
    {
        var (a, _) = Descriptors(MessageLimit);
        var wire = new List<byte[]>();
        using var connection = FakeConnection(wire);
        var ct = TestContext.Current.CancellationToken;
        await using var session = await Ctap2Session.CreateAsync(connection, cancellationToken: ct);
        await using var client = CreateClient(session);

        var error = await Assert.ThrowsAnyAsync<ArgumentException>(() => client.GetAssertionAsync(new PublicKeyCredentialRequestOptions
        {
            Challenge = new byte[32],
            RpId = RpId,
            AllowCredentials = [a],
            UserVerification = UserVerificationPreference.Required
        }, cancellationToken: ct));
        Assert.Equal("request", error.ParamName);
        Assert.Empty(wire);
    }

    private static (PublicKeyCredentialDescriptor A, PublicKeyCredentialDescriptor B) Descriptors(int length) =>
        (new(Enumerable.Repeat((byte)1, length).ToArray()), new(Enumerable.Repeat((byte)2, length).ToArray()));

    [Fact]
    public async Task TokenlessUvSize_ResolutionFailureFromAnotherSessionDoesNotReplayAssertion()
    {
        var (a, b) = Descriptors(16);
        var outerWire = new List<byte[]>();
        var innerWire = new List<byte[]>();
        using var outerConnection = FakeConnection(outerWire, Miss, AssertionSuccess());
        using var innerConnection = FakeConnection(innerWire);
        var ct = TestContext.Current.CancellationToken;
        await using var innerSession = await Ctap2Session.CreateAsync(innerConnection, cancellationToken: ct);
        var prompt = Substitute.For<IUserPresencePrompt>();
        ArgumentException? innerFailure = null;
        prompt.OnUserPresenceResolvedAsync(Arg.Any<UserPresenceContext>(), UserPresenceOutcome.Completed, CancellationToken.None)
            .Returns(_ => ResolveAsync());
        await using var outerSession = await Ctap2Session.CreateAsync(outerConnection,
            new SessionCreationOptions { UserPresencePrompt = prompt }, ct);
        await using var client = CreateClient(outerSession);

        var error = await Assert.ThrowsAnyAsync<ArgumentException>(() => client.GetAssertionAsync(new PublicKeyCredentialRequestOptions
        {
            Challenge = new byte[32],
            RpId = RpId,
            AllowCredentials = [a, b],
            UserVerification = UserVerificationPreference.Required
        }, cancellationToken: ct));

        Assert.Same(innerFailure, error);
        Assert.Empty(innerWire);
        Assert.Equal(2, outerWire.Count); // One silent miss, one completed real assertion; no retry or reselection.
        var sent = ReadAssertion(outerWire[1]);
        Assert.Contains(sent.Hash, value => value != 0);
        AssertChunk(sent, sent.Hash, a, b);

        async ValueTask ResolveAsync()
        {
            try
            {
                await innerSession.GetAssertionAsync(new string('a', MessageLimit), new byte[32], cancellationToken: ct);
            }
            catch (ArgumentException ex)
            {
                innerFailure = ex;
                throw;
            }
        }
    }

    private static WebAuthnClient CreateClient(Ctap2Session session)
    {
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        return new WebAuthnClient(session, origin, new() { RpIdValidator = (_, rp) => rp == RpId });
    }

    private static ISmartCardConnection FakeConnection(List<byte[]> wire, params byte[][] responses)
    {
        var info = MockCtap2Responses.CreateMockAuthenticatorInfo(uvSupported: true, extensions: ["credBlob"],
            maxCredentialCountInList: 2, maxMsgSize: MessageLimit);
        byte[] getInfo = [0x00, .. info.RawData.Span, 0x90, 0x00];
        var pending = new Queue<byte[]>(responses);
        var connection = Substitute.For<ISmartCardConnection>();
        connection.Transport.Returns(Transport.Nfc);
        connection.SupportsExtendedApdu().Returns(true);
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call => Reply(call.Arg<ReadOnlyMemory<byte>>()));
        return connection;

        ReadOnlyMemory<byte> Reply(ReadOnlyMemory<byte> command)
        {
            if (command.Span[1] == 0xA4)
                return new byte[] { 0x90, 0x00 }; // SELECT

            Assert.Equal(0x10, command.Span[1]);
            int header = command.Span[4] == 0 ? 7 : 5;
            int length = header == 7 ? BinaryPrimitives.ReadUInt16BigEndian(command.Span[5..]) : command.Span[4];
            Assert.InRange(length, 1, MessageLimit);
            var ctap = command.Slice(header, length);
            if (ctap.Span[0] == CtapCommand.GetInfo)
                return getInfo;

            wire.Add(ctap.ToArray());
            Assert.True(pending.TryDequeue(out var reply), "Unexpected CTAP command at the fake transport.");
            return reply;
        }
    }

    private static byte[] AssertionSuccess()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2); // CTAP 2.0 singleton credential omission.
        writer.WriteInt32(2);
        writer.WriteByteString(MockCtap2Responses.BuildAuthData());
        writer.WriteInt32(3);
        writer.WriteByteString([0x42]);
        writer.WriteEndMap();
        return [0x00, .. writer.Encode(), 0x90, 0x00];
    }

    private static (byte[] Hash, List<byte[]> Ids, bool HasExtensions) ReadAssertion(byte[] command)
    {
        Assert.Equal(CtapCommand.GetAssertion, command[0]);
        return ReadCredentialCommand(command, hashKey: 2, listKey: 3, extensionKey: 4);
    }

    private static (byte[] Hash, List<byte[]> Ids, bool HasExtensions) ReadCredentialCommand(
        byte[] command, int hashKey, int listKey, int extensionKey)
    {
        var reader = new CborReader(command.AsMemory(1), CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap();
        byte[]? hash = null;
        var ids = new List<byte[]>();
        bool hasExtensions = false;
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            int key = reader.ReadInt32();
            if (key == hashKey)
                hash = reader.ReadByteString();
            else if (key == listKey)
            {
                reader.ReadStartArray();
                while (reader.PeekState() != CborReaderState.EndArray)
                    ids.Add(ReadDescriptorId(reader));
                reader.ReadEndArray();
            }
            else
            {
                hasExtensions |= key == extensionKey;
                reader.SkipValue();
            }
        }
        return (Assert.IsType<byte[]>(hash), ids, hasExtensions);
    }

    private static byte[] ReadDescriptorId(CborReader reader)
    {
        Assert.Equal(2, reader.ReadStartMap());
        Assert.Equal("id", reader.ReadTextString());
        var id = reader.ReadByteString();
        Assert.Equal("type", reader.ReadTextString());
        Assert.Equal("public-key", reader.ReadTextString());
        reader.ReadEndMap();
        return id;
    }

    private static void AssertChunk((byte[] Hash, List<byte[]> Ids, bool HasExtensions) request,
        ReadOnlyMemory<byte> hash, params PublicKeyCredentialDescriptor[] expected)
    {
        Assert.Equal(hash.ToArray(), request.Hash);
        Assert.Equal(expected.Select(d => d.Id.ToArray()), request.Ids);
    }
}