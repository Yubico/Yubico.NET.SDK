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
using System.Text;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2;

[Trait(TraitTypes.Category, TestCategories.Elevated)]
[Trait(TraitTypes.Category, TestCategories.RequiresSetup)] // Requires pin 123456 and one FIDO credential set up
public class SimpleCredMgmtTests : SimpleIntegrationTestConnection
{
    public SimpleCredMgmtTests()
        : base(YubiKeyApplication.Fido2)
    {
    }

    [Fact]
    public void GetMetadata_Succeeds() // Works when at least one credential is set up
    {
        using var fido2Session = new Fido2Session(Device);
        fido2Session.KeyCollector = LocalKeyCollector;

        var (credCount, slotCount) = fido2Session.GetCredentialMetadata();

        Assert.InRange(credCount, 1, int.MaxValue);
        Assert.InRange(slotCount, 1, int.MaxValue);
    }

    [Fact]
    public void EnumerateRps_Succeeds() // Works when at least one credential is set up
    {
        using var fido2Session = new Fido2Session(Device);
        fido2Session.KeyCollector = LocalKeyCollector;

        var rpList = fido2Session.EnumerateRelyingParties();

        Assert.Single(rpList);
    }

    [Fact]
    public void EnumerateCreds_Succeeds() // Works when at least one credential is set up
    {
        using var fido2Session = new Fido2Session(Device);
        fido2Session.KeyCollector = LocalKeyCollector;

        var rpList = fido2Session.EnumerateRelyingParties();
        var ykCredList = fido2Session.EnumerateCredentialsForRelyingParty(rpList[0]);

        Assert.NotEmpty(ykCredList);
    }

    [Fact]
    public void DeleteCred_Succeeds() // Works when at least one credential is set up
    {
        using var fido2Session = new Fido2Session(Device);
        fido2Session.KeyCollector = LocalKeyCollector;

        var rpList = fido2Session.EnumerateRelyingParties();
        var credList = fido2Session.EnumerateCredentialsForRelyingParty(rpList[0]);
        var count = credList.Count;

        fido2Session.ClearAuthToken();
        fido2Session.DeleteCredential(credList[0].CredentialId);
        credList = fido2Session.EnumerateCredentialsForRelyingParty(rpList[0]);

        Assert.NotNull(credList);
        Assert.True(credList.Count == count - 1);
    }

    private bool LocalKeyCollector(
        KeyEntryData arg)
    {
        switch (arg.Request)
        {
            case KeyEntryRequest.VerifyFido2Pin:
                arg.SubmitValue(Encoding.UTF8.GetBytes("11234567"));
                break;
            case KeyEntryRequest.VerifyFido2Uv:
                Console.WriteLine("Fingerprint requested.");
                break;
            case KeyEntryRequest.TouchRequest:
                Console.WriteLine("Touch requested.");
                break;
            case KeyEntryRequest.Release:
                break;
            default:
                throw new NotSupportedException("Not supported by this test");
        }

        return true;
    }
}
