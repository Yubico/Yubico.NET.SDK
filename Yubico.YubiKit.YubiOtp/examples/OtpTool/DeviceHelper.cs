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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool;

/// <summary>
/// Handles YubiKey device discovery for OTP operations.
/// Auto-selects when exactly one device is connected (non-interactive).
/// </summary>
public static class DeviceHelper
{
    private static readonly ConnectionType[] SupportedConnectionTypes =
    [
        ConnectionType.SmartCard,
        ConnectionType.HidOtp
    ];

    /// <summary>
    /// Finds a YubiKey and creates a YubiOTP session.
    /// Auto-selects when exactly one YubiKey is connected.
    /// Fails with an error when zero or multiple devices are found.
    /// </summary>
    public static async Task<YubiOtpSession> CreateSessionAsync(CancellationToken cancellationToken)
    {
        var device = await SelectDeviceAsync(cancellationToken);
        return await device.CreateYubiOtpSessionAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Finds and selects a YubiKey device (non-interactive).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no devices are found or multiple devices are connected.
    /// </exception>
    public static async Task<IYubiKey> SelectDeviceAsync(CancellationToken cancellationToken)
    {
        var allDevices = await YubiKeyManager.FindAllAsync(
            ConnectionType.All,
            cancellationToken: cancellationToken);

        var devices = allDevices
            .Where(d => SupportedConnectionTypes.Contains(d.ConnectionType))
            .ToList();

        if (devices.Count is 0)
        {
            throw new InvalidOperationException(
                "No YubiKey detected. Insert a YubiKey with SmartCard (CCID) or OTP HID support.");
        }

        // A single YubiKey may appear on multiple transports (SmartCard + HidOtp).
        // Prefer SmartCard for richer protocol support.
        return devices
            .OrderBy(d => d.ConnectionType == ConnectionType.SmartCard ? 0 : 1)
            .First();
    }
}