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
    public class CccIntegrationTests
    {
        [Fact]
        public void ReadCcc_IsEmpty_Correct()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                CardCapabilityContainer ccc = pivSession.ReadObject<CardCapabilityContainer>();

                Assert.True(ccc.IsEmpty);
            }
        }

        [Fact]
        public void WriteThenReadCcc_Data_Correct()
        {
            var expected = new ReadOnlySpan<byte>(new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03
            });
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            try
            {
                using (var pivSession = new PivSession(yubiKey))
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    CardCapabilityContainer ccc = pivSession.ReadObject<CardCapabilityContainer>();
                    Assert.True(ccc.IsEmpty);

                    ccc.SetCardId(expected);

                    pivSession.WriteObject(ccc);

                    ccc = pivSession.ReadObject<CardCapabilityContainer>();
                    Assert.False(ccc.IsEmpty);

                    bool isValid = MemoryExtensions.SequenceEqual<byte>(expected, ccc.CardIdentifier.Span);
                    Assert.True(isValid);
                }
            }
            finally
            {
                using (var pivSession = new PivSession(yubiKey))
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Fact]
        public void AltTag_WriteThenReadCcc_Data_Correct()
        {
            var expected = new ReadOnlySpan<byte>(new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03
            });
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            try
            {
                using (var pivSession = new PivSession(yubiKey))
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    CardCapabilityContainer ccc = pivSession.ReadObject<CardCapabilityContainer>();
                    Assert.True(ccc.IsEmpty);

                    ccc.SetCardId(expected);
                    ccc.DataTag = 0x5F1110;

                    pivSession.WriteObject(ccc);

                    ccc = pivSession.ReadObject<CardCapabilityContainer>(0x5F1110);
                    Assert.False(ccc.IsEmpty);

                    bool isValid = MemoryExtensions.SequenceEqual<byte>(expected, ccc.CardIdentifier.Span);
                    Assert.True(isValid);
                }
            }
            finally
            {
                using (var pivSession = new PivSession(yubiKey))
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Fact]
        public void WriteEmpty_Correct()
        {
            var expected = new ReadOnlySpan<byte>(new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03
            });
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            try
            {
                using (var pivSession = new PivSession(yubiKey))
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    CardCapabilityContainer emptyCcc = pivSession.ReadObject<CardCapabilityContainer>();
                    Assert.True(emptyCcc.IsEmpty);

                    // Write an empty object.
                    pivSession.WriteObject(emptyCcc);

                    // Make sure the contents are still empty.
                    CardCapabilityContainer ccc = pivSession.ReadObject<CardCapabilityContainer>();
                    Assert.True(ccc.IsEmpty);

                    // Now write a CCC with data.
                    ccc.SetCardId(expected);
                    pivSession.WriteObject(ccc);

                    // Make sure that worked.
                    ccc = pivSession.ReadObject<CardCapabilityContainer>();
                    Assert.False(ccc.IsEmpty);
                    bool isValid = MemoryExtensions.SequenceEqual<byte>(expected, ccc.CardIdentifier.Span);
                    Assert.True(isValid);

                    // Now write an empty object.
                    pivSession.WriteObject(emptyCcc);

                    // Make sure the contents were emptied.
                    ccc = pivSession.ReadObject<CardCapabilityContainer>();
                    Assert.True(ccc.IsEmpty);
                }
            }
            finally
            {
                using (var pivSession = new PivSession(yubiKey))
                {
                    pivSession.ResetApplication();
                }
            }
        }

        [Theory]
        [InlineData(0x015FFF10)]
        [InlineData(0x0000007E)]
        [InlineData(0x00007F61)]
        [InlineData(0x005FC101)]
        [InlineData(0x005FC104)]
        [InlineData(0x005FC105)]
        [InlineData(0x005FC10A)]
        [InlineData(0x005FC10B)]
        [InlineData(0x005FC10D)]
        [InlineData(0x005FC120)]
        [InlineData(0x005FFF01)]
        public void Read_InvalidTag_Throws(int newTag)
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                _ = Assert.Throws<ArgumentException>(() => pivSession.ReadObject<CardCapabilityContainer>(newTag));
            }
        }
    }
}
