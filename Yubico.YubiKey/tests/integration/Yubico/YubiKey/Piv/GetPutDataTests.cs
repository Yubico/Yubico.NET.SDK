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
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;
using Yubico.Core.Tlv;
using Xunit;

namespace Yubico.YubiKey.Piv
{
    public class GetPutDataTests
    {
        [Fact]
        public void Cert_Auth_Req()
        {
            bool isValid = SampleKeyPairs.GetMatchingKeyAndCert(
                out X509Certificate2 cert, out PivPrivateKey privateKey);
            Assert.True(isValid);

            byte[] certDer = cert.GetRawCertData();
            byte[] feData = new byte[] { 0xFE, 0x00 };
            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(0x53))
            {
                tlvWriter.WriteValue(0x70, certDer);
                tlvWriter.WriteByte(0x71, 0);
                tlvWriter.WriteEncoded(feData);
            }
            byte[] certData = tlvWriter.Encode();
            tlvWriter.Clear();

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();
                pivSession.ImportPrivateKey(PivSlot.Authentication, privateKey, PivPinPolicy.Never, PivTouchPolicy.Never);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // There should be no data.
                var getDataCommand = new GetDataCommand(PivDataTag.Authentication);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now put some data.
                // This should fail because the mgmt key is needed.
                var putDataCommand = new PutDataCommand(PivDataTag.Authentication, certData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // With the PIN verified, this should still not work.
                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Create a new session so the PIN is no longer verified.
            // PUT DATA again, but this time with only the mgmt key authenticated.
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(PivDataTag.Authentication, certData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // There should be data this time.
                var getDataCommand = new GetDataCommand(PivDataTag.Authentication);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                ReadOnlyMemory<byte> getData = getDataResponse.GetData();
                Assert.Equal(certData.Length, getData.Length);
            }
        }

        [Fact]
        public void Chuid_Auth_Req()
        {
            byte[] chuidData = new byte[] {
                0x53, 0x3b, 0x30, 0x19, 0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39, 0xce, 0x73, 0x9d, 0x83,
                0x68, 0x58, 0x21, 0x08, 0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb, 0x34, 0x10, 0x39,
                0x38, 0x37, 0x36, 0x35, 0x34, 0x33, 0x32, 0x49, 0x48, 0x47, 0x46, 0x45, 0x44, 0x43, 0x42, 0x35,
                0x08, 0x32, 0x30, 0x33, 0x30, 0x30, 0x31, 0x30, 0x31, 0x3e, 0x00, 0xfe, 0x00
            };

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                // There should be no data.
                var getDataCommand = new GetDataCommand(PivDataTag.Chuid);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now put some data.
                // This should fail because the mgmt key is needed.
                var putDataCommand = new PutDataCommand(PivDataTag.Chuid, chuidData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // With the PIN verified, this should still not work.
                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Create a new session so the PIN is no longer verified.
            // PUT DATA again, but this time with only the mgmt key authenticated.
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(PivDataTag.Chuid, chuidData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to read the CHUID without authenicating the mgmt key, nor
            // verifying the PIN. It should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                var getDataCommand = new GetDataCommand(PivDataTag.Chuid);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                ReadOnlyMemory<byte> getData = getDataResponse.GetData();
                Assert.Equal(61, getData.Length);
            }
        }

        [Fact]
        public void Capability_Auth_Req()
        {
            byte[] capabilityData = new byte[] {
                0x53, 0x33, 0xF0, 0x15, 0xA0, 0x00, 0x00, 0x01, 0x16, 0xFF, 0x02, 0x21, 0x08, 0x42, 0x10, 0x84,
                0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb, 0x34, 0x10, 0x39, 0xF1, 0x01, 0x21, 0xF2, 0x01, 0x21, 0xF3,
                0x00, 0xF4, 0x01, 0x00, 0xF5, 0x01, 0x10, 0xF6, 0x00, 0xF7, 0x00, 0xFA, 0x00, 0xFB, 0x00, 0xFC,
                0x00, 0xFD, 0x00, 0xFE, 0x00
            };

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                // There should be no data.
                var getDataCommand = new GetDataCommand(PivDataTag.Capability);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now put some data.
                // This should fail because the mgmt key is needed.
                var putDataCommand = new PutDataCommand(PivDataTag.Capability, capabilityData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // With the PIN verified, this should still not work.
                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Create a new session so the PIN is no longer verified.
            // PUT DATA again, but this time with only the mgmt key authenticated.
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(PivDataTag.Capability, capabilityData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to read the CCC without authenicating the mgmt key, nor
            // verifying the PIN. It should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                var getDataCommand = new GetDataCommand(PivDataTag.Capability);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                ReadOnlyMemory<byte> getData = getDataResponse.GetData();
                Assert.Equal(53, getData.Length);
            }
        }

        [Fact]
        public void Discovery_Auth_Req()
        {
            byte[] discoveryData = new byte[] {
                0x7E, 0x12, 0x4F, 0x0B, 0xA0, 0x00, 0x00, 0x03, 0x08, 0x00, 0x00, 0x10, 0x00, 0x01, 0x00, 0x5F,
                0x2F, 0x02, 0x40, 0x00,
            };

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                // There should be data.
                var getDataCommand = new GetDataCommand(PivDataTag.Discovery);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                ReadOnlyMemory<byte> getData = getDataResponse.GetData();
                Assert.Equal(20, getData.Length);

                // Now put some data.
                // This should throw an exception, it doesn't matter what has or
                // has not been verified/authenticated.
                _ = Assert.Throws<ArgumentException>(() => new PutDataCommand(PivDataTag.Discovery, discoveryData));
            }
        }

        [Fact]
        public void Printed_Auth_Req()
        {
            byte[] printedData = new byte[] {
                0x53, 0x04, 0x04, 0x02, 0xd4, 0xe7
            };

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                // There should be no data, but even so, the error should be Auth Required.
                var getDataCommand = new GetDataCommand(PivDataTag.Printed);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                // Authenticate the mgmt key and try again. It should still
                // return the error Auth Required.
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // Get the data. This time we should be able to see that there's
                // NoData.
                var getDataCommand = new GetDataCommand(PivDataTag.Printed);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now try to put some data.
                // This should throw an exception because the SDK does not allow
                // putting with the DataTag of Printed.
                _ = Assert.Throws<ArgumentException>(() => new PutDataCommand(PivDataTag.Printed, printedData));
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // Use the internal constructor to build the PutDataCommand. Then
                // put data into Printed.
                // With no PIN nor mgmt key, or with only the PIN, this should
                // fail.
                var putDataCommand = new PutDataCommand(0x5FC109, printedData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Now put after authenticating the mgmt key. This should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(0x5FC109, printedData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to get the data with neither PIN nor mgmt key, then only the
            // mgmt key authenticated. These should fail.
            using (var pivSession = new PivSession(yubiKey))
            {
                var getDataCommand = new GetDataCommand(PivDataTag.Printed);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            // Now get with the PIN. This should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                var getDataCommand = new GetDataCommand(PivDataTag.Printed);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                ReadOnlyMemory<byte> getData = getDataResponse.GetData();
                Assert.Equal(6, getData.Length);
            }
        }

        [Fact]
        public void Security_Auth_Req()
        {
            byte[] securityData = new byte[] {
                0x53, 0x08, 0xBA, 0x01, 0x11, 0xBB, 0x01, 0x22, 0xFE, 0x00
            };

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                // There should be no data.
                var getDataCommand = new GetDataCommand(PivDataTag.SecurityObject);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now put some data.
                // This should fail because the mgmt key is needed.
                var putDataCommand = new PutDataCommand(PivDataTag.SecurityObject, securityData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // With the PIN verified, this should still not work.
                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Create a new session so the PIN is no longer verified.
            // PUT DATA again, but this time with only the mgmt key authenticated.
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(PivDataTag.SecurityObject, securityData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to read the SecurityObject without authenicating the mgmt key, nor
            // verifying the PIN. It should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                var getDataCommand = new GetDataCommand(PivDataTag.SecurityObject);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                ReadOnlyMemory<byte> getData = getDataResponse.GetData();
                Assert.Equal(10, getData.Length);
            }
        }

        [Fact]
        public void KeyHistory_Auth_Req()
        {
            byte[] keyHistoryData = new byte[] {
                0x53, 0x0A, 0xC1, 0x01, 0x00, 0xC2, 0x01, 0x00, 0xF3, 0x00, 0xFE, 0x00
            };

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                // There should be no data.
                var getDataCommand = new GetDataCommand(PivDataTag.KeyHistory);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now put some data.
                // This should fail because the mgmt key is needed.
                var putDataCommand = new PutDataCommand(PivDataTag.KeyHistory, keyHistoryData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // With the PIN verified, this should still not work.
                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Create a new session so the PIN is no longer verified.
            // PUT DATA again, but this time with only the mgmt key authenticated.
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(PivDataTag.KeyHistory, keyHistoryData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to read the KeyHistory without authenicating the mgmt key, nor
            // verifying the PIN. It should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                var getDataCommand = new GetDataCommand(PivDataTag.KeyHistory);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                ReadOnlyMemory<byte> getData = getDataResponse.GetData();
                Assert.Equal(12, getData.Length);
            }
        }

        [Fact]
        public void Iris_Auth_Req()
        {
            byte[] irisData = new byte[] {
                0x53, 0x05, 0xBC, 0x01, 0x11, 0xFE, 0x00
            };

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                // There should be no data, but even so, the error should be Auth Required.
                var getDataCommand = new GetDataCommand(PivDataTag.IrisImages);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                // Authenticate the mgmt key and try to get data again. It should still
                // return the error Auth Required.
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // Get the data. This time we should be able to see that there's
                // NoData.
                var getDataCommand = new GetDataCommand(PivDataTag.IrisImages);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // Try to PUT some data into the space.
                // With no PIN nor mgmt key, or with only the PIN, this should
                // fail.
                var putDataCommand = new PutDataCommand(PivDataTag.IrisImages, irisData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Now put after authenticating the mgmt key. This should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(PivDataTag.IrisImages, irisData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to get the data with neither PIN nor mgmt key, then only the
            // mgmt key authenticated. These should fail.
            using (var pivSession = new PivSession(yubiKey))
            {
                var getDataCommand = new GetDataCommand(PivDataTag.IrisImages);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            // Now get with the PIN. This should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                var getDataCommand = new GetDataCommand(PivDataTag.IrisImages);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                ReadOnlyMemory<byte> getData = getDataResponse.GetData();
                Assert.Equal(7, getData.Length);
            }
        }

        [Fact]
        public void Facial_Auth_Req()
        {
            byte[] facialData = new byte[] {
                0x53, 0x05, 0xBC, 0x01, 0x11, 0xFE, 0x00
            };

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                // There should be no data, but even so, the error should be Auth Required.
                var getDataCommand = new GetDataCommand(PivDataTag.FacialImage);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                // Authenticate the mgmt key and try to get data again. It should still
                // return the error Auth Required.
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // Get the data. This time we should be able to see that there's
                // NoData.
                var getDataCommand = new GetDataCommand(PivDataTag.FacialImage);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // Try to PUT some data into the space.
                // With no PIN nor mgmt key, or with only the PIN, this should
                // fail.
                var putDataCommand = new PutDataCommand(PivDataTag.FacialImage, facialData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Now put after authenticating the mgmt key. This should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(PivDataTag.FacialImage, facialData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to get the data with neither PIN nor mgmt key, then only the
            // mgmt key authenticated. These should fail.
            using (var pivSession = new PivSession(yubiKey))
            {
                var getDataCommand = new GetDataCommand(PivDataTag.FacialImage);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            // Now get with the PIN. This should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                var getDataCommand = new GetDataCommand(PivDataTag.FacialImage);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                ReadOnlyMemory<byte> getData = getDataResponse.GetData();
                Assert.Equal(7, getData.Length);
            }
        }

        [Fact]
        public void Fingerprint_Auth_Req()
        {
            byte[] fingerprintData = new byte[] {
                0x53, 0x05, 0xBC, 0x01, 0x11, 0xFE, 0x00
            };

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                // There should be no data, but even so, the error should be Auth Required.
                var getDataCommand = new GetDataCommand(PivDataTag.Fingerprints);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                // Authenticate the mgmt key and try to get data again. It should still
                // return the error Auth Required.
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // Get the data. This time we should be able to see that there's
                // NoData.
                var getDataCommand = new GetDataCommand(PivDataTag.Fingerprints);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // Try to PUT some data into the space.
                // With no PIN nor mgmt key, or with only the PIN, this should
                // fail.
                var putDataCommand = new PutDataCommand(PivDataTag.Fingerprints, fingerprintData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Now put after authenticating the mgmt key. This should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(PivDataTag.Fingerprints, fingerprintData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to get the data with neither PIN nor mgmt key, then only the
            // mgmt key authenticated. These should fail.
            using (var pivSession = new PivSession(yubiKey))
            {
                var getDataCommand = new GetDataCommand(PivDataTag.Fingerprints);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            // Now get with the PIN. This should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                var getDataCommand = new GetDataCommand(PivDataTag.Fingerprints);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                ReadOnlyMemory<byte> getData = getDataResponse.GetData();
                Assert.Equal(7, getData.Length);
            }
        }

        [Fact]
        public void Bitgt_Auth_Req()
        {
            byte[] bitgtData = new byte[] {
                0x7F, 0x61, 0x07, 0x02, 0x01, 0x01, 0x7F, 0x60, 0x01, 0x01
            };

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                // There should be no data.
                var getDataCommand = new GetDataCommand(PivDataTag.BiometricGroupTemplate);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now try to put some data.
                // This should throw an exception because the SDK does not allow
                // putting BITGT data.
                _ = Assert.Throws<ArgumentException>(() => new PutDataCommand(PivDataTag.BiometricGroupTemplate, bitgtData));
                _ = Assert.Throws<InvalidOperationException>(() => new PutDataCommand(0x7F61, bitgtData));
            }
        }

        [Fact]
        public void SMSigner_Auth_Req()
        {
            byte[] smSignerData = new byte[] {
                0x53, 0x08, 0x70, 0x01, 0x11, 0x71, 0x01, 0x00, 0xFE, 0x00
            };

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                // There is no auth required to get data, but there should be no
                // data at the moment.
                var getDataCommand = new GetDataCommand(PivDataTag.SecureMessageSigner);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // Try to PUT some data into the space.
                // With no PIN nor mgmt key, or with only the PIN, this should
                // fail.
                var putDataCommand = new PutDataCommand(PivDataTag.SecureMessageSigner, smSignerData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Now put after authenticating the mgmt key. This should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(PivDataTag.SecureMessageSigner, smSignerData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to get the data with neither PIN nor mgmt key. It should
            // work.
            using (var pivSession = new PivSession(yubiKey))
            {
                var getDataCommand = new GetDataCommand(PivDataTag.SecureMessageSigner);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                ReadOnlyMemory<byte> getData = getDataResponse.GetData();
                Assert.Equal(10, getData.Length);
            }
        }

        [Fact]
        public void PCRef_Auth_Req()
        {
            byte[] pcRefData = new byte[] {
                0x53, 0x0C, 0x99, 0x08, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0xFE, 0x00
            };

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                // There is no auth required to get data, but there should be no
                // data at the moment.
                var getDataCommand = new GetDataCommand(PivDataTag.PairingCodeReferenceData);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // Try to PUT some data into the space.
                // With no PIN nor mgmt key, or with only the PIN, this should
                // fail.
                var putDataCommand = new PutDataCommand(PivDataTag.PairingCodeReferenceData, pcRefData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Now put after authenticating the mgmt key. This should work.
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(PivDataTag.PairingCodeReferenceData, pcRefData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to get the data with neither PIN nor mgmt key. It should
            // work.
            using (var pivSession = new PivSession(yubiKey))
            {
                var getDataCommand = new GetDataCommand(PivDataTag.PairingCodeReferenceData);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                ReadOnlyMemory<byte> getData = getDataResponse.GetData();
                Assert.Equal(14, getData.Length);
            }
        }

        [Fact]
        public void AdminData_Auth_Req()
        {
            byte[] adminData = new byte[] {
                0x53, 0x09, 0x80, 0x07, 0x81, 0x01, 0x00, 0x03, 0x02, 0x5C, 0x29
            };

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                // There should be no data.
                var getDataCommand = new GetDataCommand(0x5FFF00);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now put some data.
                // This should fail because the mgmt key is needed.
                var putDataCommand = new PutDataCommand(0x5FFF00, adminData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // With the PIN verified, this should still not work.
                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Create a new session so the PIN is no longer verified.
            // PUT DATA again, but this time with only the mgmt key authenticated.
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(0x5FFF00, adminData);
                PutDataResponse putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // There should be data this time.
                var getDataCommand = new GetDataCommand(0x5FFF00);
                GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                ReadOnlyMemory<byte> getData = getDataResponse.GetData();
                Assert.Equal(11, getData.Length);
            }
        }

        // This will only return the PIN. Anything else and it returns false.
        // This is so we can run a test and specifically not authenticate the
        // mgmt key but verify the PIN.
        public static bool PinOnlyKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            if (keyEntryData.IsRetry == true)
            {
                return false;
            }

            if (keyEntryData.Request != KeyEntryRequest.VerifyPivPin)
            {
                return false;
            }

            keyEntryData.SubmitValue(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 });

            return true;
        }

        // This will only return the Mgmt Key. Anything else and it returns false.
        // This is so we can run a test and specifically not verify the PIN, but
        // authenticate the mgmt key.
        public static bool MgmtKeyOnlyKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            if (keyEntryData.IsRetry == true)
            {
                return false;
            }

            if (keyEntryData.Request != KeyEntryRequest.AuthenticatePivManagementKey)
            {
                return false;
            }

            keyEntryData.SubmitValue(new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            });

            return true;
        }
    }
}
