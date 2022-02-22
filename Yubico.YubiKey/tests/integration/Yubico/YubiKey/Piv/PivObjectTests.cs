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
using Yubico.YubiKey.TestUtilities;
using Yubico.YubiKey.Piv.Objects;
using Xunit;

namespace Yubico.YubiKey.Piv
{
    public class PivObjectTests
    {
        [Fact]
        public void ReadChuid_IsEmpty_Correct()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                CardholderUniqueId chuid = pivSession.ReadObject<CardholderUniqueId>();

                Assert.True(chuid.IsEmpty);
            }
        }

        [Fact]
        public void WriteThenReadChuid_Data_Correct()
        {
            var expected = new ReadOnlySpan<byte>(new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01
            });
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            try
            {
                using (var pivSession = new PivSession(yubiKey))
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    CardholderUniqueId chuid = pivSession.ReadObject<CardholderUniqueId>();
                    Assert.True(chuid.IsEmpty);

                    chuid.SetGuid(expected);

                    pivSession.WriteObject(chuid);

                    chuid = pivSession.ReadObject<CardholderUniqueId>();
                    Assert.False(chuid.IsEmpty);

                    bool isValid = MemoryExtensions.SequenceEqual<byte>(expected, chuid.GuidValue.Span);
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
        public void AltTag_WriteThenReadChuid_Data_Correct()
        {
            var expected = new ReadOnlySpan<byte>(new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01
            });
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            try
            {
                using (var pivSession = new PivSession(yubiKey))
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    CardholderUniqueId chuid = pivSession.ReadObject<CardholderUniqueId>();
                    Assert.True(chuid.IsEmpty);

                    chuid.SetGuid(expected);
                    chuid.DataTag = 0x5F0010;

                    pivSession.WriteObject(chuid);

                    chuid = pivSession.ReadObject<CardholderUniqueId>(0x5F0010);
                    Assert.False(chuid.IsEmpty);

                    bool isValid = MemoryExtensions.SequenceEqual<byte>(expected, chuid.GuidValue.Span);
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
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01
            });
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            try
            {
                using (var pivSession = new PivSession(yubiKey))
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    CardholderUniqueId emptyChuid = pivSession.ReadObject<CardholderUniqueId>();
                    Assert.True(emptyChuid.IsEmpty);

                    // Write an empty object.
                    pivSession.WriteObject(emptyChuid);

                    // Make sure the contents are still empty.
                    CardholderUniqueId chuid = pivSession.ReadObject<CardholderUniqueId>();
                    Assert.True(chuid.IsEmpty);

                    // Now write a CHUID with data.
                    chuid.SetGuid(expected);
                    pivSession.WriteObject(chuid);

                    // Make sure that worked.
                    chuid = pivSession.ReadObject<CardholderUniqueId>();
                    Assert.False(chuid.IsEmpty);
                    bool isValid = MemoryExtensions.SequenceEqual<byte>(expected, chuid.GuidValue.Span);
                    Assert.True(isValid);

                    // Now write an empty object.
                    pivSession.WriteObject(emptyChuid);

                    // Make sure the contents were unchanged.
                    chuid = pivSession.ReadObject<CardholderUniqueId>();
                    Assert.False(chuid.IsEmpty);
                    isValid = MemoryExtensions.SequenceEqual<byte>(expected, chuid.GuidValue.Span);
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

                _ = Assert.Throws<ArgumentException>(() => pivSession.ReadObject<CardholderUniqueId>(newTag));
            }
        }

        [Fact]
        public void Write_NullArg_Throws()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                _ = Assert.Throws<ArgumentNullException>(() => pivSession.WriteObject(null));
#pragma warning restore CS8625 // Suppressed so we can test a null input.
            }
        }
    }
}
