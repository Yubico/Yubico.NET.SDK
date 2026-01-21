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
using Yubico.YubiKit.Tests.Shared.Infrastructure;

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
    private const int PlaceholderSerialNumber = -1;
    private static int s_placeholderCounter;

    /// <summary>
    ///     Gets a placeholder instance used during test discovery.
    /// </summary>
    /// <remarks>
    ///     During discovery, we don't want to connect to actual hardware.
    ///     This placeholder is returned instead and tests are re-enumerated
    ///     with real devices when actually run.
    /// </remarks>
    public static YubiKeyTestState Placeholder { get; } = new(isPlaceholder: true, filterDescription: null);

    /// <summary>
    ///     Creates a unique placeholder with a specific filter description.
    ///     Used to avoid duplicate test IDs when multiple [WithYubiKey] attributes
    ///     are applied to the same test method.
    /// </summary>
    /// <param name="filterDescription">A description of the filter criteria.</param>
    /// <returns>A new placeholder instance with unique identity.</returns>
    public static YubiKeyTestState CreatePlaceholder(string? filterDescription) =>
        new(isPlaceholder: true, filterDescription: filterDescription);

    /// <summary>
    ///     Gets whether this is a placeholder instance (used during discovery).
    /// </summary>
    public bool IsPlaceholder { get; private set; }

    /// <summary>
    ///     Gets the unique ID for this placeholder (0 if not a placeholder or no filter).
    /// </summary>
    private int PlaceholderId { get; set; }

    /// <summary>
    ///     Gets the filter description for this placeholder (null for real devices).
    /// </summary>
    private string? FilterDescription { get; set; }

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
    ///     Private constructor for creating placeholders.
    /// </summary>
    private YubiKeyTestState(bool isPlaceholder, string? filterDescription = null)
    {
        IsPlaceholder = isPlaceholder;
        ConnectionType = ConnectionType.Unknown;
        FilterDescription = filterDescription;

        // Each placeholder gets a unique ID to avoid duplicate test IDs
        if (isPlaceholder && filterDescription is not null)
            PlaceholderId = Interlocked.Increment(ref s_placeholderCounter);
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
    ///     For placeholders, accessing this property triggers lazy device binding.
    /// </summary>
    public IYubiKey Device
    {
        get
        {
            if (_device is null && IsPlaceholder)
            {
                BindToRealDevice();
            }

            return _device!;
        }
        private set => _device = value;
    }

    private IYubiKey? _device;

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
    ///     xUnit calls this method during BOTH discovery and execution.
    ///     We cannot distinguish between the two phases here.
    ///     For placeholders, we just restore the placeholder state - actual device binding
    ///     happens lazily when the Device property is accessed during test execution.
    /// </remarks>
    public void Deserialize(IXunitSerializationInfo info)
    {
        var serialNumber = info.GetValue<int>(nameof(SerialNumber));
        var connectionType = info.GetValue<ConnectionType>(nameof(ConnectionType));
        var isPlaceholder = info.GetValue<bool>(nameof(IsPlaceholder));
        var filterDescription = info.GetValue<string?>("FilterDescription");

        // If this was a placeholder during discovery, restore placeholder state
        // Actual device binding happens lazily when Device property is accessed
        if (isPlaceholder)
        {
            IsPlaceholder = true;
            FilterDescription = filterDescription;
            ConnectionType = ConnectionType.Unknown;
            // Don't initialize infrastructure here - it crashes during discovery
            // The Device property getter will handle lazy binding during execution
            return;
        }

        // Normal deserialization - look up device from cache
        var deviceFromCache = YubiKeyDeviceCache.GetDevice(serialNumber, connectionType);
        if (deviceFromCache is null)
            throw new InvalidOperationException(
                $"Device with serial number {serialNumber} and connection type {connectionType} not found in cache. " +
                "This should not happen - device should be cached during initialization.");

        Device = deviceFromCache.Device;
        DeviceInfo = deviceFromCache.DeviceInfo;
        ConnectionType = deviceFromCache.ConnectionType;
    }

    /// <summary>
    ///     Binds this placeholder to a real YubiKey device.
    ///     Called lazily when Device property is accessed during test execution.
    /// </summary>
    private void BindToRealDevice()
    {
        // Initialize infrastructure (triggers device discovery)
        var allDevices = YubiKeyTestInfrastructure.AllAuthorizedDevices;

        if (allDevices.Count == 0)
            throw new Xunit.SkipException(
                "No authorized YubiKey devices available. " +
                "Add device serial numbers to appsettings.json AllowedSerialNumbers array.");

        // For now, just pick the first available device
        // TODO: Parse FilterDescription and apply filters to select appropriate device
        var device = allDevices[0];
        _device = device.Device;
        DeviceInfo = device.DeviceInfo;
        ConnectionType = device.ConnectionType;
        IsPlaceholder = false;
    }

    /// <summary>
    ///     Serializes test device data for xUnit.
    /// </summary>
    /// <param name="info">The serialization information.</param>
    /// <remarks>
    ///     xUnit calls this method during test discovery.
    ///     For placeholders, we serialize the placeholder flag and filter description.
    ///     For real devices, we serialize serial number and connection type.
    /// </remarks>
    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(IsPlaceholder), IsPlaceholder);

        if (IsPlaceholder)
        {
            // Serialize placeholder info for later binding
            info.AddValue(nameof(SerialNumber), 0);
            info.AddValue(nameof(ConnectionType), ConnectionType.Unknown);
            info.AddValue("FilterDescription", FilterDescription);
        }
        else
        {
            // Serialize real device info
            info.AddValue(nameof(SerialNumber), DeviceInfo.SerialNumber);
            info.AddValue(nameof(ConnectionType), ConnectionType);
            info.AddValue("FilterDescription", (string?)null);
        }
    }

    #endregion

    /// <summary>
    ///     Returns a friendly string representation for test output.
    /// </summary>
    /// <returns>
    ///     A formatted string like: <c>YubiKey(SN:12345678,FW:5.7.2,UsbAKeychain,Ccid)</c>
    /// </returns>
    public override string ToString()
    {
        if (!IsPlaceholder)
            return
                $"YubiKey(SN:{DeviceInfo.SerialNumber},FW:{DeviceInfo.FirmwareVersion},{DeviceInfo.FormFactor},{ConnectionType})";

        return FilterDescription is not null
            ? $"YubiKey(Placeholder #{PlaceholderId}: {FilterDescription})"
            : "YubiKey(Placeholder - device will be bound at runtime)";
    }

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