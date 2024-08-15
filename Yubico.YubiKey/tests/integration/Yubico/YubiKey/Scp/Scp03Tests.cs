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
using System.Linq;
using Xunit;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Scp03;
using Yubico.YubiKey.TestUtilities;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Yubico.YubiKey.Scp
{
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

            var newKeyParams = new Scp03KeyParameters(ScpKid.Scp03, 0x01, new StaticKeys(
                sk,
                sk,
                sk));

            // assumeFalse("SCP03 not supported over NFC on FIPS capable devices",
            //     state.getDeviceInfo().getFipsCapable() != 0 && !state.isUsbTransport());

            using (var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {
                session.PutKeySet(newKeyParams);
            }

            using (_ = new SecurityDomainSession(Device, newKeyParams))
            {
                // Authentication with new key should succeed
            }

            Assert.Throws<ArgumentException>(() => //TODO Is this the correct exception to throw? 
            {
                using (_ = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
                {
                    // Default key should not work now and throw an exception
                }
            });
        }

        [Fact]
        public void TestDeleteKey()
        {
            var keyRef1 = new Scp03KeyParameters(ScpKid.Scp03, 0x10, RandomStaticKeys());
            var keyRef2 = new Scp03KeyParameters(ScpKid.Scp03, 0x55, RandomStaticKeys());

            // Auth with default key, then replace default key
            using (var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {
                session.PutKeySet(keyRef1);
            }

            // Authenticate with key1, then add additional key, keyref2
            using (var session = new SecurityDomainSession(Device, keyRef1))
            {
                session.PutKeySet(keyRef2);
            }

            // Authenticate with key2, delete key 1
            using (var session = new SecurityDomainSession(Device, keyRef2))
            {
                session.DeleteKeySet(keyRef1.KeyReference.VersionNumber);
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
                session.DeleteKeySet(keyRef2.KeyReference.VersionNumber, true);
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

            using (var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {
                session.PutKeySet(keyRef1);
            }

            using (var session = new SecurityDomainSession(Device, keyRef1))
            {
                session.PutKeySet(keyRef2);
            }

            using (_ = new SecurityDomainSession(Device, keyRef2))
            {
                // Authentication with new key should succeed
            }

            Assert.Throws<SecureChannelException>(
                () => // TODO SecureChannelException this time for some reason, but ArgumentException if I try with the DefaultKey, check google Keep 
                {
                    using (_ = new SecurityDomainSession(Device, keyRef1))
                    {
                    }
                });

            using (_ = new SecurityDomainSession(Device, keyRef2))
            {
                // Authentication with new key should succeed
            }
        }

        [Fact]
        public void AuthenticateWithWrongKey_Should_ThrowException()
        {
            var incorrectKeys = RandomStaticKeys();
            var keyRef = new Scp03KeyParameters(ScpKid.Scp03, 0x01, incorrectKeys);

            Assert.Throws<ArgumentException>(() => new SecurityDomainSession(Device, keyRef));

            using (_ = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {
                // Authentication with default key should succeed
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
            var res = response.GetData();

            // var result = session.GetKeyInformation();
            // Assert.NotEmpty(result);
            // Assert.Equal(4, result.Count);
            // Assert.Equal(0xFF, result.Keys.First().VersionNumber);
        }

        [Fact]
        public void TestGetCertificateBundle()
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
                session.PutKeySet(newKeyParams);
            }

            using (var session = new SecurityDomainSession(Device, newKeyParams))
            {
                // Authentication with new key should succeed
                session.GetKeyInformation();
            }

            Assert.Throws<ArgumentException>(() => //TODO Is this the correct exception to throw? 
            {
                using (_ = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
                {
                    // Default key should not work now and throw an exception
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
        public void TryConnectScp_WithApplicationId_Succeeds()
        {
            var isValid =
                Device.TryConnectScp(YubiKeyApplication.Piv, Scp03KeyParameters.DefaultKey, out var connection);
            using (connection)
            {
                Assert.True(isValid);
                Assert.NotNull(connection);

                var cmd = new VerifyPinCommand(_defaultPin);
                var rsp = connection.SendCommand(cmd);
                Assert.Equal(ResponseStatus.Success, rsp.Status);
            }
        }

        [Fact]
        public void TryConnectScp_WithApplication_Succeeds()
        {
            var isValid =
                Device.TryConnectScp(YubiKeyApplication.Piv, Scp03KeyParameters.DefaultKey, out var connection);
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
        public void ConnectScp_WithApplication_Succeeds()
        {
            using IYubiKeyConnection connection =
                Device.ConnectScp(YubiKeyApplication.Piv, Scp03KeyParameters.DefaultKey);
            Assert.NotNull(connection);

            var cmd = new VerifyPinCommand(_defaultPin);
            var rsp = connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
        }

        [Fact]
        public void ConnectScp_WithApplicationId_Succeeds()
        {
            using IYubiKeyConnection connection = Device.ConnectScp(
                YubiKeyApplication.Piv.GetIso7816ApplicationId(),
                Scp03KeyParameters.DefaultKey);

            Assert.NotNull(connection);

            var cmd = new VerifyPinCommand(_defaultPin);
            var rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
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
