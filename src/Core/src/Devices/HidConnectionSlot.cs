// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>One live enumerated HID interface candidate that can open its raw FIDO or OTP connection.</summary>
internal sealed class HidConnectionSlot : IYubiKeyConnectionSlot, IDiscoveryConnectionProvider
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<HidConnectionSlot>();

    private readonly IHidDevice _hidDevice;
    private readonly IHidInputBridge? _inputBridge;
    private readonly IIOKitDeviceLifetime? _otpLifetime;

    internal HidConnectionSlot(IHidDevice hidDevice, IHidInputBridge? inputBridge = null, IIOKitDeviceLifetime? otpLifetime = null)
    {
        _hidDevice = hidDevice;
        _inputBridge = inputBridge;
        _otpLifetime = otpLifetime;
        InterfaceId = $"hid:{hidDevice.ReaderName}:{hidDevice.DescriptorInfo.Usage:X4}";
        ConnectionType = ConnectionTypeMapper.ToConnectionType(hidDevice.InterfaceType)
            .SingleConcreteConnectionOrUnknown();
        if (ConnectionType == ConnectionType.Unknown)
        {
            throw new NotSupportedException(
                $"HID interface type {hidDevice.InterfaceType} is not supported as a connection slot.");
        }
    }

    public string InterfaceId { get; }

    /// <summary>Either <see cref="ConnectionType.HidFido" /> or <see cref="ConnectionType.HidOtp" /> (ctor-enforced).</summary>
    public ConnectionType ConnectionType { get; }

    public Task<IConnection> OpenRawConnectionAsync(
        ConnectionType connection,
        CancellationToken cancellationToken = default)
    {
        if (connection != ConnectionType)
        {
            return Task.FromException<IConnection>(new NotSupportedException(
                $"Connection type {connection} is not supported by this device connection slot."));
        }

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IConnection>(cancellationToken);

        Logger.LogInformation(
            "Connecting to {ConnectionType} HID interface VID={VendorId:X4} PID={ProductId:X4}",
            ConnectionType,
            _hidDevice.DescriptorInfo.VendorId,
            _hidDevice.DescriptorInfo.ProductId);

        if (ConnectionType == ConnectionType.HidFido && _hidDevice is MacOSHidDevice macDevice)
            return OpenMacFidoAsync(macDevice, cancellationToken);

        if (ConnectionType == ConnectionType.HidOtp && _hidDevice is MacOSHidDevice macOtpDevice)
            return OpenMacOtpAsync(macOtpDevice, cancellationToken);

        // The ctor guarantees exactly these two values.
        return Task.FromResult<IConnection>(ConnectionType == ConnectionType.HidFido
            ? new FidoHidConnection(_hidDevice.ConnectToIOReports())
            : new OtpHidConnection(_hidDevice.ConnectToFeatureReports()));
    }

    private async Task<IConnection> OpenMacFidoAsync(MacOSHidDevice device, CancellationToken token) =>
        await MacOSFidoHidConnection.OpenAsync(device.EntryId, _inputBridge ?? new NativeHidInputBridge(), token).ConfigureAwait(false);

    private async Task<IConnection> OpenMacOtpAsync(MacOSHidDevice device, CancellationToken token) =>
        await MacOSOtpHidConnection.OpenAsync(device.EntryId, _otpLifetime ?? IOKitDeviceLifetime.Instance, token).ConfigureAwait(false);

    internal bool IsBuiltInMacFido => ConnectionType == ConnectionType.HidFido && _hidDevice is MacOSHidDevice;

    internal bool IsBuiltInMacOtp => ConnectionType == ConnectionType.HidOtp && _hidDevice is MacOSHidDevice;

    internal async Task<IConnection> OpenRegisteredConnectionAsync(IDisposable registration, CancellationToken token)
    {
        if (_hidDevice is not MacOSHidDevice device)
            throw new InvalidOperationException("Only built-in macOS HID owns its claim before native open.");
        return IsBuiltInMacFido
            ? await MacOSFidoHidConnection.OpenAsync(device.EntryId, _inputBridge ?? new NativeHidInputBridge(), token, registration).ConfigureAwait(false)
            : await MacOSOtpHidConnection.OpenAsync(device.EntryId, _otpLifetime ?? IOKitDeviceLifetime.Instance, token, registration).ConfigureAwait(false);
    }

    Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
        ConnectionType connection,
        CancellationToken cancellationToken) =>
        OpenRawConnectionAsync(connection, cancellationToken);
}
