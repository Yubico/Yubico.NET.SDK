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

using System.Security;
using Yubico.YubiKey.TestUtilities;
using Xunit;

namespace Yubico.YubiKey.Piv
{
    public class AuthTests
    {
        [Fact]
        public void VerifyPin_Blocked_ThrowsSecurityException()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector
                    {
                        KeyFlag = 1,
                        RetryFlag = 1
                    };
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    _ = Assert.Throws<SecurityException>(() => pivSession.VerifyPin());
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Fact]
        public void ChangePin_Blocked_ThrowsSecurityException()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector
                    {
                        KeyFlag = 1,
                        RetryFlag = 1
                    };
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    _ = Assert.Throws<SecurityException>(() => pivSession.ChangePin());
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Fact]
        public void ChangePuk_Blocked_ThrowsSecurityException()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector
                    {
                        KeyFlag = 1,
                        RetryFlag = 1
                    };
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    _ = Assert.Throws<SecurityException>(() => pivSession.ChangePuk());
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Fact]
        public void ResetPin_Blocked_ThrowsSecurityException()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector
                    {
                        KeyFlag = 1,
                        RetryFlag = 1
                    };
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    _ = Assert.Throws<SecurityException>(() => pivSession.ResetPin());
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }
    }
}
