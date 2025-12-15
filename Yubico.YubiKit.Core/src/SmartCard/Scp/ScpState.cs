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
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
///     Internal SCP state class for managing SCP state, handling encryption/decryption and MAC.
/// </summary>
internal sealed class ScpState(SessionKeys keys, byte[] macChain, ILogger<ScpState>? logger = null)
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

            data[..(data.Length - 8)].CopyTo(msg);
            BinaryPrimitives.WriteInt16BigEndian(msg[(data.Length - 8)..], sw);

            using var mac = new AesCmac(keys.Srmac);
            mac.AppendData(_macChain);
            mac.AppendData(msg);
            Span<byte> computedMac = stackalloc byte[16];
            mac.GetHashAndReset().AsSpan().CopyTo(computedMac);

            var receivedMac = data[(data.Length - 8)..];

            if (CryptographicOperations.FixedTimeEquals(computedMac[..8], receivedMac))
                return msg[..(msg.Length - 2)].ToArray();
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

    public static async Task<(ScpState State, byte[] HostCryptogram)> Scp03InitAsync(
        IApduProcessor processor,
        Scp03KeyParameters keyParams,
        byte[]? hostChallenge = null,
        CancellationToken cancellationToken = default)
    {
        hostChallenge ??= RandomNumberGenerator.GetBytes(8);

        var initCommand = new ApduCommand(
            0x80,
            InsInitializeUpdate,
            keyParams.KeyReference.Kvn,
            0x00,
            hostChallenge);

        var resp = await processor.TransmitAsync(initCommand, false, cancellationToken).ConfigureAwait(false);
        if (!resp.IsOK())
            throw ApduException.FromResponse(resp, initCommand, "SCP03 INITIALIZE UPDATE failed");

        var responseData = resp.Data.Span;
        var diversificationData = responseData[..10];
        var keyInfo = responseData[10..13];
        var cardChallenge = responseData[13..21];
        var cardCryptogram = responseData[21..29];

        Console.WriteLine("[DEBUG] INITIALIZE UPDATE response:");
        Console.WriteLine($"[DEBUG]   Key Info: {Convert.ToHexString(keyInfo)}");
        Console.WriteLine($"[DEBUG]   Key Info[0] (Key Diversification): 0x{keyInfo[0]:X2}");
        Console.WriteLine($"[DEBUG]   Key Info[1] (Key Version): 0x{keyInfo[1]:X2}");
        Console.WriteLine($"[DEBUG]   Key Info[2] (SCP ID): 0x{keyInfo[2]:X2}");

        Span<byte> context = stackalloc byte[16];
        hostChallenge.AsSpan().CopyTo(context);
        cardChallenge.CopyTo(context[8..]);

        Console.WriteLine($"[DEBUG] Host challenge: {Convert.ToHexString(hostChallenge)}");
        Console.WriteLine($"[DEBUG] Card challenge: {Convert.ToHexString(cardChallenge)}");
        Console.WriteLine($"[DEBUG] Context: {Convert.ToHexString(context)}");

        var sessionKeys = keyParams.Keys.Derive(context);

        Console.WriteLine($"[DEBUG] S-ENC:  {Convert.ToHexString(sessionKeys.Senc)}");
        Console.WriteLine($"[DEBUG] S-MAC:  {Convert.ToHexString(sessionKeys.Smac)}");
        Console.WriteLine($"[DEBUG] S-RMAC: {Convert.ToHexString(sessionKeys.Srmac)}");

        Span<byte> genCardCryptogram = stackalloc byte[8];
        StaticKeys.DeriveKey(sessionKeys.Smac, DerivationTypeCardCryptogram, context, DerivationContextLength,
            genCardCryptogram);

        Console.WriteLine($"[DEBUG] Generated card cryptogram: {Convert.ToHexString(genCardCryptogram)}");
        Console.WriteLine($"[DEBUG] Received card cryptogram:  {Convert.ToHexString(cardCryptogram)}");

        if (!CryptographicOperations.FixedTimeEquals(genCardCryptogram, cardCryptogram))
            throw new BadResponseException(
                $"Wrong SCP03 key set - Expected: {Convert.ToHexString(cardCryptogram)}, Got: {Convert.ToHexString(genCardCryptogram)}");

        Span<byte> hostCryptogramBytes = stackalloc byte[8];
        StaticKeys.DeriveKey(sessionKeys.Smac, DerivationTypeHostCryptogram, context, DerivationContextLength,
            hostCryptogramBytes);

        return (new ScpState(sessionKeys, new byte[16]), hostCryptogramBytes.ToArray());
    }

    public static async Task<ScpState> Scp11InitAsync(
        IApduProcessor processor,
        Scp11KeyParameters keyParams,
        CancellationToken cancellationToken = default)
    {
        // GPC v2.3 Amendment F (SCP11) v1.4 §7.1.1
        var kid = keyParams.KeyReference.Kid;

        var scpTypeParam = kid switch
        {
            ScpKid.SCP11a => Scp11aTypeParam,
            ScpKid.SCP11b => Scp11bTypeParam,
            ScpKid.SCP11c => Scp11cTypeParam,
            _ => throw new ArgumentException("Invalid SCP11 KID")
        };

        if (kid is ScpKid.SCP11a or ScpKid.SCP11c)
        {
            // GPC v2.3 Amendment F (SCP11) v1.4 §7.5
            ArgumentNullException.ThrowIfNull(keyParams.SkOceEcka);

            var n = keyParams.Certificates.Count - 1;
            if (n < 0) throw new ArgumentException("SCP11a and SCP11c require a certificate chain");

            var oceRef = keyParams.OceKeyRef ?? new KeyReference(0, 0);
            for (var i = 0; i <= n; i++)
            {
                var certData = keyParams.Certificates[i].GetRawCertData();
                var p2 = (byte)(oceRef.Kid | (i < n ? Scp11MoreFragmentsFlag : 0x00));
                var certCommand = new ApduCommand(
                    0x80,
                    InsPerformSecurityOperation,
                    oceRef.Kvn,
                    p2,
                    certData);
                var resp = await processor.TransmitAsync(certCommand, false, cancellationToken).ConfigureAwait(false);
                if (resp.SW != SWConstants.Success)
                    throw ApduException.FromResponse(resp, certCommand, "SCP11 PERFORM SECURITY OPERATION failed");
            }
        }

        byte[] keyUsage = [Scp11KeyUsage]; // AUTHENTICATED | C_MAC | C_DECRYPTION | R_MAC | R_ENCRYPTION
        byte[] keyType = [Scp11KeyType]; // AES
        byte[] keyLen = [16]; // 128-bit

        // Host ephemeral key
        var pkSdEcka = keyParams.PkSdEcka;

        // Create ephemeral OCE key using the same curve as the SD public key
        var sdParameters = pkSdEcka.Parameters;

        // Create ephemeral OCE ECDH key pair using the same curve
        var ephemeralOceEcka = ECDiffieHellman.Create(sdParameters.Curve);
        var epkOceEcka = ECPublicKey.CreateFromParameters(ephemeralOceEcka.ExportParameters(false));

        // GPC v2.3 Amendment F (SCP11) v1.4 §7.6.2.3
        using var scpTypeTlv = new Tlv(0x90, [0x11, scpTypeParam]);
        using var keyUsageTlv = new Tlv(0x95, keyUsage);
        using var keyTypeTlv = new Tlv(0x80, keyType);
        using var keyLenTlv = new Tlv(0x81, keyLen);
        var innerTlvs = TlvHelper.EncodeList([scpTypeTlv, keyUsageTlv, keyTypeTlv, keyLenTlv]);

        using var outerTlv1 = new Tlv(0xA6, innerTlvs.Span);
        using var outerTlv2 = new Tlv(0x5F49, epkOceEcka.PublicPoint.Span);
        var outerTlvs = TlvHelper.EncodeList([outerTlv1, outerTlv2]);
        var data = outerTlvs;

        var ins = keyParams.KeyReference.Kid == ScpKid.SCP11b
            ? InsInternalAuthenticate
            : InsExternalAuthenticate;

        var authCommand = new ApduCommand(
            0x80,
            ins,
            keyParams.KeyReference.Kvn,
            keyParams.KeyReference.Kid,
            data);

        var response = await processor.TransmitAsync(authCommand, false, cancellationToken).ConfigureAwait(false);
        if (response.SW != SWConstants.Success)
            throw ApduException.FromResponse(response, authCommand, "SCP11 authentication failed");

        using var tlvs = TlvHelper.DecodeList(response.Data.Span);

        var epkSdEckaTlv = tlvs[0];
        var epkSdEckaEncodedPointMem = TlvHelper.GetValue(0x5F49, epkSdEckaTlv.Value.Span);
        var receiptMem = TlvHelper.GetValue(0x86, tlvs[1].Value.Span);
        var receipt = receiptMem.ToArray();

        // Static host key (SCP11a/c), or ephemeral key again (SCP11b)
        var skOceEcka = keyParams.SkOceEcka?.ToECDiffieHellman() ?? ephemeralOceEcka;

        // GPC v2.3 Amendment F (SCP11) v1.3 §3.1.2 Key Derivation
        var epkSdEckaTlvBytes = epkSdEckaTlv.AsMemory();
        byte[]? rentedKeyAgreementData = null;
        byte[]? rentedKeyMaterial = null;

        try
        {
            var keyAgreementDataLen = data.Length + epkSdEckaTlvBytes.Length;
            rentedKeyAgreementData = ArrayPool<byte>.Shared.Rent(keyAgreementDataLen);
            var keyAgreementData = rentedKeyAgreementData.AsSpan(0, keyAgreementDataLen);
            data.Span.CopyTo(keyAgreementData);
            epkSdEckaTlvBytes.Span.CopyTo(keyAgreementData[data.Length..]);

            Span<byte> sharedInfo = stackalloc byte[keyUsage.Length + keyType.Length + keyLen.Length];
            keyUsage.AsSpan().CopyTo(sharedInfo);
            keyType.AsSpan().CopyTo(sharedInfo[keyUsage.Length..]);
            keyLen.AsSpan().CopyTo(sharedInfo[(keyUsage.Length + keyType.Length)..]);

            // Key agreement 1: ephemeral OCE private key with ephemeral SD public key
            var epkSdEcka = ECPublicKey.CreateFromValue(
                epkSdEckaEncodedPointMem,
                pkSdEcka.KeyType);
            var ka1 = epkSdEcka.DeriveKeyMaterial(ephemeralOceEcka);

            // Key agreement 2: static/ephemeral OCE private key with static SD public key
            var ka2 = pkSdEcka.DeriveKeyMaterial(skOceEcka);

            var keyMaterialLen = ka1.Length + ka2.Length;
            rentedKeyMaterial = ArrayPool<byte>.Shared.Rent(keyMaterialLen);
            var keyMaterial = rentedKeyMaterial.AsSpan(0, keyMaterialLen);
            ka1.AsSpan().CopyTo(keyMaterial);
            ka2.AsSpan().CopyTo(keyMaterial[ka1.Length..]);
            CryptographicOperations.ZeroMemory(ka1);
            CryptographicOperations.ZeroMemory(ka2);

            var keys = new List<byte[]>();
            var counter = 1;

            // We need 5 16-byte keys, which requires 3 iterations of SHA256
            Span<byte> counterBytes = stackalloc byte[4];
            Span<byte> digest = stackalloc byte[32];
            for (var i = 0; i < 3; i++)
            {
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                BinaryPrimitives.WriteInt32BigEndian(counterBytes, counter++);

                hash.AppendData(keyMaterial);
                hash.AppendData(counterBytes);
                hash.AppendData(sharedInfo);

                // Each iteration gives us 2 keys
                var bytesWritten = hash.GetHashAndReset(digest);
                if (bytesWritten != 32) throw new InvalidOperationException("Hash computation failed");

                var key1 = digest[..16].ToArray();
                var key2 = digest[16..32].ToArray();
                keys.Add(key1);
                keys.Add(key2);
                CryptographicOperations.ZeroMemory(digest);
            }

            // 6 keys were derived: one for verification of receipt, 4 keys to use, and 1 which is discarded
            var key = keys[0];
            using var mac = new AesCmac(key);
            mac.AppendData(keyAgreementData);
            var genReceipt = mac.GetHashAndReset();

            if (!CryptographicOperations.FixedTimeEquals(receipt, genReceipt))
                throw new BadResponseException("Receipt does not match");

            return new ScpState(
                new SessionKeys(keys[1], keys[2], keys[3], keys[4]),
                receipt);
        }
        finally
        {
            if (rentedKeyAgreementData != null)
            {
                CryptographicOperations.ZeroMemory(rentedKeyAgreementData);
                ArrayPool<byte>.Shared.Return(rentedKeyAgreementData);
            }

            if (rentedKeyMaterial != null)
            {
                CryptographicOperations.ZeroMemory(rentedKeyMaterial);
                ArrayPool<byte>.Shared.Return(rentedKeyMaterial);
            }

            CryptographicOperations.ZeroMemory(epkSdEckaTlvBytes.Span);
            CryptographicOperations.ZeroMemory(epkSdEckaEncodedPointMem.Span);
            CryptographicOperations.ZeroMemory(receipt);
            CryptographicOperations.ZeroMemory(data.Span);
            CryptographicOperations.ZeroMemory(innerTlvs.Span);
            CryptographicOperations.ZeroMemory(outerTlvs.Span);

            skOceEcka.Dispose();
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