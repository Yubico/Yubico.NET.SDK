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
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>One live enumerated interface candidate that can open its raw connection.</summary>
internal sealed class DeviceConnectionSlot : IYubiKeyConnectionSlot, IDiscoveryConnectionProvider
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<DeviceConnectionSlot>();

    private readonly IPcscDevice? _pcscDevice;
    private readonly IHidDevice? _hidDevice;
    private readonly ISmartCardConnectionFactory? _smartCardConnectionFactory;
    internal DeviceConnectionSlot(
        IPcscDevice pcscDevice,
        ISmartCardConnectionFactory smartCardConnectionFactory)
    {
        _pcscDevice = pcscDevice;
        _smartCardConnectionFactory = smartCardConnectionFactory;
        InterfaceId = $"pcsc:{pcscDevice.ReaderName}";
        ConnectionType = ConnectionType.SmartCard;
    }

    internal DeviceConnectionSlot(IHidDevice hidDevice)
    {
        _hidDevice = hidDevice;
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

    public ConnectionType ConnectionType { get; }

    public async Task<IConnection> OpenRawConnectionAsync(
        ConnectionType connection,
        CancellationToken cancellationToken = default)
    {
        if (connection != ConnectionType)
        {
            throw new NotSupportedException(
                $"Connection type {connection} is not supported by this device connection slot.");
        }

        if (_pcscDevice is { } pcscDevice && _smartCardConnectionFactory is { } connectionFactory)
        {
            var result = await connectionFactory.CreateAsync(pcscDevice, cancellationToken).ConfigureAwait(false);
            Logger.LogInformation("Connected to YubiKey in reader {ReaderName}", pcscDevice.ReaderName);
            return result;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return connection switch
        {
            ConnectionType.HidFido => CreateFidoConnection(),
            ConnectionType.HidOtp => CreateOtpConnection(),
            _ => throw new NotSupportedException(
                $"Connection type {connection} is not supported by this device connection slot.")
        };
    }

    Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
        ConnectionType connection,
        CancellationToken cancellationToken) =>
        OpenRawConnectionAsync(connection, cancellationToken);

    private IFidoHidConnection CreateFidoConnection()
    {
        var hidDevice = _hidDevice
            ?? throw new InvalidOperationException("A FIDO connection requires a HID device.");
        if (hidDevice.InterfaceType != HidInterfaceType.Fido)
        {
            throw new NotSupportedException(
                $"FIDO connection requires FIDO HID interface (UsagePage=0xF1D0, Usage=0x01), " +
                $"found {hidDevice.InterfaceType} (UsagePage=0x{hidDevice.DescriptorInfo.UsagePage:X4}, Usage=0x{hidDevice.DescriptorInfo.Usage:X4})");
        }

        Logger.LogInformation(
            "Connecting to FIDO HID interface VID={VendorId:X4} PID={ProductId:X4}",
            hidDevice.DescriptorInfo.VendorId,
            hidDevice.DescriptorInfo.ProductId);
        return new FidoHidConnection(hidDevice.ConnectToIOReports());
    }

    private IOtpHidConnection CreateOtpConnection()
    {
        var hidDevice = _hidDevice
            ?? throw new InvalidOperationException("An OTP connection requires a HID device.");
        if (hidDevice.InterfaceType != HidInterfaceType.Otp)
        {
            throw new NotSupportedException(
                $"OTP connection requires OTP/Keyboard HID interface (UsagePage=0x0001, Usage=0x06), " +
                $"found {hidDevice.InterfaceType} (UsagePage=0x{hidDevice.DescriptorInfo.UsagePage:X4}, Usage=0x{hidDevice.DescriptorInfo.Usage:X4})");
        }

        Logger.LogInformation(
            "Connecting to OTP/Keyboard HID interface VID={VendorId:X4} PID={ProductId:X4}",
            hidDevice.DescriptorInfo.VendorId,
            hidDevice.DescriptorInfo.ProductId);
        return new OtpHidConnection(hidDevice.ConnectToFeatureReports());
    }
}