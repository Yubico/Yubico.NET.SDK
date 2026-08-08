// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Buffers.Binary;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.SecurityDomain.UnitTests;

/// <summary>
///     A minimal, protocol-conformant fake SCP03 device used only to prove that
///     <see cref="SecurityDomainSession.ResetAsync" />'s post-reset secure-channel reinitialization
///     is wrapped by <see cref="SecureChannelException" /> exactly like
///     <see cref="SecurityDomainSession.CreateAsync" />'s initial handshake.
/// </summary>
/// <remarks>
///     <para>
///         Unlike <see cref="Yubico.YubiKit.Tests.Shared.RecordingSmartCardConnection" />, this fake
///         cannot play back static queued bytes: a genuinely successful SCP03 handshake requires the
///         "device" side to derive session keys and a card cryptogram from the host challenge the
///         client actually sends (which Core generates randomly and does not expose), so this fixture
///         independently implements the public, standardized GlobalPlatform SCP03 key-derivation and
///         secure-messaging scheme (AES-128 CMAC-based KDF, NIST SP 800-108 counter-mode construction)
///         using only BCL primitives (<see cref="Aes" />). It is the "other side" of the protocol, not
///         a copy of Core's internal implementation, and is validated against the official RFC 4493
///         AES-CMAC test vectors in <see cref="Scp03HandshakeFakeConnectionTests" /> to avoid silently
///         trusting unverified hand-rolled crypto.
///     </para>
///     <para>
///         This fixture only implements exactly the exchanges needed for one scenario: a full SCP03
///         handshake (SELECT, INITIALIZE UPDATE, EXTERNAL AUTHENTICATE), one encrypted+MACed GET DATA
///         round trip (key information, used by <c>ResetAsync</c>'s pre-reset key enumeration), the raw
///         (non-SCP) key-blocking APDU used by <c>ResetAsync</c>, a re-SELECT, and a final rejected
///         INITIALIZE UPDATE simulating the just-blocked key failing to reauthenticate during reinit.
///         It is not a general-purpose SCP simulator.
///     </para>
/// </remarks>
internal sealed class Scp03HandshakeFakeConnection : ISmartCardConnection
{
    private const byte InsSelect = 0xA4;
    private const byte InsInitializeUpdate = 0x50;
    private const byte InsExternalAuthenticate = 0x82;
    private const byte InsGetData = 0xCA;

    private const byte DerivationTypeSEnc = 0x04;
    private const byte DerivationTypeSMac = 0x06;
    private const byte DerivationTypeSRMac = 0x07;
    private const byte DerivationTypeCardCryptogram = 0x00;
    private const short DerivationContextLengthBits = 0x40; // 64 bits
    private const short SessionKeyLengthBits = 128;

    private static readonly byte[] CardChallenge = [0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18];

    /// <summary>The single key entry to report from the (only) GET DATA call: matches the SCP03 default KID/KVN.</summary>
    private static readonly byte[] KeyInformationPlaintext = [0xC0, 0x04, 0x01, 0xFF, 0x88, 0x10];

    private readonly StaticKeys _staticKeys = StaticKeys.GetDefaultKeys();

    private int _step;
    private byte[] _macChain = new byte[16];
    private byte[]? _senc;
    private byte[]? _smac;
    private byte[]? _srmac;

    /// <summary>Gets the raw wire bytes of every command transmitted through this connection, in order.</summary>
    public List<byte[]> TransmittedCommands { get; } = [];

    /// <summary>
    ///     Gets or sets the status word returned for the reinit's INITIALIZE UPDATE (step index 5).
    ///     Defaults to 0x6982 (Security status not satisfied), simulating a just-blocked key.
    /// </summary>
    public short ReinitRejectionStatusWord { get; set; } = unchecked((short)0x6982);

    public Transport Transport { get; } = Transport.Usb;
    public ConnectionType Type { get; } = ConnectionType.SmartCard;

    public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var wire = command.ToArray();
        TransmittedCommands.Add(wire);

        var response = HandleCommand(wire);
        return Task.FromResult((ReadOnlyMemory<byte>)response);
    }

    public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => NullDisposable.Instance;

    public bool SupportsExtendedApdu() => false;

    public void Dispose() => _staticKeys.Dispose();

    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }

    private byte[] HandleCommand(byte[] wire)
    {
        var ins = wire[1];

        // Steps proceed strictly in the order this specific scenario drives them:
        // 0: SELECT (initial CreateAsync)
        // 1: INITIALIZE UPDATE (initial handshake)
        // 2: EXTERNAL AUTHENTICATE (initial handshake)
        // 3: GET DATA, encrypted (ResetAsync's GetKeyInfoAsync)
        // 4: raw (non-SCP) key-blocking attempt
        // 5: SELECT (post-reset reinit)
        // 6: INITIALIZE UPDATE (reinit) -- deliberately rejected
        var step = _step++;

        return (step, ins) switch
        {
            (0, InsSelect) => Ok(),
            (1, InsInitializeUpdate) => HandleInitializeUpdate(wire),
            (2, InsExternalAuthenticate) => HandleExternalAuthenticate(wire),
            (3, InsGetData) => HandleGetData(wire),
            (4, InsInitializeUpdate) => Rejected(SWConstants.AuthenticationMethodBlocked),
            (5, InsSelect) => Ok(),
            (6, InsInitializeUpdate) => Rejected(ReinitRejectionStatusWord),
            _ => throw new InvalidOperationException(
                $"Unexpected command at step {step}: INS=0x{ins:X2}, wire={Convert.ToHexString(wire)}")
        };
    }

    private byte[] HandleInitializeUpdate(byte[] wire)
    {
        var hostChallenge = ExtractData(wire);

        var context = new byte[16];
        hostChallenge.CopyTo(context, 0);
        CardChallenge.CopyTo(context, 8);

        _smac = DeriveKey(_staticKeys.Mac.ToArray(), DerivationTypeSMac, context, SessionKeyLengthBits);
        _senc = DeriveKey(_staticKeys.Enc.ToArray(), DerivationTypeSEnc, context, SessionKeyLengthBits);
        _srmac = DeriveKey(_staticKeys.Mac.ToArray(), DerivationTypeSRMac, context, SessionKeyLengthBits);
        var cardCryptogram = DeriveKey(_smac, DerivationTypeCardCryptogram, context, DerivationContextLengthBits);

        _macChain = new byte[16]; // matches ScpInitializer's `new ScpState(sessionKeys, new byte[16])`

        byte[] diversificationData = new byte[10];
        byte[] keyInfo = [0xFF, 0x02, 0x00];
        var data = Concat(diversificationData, keyInfo, CardChallenge, cardCryptogram);
        return Concat(data, StatusWordBytes(SWConstants.Success));
    }

    private byte[] HandleExternalAuthenticate(byte[] wire)
    {
        AdvanceMacChain(wire);
        return Ok(); // Zero-length response data: ScpProcessor skips R-MAC verification entirely.
    }

    private byte[] HandleGetData(byte[] wire)
    {
        AdvanceMacChain(wire); // macChain used for this response's R-MAC is the chain AFTER this command.

        if (_senc is null || _srmac is null)
            throw new InvalidOperationException("SCP03 handshake state not established before GET DATA.");

        const int encCounterForFirstEncryptedExchange = 1; // Client's _encCounter is 1 for the first post-handshake Encrypt()/Decrypt() pair.
        var ciphertext = EncryptResponsePayload(_senc, KeyInformationPlaintext, encCounterForFirstEncryptedExchange);
        var rmac = ComputeRMac(_srmac, _macChain, ciphertext, SWConstants.Success);

        return Concat(ciphertext, rmac, StatusWordBytes(SWConstants.Success));
    }

    private void AdvanceMacChain(byte[] wire)
    {
        if (_smac is null)
            throw new InvalidOperationException("SCP03 handshake state not established before a MACed command.");

        // Mirrors ScpProcessor.TransmitAsync's MAC scope: the full formatted command minus the
        // trailing 8-byte MAC and 1-byte Le (this fixture only ever sees short-APDU-formatted commands).
        var apduToMac = wire[..^9];
        _macChain = AesCmac(_smac, Concat(_macChain, apduToMac));
    }

    private static byte[] ExtractData(byte[] wire) =>
        wire.Length == 5 ? [] : wire[5..(5 + wire[4])];

    private static byte[] Ok() => StatusWordBytes(SWConstants.Success);

    private static byte[] Rejected(short sw) => StatusWordBytes(sw);

    private static byte[] StatusWordBytes(short sw)
    {
        var bytes = new byte[2];
        BinaryPrimitives.WriteInt16BigEndian(bytes, sw);
        return bytes;
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var result = new byte[parts.Sum(p => p.Length)];
        var offset = 0;
        foreach (var part in parts)
        {
            part.CopyTo(result, offset);
            offset += part.Length;
        }

        return result;
    }

    // --- SCP03 key derivation (GlobalPlatform Card Spec Amendment D / NIST SP 800-108 counter mode) ---

    internal static byte[] DeriveKey(byte[] key, byte derivationType, byte[] context16, short lengthBits)
    {
        var data = new byte[11 + 1 + 1 + 2 + 1 + context16.Length];
        data[11] = derivationType;
        data[12] = 0x00;
        BinaryPrimitives.WriteInt16BigEndian(data.AsSpan(13, 2), lengthBits);
        data[15] = 0x01;
        context16.CopyTo(data, 16);

        var mac = AesCmac(key, data);
        return mac[..(lengthBits / 8)];
    }

    private static byte[] EncryptResponsePayload(byte[] senc, byte[] plaintext, int encCounter)
    {
        using var aes = Aes.Create();
        aes.Key = senc;

        var iv = DeriveIv(aes, encCounter, ivPrefix: 0x80); // IvPrefixForDecryption in ScpState.cs

        var padLen = 16 - plaintext.Length % 16;
        var padded = new byte[plaintext.Length + padLen];
        plaintext.CopyTo(padded, 0);
        padded[plaintext.Length] = 0x80;

        var ciphertext = new byte[padded.Length];
        aes.EncryptCbc(padded, iv, ciphertext, PaddingMode.None);
        return ciphertext;
    }

    private static byte[] DeriveIv(Aes aes, int counter, byte ivPrefix)
    {
        var ivData = new byte[16];
        ivData[0] = ivPrefix;
        BinaryPrimitives.WriteInt32BigEndian(ivData.AsSpan(12, 4), counter);

        var iv = new byte[16];
        aes.EncryptEcb(ivData, iv, PaddingMode.None);
        return iv;
    }

    private static byte[] ComputeRMac(byte[] srmac, byte[] macChain, byte[] ciphertext, short sw)
    {
        var msg = Concat(ciphertext, StatusWordBytes(sw));
        var fullMac = AesCmac(srmac, Concat(macChain, msg));
        return fullMac[..8];
    }

    // --- RFC 4493 AES-128-CMAC, validated against the RFC's official test vectors in
    //     Scp03HandshakeFakeConnectionTests. Built from raw AES-ECB (BCL) since this SDK targets a
    //     .NET version with no built-in AES-CMAC primitive. ---

    internal static byte[] AesCmac(byte[] key, byte[] message)
    {
        using var aes = Aes.Create();
        aes.Key = key;

        var zero = new byte[16];
        var l = EncryptBlock(aes, zero);
        var k1 = ShiftLeftOneWithRb(l);
        var k2 = ShiftLeftOneWithRb(k1);

        var n = message.Length == 0 ? 1 : (message.Length + 15) / 16;
        var completeLastBlock = message.Length != 0 && message.Length % 16 == 0;

        byte[] mLast;
        if (completeLastBlock)
        {
            var lastBlock = message[^16..];
            mLast = Xor(lastBlock, k1);
        }
        else
        {
            var lastLen = message.Length - (n - 1) * 16;
            var lastBlock = new byte[16];
            Array.Copy(message, (n - 1) * 16, lastBlock, 0, lastLen);
            lastBlock[lastLen] = 0x80;
            mLast = Xor(lastBlock, k2);
        }

        var x = new byte[16];
        for (var i = 0; i < n - 1; i++)
        {
            var block = message.AsSpan(i * 16, 16).ToArray();
            x = EncryptBlock(aes, Xor(x, block));
        }

        var yLast = Xor(mLast, x);
        return EncryptBlock(aes, yLast);
    }

    private static byte[] ShiftLeftOneWithRb(byte[] input)
    {
        var msbSet = (input[0] & 0x80) != 0;
        var shifted = ShiftLeftOne(input);
        if (!msbSet)
            return shifted;

        shifted[15] ^= 0x87; // Rb constant for a 128-bit block cipher (RFC 4493 §2.3)
        return shifted;
    }

    private static byte[] ShiftLeftOne(byte[] input)
    {
        var output = new byte[input.Length];
        byte carry = 0;
        for (var i = input.Length - 1; i >= 0; i--)
        {
            var shifted = (input[i] << 1) | carry;
            output[i] = (byte)shifted;
            carry = (byte)((shifted >> 8) & 1);
        }

        return output;
    }

    private static byte[] EncryptBlock(Aes aes, byte[] block)
    {
        var output = new byte[16];
        aes.EncryptEcb(block, output, PaddingMode.None);
        return output;
    }

    private static byte[] Xor(byte[] a, byte[] b)
    {
        var result = new byte[a.Length];
        for (var i = 0; i < a.Length; i++)
            result[i] = (byte)(a[i] ^ b[i]);

        return result;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static NullDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}