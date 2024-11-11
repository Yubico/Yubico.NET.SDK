// Copyright 2023 Yubico AB
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
using System.Security.Cryptography;
using Xunit;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;
using GetDataCommand = Yubico.YubiKey.Scp.Commands.GetDataCommand;

namespace Yubico.YubiKey.Scp
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class Scp03Tests
    {
        private IYubiKeyDevice GetDevice(
            StandardTestDevice desiredDeviceType,
            Transport transport = Transport.All, 
            FirmwareVersion? minimumFirmwareVersion = null) 
            => IntegrationTestDeviceEnumeration.GetTestDevice(desiredDeviceType, transport, minimumFirmwareVersion);

        private readonly ReadOnlyMemory<byte> _defaultPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

        public Scp03Tests()
        {
            ResetAllowedDevices();
        }

        private static void ResetAllowedDevices()
        {
            // Reset all attached allowed devices
            foreach (var availableDevice in IntegrationTestDeviceEnumeration.GetTestDevices())
            {
                using var session = new SecurityDomainSession(availableDevice);
                session.Reset();
            }
        }


        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_TestImportKey(StandardTestDevice desiredDeviceType)
        {
            byte[] sk =
            {
                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
            };

            var testDevice = GetDevice(desiredDeviceType);
            var newKeyParams = Scp03KeyParameters.FromStaticKeys(new StaticKeys(sk, sk, sk));

            // Authenticate with default key, then replace default key
            using (var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey))
            {
                session.PutKey(newKeyParams.KeyReference, newKeyParams.StaticKeys, 0);
            }

            using (_ = new SecurityDomainSession(testDevice, newKeyParams))
            {
            }

            // Default key should not work now and throw an exception
            Assert.Throws<ArgumentException>(() =>
            {
                using (_ = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey))
                {
                }
            });
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_PutKey_WithPublicKey_Succeeds(StandardTestDevice desiredDeviceType)
        {
            var keyReference = new KeyReference(ScpKeyIds.ScpCaPublicKey, 0x3);
            var testDevice = GetDevice(desiredDeviceType, Transport.All, FirmwareVersion.V5_7_2);

            using var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey);
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            var publicKey = new ECPublicKeyParameters(ecdsa);
            session.PutKey(keyReference, publicKey, 0);

            var keyInformation = session.GetKeyInformation();
            Assert.True(keyInformation.ContainsKey(keyReference));
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_AuthenticateWithWrongKey_Should_ThrowException(StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            var incorrectKeys = RandomStaticKeys();
            var keyRef = Scp03KeyParameters.FromStaticKeys(incorrectKeys);

            // Authentication with incorrect key should throw
            Assert.Throws<ArgumentException>(() =>
            {
                using (var session = new SecurityDomainSession(testDevice, keyRef)) { }
            });

            // Authentication with default key should succeed
            using (_ = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey))
            {
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_GetInformation_WithDefaultKey_Returns_DefaultKey(StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            using var session = new SecurityDomainSession(testDevice);

            var result = session.GetKeyInformation();
            if (testDevice.FirmwareVersion < FirmwareVersion.V5_7_2)
            {
                Assert.Equal(3, result.Count);
            }
            else
            {
                Assert.Equal(4, result.Count);
            }
            Assert.Equal(0xFF, result.Keys.First().VersionNumber);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_Connect_GetKeyInformation_WithDefaultKey_Returns_DefaultKey(StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            using var connection = testDevice.Connect(YubiKeyApplication.SecurityDomain);
            const byte TAG_KEY_INFORMATION = 0xE0;
            var response = connection.SendCommand(new GetDataCommand(TAG_KEY_INFORMATION));
            var result = response.GetData();

            var keyInformation = new Dictionary<KeyReference, Dictionary<byte, byte>>();

            foreach (var tlvObject in TlvObjects.DecodeList(result.Span))
            {
                var value = TlvObjects.UnpackValue(0xC0, tlvObject.GetBytes().Span);
                var keyRef = new KeyReference(value.Span[0], value.Span[1]);
                var keyComponents = new Dictionary<byte, byte>();

                // Iterate while there are more key components, each component is 2 bytes, so take 2 bytes at a time
                while (!(value = value[2..]).IsEmpty)
                {
                    keyComponents.Add(value.Span[0], value.Span[1]);
                }

                keyInformation.Add(keyRef, keyComponents);
            }

            Assert.NotEmpty(keyInformation);
            Assert.Equal(0xFF, keyInformation.Keys.First().VersionNumber); // 0xff, Default kvn

            if (testDevice.FirmwareVersion < FirmwareVersion.V5_7_2)
            {
                Assert.Equal(3, keyInformation.Keys.Count);
            }
            else
            {
                Assert.Equal(4, keyInformation.Keys.Count);
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_GetCertificates_ReturnsCerts(StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType, Transport.All, FirmwareVersion.V5_7_2);

            using var session = new SecurityDomainSession(testDevice);

            var keyReference = new KeyReference(ScpKeyIds.Scp11B, 0x1);
            var certificateList = session.GetCertificates(keyReference);

            Assert.NotEmpty(certificateList);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_Reset_Restores_SecurityDomainKeys_To_FactoryKeys(StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            byte[] sk =
            {
                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
            };

            var newKeyParams = new Scp03KeyParameters(
                ScpKeyIds.Scp03,
                0x01,
                new StaticKeys(sk, sk, sk));

            //  assumeFalse("SCP03 not supported over NFC on FIPS capable devices", TODO
            //     state.getDeviceInfo().getFipsCapable() != 0 && !state.isUsbTransport());

            using (var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey))
            {
                session.PutKey(newKeyParams.KeyReference, newKeyParams.StaticKeys, 0);
            }

            // Authentication with new key should succeed
            using (var session = new SecurityDomainSession(testDevice, newKeyParams))
            {
                session.GetKeyInformation();
            }

            // Default key should not work now and throw an exception
            Assert.Throws<ArgumentException>(() =>
            {
                using (_ = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey))
                {
                }
            });

            using (var session = new SecurityDomainSession(testDevice))
            {
                session.Reset();
            }

            // Successful authentication with default key means key has been restored to factory settings
            using (var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey))
            {
                _ = session.GetKeyInformation();
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_GetSupportedCaIdentifiers_Succeeds(StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType, Transport.All, FirmwareVersion.V5_7_2);
            using var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey);

            var result = session.GetSupportedCaIdentifiers(true, true);
            Assert.NotEmpty(result);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_GetCardRecognitionData_Succeeds(StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            using var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey);
            var result = session.GetCardRecognitionData();
            Assert.True(result.Length > 0);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_GetData_Succeeds(StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            using var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey);
            var result = session.GetData(0x66); // Card Data
            Assert.True(result.Length > 0);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_PivSession_TryVerifyPinAndGetMetaData_Succeeds(StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            Assert.True(testDevice.FirmwareVersion >= FirmwareVersion.V5_3_0);
            Assert.True(testDevice.HasFeature(YubiKeyFeature.Scp03));

            using var pivSession = new PivSession(testDevice, Scp03KeyParameters.DefaultKey);

            var result = pivSession.TryVerifyPin(_defaultPin, out _);

            Assert.True(result);

            var metadata = pivSession.GetMetadata(PivSlot.Pin)!;
            Assert.Equal(3, metadata.RetryCount);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_Device_Connect_With_Application_Succeeds(StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            Assert.True(testDevice.FirmwareVersion >= FirmwareVersion.V5_3_0);

            using var connection = testDevice.Connect(YubiKeyApplication.Piv, Scp03KeyParameters.DefaultKey);
            Assert.NotNull(connection);

            var cmd = new VerifyPinCommand(_defaultPin);
            var rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_Device_Connect_ApplicationId_Succeeds(StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            Assert.True(testDevice.FirmwareVersion >= FirmwareVersion.V5_3_0);

            using IYubiKeyConnection connection = testDevice.Connect(
                YubiKeyApplication.Piv.GetIso7816ApplicationId(), Scp03KeyParameters.DefaultKey);

            Assert.NotNull(connection);

            var cmd = new VerifyPinCommand(_defaultPin);
            var rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_Device_TryConnect_With_Application_Succeeds(StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            Assert.True(testDevice.FirmwareVersion >= FirmwareVersion.V5_3_0);

            var isValid = testDevice.TryConnect(
                YubiKeyApplication.Piv,
                Scp03KeyParameters.DefaultKey,
                out var connection);

            using (connection)
            {
                Assert.NotNull(connection);
                Assert.True(isValid);
                var cmd = new VerifyPinCommand(_defaultPin);
                var rsp = connection!.SendCommand(cmd);
                Assert.Equal(ResponseStatus.Success, rsp.Status);
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp03_Device_TryConnect_With_ApplicationId_Succeeds(StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            Assert.True(testDevice.FirmwareVersion >= FirmwareVersion.V5_3_0);

            var isValid = testDevice.TryConnect(YubiKeyApplication.Piv, Scp03KeyParameters.DefaultKey,
                out var connection);

            using (connection)
            {
                Assert.True(isValid);
                Assert.NotNull(connection);
                var cmd = new VerifyPinCommand(_defaultPin);
                var rsp = connection!.SendCommand(cmd);
                Assert.Equal(ResponseStatus.Success, rsp.Status);
            }
        }

        #region Helpers

        private static StaticKeys RandomStaticKeys() =>
            new StaticKeys(
                GetRandom16Bytes(),
                GetRandom16Bytes(),
                GetRandom16Bytes()
            );

        private static ReadOnlyMemory<byte> GetRandom16Bytes()
        {
            var buffer = new byte[16];
            Random.Shared.NextBytes(buffer);
            return buffer;
        }

        #endregion
    }
}
