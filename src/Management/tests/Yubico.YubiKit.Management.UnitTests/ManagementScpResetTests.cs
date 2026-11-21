// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Reflection;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.SecurityDomain.UnitTests;

namespace Yubico.YubiKit.Management.UnitTests;

public sealed class ManagementScpResetTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetDeviceAsync_RefreshesSecureChannelOrEndsSession(bool rejectRehandshake)
    {
        var ct = TestContext.Current.CancellationToken;
        using var keys = Scp03KeyParameters.Default;
        await using var connection = new ResetConnection(rejectRehandshake);
        await using var session = await ManagementSession.CreateAsync(connection,
            new SessionCreationOptions { ScpKeyParameters = keys }, ct);
        Assert.True(session.IsAuthenticated);

        if (rejectRehandshake)
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ResetDeviceAsync(ct));
            Assert.Equal(unchecked((short)0x6A88), Assert.IsType<ApduException>(failure.InnerException).SW);
            Assert.False(session.IsInitialized);
            Assert.False(session.IsAuthenticated);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => session.GetDeviceInfoAsync(ct));
        }
        else
        {
            await session.ResetDeviceAsync(ct);
            Assert.True(session.IsInitialized);
            Assert.True(session.IsAuthenticated);
        }

        byte[] expected = rejectRehandshake
            ? [0xA4, 0x1D, 0x50, 0x82, 0x1F, 0xA4, 0x1D, 0x50]
            : [0xA4, 0x1D, 0x50, 0x82, 0x1F, 0xA4, 0x1D, 0x50, 0x82];
        Assert.Equal(expected, connection.Instructions);
        Assert.Equal(new byte[] { 0x04, 0x1F, 0, 0 }, connection.ResetHeader);
    }

    // Reuse the SCP03 handshake fake; synthesize the empty RESET response with an R-MAC.
    // This verifies the refresh transcript and error boundary, not device interoperability.
    private sealed class ResetConnection(bool rejectRehandshake) : ISmartCardConnection
    {
        private readonly Scp03HandshakeFakeConnection _initial = new();
        private readonly Scp03HandshakeFakeConnection _refresh = new();
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;
        public List<byte> Instructions { get; } = [];
        public byte[] ResetHeader { get; private set; } = [];

        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ins = command.Span[1];
            Instructions.Add(ins);
            bool refreshing = Instructions.Count > 5;
            var handshake = refreshing ? _refresh : _initial;
            if (ins is 0xA4 or 0x1D)
                Assert.Equal(0, command.Span[0]);
            if (ins == 0x1D)
                return new byte[] { 5, 0x05, 3, 5, 7, 0, 0x90, 0 };
            if (ins == 0x1F)
            {
                ResetHeader = command[..4].ToArray();
                return ResetResponse(handshake, command.Span);
            }
            if (refreshing && rejectRehandshake && ins == 0x50)
                return new byte[] { 0x6A, 0x88 };
            return await handshake.TransmitAndReceiveAsync(command, cancellationToken);
        }

        private static byte[] ResetResponse(Scp03HandshakeFakeConnection handshake, ReadOnlySpan<byte> command)
        {
            byte[] nextChain = Scp03HandshakeFakeConnection.AesCmac(Bytes(handshake, "_smac"),
                [.. Bytes(handshake, "_macChain"), .. command[..^9]]);
            byte[] rmac = Scp03HandshakeFakeConnection.AesCmac(Bytes(handshake, "_srmac"), [.. nextChain, 0x90, 0]);
            try { return [.. rmac.AsSpan(0, 8), 0x90, 0]; }
            finally
            {
                CryptographicOperations.ZeroMemory(nextChain);
                CryptographicOperations.ZeroMemory(rmac);
            }
        }

        private static byte[] Bytes(Scp03HandshakeFakeConnection handshake, string name) =>
            Assert.IsType<byte[]>(handshake.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(handshake));

        public IDisposable BeginTransaction(CancellationToken cancellationToken) => _initial.BeginTransaction(cancellationToken);
        public bool SupportsExtendedApdu() => false;
        public void Dispose()
        {
            ClearHandshake(_initial);
            ClearHandshake(_refresh);
        }

        private static void ClearHandshake(Scp03HandshakeFakeConnection handshake)
        {
            foreach (var command in handshake.TransmittedCommands)
                CryptographicOperations.ZeroMemory(command);
            foreach (string field in new[] { "_senc", "_smac", "_srmac", "_macChain" })
            {
                if (handshake.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(handshake) is byte[] key)
                    CryptographicOperations.ZeroMemory(key);
            }
            handshake.Dispose();
        }

        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}