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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Reads <see cref="DeviceInfo"/> over an already-open connection by building the matching Core protocol.
/// </summary>
/// <remarks>
///     Takes ownership of the supplied connection: it builds a protocol over the connection and disposes the
///     protocol (which disposes the connection) before returning. The caller must not dispose the connection
///     separately. Shared by discovery's serial-disambiguation read and the composite metadata read.
/// </remarks>
internal static class ProtocolDeviceInfo
{
    /// <summary>
    ///     Opens a short-lived connection over the given interface and reads <see cref="DeviceInfo" />,
    ///     bounded by a hard wall-clock budget.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The budget bounds the caller's <em>wait</em>, not the work: an in-flight native call (e.g.
    ///         <c>SCardTransmit</c> against a card busy with a long applet operation such as RSA key
    ///         generation) cannot observe cancellation. On budget exhaustion the read is therefore
    ///         <em>abandoned, not aborted</em> — this method throws <see cref="TimeoutException" /> so the
    ///         scan can proceed, while the abandoned task keeps running in the background and disposes its
    ///         protocol/connection through the normal <see cref="ReadAsync" /> control flow when the native
    ///         call eventually returns.
    ///     </para>
    ///     <para>
    ///         External cancellation via <paramref name="cancellationToken" /> likewise abandons the
    ///         in-flight read (propagating <see cref="OperationCanceledException" />).
    ///     </para>
    /// </remarks>
    /// <exception cref="TimeoutException">The budget elapsed before the read completed.</exception>
    public static async Task<DeviceInfo> ReadBoundedAsync(
        IYubiKey device,
        ConnectionType connection,
        TimeSpan budget,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var readTask = ConnectAndReadAsync(device, connection, cancellationToken);
        try
        {
            return await readTask.WaitAsync(budget, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            ObserveAbandoned(readTask, device.DeviceId, connection, logger);
            throw;
        }
        catch (OperationCanceledException)
        {
            ObserveAbandoned(readTask, device.DeviceId, connection, logger);
            throw;
        }
    }

    private static async Task<DeviceInfo> ConnectAndReadAsync(
        IYubiKey device,
        ConnectionType connection,
        CancellationToken cancellationToken)
    {
        var conn = await ConnectAsync(device, connection, cancellationToken).ConfigureAwait(false);
        return await ReadAsync(conn, cancellationToken).ConfigureAwait(false);
    }

    private static Task<IConnection> ConnectAsync(
        IYubiKey device,
        ConnectionType connection,
        CancellationToken cancellationToken) => connection switch
        {
            ConnectionType.SmartCard => Upcast(device.ConnectAsync<ISmartCardConnection>(cancellationToken)),
            ConnectionType.HidFido => Upcast(device.ConnectAsync<IFidoHidConnection>(cancellationToken)),
            ConnectionType.HidOtp => Upcast(device.ConnectAsync<IOtpHidConnection>(cancellationToken)),
            _ => throw new NotSupportedException($"Cannot open connection {connection} for device info read.")
        };

    private static async Task<IConnection> Upcast<TConnection>(Task<TConnection> task)
        where TConnection : class, IConnection => await task.ConfigureAwait(false);

    // Attaches a fire-and-forget continuation so an abandoned read's eventual outcome is observed (no
    // unobserved task exceptions) and visible in debug logs. Safe to attach to already-completed tasks.
    private static void ObserveAbandoned(
        Task<DeviceInfo> task,
        string deviceId,
        ConnectionType connection,
        ILogger logger) =>
        _ = task.ContinueWith(
            t => logger.LogDebug(
                t.Exception?.GetBaseException(),
                "Abandoned discovery device-info read for {DeviceId} over {Connection} finished in the background (status: {Status}).",
                deviceId,
                connection,
                t.Status),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

    public static async Task<DeviceInfo> ReadAsync(IConnection connection, CancellationToken cancellationToken)
    {
        switch (connection)
        {
            case ISmartCardConnection smartCard:
                {
                    var protocol = PcscProtocolFactory<ISmartCardConnection>.Create().Create(smartCard);
                    try
                    {
                        await protocol.SelectAsync(ApplicationIds.Management, cancellationToken).ConfigureAwait(false);
                        return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        protocol.Dispose();
                    }
                }
            case IFidoHidConnection fido:
                {
                    var protocol = FidoProtocolFactory.Create().Create(fido);
                    try
                    {
                        // Initializes the HID channel; the application id is unused for HID.
                        await protocol.SelectAsync(ApplicationIds.Management, cancellationToken).ConfigureAwait(false);
                        return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        protocol.Dispose();
                    }
                }
            case IOtpHidConnection otp:
                {
                    var protocol = OtpProtocolFactory.Create().Create(otp);
                    try
                    {
                        return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        protocol.Dispose();
                    }
                }
            default:
                throw new NotSupportedException(
                    $"Connection type {connection.GetType().Name} is not supported for reading device info.");
        }
    }
}