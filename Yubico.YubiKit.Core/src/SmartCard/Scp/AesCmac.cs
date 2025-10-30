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

using System;
using System.Buffers;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
/// Implements AES-CMAC (Cipher-based Message Authentication Code) as defined in NIST SP 800-38B.
/// </summary>
internal sealed class AesCmac : IDisposable
{
    private const int BlockSize = 16;
    private readonly byte[] _key;
    private readonly byte[] _subkey1;
    private readonly byte[] _subkey2;
    private readonly byte[] _buffer;
    private int _bufferOffset;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesCmac"/> class.
    /// </summary>
    /// <param name="key">The AES key (must be 16 bytes for AES-128).</param>
    /// <exception cref="ArgumentException">Thrown if the key is not 16 bytes.</exception>
    public AesCmac(ReadOnlySpan<byte> key)
    {
        if (key.Length != 16)
        {
            throw new ArgumentException("AES-CMAC key must be 16 bytes", nameof(key));
        }

        _key = key.ToArray();
        _buffer = new byte[BlockSize];
        _subkey1 = new byte[BlockSize];
        _subkey2 = new byte[BlockSize];

        GenerateSubkeys();
    }

    /// <summary>
    /// Appends data to the CMAC computation.
    /// </summary>
    /// <param name="data">The data to append.</param>
    public void AppendData(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        int offset = 0;
        int remaining = data.Length;

        // Fill buffer if partially full
        if (_bufferOffset > 0)
        {
            int toCopy = Math.Min(BlockSize - _bufferOffset, remaining);
            data[offset..(offset + toCopy)].CopyTo(_buffer.AsSpan(_bufferOffset));
            _bufferOffset += toCopy;
            offset += toCopy;
            remaining -= toCopy;

            if (_bufferOffset == BlockSize)
            {
                ProcessBlock(_buffer);
                _bufferOffset = 0;
            }
        }

        // Process complete blocks
        while (remaining >= BlockSize)
        {
            ProcessBlock(data[offset..(offset + BlockSize)]);
            offset += BlockSize;
            remaining -= BlockSize;
        }

        // Buffer remaining data
        if (remaining > 0)
        {
            data[offset..(offset + remaining)].CopyTo(_buffer.AsSpan(_bufferOffset));
            _bufferOffset += remaining;
        }
    }

    /// <summary>
    /// Finalizes the CMAC computation, returns the MAC, and resets the state.
    /// </summary>
    /// <returns>The 16-byte CMAC value.</returns>
    public byte[] GetHashAndReset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        byte[] lastBlock = new byte[BlockSize];

        if (_bufferOffset == BlockSize)
        {
            // Complete final block - XOR with K1
            for (int i = 0; i < BlockSize; i++)
            {
                lastBlock[i] = (byte)(_buffer[i] ^ _subkey1[i]);
            }
        }
        else
        {
            // Incomplete final block - pad and XOR with K2
            _buffer.AsSpan(0, _bufferOffset).CopyTo(lastBlock);
            lastBlock[_bufferOffset] = 0x80;
            // Remaining bytes are already zero

            for (int i = 0; i < BlockSize; i++)
            {
                lastBlock[i] ^= _subkey2[i];
            }
        }

        // Final encryption
        using var aes = Aes.Create();
        aes.Key = _key;

        Span<byte> iv = stackalloc byte[BlockSize]; // Zero IV
        var result = new byte[BlockSize];
        var bytesWritten = aes.EncryptCbc(lastBlock, iv, result, PaddingMode.None);
        if (bytesWritten != BlockSize)
            throw new InvalidOperationException("Final CMAC encryption failed");

        // Reset state
        Array.Clear(_buffer);
        _bufferOffset = 0;

        return result;
    }

    /// <summary>
    /// Releases the resources used by this instance and securely zeroes all key material.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_key);
        CryptographicOperations.ZeroMemory(_subkey1);
        CryptographicOperations.ZeroMemory(_subkey2);
        CryptographicOperations.ZeroMemory(_buffer);

        _disposed = true;
    }

    private void GenerateSubkeys()
    {
        using var aes = Aes.Create();
        aes.Key = _key;

        // Encrypt zero block to get L
        Span<byte> zero = stackalloc byte[BlockSize];
        var l = new byte[BlockSize];
        var bytesWritten = aes.EncryptEcb(zero, l, PaddingMode.None);
        if (bytesWritten != BlockSize)
            throw new InvalidOperationException("Subkey generation failed");

        // Generate K1 by left-shifting L and conditionally XORing with Rb
        LeftShiftOneAnd(l, _subkey1);

        // Generate K2 by left-shifting K1 and conditionally XORing with Rb
        LeftShiftOneAnd(_subkey1, _subkey2);

        CryptographicOperations.ZeroMemory(l);
    }

    private static void LeftShiftOneAnd(ReadOnlySpan<byte> input, Span<byte> output)
    {
        const byte Rb = 0x87; // Reduction polynomial for AES block size

        byte overflow = 0;
        for (int i = BlockSize - 1; i >= 0; i--)
        {
            output[i] = (byte)((input[i] << 1) | overflow);
            overflow = (byte)((input[i] >> 7) & 1);
        }

        // If MSB of input was 1, XOR with Rb
        if (overflow != 0)
        {
            output[BlockSize - 1] ^= Rb;
        }
    }

    private void ProcessBlock(ReadOnlySpan<byte> block)
    {
        using var aes = Aes.Create();
        aes.Key = _key;

        // XOR block with previous output (CBC mode)
        for (int i = 0; i < BlockSize; i++)
        {
            _buffer[i] ^= block[i];
        }

        // Encrypt
        Span<byte> iv = stackalloc byte[BlockSize]; // Zero IV
        Span<byte> encrypted = stackalloc byte[BlockSize];
        var bytesWritten = aes.EncryptCbc(_buffer, iv, encrypted, PaddingMode.None);
        if (bytesWritten != BlockSize)
            throw new InvalidOperationException("Block encryption failed");

        encrypted.CopyTo(_buffer);
    }
}
