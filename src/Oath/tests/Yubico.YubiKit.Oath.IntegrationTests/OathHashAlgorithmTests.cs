// Copyright 2026 Yubico AB
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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Oath.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Oath.IntegrationTests;

public class OathHashAlgorithmTests
{
    private static CancellationToken NewToken(int timeoutSeconds = 30) =>
        new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;

    private static readonly byte[] TestSecret =
    [
        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30
    ];

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task TotpCredential_Sha256_CalculatesCode(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            var credData = new CredentialData
            {
                Name = "sha256@example.com",
                OathType = OathType.Totp,
                HashAlgorithm = OathHashAlgorithm.Sha256,
                Secret = TestSecret,
                Issuer = "Sha256Test"
            };

            await session.PutCredentialAsync(credData, cancellationToken: NewToken());

            var credentials = await session.ListCredentialsAsync(NewToken());
            Assert.Single(credentials);

            var credential = credentials[0];
            Assert.Equal(OathType.Totp, credential.OathType);

            long timestamp = 1704067200; // 2024-01-01 00:00:00 UTC
            var code = await session.CalculateCodeAsync(credential, timestamp, NewToken());
            Assert.NotNull(code);
            Assert.Equal(6, code.Value.Length);
        }, cancellationToken: NewToken());

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task TotpCredential_Sha512_CalculatesCode(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            var credData = new CredentialData
            {
                Name = "sha512@example.com",
                OathType = OathType.Totp,
                HashAlgorithm = OathHashAlgorithm.Sha512,
                Secret = TestSecret,
                Issuer = "Sha512Test"
            };

            await session.PutCredentialAsync(credData, cancellationToken: NewToken());

            var credentials = await session.ListCredentialsAsync(NewToken());
            Assert.Single(credentials);

            var credential = credentials[0];
            long timestamp = 1704067200;
            var code = await session.CalculateCodeAsync(credential, timestamp, NewToken());
            Assert.NotNull(code);
            Assert.Equal(6, code.Value.Length);
        }, cancellationToken: NewToken());

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task TotpCredential_NonDefaultPeriod60s_CalculatesCode(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            var credData = new CredentialData
            {
                Name = "period60@example.com",
                OathType = OathType.Totp,
                HashAlgorithm = OathHashAlgorithm.Sha1,
                Secret = TestSecret,
                Issuer = "Period60Test",
                Period = 60
            };

            await session.PutCredentialAsync(credData, cancellationToken: NewToken());

            var credentials = await session.ListCredentialsAsync(NewToken());
            Assert.Single(credentials);

            var credential = credentials[0];
            Assert.Equal(60, credential.Period);

            long timestamp = 1704067200;
            var code = await session.CalculateCodeAsync(credential, timestamp, NewToken());
            Assert.NotNull(code);
            Assert.Equal(6, code.Value.Length);
            // With 60s period, the valid window should span 60 seconds
            Assert.Equal(60, code.ValidTo - code.ValidFrom);
        }, cancellationToken: NewToken());

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task TotpCredential_8Digits_ReturnsEightCharacterCode(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            var credData = new CredentialData
            {
                Name = "eightdigit@example.com",
                OathType = OathType.Totp,
                HashAlgorithm = OathHashAlgorithm.Sha1,
                Secret = TestSecret,
                Issuer = "EightDigitTest",
                Digits = 8
            };

            await session.PutCredentialAsync(credData, cancellationToken: NewToken());

            var credentials = await session.ListCredentialsAsync(NewToken());
            Assert.Single(credentials);

            long timestamp = 1704067200;
            var code = await session.CalculateCodeAsync(credentials[0], timestamp, NewToken());
            Assert.NotNull(code);
            Assert.Equal(8, code.Value.Length);
        }, cancellationToken: NewToken());

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task TotpCredential_Sha256With8Digits_CombinedSettings(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            var credData = new CredentialData
            {
                Name = "combined@example.com",
                OathType = OathType.Totp,
                HashAlgorithm = OathHashAlgorithm.Sha256,
                Secret = TestSecret,
                Issuer = "CombinedTest",
                Digits = 8,
                Period = 60
            };

            await session.PutCredentialAsync(credData, cancellationToken: NewToken());

            var credentials = await session.ListCredentialsAsync(NewToken());
            Assert.Single(credentials);

            var credential = credentials[0];
            Assert.Equal(60, credential.Period);

            long timestamp = 1704067200;
            var code = await session.CalculateCodeAsync(credential, timestamp, NewToken());
            Assert.NotNull(code);
            Assert.Equal(8, code.Value.Length);
            Assert.Equal(60, code.ValidTo - code.ValidFrom);
        }, cancellationToken: NewToken());

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task LockedSession_ValidateWithWrongKey_Throws(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            // Set an access key to lock the OATH application
            string password = "locked-test-password";
            byte[] key = session.DeriveKey(password);

            try
            {
                await session.SetKeyAsync(key, NewToken());

                // Open a new session -- it should be locked because a key is set
                await using var lockedSession = await state.Device
                    .CreateOathSessionAsync(cancellationToken: NewToken());

                Assert.True(lockedSession.IsLocked);

                // Validate with the wrong key should fail
                byte[] wrongKey = session.DeriveKey("wrong-password");
                try
                {
                    await Assert.ThrowsAnyAsync<Exception>(async () =>
                        await lockedSession.ValidateAsync(wrongKey, NewToken()));
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(wrongKey);
                }

                // Validate with the correct key should succeed and unlock
                await lockedSession.ValidateAsync(key, NewToken());
                Assert.False(lockedSession.IsLocked);

                // After unlocking, operations should work
                var credentials = await lockedSession.ListCredentialsAsync(NewToken());
                Assert.NotNull(credentials);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);

                // Clean up: unset the key so other tests are unaffected
                await session.UnsetKeyAsync(NewToken());
            }
        }, cancellationToken: NewToken());
}
