// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Reflection;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.SecurityDomain.UnitTests;

namespace Yubico.YubiKit.Oath.UnitTests;

public sealed class OathScpResetTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetAsync_Scp03_ClearsOldKeysAndRehandshakesOrEndsSession(bool rejectRehandshake)
    {
        using var parameters = new Scp03KeyParameters(new KeyReference(1, 0x42),
            new StaticKeys("fixture-enc-key!"u8, "fixture-mac-key!"u8, "fixture-dek-key!"u8));
        await using var connection = new OathScpConnection(parameters, rejectRehandshake);
        await using var session = await OathSession.CreateAsync(connection,
            new SessionCreationOptions { ScpKeyParameters = parameters }, TestContext.Current.CancellationToken);
        Assert.True(session.IsAuthenticated);

        // Observe the actual buffers owned by Core, not a test double's disposal flag.
        var protocolProperty = typeof(ApplicationSession).GetProperty("Protocol", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Session protocol property not found.");
        var protocol = protocolProperty.GetValue(session) ?? throw new InvalidOperationException("Missing protocol.");
        var receiver = Field(protocol, "_scpProcessor");
        var processor = receiver.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(field => field.GetValue(receiver)).Single(value => value?.GetType().Name == "ScpProcessor")
            ?? throw new InvalidOperationException("Missing SCP processor.");
        var stateProperty = processor.GetType().GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SCP state property not found.");
        var state = stateProperty.GetValue(processor) ?? throw new InvalidOperationException("Missing SCP state.");
        var keys = state.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(field => field.GetValue(state)).Single(value => value?.GetType().Name == "SessionKeys")
            ?? throw new InvalidOperationException("Missing session keys.");
        byte[][] derived = [Assert.IsType<byte[]>(Field(keys, "_senc")), Assert.IsType<byte[]>(Field(keys, "_smac")), Assert.IsType<byte[]>(Field(keys, "_srmac")), Assert.IsType<byte[]>(Field(keys, "_dek"))];
        Assert.All(derived, key => Assert.Contains(key, value => value != 0));

        var oldSalt = session.Salt;
        var token = TestContext.Current.CancellationToken;
        if (rejectRehandshake)
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ResetAsync(token));
            Assert.Contains("reset succeeded", failure.Message, StringComparison.OrdinalIgnoreCase);
            Assert.IsType<Core.Protocols.SmartCard.Apdu.ApduException>(failure.InnerException);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ListCredentialsAsync(token));
        }
        else
        {
            await session.ResetAsync(token);
            Assert.Equal(new byte[] { 2 }, session.Salt.ToArray());
            Assert.Empty(await session.ListCredentialsAsync(token));
        }

        Assert.Equal(!rejectRehandshake, session.IsInitialized);
        Assert.Equal(!rejectRehandshake, session.IsAuthenticated);
        Assert.All(oldSalt.ToArray(), value => Assert.Equal(0, value));
        Assert.All(derived, key => Assert.All(key, value => Assert.Equal(0, value)));
        Assert.True(Assert.IsType<bool>(Field(state, "_disposed")));
        byte[] expected = rejectRehandshake
            ? [0xA4, 0x50, 0x82, 0x04, 0xA4, 0x50]
            : [0xA4, 0x50, 0x82, 0x04, 0xA4, 0x50, 0x82, 0xA1];
        Assert.Equal(expected, connection.Commands.Select(command => command[1]));
        Assert.Equal(new byte[] { 0x04, 0x04, 0xDE, 0xAD }, connection.Commands[3][..4]);
        Assert.Equal(0x00, connection.Commands[4][0]); // SELECT uses the new plain protocol.
        if (!rejectRehandshake)
            Assert.Equal(0x04, connection.Commands[^1][0]); // LIST uses the new secure protocol.
        Assert.Equal(0, connection.DisposeCount); // borrowed, still the caller's connection
    }

    [Theory]
    [InlineData(0, "bb1d6929e95937287fa37d129b756746")]
    [InlineData(16, "070a16b46b4d4144f79bdd9dd04a287c")]
    [InlineData(40, "dfa66747de9ae63030ca32611497c827")]
    [InlineData(64, "51f0bebf7e3b9d92fc49741779363cfe")]
    public void ReusedHandshakeCmac_Rfc4493Vectors(int length, string expected)
    {
        byte[] key = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");
        byte[] message = Convert.FromHexString("6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e5130c81c46a35ce411e5fbc1191a0a52eff69f2445df4f9b17ad2b417be66c3710");
        byte[] mac = Scp03HandshakeFakeConnection.AesCmac(key, message[..length]);
        try { Assert.Equal(expected, Convert.ToHexString(mac).ToLowerInvariant()); }
        finally { CryptographicOperations.ZeroMemory(mac); }
    }

    private static object Field(object owner, string name) =>
        owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner)
        ?? throw new InvalidOperationException($"Missing field {name}.");

    // Adapt SELECT metadata and synthesize an R-MAC-bearing empty RESET response for the cleanup oracle.
    // The existing fake handles the handshake, but does not verify outbound encryption or command MACs.
    // Actual key-array clearing/order is tested; whole-SCP conformance and live secure reset are separate.
    private sealed class OathScpConnection(Scp03KeyParameters parameters, bool rejectRehandshake) : ISmartCardConnection
    {
        private readonly Scp03HandshakeFakeConnection _handshake = CreateHandshake(parameters.Keys);
        private readonly Scp03HandshakeFakeConnection _rehandshake = CreateHandshake(parameters.Keys);
        public List<byte[]> Commands { get; } = [];
        public int DisposeCount { get; private set; }
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;
        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.ToArray());
            var handshake = Commands.Count <= 4 ? _handshake : _rehandshake;
            if (command.Span[1] == 0x50)
                Assert.Equal(parameters.KeyReference.Kvn, command.Span[2]);
            if (Commands.Count == 6 && rejectRehandshake)
                return new byte[] { 0x6A, 0x88 };

            if (command.Span[1] is 0x04 or 0xA1)
            {
                var smac = Assert.IsType<byte[]>(Field(handshake, "_smac"));
                var srmac = Assert.IsType<byte[]>(Field(handshake, "_srmac"));
                var chain = Assert.IsType<byte[]>(Field(handshake, "_macChain"));
                byte[] nextChain = Scp03HandshakeFakeConnection.AesCmac(smac, [.. chain, .. command.Span[..^9]]);
                byte[] rmac = Scp03HandshakeFakeConnection.AesCmac(srmac, [.. nextChain, 0x90, 0]);
                try
                {
                    byte[] resetResponse = [.. rmac.AsSpan(0, 8), 0x90, 0];
                    return resetResponse;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(nextChain);
                    CryptographicOperations.ZeroMemory(rmac);
                }
            }
            var response = await handshake.TransmitAndReceiveAsync(command, cancellationToken);
            if (command.Span[1] == 0x50)
            {
                byte[] initializeUpdate = response.ToArray();
                initializeUpdate[10] = parameters.KeyReference.Kvn;
                return initializeUpdate;
            }
            return command.Span[1] == 0xA4
                ? new byte[] { 0x79, 3, 5, 7, 0, 0x71, 1, (byte)(Commands.Count == 1 ? 1 : 2), 0x90, 0 }
                : response;
        }
        private static Scp03HandshakeFakeConnection CreateHandshake(StaticKeys keys)
        {
            // Configure the linked fixture's own key buffers without changing the Security Domain fixture.
            var handshake = new Scp03HandshakeFakeConnection();
            var storage = Field(handshake, "_staticKeys");
            keys.Enc.CopyTo(Assert.IsType<byte[]>(Field(storage, "_enc")));
            keys.Mac.CopyTo(Assert.IsType<byte[]>(Field(storage, "_mac")));
            keys.Dek.CopyTo(Assert.IsType<byte[]>(Field(storage, "_dek")));
            return handshake;
        }
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => _handshake.BeginTransaction(cancellationToken);
        public bool SupportsExtendedApdu() => false;
        public void Dispose()
        {
            DisposeCount++;
            foreach (byte[] command in Commands)
                CryptographicOperations.ZeroMemory(command);
            DisposeHandshake(_handshake);
            DisposeHandshake(_rehandshake);
        }
        private static void DisposeHandshake(Scp03HandshakeFakeConnection handshake)
        {
            foreach (byte[] command in handshake.TransmittedCommands)
                CryptographicOperations.ZeroMemory(command);
            foreach (string field in new[] { "_senc", "_smac", "_srmac", "_macChain" })
            {
                if (handshake.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(handshake) is byte[] bytes)
                    CryptographicOperations.ZeroMemory(bytes);
            }
            handshake.Dispose();
        }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}