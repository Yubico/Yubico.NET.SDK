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
using System.Text;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Oath.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Oath.IntegrationTests;

public class OathHashAlgorithmTests
{
    private static CancellationToken NewToken(int timeoutSeconds = 30) =>
        new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;

    private static CredentialData CreateCredential(
        string name, OathHashAlgorithm algorithm, string issuer, int digits = 6, int period = 30)
    {
        byte[] secret = "12345678901234567890"u8.ToArray();
        try
        {
            return new CredentialData
            {
                Name = name,
                OathType = OathType.Totp,
                HashAlgorithm = algorithm,
                Secret = secret,
                Issuer = issuer,
                Digits = digits,
                Period = period
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task TotpCredential_Sha256_CalculatesCode(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            using var credData = CreateCredential("sha256@example.com", OathHashAlgorithm.Sha256, "Sha256Test");

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

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task TotpCredential_Sha512_CalculatesCode(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            using var credData = CreateCredential("sha512@example.com", OathHashAlgorithm.Sha512, "Sha512Test");

            await session.PutCredentialAsync(credData, cancellationToken: NewToken());

            var credentials = await session.ListCredentialsAsync(NewToken());
            Assert.Single(credentials);

            var credential = credentials[0];
            long timestamp = 1704067200;
            var code = await session.CalculateCodeAsync(credential, timestamp, NewToken());
            Assert.NotNull(code);
            Assert.Equal(6, code.Value.Length);
        }, cancellationToken: NewToken());

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task TotpCredential_NonDefaultPeriod60s_CalculatesCode(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            using var credData = CreateCredential("period60@example.com", OathHashAlgorithm.Sha1, "Period60Test", period: 60);

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

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task TotpCredential_8Digits_ReturnsEightCharacterCode(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            using var credData = CreateCredential("eightdigit@example.com", OathHashAlgorithm.Sha1, "EightDigitTest", digits: 8);

            await session.PutCredentialAsync(credData, cancellationToken: NewToken());

            var credentials = await session.ListCredentialsAsync(NewToken());
            Assert.Single(credentials);

            long timestamp = 1704067200;
            var code = await session.CalculateCodeAsync(credentials[0], timestamp, NewToken());
            Assert.NotNull(code);
            Assert.Equal(8, code.Value.Length);
        }, cancellationToken: NewToken());

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task TotpCredential_Sha256With8Digits_CombinedSettings(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            using var credData = CreateCredential("combined@example.com", OathHashAlgorithm.Sha256, "CombinedTest", digits: 8, period: 60);

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

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task LockedSession_ValidateWithWrongKey_Throws(YubiKeyTestState state)
    {
        const string password = "locked-test-password";

        // Reset the applet and set an access key to lock the OATH application.
        await state.WithOathSessionAsync(async session =>
        {
            byte[] key = session.DeriveKey(Encoding.UTF8.GetBytes(password));
            try
            {
                await session.SetKeyAsync(key, NewToken());
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }, cancellationToken: NewToken());

        // A FRESH session is what observes the lock — the session that set the key is already
        // authenticated and would report nothing useful. It is disposed above rather than held open
        // alongside this one: each convenience session owns a connection, and the physical key admits one.
        await using var lockedSession = await state.Device
            .CreateOathSessionAsync(cancellationToken: NewToken());

        byte[] correctKey = lockedSession.DeriveKey(Encoding.UTF8.GetBytes(password));
        try
        {
            Assert.True(lockedSession.IsLocked);

            // Validate with the wrong key should fail
            byte[] wrongKey = lockedSession.DeriveKey(Encoding.UTF8.GetBytes("wrong-password"));
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
            await lockedSession.ValidateAsync(correctKey, NewToken());
            Assert.False(lockedSession.IsLocked);

            // After unlocking, operations should work
            var credentials = await lockedSession.ListCredentialsAsync(NewToken());
            Assert.NotNull(credentials);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(correctKey);

            // Clean up: unset the key so other tests are unaffected
            await lockedSession.UnsetKeyAsync(NewToken());
        }
    }
}