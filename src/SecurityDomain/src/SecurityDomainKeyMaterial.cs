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

using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.SecurityDomain;

internal static class SecurityDomainKeyMaterial
{
    private const byte KeyTypeAes = 0x88;

    internal static void ValidateCheckSum(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
    {
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            throw new InvalidOperationException("Checksum validation failed");
    }

    internal static void EncodeKeyComponent(
        ReadOnlySpan<byte> key,
        DataEncryptor encryptor,
        ArrayBufferWriter<byte> buffer)
    {
        Span<byte> kcv = stackalloc byte[3];
        CalculateKcv(key, kcv);

        byte[]? keyBuffer = null;
        try
        {
            keyBuffer = ArrayPool<byte>.Shared.Rent(key.Length);
            key.CopyTo(keyBuffer);
            var encrypted = encryptor(keyBuffer.AsSpan(0, key.Length));

            using var keyTlv = new Tlv(KeyTypeAes, encrypted);
            buffer.Write(keyTlv.AsSpan());

            buffer.Write([(byte)kcv.Length]);
            buffer.Write(kcv);
        }
        finally
        {
            if (keyBuffer is not null)
            {
                CryptographicOperations.ZeroMemory(keyBuffer.AsSpan(0, key.Length));
                ArrayPool<byte>.Shared.Return(keyBuffer);
            }
        }
    }

    internal static void ValidateKcv(StaticKeys staticKeys, ReadOnlySpan<byte> response)
    {
        if (response.Length < 10)
            throw new InvalidOperationException("Response too short for KCV validation");

        Span<byte> expectedKcvEnc = stackalloc byte[3];
        Span<byte> expectedKcvMac = stackalloc byte[3];
        Span<byte> expectedKcvDek = stackalloc byte[3];

        CalculateKcv(staticKeys.Enc, expectedKcvEnc);
        CalculateKcv(staticKeys.Mac, expectedKcvMac);
        CalculateKcv(staticKeys.Dek, expectedKcvDek);

        var actualKcvEnc = response.Slice(1, 3);
        var actualKcvMac = response.Slice(4, 3);
        var actualKcvDek = response.Slice(7, 3);

        if (!CryptographicOperations.FixedTimeEquals(expectedKcvEnc, actualKcvEnc))
            throw new InvalidOperationException("ENC key check value mismatch");

        if (!CryptographicOperations.FixedTimeEquals(expectedKcvMac, actualKcvMac))
            throw new InvalidOperationException("MAC key check value mismatch");

        if (!CryptographicOperations.FixedTimeEquals(expectedKcvDek, actualKcvDek))
            throw new InvalidOperationException("DEK key check value mismatch");
    }

    private static void CalculateKcv(ReadOnlySpan<byte> key, Span<byte> output)
    {
        using var aes = Aes.Create();

        aes.SetKey(key);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        Span<byte> kcvInput = stackalloc byte[16];
        kcvInput.Fill(0x01);

        Span<byte> iv = stackalloc byte[16];
        iv.Clear();

        Span<byte> encrypted = stackalloc byte[16];
        aes.EncryptCbc(kcvInput, iv, encrypted, PaddingMode.None);

        encrypted[..3].CopyTo(output);
    }
}