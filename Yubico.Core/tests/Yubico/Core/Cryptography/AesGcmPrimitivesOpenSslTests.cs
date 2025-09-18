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

using System;
using System.Security.Cryptography;
using Xunit;

namespace Yubico.Core.Cryptography;

public class AesGcmPrimitivesOpenSslTests
{
    [Fact]
    public void Instantiate_Succeeds()
    {
        var aesObj = AesGcmPrimitives.Create();
        Assert.NotNull(aesObj);
    }

    [Fact]
    public void Encrypt_Decrypt_Succeeds()
    {
        var keyData = GetKeyData(null);
        var nonce = GetNonce(null);
        var plaintext = GetPlaintext(null);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        var associatedData = GetAssociatedData(plaintext.Length);
        var encryptedData = GetEncryptedData();
        var authTag = GetAuthTag();

        var aesObj = AesGcmPrimitives.Create();
        aesObj.EncryptAndAuthenticate(keyData, nonce, plaintext, ciphertext, tag, associatedData);

        var isValid = encryptedData.AsSpan().SequenceEqual(ciphertext.AsSpan());
        Assert.True(isValid);
        isValid = authTag.AsSpan().SequenceEqual(tag.AsSpan());
        Assert.True(isValid);

        var decryptedData = new byte[ciphertext.Length];
        var isVerified = aesObj.DecryptAndVerify(keyData, nonce, ciphertext, tag, decryptedData, associatedData);
        Assert.True(isVerified);

        isValid = plaintext.AsSpan().SequenceEqual(decryptedData.AsSpan());
        Assert.True(isValid);
    }

    [SkippableFact(typeof(PlatformNotSupportedException))]
    public void Encrypt_Decrypt_Succeeds_RandomValues_Succeed()
    {
        var random = RandomNumberGenerator.Create();
        var keyData = GetKeyData(random);
        var nonce = GetNonce(random);
        var plaintext = GetPlaintext(random, 50);
        var ciphertext = new byte[plaintext.Length];
        var ciphertextS = new byte[plaintext.Length];
        var tag = new byte[16];
        var tagS = new byte[16];
        var associatedData = GetAssociatedData(plaintext.Length);

        var aesObj = AesGcmPrimitives.Create();
        aesObj.EncryptAndAuthenticate(keyData, nonce, plaintext, ciphertext, tag, associatedData);

        var aesGcm = new AesGcm(keyData, tag.Length);
        aesGcm.Encrypt(nonce, plaintext, ciphertextS, tagS, associatedData);

        var isValid = ciphertextS.AsSpan().SequenceEqual(ciphertext.AsSpan());
        Assert.True(isValid);
        isValid = tagS.AsSpan().SequenceEqual(tag.AsSpan());
        Assert.True(isValid);

        var decryptedData = new byte[ciphertext.Length];
        var isVerified = aesObj.DecryptAndVerify(keyData, nonce, ciphertext, tag, decryptedData, associatedData);
        Assert.True(isVerified);

        var decryptedDataS = new byte[ciphertextS.Length];
        aesGcm.Decrypt(nonce, ciphertextS, tag, decryptedDataS, associatedData);

        isValid = decryptedDataS.AsSpan().SequenceEqual(decryptedData.AsSpan());
        Assert.True(isValid);
        isValid = plaintext.AsSpan().SequenceEqual(decryptedData.AsSpan());
        Assert.True(isValid);
    }

    private static byte[] GetKeyData(
        RandomNumberGenerator? random)
    {
        var keyBytes = new byte[]
        {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
        };

        random?.GetBytes(keyBytes);

        return keyBytes;
    }

    private static byte[] GetNonce(
        RandomNumberGenerator? random)
    {
        var nonceBytes = new byte[]
        {
            0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            0x41, 0x42, 0x43, 0x44
        };

        random?.GetBytes(nonceBytes);

        return nonceBytes;
    }

    private static byte[] GetPlaintext(
        RandomNumberGenerator? random,
        int dataLength = 18)
    {
        byte[] dataToEncrypt;
        if (dataLength == 18)
        {
            dataToEncrypt = new byte[]
            {
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                0x51, 0x52
            };
        }
        else
        {
            dataToEncrypt = new byte[dataLength];
        }

        random?.GetBytes(dataToEncrypt);

        return dataToEncrypt;
    }

    private static byte[] GetAssociatedData(
        int originalSize)
    {
        var associatedData = new byte[]
        {
            0x62, 0x6c, 0x6f, 0x62,
            (byte)originalSize,
            (byte)(originalSize >> 8),
            (byte)(originalSize >> 16),
            (byte)(originalSize >> 24), 0, 0, 0, 0
        };

        return associatedData;
    }

    private static byte[] GetEncryptedData()
    {
        var encryptedData = new byte[]
        {
            0xea, 0x6a, 0x01, 0x13, 0x8d, 0x78, 0xa6, 0xa7,
            0xec, 0x57, 0x91, 0x13, 0xbe, 0xe1, 0xcd, 0x75,
            0xba, 0x87
        };

        return encryptedData;
    }

    private static byte[] GetAuthTag()
    {
        var authTag = new byte[]
        {
            0xba, 0x13, 0x8f, 0x68, 0xaf, 0xc7, 0xff, 0x26,
            0x5f, 0x75, 0x25, 0xb2, 0xcc, 0xe9, 0x6b, 0xae
        };

        return authTag;
    }
}
