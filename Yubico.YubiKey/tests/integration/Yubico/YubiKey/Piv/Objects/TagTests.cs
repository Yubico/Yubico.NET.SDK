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
    public class TagTests
    {
        [Fact]
        public void AlternateTag_Minimum_Succeeds()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    pivSession.AuthenticateManagementKey();

                    byte[] arbitraryData = new byte[] {
                        0x53, 0x02, 0x04, 0x00
                    };

                    var putCmd = new PutDataCommand(0x005F0000, arbitraryData);
                    PutDataResponse putRsp = pivSession.Connection.SendCommand(putCmd);

                    Assert.Equal(ResponseStatus.Success, putRsp.Status);

                    var getCmd = new GetDataCommand(0x005F0000);
                    GetDataResponse getRsp = pivSession.Connection.SendCommand(getCmd);

                    Assert.Equal(ResponseStatus.Success, getRsp.Status);

                    ReadOnlyMemory<byte> theData = getRsp.GetData();

                    bool isValid = MemoryExtensions.SequenceEqual<byte>(arbitraryData, theData.Span);
                    Assert.True(isValid);
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Fact]
        public void AlternateTag_Invalid_Error()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    pivSession.AuthenticateManagementKey();

                    byte[] arbitraryData = new byte[] {
                        0x53, 0x02, 0x04, 0x00
                    };

                    PutDataCommand putCmd;
                    _ = Assert.Throws<ArgumentException>(() => putCmd = new PutDataCommand(0x005EFFFF, arbitraryData));

                    GetDataCommand getCmd;
                    _ = Assert.Throws<ArgumentException>(() => getCmd = new GetDataCommand(0x005EFFFF));
                }
                finally
                {
                    pivSession.ResetApplication();
                }
            }
        }
    }
}
