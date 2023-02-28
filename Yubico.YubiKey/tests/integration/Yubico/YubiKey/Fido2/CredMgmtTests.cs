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
using System.Collections.Generic;
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    public class CredMgmtTests : IClassFixture<BioFido2Fixture>
    {
        private readonly BioFido2Fixture _bioFido2Fixture;

        public CredMgmtTests(BioFido2Fixture bioFido2Fixture)
        {
            _bioFido2Fixture = bioFido2Fixture;

            if (!_bioFido2Fixture.HasCredentials)
            {
                _bioFido2Fixture.AddCredentials(2, 1);
                _bioFido2Fixture.AddCredentials(1, 0);
            }
        }

        [Fact]
        public void GetMetadata_Succeeds()
        {
            using (var fido2Session = new Fido2Session(_bioFido2Fixture.Device))
            {
                fido2Session.KeyCollector = _bioFido2Fixture.KeyCollector;
                fido2Session.AddPermissions(
                    PinUvAuthTokenPermissions.MakeCredential, _bioFido2Fixture.RpInfoList[0].RelyingParty.Id);

                CredentialManagementData mgmtData = fido2Session.GetCredentialMetadata();
                Assert.Equal(3, mgmtData.NumberOfDiscoverableCredentials);
                Assert.Equal(22, mgmtData.RemainingCredentialCount);
            }
        }

        [Fact]
        public void EnumerateRps_Succeeds()
        {
            using (var fido2Session = new Fido2Session(_bioFido2Fixture.Device))
            {
                fido2Session.KeyCollector = _bioFido2Fixture.KeyCollector;
                fido2Session.AddPermissions(PinUvAuthTokenPermissions.CredentialManagement, "rp-3");

                IReadOnlyList<CredentialManagementData> ykDataList = fido2Session.EnumerateRelyingParties();
                Assert.Equal(2, ykDataList.Count);

                RelyingParty ykRp = ykDataList[0].RelyingParty
                    ?? throw new InvalidOperationException("No matching Rp.");
                ReadOnlyMemory<byte> ykDigest = ykDataList[0].RelyingPartyIdHash
                    ?? throw new InvalidOperationException("No matching Rp.");

                RpInfo rpInfo = _bioFido2Fixture.MatchRelyingParty(ykRp);
                bool isValid = MemoryExtensions.SequenceEqual<byte>(ykDigest.Span, rpInfo.RelyingPartyIdHash.Span);
                Assert.True(isValid);
            }
        }

        [Fact]
        public void EnumerateCreds_Succeeds()
        {
            using (var fido2Session = new Fido2Session(_bioFido2Fixture.Device))
            {
                fido2Session.KeyCollector = _bioFido2Fixture.KeyCollector;
                fido2Session.AddPermissions(PinUvAuthTokenPermissions.MakeCredential, "rp-3");

                RpInfo rpInfo = _bioFido2Fixture.RpInfoList[0];
                IReadOnlyList<CredentialManagementData> ykCredList =
                    fido2Session.EnumerateCredentialsForRelyingParty(rpInfo.RelyingParty.Id);
                Assert.Equal(rpInfo.DiscoverableCount, ykCredList.Count);

                UserEntity ykUser = ykCredList[0].User
                    ?? throw new InvalidOperationException("No matching User.");

                Tuple<UserEntity,MakeCredentialData> userInfo = _bioFido2Fixture.MatchUser(rpInfo.RelyingParty, ykUser);
                ReadOnlyMemory<byte> targetKey = userInfo.Item2.LargeBlobKey
                    ?? throw new InvalidOperationException("No matching User.");
                ReadOnlyMemory<byte> ykLargeBlobKey = ykCredList[0].LargeBlobKey
                    ?? throw new InvalidOperationException("No matching User.");

                bool isValid = MemoryExtensions.SequenceEqual<byte>(targetKey.Span, ykLargeBlobKey.Span);
                Assert.True(isValid);
            }
        }
    }
}
