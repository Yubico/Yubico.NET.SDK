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
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Transports.Hid;

namespace Yubico.YubiKit.Core.Devices;

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
    : IYubiKey, IDiscoveryConnectionProvider
{
    public string DeviceId { get; } =
        $"hid:{hidDevice.ReaderName}:{hidDevice.DescriptorInfo.Usage:X4}";

    /// <summary>
    /// The connections this YubiKey HID interface exposes (a single concrete HID connection in this phase).
    /// </summary>
    public ConnectionType AvailableConnections => ConnectionTypeMapper.ToConnectionType(hidDevice.InterfaceType);

    public async Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (typeof(TConnection) != typeof(IFidoHidConnection)
            && typeof(TConnection) != typeof(IOtpHidConnection))
        {
            throw new NotSupportedException(
                $"Connection type {typeof(TConnection).Name} is not supported by this YubiKey device.");
        }

        // Shared: HID has no applet-selection state, so concurrent connections to one HID interface are
        // safe and are the route Management takes while CCID is held.
        var ownership = await DeviceConnectionRegistry
            .AcquireConnectionAsync(DeviceId, exclusive: false, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            if (typeof(TConnection) == typeof(IFidoHidConnection))
            {
                var registered = new RegisteredFidoHidConnection(CreateFidoConnection(), ownership);
                return registered as TConnection ??
                       throw new InvalidOperationException("Connection is not of the expected type.");
            }

            if (typeof(TConnection) == typeof(IOtpHidConnection))
            {
                var registered = new RegisteredOtpHidConnection(CreateOtpConnection(), ownership);
                return registered as TConnection ??
                       throw new InvalidOperationException("Connection is not of the expected type.");
            }
            throw new InvalidOperationException("Connection type validation did not select a HID implementation.");
        }
        catch
        {
            ownership.Dispose();
            throw;
        }
    }

    Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
        ConnectionType connection,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IConnection result = connection switch
        {
            ConnectionType.HidFido => CreateFidoConnection(),
            ConnectionType.HidOtp => CreateOtpConnection(),
            _ => throw new NotSupportedException(
                $"Connection type {connection} is not supported by this YubiKey device.")
        };

        return Task.FromResult(result);
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