// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Registration;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

/// <summary>
///     Runs a real <see cref="FidoSession" /> under the WebAuthn client, so the intent has to cross every layer between
///     the application's call and the user-presence prompt.
/// </summary>
public sealed class UserPresenceIntentTests
{
    [Fact]
    public async Task MakeCredentialAsync_InsideIntentScope_PromptReceivesIntentAndOperation()
    {
        var prompt = new RecordingPrompt();
        await using var client = await CreateClientAsync(prompt);

        using (UserPresenceIntent.BeginScope("register your work passkey"))
        {
            _ = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
                client.MakeCredentialAsync(CreateRegistration(), pinBytes: null, TestContext.Current.CancellationToken));
        }

        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Equal("register your work passkey", requested.Intent);
        Assert.Equal(UserPresenceOperations.Fido2.MakeCredential, requested.Operation);
        Assert.Equal("example.com", requested.Scope);
    }

    [Fact]
    public async Task MakeCredentialAsync_WithoutScope_PromptReceivesNoIntent()
    {
        var prompt = new RecordingPrompt();
        await using var client = await CreateClientAsync(prompt);

        _ = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            client.MakeCredentialAsync(CreateRegistration(), pinBytes: null, TestContext.Current.CancellationToken));

        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Null(requested.Intent);
        Assert.Equal(UserPresenceOperations.Fido2.MakeCredential, requested.Operation);
    }

    private static async Task<WebAuthnClient> CreateClientAsync(IUserPresencePrompt prompt)
    {
        FidoSession session = await FidoSession.CreateAsync(
            new CtapSmartCardConnection(),
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        return new WebAuthnClient(
            session,
            origin,
            new WebAuthnClientOptions { PublicSuffixChecker = domain => domain == "com" });
    }

    private static RegistrationOptions CreateRegistration() => new()
    {
        Challenge = RandomNumberGenerator.GetBytes(32),
        Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
        User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "alice@example.com", "Alice"),
        PubKeyCredParams = [CoseAlgorithm.Es256]
    };

    /// <summary>Answers SELECT and CTAP GetInfo, and times out every other CTAP command.</summary>
    private sealed class CtapSmartCardConnection : ISmartCardConnection
    {
        private const byte CtapGetInfo = 0x04;

        public Transport Transport => Transport.Usb;

        public ConnectionType Type => ConnectionType.SmartCard;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            ReadOnlySpan<byte> apdu = command.Span;
            byte[] response = apdu[1] switch
            {
                0xA4 => [0x90, 0x00],
                0x10 when apdu[5] == CtapGetInfo => [0x00, .. GetInfo(), 0x90, 0x00],
                _ => [(byte)CtapStatus.UserActionTimeout, 0x90, 0x00]
            };

            return Task.FromResult<ReadOnlyMemory<byte>>(response);
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => new NoOp();

        public bool SupportsExtendedApdu() => false;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private static byte[] GetInfo()
        {
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteStartMap(1);
            writer.WriteInt32(0x01);
            writer.WriteStartArray(1);
            writer.WriteTextString("FIDO_2_0");
            writer.WriteEndArray();
            writer.WriteEndMap();
            return writer.Encode();
        }

        private sealed class NoOp : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private sealed class RecordingPrompt : IUserPresencePrompt
    {
        public List<UserPresenceContext> Requested { get; } = [];

        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
        {
            Requested.Add(context);
            return default;
        }
    }
}