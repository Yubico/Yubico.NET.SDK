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
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
/// Manages static SCP03 keys and derives session keys from them.
/// </summary>
internal sealed class StaticKeys : IDisposable
{
    private static readonly byte[] DefaultKeyBytes = [0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F];

    private byte[]? _enc;
    private byte[]? _mac;
    private byte[]? _dek;
    private bool _disposed;

    /// <summary>
    /// Gets the static encryption key.
    /// </summary>
    internal ReadOnlySpan<byte> Enc => _enc;

    /// <summary>
    /// Gets the static MAC key.
    /// </summary>
    internal ReadOnlySpan<byte> Mac => _mac;

    /// <summary>
    /// Gets the static DEK (Data Encryption Key), if available.
    /// </summary>
    internal ReadOnlySpan<byte> Dek => _dek;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticKeys"/> class.
    /// </summary>
    /// <param name="enc">The static encryption key (must be 16 bytes).</param>
    /// <param name="mac">The static MAC key (must be 16 bytes).</param>
    /// <param name="dek">The optional static DEK (must be 16 bytes if provided).</param>
    /// <exception cref="ArgumentException">Thrown if any key is not 16 bytes.</exception>
    public StaticKeys(ReadOnlySpan<byte> enc, ReadOnlySpan<byte> mac, ReadOnlySpan<byte> dek = default)
    {
        if (enc.Length != 16)
        {
            throw new ArgumentException("ENC key must be 16 bytes", nameof(enc));
        }
        if (mac.Length != 16)
        {
            throw new ArgumentException("MAC key must be 16 bytes", nameof(mac));
        }
        if (dek.Length != 0 && dek.Length != 16)
        {
            throw new ArgumentException("DEK must be 16 bytes if provided", nameof(dek));
        }

        _enc = enc.ToArray();
        _mac = mac.ToArray();
        _dek = dek.Length == 16 ? dek.ToArray() : null;
    }

    /// <summary>
    /// Returns the default SCP03 key set.
    /// </summary>
    /// <returns>A <see cref="StaticKeys"/> instance with default keys.</returns>
    public static StaticKeys GetDefaultKeys() => new(DefaultKeyBytes, DefaultKeyBytes, DefaultKeyBytes);

    /// <summary>
    /// Derives session keys from the static keys using the provided context.
    /// </summary>
    /// <param name="context">The 16-byte context (host challenge || card challenge).</param>
    /// <returns>The derived session keys.</returns>
    /// <exception cref="ArgumentException">Thrown if context is not 16 bytes.</exception>
    public SessionKeys Derive(ReadOnlySpan<byte> context)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (context.Length != 16)
        {
            throw new ArgumentException("Context must be 16 bytes", nameof(context));
        }

        Span<byte> senc = stackalloc byte[16];
        Span<byte> smac = stackalloc byte[16];
        Span<byte> srmac = stackalloc byte[16];
        Span<byte> dek = stackalloc byte[16];

        // Derive S-ENC from ENC
        DeriveKey(_enc, 0x04, context, 128, senc);

        // Derive S-MAC and S-RMAC from MAC
        DeriveKey(_mac, 0x06, context, 128, smac);
        DeriveKey(_mac, 0x07, context, 128, srmac);

        // Derive DEK if available
        bool hasDek = false;
        if (_dek != null)
        {
            DeriveKey(_dek, 0x04, context, 128, dek);
            hasDek = true;
        }

        try
        {
            return hasDek
                ? new SessionKeys(senc, smac, srmac, dek)
                : new SessionKeys(senc, smac, srmac);
        }
        finally
        {
            // Zero the stack-allocated key material
            CryptographicOperations.ZeroMemory(senc);
            CryptographicOperations.ZeroMemory(smac);
            CryptographicOperations.ZeroMemory(srmac);
            if (hasDek)
            {
                CryptographicOperations.ZeroMemory(dek);
            }
        }
    }

    /// <summary>
    /// Derives a key using AES-CMAC according to SCP03 specification.
    /// </summary>
    /// <param name="key">The base key.</param>
    /// <param name="derivationType">The derivation type byte.</param>
    /// <param name="context">The derivation context.</param>
    /// <param name="lengthBits">The desired key length in bits.</param>
    /// <param name="output">The output buffer (must be at least lengthBits/8 bytes).</param>
    internal static void DeriveKey(ReadOnlySpan<byte> key, byte derivationType, ReadOnlySpan<byte> context, short lengthBits, Span<byte> output)
    {
        if (output.Length < lengthBits / 8)
        {
            throw new ArgumentException("Output buffer too small", nameof(output));
        }

        // Build derivation data: 11 zero bytes || derivationType || length(2 bytes BE) || 0x01 || context
        Span<byte> derivationData = stackalloc byte[11 + 1 + 2 + 1 + context.Length];
        derivationData[..11].Clear(); // 11 zero bytes
        derivationData[11] = derivationType;
        BinaryPrimitives.WriteInt16BigEndian(derivationData[12..14], lengthBits);
        derivationData[14] = 0x01;
        context.CopyTo(derivationData[15..]);

        using var cmac = new AesCmac(key);
        cmac.AppendData(derivationData);
        byte[] mac = cmac.GetHashAndReset();

        try
        {
            mac.AsSpan(0, lengthBits / 8).CopyTo(output);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mac);
        }
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

        if (_enc != null)
        {
            CryptographicOperations.ZeroMemory(_enc);
            _enc = null;
        }

        if (_mac != null)
        {
            CryptographicOperations.ZeroMemory(_mac);
            _mac = null;
        }

        if (_dek != null)
        {
            CryptographicOperations.ZeroMemory(_dek);
            _dek = null;
        }

        _disposed = true;
    }
}
