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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivManagementKeyTests
{
    private static readonly byte[] DefaultManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };

    private static readonly byte[] DefaultAesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultManagementKey;

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task SetManagementKeyAsync_ChangesToNewKey(YubiKeyTestState state)
    {
        // Use appropriate key type based on firmware
        var newKeyType = state.FirmwareVersion >= new FirmwareVersion(5, 7, 0)
            ? PivManagementKeyType.Aes192
            : PivManagementKeyType.TripleDes;
        var newKey = new byte[] {
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22,
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22,
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22
        };

        try
        {
            await using (var session = await state.Device.CreatePivSessionAsync())
            {
                await session.ResetAsync();
                await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
                await session.SetManagementKeyAsync(newKeyType, newKey);
            }

            // Successive, not nested: the mutating session above has released the CCID interface.
            // The requirement is that the new key authenticates on a FRESH session, which is what
            // a consumer would do. Two live PIV sessions on one interface are refused by design —
            // they would share the card's security state.
            await using var verification = await state.Device.CreatePivSessionAsync();

            // Old key should fail
            await Assert.ThrowsAsync<ApduException>(
                () => verification.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion)));

            // New key should work
            await verification.AuthenticateAsync(newKey);
            Assert.True(verification.IsManagementKeyAuthenticated);
        }
        finally
        {
            await using var cleanup = await state.Device.CreatePivSessionAsync();
            await cleanup.ResetAsync();
        }
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.2")]
    public async Task SetManagementKeyAsync_AES256_Succeeds(YubiKeyTestState state)
    {
        // AES256 = 32 bytes
        var aes256Key = new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
        };

        try
        {
            await using (var session = await state.Device.CreatePivSessionAsync())
            {
                await session.ResetAsync();
                await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

                // Change to AES256
                await session.SetManagementKeyAsync(PivManagementKeyType.Aes256, aes256Key);

                // Verify via metadata
                var metadata = await session.GetManagementKeyMetadataAsync();
                Assert.Equal(PivManagementKeyType.Aes256, metadata.KeyType);
                Assert.False(metadata.IsDefault);
            }

            // Successive, not nested — see SetManagementKeyAsync_ChangesToNewKey.
            await using var verification = await state.Device.CreatePivSessionAsync();
            await verification.AuthenticateAsync(aes256Key);
            Assert.True(verification.IsManagementKeyAuthenticated);
        }
        finally
        {
            await using var cleanup = await state.Device.CreatePivSessionAsync();
            await cleanup.ResetAsync();
        }
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.2")]
    public async Task SetManagementKeyAsync_AES128_Succeeds(YubiKeyTestState state)
    {
        // AES128 = 16 bytes
        var aes128Key = new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18
        };

        try
        {
            await using (var session = await state.Device.CreatePivSessionAsync())
            {
                await session.ResetAsync();
                await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

                // Change to AES128
                await session.SetManagementKeyAsync(PivManagementKeyType.Aes128, aes128Key);

                // Verify via metadata
                var metadata = await session.GetManagementKeyMetadataAsync();
                Assert.Equal(PivManagementKeyType.Aes128, metadata.KeyType);
                Assert.False(metadata.IsDefault);
            }

            // Successive, not nested — see SetManagementKeyAsync_ChangesToNewKey.
            await using var verification = await state.Device.CreatePivSessionAsync();
            await verification.AuthenticateAsync(aes128Key);
            Assert.True(verification.IsManagementKeyAuthenticated);
        }
        finally
        {
            await using var cleanup = await state.Device.CreatePivSessionAsync();
            await cleanup.ResetAsync();
        }
    }

    /// <summary>
    /// Destructively verifies same-session management-key state across SET and RESET transitions.
    /// </summary>
    /// <remarks>
    /// This test resets the PIV application before use and again from a fresh cleanup session.
    /// Run only on an explicitly authorized test YubiKey.
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.2")]
    public async Task SetManagementKeyAsync_SameSessionStateTracksSuccessfulSetAndReset(YubiKeyTestState state)
    {
        byte[] aes128Key =
        [
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
            0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27
        ];
        PivSession? session = null;

        try
        {
            session = await state.Device.CreatePivSessionAsync();
            await session.ResetAsync();
            await session.AuthenticateAsync(DefaultManagementKey);

            await session.SetManagementKeyAsync(PivManagementKeyType.Aes128, aes128Key);

            Assert.Equal(PivManagementKeyType.Aes128, session.ManagementKeyType);
            Assert.True(session.IsManagementKeyAuthenticated);

            // Key generation is management-key privileged and proves SET left the new key
            // authenticated in this same physical card session without reauthentication.
            _ = await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);

            await session.ResetAsync();

            var resetMetadata = await session.GetManagementKeyMetadataAsync();
            var expectedResetType = state.FirmwareVersion >= new FirmwareVersion(5, 7, 0)
                ? PivManagementKeyType.Aes192
                : PivManagementKeyType.TripleDes;
            Assert.False(session.IsManagementKeyAuthenticated);
            Assert.Equal(expectedResetType, session.ManagementKeyType);
            Assert.Equal(resetMetadata.KeyType, session.ManagementKeyType);

            await session.AuthenticateAsync(DefaultManagementKey);
            Assert.True(session.IsManagementKeyAuthenticated);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(aes128Key);
            try
            {
                if (session is not null)
                {
                    await session.DisposeAsync();
                }
            }
            finally
            {
                // Cleanup must not depend on the possibly-failed session state or changed key.
                await using var cleanupSession = await state.Device.CreatePivSessionAsync();
                await cleanupSession.ResetAsync();
            }
        }
    }
}
