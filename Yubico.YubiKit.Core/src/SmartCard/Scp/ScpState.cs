// Copyright (C) 2024 Yubico.
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

using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
///     Internal SCP state class for managing SCP state, handling encryption/decryption and MAC.
/// </summary>
internal sealed partial class ScpState(SessionKeys keys, byte[] macChain, ILogger<ScpState>? logger = null)
{
    // SecurityDomainSession instruction codes (temporary - will move to SecurityDomainSession class)
    private const byte InsInitializeUpdate = 0x50;
    private const byte InsPerformSecurityOperation = 0x2A;
    private const byte InsInternalAuthenticate = 0x88;
    private const byte InsExternalAuthenticate = 0x82;

    // Encryption/Padding Constants
    private const byte PaddingByte = 0x80; // ISO/IEC 9797-1 Padding Method 2
    private const byte IvPrefixForDecryption = 0x80; // IV generation prefix for response decryption

    // Key Derivation Constants
    private const byte DerivationTypeCardCryptogram = 0x00; // Derivation type for card cryptogram verification
    private const byte DerivationTypeHostCryptogram = 0x01; // Derivation type for host cryptogram generation
    private const byte DerivationContextLength = 0x40; // Context length in bits (64 bits = 8 bytes)

    // SCP11 Constants
    private const byte Scp11aTypeParam = 0x01;
    private const byte Scp11bTypeParam = 0x00;
    private const byte Scp11cTypeParam = 0x03;
    private const byte Scp11MoreFragmentsFlag = 0x80;
    private const byte Scp11KeyUsage = 0x3C; // AUTHENTICATED | C_MAC | C_DECRYPTION | R_MAC | R_ENCRYPTION
    private const byte Scp11KeyType = 0x88; // AES

    private int _encCounter = 1; // Counter for command encryption (host->card)
    private byte[] _macChain = macChain;
    private int _respCounter = 1; // Counter for response decryption (card->host)

    public DataEncryptor GetDataEncryptor() =>
        keys.Dek.IsEmpty
            ? throw new InvalidOperationException("Decryption key not initialized")
            : data => CbcEncrypt(keys.Dek, data);

    public byte[] Encrypt(ReadOnlySpan<byte> data)
    {
        // Pad the data
        logger?.LogTrace("Plaintext data: {Data}", Convert.ToHexString(data));

        var padLen = 16 - (data.Length % 16);
        var paddedLength = data.Length + padLen;
        var padded = new byte[paddedLength];

        data.CopyTo(padded);
        padded[data.Length] = PaddingByte;

        byte[]? iv = null;
        using var aes = Aes.Create();
        aes.Key = keys.Senc.ToArray();

        try
        {
            // Generate IV using ECB encryption of counter
            Span<byte> ivData = stackalloc byte[16];
            BinaryPrimitives.WriteInt32BigEndian(ivData[12..], _encCounter++);

            iv = new byte[16];
            var ivBytesWritten = aes.EncryptEcb(ivData, iv, PaddingMode.None);
            if (ivBytesWritten != 16)
                throw new InvalidOperationException("IV encryption failed");

            // Encrypt using CBC with generated IV
            var encrypted = new byte[paddedLength];
            var encryptedBytesWritten = aes.EncryptCbc(padded, iv, encrypted, PaddingMode.None);
            if (encryptedBytesWritten != paddedLength)
                throw new InvalidOperationException("Data encryption failed");

            return encrypted;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(padded);
            if (iv != null)
                CryptographicOperations.ZeroMemory(iv);
        }
    }

    public byte[] Decrypt(ReadOnlySpan<byte> encrypted)
    {
        byte[]? iv = null;
        byte[]? decrypted = null;
        using var aes = Aes.Create();
        aes.Key = keys.Senc.ToArray();

        try
        {
            // Generate IV using ECB encryption of counter with prefix
            // Use separate response counter that increments independently
            Span<byte> ivData = stackalloc byte[16];
            ivData[0] = IvPrefixForDecryption;
            BinaryPrimitives.WriteInt32BigEndian(ivData[12..], _respCounter++);

            iv = new byte[16];
            var ivBytesWritten = aes.EncryptEcb(ivData, iv, PaddingMode.None);
            if (ivBytesWritten != 16)
                throw new InvalidOperationException("IV encryption failed");

            // Decrypt using CBC
            decrypted = new byte[encrypted.Length];
            var decryptedBytesWritten = aes.DecryptCbc(encrypted, iv, decrypted, PaddingMode.None);
            if (decryptedBytesWritten != encrypted.Length)
                throw new InvalidOperationException("Data decryption failed");

            // Find and remove padding
            for (var i = decrypted.Length - 1; i > 0; i--)
                if (decrypted[i] == PaddingByte)
                {
                    logger?.LogTrace("Plaintext resp: {Data}", Convert.ToHexString(decrypted.AsSpan(0, i)));
                    var result = decrypted[..i];
                    return result;
                }
                else if (decrypted[i] != 0x00)
                {
                    break;
                }

            throw new BadResponseException("Bad padding");
        }
        finally
        {
            if (iv != null)
                CryptographicOperations.ZeroMemory(iv);
            if (decrypted != null)
                CryptographicOperations.ZeroMemory(decrypted);
        }
    }

    public byte[] Mac(ReadOnlySpan<byte> data)
    {
        try
        {
            Console.WriteLine($"[MAC DEBUG] Computing MAC over {data.Length} bytes");
            Console.WriteLine($"[MAC DEBUG] MAC chain: {Convert.ToHexString(_macChain)}");
            Console.WriteLine($"[MAC DEBUG] Data: {Convert.ToHexString(data)}");

            using var mac = new AesCmac(keys.Smac);
            mac.AppendData(_macChain);
            mac.AppendData(data);
            _macChain = mac.GetHashAndReset();

            Console.WriteLine($"[MAC DEBUG] Full MAC (16 bytes): {Convert.ToHexString(_macChain)}");
            Console.WriteLine($"[MAC DEBUG] C-MAC (first 8 bytes): {Convert.ToHexString(_macChain.AsSpan()[..8])}");

            return _macChain[..8].ToArray();
        }
        catch (Exception e)
        {
            throw new NotSupportedException("Cryptography provider does not support AESCMAC", e);
        }
    }

    public byte[] Unmac(ReadOnlySpan<byte> data, short sw)
    {
        var msgLength = data.Length - 8 + 2;
        byte[]? rentedMsg = null;

        try
        {
            var msg = msgLength <= 512
                ? stackalloc byte[msgLength]
                : (rentedMsg = ArrayPool<byte>.Shared.Rent(msgLength)).AsSpan(0, msgLength);

            data[..^8].CopyTo(msg);
            BinaryPrimitives.WriteInt16BigEndian(msg[(data.Length - 8)..], sw);

            using var mac = new AesCmac(keys.Srmac);
            mac.AppendData(_macChain);
            mac.AppendData(msg);
            Span<byte> computedMac = stackalloc byte[16];
            mac.GetHashAndReset().AsSpan().CopyTo(computedMac);

            var receivedMac = data[(data.Length - 8)..];

            if (CryptographicOperations.FixedTimeEquals(computedMac[..8], receivedMac))
                return msg[..^2].ToArray();

            throw new BadResponseException("Wrong MAC");
        }
        catch (BadResponseException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new NotSupportedException("Cryptography provider does not support AESCMAC", e);
        }
        finally
        {
            if (rentedMsg != null)
            {
                CryptographicOperations.ZeroMemory(rentedMsg);
                ArrayPool<byte>.Shared.Return(rentedMsg);
            }
        }
    }

    private static byte[] CbcEncrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data)
    {
        using var aes = Aes.Create();

        aes.Mode = CipherMode.CBC;
        aes.Key = key.ToArray();
        Span<byte> iv = stackalloc byte[16]; // Zero IV
        var encrypted = new byte[data.Length];
        var bytesWritten = aes.EncryptCbc(data, iv, encrypted, PaddingMode.None);
        return bytesWritten != data.Length
            ? throw new InvalidOperationException("CBC encryption failed")
            : encrypted;
    }
}