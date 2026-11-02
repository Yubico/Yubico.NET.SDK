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

using System.Text;

namespace Yubico.YubiKit.Oath.UnitTests;

public class CredentialOrderingTests
{
    [Fact]
    public void SortedSet_SameDisplayAcrossDevices_RetainsBothIdentities()
    {
        var first = new Credential("device1", "GitHub:user"u8.ToArray(), "GitHub", "user", OathType.Totp, 30, null);
        var second = new Credential("device2", "GitHub:user"u8.ToArray(), "GitHub", "user", OathType.Totp, 30, null);
        var credentials = new SortedSet<Credential> { first };

        Assert.NotEqual(first, second);
        Assert.True(credentials.Add(second));
        Assert.Equal(2, credentials.Count);
        Assert.Contains(first, credentials);
        Assert.Contains(second, credentials);
    }

    [Theory]
    [InlineData("60/GitHub:user", "GitHub", "user", 60)]
    [InlineData("github:USER", "github", "USER", 30)]
    public void SortedSet_DifferentRawIdsWithSameDisplayOrder_RetainsBothIdentities(
        string rawId,
        string issuer,
        string name,
        int period)
    {
        var first = new Credential("device1", "GitHub:user"u8.ToArray(), "GitHub", "user", OathType.Totp, 30, null);
        var second = new Credential("device1", Encoding.UTF8.GetBytes(rawId), issuer, name, OathType.Totp, period, null);
        var credentials = new SortedSet<Credential> { first };

        Assert.NotEqual(first, second);
        Assert.True(credentials.Add(second));
        Assert.Equal(2, credentials.Count);
    }

    [Fact]
    public void SortedSet_EqualIdentityWithDifferentNonDisplayMetadata_Deduplicates()
    {
        var first = new Credential("device1", "GitHub:user"u8.ToArray(), "GitHub", "user", OathType.Totp, 30, null);
        var second = new Credential("device1", "GitHub:user"u8.ToArray(), "GitHub", "user", OathType.Hotp, 0, true);
        var credentials = new SortedSet<Credential> { first };

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal(0, first.CompareTo(second));
        Assert.Equal(0, second.CompareTo(first));
        Assert.False(credentials.Add(second));
        Assert.Single(credentials);
    }

    [Fact]
    public void SortedSet_EqualIdentityWithDifferentDisplayMetadata_Deduplicates()
    {
        var first = new Credential("device1", "same"u8.ToArray(), null, "Alpha", OathType.Totp, 30, null);
        var second = new Credential("device1", "same"u8.ToArray(), null, "Zulu", OathType.Totp, 30, null);
        var credentials = new SortedSet<Credential> { first };

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(credentials.Add(second));
        Assert.Single(credentials);
    }

    [Fact]
    public void CompareTo_EqualIdentityMustHaveSameRelationToThirdCredential()
    {
        var alpha = new Credential("device1", "same"u8.ToArray(), null, "Alpha", OathType.Totp, 30, null);
        var zulu = new Credential("device1", "same"u8.ToArray(), null, "Zulu", OathType.Totp, 30, null);
        var middle = new Credential("device1", "middle"u8.ToArray(), null, "Middle", OathType.Totp, 30, null);

        Assert.Equal(alpha, zulu);
        Assert.Equal(Math.Sign(alpha.CompareTo(middle)), Math.Sign(zulu.CompareTo(middle)));
    }

    [Fact]
    public void CompareTo_EqualIdentityParsedAsDifferentOathTypes_MustHaveSameRelationToThirdCredential()
    {
        byte[] id = "60/Zulu:user"u8.ToArray();
        var (totpIssuer, totpName, totpPeriod) = Credential.ParseCredentialId(id, OathType.Totp);
        var (hotpIssuer, hotpName, hotpPeriod) = Credential.ParseCredentialId(id, OathType.Hotp);
        var totp = new Credential("device1", id, totpIssuer, totpName, OathType.Totp, totpPeriod, null);
        var hotp = new Credential("device1", id, hotpIssuer, hotpName, OathType.Hotp, hotpPeriod, null);
        var middle = new Credential("device1", "Middle:user"u8.ToArray(), "Middle", "user", OathType.Totp, 30, null);

        Assert.Equal(totp, hotp);
        Assert.Equal(Math.Sign(totp.CompareTo(middle)), Math.Sign(hotp.CompareTo(middle)));
    }

    [Fact]
    public void CompareTo_DeviceIdsAreOrdinalAndCaseSensitive()
    {
        var upper = new Credential("Device", "same"u8.ToArray(), null, "same", OathType.Totp, 30, null);
        var lower = new Credential("device", "same"u8.ToArray(), null, "same", OathType.Totp, 30, null);

        Assert.NotEqual(upper, lower);
        Assert.True(upper.CompareTo(lower) < 0);
        Assert.True(lower.CompareTo(upper) > 0);
    }

    [Fact]
    public void SortedSet_ArbitraryRawIds_OrdersUnsignedBytesLexicographically()
    {
        Credential[] credentials =
        [
            new("device1", ReadOnlyMemory<byte>.Empty, null, "same", OathType.Totp, 30, null),
            new("device1", (byte[])[0x00], null, "same", OathType.Totp, 30, null),
            new("device1", (byte[])[0x00, 0x01], null, "same", OathType.Totp, 30, null),
            new("device1", (byte[])[0x01], null, "same", OathType.Totp, 30, null),
            new("device1", (byte[])[0x7F], null, "same", OathType.Totp, 30, null),
            new("device1", (byte[])[0x80], null, "same", OathType.Totp, 30, null),
            new("device1", (byte[])[0xFF], null, "same", OathType.Totp, 30, null)
        ];

        var sorted = new SortedSet<Credential>(credentials.Reverse());

        Assert.Equal(credentials, sorted);
        Assert.Equal(credentials.Length, sorted.Count);
    }

    [Fact]
    public void CompareTo_IdentityOrdering_IsReflexiveAntisymmetricAndConsistentWithEquality()
    {
        Credential[] credentials = CreateOrderingLawCredentials();

        foreach (var first in credentials)
        {
            Assert.Equal(0, first.CompareTo(first));
            Assert.True(first.CompareTo(null) > 0);
            foreach (var second in credentials)
            {
                Assert.Equal(first.Equals(second), first.CompareTo(second) == 0);
                Assert.Equal(-Math.Sign(second.CompareTo(first)), Math.Sign(first.CompareTo(second)));
            }
        }
    }

    [Fact]
    public void CompareTo_IdentityOrdering_IsTransitiveIncludingEqualIdentityRepresentatives()
    {
        Credential[] credentials = CreateOrderingLawCredentials();

        foreach (var first in credentials)
        {
            foreach (var second in credentials)
            {
                foreach (var third in credentials)
                {
                    if (first.CompareTo(second) <= 0 && second.CompareTo(third) <= 0)
                    {
                        Assert.True(first.CompareTo(third) <= 0);
                    }
                }
            }
        }
    }

    private static Credential[] CreateOrderingLawCredentials() =>
    [
        new("device1", "same"u8.ToArray(), null, "Alpha", OathType.Totp, 30, null),
        new("device1", "same"u8.ToArray(), "Zulu", "Zulu", OathType.Hotp, 0, true),
        new("device1", "middle"u8.ToArray(), null, "Middle", OathType.Totp, 30, false),
        new("Device1", "same"u8.ToArray(), null, "Alpha", OathType.Totp, 30, null),
        new("device2", "same"u8.ToArray(), null, "Alpha", OathType.Totp, 30, null),
        new("device1", "60/Zulu:user"u8.ToArray(), "Zulu", "user", OathType.Totp, 60, null),
        new("device1", "60/Zulu:user"u8.ToArray(), "60/Zulu", "user", OathType.Hotp, 0, null),
        new(string.Empty, ReadOnlyMemory<byte>.Empty, null, string.Empty, (OathType)0, -1, null),
        new("device1", (byte[])[0xFF], null, string.Empty, (OathType)0, -1, null)
    ];
}