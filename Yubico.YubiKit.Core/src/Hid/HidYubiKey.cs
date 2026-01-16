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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Hid.Otp;
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Represents a YubiKey device accessed via HID interface.
/// </summary>
/// <remarks>
/// HID connections are inherently synchronous (OS-level ioctl calls), so connection
/// creation completes synchronously. The async API is maintained for interface consistency.
/// </remarks>
internal class HidYubiKey(
    IHidDevice hidDevice,
    ILogger<HidYubiKey> logger)
    : IYubiKey
{
    public string DeviceId { get; } =
        $"hid:{hidDevice.DescriptorInfo.VendorId:X4}:{hidDevice.DescriptorInfo.ProductId:X4}:{hidDevice.DescriptorInfo.Usage:X4}";

    /// <summary>
    /// The connection type this YubiKey interface supports.
    /// </summary>
    public ConnectionType ConnectionType => ConnectionTypeMapper.ToConnectionType(hidDevice.InterfaceType);

    public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (typeof(TConnection) == typeof(IFidoHidConnection))
        {
            var connection = CreateFidoConnection();
            return Task.FromResult(connection as TConnection ??
                   throw new InvalidOperationException("Connection is not of the expected type."));
        }

        if (typeof(TConnection) == typeof(IOtpHidConnection))
        {
            var connection = CreateOtpConnection();
            return Task.FromResult(connection as TConnection ??
                   throw new InvalidOperationException("Connection is not of the expected type."));
        }

        throw new NotSupportedException(
            $"Connection type {typeof(TConnection).Name} is not supported by this YubiKey device.");
    }

    private IFidoHidConnection CreateFidoConnection()
    {
        if (hidDevice.InterfaceType != HidInterfaceType.Fido)
        {
            throw new NotSupportedException(
                $"FIDO connection requires FIDO HID interface (UsagePage=0xF1D0, Usage=0x01), " +
                $"found {hidDevice.InterfaceType} (UsagePage=0x{hidDevice.DescriptorInfo.UsagePage:X4}, Usage=0x{hidDevice.DescriptorInfo.Usage:X4})");
        }

        logger.LogInformation(
            "Connecting to FIDO HID interface VID={VendorId:X4} PID={ProductId:X4}",
            hidDevice.DescriptorInfo.VendorId,
            hidDevice.DescriptorInfo.ProductId);

        var syncConnection = hidDevice.ConnectToIOReports();
        return new FidoHidConnection(syncConnection);
    }

    private IOtpHidConnection CreateOtpConnection()
    {
        if (hidDevice.InterfaceType != HidInterfaceType.Otp)
        {
            throw new NotSupportedException(
                $"OTP connection requires OTP/Keyboard HID interface (UsagePage=0x0001, Usage=0x06), " +
                $"found {hidDevice.InterfaceType} (UsagePage=0x{hidDevice.DescriptorInfo.UsagePage:X4}, Usage=0x{hidDevice.DescriptorInfo.Usage:X4})");
        }

        logger.LogInformation(
            "Connecting to OTP/Keyboard HID interface VID={VendorId:X4} PID={ProductId:X4}",
            hidDevice.DescriptorInfo.VendorId,
            hidDevice.DescriptorInfo.ProductId);

        var syncConnection = hidDevice.ConnectToFeatureReports();
        return new OtpHidConnection(syncConnection);
    }

}