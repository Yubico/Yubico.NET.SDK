// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

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
    // Synthetic non-default key bytes; every participant owns a separate clone.
    private static ReadOnlySpan<byte> Enc => "test-enc-key-001"u8;
    private static ReadOnlySpan<byte> Mac => "test-mac-key-002"u8;
    private static ReadOnlySpan<byte> Dek => "test-dek-key-003"u8;

    private static StaticKeys CreateKeys() => new(Enc, Mac, Dek);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetAsync_ReauthenticatesWithOriginalParametersOrEndsSession(bool rejectRefresh)
    {
        var token = TestContext.Current.CancellationToken;
        using var parameters = new Scp03KeyParameters(new KeyReference(0x01, 0x22), CreateKeys());
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

        await session.DisposeAsync();
        Assert.Equal(Enc.ToArray(), parameters.Keys.Enc.ToArray());
        Assert.Equal(Mac.ToArray(), parameters.Keys.Mac.ToArray());
        Assert.Equal(Dek.ToArray(), parameters.Keys.Dek.ToArray());
        using var nextConnection = new ResetConnection(rejectRefresh: false);
        await using var next = await HsmAuthSession.CreateAsync(nextConnection,
            new SessionCreationOptions { ScpKeyParameters = parameters }, token);
        Assert.True(next.IsAuthenticated);
        Assert.Empty(await next.ListCredentialsAsync(token));
    }

    // Reuse the existing fixture's CMAC/KDF, not its default key set. This checks refresh and
    // key ownership, not independent SCP conformance.
    private sealed class ResetConnection(bool rejectRefresh) : ISmartCardConnection
    {
        private readonly Peer _initial = new("initial!"u8.ToArray());
        private readonly Peer _refreshed = new("refresh!"u8.ToArray());
        private int _selectCount;
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;
        public List<byte> Instructions { get; } = [];
        public List<byte> KeyVersions { get; } = [];

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte ins = command.Span[1];
            Instructions.Add(ins);
            if (ins == 0xA4)
                _selectCount++;
            bool refreshing = _selectCount == 2;
            var handshake = refreshing ? _refreshed : _initial;
            if (ins == 0x50)
            {
                KeyVersions.Add(command.Span[2]);
                if (refreshing && rejectRefresh)
                    return Task.FromResult<ReadOnlyMemory<byte>>(new byte[] { 0x6A, 0x88 });
            }

            return Task.FromResult<ReadOnlyMemory<byte>>(handshake.Respond(command.Span));
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The applet reset does not open a PC/SC transaction.");
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

    private sealed class Peer(byte[] cardChallenge) : IDisposable
    {
        private readonly StaticKeys _keys = CreateKeys();
        private byte[] _senc = [];
        private byte[] _smac = [];
        private byte[] _srmac = [];
        private byte[] _chain = new byte[16];

        public byte[] Respond(ReadOnlySpan<byte> command) => command[1] switch
        {
            0xA4 => [0x90, 0x00],
            0x50 => Initialize(command.Slice(5, 8)),
            0x82 => Authenticate(command),
            0x06 or 0x05 => ProtectedEmptyResponse(command),
            _ => throw new InvalidOperationException($"Unexpected instruction 0x{command[1]:X2}.")
        };

        private byte[] Initialize(ReadOnlySpan<byte> hostChallenge)
        {
            byte[] context = [.. hostChallenge, .. cardChallenge];
            _senc = Scp03HandshakeFakeConnection.DeriveKey(_keys.Enc.ToArray(), 0x04, context, 128);
            _smac = Scp03HandshakeFakeConnection.DeriveKey(_keys.Mac.ToArray(), 0x06, context, 128);
            _srmac = Scp03HandshakeFakeConnection.DeriveKey(_keys.Mac.ToArray(), 0x07, context, 128);
            byte[] cryptogram = Scp03HandshakeFakeConnection.DeriveKey(_smac, 0x00, context, 64);
            return [.. new byte[10], 0x22, 0x02, 0x00, .. cardChallenge, .. cryptogram, 0x90, 0x00];
        }

        private byte[] Authenticate(ReadOnlySpan<byte> command)
        {
            VerifyMac(command);
            return [0x90, 0x00];
        }

        private byte[] ProtectedEmptyResponse(ReadOnlySpan<byte> command)
        {
            Assert.Equal(0x04, command[0]);
            Assert.Equal(24, command[4]);
            VerifyMac(command);

            // Both are the first encrypted command on their respective channels.
            using var aes = Aes.Create();
            aes.Key = _senc;
            Span<byte> ivInput = stackalloc byte[16];
            ivInput.Clear();
            ivInput[^1] = 1;
            byte[] iv = aes.EncryptEcb(ivInput, PaddingMode.None);
            byte[] expected = [0x80, .. new byte[15]];
            Assert.True(CryptographicOperations.FixedTimeEquals(expected,
                aes.DecryptCbc(command.Slice(5, 16), iv, PaddingMode.None)));

            byte[] responseMac = Scp03HandshakeFakeConnection.AesCmac(_srmac, [.. _chain, 0x90, 0x00]);
            return [.. responseMac.AsSpan(0, 8), 0x90, 0x00];
        }

        private void VerifyMac(ReadOnlySpan<byte> command)
        {
            _chain = Scp03HandshakeFakeConnection.AesCmac(_smac, [.. _chain, .. command[..^9]]);
            Assert.True(CryptographicOperations.FixedTimeEquals(_chain.AsSpan(0, 8), command[^9..^1]));
        }

        public void Dispose() => _keys.Dispose();
    }
}