PROPOSED SAMPLE — no verdict: core-001:unit:d75a3dbb4ec57c42 / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/Conventions.cs:32–35
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>--smoke: skip slow and user-presence tests.</summary>
    public static readonly string SmokeFilter = $"{IsNot(Slow)}&{IsNot(RequiresUserPresence)}";


PROPOSED SAMPLE — no verdict: core-001:unit:816c2fdd798610af / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Shared/src/Device/ConnectionTypeFormatter.cs:23–41
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>
    /// Formats a connection type as a human-readable string.
    /// </summary>
    public static string Format(ConnectionType connectionType)
    {
        if (connectionType == ConnectionType.Unknown)
            return "Unknown";

        var parts = new List<string>();
        if ((connectionType & ConnectionType.SmartCard) != 0)
            parts.Add("SmartCard");
        if ((connectionType & ConnectionType.HidFido) != 0)
            parts.Add("FIDO HID");
        if ((connectionType & ConnectionType.HidOtp) != 0)
            parts.Add("OTP HID");

        return parts.Count == 0 ? "Unknown" : string.Join(", ", parts);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:6e353becfd918253 / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Abstractions/IYubiKey.cs:177–186
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Whether this device can open the requested connection. Only concrete openable types are valid
    ///     (<see cref="ConnectionType.SmartCard"/>, <see cref="ConnectionType.HidFido"/>,
    ///     <see cref="ConnectionType.HidOtp"/>); <see cref="ConnectionType.Hid"/> means a HID interface is present;
    ///     <see cref="ConnectionType.Unknown"/>, <see cref="ConnectionType.All"/>, and other combinations return <c>false</c>.
    /// </summary>
    bool SupportsConnection(ConnectionType connectionType) =>
        AvailableConnections.SupportsConnection(connectionType);


PROPOSED SAMPLE — no verdict: core-001:unit:1fc213103a564722 / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/ConnectionTypeExtensions.cs:27–39
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Expands the HID group and ignores undefined bits, like discovery result filtering.</summary>
    /// <example>
    /// <code>
    /// ConnectionType.Hid.ToConcreteScope();                             // HidFido | HidOtp
    /// (ConnectionType.SmartCard | ConnectionType.Hid).ToConcreteScope(); // SmartCard | HidFido | HidOtp
    /// ((ConnectionType)0x40).ToConcreteScope();                         // Unknown: undefined bits are dropped
    /// </code>
    /// </example>
    public static ConnectionType ToConcreteScope(this ConnectionType scope) =>
        (scope & ConcreteConnections) |
        ((scope & ConnectionType.Hid) != 0 ? ConnectionType.HidFido | ConnectionType.HidOtp : ConnectionType.Unknown);


PROPOSED SAMPLE — no verdict: core-001:unit:d231181d1329769b / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/ConnectionTypeMapper.cs:23–35
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>
    /// Maps YubiKey HID interface type to the public ConnectionType API.
    /// </summary>
    /// <param name="interfaceType">The YubiKey HID interface type.</param>
    /// <returns>The corresponding <see cref="ConnectionType"/>.</returns>
    public static ConnectionType ToConnectionType(HidInterfaceType interfaceType) =>
        interfaceType switch
        {
            HidInterfaceType.Fido => ConnectionType.HidFido,
            HidInterfaceType.Otp => ConnectionType.HidOtp,
            _ => ConnectionType.Unknown
        };


PROPOSED SAMPLE — no verdict: core-001:unit:076b4d4328825902 / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DiscoveryReadSkippedException.cs:1–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Why a best-effort discovery device-info read was skipped. Carried on
///     <see cref="DiscoveryReadSkippedException" /> so logs and diagnostics can distinguish unsupported
///     discovery access, active connection ownership, worker-admission saturation, and transport
///     supersession.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:711e47d18bab0742 / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/HotplugSignals.cs:80–98
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Clears all callbacks, stops all listeners, disposes all listeners, then completes the signal channel.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        foreach (var (transport, listener) in _listeners)
            BestEffort(() => listener.Changed = null, $"clear {transport} listener callback");
        foreach (var (transport, listener) in _listeners)
            BestEffort(listener.Stop, $"stop {transport} listener");
        foreach (var (transport, listener) in _listeners)
            BestEffort(listener.Dispose, $"dispose {transport} listener");

        _channel.Writer.TryComplete();
        _logger.LogDebug("Device listeners torn down");
    }


PROPOSED SAMPLE — no verdict: core-001:unit:2671911eb64af1aa / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/ReaderNamePidParser.cs:38–49
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>All USB Product IDs this parser can produce, plus SKY. Used to gate mergeability.</summary>
    internal static readonly FrozenSet<ushort> KnownPids = new ushort[]
    {
        // YubiKey NEO
        0x0110, 0x0111, 0x0112, 0x0113, 0x0114, 0x0115, 0x0116,
        // Security Key
        SkyPid,
        // YubiKey 4/5 and later
        0x0401, 0x0402, 0x0403, 0x0404, 0x0405, 0x0406, 0x0407
    }.ToFrozenSet();


PROPOSED SAMPLE — no verdict: core-001:unit:fde9854638dd24be / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/RescanScheduler.cs:41–63
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Yields scan triggers sequentially until the channel completes. Dispose the enumeration when finished.</summary>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled.</exception>
    internal async IAsyncEnumerable<RescanTrigger> RunAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return RescanTrigger.Initial;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var trigger = await ReadAsync(_interval, cancellationToken).ConfigureAwait(false);
            if (trigger is { Reason: RescanReason.Hotplug } hotplug)
                trigger = await CoalesceAsync(hotplug.Transports, cancellationToken).ConfigureAwait(false);
            if (trigger is null)
                yield break;

            cancellationToken.ThrowIfCancellationRequested();
            yield return trigger.Value;
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:4a3cd5465f24843d / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/VersionQualifier.cs:61–72
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Initializes a new instance of the <see cref="VersionQualifier" /> class with default values.
    ///     The default firmware version is set to a new instance of <see cref="FirmwareVersion" />,
    ///     the type is set to <see cref="VersionQualifierType.Final" />, and the iteration is set to 0.
    /// </summary>
    public VersionQualifier()
    {
        FirmwareVersion = new FirmwareVersion();
        Type = VersionQualifierType.Final;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:3f972c5207254bdd / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/Infrastructure/AuthorizedDevices.cs:147–152
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Gets the count of authorized devices.
    /// </summary>
    public static int Count => GetAll().Count;


PROPOSED SAMPLE — no verdict: core-001:unit:36956745104ff731 / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/Infrastructure/FilterCriteria.cs:35–41
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Gets the required form factor.
    ///     <see cref="FormFactor.Unknown"/> means any form factor is accepted.
    /// </summary>
    public FormFactor FormFactor { get; init; } = FormFactor.Unknown;


PROPOSED SAMPLE — no verdict: core-001:unit:0f2b4a412a201ec2 / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/Infrastructure/FilterCriteria.cs:57–63
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Gets the required capability (must be enabled on the device).
    ///     <see cref="DeviceCapabilities.None"/> means no capability requirement.
    /// </summary>
    public DeviceCapabilities Capability { get; init; } = DeviceCapabilities.None;


PROPOSED SAMPLE — no verdict: core-001:unit:3d71e6d3bd862995 / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs:98–104
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Gets or sets the required capability (enabled over USB or NFC, independently of RequireUsb).
    ///     Use DeviceCapabilities.None (default) to skip capability filtering.
    /// </summary>
    public DeviceCapabilities Capability { get; set; } = DeviceCapabilities.None;


PROPOSED SAMPLE — no verdict: core-001:unit:20c9c91498bfd689 / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/YubiKeyTestState.cs:94–100
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Filter criteria for device binding (placeholders only).
    ///     Serialized with the placeholder and used during <see cref="BindToRealDevice"/>.
    /// </summary>
    private FilterCriteria _filterCriteria = new();


PROPOSED SAMPLE — no verdict: core-001:unit:2d7384815a34e691 / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/YubiKeyTestState.cs:223–229
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Gets the firmware version.
    ///     Convenience property for <c>DeviceInfo.FirmwareVersion</c>.
    /// </summary>
    public FirmwareVersion FirmwareVersion => DeviceInfo.FirmwareVersion;


PROPOSED SAMPLE — no verdict: core-001:unit:019eae4efe42d6e5 / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/YubiKeyTestState.cs:241–246
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Gets whether the device supports USB transport.
    /// </summary>
    public bool IsUsbTransport => DeviceInfo.UsbSupported != DeviceCapabilities.None;


PROPOSED SAMPLE — no verdict: core-001:unit:c20e4d6e55c8a0bb / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/YubiKeyTestState.cs:441–447
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Gets the cache key for a device.
    /// </summary>
    private static string GetCacheKey(int serialNumber, ConnectionType connectionType) =>
        $"{serialNumber}:{connectionType}";


PROPOSED SAMPLE — no verdict: core-001:unit:35700b4303b7b14b / A / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/YubiKeyTestState.cs:471–482
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


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


PROPOSED SAMPLE — no verdict: core-001:unit:c085f1cd3632e7c2 / A / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Devices/DiscoveryReadSkippedException.cs:26–31
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     A connection in this process owns (or is waiting for) the interface; discovery must not clobber it.
    /// </summary>
    InterfaceLeaseHeld,
