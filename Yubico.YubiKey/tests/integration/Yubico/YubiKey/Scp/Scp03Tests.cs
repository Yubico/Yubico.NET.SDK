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
        private readonly ReadOnlyMemory<byte> _defaultPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
        private IYubiKeyDevice Device { get; set; }

        public Scp03Tests()
        {
            Device = IntegrationTestDeviceEnumeration.GetTestDevice(
                Transport.SmartCard,
                minimumFirmwareVersion: FirmwareVersion.V5_3_0);

            using var session = new SecurityDomainSession(Device);
            session.Reset();
        }

        [Fact]
        public void TestImportKey()
        {
            byte[] sk =
            {
                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
            };

            var newKeyParams = Scp03KeyParameters.FromStaticKeys(new StaticKeys(sk, sk, sk));

            // Authenticate with default key, then replace default key
            using (var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {
                session.PutKey(newKeyParams.KeyReference, newKeyParams.StaticKeys, 0);
            }

            using (_ = new SecurityDomainSession(Device, newKeyParams))
            {
            }

            // Default key should not work now and throw an exception
            Assert.Throws<ArgumentException>(() =>
            {
                using (_ = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
                {
                }
            });
        }


        [Fact]
        public void PutKey_WithPublicKey_Succeeds()
        {
            var keyReference = new KeyReference(0x10, 0x3);

            using var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey);
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            var publicKey = new ECPublicKeyParameters(ecdsa);
            session.PutKey(keyReference, publicKey, 0);

            var keyInformation = session.GetKeyInformation();
            Assert.True(keyInformation.ContainsKey(keyReference));
        }

        [Fact]
        public void TestDeleteKey()
        {
            var keyRef1 = new Scp03KeyParameters(ScpKid.Scp03, 0x10, RandomStaticKeys());
            var keyRef2 = new Scp03KeyParameters(ScpKid.Scp03, 0x55, RandomStaticKeys());

            // Auth with default key, then replace default key
            using (var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {
                session.PutKey(keyRef1.KeyReference, keyRef1.StaticKeys, 0);
            }

            // Authenticate with key1, then add additional key, keyref2
            using (var session = new SecurityDomainSession(Device, keyRef1))
            {
                session.PutKey(keyRef2.KeyReference, keyRef2.StaticKeys, 0);
            }

            // Authenticate with key2, delete key 1
            using (var session = new SecurityDomainSession(Device, keyRef2))
            {
                session.DeleteKey(keyRef1.KeyReference);
            }

            // Authenticate with key 1, 
            // Should throw because we just deleted it
            Assert.Throws<ArgumentException>(() =>
            {
                using (_ = new SecurityDomainSession(Device, keyRef1))
                {
                }
            });

            using (var session = new SecurityDomainSession(Device, keyRef2))
            {
                session.DeleteKey(keyRef2.KeyReference, true);
            }

            // Try to authenticate with key 2, 
            // Should throw because we just deleted the last key
            Assert.Throws<ArgumentException>(() =>
            {
                using (_ = new SecurityDomainSession(Device, keyRef2))
                {
                }
            });
        }

        [Fact]
        public void TestReplaceKey()
        {
            var keyRef1 = new Scp03KeyParameters(ScpKid.Scp03, 0x10, RandomStaticKeys());
            var keyRef2 = new Scp03KeyParameters(ScpKid.Scp03, 0x10, RandomStaticKeys());

            // Authenticate with default key, then replace default key
            using (var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {
                session.PutKey(keyRef1.KeyReference, keyRef1.StaticKeys, 0);
            }
            // Authenticate with key1, then add additional key, keyref2
            using (var session = new SecurityDomainSession(Device, keyRef1))
            {
                session.PutKey(keyRef2.KeyReference, keyRef2.StaticKeys, keyRef1.KeyReference.VersionNumber);
            }

            // Authentication with new key 2 should succeed
            using (_ = new SecurityDomainSession(Device, keyRef2))
            {
            }

            // The ssession throws a SecureChannelException if the attempted key is incorrect --
            // But only if its not the default key. If it is the default key, it will throw an ArgumentException
            Assert.Throws<SecureChannelException>(
                () =>
                {
                    using (_ = new SecurityDomainSession(Device, keyRef1))
                    {
                    }
                });
        }

        [Fact]
        public void AuthenticateWithWrongKey_Should_ThrowException()
        {
            var incorrectKeys = RandomStaticKeys();
            var keyRef = Scp03KeyParameters.FromStaticKeys(incorrectKeys);

            // Authentication with incorrect key should throw
            Assert.Throws<ArgumentException>(() =>
            {
                using (var session = new SecurityDomainSession(Device, keyRef)) { };
            });

            // Authentication with default key should succeed
            using (_ = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {

            }
        }

        [Fact]
        public void GetInformation_WithDefaultKey_Returns_DefaultKey()
        {
            using var session = new SecurityDomainSession(Device);

            var result = session.GetKeyInformation();
            Assert.NotEmpty(result);
            Assert.Equal(4, result.Count);
            Assert.Equal(0xFF, result.Keys.First().VersionNumber);
        }

        [Fact]
        public void Connect_GetInformation_WithDefaultKey_Returns_DefaultKey()
        {
            using var connection = Device.Connect(YubiKeyApplication.SecurityDomain);
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
            Assert.Equal(4, keyInformation.Keys.Count);
            Assert.Equal(0xFF, keyInformation.Keys.First().VersionNumber);
        }

        [Fact]
        public void GetCertificates_ReturnsCerts()
        {
            Skip.IfNot(Device.FirmwareVersion >= FirmwareVersion.V5_7_2);

            using var session = new SecurityDomainSession(Device);

            var keyReference = new KeyReference(ScpKid.Scp11b, 0x1);
            var certificateList = session.GetCertificates(keyReference);

            Assert.NotEmpty(certificateList);
        }

        [Fact]
        public void Reset_Restores_SecurityDomainKeys_To_FactoryKeys()
        {
            byte[] sk =
            {
                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
            };

            var newKeyParams = new Scp03KeyParameters(
                ScpKid.Scp03,
                0x01,
                new StaticKeys(sk, sk, sk));

            // TODO assumeFalse("SCP03 not supported over NFC on FIPS capable devices",
            //     state.getDeviceInfo().getFipsCapable() != 0 && !state.isUsbTransport());

            using (var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {
                session.PutKey(newKeyParams.KeyReference, newKeyParams.StaticKeys, 0);
            }

            // Authentication with new key should succeed
            using (var session = new SecurityDomainSession(Device, newKeyParams))
            {
                session.GetKeyInformation();
            }


            // Default key should not work now and throw an exception
            Assert.Throws<ArgumentException>(() =>
            {
                using (_ = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
                {
                }
            });

            using (var session = new SecurityDomainSession(Device))
            {
                session.Reset();
            }

            // Successful authentication with default key means key has been restored to factory settings
            using (var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {
                _ = session.GetKeyInformation();
            }
        }

        [Fact]
        public void Scp03_GetSupportedCaIdentifiers_Succeeds()
        {
            using var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey);
            var result = session.GetSupportedCaIdentifiers(true, true);
            Assert.NotEmpty(result);
        }
        
        [Fact]
        public void Scp03_GetCardRecognitionData_Succeeds()
        {
            using var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey);
            var result = session.GetCardRecognitionData();
            Assert.True(result.Length > 0);
        }
        
        [Fact]
        public void Scp03_GetData_Succeeds()
        {
            using var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey);
            var result = session.GetData(0x66); // Card Data
            Assert.True(result.Length > 0);
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void PivSession_TryVerifyPinAndGetMetaData_Succeeds(
            StandardTestDevice testDeviceType)
        {
            var device = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            Assert.True(device.FirmwareVersion >= FirmwareVersion.V5_3_0);
            Assert.True(device.HasFeature(YubiKeyFeature.Scp03));

            using var pivSession = new PivSession(device, Scp03KeyParameters.DefaultKey);

            var result = pivSession.TryVerifyPin(
                new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 }),
                out _);

            Assert.True(result);

            var metadata = pivSession.GetMetadata(PivSlot.Pin)!;
            Assert.Equal(3, metadata.RetryCount);
        }

        [Fact]
        public void Device_Connect_With_Application_Succeeds()
        {
            var device = IntegrationTestDeviceEnumeration.GetTestDevice(
                Transport.SmartCard, FirmwareVersion.V5_3_0);

            using var connection = device.Connect(YubiKeyApplication.Piv, Scp03KeyParameters.DefaultKey);
            Assert.NotNull(connection);

            var cmd = new VerifyPinCommand(_defaultPin);
            var rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
        }

        [Fact]
        public void Device_Connect_ApplicationId_Succeeds()
        {
            var device = IntegrationTestDeviceEnumeration.GetTestDevice(
                Transport.SmartCard, FirmwareVersion.V5_3_0);

            using IYubiKeyConnection connection = device.Connect(
                YubiKeyApplication.Piv.GetIso7816ApplicationId(), Scp03KeyParameters.DefaultKey);

            Assert.NotNull(connection);

            var cmd = new VerifyPinCommand(_defaultPin);
            var rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
        }

        [Fact]
        public void Device_TryConnect_With_Application_Succeeds()
        {
            var device = IntegrationTestDeviceEnumeration.GetTestDevice(
                Transport.SmartCard, FirmwareVersion.V5_3_0);

            var isValid = device.TryConnect(
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

        [Fact]
        public void Device_TryConnect_With_ApplicationId_Succeeds()
        {
            var device = IntegrationTestDeviceEnumeration.GetTestDevice(
                Transport.SmartCard, FirmwareVersion.V5_3_0);

            var isValid = device.TryConnect(YubiKeyApplication.Piv, Scp03KeyParameters.DefaultKey,
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
