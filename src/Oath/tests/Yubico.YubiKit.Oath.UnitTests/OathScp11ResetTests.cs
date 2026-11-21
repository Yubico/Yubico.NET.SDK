// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.SecurityDomain.UnitTests;

namespace Yubico.YubiKit.Oath.UnitTests;

public sealed class OathScp11ResetTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetAsync_Scp11a_ReusesBorrowedInputsAndEndsSessionOnBadReceipt(bool rejectRehandshake)
    {
        using var card = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using var host = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using var hostPrivateKey = ECPrivateKey.CreateFromEcdh(host);
        ECParameters hostPublicParameters = host.ExportParameters(false);
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var now = DateTimeOffset.UtcNow;
        var subject = new X500DistinguishedName("CN=OATH test OCE");
        var leaf = new CertificateRequest(subject, new System.Security.Cryptography.X509Certificates.PublicKey(host),
            HashAlgorithmName.SHA256);
        using var certificate = leaf.Create(new X500DistinguishedName("CN=OATH test issuer"),
            X509SignatureGenerator.CreateForECDsa(signer), now.AddHours(-1), now.AddHours(1), [1]);
        byte[] expectedCertificate = certificate.RawData;
        using var parameters = new Scp11KeyParameters(new KeyReference(ScpKid.SCP11a, 0x42),
            ECPublicKey.CreateFromParameters(card.ExportParameters(false)), hostPrivateKey,
            new KeyReference(0x10, 0x43), [certificate]);
        using var hostPublic = host.PublicKey;
        await using var connection = new Scp11aConnection(card, hostPublic, expectedCertificate, rejectRehandshake);
        var token = TestContext.Current.CancellationToken;
        var session = await OathSession.CreateAsync(connection,
            new SessionCreationOptions { ScpKeyParameters = parameters }, token);
        await using (session)
        {
            Assert.True(session.IsAuthenticated);
            var oldSalt = session.Salt;
            if (rejectRehandshake)
            {
                var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ResetAsync(token));
                Assert.Contains("reset succeeded", failure.Message, StringComparison.OrdinalIgnoreCase);
                Assert.IsType<BadResponseException>(failure.InnerException);
                Assert.False(session.IsInitialized);
                Assert.False(session.IsAuthenticated);
                await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ListCredentialsAsync(token));
            }
            else
            {
                await session.ResetAsync(token);
                Assert.True(session.IsInitialized);
                Assert.True(session.IsAuthenticated);
                Assert.Equal(new byte[] { 2 }, session.Salt.ToArray());
                Assert.Empty(await session.ListCredentialsAsync(token));
            }
            Assert.All(oldSalt.ToArray(), value => Assert.Equal(0, value));
        }

        byte[] expected = rejectRehandshake
            ? [0xA4, 0x2A, 0x82, 0x04, 0xA4, 0x2A, 0x82]
            : [0xA4, 0x2A, 0x82, 0x04, 0xA4, 0x2A, 0x82, 0xA1];
        Assert.Equal(expected, connection.Instructions);
        Assert.False(connection.Disposed); // The session does not own the connection or the parameters.
        Assert.Equal(expectedCertificate, certificate.RawData);
        using var stillUsable = hostPrivateKey.ToECDiffieHellman();
        Assert.Equal(hostPublicParameters.Q.X, stillUsable.ExportParameters(false).Q.X);
    }

    // Narrow card-side script: receipt and protected empty responses only. This is not a trust or device oracle.
    private sealed class Scp11aConnection(
        ECDiffieHellman staticCard, ECDiffieHellmanPublicKey staticHost, byte[] certificate, bool reject)
        : ISmartCardConnection
    {
        private readonly byte[] _derived = new byte[80];
        private readonly byte[] _chain = new byte[16];
        private byte _selection;
        public List<byte> Instructions { get; } = [];
        public bool Disposed { get; private set; }
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte ins = command.Span[1];
            Instructions.Add(ins);
            byte[] response = ins switch
            {
                0xA4 => Select(command.Span),
                0x2A => ReceiveCertificate(command.Span),
                0x82 => Authenticate(command.Span),
                0x04 or 0xA1 => ProtectedResponse(command.Span),
                _ => throw new InvalidOperationException($"Unexpected instruction 0x{ins:X2}.")
            };
            return Task.FromResult<ReadOnlyMemory<byte>>(response);
        }

        private byte[] Select(ReadOnlySpan<byte> command)
        {
            Assert.Equal(0, command[0]);
            return [0x79, 3, 5, 7, 2, 0x71, 1, ++_selection, 0x90, 0];
        }

        private byte[] ReceiveCertificate(ReadOnlySpan<byte> command)
        {
            Assert.True(command[..4].SequenceEqual(new byte[] { 0x80, 0x2A, 0x43, 0x10 }));
            Assert.True(CommandData(command).SequenceEqual(certificate));
            return [0x90, 0];
        }

        private byte[] Authenticate(ReadOnlySpan<byte> command)
        {
            Assert.True(command[..4].SequenceEqual(new byte[] { 0x80, 0x82, 0x42, ScpKid.SCP11a }));
            ReadOnlySpan<byte> data = CommandData(command);
            using var tlvs = TlvHelper.DecodeList(data);
            ReadOnlySpan<byte> point = tlvs.Single(tlv => tlv.Tag == 0x5F49).Value.Span;
            using var ephemeralHost = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = point[1..33].ToArray(), Y = point[33..65].ToArray() }
            });
            using var hostPublic = ephemeralHost.PublicKey;
            using var ephemeralCard = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var cardPoint = ephemeralCard.ExportParameters(false).Q;
            using var cardPointTlv = new Tlv(0x5F49, [0x04, .. cardPoint.X.AsSpan(), .. cardPoint.Y.AsSpan()]);
            DeriveCardKeys(ephemeralCard, hostPublic);
            byte[] receiptKey = _derived[..16];
            byte[] receipt;
            try { receipt = Scp03HandshakeFakeConnection.AesCmac(receiptKey, [.. data, .. cardPointTlv.AsSpan()]); }
            finally { CryptographicOperations.ZeroMemory(receiptKey); }
            try
            {
                receipt.CopyTo(_chain, 0);
                if (reject && _selection == 2)
                    receipt[0] ^= 1;
                using var receiptTlv = new Tlv(0x86, receipt);
                return [.. cardPointTlv.AsSpan(), .. receiptTlv.AsSpan(), 0x90, 0];
            }
            finally { CryptographicOperations.ZeroMemory(receipt); }
        }

        private void DeriveCardKeys(ECDiffieHellman ephemeralCard, ECDiffieHellmanPublicKey ephemeralHost)
        {
            byte[] ephemeralSecret = ephemeralCard.DeriveRawSecretAgreement(ephemeralHost);
            byte[]? staticSecret = null;
            Span<byte> shared = stackalloc byte[64];
            try
            {
                staticSecret = staticCard.DeriveRawSecretAgreement(staticHost);
                ephemeralSecret.CopyTo(shared);
                staticSecret.CopyTo(shared[32..]);
                byte[] derived = X963Kdf.DeriveKeyMaterial(shared, [0x3C, 0x88, 16], _derived.Length);
                try { derived.CopyTo(_derived, 0); }
                finally { CryptographicOperations.ZeroMemory(derived); }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ephemeralSecret);
                CryptographicOperations.ZeroMemory(staticSecret);
                CryptographicOperations.ZeroMemory(shared);
            }
        }

        private byte[] ProtectedResponse(ReadOnlySpan<byte> command)
        {
            Assert.Equal(0x04, command[0]);
            byte[] commandKey = _derived[32..48];
            byte[] responseKey = _derived[48..64];
            byte[]? nextChain = null;
            byte[]? responseMac = null;
            try
            {
                nextChain = Scp03HandshakeFakeConnection.AesCmac(commandKey, [.. _chain, .. command[..^10]]);
                Assert.True(CryptographicOperations.FixedTimeEquals(nextChain.AsSpan(0, 8), command[^10..^2]));
                nextChain.CopyTo(_chain, 0);
                responseMac = Scp03HandshakeFakeConnection.AesCmac(responseKey, [.. _chain, 0x90, 0]);
                return [.. responseMac.AsSpan(0, 8), 0x90, 0];
            }
            finally
            {
                CryptographicOperations.ZeroMemory(commandKey);
                CryptographicOperations.ZeroMemory(responseKey);
                CryptographicOperations.ZeroMemory(nextChain);
                CryptographicOperations.ZeroMemory(responseMac);
            }
        }

        private static ReadOnlySpan<byte> CommandData(ReadOnlySpan<byte> command) =>
            command.Slice(7, BinaryPrimitives.ReadUInt16BigEndian(command[5..7]));

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => new FakeTransaction();
        public bool SupportsExtendedApdu() => true;
        public void Dispose()
        {
            Disposed = true;
            CryptographicOperations.ZeroMemory(_derived);
            CryptographicOperations.ZeroMemory(_chain);
        }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }

        private sealed class FakeTransaction : IDisposable
        {
            public void Dispose() { }
        }
    }
}