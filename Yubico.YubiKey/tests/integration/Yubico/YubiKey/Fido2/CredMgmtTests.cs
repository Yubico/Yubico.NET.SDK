// Copyright 2022 Yubico AB
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
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    [Trait("Category", "RequiresBio")]
    public class CredMgmtTests : IClassFixture<BioFido2Fixture>
    {
        private readonly BioFido2Fixture _bioFido2Fixture;

        public CredMgmtTests(BioFido2Fixture bioFido2Fixture)
        {
            _bioFido2Fixture = bioFido2Fixture;
            if (_bioFido2Fixture.HasCredentials)
            {
                return;
            }

            _bioFido2Fixture.AddCredentials(discoverableCount: 2, nonDiscoverableCount: 1);
            _bioFido2Fixture.AddCredentials(discoverableCount: 1, nonDiscoverableCount: 0);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void GetMetadata_Succeeds()
        {
            using (var fido2Session = new Fido2Session(_bioFido2Fixture.Device))
            {
                fido2Session.KeyCollector = _bioFido2Fixture.KeyCollector;
                fido2Session.AddPermissions(
                    PinUvAuthTokenPermissions.MakeCredential, _bioFido2Fixture.RpInfoList[index: 0].RelyingParty.Id);

                (var credCount, var slotCount) = fido2Session.GetCredentialMetadata();
                Assert.Equal(expected: 3, credCount);
                Assert.Equal(expected: 22, slotCount);
            }
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void EnumerateRps_Succeeds()
        {
            using (var fido2Session = new Fido2Session(_bioFido2Fixture.Device))
            {
                fido2Session.KeyCollector = _bioFido2Fixture.KeyCollector;
                fido2Session.AddPermissions(PinUvAuthTokenPermissions.CredentialManagement, "rp-3");

                var rpList = fido2Session.EnumerateRelyingParties();
                Assert.Equal(expected: 2, rpList.Count);

                var rpInfo = _bioFido2Fixture.MatchRelyingParty(rpList[index: 0]);
                var isValid = rpInfo.RelyingPartyIdHash.Span.SequenceEqual(rpList[index: 0].RelyingPartyIdHash.Span);
                Assert.True(isValid);
            }
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void EnumerateCreds_Succeeds()
        {
            using (var fido2Session = new Fido2Session(_bioFido2Fixture.Device))
            {
                fido2Session.KeyCollector = _bioFido2Fixture.KeyCollector;
                fido2Session.AddPermissions(PinUvAuthTokenPermissions.MakeCredential, "rp-3");

                var rpInfo = _bioFido2Fixture.RpInfoList[index: 0];
                var ykCredList =
                    fido2Session.EnumerateCredentialsForRelyingParty(rpInfo.RelyingParty);
                Assert.Equal(rpInfo.DiscoverableCount, ykCredList.Count);

                var ykUser = ykCredList[index: 0].User;

                var userInfo = _bioFido2Fixture.MatchUser(rpInfo.RelyingParty, ykUser);
                var targetKey = userInfo.Item2.LargeBlobKey
                                ?? throw new InvalidOperationException("No matching User.");
                var ykLargeBlobKey = ykCredList[index: 0].LargeBlobKey
                                     ?? throw new InvalidOperationException("No matching User.");

                var isValid = targetKey.Span.SequenceEqual(ykLargeBlobKey.Span);
                Assert.True(isValid);
            }
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void DeleteCred_Succeeds()
        {
            _bioFido2Fixture.AddCredentials(discoverableCount: 1, nonDiscoverableCount: 0);
            using (var fido2Session = new Fido2Session(_bioFido2Fixture.Device))
            {
                fido2Session.KeyCollector = _bioFido2Fixture.KeyCollector;
                fido2Session.AddPermissions(PinUvAuthTokenPermissions.AuthenticatorConfiguration);

                var credList =
                    fido2Session.EnumerateCredentialsForRelyingParty(_bioFido2Fixture.RpInfoList[index: 2]
                        .RelyingParty);
                var count = credList.Count;
                Assert.Equal(expected: 1, count);

                fido2Session.ClearAuthToken();
                fido2Session.AddPermissions(PinUvAuthTokenPermissions.AuthenticatorConfiguration);

                fido2Session.DeleteCredential(credList[index: 0].CredentialId);

                credList = fido2Session.EnumerateCredentialsForRelyingParty(_bioFido2Fixture.RpInfoList[index: 2]
                    .RelyingParty);
                Assert.NotNull(credList);
                Assert.True(credList.Count == count - 1);
            }
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void UpdateUserInfo_Succeeds()
        {
            var updatedDisplayName = "Updated Display Name";

            using (var fido2Session = new Fido2Session(_bioFido2Fixture.Device))
            {
                fido2Session.KeyCollector = _bioFido2Fixture.KeyCollector;
                fido2Session.AddPermissions(PinUvAuthTokenPermissions.AuthenticatorConfiguration);

                var credList =
                    fido2Session.EnumerateCredentialsForRelyingParty(_bioFido2Fixture.RpInfoList[index: 0]
                        .RelyingParty);
                Assert.NotEmpty(credList);

                fido2Session.ClearAuthToken();
                fido2Session.AddPermissions(PinUvAuthTokenPermissions.AuthenticatorConfiguration);

                var newInfo = credList[index: 0].User;
                newInfo.DisplayName = updatedDisplayName;

                fido2Session.UpdateUserInfoForCredential(credList[index: 0].CredentialId, newInfo);

                credList = fido2Session.EnumerateCredentialsForRelyingParty(_bioFido2Fixture.RpInfoList[index: 0]
                    .RelyingParty);

                var displayName = credList[index: 0].User.DisplayName ?? "";
                Assert.Equal(updatedDisplayName, displayName);
            }
        }
    }
}
