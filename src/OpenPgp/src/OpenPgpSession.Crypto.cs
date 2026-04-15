// Copyright 2025 Yubico AB
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

using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.OpenPgp;

public sealed partial class OpenPgpSession
{
    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> SignAsync(
        ReadOnlyMemory<byte> message,
        HashAlgorithmName hashAlgorithm,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Signing with {Hash}", hashAlgorithm.Name);

        var sigAttrs = _appData.Discretionary.AlgorithmAttributesSig;
        var payload = FormatSignPayload(sigAttrs, message.Span, hashAlgorithm);

        // PSO: COMPUTE DIGITAL SIGNATURE — INS=0x2A, P1=0x9E, P2=0x9A
        var command = new ApduCommand(0x00, (int)Ins.Pso, 0x9E, 0x9A, payload);
        var response = await TransmitWithResponseAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return FormatSignResponse(sigAttrs, response.Data);
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> DecryptAsync(
        ReadOnlyMemory<byte> ciphertext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Decrypting ({Length} bytes)", ciphertext.Length);

        var decAttrs = _appData.Discretionary.AlgorithmAttributesDec;
        var payload = FormatDecryptPayload(decAttrs, ciphertext.Span);

        // PSO: DECIPHER — INS=0x2A, P1=0x80, P2=0x86
        var command = new ApduCommand(0x00, (int)Ins.Pso, 0x80, 0x86, payload);
        var response = await TransmitWithResponseAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return response.Data;
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> AuthenticateAsync(
        ReadOnlyMemory<byte> data,
        HashAlgorithmName hashAlgorithm,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Authenticating with {Hash}", hashAlgorithm.Name);

        var autAttrs = _appData.Discretionary.AlgorithmAttributesAut;
        var payload = FormatSignPayload(autAttrs, data.Span, hashAlgorithm);

        // INTERNAL AUTHENTICATE — INS=0x88, P1=0x00, P2=0x00
        var command = new ApduCommand(0x00, (int)Ins.InternalAuthenticate, 0x00, 0x00, payload);
        var response = await TransmitWithResponseAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return FormatSignResponse(autAttrs, response.Data);
    }

    // ── Private Helpers ───────────────────────────────────────────────

    /// <summary>
    ///     Formats the payload for Sign and Authenticate operations based on the key algorithm.
    /// </summary>
    private static byte[] FormatSignPayload(
        AlgorithmAttributes attrs,
        ReadOnlySpan<byte> message,
        HashAlgorithmName hashAlgorithm)
    {
        return attrs switch
        {
            RsaAttributes => FormatRsaSignPayload(message, hashAlgorithm),
            EcAttributes { AlgorithmId: EcAttributes.EddsaAlgorithmId } => message.ToArray(),
            EcAttributes => FormatEcSignPayload(message, hashAlgorithm),
            _ => throw new NotSupportedException($"Unsupported algorithm: {attrs.AlgorithmId}"),
        };
    }

    /// <summary>
    ///     RSA signing: DigestInfo header (PKCS#1 v1.5) + hash of the message.
    /// </summary>
    private static byte[] FormatRsaSignPayload(ReadOnlySpan<byte> message, HashAlgorithmName hashAlgorithm)
    {
        var hashSize = GetHashSize(hashAlgorithm);
        Span<byte> hash = stackalloc byte[hashSize];
        HashMessage(hashAlgorithm, message, hash);

        return DigestInfo.Build(hashAlgorithm, hash);
    }

    /// <summary>
    ///     EC signing: raw hash of the message.
    /// </summary>
    private static byte[] FormatEcSignPayload(ReadOnlySpan<byte> message, HashAlgorithmName hashAlgorithm)
    {
        var hashSize = GetHashSize(hashAlgorithm);
        var hash = new byte[hashSize];
        HashMessage(hashAlgorithm, message, hash);
        return hash;
    }

    /// <summary>
    ///     Formats the response from Sign and Authenticate operations.
    ///     EC signatures are DER-encoded from the raw (r || s) concatenation.
    /// </summary>
    private static ReadOnlyMemory<byte> FormatSignResponse(
        AlgorithmAttributes attrs,
        ReadOnlyMemory<byte> response)
    {
        if (attrs is not EcAttributes || attrs.AlgorithmId == EcAttributes.EddsaAlgorithmId)
        {
            // RSA and EdDSA: raw signature bytes
            return response;
        }

        // ECDSA: card returns r || s concatenated, encode as DER
        return EncodeDerSignature(response.Span);
    }

    /// <summary>
    ///     Formats the payload for Decrypt operations based on the key algorithm.
    /// </summary>
    private static byte[] FormatDecryptPayload(
        AlgorithmAttributes attrs,
        ReadOnlySpan<byte> ciphertext)
    {
        return attrs switch
        {
            RsaAttributes => FormatRsaDecryptPayload(ciphertext),
            EcAttributes { Oid: CurveOid.X25519 } => ciphertext.ToArray(),
            EcAttributes => FormatEcDecryptPayload(ciphertext),
            _ => throw new NotSupportedException($"Unsupported algorithm: {attrs.AlgorithmId}"),
        };
    }

    /// <summary>
    ///     RSA decrypt: prepend 0x00 padding indicator byte.
    /// </summary>
    private static byte[] FormatRsaDecryptPayload(ReadOnlySpan<byte> ciphertext)
    {
        var result = new byte[1 + ciphertext.Length];
        result[0] = 0x00;
        ciphertext.CopyTo(result.AsSpan(1));
        return result;
    }

    /// <summary>
    ///     EC decrypt (ECDH): wrap in TLV(0xA6, TLV(0x7F49, TLV(0x86, publicKey))).
    /// </summary>
    private static byte[] FormatEcDecryptPayload(ReadOnlySpan<byte> ephemeralPublicKey)
    {
        using var innerTlv = new Tlv(0x86, ephemeralPublicKey);
        using var middleTlv = new Tlv(0x7F49, innerTlv.AsSpan());
        using var outerTlv = new Tlv(0xA6, middleTlv.AsSpan());
        return outerTlv.AsSpan().ToArray();
    }

    /// <summary>
    ///     DER-encodes an ECDSA signature from raw (r || s) concatenation to ASN.1 SEQUENCE.
    /// </summary>
    private static byte[] EncodeDerSignature(ReadOnlySpan<byte> rawSignature)
    {
        var half = rawSignature.Length / 2;
        var r = rawSignature[..half];
        var s = rawSignature[half..];

        var rDer = EncodeAsn1Integer(r);
        var sDer = EncodeAsn1Integer(s);

        // SEQUENCE { r INTEGER, s INTEGER }
        var seqLength = rDer.Length + sDer.Length;

        byte[] result;
        int contentOffset;

        if (seqLength >= 128)
        {
            // Long-form DER length encoding: 0x30 0x81 <length> <content>
            result = new byte[3 + seqLength];
            result[0] = 0x30;
            result[1] = 0x81;
            result[2] = (byte)seqLength;
            contentOffset = 3;
        }
        else
        {
            // Short-form DER length encoding: 0x30 <length> <content>
            result = new byte[2 + seqLength];
            result[0] = 0x30;
            result[1] = (byte)seqLength;
            contentOffset = 2;
        }

        rDer.CopyTo(result.AsSpan(contentOffset));
        sDer.CopyTo(result.AsSpan(contentOffset + rDer.Length));

        return result;
    }

    /// <summary>
    ///     Encodes a big-endian unsigned integer as ASN.1 INTEGER.
    /// </summary>
    private static byte[] EncodeAsn1Integer(ReadOnlySpan<byte> value)
    {
        // Skip leading zeros
        var startIndex = 0;
        while (startIndex < value.Length - 1 && value[startIndex] == 0)
        {
            startIndex++;
        }

        var trimmed = value[startIndex..];
        var needsPadding = (trimmed[0] & 0x80) != 0;
        var length = trimmed.Length + (needsPadding ? 1 : 0);

        var result = new byte[2 + length]; // tag + length + value
        result[0] = 0x02; // INTEGER tag
        result[1] = (byte)length;

        if (needsPadding)
        {
            result[2] = 0x00;
            trimmed.CopyTo(result.AsSpan(3));
        }
        else
        {
            trimmed.CopyTo(result.AsSpan(2));
        }

        return result;
    }

    private static int GetHashSize(HashAlgorithmName hashAlgorithm) =>
        hashAlgorithm.Name switch
        {
            "SHA1" => 20,
            "SHA256" => 32,
            "SHA384" => 48,
            "SHA512" => 64,
            _ => throw new NotSupportedException($"Unsupported hash algorithm: {hashAlgorithm.Name}"),
        };

    private static void HashMessage(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> message, Span<byte> destination)
    {
        var result = hashAlgorithm.Name switch
        {
            "SHA1" => SHA1.TryHashData(message, destination, out _),
            "SHA256" => SHA256.TryHashData(message, destination, out _),
            "SHA384" => SHA384.TryHashData(message, destination, out _),
            "SHA512" => SHA512.TryHashData(message, destination, out _),
            _ => throw new NotSupportedException($"Unsupported hash algorithm: {hashAlgorithm.Name}"),
        };

        if (!result)
        {
            throw new CryptographicException("Hash computation failed.");
        }
    }
}