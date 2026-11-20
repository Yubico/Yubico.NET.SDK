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
    [Fact]
    public async Task ResetAsync_Scp03_DestroysActualDerivedStateWithoutSelectOrRehandshake()
    {
        using var parameters = Scp03KeyParameters.Default;
        await using var connection = new OathScpConnection();
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

        // Creation parameters are not retained: reset must work even after the caller releases them.
        parameters.Dispose();
        await session.ResetAsync(TestContext.Current.CancellationToken);

        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
        Assert.All(derived, key => Assert.All(key, value => Assert.Equal(0, value)));
        Assert.True(Assert.IsType<bool>(Field(state, "_disposed")));
        Assert.Equal(new byte[] { 0xA4, 0x50, 0x82, 0x04 }, connection.Commands.Select(command => command[1]));
        Assert.Equal(new byte[] { 0x04, 0x04, 0xDE, 0xAD }, connection.Commands[^1][..4]);
        Assert.Equal(0, connection.DisposeCount); // borrowed, still the caller's connection
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ListCredentialsAsync(TestContext.Current.CancellationToken));
        Assert.Equal(4, connection.Commands.Count);
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
    private sealed class OathScpConnection : ISmartCardConnection
    {
        private readonly Scp03HandshakeFakeConnection _handshake = new();
        public List<byte[]> Commands { get; } = [];
        public int DisposeCount { get; private set; }
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;
        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.ToArray());
            if (Commands.Count == 4 && command.Span[1] == 0x04)
            {
                var smac = Assert.IsType<byte[]>(Field(_handshake, "_smac"));
                var srmac = Assert.IsType<byte[]>(Field(_handshake, "_srmac"));
                var chain = Assert.IsType<byte[]>(Field(_handshake, "_macChain"));
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
            var response = await _handshake.TransmitAndReceiveAsync(command, cancellationToken);
            return Commands.Count == 1 ? new byte[] { 0x79, 3, 5, 7, 0, 0x71, 1, 1, 0x90, 0 } : response;
        }
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => _handshake.BeginTransaction(cancellationToken);
        public bool SupportsExtendedApdu() => false;
        public void Dispose()
        {
            DisposeCount++;
            foreach (byte[] command in Commands.Concat(_handshake.TransmittedCommands))
                CryptographicOperations.ZeroMemory(command);
            foreach (string field in new[] { "_senc", "_smac", "_srmac", "_macChain" })
            {
                if (_handshake.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_handshake) is byte[] bytes)
                    CryptographicOperations.ZeroMemory(bytes);
            }
            _handshake.Dispose();
        }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}