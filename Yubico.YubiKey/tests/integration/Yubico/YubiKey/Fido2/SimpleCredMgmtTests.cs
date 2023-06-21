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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    public class SimpleCredMgmtTests : SimpleIntegrationTestConnection
    {
        public SimpleCredMgmtTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Fw5)
        {
        }

        [Fact]
        public void GetMetadata_Succeeds()
        {
            using (var fido2Session = new Fido2Session(Device))
            {
                fido2Session.KeyCollector = LocalKeyCollector;

                (int credCount, int slotCount) = fido2Session.GetCredentialMetadata();
                Assert.Equal(1, credCount);
                Assert.Equal(24, slotCount);
            }
        }

        [Fact]
        public void EnumerateRps_Succeeds()
        {
            using (var fido2Session = new Fido2Session(Device))
            {
                fido2Session.KeyCollector = LocalKeyCollector;

                IReadOnlyList<RelyingParty> rpList = fido2Session.EnumerateRelyingParties();
                Assert.Equal(2, rpList.Count);
            }
        }

        [Fact]
        public void EnumerateCreds_Succeeds()
        {
            using (var fido2Session = new Fido2Session(Device))
            {
                fido2Session.KeyCollector = LocalKeyCollector;

                IReadOnlyList<RelyingParty> rpList = fido2Session.EnumerateRelyingParties();
                IReadOnlyList<CredentialUserInfo> ykCredList =
                    fido2Session.EnumerateCredentialsForRelyingParty(rpList[0]);
                Assert.Equal(2, ykCredList.Count);
            }
        }

        [Fact]
        public void DeleteCred_Succeeds()
        {
            using (var fido2Session = new Fido2Session(Device))
            {
                fido2Session.KeyCollector = LocalKeyCollector;

                IReadOnlyList<RelyingParty> rpList = fido2Session.EnumerateRelyingParties();
                IReadOnlyList<CredentialUserInfo> credList =
                    fido2Session.EnumerateCredentialsForRelyingParty(rpList[0]);
                int count = credList.Count;

                fido2Session.ClearAuthToken();

                fido2Session.DeleteCredential(credList[0].CredentialId);

                credList = fido2Session.EnumerateCredentialsForRelyingParty(rpList[0]);
                Assert.NotNull(credList);
                Assert.True(credList.Count == count - 1);
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
}
