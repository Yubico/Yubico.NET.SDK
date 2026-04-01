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

namespace Yubico.YubiKit.Oath.UnitTests;

public class CredentialTests
{
    [Fact]
    public void Equals_SameDeviceIdAndId_ReturnsTrue()
    {
        var cred1 = new Credential("device1", "GitHub:user"u8.ToArray(), "GitHub", "user", OathType.Totp, 30, false);
        var cred2 = new Credential("device1", "GitHub:user"u8.ToArray(), "GitHub", "user", OathType.Totp, 30, true);

        Assert.Equal(cred1, cred2);
    }

    [Fact]
    public void Equals_DifferentDeviceId_ReturnsFalse()
    {
        var cred1 = new Credential("device1", "GitHub:user"u8.ToArray(), "GitHub", "user", OathType.Totp, 30, false);
        var cred2 = new Credential("device2", "GitHub:user"u8.ToArray(), "GitHub", "user", OathType.Totp, 30, false);

        Assert.NotEqual(cred1, cred2);
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        var cred1 = new Credential("device1", "GitHub:user1"u8.ToArray(), "GitHub", "user1", OathType.Totp, 30, false);
        var cred2 = new Credential("device1", "GitHub:user2"u8.ToArray(), "GitHub", "user2", OathType.Totp, 30, false);

        Assert.NotEqual(cred1, cred2);
    }

    [Fact]
    public void GetHashCode_SameDeviceIdAndId_ReturnsSameHash()
    {
        var cred1 = new Credential("device1", "GitHub:user"u8.ToArray(), "GitHub", "user", OathType.Totp, 30, false);
        var cred2 = new Credential("device1", "GitHub:user"u8.ToArray(), "GitHub", "user", OathType.Totp, 30, true);

        Assert.Equal(cred1.GetHashCode(), cred2.GetHashCode());
    }

    [Fact]
    public void Credential_CanBeUsedAsDictionaryKey()
    {
        var cred = new Credential("device1", "GitHub:user"u8.ToArray(), "GitHub", "user", OathType.Totp, 30, false);
        var dict = new Dictionary<Credential, string> { [cred] = "test" };

        var lookupCred = new Credential("device1", "GitHub:user"u8.ToArray(), "GitHub", "user", OathType.Totp, 30, true);

        Assert.True(dict.ContainsKey(lookupCred));
        Assert.Equal("test", dict[lookupCred]);
    }

    [Fact]
    public void CompareTo_IssuerBasedSorting_OrdersCorrectly()
    {
        var alpha = new Credential("d", "Alpha:user"u8.ToArray(), "Alpha", "user", OathType.Totp, 30, null);
        var beta = new Credential("d", "Beta:user"u8.ToArray(), "Beta", "user", OathType.Totp, 30, null);

        Assert.True(alpha.CompareTo(beta) < 0);
        Assert.True(beta.CompareTo(alpha) > 0);
    }

    [Fact]
    public void CompareTo_NoIssuerUsesNameForSortKey()
    {
        var withIssuer = new Credential("d", "Zebra:a"u8.ToArray(), "Zebra", "a", OathType.Totp, 30, null);
        var noIssuer = new Credential("d", "alpha"u8.ToArray(), null, "alpha", OathType.Totp, 30, null);

        // withIssuer sorts by "zebra" (issuer), noIssuer sorts by "alpha" (name)
        Assert.True(noIssuer.CompareTo(withIssuer) < 0);
    }

    [Fact]
    public void CompareTo_SameIssuer_SortsByName()
    {
        var a = new Credential("d", "GitHub:alice"u8.ToArray(), "GitHub", "alice", OathType.Totp, 30, null);
        var b = new Credential("d", "GitHub:bob"u8.ToArray(), "GitHub", "bob", OathType.Totp, 30, null);

        Assert.True(a.CompareTo(b) < 0);
    }

    [Fact]
    public void OperatorEquals_BothNull_ReturnsTrue()
    {
        Credential? a = null;
        Credential? b = null;

        Assert.True(a == b);
    }

    [Fact]
    public void OperatorEquals_OneNull_ReturnsFalse()
    {
        var cred = new Credential("d", "user"u8.ToArray(), null, "user", OathType.Totp, 30, null);

        Assert.False(cred == null);
        Assert.False(null == cred);
    }
}