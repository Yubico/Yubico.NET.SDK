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
using Xunit;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.Scp03;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class GetPutDataTests
    {
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void Cert_Auth_Req(StandardTestDevice testDeviceType)
        {
            var isValid = SampleKeyPairs.GetMatchingKeyAndCert(PivAlgorithm.Rsa2048,
                out var cert, out var privateKey);
            Assert.True(isValid);

            var certDer = cert.GetRawCertData();
            byte[] feData = { 0xFE, 0x00 };
            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(0x53))
            {
                tlvWriter.WriteValue(0x70, certDer);
                tlvWriter.WriteByte(0x71, 0);
                tlvWriter.WriteEncoded(feData);
            }

            var certData = tlvWriter.Encode();
            tlvWriter.Clear();

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();

                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();
                pivSession.ImportPrivateKey(PivSlot.Authentication, privateKey, PivPinPolicy.Never,
                    PivTouchPolicy.Never);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                // There should be no data.
                var getDataCommand = new GetDataCommand((int)PivDataTag.Authentication);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now put some data.
                // This should fail because the mgmt key is needed.
                var putDataCommand = new PutDataCommand((int)PivDataTag.Authentication, certData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
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
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand((int)PivDataTag.Authentication, certData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                // There should be data this time.
                var getDataCommand = new GetDataCommand((int)PivDataTag.Authentication);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                var getData = getDataResponse.GetData();
                Assert.Equal(certData.Length, getData.Length);
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void Chuid_Auth_Req(StandardTestDevice testDeviceType)
        {
            byte[] chuidData = {
                0x53, 0x3b, 0x30, 0x19, 0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39, 0xce, 0x73, 0x9d, 0x83,
                0x68, 0x58, 0x21, 0x08, 0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb, 0x34, 0x10, 0x39,
                0x38, 0x37, 0x36, 0x35, 0x34, 0x33, 0x32, 0x49, 0x48, 0x47, 0x46, 0x45, 0x44, 0x43, 0x42, 0x35,
                0x08, 0x32, 0x30, 0x33, 0x30, 0x30, 0x31, 0x30, 0x31, 0x3e, 0x00, 0xfe, 0x00
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();
            }

            using (var pivSession = new PivSession(testDevice, Scp03KeyParameters.DefaultKey))
            {
                // There should be no data.
                var getDataCommand = new GetDataCommand((int)PivDataTag.Chuid);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now put some data.
                // This should fail because the mgmt key is needed.
                var putDataCommand = new PutDataCommand((int)PivDataTag.Chuid, chuidData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
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
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand((int)PivDataTag.Chuid, chuidData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to read the CHUID without authenticating the mgmt key, nor
            // verifying the PIN. It should work.
            using (var pivSession = new PivSession(testDevice))
            {
                var getDataCommand = new GetDataCommand((int)PivDataTag.Chuid);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                var getData = getDataResponse.GetData();
                Assert.Equal(61, getData.Length);
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void Capability_Auth_Req(StandardTestDevice testDeviceType)
        {
            byte[] capabilityData = {
                0x53, 0x33, 0xF0, 0x15, 0xA0, 0x00, 0x00, 0x01, 0x16, 0xFF, 0x02, 0x21, 0x08, 0x42, 0x10, 0x84,
                0x21, 0xc8, 0x42, 0x10, 0xc3, 0xeb, 0x34, 0x10, 0x39, 0xF1, 0x01, 0x21, 0xF2, 0x01, 0x21, 0xF3,
                0x00, 0xF4, 0x01, 0x00, 0xF5, 0x01, 0x10, 0xF6, 0x00, 0xF7, 0x00, 0xFA, 0x00, 0xFB, 0x00, 0xFC,
                0x00, 0xFD, 0x00, 0xFE, 0x00
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();

                // There should be no data.
#pragma warning disable CS0618 // Testing an obsolete feature
                var getDataCommand = new GetDataCommand(PivDataTag.Capability);
#pragma warning restore CS0618 // Type or member is obsolete
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now put some data.
                // This should fail because the mgmt key is needed.
                var putDataCommand = new PutDataCommand((int)PivDataTag.Capability, capabilityData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
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
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

#pragma warning disable CS0618 // Testing an obsolete feature
                var putDataCommand = new PutDataCommand(PivDataTag.Capability, capabilityData);
#pragma warning restore CS0618 // Type or member is obsolete
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to read the CCC without authenticating the mgmt key, nor
            // verifying the PIN. It should work.
            using (var pivSession = new PivSession(testDevice))
            {
#pragma warning disable CS0618 // Testing an obsolete feature
                var getDataCommand = new GetDataCommand(PivDataTag.Capability);
#pragma warning restore CS0618 // Type or member is obsolete
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                var getData = getDataResponse.GetData();
                Assert.Equal(53, getData.Length);
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void Discovery_Auth_Req(StandardTestDevice testDeviceType)
        {
            byte[] discoveryData = {
                0x7E, 0x12, 0x4F, 0x0B, 0xA0, 0x00, 0x00, 0x03, 0x08, 0x00, 0x00, 0x10, 0x00, 0x01, 0x00, 0x5F,
                0x2F, 0x02, 0x40, 0x00,
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();

                // There should be data.
                var getDataCommand = new GetDataCommand((int)PivDataTag.Discovery);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                var getData = getDataResponse.GetData();
                Assert.Equal(20, getData.Length);

                // Now put some data.
                // This should throw an exception, it doesn't matter what has or
                // has not been verified/authenticated.
                _ = Assert.Throws<ArgumentException>(() =>
                    new PutDataCommand((int)PivDataTag.Discovery, discoveryData));
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void Printed_Auth_Req(StandardTestDevice testDeviceType)
        {
            byte[] printedData = {
                0x53, 0x04, 0x04, 0x02, 0xd4, 0xe7
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();

                // There should be no data, but even so, the error should be Auth Required.
                var getDataCommand = new GetDataCommand((int)PivDataTag.Printed);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                // Authenticate the mgmt key and try again. It should still
                // return the error Auth Required.
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }
            using (var pivSession = new PivSession(testDevice))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // Get the data. This time we should be able to see that there's
                // NoData.
                var getDataCommand = new GetDataCommand((int)PivDataTag.Printed);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                // Use the internal constructor to build the PutDataCommand. Then
                // put data into Printed.
                // With no PIN nor mgmt key, or with only the PIN, this should
                // fail.
                var putDataCommand = new PutDataCommand(0x5FC109, printedData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Now put after authenticating the mgmt key. This should work.
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(0x5FC109, printedData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to get the data with neither PIN nor mgmt key, then only the
            // mgmt key authenticated. These should fail.
            using (var pivSession = new PivSession(testDevice))
            {
                var getDataCommand = new GetDataCommand((int)PivDataTag.Printed);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            // Now get with the PIN. This should work.
            using (var pivSession = new PivSession(testDevice))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                var getDataCommand = new GetDataCommand((int)PivDataTag.Printed);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                var getData = getDataResponse.GetData();
                Assert.Equal(6, getData.Length);
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void Security_Auth_Req(StandardTestDevice testDeviceType)
        {
            byte[] securityData = {
                0x53, 0x08, 0xBA, 0x01, 0x11, 0xBB, 0x01, 0x22, 0xFE, 0x00
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();

                // There should be no data.
                var getDataCommand = new GetDataCommand((int)PivDataTag.SecurityObject);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now put some data.
                // This should fail because the mgmt key is needed.
                var putDataCommand = new PutDataCommand((int)PivDataTag.SecurityObject, securityData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
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
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand((int)PivDataTag.SecurityObject, securityData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to read the SecurityObject without authenticating the mgmt key, nor
            // verifying the PIN. It should work.
            using (var pivSession = new PivSession(testDevice))
            {
                var getDataCommand = new GetDataCommand((int)PivDataTag.SecurityObject);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                var getData = getDataResponse.GetData();
                Assert.Equal(10, getData.Length);
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void KeyHistory_Auth_Req(StandardTestDevice testDeviceType)
        {
            byte[] keyHistoryData = {
                0x53, 0x0A, 0xC1, 0x01, 0x00, 0xC2, 0x01, 0x00, 0xF3, 0x00, 0xFE, 0x00
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();

                // There should be no data.
                var getDataCommand = new GetDataCommand((int)PivDataTag.KeyHistory);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now put some data.
                // This should fail because the mgmt key is needed.
                var putDataCommand = new PutDataCommand((int)PivDataTag.KeyHistory, keyHistoryData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
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
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand((int)PivDataTag.KeyHistory, keyHistoryData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to read the KeyHistory without authenticating the mgmt key, nor
            // verifying the PIN. It should work.
            using (var pivSession = new PivSession(testDevice))
            {
                var getDataCommand = new GetDataCommand((int)PivDataTag.KeyHistory);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                var getData = getDataResponse.GetData();
                Assert.Equal(12, getData.Length);
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void Iris_Auth_Req(StandardTestDevice testDeviceType)
        {
            byte[] irisData = {
                0x53, 0x05, 0xBC, 0x01, 0x11, 0xFE, 0x00
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();

                // There should be no data, but even so, the error should be Auth Required.
                var getDataCommand = new GetDataCommand((int)PivDataTag.IrisImages);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                // Authenticate the mgmt key and try to get data again. It should still
                // return the error Auth Required.
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // Get the data. This time we should be able to see that there's
                // NoData.
                var getDataCommand = new GetDataCommand((int)PivDataTag.IrisImages);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                // Try to PUT some data into the space.
                // With no PIN nor mgmt key, or with only the PIN, this should
                // fail.
                var putDataCommand = new PutDataCommand((int)PivDataTag.IrisImages, irisData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Now put after authenticating the mgmt key. This should work.
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand((int)PivDataTag.IrisImages, irisData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to get the data with neither PIN nor mgmt key, then only the
            // mgmt key authenticated. These should fail.
            using (var pivSession = new PivSession(testDevice))
            {
                var getDataCommand = new GetDataCommand((int)PivDataTag.IrisImages);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            // Now get with the PIN. This should work.
            using (var pivSession = new PivSession(testDevice))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                var getDataCommand = new GetDataCommand((int)PivDataTag.IrisImages);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                var getData = getDataResponse.GetData();
                Assert.Equal(7, getData.Length);
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [InlineData(StandardTestDevice.Fw5)]
        public void Facial_Auth_Req(StandardTestDevice testDeviceType)
        {
            byte[] facialData = {
                0x53, 0x05, 0xBC, 0x01, 0x11, 0xFE, 0x00
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();

                // There should be no data, but even so, the error should be Auth Required.
                var getDataCommand = new GetDataCommand((int)PivDataTag.FacialImage);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                // Authenticate the mgmt key and try to get data again. It should still
                // return the error Auth Required.
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // Get the data. This time we should be able to see that there's
                // NoData.
                var getDataCommand = new GetDataCommand((int)PivDataTag.FacialImage);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                // Try to PUT some data into the space.
                // With no PIN nor mgmt key, or with only the PIN, this should
                // fail.
                var putDataCommand = new PutDataCommand((int)PivDataTag.FacialImage, facialData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Now put after authenticating the mgmt key. This should work.
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand((int)PivDataTag.FacialImage, facialData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to get the data with neither PIN nor mgmt key, then only the
            // mgmt key authenticated. These should fail.
            using (var pivSession = new PivSession(testDevice))
            {
                var getDataCommand = new GetDataCommand((int)PivDataTag.FacialImage);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            // Now get with the PIN. This should work.
            using (var pivSession = new PivSession(testDevice))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                var getDataCommand = new GetDataCommand((int)PivDataTag.FacialImage);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                var getData = getDataResponse.GetData();
                Assert.Equal(7, getData.Length);
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void Fingerprint_Auth_Req(StandardTestDevice testDeviceType)
        {
            byte[] fingerprintData = {
                0x53, 0x05, 0xBC, 0x01, 0x11, 0xFE, 0x00
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();

                // There should be no data, but even so, the error should be Auth Required.
                var getDataCommand = new GetDataCommand((int)PivDataTag.Fingerprints);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                // Authenticate the mgmt key and try to get data again. It should still
                // return the error Auth Required.
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                // Get the data. This time we should be able to see that there's
                // NoData.
                var getDataCommand = new GetDataCommand((int)PivDataTag.Fingerprints);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                // Try to PUT some data into the space.
                // With no PIN nor mgmt key, or with only the PIN, this should
                // fail.
                var putDataCommand = new PutDataCommand((int)PivDataTag.Fingerprints, fingerprintData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Now put after authenticating the mgmt key. This should work.
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand((int)PivDataTag.Fingerprints, fingerprintData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to get the data with neither PIN nor mgmt key, then only the
            // mgmt key authenticated. These should fail.
            using (var pivSession = new PivSession(testDevice))
            {
                var getDataCommand = new GetDataCommand((int)PivDataTag.Fingerprints);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);

                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, getDataResponse.Status);
            }

            // Now get with the PIN. This should work.
            using (var pivSession = new PivSession(testDevice))
            {
                // Verify the PIN
                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                var getDataCommand = new GetDataCommand((int)PivDataTag.Fingerprints);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                var getData = getDataResponse.GetData();
                Assert.Equal(7, getData.Length);
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void Bitgt_Auth_Req(StandardTestDevice testDeviceType)
        {
            byte[] bitgtData = {
                0x7F, 0x61, 0x07, 0x02, 0x01, 0x01, 0x7F, 0x60, 0x01, 0x01
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();

                // There should be no data.
#pragma warning disable CS0618 // Testing an obsolete feature
                var getDataCommand = new GetDataCommand(PivDataTag.BiometricGroupTemplate);
#pragma warning restore CS0618 // Type or member is obsolete
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now try to put some data.
                // This should throw an exception because the SDK does not allow
                // putting BITGT data.
#pragma warning disable CS0618 // Testing an obsolete feature
                _ = Assert.Throws<ArgumentException>(() =>
                    new PutDataCommand(PivDataTag.BiometricGroupTemplate, bitgtData));
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void SMSigner_Auth_Req(StandardTestDevice testDeviceType)
        {
            byte[] smSignerData = {
                0x53, 0x08, 0x70, 0x01, 0x11, 0x71, 0x01, 0x00, 0xFE, 0x00
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();

                // There is no auth required to get data, but there should be no
                // data at the moment.
                var getDataCommand = new GetDataCommand((int)PivDataTag.SecureMessageSigner);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                // Try to PUT some data into the space.
                // With no PIN nor mgmt key, or with only the PIN, this should
                // fail.
                var putDataCommand = new PutDataCommand((int)PivDataTag.SecureMessageSigner, smSignerData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Now put after authenticating the mgmt key. This should work.
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand((int)PivDataTag.SecureMessageSigner, smSignerData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to get the data with neither PIN nor mgmt key. It should
            // work.
            using (var pivSession = new PivSession(testDevice))
            {
                var getDataCommand = new GetDataCommand((int)PivDataTag.SecureMessageSigner);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                var getData = getDataResponse.GetData();
                Assert.Equal(10, getData.Length);
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void PCRef_Auth_Req(StandardTestDevice testDeviceType)
        {
            byte[] pcRefData = {
                0x53, 0x0C, 0x99, 0x08, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0xFE, 0x00
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();

                // There is no auth required to get data, but there should be no
                // data at the moment.
                var getDataCommand = new GetDataCommand((int)PivDataTag.PairingCodeReferenceData);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                // Try to PUT some data into the space.
                // With no PIN nor mgmt key, or with only the PIN, this should
                // fail.
                var putDataCommand = new PutDataCommand((int)PivDataTag.PairingCodeReferenceData, pcRefData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);

                pivSession.KeyCollector = PinOnlyKeyCollectorDelegate;
                pivSession.VerifyPin();

                putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.AuthenticationRequired, putDataResponse.Status);
            }

            // Now put after authenticating the mgmt key. This should work.
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand((int)PivDataTag.PairingCodeReferenceData, pcRefData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            // Now try to get the data with neither PIN nor mgmt key. It should
            // work.
            using (var pivSession = new PivSession(testDevice))
            {
                var getDataCommand = new GetDataCommand((int)PivDataTag.PairingCodeReferenceData);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                var getData = getDataResponse.GetData();
                Assert.Equal(14, getData.Length);
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        public void AdminData_Auth_Req(StandardTestDevice testDeviceType)
        {
            byte[] adminData = {
                0x53, 0x09, 0x80, 0x07, 0x81, 0x01, 0x00, 0x03, 0x02, 0x5C, 0x29
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                // There should be no data.
                var getDataCommand = new GetDataCommand(0x5FFF00);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.NoData, getDataResponse.Status);

                // Now put some data.
                // This should fail because the mgmt key is needed.
                var putDataCommand = new PutDataCommand(0x5FFF00, adminData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
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
            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.KeyCollector = MgmtKeyOnlyKeyCollectorDelegate;
                pivSession.AuthenticateManagementKey();

                var putDataCommand = new PutDataCommand(0x5FFF00, adminData);
                var putDataResponse = pivSession.Connection.SendCommand(putDataCommand);
                Assert.Equal(ResponseStatus.Success, putDataResponse.Status);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                // There should be data this time.
                var getDataCommand = new GetDataCommand(0x5FFF00);
                var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);
                Assert.Equal(ResponseStatus.Success, getDataResponse.Status);

                var getData = getDataResponse.GetData();
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

            if (keyEntryData.IsRetry)
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

            if (keyEntryData.IsRetry)
            {
                return false;
            }

            if (keyEntryData.Request != KeyEntryRequest.AuthenticatePivManagementKey)
            {
                return false;
            }

            keyEntryData.SubmitValue(new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            });

            return true;
        }
    }
}
