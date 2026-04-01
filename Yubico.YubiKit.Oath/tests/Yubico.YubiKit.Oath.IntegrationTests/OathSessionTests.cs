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

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Oath.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Oath.IntegrationTests;

public class OathSessionTests
{
    private static readonly CancellationTokenSource CancellationTokenSource = new(TimeSpan.FromSeconds(60));

    private static CredentialData CreateTotpCredential(
        string name = "user@example.com",
        string? issuer = "TestIssuer") =>
        new()
        {
            Name = name,
            OathType = OathType.Totp,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = [0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
                      0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30],
            Issuer = issuer
        };

    private static CredentialData CreateHotpCredential(
        string name = "hotp-user@example.com",
        string? issuer = "HotpIssuer",
        int counter = 0) =>
        new()
        {
            Name = name,
            OathType = OathType.Hotp,
            HashAlgorithm = OathHashAlgorithm.Sha1,
            Secret = [0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
                      0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30],
            Issuer = issuer,
            Counter = counter
        };

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task CredentialLifecycle_PutListCalculateDelete_Succeeds(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            // After reset, list should be empty
            var credentials = await session.ListCredentialsAsync(CancellationTokenSource.Token);
            Assert.Empty(credentials);

            // Put a TOTP credential
            var credData = CreateTotpCredential();
            await session.PutCredentialAsync(credData, cancellationToken: CancellationTokenSource.Token);

            // List should now contain the credential
            credentials = await session.ListCredentialsAsync(CancellationTokenSource.Token);
            Assert.Single(credentials);

            var credential = credentials[0];
            Assert.Equal("TestIssuer", credential.Issuer);
            Assert.Equal("user@example.com", credential.Name);
            Assert.Equal(OathType.Totp, credential.OathType);
            Assert.Equal(30, credential.Period);

            // Calculate a code using a fixed timestamp
            long timestamp = 1704067200; // 2024-01-01 00:00:00 UTC
            var code = await session.CalculateCodeAsync(credential, timestamp, CancellationTokenSource.Token);
            Assert.NotNull(code);
            Assert.Equal(6, code.Value.Length);
            Assert.True(code.ValidFrom <= timestamp);
            Assert.True(code.ValidTo > timestamp);

            // Delete the credential
            await session.DeleteCredentialAsync(credential, CancellationTokenSource.Token);

            // List should be empty again
            credentials = await session.ListCredentialsAsync(CancellationTokenSource.Token);
            Assert.Empty(credentials);
        }, cancellationToken: CancellationTokenSource.Token);

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task AccessKeyLifecycle_SetValidateUnset_Succeeds(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            // After reset, device should not be locked
            Assert.False(session.IsLocked);

            // Derive and set an access key
            string password = "test-password-123";
            byte[] key = session.DeriveKey(password);

            try
            {
                await session.SetKeyAsync(key, CancellationTokenSource.Token);

                // Create a new session to verify the key is required
                await using var lockedSession = await state.Device
                    .CreateOathSessionAsync(cancellationToken: CancellationTokenSource.Token);

                Assert.True(lockedSession.IsLocked);

                // Validate with the correct key
                byte[] validateKey = lockedSession.DeriveKey(password);
                try
                {
                    await lockedSession.ValidateAsync(validateKey, CancellationTokenSource.Token);
                    Assert.False(lockedSession.IsLocked);
                }
                finally
                {
                    System.Security.Cryptography.CryptographicOperations.ZeroMemory(validateKey);
                }

                // Unset the key (using original unlocked session)
                await session.UnsetKeyAsync(CancellationTokenSource.Token);

                // Verify device is no longer locked
                await using var unlockedSession = await state.Device
                    .CreateOathSessionAsync(cancellationToken: CancellationTokenSource.Token);

                Assert.False(unlockedSession.IsLocked);
            }
            finally
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
            }
        }, cancellationToken: CancellationTokenSource.Token);

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.1")]
    public async Task RenameCredential_ChangesNameAndIssuer(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            // Put a credential
            var credData = CreateTotpCredential("original@example.com", "OriginalIssuer");
            await session.PutCredentialAsync(credData, cancellationToken: CancellationTokenSource.Token);

            var credentials = await session.ListCredentialsAsync(CancellationTokenSource.Token);
            Assert.Single(credentials);
            var original = credentials[0];

            // Rename
            var renamed = await session.RenameCredentialAsync(
                original, "NewIssuer", "renamed@example.com", CancellationTokenSource.Token);

            Assert.Equal("NewIssuer", renamed.Issuer);
            Assert.Equal("renamed@example.com", renamed.Name);
            Assert.Equal(original.OathType, renamed.OathType);
            Assert.Equal(original.Period, renamed.Period);

            // Verify via list
            credentials = await session.ListCredentialsAsync(CancellationTokenSource.Token);
            Assert.Single(credentials);
            Assert.Equal("NewIssuer", credentials[0].Issuer);
            Assert.Equal("renamed@example.com", credentials[0].Name);
        }, cancellationToken: CancellationTokenSource.Token);

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task CalculateAll_WithMultipleCredentials_ReturnsAllCodes(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            // Put multiple TOTP credentials
            var cred1 = CreateTotpCredential("alice@example.com", "ServiceA");
            var cred2 = CreateTotpCredential("bob@example.com", "ServiceB");

            await session.PutCredentialAsync(cred1, cancellationToken: CancellationTokenSource.Token);
            await session.PutCredentialAsync(cred2, cancellationToken: CancellationTokenSource.Token);

            // Calculate all
            long timestamp = 1704067200;
            var results = await session.CalculateAllAsync(timestamp, CancellationTokenSource.Token);

            Assert.Equal(2, results.Count);

            // All TOTP credentials should have non-null codes
            foreach (var (credential, code) in results)
            {
                Assert.NotNull(code);
                Assert.Equal(6, code.Value.Length);
                Assert.Equal(OathType.Totp, credential.OathType);
            }
        }, cancellationToken: CancellationTokenSource.Token);

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task CalculateAll_WithHotpCredential_ReturnsNullCode(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            // Put a TOTP and an HOTP credential
            var totpCred = CreateTotpCredential("totp@example.com", "TotpService");
            var hotpCred = CreateHotpCredential();

            await session.PutCredentialAsync(totpCred, cancellationToken: CancellationTokenSource.Token);
            await session.PutCredentialAsync(hotpCred, cancellationToken: CancellationTokenSource.Token);

            // Calculate all — HOTP should return null code
            long timestamp = 1704067200;
            var results = await session.CalculateAllAsync(timestamp, CancellationTokenSource.Token);

            Assert.Equal(2, results.Count);

            var hotpEntry = results.First(kv => kv.Key.OathType == OathType.Hotp);
            Assert.Null(hotpEntry.Value);

            var totpEntry = results.First(kv => kv.Key.OathType == OathType.Totp);
            Assert.NotNull(totpEntry.Value);
        }, cancellationToken: CancellationTokenSource.Token);

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task HotpCredential_PutAndCalculate_Succeeds(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            var credData = CreateHotpCredential();
            await session.PutCredentialAsync(credData, cancellationToken: CancellationTokenSource.Token);

            var credentials = await session.ListCredentialsAsync(CancellationTokenSource.Token);
            Assert.Single(credentials);

            var credential = credentials[0];
            Assert.Equal(OathType.Hotp, credential.OathType);
            Assert.Equal("HotpIssuer", credential.Issuer);

            // Calculate HOTP code (counter-based)
            var code1 = await session.CalculateCodeAsync(credential, cancellationToken: CancellationTokenSource.Token);
            Assert.NotNull(code1);
            Assert.Equal(6, code1.Value.Length);

            // Calculate again — HOTP counter increments, so code should differ
            var code2 = await session.CalculateCodeAsync(credential, cancellationToken: CancellationTokenSource.Token);
            Assert.NotNull(code2);
            Assert.Equal(6, code2.Value.Length);
            Assert.NotEqual(code1.Value, code2.Value);
        }, cancellationToken: CancellationTokenSource.Token);

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task Reset_ClearsAllCredentials(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            // Put a credential
            var credData = CreateTotpCredential();
            await session.PutCredentialAsync(credData, cancellationToken: CancellationTokenSource.Token);

            var credentials = await session.ListCredentialsAsync(CancellationTokenSource.Token);
            Assert.Single(credentials);

            string oldDeviceId = session.DeviceId;

            // Reset
            await session.ResetAsync(CancellationTokenSource.Token);

            // Credentials should be gone
            credentials = await session.ListCredentialsAsync(CancellationTokenSource.Token);
            Assert.Empty(credentials);

            // DeviceId changes after reset (new salt)
            Assert.NotEqual(oldDeviceId, session.DeviceId);
            Assert.False(session.IsLocked);
        }, cancellationToken: CancellationTokenSource.Token);

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task TouchRequiredCredential_CalculateAll_ReturnsNullCode(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            // Put a touch-required credential
            var credData = CreateTotpCredential("touch@example.com", "TouchService");
            await session.PutCredentialAsync(credData, requireTouch: true,
                cancellationToken: CancellationTokenSource.Token);

            // CalculateAll should return null code for touch-required credentials
            long timestamp = 1704067200;
            var results = await session.CalculateAllAsync(timestamp, CancellationTokenSource.Token);

            Assert.Single(results);
            var entry = results.First();
            Assert.Null(entry.Value);
            Assert.True(entry.Key.TouchRequired);
        }, cancellationToken: CancellationTokenSource.Token);
}
