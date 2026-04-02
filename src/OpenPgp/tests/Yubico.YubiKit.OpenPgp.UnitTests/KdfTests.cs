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

using System.Security.Cryptography;
using System.Text;

namespace Yubico.YubiKit.OpenPgp.UnitTests;

public class KdfTests
{
    [Fact]
    public void KdfNone_Process_ReturnsUtf8Bytes()
    {
        var kdf = new KdfNone();

        var result = kdf.Process(Pw.User, "123456");

        Assert.Equal("123456"u8.ToArray(), result);
    }

    [Fact]
    public void KdfNone_Algorithm_IsZero()
    {
        var kdf = new KdfNone();
        Assert.Equal(0, kdf.Algorithm);
    }

    [Fact]
    public void KdfNone_ToBytes_RoundTrips()
    {
        var kdf = new KdfNone();
        var bytes = kdf.ToBytes();
        var parsed = Kdf.Parse(bytes);

        Assert.IsType<KdfNone>(parsed);
    }

    [Fact]
    public void KdfIterSaltedS2k_Process_VerifyByteCountIteration()
    {
        // Test that iteration_count means total bytes fed to hash, not rounds.
        // Use a known salt and PIN, then verify the output matches manual computation.
        byte[] salt = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        var pin = "123456";
        var iterationCount = 100; // Small count for testing

        var kdf = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256,
            IterationCount = iterationCount,
            SaltUser = salt,
        };

        var result = kdf.Process(Pw.User, pin);

        // Manually compute expected value:
        // data = salt + "123456" (UTF-8) = 14 bytes
        // data_count = 100 / 14 = 7, trailing = 100 % 14 = 2
        // Feed data 7 times (98 bytes) + data[..2] (2 bytes) = 100 bytes total
        var pinBytes = Encoding.UTF8.GetBytes(pin);
        var data = new byte[salt.Length + pinBytes.Length]; // 14 bytes
        salt.CopyTo(data, 0);
        pinBytes.CopyTo(data, salt.Length);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        for (var i = 0; i < 7; i++) // data_count = 7
        {
            hash.AppendData(data);
        }

        hash.AppendData(data.AsSpan(0, 2)); // trailing = 2
        var expected = hash.GetHashAndReset();

        Assert.Equal(expected, result);
        Assert.Equal(32, result.Length); // SHA256 produces 32 bytes
    }

    [Fact]
    public void KdfIterSaltedS2k_Process_Sha512_ProducesCorrectLength()
    {
        byte[] salt = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        var kdf = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha512,
            IterationCount = 0x780000,
            SaltUser = salt,
        };

        var result = kdf.Process(Pw.User, "123456");

        Assert.Equal(64, result.Length); // SHA512 produces 64 bytes
    }

    [Fact]
    public void KdfIterSaltedS2k_Process_UsesCorrectSaltPerPw()
    {
        byte[] saltUser = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        byte[] saltAdmin = [0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18];

        var kdf = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256,
            IterationCount = 100,
            SaltUser = saltUser,
            SaltAdmin = saltAdmin,
        };

        var userResult = kdf.Process(Pw.User, "123456");
        var adminResult = kdf.Process(Pw.Admin, "123456");

        // Different salts must produce different outputs
        Assert.NotEqual(userResult, adminResult);
    }

    [Fact]
    public void KdfIterSaltedS2k_GetSalt_FallsBackToUserSalt()
    {
        byte[] saltUser = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        var kdf = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256,
            IterationCount = 100,
            SaltUser = saltUser,
            SaltReset = null,
            SaltAdmin = null,
        };

        // When SaltReset and SaltAdmin are null, falls back to SaltUser
        Assert.Equal(saltUser, kdf.GetSalt(Pw.Reset).ToArray());
        Assert.Equal(saltUser, kdf.GetSalt(Pw.Admin).ToArray());
    }

    [Fact]
    public void KdfIterSaltedS2k_ToBytes_RoundTrips()
    {
        byte[] saltUser = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        byte[] saltReset = [0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18];
        byte[] saltAdmin = [0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28];

        var kdf = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256,
            IterationCount = 0x780000,
            SaltUser = saltUser,
            SaltReset = saltReset,
            SaltAdmin = saltAdmin,
        };

        var bytes = kdf.ToBytes();
        var parsed = Kdf.Parse(bytes);

        var s2k = Assert.IsType<KdfIterSaltedS2k>(parsed);
        Assert.Equal(KdfHashAlgorithm.Sha256, s2k.HashAlgorithm);
        Assert.Equal(0x780000, s2k.IterationCount);
        Assert.Equal(saltUser, s2k.SaltUser.ToArray());
        Assert.Equal(saltReset, s2k.SaltReset?.ToArray());
        Assert.Equal(saltAdmin, s2k.SaltAdmin?.ToArray());
    }

    [Fact]
    public void KdfIterSaltedS2k_ToBytes_WithInitialHashes_RoundTrips()
    {
        byte[] saltUser = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        byte[] hashUser = new byte[32];
        byte[] hashAdmin = new byte[32];
        RandomNumberGenerator.Fill(hashUser);
        RandomNumberGenerator.Fill(hashAdmin);

        var kdf = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256,
            IterationCount = 0x780000,
            SaltUser = saltUser,
            InitialHashUser = hashUser,
            InitialHashAdmin = hashAdmin,
        };

        var bytes = kdf.ToBytes();
        var parsed = Kdf.Parse(bytes);

        var s2k = Assert.IsType<KdfIterSaltedS2k>(parsed);
        Assert.Equal(hashUser, s2k.InitialHashUser?.ToArray());
        Assert.Equal(hashAdmin, s2k.InitialHashAdmin?.ToArray());
    }

    [Fact]
    public void Kdf_Parse_EmptyOrUnknown_ReturnsKdfNone()
    {
        // Unknown algorithm ID
        var parsed = Kdf.Parse([0x81, 0x01, 0xFF]);
        Assert.IsType<KdfNone>(parsed);
    }

    [Fact]
    public void KdfIterSaltedS2k_Process_ExactMultipleOfDataLength()
    {
        // When iterationCount is exact multiple of data length, trailing = 0
        byte[] salt = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        var pin = "123456"; // data = salt(8) + pin(6) = 14 bytes
        var iterationCount = 14 * 5; // Exact multiple: 70

        var kdf = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256,
            IterationCount = iterationCount,
            SaltUser = salt,
        };

        var result = kdf.Process(Pw.User, pin);

        // Verify manually
        var data = new byte[14];
        salt.CopyTo(data, 0);
        Encoding.UTF8.GetBytes(pin).CopyTo(data, 8);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        for (var i = 0; i < 5; i++)
        {
            hash.AppendData(data);
        }

        Assert.Equal(hash.GetHashAndReset(), result);
    }
}
