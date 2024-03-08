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
using Yubico.Core.Iso7816;
using Yubico.PlatformInterop;
using Yubico.YubiKey.U2f.Commands;

namespace Yubico.YubiKey.U2f
{
    public class PinTests : IDisposable
    {
        private readonly FidoConnection _fidoConnection;

        public PinTests()
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

            HidDevice? deviceToUse = CommandTests.GetFidoHid(devices);
            Assert.NotNull(deviceToUse);

            if (deviceToUse is null)
            {
                throw new ArgumentException("null device");
            }

            _fidoConnection = new FidoConnection(deviceToUse);
            Assert.NotNull(_fidoConnection);
        }

        public void Dispose()
        {
            _fidoConnection.Dispose();
        }

        [Fact]
        public void SetPin_Succeeds()
        {
            byte[] currentPin = new byte[] {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36
            };
            byte[] newPin = new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46
            };

            if (_fidoConnection is null)
            {
                return;
            }

            var cmd = new GetDeviceInfoCommand();
            GetDeviceInfoResponse rsp = _fidoConnection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            YubiKeyDeviceInfo getData = rsp.GetData();
            if (!getData.IsFipsSeries)
            {
                return;
            }

            var vfyCmd = new VerifyFipsModeCommand();
            VerifyFipsModeResponse vfyRsp = _fidoConnection.SendCommand(vfyCmd);
            Assert.Equal(ResponseStatus.Success, vfyRsp.Status);
            bool isFipsMode = vfyRsp.GetData();
            Assert.True(isFipsMode);

            var setCmd = new SetPinCommand(currentPin, newPin);
            SetPinResponse setRsp = _fidoConnection.SendCommand(setCmd);
            Assert.Equal(ResponseStatus.Success, setRsp.Status);

            setCmd = new SetPinCommand(newPin, currentPin);
            setRsp = _fidoConnection.SendCommand(setCmd);
            Assert.Equal(ResponseStatus.Success, setRsp.Status);

            setCmd = new SetPinCommand(newPin, currentPin);
            setRsp = _fidoConnection.SendCommand(setCmd);
            Assert.Equal(ResponseStatus.ConditionsNotSatisfied, setRsp.Status);
        }

        [Fact]
        public void InvalidPin_CorrectError()
        {
            byte[] currentPin = new byte[] {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36
            };
            byte[] badPin = new byte[] {
                0x41, 0x42, 0x43, 0x44
            };

            if (_fidoConnection is null)
            {
                return;
            }

            var setCmd = new SetPinCommand(currentPin, badPin);
            SetPinResponse setRsp = _fidoConnection.SendCommand(setCmd);
            Assert.NotEqual(ResponseStatus.Success, setRsp.Status);
        }

        [Fact]
        public void VerifyPin_Succeeds()
        {
            byte[] correctPin = new byte[] {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36
            };
            byte[] wrongPin = new byte[] {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37
            };

            if (_fidoConnection is null)
            {
                return;
            }

            var cmd = new GetDeviceInfoCommand();
            GetDeviceInfoResponse rsp = _fidoConnection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            YubiKeyDeviceInfo getData = rsp.GetData();
            if (!getData.IsFipsSeries)
            {
                return;
            }

            var vfyCmd = new VerifyPinCommand(correctPin);
            VerifyPinResponse vfyRsp = _fidoConnection.SendCommand(vfyCmd);
            Assert.Equal(ResponseStatus.Success, vfyRsp.Status);

            vfyCmd = new VerifyPinCommand(wrongPin);
            vfyRsp = _fidoConnection.SendCommand(vfyCmd);
            Assert.Equal(ResponseStatus.Failed, vfyRsp.Status);

            vfyCmd = new VerifyPinCommand(correctPin);
            vfyRsp = _fidoConnection.SendCommand(vfyCmd);
            Assert.Equal(ResponseStatus.Success, vfyRsp.Status);
        }

        // Run this test only if the PIN is indeed "123456", or the YubiKey has
        // not had the PIN set yet.
        // NOTE!!!!!
        // This test will make block the YubiKey's U2F application. The only way
        // to unblock is to reset, but once a U2F application has been reset, it
        // is not possible to put that YubiKey back into FIPS mode.
        [Fact]
        public void WrongPin_ThreeTimes()
        {
            byte[] correctPin = new byte[] {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36
            };
            byte[] wrongPin = new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46
            };

            bool isValid = IsYubiKeyVersion4Fips(out bool isFipsMode);
            Assert.True(isValid);
            if (!isFipsMode)
            {
                isValid = SetU2fPin(correctPin);
                Assert.True(isValid);
            }

            var vfyCmd = new VerifyPinCommand(correctPin);
            VerifyPinResponse vfyRsp = _fidoConnection.SendCommand(vfyCmd);
            Assert.Equal(ResponseStatus.Success, vfyRsp.Status);

            // Verify with the wrong PIN 3 times.
            // The first two times, the return should be SWConstants.VerifyFail.
            // The third time it should be
            // SWConstants.AuthenticationMethodBlocked.
            vfyCmd = new VerifyPinCommand(wrongPin);
            vfyRsp = _fidoConnection.SendCommand(vfyCmd);
            Assert.Equal(SWConstants.VerifyFail, vfyRsp.StatusWord);

            vfyCmd = new VerifyPinCommand(wrongPin);
            vfyRsp = _fidoConnection.SendCommand(vfyCmd);
            Assert.Equal(SWConstants.VerifyFail, vfyRsp.StatusWord);

            vfyCmd = new VerifyPinCommand(wrongPin);
            vfyRsp = _fidoConnection.SendCommand(vfyCmd);
            Assert.Equal(SWConstants.AuthenticationMethodBlocked, vfyRsp.StatusWord);

            // At this point, the YubiKey's U2F application is blocked and the
            // only way to unblock it is to reset.
        }

        private bool IsYubiKeyVersion4Fips(out bool isFipsMode)
        {
            isFipsMode = false;

            if (_fidoConnection is null)
            {
                return false;
            }

            var cmd = new GetDeviceInfoCommand();
            GetDeviceInfoResponse rsp = _fidoConnection.SendCommand(cmd);
            if (rsp.Status != ResponseStatus.Success)
            {
                return false;
            }

            YubiKeyDeviceInfo getData = rsp.GetData();
            if ((!getData.IsFipsSeries) ||
                (getData.FirmwareVersion >= new FirmwareVersion(5, 0, 0)) ||
                (getData.FirmwareVersion < new FirmwareVersion(4, 0, 0)))
            {
                return false;
            }

            var vfyCmd = new VerifyFipsModeCommand();
            VerifyFipsModeResponse vfyRsp = _fidoConnection.SendCommand(vfyCmd);
            if (vfyRsp.Status != ResponseStatus.Success)
            {
                return false;
            }

            isFipsMode = vfyRsp.GetData();

            return true;
        }

        // Set the PIN. That is, the U2F application is not in FIPS mode, so we
        // need to set the initial PIN.
        private bool SetU2fPin(byte[] newPin)
        {
            var setCmd = new SetPinCommand(ReadOnlyMemory<byte>.Empty, newPin);
            SetPinResponse setRsp = _fidoConnection.SendCommand(setCmd);

            return setRsp.Status == ResponseStatus.Success;
        }
    }
}
