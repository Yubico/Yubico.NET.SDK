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
using System.Security;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Piv.Objects;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    // All these tests will reset the PIV application, run, then reset the PIV
    // application again.
    // All these tests will also use a random number generator with a specified
    // set of bytes, followed by 2048 random bytes. If you want to get only
    // random bytes, skip the first SpecifiedStart bytes (get a random object and
    // generate that many bytes).
    public class PinOnlyWithResetTests : IDisposable
    {
        private const int SpecifiedStart = 72;
        private const int RandomTrailingCount = 2048;
        private readonly IYubiKeyDevice yubiKey;
        readonly RandomObjectUtility replacement;
        readonly private byte[] specifiedBytes;

        public PinOnlyWithResetTests()
        {
            // This buffer will hold the random bytes to return.
            // The first 24 will be a weak 3DES key
            // The first 16 can also be a salt.
            // The second 24 will be a non-weak 3DES key.
            // The third 24 bytes is the 3DES key that is derived using a PIN of
            // "123456" and a salt of the first 16 bytes.
            // Then there will be 2048 random bytes.
            specifiedBytes = new byte[SpecifiedStart] {
                0x05, 0x01, 0xC9, 0x5E, 0x72, 0xAB, 0x58, 0x9E,
                0x6D, 0x82, 0x95, 0xA3, 0x74, 0xB7, 0x69, 0x2B,
                0x6D, 0x82, 0x95, 0xA3, 0x74, 0xB7, 0x69, 0x2B,
                0x6D, 0x82, 0x95, 0xA3, 0x74, 0xB7, 0x69, 0x2B,
                0x05, 0x01, 0xC9, 0x5E, 0x72, 0xAB, 0x58, 0x9E,
                0x6D, 0x82, 0x95, 0xA3, 0x74, 0xB7, 0x69, 0x2B,
                0xc9, 0xf4, 0x20, 0x5a, 0x29, 0x38, 0x1b, 0xb8,
                0x60, 0x6b, 0xd4, 0xde, 0x18, 0xef, 0xf4, 0x3d,
                0x43, 0x24, 0x87, 0x3e, 0x5e, 0xd2, 0xc1, 0xed,
            };

            byte[] randomBytes = new byte[SpecifiedStart + RandomTrailingCount];
            using RandomNumberGenerator random = RandomObjectUtility.GetRandomObject(null);
            {
                random.GetBytes(randomBytes);
            }

            Array.Copy(specifiedBytes, 0, randomBytes, 0, specifiedBytes.Length);

            replacement = RandomObjectUtility.SetRandomProviderFixedBytes(randomBytes);

            yubiKey = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);
            ResetPiv(yubiKey);
        }

        public void Dispose()
        {
            ResetPiv(yubiKey);
            replacement.RestoreRandomProvider();
        }

        [Fact]
        public void NotPinOnly_GetMode_ReturnsNone()
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                PivPinOnlyMode mode = pivSession.GetPinOnlyMode();

                Assert.Equal(PivPinOnlyMode.None, mode);
            }
        }

        [Fact]
        public void SetPinDerived_GetMode_ReturnsCorrect()
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinDerived);

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);

                PivPinOnlyMode mode = pivSession.GetPinOnlyMode();

                Assert.Equal(PivPinOnlyMode.PinDerived, mode);

                AdminData adminData = pivSession.ReadObject<AdminData>();
                Assert.Null(adminData.PinLastUpdated);
                Assert.False(adminData.PinProtected);
                Assert.True(adminData.PukBlocked);
                _ = Assert.NotNull(adminData.Salt);

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();
                Assert.True(pinProtect.IsEmpty);
                Assert.Null(pinProtect.ManagementKey);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.AuthenticateManagementKey();

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
            }

            bool isBlocked = IsPukBlocked();
            Assert.True(isBlocked);
        }

        [Fact]
        public void Run_SetPinDerived_UsesSalt()
        {
            Span<byte> expected = GetSpecifiedSpan(0, 16);
            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinDerived);

                AdminData adminData = pivSession.ReadObject<AdminData>();
                _ = Assert.NotNull(adminData.Salt);
                if (!(adminData.Salt is null))
                {
                    var result = (ReadOnlyMemory<byte>)adminData.Salt;
                    bool isValid = expected.SequenceEqual(result.Span);
                    Assert.True(isValid);
                }
            }
        }

        [Fact]
        public void SetPinProtected_GetMode_ReturnsCorrect()
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected);

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);

                PivPinOnlyMode mode = pivSession.GetPinOnlyMode();

                Assert.Equal(PivPinOnlyMode.PinProtected, mode);

                AdminData adminData = pivSession.ReadObject<AdminData>();
                Assert.Null(adminData.PinLastUpdated);
                Assert.True(adminData.PinProtected);
                Assert.True(adminData.PukBlocked);
                Assert.Null(adminData.Salt);

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();
                Assert.False(pinProtect.IsEmpty);
                _ = Assert.NotNull(pinProtect.ManagementKey);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.AuthenticateManagementKey();

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
            }

            bool isBlocked = IsPukBlocked();
            Assert.True(isBlocked);
        }

        [Fact]
        public void SetBoth_GetMode_ReturnsCorrect()
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived);

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);

                PivPinOnlyMode mode = pivSession.GetPinOnlyMode();

                Assert.Equal(PivPinOnlyMode.PinDerived | PivPinOnlyMode.PinProtected, mode);
            }

            bool isBlocked = IsPukBlocked();
            Assert.True(isBlocked);
        }

        [Fact]
        public void SetProtectThenDerive_GetMode_ReturnsCorrect()
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinDerived);

                PivPinOnlyMode mode = pivSession.GetPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.PinDerived | PivPinOnlyMode.PinProtected, mode);
            }

            bool isBlocked = IsPukBlocked();
            Assert.True(isBlocked);
        }

        [Fact]
        public void SetProtectThenDerive_CorrectMgmtKey()
        {
            Span<byte> expected1 = GetSpecifiedSpan(24, 24);
            Span<byte> expected2 = GetSpecifiedSpan(48, 24);
            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected);

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();
                Assert.False(pinProtect.IsEmpty);
                _ = Assert.NotNull(pinProtect.ManagementKey);
                if (!(pinProtect.ManagementKey is null))
                {
                    var result = (ReadOnlyMemory<byte>)pinProtect.ManagementKey;
                    bool isValid = expected1.SequenceEqual(result.Span);
                    Assert.True(isValid);
                }
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinDerived);

                PivPinOnlyMode mode = pivSession.GetPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, mode);

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();
                Assert.False(pinProtect.IsEmpty);
                _ = Assert.NotNull(pinProtect.ManagementKey);
                if (!(pinProtect.ManagementKey is null))
                {
                    var result = (ReadOnlyMemory<byte>)pinProtect.ManagementKey;
                    bool isValid = expected2.SequenceEqual(result.Span);
                    Assert.True(isValid);
                }
            }
        }

        [Fact]
        public void SetProtect_RejectsWeakKey()
        {
            Span<byte> expected = GetSpecifiedSpan(24, 24);
            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected);

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();

                Assert.False(pinProtect.IsEmpty);
                _ = Assert.NotNull(pinProtect.ManagementKey);
                if (!(pinProtect.ManagementKey is null))
                {
                    var result = (ReadOnlyMemory<byte>)pinProtect.ManagementKey;
                    bool isValid = expected.SequenceEqual(result.Span);
                    Assert.True(isValid);
                }
            }
        }

        [Fact]
        public void SetProtect_ThenNone_CorrectMode()
        {
            Span<byte> mgmtKey = GetSpecifiedSpan(24, 24);
            var specifiedCollector = new SpecifiedKeyCollector(
                new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 },
                new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 },
                mgmtKey.ToArray());

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected);

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();

                Assert.False(pinProtect.IsEmpty);
                _ = Assert.NotNull(pinProtect.ManagementKey);
                if (!(pinProtect.ManagementKey is null))
                {
                    var result = (ReadOnlyMemory<byte>)pinProtect.ManagementKey;
                    bool isValid = mgmtKey.SequenceEqual(result.Span);
                    Assert.True(isValid);
                }
            }

            bool isBlocked = IsPukBlocked();
            Assert.True(isBlocked);

            using (var pivSession = new PivSession(yubiKey))
            {
                // This will return the default mgmt key, but the mgmt key has
                // been changed. However, we should never ask the KeyCollector
                // for the mgmt key, so it shouldn't matter. This will test that
                // the KeyCollector is not called, but the mgmt key will be
                // authenticated.
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.SetPinOnlyMode(PivPinOnlyMode.None);

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                // We should not need the new mgmt key to read objects, so
                // provide a mgmt key that will return the wrong value.
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                PivPinOnlyMode mode = pivSession.GetPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.None, mode);

                AdminData adminData = pivSession.ReadObject<AdminData>();
                Assert.True(adminData.IsEmpty);

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();
                Assert.True(pinProtect.IsEmpty);
            }

            isBlocked = IsPukBlocked();
            Assert.True(isBlocked);

            using (var pivSession = new PivSession(yubiKey))
            {
                // In order to change retry counts, we need the correct mgmt key,
                // which was reset to default.
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ChangePinAndPukRetryCounts(5, 6);
            }

            isBlocked = IsPukBlocked();
            Assert.False(isBlocked);

            // Try changing but call the auth and vfy outside the Change method.
            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.VerifyPin();
                pivSession.AuthenticateManagementKey();
                pivSession.ChangePinAndPukRetryCounts(7, 8);
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.Aes128, PivPinOnlyMode.PinProtected, 0x83)]
        [InlineData(PivAlgorithm.Aes128, PivPinOnlyMode.PinDerived, 0x84)]
        [InlineData(PivAlgorithm.Aes128, PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, 0x85)]
        [InlineData(PivAlgorithm.Aes192, PivPinOnlyMode.PinProtected, 0x86)]
        [InlineData(PivAlgorithm.Aes192, PivPinOnlyMode.PinDerived, 0x87)]
        [InlineData(PivAlgorithm.Aes192, PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, 0x88)]
        [InlineData(PivAlgorithm.Aes256, PivPinOnlyMode.PinProtected, 0x89)]
        [InlineData(PivAlgorithm.Aes256, PivPinOnlyMode.PinDerived, 0x8A)]
        [InlineData(PivAlgorithm.Aes256, PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, 0x8B)]
        [InlineData(PivAlgorithm.TripleDes, PivPinOnlyMode.PinProtected, 0x8C)]
        [InlineData(PivAlgorithm.TripleDes, PivPinOnlyMode.PinDerived, 0x8D)]
        [InlineData(PivAlgorithm.TripleDes, PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, 0x8E)]
        public void SetPinOnly_Algorithms_Success(PivAlgorithm algorithm, PivPinOnlyMode mode, byte slotNumber)
        {
            var specifiedCollector = new SpecifiedKeyCollector(
                new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 },
                new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 },
                new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
            );

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.SetPinOnlyMode(mode, algorithm);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                PivPinOnlyMode currentMode = pivSession.GetPinOnlyMode();

                Assert.Equal(mode, currentMode);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.AuthenticateManagementKey();

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                PivPublicKey publicKey = pivSession.GenerateKeyPair(
                    slotNumber, PivAlgorithm.EccP256);

                Assert.Equal(PivAlgorithm.EccP256, publicKey.Algorithm);
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.Aes128, PivPinOnlyMode.PinProtected, 0x82)]
        [InlineData(PivAlgorithm.Aes192, PivPinOnlyMode.PinDerived, 0x83)]
        public void SetPinOnly_ThenBoth_Success(PivAlgorithm algorithm, PivPinOnlyMode mode, byte slotNumber)
        {
            var specifiedCollector = new SpecifiedKeyCollector(
                new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 },
                new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 },
                new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
            );

            PivPinOnlyMode newMode = mode == PivPinOnlyMode.PinProtected ?
                PivPinOnlyMode.PinDerived : PivPinOnlyMode.PinProtected;

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);

                pivSession.SetPinOnlyMode(mode, algorithm);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                PivPinOnlyMode currentMode = pivSession.GetPinOnlyMode();

                Assert.Equal(mode, currentMode);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                pivSession.SetPinOnlyMode(newMode, algorithm);

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                PivPinOnlyMode currentMode = pivSession.GetPinOnlyMode();

                Assert.Equal(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, currentMode);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                PivPublicKey publicKey = pivSession.GenerateKeyPair(
                    slotNumber, PivAlgorithm.EccP256);

                Assert.Equal(PivAlgorithm.EccP256, publicKey.Algorithm);
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.Aes128, PivPinOnlyMode.PinProtected, 0x8F)]
        [InlineData(PivAlgorithm.Aes192, PivPinOnlyMode.PinDerived, 0x90)]
        [InlineData(PivAlgorithm.Aes256, PivPinOnlyMode.PinProtected, 0x91)]
        [InlineData(PivAlgorithm.TripleDes, PivPinOnlyMode.PinDerived, 0x92)]
        public void SetPinOnly_ThenNewAlg_Success(PivAlgorithm algorithm, PivPinOnlyMode mode, byte slotNumber)
        {
            var specifiedCollector = new SpecifiedKeyCollector(
                new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 },
                new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 },
                new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
            );

            PivAlgorithm newAlg = algorithm switch
            {
                PivAlgorithm.Aes128 => PivAlgorithm.Aes192,
                PivAlgorithm.Aes192 => PivAlgorithm.Aes256,
                PivAlgorithm.Aes256 => PivAlgorithm.TripleDes,
                _ => PivAlgorithm.Aes128,
            };

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);

                pivSession.SetPinOnlyMode(mode, algorithm);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                PivPinOnlyMode currentMode = pivSession.GetPinOnlyMode();

                Assert.Equal(mode, currentMode);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                pivSession.SetPinOnlyMode(mode, newAlg);

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(newAlg, pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                PivPinOnlyMode currentMode = pivSession.GetPinOnlyMode();

                Assert.Equal(mode, currentMode);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                PivPublicKey publicKey = pivSession.GenerateKeyPair(
                    slotNumber, PivAlgorithm.EccP256);

                Assert.Equal(PivAlgorithm.EccP256, publicKey.Algorithm);
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.Aes128, PivPinOnlyMode.PinProtected, 0x84)]
        [InlineData(PivAlgorithm.Aes192, PivPinOnlyMode.PinDerived, 0x85)]
        [InlineData(PivAlgorithm.Aes256, PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, 0x86)]
        public void SetPinOnly_ThenNone_Success(PivAlgorithm algorithm, PivPinOnlyMode mode, byte slotNumber)
        {
            var specifiedCollector = new SpecifiedKeyCollector(
                new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 },
                new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 },
                new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
            );

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);

                pivSession.SetPinOnlyMode(mode, algorithm);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                PivPinOnlyMode currentMode = pivSession.GetPinOnlyMode();

                Assert.Equal(mode, currentMode);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                PivPublicKey publicKey = pivSession.GenerateKeyPair(
                    slotNumber, PivAlgorithm.EccP256);

                Assert.Equal(PivAlgorithm.EccP256, publicKey.Algorithm);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                pivSession.SetPinOnlyMode(PivPinOnlyMode.None, algorithm);

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                PivPinOnlyMode currentMode = pivSession.GetPinOnlyMode();

                Assert.Equal(PivPinOnlyMode.None, currentMode);

                var mgmtKey = new ReadOnlyMemory<byte>(new byte[] {
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                });
                bool isValid = pivSession.TryAuthenticateManagementKey(mgmtKey, true);
            }
        }

        private Span<byte> GetSpecifiedSpan(int offset, int length) =>
            new Span<byte>(specifiedBytes, offset, length);

        // If the PUK is blocked, return true.
        // If the PUK is not blocked, return false.
        // If there is any other error, throw an exception.
        private bool IsPukBlocked()
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                try
                {
                    var collectorObj = new Simple39KeyCollector();
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                    pivSession.ChangePuk();

                    // If that worked, change the PUK back.
                    collectorObj.KeyFlag = 1;
                    pivSession.ChangePuk();
                }
                catch (SecurityException)
                {
                    return true;
                }

                return false;
            }
        }

        private static void ResetPiv(IYubiKeyDevice yubiKey)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();
            }
        }
    }
}
