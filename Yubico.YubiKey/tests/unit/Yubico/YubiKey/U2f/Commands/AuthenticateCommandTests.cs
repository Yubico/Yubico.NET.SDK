// Copyright 2021 Yubico AB
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

namespace Yubico.YubiKey.U2f.Commands
{
    public class AuthenticateCommandTests
    {
        [Fact]
        public void Constructor_CorrectApplication()
        {
            var cmd = new AuthenticateCommand(
                U2fAuthenticationType.EnforceUserPresence,
                RegistrationDataTests.GetAppIdArray(isValid: true),
                RegistrationDataTests.GetClientDataHashArray(isValid: true),
                RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _));
            Assert.Equal(YubiKeyApplication.FidoU2f, cmd.Application);
        }

        [Theory]
        [InlineData(U2fAuthenticationType.CheckOnly)]
        [InlineData(U2fAuthenticationType.EnforceUserPresence)]
        [InlineData(U2fAuthenticationType.DontEnforceUserPresence)]
        public void Constructor_CorrectControlByte(U2fAuthenticationType controlByte)
        {
            var cmd = new AuthenticateCommand(
                controlByte,
                RegistrationDataTests.GetAppIdArray(isValid: true),
                RegistrationDataTests.GetClientDataHashArray(isValid: true),
                RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _));
            Assert.Equal(controlByte, cmd.ControlByte);
        }

        [Theory]
        [InlineData(U2fAuthenticationType.CheckOnly)]
        [InlineData(U2fAuthenticationType.EnforceUserPresence)]
        [InlineData(U2fAuthenticationType.DontEnforceUserPresence)]
        public void Constructor_ChangeControlByte_Correct(U2fAuthenticationType controlByte)
        {
            var newByte = controlByte switch
            {
                U2fAuthenticationType.CheckOnly => U2fAuthenticationType.EnforceUserPresence,
                U2fAuthenticationType.EnforceUserPresence => U2fAuthenticationType.DontEnforceUserPresence,
                U2fAuthenticationType.DontEnforceUserPresence => U2fAuthenticationType.CheckOnly,
                _ => U2fAuthenticationType.Unknown
            };

#pragma warning disable IDE0017 // Testing specific behavior
            var cmd = new AuthenticateCommand(
                controlByte,
                RegistrationDataTests.GetAppIdArray(isValid: true),
                RegistrationDataTests.GetClientDataHashArray(isValid: true),
                RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _));
            cmd.ControlByte = newByte;
#pragma warning restore IDE0017
            Assert.Equal(newByte, cmd.ControlByte);
        }

        [Fact]
        public void InvalidClientDataLength_Throws()
        {
            _ = Assert.Throws<ArgumentException>(() => new AuthenticateCommand(
                U2fAuthenticationType.EnforceUserPresence,
                RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _),
                RegistrationDataTests.GetAppIdArray(isValid: true),
                RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _)));
        }

        [Fact]
        public void Reset_InvalidClientDataLength_Throws()
        {
            var cmd = new AuthenticateCommand(
                U2fAuthenticationType.EnforceUserPresence,
                RegistrationDataTests.GetAppIdArray(isValid: true),
                RegistrationDataTests.GetClientDataHashArray(isValid: true),
                RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _));

            _ = Assert.Throws<ArgumentException>(() =>
                cmd.ClientDataHash = RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _));
        }

        [Fact]
        public void InvalidAppIdLength_Throws()
        {
            _ = Assert.Throws<ArgumentException>(() => new AuthenticateCommand(
                U2fAuthenticationType.EnforceUserPresence,
                RegistrationDataTests.GetClientDataHashArray(isValid: true),
                RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _),
                RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _)));
        }

        [Fact]
        public void Reset_InvalidAppIdLength_Throws()
        {
            var cmd = new AuthenticateCommand(
                U2fAuthenticationType.EnforceUserPresence,
                RegistrationDataTests.GetAppIdArray(isValid: true),
                RegistrationDataTests.GetClientDataHashArray(isValid: true),
                RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _));

            _ = Assert.Throws<ArgumentException>(() =>
                cmd.ApplicationId = RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _));
        }

        [Fact]
        public void InvalidKeyHandleLength_Throws()
        {
            _ = Assert.Throws<ArgumentException>(() => new AuthenticateCommand(
                U2fAuthenticationType.EnforceUserPresence,
                RegistrationDataTests.GetAppIdArray(isValid: true),
                RegistrationDataTests.GetClientDataHashArray(isValid: true),
                RegistrationDataTests.GetAppIdArray(isValid: true)));
        }

        [Fact]
        public void Reset_InvalidKeyHandleLength_Throws()
        {
            var cmd = new AuthenticateCommand(
                U2fAuthenticationType.EnforceUserPresence,
                RegistrationDataTests.GetAppIdArray(isValid: true),
                RegistrationDataTests.GetClientDataHashArray(isValid: true),
                RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _));

            _ = Assert.Throws<ArgumentException>(() =>
                cmd.KeyHandle = RegistrationDataTests.GetClientDataHashArray(isValid: true));
        }

        [Fact]
        public void CommandApdu_ClaProperty_Zero()
        {
            var cmd = new AuthenticateCommand(
                U2fAuthenticationType.EnforceUserPresence,
                RegistrationDataTests.GetAppIdArray(isValid: true),
                RegistrationDataTests.GetClientDataHashArray(isValid: true),
                RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _));
            var cmdApdu = cmd.CreateCommandApdu();

            Assert.Equal(expected: 0, cmdApdu.Cla);
        }

        [Fact]
        public void CommandApdu_InsProperty_Three()
        {
            var cmd = new AuthenticateCommand(
                U2fAuthenticationType.EnforceUserPresence,
                RegistrationDataTests.GetAppIdArray(isValid: true),
                RegistrationDataTests.GetClientDataHashArray(isValid: true),
                RegistrationDataTests.GetKeyHandleArray(isValid: true, out var _));
            var cmdApdu = cmd.CreateCommandApdu();

            Assert.Equal(expected: 3, cmdApdu.Ins);
        }
    }
}
