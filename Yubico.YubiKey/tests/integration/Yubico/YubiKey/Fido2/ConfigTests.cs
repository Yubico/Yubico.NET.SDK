// Copyright 2023 Yubico AB
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
using System.Collections.Generic;
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    public class ConfigTests : SimpleIntegrationTestConnection
    {
        public ConfigTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Bio)
        {
        }

        [Fact]
        public void EnableEnterpriseAttestation_Succeeds()
        {
            using (var fido2Session = new Fido2Session(Device))
            {
                fido2Session.KeyCollector = LocalKeyCollector;

                OptionValue optionValue = fido2Session.AuthenticatorInfo.GetOptionValue("ep");

                bool expectedResult = false;
                if ((optionValue == OptionValue.True) || (optionValue == OptionValue.False))
                {
                    expectedResult = true;
                }

                bool isSet = fido2Session.TryEnableEnterpriseAttestation();

                Assert.Equal(expectedResult, isSet);
            }
        }

        [Fact]
        public void ToggleAlwaysUv_Succeeds()
        {
            using (var fido2Session = new Fido2Session(Device))
            {
                fido2Session.KeyCollector = LocalKeyCollector;

                OptionValue optionValue = fido2Session.AuthenticatorInfo.GetOptionValue("alwaysUv");

                bool expectedResult = false;
                OptionValue expectedValue = optionValue switch
                {
                    OptionValue.True => OptionValue.False,
                    OptionValue.False => OptionValue.True,
                    _ => OptionValue.NotSupported,
                };

                if (expectedValue != OptionValue.NotSupported)
                {
                    expectedResult = true;
                }

                bool isSet = fido2Session.TryToggleAlwaysUv();

                optionValue = fido2Session.AuthenticatorInfo.GetOptionValue("alwaysUv");
                Assert.Equal(expectedResult, isSet);
                Assert.Equal(expectedValue, optionValue);
            }
        }

        private bool LocalKeyCollector(KeyEntryData arg)
        {
            switch (arg.Request)
            {
                case KeyEntryRequest.VerifyFido2Pin:
                    arg.SubmitValue(Encoding.UTF8.GetBytes("123456"));
                    break;
                case KeyEntryRequest.VerifyFido2Uv:
                    Console.WriteLine("Fingerprint requested.");
                    break;
                case KeyEntryRequest.Release:
                    break;
                default:
                    throw new NotSupportedException("Not supported by this test");
            }

            return true;
        }
    }
}
