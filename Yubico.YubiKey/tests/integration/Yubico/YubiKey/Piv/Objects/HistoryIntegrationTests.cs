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
    public class HistoryIntegrationTests
    {
        [Fact]
        public void ReadHistory_IsEmpty_Correct()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                KeyHistory history = pivSession.ReadObject<KeyHistory>();

                Assert.True(history.IsEmpty);
            }
        }

        [Fact]
        public void WriteThenReadHistory_Data_Correct()
        {
            var expected = new ReadOnlyMemory<byte>(new byte[] {
                0x53, 0x1B,
                      0xC1, 0x01, 0x01, 0xC2, 0x01, 0x02,
                      0xF3, 0x11,
                            0x66, 0x69, 0x6c, 0x65, 0x3a, 0x2f, 0x2f, 0x75, 0x73, 0x65, 0x72, 0x2f, 0x63, 0x65, 0x72, 0x74, 0x73,
                      0xFE, 0x00
            });
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            try
            {
                using (var pivSession = new PivSession(yubiKey))
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    KeyHistory history = pivSession.ReadObject<KeyHistory>();
                    Assert.True(history.IsEmpty);

                    history.OnCardCertificates = 1;
                    history.OffCardCertificates = 2;
                    string urlString = "file://user/certs";
                    history.OffCardCertificateUrl = new Uri(urlString);

                    pivSession.WriteObject(history);

                    KeyHistory historyCopy = pivSession.ReadObject<KeyHistory>();
                    Assert.False(historyCopy.IsEmpty);
                    Assert.Equal(1, historyCopy.OnCardCertificates);
                    Assert.Equal(2, historyCopy.OffCardCertificates);
                    Assert.NotNull(historyCopy.OffCardCertificateUrl);
                    if (!(historyCopy.OffCardCertificateUrl is null))
                    {
                        int cmpResult = urlString.CompareTo(historyCopy.OffCardCertificateUrl.AbsoluteUri);
                        Assert.Equal(0, cmpResult);
                    }

                    byte[] encodedHistory = historyCopy.Encode();

                    bool isValid = MemoryExtensions.SequenceEqual<byte>(expected.Span, encodedHistory);
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
        public void WriteEmpty_ThenData_Correct()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            try
            {
                using (var pivSession = new PivSession(yubiKey))
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ResetApplication();

                    KeyHistory history = pivSession.ReadObject<KeyHistory>();
                    Assert.True(history.IsEmpty);

                    history.OnCardCertificates = 0;
                    pivSession.WriteObject(history);

                    KeyHistory historyCopy = pivSession.ReadObject<KeyHistory>();
                    Assert.False(historyCopy.IsEmpty);
                    Assert.Equal(0, historyCopy.OnCardCertificates);
                    Assert.Equal(0, historyCopy.OffCardCertificates);
                    Assert.Null(historyCopy.OffCardCertificateUrl);

                    history.OnCardCertificates = 1;
                    history.OffCardCertificates = 2;
                    string urlString = "file://user/certs";
                    history.OffCardCertificateUrl = new Uri(urlString);

                    pivSession.WriteObject(history);

                    historyCopy = pivSession.ReadObject<KeyHistory>();
                    Assert.False(historyCopy.IsEmpty);
                    Assert.Equal(1, historyCopy.OnCardCertificates);
                    Assert.Equal(2, historyCopy.OffCardCertificates);
                    Assert.NotNull(historyCopy.OffCardCertificateUrl);
                    if (!(historyCopy.OffCardCertificateUrl is null))
                    {
                        int cmpResult = urlString.CompareTo(historyCopy.OffCardCertificateUrl.AbsoluteUri);
                        Assert.Equal(0, cmpResult);
                    }
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
    }
}
