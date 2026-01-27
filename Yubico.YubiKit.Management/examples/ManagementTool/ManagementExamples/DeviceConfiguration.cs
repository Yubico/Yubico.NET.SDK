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
using Yubico.YubiKit.Management.Examples.ManagementTool.ManagementExamples.Results;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.ManagementExamples;

/// <summary>
/// Demonstrates configuring YubiKey device settings.
/// </summary>
/// <remarks>
/// <para>
/// This class provides examples for setting device configuration including
/// USB/NFC capabilities, timeouts, device flags, and lock codes.
/// </para>
/// <para>
/// <b>Security Note:</b> Lock codes must be zeroed after use with
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
    /// <param name="capabilities">The capabilities to enable.</param>
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
            var config = DeviceConfig.CreateBuilder()
                .WithCapabilities(transport, (int)capabilities)
                .Build();

            await session.SetDeviceConfigAsync(config, reboot, lockCode, cancellationToken: cancellationToken);
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
    /// <param name="autoEjectTimeout">Auto-eject timeout in seconds (0-3600), or null to leave unchanged.</param>
    /// <param name="challengeResponseTimeout">Challenge-response timeout in seconds (0-60), or null to leave unchanged.</param>
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
            var builder = DeviceConfig.CreateBuilder();

            if (autoEjectTimeout.HasValue)
            {
                builder.WithAutoEjectTimeout(autoEjectTimeout.Value);
            }

            if (challengeResponseTimeout.HasValue)
            {
                builder.WithChallengeResponseTimeout(challengeResponseTimeout.Value);
            }

            var config = builder.Build();
            await session.SetDeviceConfigAsync(config, reboot, lockCode, cancellationToken: cancellationToken);
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
    /// <param name="flags">The device flags to set.</param>
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
            var config = DeviceConfig.CreateBuilder()
                .WithDeviceFlags((byte)flags)
                .Build();

            await session.SetDeviceConfigAsync(config, reboot, lockCode, cancellationToken: cancellationToken);
            return ConfigResult.Succeeded(reboot);
        }
        catch (Exception ex)
        {
            return ConfigResult.Failed($"Failed to set device flags: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets whether NFC is restricted.
    /// </summary>
    /// <param name="session">A Management session.</param>
    /// <param name="restricted">Whether NFC should be restricted.</param>
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
            var config = DeviceConfig.CreateBuilder()
                .WithNfcRestricted(restricted)
                .Build();

            await session.SetDeviceConfigAsync(config, reboot, lockCode, cancellationToken: cancellationToken);
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
    /// <param name="newLockCode">The new lock code (16 bytes).</param>
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
            var config = DeviceConfig.CreateBuilder().Build();
            await session.SetDeviceConfigAsync(config, reboot: false, currentLockCode, newLockCode, cancellationToken);
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
            var config = DeviceConfig.CreateBuilder().Build();
            await session.SetDeviceConfigAsync(config, reboot: false, currentLockCode, zeroCode, cancellationToken);
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
