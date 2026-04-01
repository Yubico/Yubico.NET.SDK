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

namespace Yubico.YubiKit.YubiHsm.UnitTests;

public class HsmAuthCredentialTests
{
    [Fact]
    public void CompareTo_CaseInsensitive_OrdersCorrectly()
    {
        var alpha = new HsmAuthCredential("Alpha", HsmAuthAlgorithm.Aes128YubicoAuthentication, 0, false);
        var beta = new HsmAuthCredential("beta", HsmAuthAlgorithm.Aes128YubicoAuthentication, 1, true);
        var charlie = new HsmAuthCredential("CHARLIE", HsmAuthAlgorithm.EcP256YubicoAuthentication, 2, null);

        Assert.True(alpha.CompareTo(beta) < 0);
        Assert.True(beta.CompareTo(charlie) < 0);
        Assert.True(charlie.CompareTo(alpha) > 0);
    }

    [Fact]
    public void CompareTo_SameLabelDifferentCase_AreEqual()
    {
        var lower = new HsmAuthCredential("test", HsmAuthAlgorithm.Aes128YubicoAuthentication, 0, false);
        var upper = new HsmAuthCredential("TEST", HsmAuthAlgorithm.Aes128YubicoAuthentication, 0, false);

        Assert.Equal(0, lower.CompareTo(upper));
    }

    [Fact]
    public void CompareTo_Null_ReturnsPositive()
    {
        var cred = new HsmAuthCredential("test", HsmAuthAlgorithm.Aes128YubicoAuthentication, 0, false);

        Assert.True(cred.CompareTo(null) > 0);
    }

    [Fact]
    public void Sort_CredentialList_OrdersCaseInsensitively()
    {
        var creds = new List<HsmAuthCredential>
        {
            new("charlie", HsmAuthAlgorithm.Aes128YubicoAuthentication, 0, false),
            new("Alpha", HsmAuthAlgorithm.Aes128YubicoAuthentication, 0, false),
            new("BETA", HsmAuthAlgorithm.EcP256YubicoAuthentication, 0, true)
        };

        creds.Sort();

        Assert.Equal("Alpha", creds[0].Label);
        Assert.Equal("BETA", creds[1].Label);
        Assert.Equal("charlie", creds[2].Label);
    }

    [Fact]
    public void Record_Equality_BasedOnAllProperties()
    {
        var a = new HsmAuthCredential("test", HsmAuthAlgorithm.Aes128YubicoAuthentication, 5, true);
        var b = new HsmAuthCredential("test", HsmAuthAlgorithm.Aes128YubicoAuthentication, 5, true);
        var c = new HsmAuthCredential("test", HsmAuthAlgorithm.Aes128YubicoAuthentication, 6, true);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
