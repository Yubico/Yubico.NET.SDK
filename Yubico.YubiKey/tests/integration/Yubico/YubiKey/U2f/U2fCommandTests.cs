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
using System.Collections.Generic;
using Xunit;
using Yubico.Core.Devices.Hid;
using Yubico.PlatformInterop;
using Yubico.YubiKey.U2f.Commands;

namespace Yubico.YubiKey.U2f
{
    // These tests all reset the U2F application on the YubiKey, then run the
    // test, then Reset the application again.
    public class U2fCommandTests
    {
        private readonly FidoConnection _fidoConnection;

        public U2fCommandTests()
        {
            if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows)
            {
                if (!SdkPlatformInfo.IsElevated)
                {
                    throw new ArgumentException("Windows not elevated.");
                }
            }

            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            HidDevice? deviceToUse = GetFidoHid(devices);
            Assert.NotNull(deviceToUse);

            if (deviceToUse is null)
            {
                throw new ArgumentException("null device");
            }

            _fidoConnection = new FidoConnection(deviceToUse);
            Assert.NotNull(_fidoConnection);
        }

        [Fact]
        public void RegisterAndAuth_Succeeds()
        {
            byte[] pin = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            var vfyPinCmd = new VerifyPinCommand(pin);
            VerifyPinResponse vfyPinRsp = _fidoConnection.SendCommand(vfyPinCmd);
            Assert.Equal(ResponseStatus.Success, vfyPinRsp.Status);

            byte[] appId = RegistrationDataTests.GetAppIdArray(true);
            byte[] clientDataHash = RegistrationDataTests.GetClientDataHashArray(true);
            var registerCmd = new RegisterCommand(appId, clientDataHash);
            RegisterResponse registerRsp = _fidoConnection.SendCommand(registerCmd);
            if (registerRsp.Status == ResponseStatus.ConditionsNotSatisfied)
            {
                registerRsp = _fidoConnection.SendCommand(registerCmd);
            }
            Assert.Equal(ResponseStatus.Success, registerRsp.Status);

            RegistrationData regData = registerRsp.GetData();
            bool isVerified = regData.VerifySignature(appId, clientDataHash);
            Assert.True(isVerified);

            var authCmd = new AuthenticateCommand(
                U2fAuthenticationType.DontEnforceUserPresence, appId, clientDataHash, regData.KeyHandle);
            AuthenticateResponse authRsp = _fidoConnection.SendCommand(authCmd);
            if (authRsp.Status == ResponseStatus.ConditionsNotSatisfied)
            {
                authRsp = _fidoConnection.SendCommand(authCmd);
            }
            Assert.Equal(ResponseStatus.Success, authRsp.Status);
            AuthenticationData authData = authRsp.GetData();
            Assert.False(authData.UserPresenceVerified);

            isVerified = authData.VerifySignature(regData.UserPublicKey, appId, clientDataHash);
            Assert.True(isVerified);
        }

        [Fact]
        public void Auth_Succeeds()
        {
            if (_fidoConnection is null)
            {
                return;
            }

            byte[] appId = RegistrationDataTests.GetAppIdArray(true);
            byte[] clientDataHash = RegistrationDataTests.GetClientDataHashArray(true);
            byte[] keyHandle = RegistrationDataTests.GetKeyHandleArray(true, out byte _);
            byte[] pubKey = RegistrationDataTests.GetPubKeyArray(true);

            var authCmd = new AuthenticateCommand(
                U2fAuthenticationType.DontEnforceUserPresence, appId, clientDataHash, keyHandle);
            AuthenticateResponse authRsp = _fidoConnection.SendCommand(authCmd);
            if (authRsp.Status == ResponseStatus.ConditionsNotSatisfied)
            {
                authRsp = _fidoConnection.SendCommand(authCmd);
            }
            Assert.Equal(ResponseStatus.Success, authRsp.Status);
            AuthenticationData authData = authRsp.GetData();
            Assert.False(authData.UserPresenceVerified);

            bool isVerified = authData.VerifySignature(pubKey, appId, clientDataHash);
            Assert.True(isVerified);
        }

        private static HidDevice? GetFidoHid(IEnumerable<HidDevice> devices)
        {
            foreach (HidDevice currentDevice in devices)
            {
                if (currentDevice.VendorId == 0x1050 &&
                    currentDevice.UsagePage == HidUsagePage.Fido)
                {
                    return currentDevice;
                }
            }

            return null;
        }
    }
}
