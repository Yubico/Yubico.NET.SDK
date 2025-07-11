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
using Xunit;

namespace Yubico.YubiKey.U2f
{
    public class AuthDataTests
    {
        [Fact]
        public void IncorrectUserPresence_Throws()
        {
            byte[] authData = RegistrationDataTests.GetGoodAuthDataArray();
            authData[0] = 0x81;

            _ = Assert.Throws<ArgumentException>(() => new AuthenticationData(authData));
        }

        [Fact]
        public void EncodedCounter_CorrectInAuthData()
        {
            byte[] authData = RegistrationDataTests.GetGoodAuthDataArray();
            authData[1] = 0;
            authData[2] = 0;
            authData[3] = 0x14;
            authData[4] = 0x86;

            var authenticationData = new AuthenticationData(authData);

            Assert.Equal(0x1486, authenticationData.Counter);
        }

        [Fact]
        public void UserPresenceSet_VerifiedTrue()
        {
            byte[] authData = RegistrationDataTests.GetGoodAuthDataArray();
            authData[0] = 0x01;

            var authenticationData = new AuthenticationData(authData);

            Assert.True(authenticationData.UserPresenceVerified);
        }

        [Fact]
        public void UserPresenceNotSet_VerifiedFalse()
        {
            byte[] authData = RegistrationDataTests.GetGoodAuthDataArray();
            authData[0] = 0x00;

            var authenticationData = new AuthenticationData(authData);

            Assert.False(authenticationData.UserPresenceVerified);
        }

        [Fact]
        public void VerifySignature_GivenCorrectData_ReturnsTrue()
        {
            byte[] appId = RegistrationDataTests.GetAppIdArray(true);
            byte[] clientDataHash = RegistrationDataTests.GetClientDataHashArray(true);
            byte[] userPublicKey = RegistrationDataTests.GetPubKeyArray(true);
            byte[] authData = RegistrationDataTests.GetGoodAuthDataArray();

            var authenticationData = new AuthenticationData(authData);

            bool isVerified = authenticationData.VerifySignature(userPublicKey, appId, clientDataHash);
            Assert.True(isVerified);
        }
    }
}
