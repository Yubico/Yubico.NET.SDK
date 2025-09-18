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
using System.Collections.Generic;
using System.Text;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2;

[Trait(TraitTypes.Category, TestCategories.Elevated)]
public class MinPinLenTests : SimpleIntegrationTestConnection
{
    private static readonly byte[] _clientDataHash =
    {
        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
    };

    private static readonly RelyingParty _rp = new("rp.minpin.length")
    {
        Name = "RP MinPinLen"
    };

    private static readonly UserEntity _userEntity = new(new byte[] { 1, 2, 3, 4, 5 })
    {
        Name = "TestUser",
        DisplayName = "Test User"
    };

    public MinPinLenTests()
        : base(YubiKeyApplication.Fido2)
    {
    }

    [Fact]
    public void GetMinPinFromCredential_Succeeds()
    {
        using (var fido2Session = new Fido2Session(Device))
        {
            fido2Session.KeyCollector = LocalKeyCollector;
            var isSupported = fido2Session.AuthenticatorInfo.IsExtensionSupported("minPinLength");
            var ovMinPin = fido2Session.AuthenticatorInfo.GetOptionValue("setMinPINLength");
            var ovCredMgmt = fido2Session.AuthenticatorInfo.GetOptionValue(
                AuthenticatorOptions.credMgmt);
            if (ovMinPin != OptionValue.True || ovCredMgmt != OptionValue.True || !isSupported)
            {
                return;
            }

            DeleteAddedCredential(fido2Session);

            var isValid = AddCredential(fido2Session, out var mcData);
            Assert.True(isValid);
            Assert.NotNull(mcData);

            var minPinLen = mcData!.AuthenticatorData.GetMinPinLengthExtension();

            _ = Assert.NotNull(minPinLen);

            DeleteAddedCredential(fido2Session);
        }
    }

    private bool AddCredential(
        Fido2Session fido2Session,
        out MakeCredentialData? mcData)
    {
        mcData = null;

        var rpList = new List<string>(1)
        {
            _rp.Id
        };
        var isSet = fido2Session.TrySetPinConfig(null, rpList);
        if (!isSet)
        {
            return false;
        }

        var mcParams = new MakeCredentialParameters(_rp, _userEntity)
        {
            ClientDataHash = _clientDataHash
        };
        mcParams.AddOption(AuthenticatorOptions.rk, true);
        mcParams.AddMinPinLengthExtension(fido2Session.AuthenticatorInfo);

        mcData = fido2Session.MakeCredential(mcParams);

        return true;
    }

    private void DeleteAddedCredential(
        Fido2Session fido2Session)
    {
        var rpList = fido2Session.EnumerateRelyingParties();
        foreach (var rp in rpList)
        {
            if (string.Compare(rp.Id, _rp.Id) == 0)
            {
                var credList =
                    fido2Session.EnumerateCredentialsForRelyingParty(rp);

                foreach (var credInfo in credList)
                {
                    fido2Session.DeleteCredential(credInfo.CredentialId);
                }
            }
        }
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
