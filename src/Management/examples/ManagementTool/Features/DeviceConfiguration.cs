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

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features.Results;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Features;

/// <summary>
/// Demonstrates configuring YubiKey device settings.
/// </summary>
/// <remarks>
/// <para>
/// This class provides examples for setting device configuration including
/// USB/NFC capabilities, timeouts, device flags, and lock codes.
/// Wrappers convert operation exceptions, including cancellation, into failure results; they do not prompt.
/// </para>
/// <para>
/// Keep borrowed lock codes valid and unchanged until completion, then zero owned source arrays with
/// <see cref="System.Security.Cryptography.CryptographicOperations.ZeroMemory"/>.
/// </para>
/// </remarks>
public static class DeviceConfiguration
{
    /// <summary>
    /// Sets the enabled capabilities for a transport.
    /// </summary>
    /// <param name="session">A Management session.</param>
    /// <param name="transport">The transport (USB or NFC).</param>
    /// <param name="capabilities">Complete replacement enabled set; omitted bits are disabled.
    /// Preserve a supported USB application and a recovery route; the SDK only rejects exact USB None.</param>
    /// <param name="lockCode">The current lock code if device is locked.</param>
    /// <param name="reboot">Whether to reboot the device after configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    /// <example>
    /// <code>
    /// var result = await DeviceConfiguration.SetCapabilitiesAsync(
    ///     session,
    ///     Transport.Usb,
    ///     DeviceCapabilities.Piv | DeviceCapabilities.Fido2,
    ///     lockCode: null,
    ///     reboot: true,
    ///     ct);
    /// </code>
    /// </example>
    public static async Task<ConfigResult> SetCapabilitiesAsync(
        IManagementSession session,
        Transport transport,
        DeviceCapabilities capabilities,
        byte[]? lockCode = null,
        bool reboot = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var config = transport switch
            {
                Transport.Usb => new DeviceConfig { UsbEnabled = capabilities },
                Transport.Nfc => new DeviceConfig { NfcEnabled = capabilities },
                _ => throw new ArgumentOutOfRangeException(nameof(transport), transport, null)
            };

            await session.SetDeviceConfigAsync(
                config,
                new SetDeviceConfigOptions { Reboot = reboot, CurrentLockCode = lockCode },
                cancellationToken);
            return ConfigResult.Succeeded(reboot);
        }
        catch (Exception ex)
        {
            return ConfigResult.Failed($"Failed to set capabilities: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets the device timeouts.
    /// </summary>
    /// <param name="session">A Management session.</param>
    /// <param name="autoEjectTimeout">CCID inactivity timeout in seconds (SDK range 0-3600), or null to
    /// leave unchanged. Zero disables auto-eject. Nonzero needs TouchEject in CCID-only mode; this does not set the flag.</param>
    /// <param name="challengeResponseTimeout">OTP challenge-response timeout in seconds (SDK range 0-60),
    /// or null to leave unchanged. Zero requests the device default, normally 15 seconds. SDK limits are not firmware maxima.</param>
    /// <param name="lockCode">The current lock code if device is locked.</param>
    /// <param name="reboot">Whether to reboot the device after configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public static async Task<ConfigResult> SetTimeoutsAsync(
        IManagementSession session,
        ushort? autoEjectTimeout = null,
        byte? challengeResponseTimeout = null,
        byte[]? lockCode = null,
        bool reboot = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var config = new DeviceConfig
            {
                AutoEjectTimeout = autoEjectTimeout,
                ChallengeResponseTimeout = challengeResponseTimeout
            };
            await session.SetDeviceConfigAsync(
                config,
                new SetDeviceConfigOptions { Reboot = reboot, CurrentLockCode = lockCode },
                cancellationToken);
            return ConfigResult.Succeeded(reboot);
        }
        catch (Exception ex)
        {
            return ConfigResult.Failed($"Failed to set timeouts: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets the device flags.
    /// </summary>
    /// <param name="session">A Management session.</param>
    /// <param name="flags">Complete replacement flag set. TouchEject is effective only in CCID-only mode.</param>
    /// <param name="lockCode">The current lock code if device is locked.</param>
    /// <param name="reboot">Whether to reboot the device after configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public static async Task<ConfigResult> SetDeviceFlagsAsync(
        IManagementSession session,
        DeviceFlags flags,
        byte[]? lockCode = null,
        bool reboot = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var config = new DeviceConfig { DeviceFlags = flags };

            await session.SetDeviceConfigAsync(
                config,
                new SetDeviceConfigOptions { Reboot = reboot, CurrentLockCode = lockCode },
                cancellationToken);
            return ConfigResult.Succeeded(reboot);
        }
        catch (Exception ex)
        {
            return ConfigResult.Failed($"Failed to set device flags: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends the encoded NFC-restriction request; not exposed by the current menus.
    /// </summary>
    /// <param name="session">A Management session.</param>
    /// <param name="restricted">True encodes restriction; false encodes zero but is not a verified recovery route.
    /// For NFC-capable 5.7+ keys, documented recovery is USB power for three seconds. The configured NDEF URL
    /// remains readable while restricted. The SDK does not preflight those field-specific requirements.</param>
    /// <param name="lockCode">The current lock code if device is locked.</param>
    /// <param name="reboot">Whether to reboot the device after configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public static async Task<ConfigResult> SetNfcRestrictedAsync(
        IManagementSession session,
        bool restricted,
        byte[]? lockCode = null,
        bool reboot = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var config = new DeviceConfig { NfcRestricted = restricted };

            await session.SetDeviceConfigAsync(
                config,
                new SetDeviceConfigOptions { Reboot = reboot, CurrentLockCode = lockCode },
                cancellationToken);
            return ConfigResult.Succeeded(reboot);
        }
        catch (Exception ex)
        {
            return ConfigResult.Failed($"Failed to set NFC restricted: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets or changes the configuration lock code.
    /// </summary>
    /// <param name="session">A Management session.</param>
    /// <param name="currentLockCode">The current lock code (null if not set).</param>
    /// <param name="newLockCode">The new lock code (16 bytes); all zero bytes remove the lock.
    /// The current code authorizes this command only, not subsequent writes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    /// <remarks>
    /// <para>
    /// <b>Security Note:</b> Both lock codes must be zeroed after use:
    /// </para>
    /// <code>
    /// byte[]? current = null;
    /// byte[]? newCode = null;
    /// try
    /// {
    ///     current = GetCurrentCode();
    ///     newCode = GetNewCode();
    ///     await DeviceConfiguration.SetLockCodeAsync(session, current, newCode, ct);
    /// }
    /// finally
    /// {
    ///     if (current is not null) CryptographicOperations.ZeroMemory(current);
    ///     if (newCode is not null) CryptographicOperations.ZeroMemory(newCode);
    /// }
    /// </code>
    /// </remarks>
    public static async Task<ConfigResult> SetLockCodeAsync(
        IManagementSession session,
        byte[]? currentLockCode,
        byte[] newLockCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(newLockCode);

        if (newLockCode.Length != 16)
        {
            return ConfigResult.Failed("Lock code must be exactly 16 bytes.");
        }

        try
        {
            var config = new DeviceConfig();
            await session.SetDeviceConfigAsync(
                config,
                new SetDeviceConfigOptions { CurrentLockCode = currentLockCode, NewLockCode = newLockCode },
                cancellationToken);
            return ConfigResult.Succeeded(rebootRequired: false);
        }
        catch (Exception ex)
        {
            return ConfigResult.Failed($"Failed to set lock code: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes the configuration lock code by setting it to all zeros.
    /// </summary>
    /// <param name="session">A Management session.</param>
    /// <param name="currentLockCode">The current lock code (16 bytes).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public static async Task<ConfigResult> RemoveLockCodeAsync(
        IManagementSession session,
        byte[] currentLockCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(currentLockCode);

        if (currentLockCode.Length != 16)
        {
            return ConfigResult.Failed("Lock code must be exactly 16 bytes.");
        }

        byte[]? zeroCode = null;
        try
        {
            zeroCode = new byte[16]; // All zeros
            var config = new DeviceConfig();
            await session.SetDeviceConfigAsync(
                config,
                new SetDeviceConfigOptions { CurrentLockCode = currentLockCode, NewLockCode = zeroCode },
                cancellationToken);
            return ConfigResult.Succeeded(rebootRequired: false);
        }
        catch (Exception ex)
        {
            return ConfigResult.Failed($"Failed to remove lock code: {ex.Message}");
        }
        finally
        {
            if (zeroCode is not null)
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(zeroCode);
            }
        }
    }
}