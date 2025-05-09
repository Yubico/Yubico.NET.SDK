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
using Yubico.YubiKey.Cryptography;
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
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class PinOnlyWithResetTests : PivSessionIntegrationTestBase
    {
        private const int SpecifiedStart = 72;
        private const int RandomTrailingCount = 2048;
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
            specifiedBytes =
            [
                0x05, 0x01, 0xC9, 0x5E, 0x72, 0xAB, 0x58, 0x9E,
                0x6D, 0x82, 0x95, 0xA3, 0x74, 0xB7, 0x69, 0x2B,
                0x6D, 0x82, 0x95, 0xA3, 0x74, 0xB7, 0x69, 0x2B,
                0x6D, 0x82, 0x95, 0xA3, 0x74, 0xB7, 0x69, 0x2B,
                0x05, 0x01, 0xC9, 0x5E, 0x72, 0xAB, 0x58, 0x9E,
                0x6D, 0x82, 0x95, 0xA3, 0x74, 0xB7, 0x69, 0x2B,
                0xc9, 0xf4, 0x20, 0x5a, 0x29, 0x38, 0x1b, 0xb8,
                0x60, 0x6b, 0xd4, 0xde, 0x18, 0xef, 0xf4, 0x3d,
                0x43, 0x24, 0x87, 0x3e, 0x5e, 0xd2, 0xc1, 0xed
            ];

            var randomBytes = new byte[SpecifiedStart + RandomTrailingCount];
            using var random = RandomObjectUtility.GetRandomObject(null);
            random.GetBytes(randomBytes);

            Array.Copy(specifiedBytes, 0, randomBytes, 0, specifiedBytes.Length);
            replacement = RandomObjectUtility.SetRandomProviderFixedBytes(randomBytes);
        }

        [Fact]
        public void NotPinOnly_GetMode_ReturnsNone()
        {
            var mode = Session.GetPinOnlyMode();
            Assert.Equal(PivPinOnlyMode.None, mode);
        }

        [Fact]
        public void SetPinDerived_GetMode_ReturnsCorrect()
        {
            using (var pivSession = GetSession())
            {
                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinDerived);
                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);

                var mode = pivSession.GetPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.PinDerived, mode);

                var adminData = pivSession.ReadObject<AdminData>();
                Assert.Null(adminData.PinLastUpdated);
                Assert.False(adminData.PinProtected);
                Assert.True(adminData.PukBlocked);
                _ = Assert.NotNull(adminData.Salt);

                var pinProtect = pivSession.ReadObject<PinProtectedData>();
                Assert.True(pinProtect.IsEmpty);
                Assert.Null(pinProtect.ManagementKey);
            }

            using (var pivSession = GetSession())
            {
                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.AuthenticateManagementKey();
                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
            }

            var isBlocked = IsPukBlocked();
            Assert.True(isBlocked);
        }

        [Fact]
        public void Run_SetPinDerived_UsesSalt()
        {
            Session.SetPinOnlyMode(PivPinOnlyMode.PinDerived);
            var adminData = Session.ReadObject<AdminData>();
            Assert.NotNull(adminData.Salt);

            var expected = GetSpecifiedSpan(0, 16);
            var result = (ReadOnlyMemory<byte>)adminData.Salt;
            var isValid = expected.SequenceEqual(result.Span);
            Assert.True(isValid);
        }

        [Fact]
        public void SetPinProtected_GetMode_ReturnsCorrect()
        {
            using (var pivSession = GetSession())
            {
                // Act
                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected);
                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);

                // Assert
                var mode = pivSession.GetPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.PinProtected, mode);

                var adminData = pivSession.ReadObject<AdminData>();
                Assert.Null(adminData.PinLastUpdated);
                Assert.True(adminData.PinProtected);
                Assert.True(adminData.PukBlocked);
                Assert.Null(adminData.Salt);

                var pinProtect = pivSession.ReadObject<PinProtectedData>();
                Assert.False(pinProtect.IsEmpty);
                Assert.NotNull(pinProtect.ManagementKey);
            }

            using (var pivSession = GetSession())
            {
                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.AuthenticateManagementKey();
                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
            }

            var isBlocked = IsPukBlocked();
            Assert.True(isBlocked);
        }

        [Fact]
        public void SetBoth_GetMode_ReturnsCorrect()
        {
            using var session = GetSession();
            session.SetPinOnlyMode(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived);
            Assert.True(session.PinVerified);
            Assert.True(session.ManagementKeyAuthenticated);

            var mode = session.GetPinOnlyMode();
            Assert.Equal(PivPinOnlyMode.PinDerived | PivPinOnlyMode.PinProtected, mode);

            var isBlocked = IsPukBlocked();
            Assert.True(isBlocked);
        }

        [Fact]
        public void SetProtectThenDerive_GetMode_ReturnsCorrect()
        {
            Session.SetPinOnlyMode(PivPinOnlyMode.PinProtected);
            Session.SetPinOnlyMode(PivPinOnlyMode.PinDerived);

            var mode = Session.GetPinOnlyMode();
            Assert.Equal(PivPinOnlyMode.PinDerived | PivPinOnlyMode.PinProtected, mode);

            var isBlocked = IsPukBlocked();
            Assert.True(isBlocked);
        }

        [Fact]
        public void SetProtectThenDerive_CorrectMgmtKey()
        {
            var expected1 = GetSpecifiedSpan(24, 24);
            var expected2 = GetSpecifiedSpan(48, 24);
            using (var pivSession = GetSession())
            {
                // Act
                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected);

                var pinProtect = pivSession.ReadObject<PinProtectedData>();
                Assert.False(pinProtect.IsEmpty);
                Assert.NotNull(pinProtect.ManagementKey);
                var result = (ReadOnlyMemory<byte>)pinProtect.ManagementKey;
                var isValid = expected1.SequenceEqual(result.Span);
                Assert.True(isValid);
            }

            using (var pivSession = GetSession())
            {
                // Act
                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinDerived);

                var mode = pivSession.GetPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, mode);

                var pinProtect = pivSession.ReadObject<PinProtectedData>();
                Assert.False(pinProtect.IsEmpty);
                Assert.NotNull(pinProtect.ManagementKey);
                var result = (ReadOnlyMemory<byte>)pinProtect.ManagementKey;
                var isValid = expected2.SequenceEqual(result.Span);
                Assert.True(isValid);
            }
        }

        [Fact]
        public void SetProtect_ThenNone_CorrectMode()
        {
            // Arrange
            var mgmtKey = GetSpecifiedSpan(24, 24);
            using (var pivSession = GetSession())
            {
                // Act
                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinProtected);

                // Assert
                var pinProtect = pivSession.ReadObject<PinProtectedData>();
                Assert.False(pinProtect.IsEmpty);
                Assert.NotNull(pinProtect.ManagementKey);
                // var isValid = mgmtKey.SequenceEqual(pinProtect.ManagementKey.Value.Span);
                // Assert.True(isValid);
            }

            var isBlocked = IsPukBlocked();
            Assert.True(isBlocked);

            using (var pivSession = GetSession())
            {
                // This will return the default mgmt key, but the mgmt key has
                // been changed. However, we should never ask the KeyCollector
                // for the mgmt key, so it shouldn't matter. This will test that
                // the KeyCollector is not called, but the mgmt key will be
                // authenticated.

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.SetPinOnlyMode(PivPinOnlyMode.None);

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
            }

            using (var pivSession = GetSession())
            {
                // We should not need the new mgmt key to read objects, so
                // provide a mgmt key that will return the wrong value.
                var specifiedCollector = new SpecifiedKeyCollector(
                    DefaultPin,
                    DefaultPuk,
                    mgmtKey.ToArray());
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                var mode = pivSession.GetPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.None, mode);

                var adminData = pivSession.ReadObject<AdminData>();
                Assert.True(adminData.IsEmpty);

                var pinProtect = pivSession.ReadObject<PinProtectedData>();
                Assert.True(pinProtect.IsEmpty);
            }

            isBlocked = IsPukBlocked();
            Assert.True(isBlocked);

            using (var pivSession = GetSession())
            {
                // In order to change retry counts, we need the correct mgmt key,
                // which was reset to default.
                pivSession.ChangePinAndPukRetryCounts(5, 6);
            }

            isBlocked = IsPukBlocked();
            Assert.False(isBlocked);

            // Try changing but call the auth and vfy outside the Change method.
            using (var pivSession = GetSession())
            {
                pivSession.VerifyPin();
                pivSession.AuthenticateManagementKey();
                pivSession.ChangePinAndPukRetryCounts(7, 8);
            }
        }

        [Theory]
        [InlineData(KeyType.AES128, PivPinOnlyMode.PinProtected, 0x83)]
        [InlineData(KeyType.AES128, PivPinOnlyMode.PinDerived, 0x84)]
        [InlineData(KeyType.AES128, PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, 0x85)]
        [InlineData(KeyType.AES192, PivPinOnlyMode.PinProtected, 0x86)]
        [InlineData(KeyType.AES192, PivPinOnlyMode.PinDerived, 0x87)]
        [InlineData(KeyType.AES192, PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, 0x88)]
        [InlineData(KeyType.AES256, PivPinOnlyMode.PinProtected, 0x89)]
        [InlineData(KeyType.AES256, PivPinOnlyMode.PinDerived, 0x8A)]
        [InlineData(KeyType.AES256, PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, 0x8B)]
        [InlineData(KeyType.TripleDES, PivPinOnlyMode.PinProtected, 0x8C)]
        [InlineData(KeyType.TripleDES, PivPinOnlyMode.PinDerived, 0x8D)]
        [InlineData(KeyType.TripleDES, PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, 0x8E)]
        public void SetPinOnly_Algorithms_Success(
            KeyType keyType,
            PivPinOnlyMode mode,
            byte slotNumber)
        {
            var specifiedCollector = new SpecifiedKeyCollector(
                DefaultPin,
                DefaultPuk,
                new byte[8]
            );

            using (var pivSession = GetSession())
            {
                pivSession.SetPinOnlyMode(mode, keyType.GetPivAlgorithm());
            }

            using (var pivSession = GetSession())
            {
                var currentMode = pivSession.GetPinOnlyMode();
                Assert.Equal(mode, currentMode);
            }

            using (var pivSession = GetSession())
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                pivSession.AuthenticateManagementKey();

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
            }

            using (var pivSession = GetSession())
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                var publicKey = pivSession.GenerateKeyPair(slotNumber, KeyType.ECP256);
                Assert.Equal(KeyType.ECP256, publicKey.KeyType);
            }
        }

        [Theory]
        [InlineData(KeyType.AES128, PivPinOnlyMode.PinProtected, 0x82)]
        [InlineData(KeyType.AES192, PivPinOnlyMode.PinDerived, 0x83)]
        public void SetPinOnly_ThenBoth_Success(
            KeyType keyType,
            PivPinOnlyMode mode,
            byte slotNumber)
        {
            var specifiedCollector = new SpecifiedKeyCollector(
                DefaultPin,
                DefaultPuk,
                new byte[8]
            );

            var newMode = mode == PivPinOnlyMode.PinProtected
                ? PivPinOnlyMode.PinDerived
                : PivPinOnlyMode.PinProtected;

            using (var pivSession = GetSession())
            {
                pivSession.SetPinOnlyMode(mode, keyType.GetPivAlgorithm());
                Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = GetSession())
            {
                var currentMode = pivSession.GetPinOnlyMode();

                Assert.Equal(mode, currentMode);
                Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = GetSession())
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                pivSession.SetPinOnlyMode(newMode, keyType.GetPivAlgorithm());

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = GetSession())
            {
                var currentMode = pivSession.GetPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, currentMode);
            }

            using (var pivSession = GetSession())
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                var publicKey = pivSession.GenerateKeyPair(
                    slotNumber, KeyType.ECP256);

                Assert.Equal(KeyType.ECP256, publicKey.KeyType);
            }
        }

        [Theory]
        [InlineData(KeyType.AES128, PivPinOnlyMode.PinProtected, 0x8F)]
        [InlineData(KeyType.AES192, PivPinOnlyMode.PinDerived, 0x90)]
        [InlineData(KeyType.AES256, PivPinOnlyMode.PinProtected, 0x91)]
        [InlineData(KeyType.TripleDES, PivPinOnlyMode.PinDerived, 0x92)]
        public void SetPinOnly_ThenNewAlg_Success(
            KeyType keyType,
            PivPinOnlyMode mode,
            byte slotNumber)
        {
            var specifiedCollector = new SpecifiedKeyCollector(
                DefaultPin,
                DefaultPuk,
                new byte[8]
            );

            var newAlg = keyType switch
            {
                KeyType.AES128 => KeyType.AES192,
                KeyType.AES192 => KeyType.AES256,
                KeyType.AES256 => KeyType.TripleDES,
                _ => KeyType.AES128,
            };

            using (var pivSession = GetSession())
            {
                pivSession.SetPinOnlyMode(mode, keyType.GetPivAlgorithm());
                Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = GetSession())
            {
                var currentMode = pivSession.GetPinOnlyMode();

                Assert.Equal(mode, currentMode);
                Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = GetSession())
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                pivSession.SetPinOnlyMode(mode, newAlg.GetPivAlgorithm());

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(newAlg.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = GetSession())
            {
                var currentMode = pivSession.GetPinOnlyMode();
                Assert.Equal(mode, currentMode);
            }

            using (var pivSession = GetSession())
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                var publicKey = pivSession.GenerateKeyPair(
                    slotNumber, KeyType.ECP256);

                Assert.Equal(KeyType.ECP256, publicKey.KeyType);
            }
        }

        [Theory]
        [InlineData(KeyType.AES128, PivPinOnlyMode.PinProtected, 0x84)]
        [InlineData(KeyType.AES192, PivPinOnlyMode.PinDerived, 0x85)]
        [InlineData(KeyType.AES256, PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, 0x86)]
        public void SetPinOnly_ThenNone_Success(
            KeyType keyType,
            PivPinOnlyMode mode,
            byte slotNumber)
        {
            var specifiedCollector = new SpecifiedKeyCollector(
                DefaultPin,
                DefaultPuk,
                new byte[8]
            );

            using (var pivSession = GetSession())
            {
                pivSession.SetPinOnlyMode(mode, keyType.GetPivAlgorithm());
                Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

                var currentMode = pivSession.GetPinOnlyMode();
                Assert.Equal(mode, currentMode);
            }

            using (var pivSession = GetSession())
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                var publicKey = pivSession.GenerateKeyPair(
                    slotNumber, KeyType.ECP256);

                Assert.Equal(KeyType.ECP256, publicKey.KeyType);
                Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = GetSession())
            {
                pivSession.KeyCollector = specifiedCollector.SpecifiedKeyCollectorDelegate;

                pivSession.SetPinOnlyMode(PivPinOnlyMode.None, keyType.GetPivAlgorithm());

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
            }

            using (var pivSession = GetSession())
            {
                var currentMode = pivSession.GetPinOnlyMode();
                Assert.Equal(PivPinOnlyMode.None, currentMode);
                pivSession.TryAuthenticateManagementKey(DefaultManagementKey);
            }
        }

        private Span<byte> GetSpecifiedSpan(
            int offset,
            int length) =>
            specifiedBytes.AsSpan(offset, length);

        // If the PUK is blocked, return true.
        // If the PUK is not blocked, return false.
        // If there is any other error, throw an exception.
        private bool IsPukBlocked()
        {
            using var pivSession = GetSession();
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

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                replacement.RestoreRandomProvider();
            }

            base.Dispose(disposing);
        }
    }
}
