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
using System.Linq;
using System.Text;
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    [Trait(TraitTypes.Category, TestCategories.Elevated)]
    [Trait(TraitTypes.Category, TestCategories.RequiresSetup)] // Requires pin 123456 and one FIDO credential set up
    public class SimpleCredMgmtTests : SimpleIntegrationTestConnection
    {
        public int LocalKeyCollectorVerifyPinCalls { get; private set; }

        public SimpleCredMgmtTests()
            : base(YubiKeyApplication.Fido2)
        {
        }

        [Fact]
        public void GetMetadata_Succeeds() // Works when at least one credential is set up
        {
            using var fido2Session = new Fido2Session(Device);
            fido2Session.KeyCollector = LocalKeyCollector;

            (var credCount, var slotCount) = fido2Session.GetCredentialMetadata();

            Assert.Equal(1, credCount);
            Assert.Equal(24, slotCount);
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

            Assert.Single(ykCredList);
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

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void CredentialManagement_Succeeds_WithRO_Token()
        {
            LocalKeyCollectorVerifyPinCalls = 0;

            // Test GetCredentialMetadata
            using (var fido2Session = new Fido2Session(Device))
            {
                Assert.Null(fido2Session.AuthTokenPersistent);
                fido2Session.KeyCollector = LocalKeyCollector;

                // Will require pin
                fido2Session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);
                Assert.NotNull(fido2Session.AuthTokenPersistent);
                Assert.Equal(1, LocalKeyCollectorVerifyPinCalls);

                // Clear the key collector (to test missing ability to generate new tokens)
                fido2Session.KeyCollector = null;

                // Will not require pin
                var (discoverableCredentialCount, remainingCredentialCount) = fido2Session.GetCredentialMetadata();
                Assert.True(remainingCredentialCount > 0);
                Assert.Equal(1, LocalKeyCollectorVerifyPinCalls); // Should be unchanged
            }

            // Reset the call count for the next test
            LocalKeyCollectorVerifyPinCalls = 0;

            // Test EnumerateRelyingParties
            using (var fido2Session = new Fido2Session(Device))
            {
                Assert.Null(fido2Session.AuthTokenPersistent);

                fido2Session.KeyCollector = LocalKeyCollector;

                // Will require pin
                fido2Session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);
                Assert.NotNull(fido2Session.AuthTokenPersistent);
                Assert.Equal(1, LocalKeyCollectorVerifyPinCalls);

                // Clear the key collector (to test missing ability to generate new tokens)
                fido2Session.KeyCollector = null;

                // Will not require pin
                var relyingParties = fido2Session.EnumerateRelyingParties();
                Assert.Equal(1, LocalKeyCollectorVerifyPinCalls); // Should be unchanged
            }

            // Reset the call count for the next test
            LocalKeyCollectorVerifyPinCalls = 0;

            // Test DeleteCredential
            using (var fido2Session = new Fido2Session(Device))
            {
                Assert.Null(fido2Session.AuthTokenPersistent);

                fido2Session.KeyCollector = LocalKeyCollector;

                // Will require pin
                fido2Session.VerifyPin(PinUvAuthTokenPermissions.PersistentCredentialManagementReadOnly);
                Assert.NotNull(fido2Session.AuthTokenPersistent);
                Assert.Equal(1, LocalKeyCollectorVerifyPinCalls);

                // Clear the key collector (to test missing ability to generate new tokens)
                fido2Session.KeyCollector = null;

                // Will not require pin
                var cred = fido2Session.EnumerateCredentialsForRelyingParty("demo.yubico.com").FirstOrDefault();
                Assert.Equal(1, LocalKeyCollectorVerifyPinCalls); // Should be unchanged

                // Send command to delete credential with RO token (fails)
                var response = fido2Session.Connection.SendCommand(new DeleteCredentialCommand(
                    cred!.CredentialId,
                    fido2Session.AuthTokenPersistent.Value,
                    fido2Session.AuthProtocol));

                Assert.Equal(CtapStatus.PinAuthInvalid, response.CtapStatus);

                fido2Session.KeyCollector = LocalKeyCollector;
                response = fido2Session.Connection.SendCommand(new DeleteCredentialCommand(
                    cred!.CredentialId,
                    fido2Session.GetAuthToken(false, PinUvAuthTokenPermissions.CredentialManagement,
                        "demo.yubico.com"),
                    fido2Session.AuthProtocol));
                Assert.Equal(CtapStatus.Ok, response.CtapStatus);
                Assert.Equal(2, LocalKeyCollectorVerifyPinCalls);
            }
        }

        private bool LocalKeyCollector(KeyEntryData arg)
        {
            switch (arg.Request)
            {
                case KeyEntryRequest.VerifyFido2Pin:
                    ++LocalKeyCollectorVerifyPinCalls;
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
}
