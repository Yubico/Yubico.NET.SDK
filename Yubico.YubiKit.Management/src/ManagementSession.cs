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
using System.Text;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Management;

public sealed class ManagementSession<TConnection>(
    TConnection connection,
    IProtocolFactory<TConnection> protocolFactory,
    ILogger<ManagementSession<TConnection>> logger)
    : ApplicationSession
    where TConnection : IConnection
{
    private const byte INS_GET_DEVICE_INFO = 0x1D;
    private const byte INS_DEVICE_RESET = 0x1F;
    private const byte INS_SET_DEVICE_CONFIG = 0x1C;

    private const int TagMoreDeviceInfo = 0x10;
    private readonly IProtocol _protocol = protocolFactory.Create(connection);

    private static readonly Feature FeatureDeviceInfo =
        new("Device Info", 4, 1, 0);

    private static readonly Feature FeatureSetConfig =
        new("Set Config", 5, 0, 0);

    private static readonly Feature FeatureDeviceReset =
        new("Device Reset", 5, 6, 0);

    private bool _isInitialized;
    private FirmwareVersion? _version;

    public static async Task<ManagementSession<TConnection>> CreateAsync(
        TConnection connection,
        ILogger<ManagementSession<TConnection>>? logger = null,
        CancellationToken cancellationToken = default)
    {
        logger ??= NullLogger<ManagementSession<TConnection>>.Instance;
        var protocolFactory = PcscProtocolFactory<TConnection>.Create();
        var session = new ManagementSession<TConnection>(connection, protocolFactory, logger);

        await session.InitializeAsync(cancellationToken).ConfigureAwait(false);
        return session;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        _version = await SetVersionAsync(cancellationToken).ConfigureAwait(false);
        _protocol.Configure(_version);
        _isInitialized = true;
    }

    public async Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        EnsureSupports(FeatureDeviceInfo);

        byte page = 0;
        var allPagesTlvs = new List<Tlv>();

        var hasMoreData = true;
        while (hasMoreData)
        {
            var apdu = new CommandApdu { Cla = 0, Ins = INS_GET_DEVICE_INFO, P1 = page, P2 = 0 };

            var encodedResult = await TransmitAsync(apdu, cancellationToken).ConfigureAwait(false);
            if (encodedResult.Length - 1 != encodedResult.Span[0])
                throw new BadResponseException("Invalid length");

            var pageTlvs = TlvHelper.Decode(encodedResult.Span[1..]);
            var moreData = pageTlvs.SingleOrDefault(t => t.Tag == TagMoreDeviceInfo);
            hasMoreData = moreData?.Length == 1 && moreData.GetValueSpan()[0] == 1;

            // Transfer ownership of Tlv objects to allPagesTlvs
            allPagesTlvs.AddRange(pageTlvs);

            ++page;
        }

        using var allTlvs = new DisposableTlvCollection(allPagesTlvs);
        return DeviceInfo.CreateFromTlvs([.. allTlvs], _version);
    }

    public async Task SetDeviceConfigAsync(
        DeviceConfig config,
        bool reboot,
        byte[]? currentLockCode = null,
        byte[]? newLockCode = null,
        CancellationToken cancellationToken = default)
    {
        EnsureSupports(FeatureSetConfig);
        ArgumentNullException.ThrowIfNull(config);

        const int LockCodeLength = 16;
        if (currentLockCode is { Length: not LockCodeLength })
            throw new ArgumentException("Current lock code must be 16 bytes", nameof(currentLockCode));

        if (newLockCode is { Length: not LockCodeLength })
            throw new ArgumentException("New lock code must be 16 bytes", nameof(newLockCode));

        var configBytes = config.GetBytes(reboot, currentLockCode, newLockCode);
        var apdu = new CommandApdu { Cla = 0, Ins = INS_SET_DEVICE_CONFIG, P1 = 0, P2 = 0, Data = configBytes };

        await TransmitAsync(apdu, cancellationToken).ConfigureAwait(false);
    }

    public async Task ResetDeviceAsync(CancellationToken cancellationToken = default)
    {
        EnsureSupports(FeatureDeviceReset);

        await TransmitAsync(new CommandApdu { Cla = 0, Ins = INS_DEVICE_RESET, P1 = 0, P2 = 0 }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<FirmwareVersion> SetVersionAsync(CancellationToken cancellationToken)
    {
        var versionBytes = await SelectAsync(cancellationToken).ConfigureAwait(false);

        var deviceText = Encoding.UTF8.GetString(versionBytes.Span);
        var versionString = deviceText.Split(' ').Last();
        var versionParts = versionString.Split('.').Select(int.Parse).ToArray();

        return versionParts.Length == 3
           ? new FirmwareVersion(versionParts[0], versionParts[1], versionParts[2])
           : new FirmwareVersion();
    }

    private async Task<ReadOnlyMemory<byte>> SelectAsync(CancellationToken cancellationToken)
    {
        if (_protocol is ISmartCardProtocol smartCardProtocol)
        {
            var response = await smartCardProtocol
                .SelectAsync(ApplicationIds.Management, cancellationToken)
                .ConfigureAwait(false);

            return response;
        }
        else
        {
            throw new NotSupportedException("Protocol not supported");
        }
    }

    private async Task<ReadOnlyMemory<byte>> TransmitAsync(CommandApdu command, CancellationToken cancellationToken)
    {
        if (_protocol is ISmartCardProtocol smartCardProtocol)
        {
            var response = await smartCardProtocol
                .TransmitAndReceiveAsync(command, cancellationToken)
                .ConfigureAwait(false);

            return response;
        }
        else
        {
            throw new NotSupportedException("Protocol not supported");
        }
    }

    internal void EnsureSupports(Feature feature)
    {
        if (!IsSupported(feature)) throw new NotSupportedException($"{feature.Name} is not supported on this YubiKey.");
    }

    internal bool IsSupported(Feature feature)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Session not initialized. Call InitializeAsync first.");

        if (_version is null)
            return false;

        return _version >= feature.Version;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _protocol.Dispose();

        base.Dispose(disposing);
    }
}