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
    [Theory]
    // hashlib.pbkdf2_hmac('sha256', b'password', b'Yubico', 10000, 32).hex()
    [InlineData("password", "090b47dbed595654901dee1cc655e420592fd483f759e29909a04c4505d2ce0a")]
    // hashlib.pbkdf2_hmac('sha256', 'páss🔑'.encode('utf-8'), b'Yubico', 10000, 32).hex()
    [InlineData("páss🔑", "3c07639daab7689bfcc41b29717cc6d4e065cf5fd980c827d4615aa2ad88c131")]
    public void DeriveKeys_KnownVector_MatchesExpected(string password, string expectedHex)
    {
        var expected = Convert.FromHexString(expectedHex);
        var result = HsmAuthSession.DeriveKeys(Encoding.UTF8.GetBytes(password));

        Assert.Equal(32, result.Length);
        Assert.True(CryptographicOperations.FixedTimeEquals(expected, result));
    }

    [Fact]
    public void DeriveKeys_SplitsIntoEncAndMacKeys()
    {
        var result = HsmAuthSession.DeriveKeys("password"u8.ToArray());

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
        var result1 = HsmAuthSession.DeriveKeys("test-password"u8.ToArray());
        var result2 = HsmAuthSession.DeriveKeys("test-password"u8.ToArray());

        Assert.True(CryptographicOperations.FixedTimeEquals(result1, result2));
    }

    [Fact]
    public void DeriveKeys_DifferentPasswords_ProduceDifferentResults()
    {
        var result1 = HsmAuthSession.DeriveKeys("password1"u8.ToArray());
        var result2 = HsmAuthSession.DeriveKeys("password2"u8.ToArray());

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