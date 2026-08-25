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

using System.Collections.Generic;
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class EnumerateCredentialsResponseTests
    {
        [Fact]
        public void BeginResponse_GetData_SupportedPublicKey_ReturnsModeledKey()
        {
            var response = new EnumerateCredentialsBeginResponse(
                BuildApdu(CredMgmtTestData.BuildEs256CoseKey(), totalCredentials: 3));

            (int credentialCount, CredentialUserInfo userInfo) = response.GetData();

            Assert.Equal(3, credentialCount);
            _ = Assert.IsType<CoseEcPublicKey>(userInfo.CredentialPublicKey);
        }

        [Fact]
        public void BeginResponse_GetData_UnsupportedPublicKey_ReturnsCredentialWithRawKey()
        {
            byte[] publicKey = CredMgmtTestData.BuildArkgSeedCoseKey();
            var response = new EnumerateCredentialsBeginResponse(
                BuildApdu(publicKey, totalCredentials: 2));

            (int credentialCount, CredentialUserInfo userInfo) = response.GetData();

            Assert.Equal(2, credentialCount);
            Assert.Equal(CredMgmtTestData.CredentialIdBytes, userInfo.CredentialId.Id.ToArray());
            Assert.Equal(CredMgmtTestData.UserIdBytes, userInfo.User.Id.ToArray());

            var unsupported = Assert.IsType<CoseUnsupportedPublicKey>(userInfo.CredentialPublicKey);
            Assert.Equal(publicKey, unsupported.EncodedKey.ToArray());
            Assert.Equal(CredMgmtTestData.ArkgPubKeyType, (int)unsupported.Type);
            Assert.Equal(CredMgmtTestData.ArkgP256Algorithm, (int)unsupported.Algorithm);
        }

        [Fact]
        public void GetNextResponse_GetData_SupportedPublicKey_ReturnsModeledKey()
        {
            var response = new EnumerateCredentialsGetNextResponse(
                BuildApdu(CredMgmtTestData.BuildEs256CoseKey()));

            CredentialUserInfo userInfo = response.GetData();

            _ = Assert.IsType<CoseEcPublicKey>(userInfo.CredentialPublicKey);
        }

        [Fact]
        public void GetNextResponse_GetData_UnsupportedPublicKey_ReturnsCredentialWithRawKey()
        {
            byte[] publicKey = CredMgmtTestData.BuildArkgSeedCoseKey();
            var response = new EnumerateCredentialsGetNextResponse(BuildApdu(publicKey));

            CredentialUserInfo userInfo = response.GetData();

            Assert.Equal(CredMgmtTestData.CredentialIdBytes, userInfo.CredentialId.Id.ToArray());

            var unsupported = Assert.IsType<CoseUnsupportedPublicKey>(userInfo.CredentialPublicKey);
            Assert.Equal(publicKey, unsupported.EncodedKey.ToArray());
        }

        /// <summary>
        /// This is the reported bug. Enumerating a relying party issues one
        /// Begin response followed by one GetNext response per remaining
        /// credential, and a single credential whose public key this SDK does
        /// not model previously aborted the whole enumeration.
        /// </summary>
        /// <remarks>
        /// This mirrors the response-decoding loop in
        /// <c>Fido2Session.EnumerateCredentialsForRelyingParty</c>, driving the
        /// GetNext count off the credential count returned by Begin. It stops at
        /// the response boundary: exercising the session method itself would
        /// require a mocked key-agreement exchange and a PIN/UV auth token
        /// encrypted under the negotiated shared secret, for which the unit test
        /// project currently has no harness.
        /// </remarks>
        [Fact]
        public void EnumerateSequence_MiddleCredentialUnsupported_AllCredentialsReadable()
        {
            var getNextResponses = new Queue<EnumerateCredentialsGetNextResponse>(
                new[]
                {
                    new EnumerateCredentialsGetNextResponse(BuildApdu(CredMgmtTestData.BuildArkgSeedCoseKey())),
                    new EnumerateCredentialsGetNextResponse(BuildApdu(CredMgmtTestData.BuildEs256CoseKey())),
                });

            var beginResponse = new EnumerateCredentialsBeginResponse(
                BuildApdu(CredMgmtTestData.BuildEs256CoseKey(), totalCredentials: 3));

            (int credentialCount, CredentialUserInfo firstInfo) = beginResponse.GetData();
            var credentials = new List<CredentialUserInfo>(credentialCount) { firstInfo };

            for (int index = 1; index < credentialCount; index++)
            {
                credentials.Add(getNextResponses.Dequeue().GetData());
            }

            Assert.Equal(3, credentials.Count);
            _ = Assert.IsType<CoseEcPublicKey>(credentials[0].CredentialPublicKey);
            _ = Assert.IsType<CoseUnsupportedPublicKey>(credentials[1].CredentialPublicKey);
            _ = Assert.IsType<CoseEcPublicKey>(credentials[2].CredentialPublicKey);

            Assert.All(
                credentials,
                credential => Assert.Equal(
                    CredMgmtTestData.CredentialIdBytes,
                    credential.CredentialId.Id.ToArray()));
        }

        private static ResponseApdu BuildApdu(byte[] encodedPublicKey, int totalCredentials = 1) =>
            new ResponseApdu(
                CredMgmtTestData.BuildCredentialUserInfo(encodedPublicKey, totalCredentials),
                SWConstants.Success);
    }
}
