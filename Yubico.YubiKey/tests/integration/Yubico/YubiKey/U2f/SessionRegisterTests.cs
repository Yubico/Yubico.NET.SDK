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
using System.Linq;
using Xunit;
using Yubico.PlatformInterop;
using Yubico.YubiKey.U2f.Commands;

namespace Yubico.YubiKey.U2f
{
    public class SessionRegisterTests
    {
        private readonly IYubiKeyDevice _yubiKeyDevice;

        public SessionRegisterTests()
        {
            if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows)
            {
                if (!SdkPlatformInfo.IsElevated)
                {
                    throw new ArgumentException("Windows not elevated.");
                }
            }

            var yubiKeys = YubiKeyDevice.FindByTransport(Transport.HidFido | Transport.UsbSmartCard);
            var yubiKeyList = yubiKeys.ToList();
            Assert.NotEmpty(yubiKeyList);

            _yubiKeyDevice = yubiKeyList[index: 0];
        }

        [Fact]
        public void RegisterFips_Succeeds()
        {
            var keyCollector = new SimpleU2fKeyCollector(isU2fPinSet: true);

            using (var u2fSession = new U2fSession(_yubiKeyDevice))
            {
                u2fSession.KeyCollector = keyCollector.SimpleU2fKeyCollectorDelegate;

                var cmd = new GetPagedDeviceInfoCommand();
                var rsp = u2fSession.Connection.SendCommand(cmd);
                Assert.Equal(ResponseStatus.Success, rsp.Status);

                var getData = YubiKeyDeviceInfo.CreateFromResponseData(rsp.GetData());
                if (!getData.IsFipsSeries)
                {
                    return;
                }

                var modeCmd = new VerifyFipsModeCommand();
                var modeRsp = u2fSession.Connection.SendCommand(modeCmd);
                var isMode = modeRsp.GetData();
                Assert.True(isMode);

                var appId = RegistrationDataTests.GetAppIdArray(isValid: true);
                var clientDataHash = RegistrationDataTests.GetClientDataHashArray(isValid: true);

                var isValid = u2fSession.TryRegister(
                    appId, clientDataHash, new TimeSpan(hours: 0, minutes: 0, seconds: 5), out var regDataQ);
                Assert.True(isValid);

                //                RegistrationData regDataQ = u2fSession.Register(
                //                    appId, clientDataHash, TimeSpan.Zero);

                Assert.NotNull(regDataQ);

                if (regDataQ is null)
                {
                    return;
                }

                var regData = regDataQ;

                var isVerified = regData.VerifySignature(appId, clientDataHash);
                Assert.True(isVerified);
            }
        }

        [Fact]
        public void AuthenticateFips_Succeeds()
        {
            var keyCollector = new SimpleU2fKeyCollector(isU2fPinSet: true);

            var appId = RegistrationDataTests.GetAppIdArray(isValid: true);
            var clientDataHash = RegistrationDataTests.GetClientDataHashArray(isValid: true);
            var keyHandle = RegistrationDataTests.GetKeyHandleArray(isValid: true, out var handleLength);
            var pubKey = RegistrationDataTests.GetPubKeyArray(isValid: true);

            using (var u2fSession = new U2fSession(_yubiKeyDevice))
            {
                u2fSession.KeyCollector = keyCollector.SimpleU2fKeyCollectorDelegate;

                var isValid = u2fSession.VerifyKeyHandle(appId, clientDataHash, keyHandle);
                Assert.True(isValid);

                isValid = u2fSession.TryAuthenticate(
                    appId, clientDataHash, keyHandle, new TimeSpan(hours: 0, minutes: 0, seconds: 5), out var authData,
                    requireProofOfPresence: false);
                Assert.True(isValid);

                Assert.NotNull(authData);

                if (authData is null)
                {
                    return;
                }

                var isVerified = authData.VerifySignature(pubKey, appId, clientDataHash);
                Assert.True(isVerified);
            }
        }
    }
}
