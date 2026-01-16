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
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Hid.Otp;
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.Hid;

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

    public async Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection
    {
        if (typeof(TConnection) == typeof(IFidoHidConnection))
        {
            var connection = await CreateFidoConnection(cancellationToken).ConfigureAwait(false);
            return connection as TConnection ??
                   throw new InvalidOperationException("Connection is not of the expected type.");
        }

        if (typeof(TConnection) == typeof(IOtpHidConnection))
        {
            var connection = await CreateOtpConnection(cancellationToken).ConfigureAwait(false);
            return connection as TConnection ??
                   throw new InvalidOperationException("Connection is not of the expected type.");
        }

        // Legacy support for generic IHidConnection
        if (typeof(TConnection) == typeof(IHidConnection))
        {
            var connection = await CreateLegacyHidConnection(cancellationToken).ConfigureAwait(false);
            return connection as TConnection ??
                   throw new InvalidOperationException("Connection is not of the expected type.");
        }

        throw new NotSupportedException(
            $"Connection type {typeof(TConnection).Name} is not supported by this YubiKey device.");
    }

    private async Task<IFidoHidConnection> CreateFidoConnection(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make async

        if (hidDevice.InterfaceType != YubiKeyHidInterfaceType.Fido)
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

    private async Task<IOtpHidConnection> CreateOtpConnection(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make async

        if (hidDevice.InterfaceType != YubiKeyHidInterfaceType.Otp)
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

    private async Task<IHidConnection> CreateLegacyHidConnection(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make async
    
        logger.LogInformation(
            "Connecting to HID YubiKey VID={VendorId:X4} PID={ProductId:X4} Usage={Usage:X4} InterfaceType={InterfaceType}",
            hidDevice.DescriptorInfo.VendorId,
            hidDevice.DescriptorInfo.ProductId,
            hidDevice.DescriptorInfo.Usage,
            hidDevice.InterfaceType);
    
        var reportType = HidInterfaceClassifier.GetReportType(hidDevice.InterfaceType);
        
        var syncConnection = reportType switch
        {
            HidReportType.InputOutput => hidDevice.ConnectToIOReports(),
            HidReportType.Feature => hidDevice.ConnectToFeatureReports(),
            _ => throw new NotSupportedException($"HID interface type {hidDevice.InterfaceType} is not supported.")
        };
    
        return new HidConnection(syncConnection);
    }
}