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
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    [Trait(TraitTypes.Category, TestCategories.RequiresBio)]
    [Trait(TraitTypes.Category, TestCategories.Elevated)]
    public class VerifyFpTests : SimpleIntegrationTestConnection
    {
        public VerifyFpTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Fw5Bio)
        {
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void VerifyUv_Succeeds()
        {
            using (var fido2Session = new Fido2Session(Device))
            {
                fido2Session.KeyCollector = LocalKeyCollector;
                fido2Session.VerifyUv(PinUvAuthTokenPermissions.GetAssertion, "rp12");
                Assert.NotNull(fido2Session.AuthProtocol);
            }
        }

        private bool LocalKeyCollector(KeyEntryData arg)
        {
            switch (arg.Request)
            {
                case KeyEntryRequest.TouchRequest:
                    Console.WriteLine("YubiKey requires touch");
                    break;
                case KeyEntryRequest.VerifyFido2Pin:
                    arg.SubmitValue(Encoding.UTF8.GetBytes("123456"));
                    break;
                case KeyEntryRequest.VerifyFido2Uv:
                    Console.WriteLine("Fingerprint requested.");
                    break;
                case KeyEntryRequest.EnrollFingerprint:
                    Console.WriteLine("Fingerprint sample requested.");
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
