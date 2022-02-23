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
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.TestUtilities;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Piv.Objects;
using Yubico.Core.Tlv;
using Xunit;

namespace Yubico.YubiKey.Piv
{
    public class ChangePinTests
    {
        [Fact]
        public void NoAdminData_ChangePin_NoUpdate()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector(true);
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    pivSession.ChangePin();

                    AdminData adminData = pivSession.ReadObject<AdminData>();

                    Assert.True(adminData.IsEmpty);
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Fact]
        public void AdminData_ChangePin_Updated()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector(true);
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    var adminData = new AdminData
                    {
                        PinProtected = true
                    };
                    pivSession.WriteObject(adminData);

                    pivSession.ChangePin();

                    adminData = pivSession.ReadObject<AdminData>();

                    Assert.False(adminData.IsEmpty);
                    Assert.True(adminData.PinProtected);
                    Assert.NotNull(adminData.PinLastUpdated);
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Fact]
        public void AdminData_ResetPin_Updated()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector(true);
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    var adminData = new AdminData
                    {
                        PinProtected = true
                    };
                    pivSession.WriteObject(adminData);

                    pivSession.ResetPin();

                    adminData = pivSession.ReadObject<AdminData>();

                    Assert.False(adminData.IsEmpty);
                    Assert.True(adminData.PinProtected);
                    Assert.NotNull(adminData.PinLastUpdated);
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Fact]
        public void AdminData_ResetRetry_Updated()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector(true);
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    var adminData = new AdminData
                    {
                        PinProtected = true
                    };
                    pivSession.WriteObject(adminData);

                    pivSession.ChangePinAndPukRetryCounts(5, 5);

                    adminData = pivSession.ReadObject<AdminData>();

                    Assert.False(adminData.IsEmpty);
                    Assert.True(adminData.PinProtected);
                    Assert.NotNull(adminData.PinLastUpdated);
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Fact]
        public void AdminData_ChangePuk_NoUpdate()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector(true);
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    var adminData = new AdminData
                    {
                        PinProtected = true
                    };
                    pivSession.WriteObject(adminData);

                    pivSession.ChangePuk();

                    adminData = pivSession.ReadObject<AdminData>();

                    Assert.False(adminData.IsEmpty);
                    Assert.True(adminData.PinProtected);
                    Assert.Null(adminData.PinLastUpdated);
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Fact]
        public void AdminData_ChangeMgmtKey_NoUpdate()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector(true);
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    var adminData = new AdminData
                    {
                        PinProtected = true
                    };
                    pivSession.WriteObject(adminData);

                    pivSession.ChangeManagementKey();

                    adminData = pivSession.ReadObject<AdminData>();

                    Assert.False(adminData.IsEmpty);
                    Assert.True(adminData.PinProtected);
                    Assert.Null(adminData.PinLastUpdated);
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }
    }
}
