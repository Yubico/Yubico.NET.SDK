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

namespace Yubico.YubiKit.YubiHsm.UnitTests;

public class Pbkdf2DerivationTests
{
    [Fact]
    public void DeriveKeys_KnownVector_MatchesExpected()
    {
        // Independently compute PBKDF2-HMAC-SHA256("password", "Yubico", 10000, 32)
        var expected = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes("password"),
            "Yubico"u8,
            10_000,
            HashAlgorithmName.SHA256,
            32);

        var result = HsmAuthSession.DeriveKeys("password");

        Assert.Equal(32, result.Length);
        Assert.True(CryptographicOperations.FixedTimeEquals(expected, result));
    }

    [Fact]
    public void DeriveKeys_SplitsIntoEncAndMacKeys()
    {
        var result = HsmAuthSession.DeriveKeys("password");

        // First 16 bytes = K-ENC, last 16 bytes = K-MAC
        var keyEnc = result.AsSpan(0, 16);
        var keyMac = result.AsSpan(16, 16);

        // Keys should not be all zeros
        Assert.False(keyEnc.SequenceEqual(new byte[16]));
        Assert.False(keyMac.SequenceEqual(new byte[16]));

        // Keys should be different from each other
        Assert.False(keyEnc.SequenceEqual(keyMac));
    }

    [Fact]
    public void DeriveKeys_SamePassword_ProducesSameResult()
    {
        var result1 = HsmAuthSession.DeriveKeys("test-password");
        var result2 = HsmAuthSession.DeriveKeys("test-password");

        Assert.True(CryptographicOperations.FixedTimeEquals(result1, result2));
    }

    [Fact]
    public void DeriveKeys_DifferentPasswords_ProduceDifferentResults()
    {
        var result1 = HsmAuthSession.DeriveKeys("password1");
        var result2 = HsmAuthSession.DeriveKeys("password2");

        Assert.False(CryptographicOperations.FixedTimeEquals(result1, result2));
    }

    [Fact]
    public void DeriveKeys_UsesCorrectConstants()
    {
        Assert.Equal(10_000, HsmAuthSession.Pbkdf2Iterations);
        Assert.Equal(32, HsmAuthSession.Pbkdf2DerivedKeyLength);
        Assert.True(HsmAuthSession.Pbkdf2Salt.SequenceEqual("Yubico"u8));
    }
}
