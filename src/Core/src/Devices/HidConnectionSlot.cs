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
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>One live enumerated HID interface candidate that can open its raw FIDO or OTP connection.</summary>
internal sealed class HidConnectionSlot : IYubiKeyConnectionSlot, IDiscoveryConnectionProvider
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<HidConnectionSlot>();

    private readonly IHidInterface _hidInterface;
    private readonly IHidInputBridge? _inputBridge;
    private readonly IIOKitDeviceLifetime? _otpLifetime;
    private readonly ILinuxHidReportApi? _linuxReportApi;
    private readonly ILinuxHidReadReactor? _linuxReadReactor;

    internal HidConnectionSlot(IHidInterface hidInterface, IHidInputBridge? inputBridge = null,
        IIOKitDeviceLifetime? otpLifetime = null, ILinuxHidReportApi? linuxReportApi = null,
        ILinuxHidReadReactor? linuxReadReactor = null)
    {
        _hidInterface = hidInterface;
        _inputBridge = inputBridge;
        _otpLifetime = otpLifetime;
        _linuxReportApi = linuxReportApi;
        _linuxReadReactor = linuxReadReactor;
        InterfaceId = $"hid:{hidInterface.ReaderName}:{hidInterface.DescriptorInfo.Usage:X4}";
        ConnectionType = ConnectionTypeMapper.ToConnectionType(hidInterface.InterfaceType)
            .SingleConcreteConnectionOrUnknown();
        if (ConnectionType == ConnectionType.Unknown)
        {
            throw new NotSupportedException(
                $"HID interface type {hidInterface.InterfaceType} is not supported as a connection slot.");
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
            _hidInterface.DescriptorInfo.VendorId,
            _hidInterface.DescriptorInfo.ProductId);

        if (OwnsNativeLifetime)
            return OpenBuiltInConnectionAsync(null, cancellationToken);

        // Windows and custom interfaces retain the synchronous compatibility adapters.
        return Task.FromResult<IConnection>(ConnectionType == ConnectionType.HidFido
            ? new FidoHidConnection(_hidInterface.ConnectToIOReports())
            : new OtpHidConnection(_hidInterface.ConnectToFeatureReports()));
    }

    /// <summary>The built-in connection accepts its claim before acquiring native resources.</summary>
    internal bool OwnsNativeLifetime => _hidInterface is MacOSHidInterface or LinuxHidInterface;

    internal Task<IConnection> OpenRegisteredConnectionAsync(IDisposable registration, CancellationToken token) =>
        OpenBuiltInConnectionAsync(registration, token);

    // Do not short-circuit cancellation here: after transfer the native owner alone releases the claim,
    // including a pre-canceled open. Raw callers checked cancellation before reaching this route.
    private async Task<IConnection> OpenBuiltInConnectionAsync(IDisposable? registration, CancellationToken token) =>
        (_hidInterface, ConnectionType) switch
        {
            (MacOSHidInterface device, ConnectionType.HidFido) =>
                await MacOSFidoHidConnection.OpenAsync(device.EntryId, _inputBridge ?? new NativeHidInputBridge(), token, registration).ConfigureAwait(false),
            (MacOSHidInterface device, ConnectionType.HidOtp) =>
                await MacOSOtpHidConnection.OpenAsync(device.EntryId, _otpLifetime ?? IOKitDeviceLifetime.Instance, token, registration).ConfigureAwait(false),
            (LinuxHidInterface device, ConnectionType.HidFido) =>
                await LinuxFidoHidConnection.OpenAsync(device.ReaderName, _linuxReportApi ?? LinuxHidReportApi.Instance,
                    _linuxReadReactor ?? LinuxHidReadReactor.Shared, token, registration).ConfigureAwait(false),
            (LinuxHidInterface device, ConnectionType.HidOtp) =>
                await LinuxOtpHidConnection.OpenAsync(device.ReaderName, _linuxReportApi ?? LinuxHidReportApi.Instance,
                    token, registration).ConfigureAwait(false),
            _ => throw new InvalidOperationException("Only built-in macOS and Linux HID own their claims before native open.")
        };

    Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
        ConnectionType connection,
        CancellationToken cancellationToken) =>
        OpenRawConnectionAsync(connection, cancellationToken);
}