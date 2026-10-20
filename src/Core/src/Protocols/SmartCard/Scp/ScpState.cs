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

namespace Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

/// <summary>
///     Per-session secure messaging state for an established SCP03 or SCP11 channel: the session keys,
///     the MAC chaining value and the encryption counter. Applies and removes C-MAC, C-DECRYPTION, R-MAC
///     and R-ENCRYPTION (GlobalPlatform Card Spec v2.3 Amd D v1.1.2.6 §6.2). SCP11 reuses the SCP03
///     secure messaging layer once its keys are derived (Amd F v1.4 §6).
/// </summary>
/// <remarks>
///     Establish a session with <see cref="Scp03InitAsync" /> or <see cref="Scp11InitAsync" />; the
///     partial files hold the protocol-specific handshakes. <c>macChain</c> is the initial MAC chaining
///     value: 16 zero bytes for SCP03 (Amd D §6.2.4), the receipt for SCP11 (Amd F §6.2.1).
/// </remarks>
internal partial class ScpState(SessionKeys keys, byte[] macChain, ILogger<ScpState>? logger = null) : IDisposable
{
    private const byte PaddingByte = 0x80; // ISO/IEC 9797-1 Padding Method 2 (Amd D §6.2.6)
    private const byte IvPrefixForDecryption = 0x80; // First ICV byte for responses; commands use 0x00 (Amd D §6.2.7)
    private const int RmacLength = 8; // S8 mode R-MAC (Amd D §6.2.5)
    private const int MacLength = 8; // S8 mode C-MAC (Amd D §6.2.4)
    private const int BlockSize = 16; // AES block, also the CMAC and chaining-value length
    private const int StackAllocThreshold = 512;

    // Starts at 1 for the first command after EXTERNAL AUTHENTICATE; one counter serves both the command
    // and its response (Amd D §6.2.6).
    private int _encCounter = 1;
    private byte[] _macChain = macChain;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        keys.Dispose();
        CryptographicOperations.ZeroMemory(_macChain);
    }

    /// <summary>
    ///     Returns an encryptor using the session DEK, for wrapping key material carried inside a
    ///     command (for example PUT KEY). Zero IV, caller supplies whole blocks.
    /// </summary>
    public DataEncryptor GetDataEncryptor() =>
        keys.Dek.IsEmpty
            ? throw new InvalidOperationException("Decryption key not initialized")
            : data => CbcEncrypt(keys.Dek, data);

    /// <summary>
    ///     Pads and encrypts a command data field (GlobalPlatform SCP03 Amd D §6.2.6).
    /// </summary>
    /// <remarks>
    ///     Advances the encryption counter. Call exactly once per protected command, even for an empty
    ///     data field, so the counter stays in step with the card.
    /// </remarks>
    /// <returns>The ciphertext. The caller owns it and must zero it after transmission.</returns>
    public byte[] Encrypt(ReadOnlySpan<byte> data)
    {
        logger?.LogTrace("Encrypting {ByteCount} bytes of command data", data.Length);

        var paddedLength = data.Length + (BlockSize - (data.Length % BlockSize));
        byte[]? rentedPadded = null;
        Span<byte> padded = paddedLength <= StackAllocThreshold
            ? stackalloc byte[paddedLength]
            : (rentedPadded = ArrayPool<byte>.Shared.Rent(paddedLength)).AsSpan(0, paddedLength);
        Span<byte> iv = stackalloc byte[BlockSize];

        try
        {
            // ISO/IEC 9797-1 Padding Method 2
            padded.Clear();
            data.CopyTo(padded);
            padded[data.Length] = PaddingByte;

            using var aes = Aes.Create();
            aes.SetKey(keys.Senc);

            DeriveIv(aes, 0x00, _encCounter++, iv);

            var encrypted = new byte[paddedLength];
            var encryptedBytesWritten = aes.EncryptCbc(padded, iv, encrypted, PaddingMode.None);
            if (encryptedBytesWritten != paddedLength)
                throw new InvalidOperationException("Data encryption failed");

            return encrypted;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(padded);
            CryptographicOperations.ZeroMemory(iv);
            if (rentedPadded is not null)
                ArrayPool<byte>.Shared.Return(rentedPadded);
        }
    }

    /// <summary>
    ///     Decrypts a response data field and strips its padding (GlobalPlatform SCP03 Amd D §6.2.7).
    /// </summary>
    /// <remarks>Does not change state. Uses the counter of the command this response answers.</remarks>
    /// <returns>The plaintext. The caller owns it.</returns>
    public byte[] Decrypt(ReadOnlySpan<byte> encrypted)
    {
        if (encrypted.Length == 0 || encrypted.Length % BlockSize != 0)
            throw new BadResponseException("Encrypted response data is not a whole number of AES blocks");

        byte[]? rentedDecrypted = null;
        Span<byte> decrypted = encrypted.Length <= StackAllocThreshold
            ? stackalloc byte[encrypted.Length]
            : (rentedDecrypted = ArrayPool<byte>.Shared.Rent(encrypted.Length)).AsSpan(0, encrypted.Length);
        Span<byte> iv = stackalloc byte[BlockSize];

        try
        {
            using var aes = Aes.Create();
            aes.SetKey(keys.Senc);

            // The response IV uses the counter of the command it answers, which Encrypt() already advanced.
            DeriveIv(aes, IvPrefixForDecryption, _encCounter - 1, iv);

            var decryptedBytesWritten = aes.DecryptCbc(encrypted, iv, decrypted, PaddingMode.None);
            if (decryptedBytesWritten != encrypted.Length)
                throw new InvalidOperationException("Data decryption failed");

            // Strip ISO/IEC 9797-1 Method 2 padding: trailing zeros, then one 0x80. The 0x80 may be at
            // index 0, which is an empty message.
            for (var i = decrypted.Length - 1; i >= 0; i--)
                if (decrypted[i] == PaddingByte)
                {
                    logger?.LogTrace("Decrypted {ByteCount} bytes of response data", i);
                    return decrypted[..i].ToArray();
                }
                else if (decrypted[i] != 0x00)
                {
                    break;
                }

            throw new BadResponseException("Bad padding");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(decrypted);
            CryptographicOperations.ZeroMemory(iv);
            if (rentedDecrypted is not null)
                ArrayPool<byte>.Shared.Return(rentedDecrypted);
        }
    }

    /// <summary>
    ///     Computes the C-MAC over a command APDU (GlobalPlatform SCP03 Amd D §6.2.4) and writes its
    ///     first 8 bytes to <paramref name="destination" />.
    /// </summary>
    /// <remarks>
    ///     Advances the MAC chaining value: the full 16-byte CMAC becomes the chaining value for the
    ///     next command and for this command's response. If the provider fails, the chaining value is
    ///     left unchanged.
    /// </remarks>
    public void Mac(ReadOnlySpan<byte> data, Span<byte> destination)
    {
        if (destination.Length < MacLength)
            throw new ArgumentException($"Destination must hold at least {MacLength} bytes", nameof(destination));

        var newMacChain = new byte[BlockSize];
        try
        {
            ComputeCmac(keys.Smac, _macChain, data, newMacChain);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(newMacChain);
            throw;
        }

        var previousMacChain = _macChain;
        _macChain = newMacChain;
        CryptographicOperations.ZeroMemory(previousMacChain);
        _macChain.AsSpan(0, MacLength).CopyTo(destination);
    }

    /// <summary>
    ///     Verifies the R-MAC on a response (GlobalPlatform SCP03 Amd D §6.2.5) and returns the data
    ///     field without it.
    /// </summary>
    /// <remarks>Does not change state. The response is checked against the current chaining value.</remarks>
    /// <exception cref="BadResponseException">The data field is too short or the R-MAC does not match.</exception>
    public byte[] Unmac(ReadOnlySpan<byte> data, short sw)
    {
        if (data.Length < RmacLength)
            throw new BadResponseException("Response too short to contain an R-MAC");

        var msgLength = data.Length - RmacLength + 2;
        byte[]? rentedMsg = null;
        Span<byte> computedMac = stackalloc byte[BlockSize];

        try
        {
            var msg = msgLength <= StackAllocThreshold
                ? stackalloc byte[msgLength]
                : (rentedMsg = ArrayPool<byte>.Shared.Rent(msgLength)).AsSpan(0, msgLength);

            data[..^RmacLength].CopyTo(msg);
            BinaryPrimitives.WriteInt16BigEndian(msg[(data.Length - RmacLength)..], sw);

            ComputeCmac(keys.Srmac, _macChain, msg, computedMac);

            var receivedMac = data[^RmacLength..];

            if (CryptographicOperations.FixedTimeEquals(computedMac[..RmacLength], receivedMac))
                return msg[..^2].ToArray();

            throw new BadResponseException("Wrong MAC");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(computedMac);
            if (rentedMsg is not null)
            {
                CryptographicOperations.ZeroMemory(rentedMsg);
                ArrayPool<byte>.Shared.Return(rentedMsg);
            }
        }
    }

    /// <summary>
    ///     Writes CMAC(<paramref name="key" />, <paramref name="chain" /> || <paramref name="data" />) to
    ///     <paramref name="destination" />. Only provider failures are wrapped, as
    ///     <see cref="NotSupportedException" />; other exceptions propagate unchanged.
    /// </summary>
    private static void ComputeCmac(
        ReadOnlySpan<byte> key, ReadOnlySpan<byte> chain, ReadOnlySpan<byte> data, Span<byte> destination)
    {
        try
        {
            using ICmacPrimitives mac = CryptographyProviders.CmacPrimitivesCreator(
                CmacBlockCipherAlgorithm.Aes128);
            mac.CmacInit(key);
            mac.CmacUpdate(chain);
            mac.CmacUpdate(data);
            mac.CmacFinal(destination);
        }
        catch (Exception e)
        {
            throw new NotSupportedException("Cryptography provider does not support AESCMAC", e);
        }
    }

    /// <summary>
    ///     Writes the SCP03 ICV, AES-ECB(S-ENC, prefix || 0..0 || counter), to <paramref name="destination" />
    ///     (GlobalPlatform SCP03 Amd D §6.2.6).
    /// </summary>
    private static void DeriveIv(Aes aes, byte prefix, int counter, Span<byte> destination)
    {
        Span<byte> ivData = stackalloc byte[BlockSize];
        ivData[0] = prefix;
        BinaryPrimitives.WriteInt32BigEndian(ivData[12..], counter);

        var ivBytesWritten = aes.EncryptEcb(ivData, destination, PaddingMode.None);
        if (ivBytesWritten != BlockSize)
            throw new InvalidOperationException("IV encryption failed");
    }

    private static byte[] CbcEncrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data)
    {
        using var aes = Aes.Create();
        aes.SetKey(key);
        Span<byte> iv = stackalloc byte[BlockSize]; // Zero IV
        var encrypted = new byte[data.Length];
        var bytesWritten = aes.EncryptCbc(data, iv, encrypted, PaddingMode.None);
        return bytesWritten != data.Length
            ? throw new InvalidOperationException("CBC encryption failed")
            : encrypted;
    }
}