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
    public void CompareTo_DeviceIdTakesPrecedenceOverRawIdAndDisplay()
    {
        var alpha = new Credential("a", "Zulu:user"u8.ToArray(), "Zulu", "user", OathType.Totp, 30, null);
        var beta = new Credential("b", "Alpha:user"u8.ToArray(), "Alpha", "user", OathType.Totp, 30, null);

        Assert.True(alpha.CompareTo(beta) < 0);
        Assert.True(beta.CompareTo(alpha) > 0);
    }

    [Fact]
    public void CompareTo_RawIdsUseOrdinalRatherThanDisplayCase()
    {
        var withIssuer = new Credential("d", "Zebra:a"u8.ToArray(), "Zebra", "a", OathType.Totp, 30, null);
        var noIssuer = new Credential("d", "alpha"u8.ToArray(), null, "alpha", OathType.Totp, 30, null);

        // Raw UTF-8 bytes are compared without case folding: uppercase Z precedes lowercase a.
        Assert.True(withIssuer.CompareTo(noIssuer) < 0);
        Assert.True(noIssuer.CompareTo(withIssuer) > 0);
    }

    [Fact]
    public void CompareTo_SameDevice_SortsByRawIdNotDisplayMetadata()
    {
        var a = new Credential("d", "GitHub:alice"u8.ToArray(), "GitHub", "bob", OathType.Totp, 30, null);
        var b = new Credential("d", "GitHub:bob"u8.ToArray(), "GitHub", "alice", OathType.Totp, 30, null);

        Assert.True(a.CompareTo(b) < 0);
    }

    [Fact]
    public void ExplicitDisplaySorting_OrdersByIssuerOrNameThenName_RetainsDistinctIdentities()
    {
        var alpha = new Credential("z", "alpha"u8.ToArray(), null, "alpha", OathType.Totp, 30, null);
        var alice = new Credential("y", "GitHub:alice"u8.ToArray(), "GitHub", "alice", OathType.Totp, 30, null);
        var bob = new Credential("x", "GitHub:bob"u8.ToArray(), "GitHub", "bob", OathType.Totp, 30, null);
        var otherBob = new Credential("w", "GitHub:bob"u8.ToArray(), "github", "BOB", OathType.Totp, 30, null);
        var zebra = new Credential("v", "Zebra:a"u8.ToArray(), "Zebra", "a", OathType.Totp, 30, null);
        Credential[] credentials = [zebra, bob, alice, otherBob, alpha];

        var displayOrder = credentials
            .OrderBy(credential => credential.Issuer ?? credential.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(credential => credential.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(credential => credential);

        Assert.Equal([alpha, alice, otherBob, bob, zebra], displayOrder);
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

        Assert.False(cred is null);
        Assert.False(null == cred);
    }
}