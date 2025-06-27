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
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv.Commands
{
    // All these tests will reset the PIV application, run, then reset the PIV
    // application again.
    // All these tests will also use a random number generator with a specified
    // set of bytes, followed by 2048 random bytes. If you want to get only
    // random bytes, skip the first SpecifiedStart bytes (get a random object and
    // generate that many bytes).
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class AuthMgmtKeyCmdTests : PivSessionIntegrationTestBase
    {
        [Fact]
        public void AuthKey_Default_Succeeds()
        {
            byte[] mgmtKey =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };

            var initCmd = new InitializeAuthenticateManagementKeyCommand(false, DefaultManagementKeyType.GetPivAlgorithm());
            var initRsp = Session.Connection.SendCommand(initCmd);
            Assert.Equal(ResponseStatus.Success, initRsp.Status);

            var completeCmd = new CompleteAuthenticateManagementKeyCommand(initRsp, mgmtKey);
            var completeRsp = Session.Connection.SendCommand(completeCmd);

            Assert.Equal(ResponseStatus.Success, completeRsp.Status);
        }

        [Fact]
        public void AuthKey_Aes_Succeeds()
        {
            byte[] mgmtKey =
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58
            };

            // TODO this test only works with 5.4.3 and lower
            var initCmd = new InitializeAuthenticateManagementKeyCommand(true, DefaultManagementKeyType.GetPivAlgorithm());
            var initRsp = Session.Connection.SendCommand(initCmd);
            Assert.Equal(ResponseStatus.Success, initRsp.Status);

            var completeCmd = new CompleteAuthenticateManagementKeyCommand(initRsp, DefaultManagementKey.Span);
            var completeRsp = Session.Connection.SendCommand(completeCmd);

            Assert.Equal(ResponseStatus.Success, completeRsp.Status);

            var setCmd =
                new SetManagementKeyCommand(mgmtKey, PivTouchPolicy.Never, KeyType.AES192.GetPivAlgorithm());

            var setRsp = Session.Connection.SendCommand(setCmd);
            Assert.Equal(ResponseStatus.Success, setRsp.Status);

            initCmd = new InitializeAuthenticateManagementKeyCommand(true, KeyType.AES192.GetPivAlgorithm());
            initRsp = Session.Connection.SendCommand(initCmd);
            Assert.Equal(ResponseStatus.Success, initRsp.Status);

            completeCmd = new CompleteAuthenticateManagementKeyCommand(initRsp, mgmtKey);
            completeRsp = Session.Connection.SendCommand(completeCmd);

            Assert.Equal(ResponseStatus.Success, completeRsp.Status);
        }
    }
}
