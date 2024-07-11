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
using System.Linq;
using Xunit;
using Yubico.PlatformInterop;

namespace Yubico.YubiKey.U2f
{
    public class SessionPinTests
    {
        private readonly IYubiKeyDevice _yubiKeyDevice;

        public SessionPinTests()
        {
            if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows)
            {
                if (!SdkPlatformInfo.IsElevated)
                {
                    throw new ArgumentException("Windows not elevated.");
                }
            }

            IEnumerable<IYubiKeyDevice> yubiKeys = YubiKeyDevice.FindByTransport(Transport.HidFido);
            var yubiKeyList = yubiKeys.ToList();
            Assert.NotEmpty(yubiKeyList);

            _yubiKeyDevice = yubiKeyList[0];
        }

        [Fact]
        public void ChangePin_Succeeds()
        {
            var keyCollector = new SimpleU2fKeyCollector(true);

            using (var u2fSession = new U2fSession(_yubiKeyDevice))
            {
                u2fSession.KeyCollector = keyCollector.SimpleU2fKeyCollectorDelegate;

                // Change the PIN.
                u2fSession.ChangePin();

                // Change it back.
                u2fSession.ChangePin();
            }
        }

        [Fact]
        public void TryChangePin_NoCollector_Succeeds()
        {
            byte[] currentPin =
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36
            };
            byte[] newPin =
            {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46
            };
            byte[] shortPin =
            {
                0x61, 0x62, 0x63, 0x64, 0x65
            };

            using (var u2fSession = new U2fSession(_yubiKeyDevice))
            {
                // use wrong PIN.
                bool isChanged = u2fSession.TryChangePin(newPin, currentPin);
                Assert.False(isChanged);

                // Change the PIN.
                isChanged = u2fSession.TryChangePin(currentPin, newPin);
                Assert.True(isChanged);

                // Use bad new PIN
                isChanged = u2fSession.TryChangePin(newPin, shortPin);
                Assert.False(isChanged);

                // Change it back.
                isChanged = u2fSession.TryChangePin(newPin, currentPin);
                Assert.True(isChanged);
            }
        }

        [Fact]
        public void VerifyPin_Succeeds()
        {
            var keyCollector = new SimpleU2fKeyCollector(true);

            using (var u2fSession = new U2fSession(_yubiKeyDevice))
            {
                u2fSession.KeyCollector = keyCollector.SimpleU2fKeyCollectorDelegate;

                u2fSession.VerifyPin();
            }
        }

        [Fact]
        public void TryVerifyPin_NoCollector_Succeeds()
        {
            byte[] currentPin =
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36
            };
            byte[] wrongPin =
            {
                0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18
            };
            byte[] shortPin =
            {
                0x61, 0x62, 0x63, 0x64, 0x65
            };

            using (var u2fSession = new U2fSession(_yubiKeyDevice))
            {
                // Wrong PIN
                bool isVerified = u2fSession.TryVerifyPin(wrongPin);
                Assert.False(isVerified);

                // Short PIN
                isVerified = u2fSession.TryVerifyPin(shortPin);
                Assert.False(isVerified);

                isVerified = u2fSession.TryVerifyPin(currentPin);
                Assert.True(isVerified);
            }
        }
    }
}
