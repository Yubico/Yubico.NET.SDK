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
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    [Trait(TraitTypes.Category, TestCategories.Elevated)]
    public class CredMgmtTests : FidoSessionIntegrationTestBase
    {
        // Defeat the macOS YubiKeyDeviceListener startup race before the
        // base class's instance ctor runs. See DeviceListenerCacheWarmup
        // for the full rationale.
        static CredMgmtTests() => DeviceListenerCacheWarmup.WaitForFirstDevice();

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void GetMetadata_Succeeds()
        {
            _ = AddCredential("metadata.example.com", 1);

            (int credCount, int slotCount) = Session.GetCredentialMetadata();

            Assert.InRange(credCount, 1, int.MaxValue);
            Assert.InRange(slotCount, 1, int.MaxValue);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void EnumerateRps_Succeeds()
        {
            RelyingParty relyingParty = AddCredential("enumerate-rps.example.com", 1);

            IReadOnlyList<RelyingParty> rpList = Session.EnumerateRelyingParties();

            Assert.Contains(rpList, rp => rp.Id == relyingParty.Id);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void EnumerateCreds_Succeeds()
        {
            RelyingParty relyingParty = AddCredential("enumerate-creds.example.com", 1);

            IReadOnlyList<CredentialUserInfo> credList =
                Session.EnumerateCredentialsForRelyingParty(relyingParty);

            Assert.Single(credList);
            Assert.Equal("user-1", credList[0].User.Name);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void DeleteCred_Succeeds()
        {
            RelyingParty relyingParty = AddCredential("delete-creds.example.com", 1);

            IReadOnlyList<CredentialUserInfo> credList =
                Session.EnumerateCredentialsForRelyingParty(relyingParty);
            Assert.Single(credList);

            Session.ClearAuthToken();
            Session.DeleteCredential(credList[0].CredentialId);

            credList = Session.EnumerateCredentialsForRelyingParty(relyingParty);
            Assert.Empty(credList);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void UpdateUserInfo_Succeeds()
        {
            string updatedDisplayName = "Updated Display Name";
            RelyingParty relyingParty = AddCredential("update-user.example.com", 1);

            IReadOnlyList<CredentialUserInfo> credList =
                Session.EnumerateCredentialsForRelyingParty(relyingParty);
            Assert.Single(credList);

            Session.ClearAuthToken();

            UserEntity newInfo = credList[0].User;
            newInfo.DisplayName = updatedDisplayName;

            Session.UpdateUserInfoForCredential(credList[0].CredentialId, newInfo);

            credList = Session.EnumerateCredentialsForRelyingParty(relyingParty);

            string displayName = credList[0].User.DisplayName ?? "";
            Assert.Equal(updatedDisplayName, displayName);
        }

        private RelyingParty AddCredential(string relyingPartyId, int userId)
        {
            var relyingParty = new RelyingParty(relyingPartyId)
            {
                Name = relyingPartyId,
            };

            var user = new UserEntity(new byte[] { (byte)userId })
            {
                Name = $"user-{userId}",
                DisplayName = $"User {userId}",
            };

            var makeCredentialParameters = new MakeCredentialParameters(relyingParty, user)
            {
                ClientDataHash = ClientDataHash,
            };
            makeCredentialParameters.AddOption(AuthenticatorOptions.rk, true);

            Session.MakeCredential(makeCredentialParameters);

            return relyingParty;
        }
    }
}
