// Copyright 2026 Yubico AB
// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

// Run only the individually authorized case on an allowlisted, pre-provisioned fixture (see PIV README).
// These tests intentionally leave the target credential changes in place for attended qualification.
// No reset, retry-limit changes, PUK manipulation, slot or attestation writes, or blind cleanup.
[Trait(TestCategories.Category, TestCategories.RequiresHardware)]
[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)] // Attended fixture provisioning and credential-state checks.
public class PivMetadataMutationTests
{
    [SkippableTheory]
    [WithYubiKey(Capability = DeviceCapabilities.Piv, ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task DerivedPinChange_RecoversWithNewPinAfterReopen(YubiKeyTestState state)
    {
        byte[] currentPin = "123456"u8.ToArray();
        byte[] newPin = "678912"u8.ToArray();
        try
        {
            byte[] originalSalt;
            await using (var session = await state.Device.CreatePivSessionAsync())
            {
                originalSalt = await RequireDerivedFixtureAsync(session);
                await session.ChangePinWithMetadataAsync(currentPin, newPin);
            }

            await using var reopened = await state.Device.CreatePivSessionAsync();
            byte[] salt = await RequireDerivedFixtureAsync(reopened);
            Assert.False(originalSalt.AsSpan().SequenceEqual(salt)); // Salts are public metadata.
            Assert.False(reopened.IsManagementKeyAuthenticated);
            Assert.Equal(PinOnlyMode.PinDerived, await reopened.RecoverPinOnlyModeAsync(newPin));
            Assert.True(reopened.IsManagementKeyAuthenticated);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(currentPin);
            CryptographicOperations.ZeroMemory(newPin);
        }
    }

    [SkippableTheory]
    [WithYubiKey(Capability = DeviceCapabilities.Piv, ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.0")]
    public async Task ProtectedRotation_RecoversReplacementAfterReopen(YubiKeyTestState state)
    {
        byte[] pin = "123456"u8.ToArray();
        byte[] replacement = RandomNumberGenerator.GetBytes(24);
        try
        {
            int pukRetries;
            await using (var session = await state.Device.CreatePivSessionAsync())
            {
                Assert.Equal(PinOnlyMode.PinProtected, await session.GetPinOnlyModeAsync());
                pukRetries = (await session.GetPukMetadataAsync()).RetriesRemaining;
                Assert.Equal(PinOnlyMode.PinProtected, await session.RecoverPinOnlyModeAsync(pin));
                await session.VerifyPinAsync(pin);
                await session.SetManagementKeyWithMetadataAsync(KeyType.Aes192, replacement, ManagementKeyStorage.PinProtected);
            }

            await using var reopened = await state.Device.CreatePivSessionAsync();
            Assert.False(reopened.IsManagementKeyAuthenticated);
            Assert.Equal(KeyType.Aes192, reopened.ManagementKeyType);
            Assert.Equal(PinOnlyMode.PinProtected, await reopened.RecoverPinOnlyModeAsync(pin));
            Assert.True(reopened.IsManagementKeyAuthenticated);
            await reopened.AuthenticateAsync(replacement);
            Assert.Equal(pukRetries, (await reopened.GetPukMetadataAsync()).RetriesRemaining);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pin);
            CryptographicOperations.ZeroMemory(replacement);
        }
    }

    [SkippableTheory]
    [WithYubiKey(Capability = DeviceCapabilities.Piv, ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.0")]
    public async Task DerivedRotation_ClearsSaltAndAuthenticatesReplacementAfterReopen(YubiKeyTestState state)
    {
        byte[] pin = "123456"u8.ToArray();
        // Store the replacement in PIN-gated PRINTED so fixture credentials remain recoverable after the test.
        byte[] replacement = RandomNumberGenerator.GetBytes(24);
        try
        {
            await using (var session = await state.Device.CreatePivSessionAsync())
            {
                _ = await RequireDerivedFixtureAsync(session);
                Assert.Equal(PinOnlyMode.PinDerived, await session.RecoverPinOnlyModeAsync(pin));
                await session.VerifyPinAsync(pin);
                await session.SetManagementKeyWithMetadataAsync(KeyType.Aes192, replacement, ManagementKeyStorage.PinProtected);
            }

            await using var reopened = await state.Device.CreatePivSessionAsync();
            Assert.Null((await reopened.GetAdminDataAsync()).Salt);
            Assert.Equal(PinOnlyMode.PinProtected, await reopened.GetPinOnlyModeAsync());
            await reopened.AuthenticateAsync(replacement);
            Assert.True(reopened.IsManagementKeyAuthenticated);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pin);
            CryptographicOperations.ZeroMemory(replacement);
        }
    }

    private static async Task<byte[]> RequireDerivedFixtureAsync(IPivSession session)
    {
        Assert.Equal(PinOnlyMode.PinDerived, await session.GetPinOnlyModeAsync());
        Assert.Equal(KeyType.TripleDes, session.ManagementKeyType);
        var salt = (await session.GetAdminDataAsync()).Salt
            ?? throw new InvalidOperationException("The authorized fixture needs a legacy derivation salt.");
        Assert.Equal(16, salt.Length);
        return salt.ToArray();
    }
}