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

namespace Yubico.YubiKit.YubiHsm.UnitTests;

public sealed class HsmAuthScpResetTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetAsync_ReauthenticatesWithOriginalParametersOrEndsSession(bool rejectRefresh)
    {
        var token = TestContext.Current.CancellationToken;
        using var parameters = new Scp03KeyParameters(new KeyReference(0x01, 0x22), StaticKeys.GetDefaultKeys());
        using var connection = new ResetConnection(rejectRefresh);
        await using var session = await HsmAuthSession.CreateAsync(connection,
            new SessionCreationOptions { ScpKeyParameters = parameters }, token);
        Assert.True(session.IsAuthenticated);

        if (rejectRefresh)
        {
            var error = await Assert.ThrowsAsync<ApduException>(() => session.ResetAsync(token));
            Assert.Equal(unchecked((short)0x6A88), error.SW);
            Assert.False(session.IsAuthenticated);
            Assert.False(session.IsInitialized);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ListCredentialsAsync(token));
        }
        else
        {
            await session.ResetAsync(token);
            Assert.True(session.IsInitialized);
            Assert.True(session.IsAuthenticated);
            Assert.Empty(await session.ListCredentialsAsync(token));
        }

        byte[] expected = rejectRefresh
            ? [0xA4, 0x50, 0x82, 0x06, 0xA4, 0x50]
            : [0xA4, 0x50, 0x82, 0x06, 0xA4, 0x50, 0x82, 0x05];
        Assert.Equal(expected, connection.Instructions);
        Assert.Equal(new byte[] { 0x22, 0x22 }, connection.KeyVersions);
    }

    // Use the existing software handshake fixture for each independent channel. RESET and LIST
    // return empty protected responses; verify their command MACs before synthesizing the R-MAC.
    private sealed class ResetConnection(bool rejectRefresh) : ISmartCardConnection
    {
        private readonly Scp03HandshakeFakeConnection _initial = new();
        private readonly Scp03HandshakeFakeConnection _refreshed = new();
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;
        public List<byte> Instructions { get; } = [];
        public List<byte> KeyVersions { get; } = [];

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte ins = command.Span[1];
            Instructions.Add(ins);
            bool refreshing = Instructions.Count > 4;
            var handshake = refreshing ? _refreshed : _initial;
            if (ins == 0x50)
            {
                KeyVersions.Add(command.Span[2]);
                if (refreshing && rejectRefresh)
                    return Task.FromResult<ReadOnlyMemory<byte>>(new byte[] { 0x6A, 0x88 });
            }

            if (ins is 0x06 or 0x05)
                return Task.FromResult<ReadOnlyMemory<byte>>(ProtectedEmptyResponse(handshake, command.Span));

            return handshake.TransmitAndReceiveAsync(command, cancellationToken);
        }

        private static byte[] ProtectedEmptyResponse(Scp03HandshakeFakeConnection handshake, ReadOnlySpan<byte> command)
        {
            Assert.Equal(0x04, command[0]);
            Assert.Equal(24, command[4]); // One padded ciphertext block and an 8-byte MAC.
            byte[] chain = Scp03HandshakeFakeConnection.AesCmac(Bytes(handshake, "_smac"),
                [.. Bytes(handshake, "_macChain"), .. command[..^9]]);
            Assert.True(CryptographicOperations.FixedTimeEquals(chain.AsSpan(0, 8), command[^9..^1]));
            byte[] responseMac = Scp03HandshakeFakeConnection.AesCmac(Bytes(handshake, "_srmac"), [.. chain, 0x90, 0x00]);
            return [.. responseMac.AsSpan(0, 8), 0x90, 0x00];
        }

        private static byte[] Bytes(Scp03HandshakeFakeConnection handshake, string field) =>
            Assert.IsType<byte[]>(handshake.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(handshake));

        public IDisposable BeginTransaction(CancellationToken cancellationToken) => _initial.BeginTransaction(cancellationToken);
        public bool SupportsExtendedApdu() => false;
        public void Dispose()
        {
            _initial.Dispose();
            _refreshed.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}