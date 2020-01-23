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
using System.Security.Cryptography;
using Yubico.YubiKey.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace Yubico.YubiKey.Piv
{
    public class ManagementKeyTests
    {
        private readonly ITestOutputHelper _output;
        private readonly byte[] _currentKey;
        private readonly byte[] _newKey;

        public ManagementKeyTests (ITestOutputHelper output)
        {
            _output = output;
            _currentKey = new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };
            _newKey = new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };
        }

        [Fact]
        public void RandomKey_Authenticates()
        {
            int count = 10;

            bool isValid = false;
            for (int index = 0; index < count; index++)
            {
                GetRandomMgmtKey();
                isValid = ChangeMgmtKey();
                if (isValid == false)
                {
                    break;
                }

                isValid = VerifyMgmtKey(false);
                if (isValid == false)
                {
                    break;
                }

                isValid = VerifyMgmtKey(true);
                if (isValid == false)
                {
                    break;
                }
            }

            ResetPiv();

            Assert.True(isValid);
        }

        private bool VerifyMgmtKey(bool isMutual)
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = TestKeyCollectorDelegate;
                return pivSession.TryAuthenticateManagementKey(isMutual);
            }
        }

        private bool ChangeMgmtKey()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = TestKeyCollectorDelegate;
                bool isChanged = pivSession.TryChangeManagementKey();
                if (isChanged)
                {
                    Array.Copy(_newKey, _currentKey, 24);
                }

                return isChanged;
            }
        }

        private static void ResetPiv()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();
            }
        }

        private void GetRandomMgmtKey()
        {
            using RandomNumberGenerator random = RandomObjectUtility.GetRandomObject(null);
            random.GetBytes(_newKey);
        }

        public bool TestKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            if (keyEntryData.IsRetry == true)
            {
                _output.WriteLine ("Retry");
                return false;
            }

            switch (keyEntryData.Request)
            {
                default:
                    return false;

                case KeyEntryRequest.Release:
                    break;

                case KeyEntryRequest.AuthenticatePivManagementKey:
                    keyEntryData.SubmitValue(_currentKey);
                    break;

                case KeyEntryRequest.ChangePivManagementKey:
                    keyEntryData.SubmitValues(_currentKey, _newKey);
                    break;
            }

            return true;
        }
    }
}
