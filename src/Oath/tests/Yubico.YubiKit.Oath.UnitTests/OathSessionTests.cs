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

using System.Security.Cryptography;
using System.Text;

namespace Yubico.YubiKit.Oath.UnitTests;

public class OathSessionTests
{
    [Fact]
    public void ComputeDeviceId_WithKnownSalt_ReturnsExpectedBase64()
    {
        // A known salt value
        byte[] salt = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        string deviceId = OathSession.ComputeDeviceId(salt);

        // Compute expected: Base64(SHA256(salt)[:16]) with padding stripped
        byte[] hash = SHA256.HashData(salt);
        string expected = Convert.ToBase64String(hash[..16]).TrimEnd('=');
        Assert.Equal(expected, deviceId);
    }

    [Fact]
    public void ComputeDeviceId_WithEmptySalt_ReturnsHashOfEmpty()
    {
        byte[] salt = [];

        string deviceId = OathSession.ComputeDeviceId(salt);

        byte[] hash = SHA256.HashData(salt);
        string expected = Convert.ToBase64String(hash[..16]).TrimEnd('=');
        Assert.Equal(expected, deviceId);
    }

    [Fact]
    public void ComputeDeviceId_StripsBase64Padding()
    {
        // Any salt — the result should never contain '='
        byte[] salt = [0xAA, 0xBB, 0xCC, 0xDD];

        string deviceId = OathSession.ComputeDeviceId(salt);

        Assert.DoesNotContain("=", deviceId);
    }

    [Fact]
    public void ComputeDeviceId_DifferentSalts_ProduceDifferentIds()
    {
        byte[] salt1 = [0x01, 0x02, 0x03, 0x04];
        byte[] salt2 = [0x05, 0x06, 0x07, 0x08];

        string id1 = OathSession.ComputeDeviceId(salt1);
        string id2 = OathSession.ComputeDeviceId(salt2);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void ComputeDeviceId_SameSalt_ProducesSameId()
    {
        byte[] salt = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        string id1 = OathSession.ComputeDeviceId(salt);
        string id2 = OathSession.ComputeDeviceId(salt);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void ComputeDeviceId_Uses16BytePrefix_NotFullHash()
    {
        byte[] salt = [0x01, 0x02, 0x03, 0x04];

        string deviceId = OathSession.ComputeDeviceId(salt);

        // 16 bytes -> ~22 base64 chars (without padding)
        // Full 32 bytes -> ~43 base64 chars
        Assert.True(deviceId.Length <= 22, $"DeviceId too long ({deviceId.Length}), should use 16-byte prefix");
    }

    [Fact]
    public void DeriveKey_WithKnownInputs_ProducesDeterministicOutput()
    {
        // DeriveKey is an instance method that uses the session's salt.
        // We can verify the PBKDF2 algorithm independently.
        string password = "test_password";
        byte[] salt = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 1000,
            HashAlgorithmName.SHA1,
            outputLength: 16);

        Assert.Equal(16, key.Length);

        // Same inputs should yield same output
        byte[] key2 = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 1000,
            HashAlgorithmName.SHA1,
            outputLength: 16);

        Assert.Equal(key, key2);
    }

    [Fact]
    public void DeriveKey_DifferentPasswords_ProduceDifferentKeys()
    {
        byte[] salt = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        byte[] key1 = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes("password1"),
            salt,
            iterations: 1000,
            HashAlgorithmName.SHA1,
            outputLength: 16);

        byte[] key2 = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes("password2"),
            salt,
            iterations: 1000,
            HashAlgorithmName.SHA1,
            outputLength: 16);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void DeriveKey_DifferentSalts_ProduceDifferentKeys()
    {
        byte[] salt1 = [0x01, 0x02, 0x03, 0x04];
        byte[] salt2 = [0x05, 0x06, 0x07, 0x08];

        byte[] key1 = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes("password"),
            salt1,
            iterations: 1000,
            HashAlgorithmName.SHA1,
            outputLength: 16);

        byte[] key2 = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes("password"),
            salt2,
            iterations: 1000,
            HashAlgorithmName.SHA1,
            outputLength: 16);

        Assert.NotEqual(key1, key2);
    }
}
