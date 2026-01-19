// Copyright 2025 Yubico AB
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

using Xunit.Abstractions;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Tests.Shared;

/// <summary>
///     Wrapper class for YubiKey devices used in parameterized tests.
///     Implements <see cref="IXunitSerializable" /> for xUnit Theory data sources.
/// </summary>
/// <remarks>
///     <para>
///         This class wraps an <see cref="IYubiKey" /> and its <see cref="Management.DeviceInfo" />
///         for use with xUnit Theory tests via <see cref="Yubico.YubiKit.Tests.Shared.Infrastructure" />.
///     </para>
///     <para>
///         The <see cref="ToString" /> method provides a friendly display name for test output:
///         <c>YubiKey(SN:12345678,FW:5.7.2,UsbAKeychain)</c>
///     </para>
/// </remarks>
public class YubiKeyTestState : IXunitSerializable
{
    /// <summary>
    ///     Parameterless constructor required by <see cref="IXunitSerializable" />.
    /// </summary>
    /// <remarks>
    ///     This constructor is used by xUnit for deserialization.
    ///     Do not use directly - use the constructor with parameters.
    /// </remarks>
    // ReSharper disable once UnusedMember.Global
    public YubiKeyTestState()
    {
        // Required for IXunitSerializable
    }

    /// <summary>
    ///     Initializes a new instance with the specified device and device information.
    /// </summary>
    /// <param name="device">The YubiKey device.</param>
    /// <param name="deviceInfo">The device information.</param>
    /// <param name="connectionType">The connection type for this device instance.</param>
    public YubiKeyTestState(IYubiKey device, DeviceInfo deviceInfo, ConnectionType connectionType)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        DeviceInfo = deviceInfo;
        ConnectionType = connectionType;
    }

    /// <summary>
    ///     Gets the YubiKey device instance.
    /// </summary>
    public IYubiKey Device { get; private set; } = null!;

    /// <summary>
    ///     Gets the device information (firmware, form factor, capabilities, etc.).
    /// </summary>
    public DeviceInfo DeviceInfo { get; private set; }

    /// <summary>
    ///     Gets the firmware version.
    ///     Convenience property for <c>Info.FirmwareVersion</c>.
    /// </summary>
    public FirmwareVersion FirmwareVersion => DeviceInfo.FirmwareVersion;

    /// <summary>
    ///     Gets the form factor.
    ///     Convenience property for <c>Info.FormFactor</c>.
    /// </summary>
    public FormFactor FormFactor => DeviceInfo.FormFactor;

    /// <summary>
    ///     Gets the device serial number.
    ///     Convenience property for <c>Info.SerialNumber</c>.
    /// </summary>
    public int? SerialNumber => DeviceInfo.SerialNumber;

    /// <summary>
    ///     Gets the connection type for this device instance.
    /// </summary>
    public ConnectionType ConnectionType { get; private set; }

    /// <summary>
    ///     Gets whether the device supports USB transport.
    /// </summary>
    public bool IsUsbTransport => DeviceInfo.UsbSupported != DeviceCapabilities.None;

    /// <summary>
    ///     Gets whether the device supports NFC transport.
    /// </summary>
    public bool IsNfcTransport => DeviceInfo.NfcSupported != DeviceCapabilities.None;

    #region IXunitSerializable Members

    /// <summary>
    ///     Deserializes test device data from xUnit.
    /// </summary>
    /// <param name="info">The serialization information.</param>
    /// <remarks>
    ///     xUnit calls this method to reconstruct the device for test execution.
    ///     We use serial number and connection type to look up the device from the global cache.
    /// </remarks>
    public void Deserialize(IXunitSerializationInfo info)
    {
        var serialNumber = info.GetValue<int>(nameof(SerialNumber));
        var connectionType = info.GetValue<ConnectionType>(nameof(ConnectionType));

        // Look up device from static cache using composite key
        var deviceFromCache = YubiKeyDeviceCache.GetDevice(serialNumber, connectionType);
        if (deviceFromCache is null)
            throw new InvalidOperationException(
                $"Device with serial number {serialNumber} and connection type {connectionType} not found in cache. " +
                "This should not happen - device should be cached by YubiKeyTheoryDiscoverer.");

        Device = deviceFromCache.Device;
        DeviceInfo = deviceFromCache.DeviceInfo;
        ConnectionType = deviceFromCache.ConnectionType;
    }

    /// <summary>
    ///     Serializes test device data for xUnit.
    /// </summary>
    /// <param name="info">The serialization information.</param>
    /// <remarks>
    ///     xUnit calls this method during test discovery.
    ///     We serialize the serial number and connection type to create a composite cache key.
    /// </remarks>
    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(SerialNumber), DeviceInfo.SerialNumber);
        info.AddValue(nameof(ConnectionType), ConnectionType);
    }

    #endregion

    /// <summary>
    ///     Returns a friendly string representation for test output.
    /// </summary>
    /// <returns>
    ///     A formatted string like: <c>YubiKey(SN:12345678,FW:5.7.2,UsbAKeychain,Ccid)</c>
    /// </returns>
    public override string ToString() =>
        $"YubiKey(SN:{DeviceInfo.SerialNumber},FW:{DeviceInfo.FirmwareVersion},{DeviceInfo.FormFactor},{ConnectionType})";

    /// <summary>
    ///     Checks if the device is FIPS-capable for the specified capability.
    /// </summary>
    /// <param name="capability">The capability to check.</param>
    /// <returns>True if FIPS-capable; otherwise, false.</returns>
    public bool IsFipsCapable(DeviceCapabilities capability) => (DeviceInfo.FipsCapabilities & capability) != 0;

    /// <summary>
    ///     Checks if the device is in FIPS-approved mode for the specified capability.
    /// </summary>
    /// <param name="capability">The capability to check.</param>
    /// <returns>True if FIPS-approved; otherwise, false.</returns>
    public bool IsFipsApproved(DeviceCapabilities capability) => (DeviceInfo.FipsApproved & capability) != 0;

    /// <summary>
    ///     Checks if the device has the specified capability enabled (USB or NFC).
    /// </summary>
    /// <param name="capability">The capability to check.</param>
    /// <returns>True if capability is enabled on USB or NFC; otherwise, false.</returns>
    public bool HasCapability(DeviceCapabilities capability) =>
        (DeviceInfo.UsbEnabled & capability) != 0 || (DeviceInfo.NfcEnabled & capability) != 0;
}

/// <summary>
///     Static cache for YubiKey devices shared across test data attributes.
/// </summary>
/// <remarks>
///     xUnit serializes/deserializes test parameters, but we can't serialize IYubiKey objects.
///     This cache stores devices by composite key (serial number + connection type) so we can 
///     reconstruct them during deserialization.
/// </remarks>
internal static class YubiKeyDeviceCache
{
    private static readonly Dictionary<string, YubiKeyTestState> s_devices = new();
    private static readonly object s_lock = new();

    /// <summary>
    ///     Gets the cache key for a device.
    /// </summary>
    private static string GetCacheKey(int serialNumber, ConnectionType connectionType) =>
        $"{serialNumber}:{connectionType}";

    /// <summary>
    ///     Adds a device to the cache.
    /// </summary>
    public static void AddDevice(YubiKeyTestState state)
    {
        lock (s_lock)
        {
            var key = GetCacheKey(state.SerialNumber.GetValueOrDefault(), state.ConnectionType);
            s_devices[key] = state;
        }
    }

    /// <summary>
    ///     Gets a device from the cache by serial number and connection type.
    /// </summary>
    public static YubiKeyTestState? GetDevice(int serialNumber, ConnectionType connectionType)
    {
        lock (s_lock)
        {
            var key = GetCacheKey(serialNumber, connectionType);
            return s_devices.GetValueOrDefault(key);
        }
    }

    /// <summary>
    ///     Clears all cached devices.
    /// </summary>
    public static void Clear()
    {
        lock (s_lock)
        {
            s_devices.Clear();
        }
    }

    /// <summary>
    ///     Gets all cached devices.
    /// </summary>
    public static IReadOnlyList<YubiKeyTestState> GetAllDevices()
    {
        lock (s_lock)
        {
            return [.. s_devices.Values];
        }
    }
}