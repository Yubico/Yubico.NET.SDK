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
using System.Text;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Iso7816;
using Yubico.YubiKit.Core.Protocols;
using Version = Yubico.YubiKit.Core.Version;

namespace Yubico.YubiKit;

public class ManagementSession<TConnection> : ApplicationSession
    where TConnection : IConnection
{
    private const byte INS_GET_DEVICE_INFO = 0x1D;
    private const int TagMoreDeviceInfo = 0x10;
    private Version? _version;

    private static readonly Feature FeatureDeviceInfo =
        new() { Name = "Device Info", VersionMajor = 4, VersionMinor = 1, VersionRevision = 0 };

    private readonly ILogger<ManagementSession<TConnection>> _logger;
    private readonly IProtocol _protocol;

    public ManagementSession(
        ILogger<ManagementSession<TConnection>> logger,
        TConnection connection,
        IProtocolFactory<TConnection> protocolFactory)
    {
        _logger = logger;
        _protocol = protocolFactory.Create(connection);
    }

    private async Task<Version> GetVersionAsync()
    {
        if (_version is not null)
            return _version;

        if (_protocol is ISmartCardProtocol smartCardProtocol)
        {
            var versionBytes = await smartCardProtocol.SelectAsync(ApplicationIds.Management).ConfigureAwait(false);
            var deviceText = Encoding.UTF8.GetString(versionBytes.Span);

            var versionString = deviceText.Split(' ').Last();
            var versionParts = versionString.Split('.').Select(s => int.Parse(s)).ToArray();
            if (versionParts.Length == 3)
            {
                _version = new Version(versionParts[0], versionParts[1], versionParts[2]);
            }
            else
            {
                _version = new Version(0, 0, 0);
            }

            _logger.LogInformation("YubiKey device text: {DeviceText}", deviceText);
            _logger.LogInformation("YubiKey version: {Version}", _version);
            _logger.LogInformation("YubiKey version string: {VersionString}", versionString);
        }
        else
        {
            throw new NotSupportedException("Protocol not supported");
        }
        
        return _version;
    }


    public async Task<DeviceInfo> GetDeviceInfoAsync()
    {
        EnsureSupports(FeatureDeviceInfo); // todo

        byte page = 0;
        IEnumerable<Tlv> allPagesTlvs = [];

        var hasMoreData = true;
        while (hasMoreData)
        {
            var apdu = new CommandApdu
            {
                Cla = 0,
                Ins = INS_GET_DEVICE_INFO,
                P1 = page,
                P2 = 0,
                Data = null
            };

            if (_protocol is ISmartCardProtocol smartCardProtocol)
            {
                var encodedResult =
                    await smartCardProtocol
                        .TransmitAndReceiveAsync(apdu);
                if (encodedResult.Length - 1 != encodedResult.Span[0])
                    throw new BadResponseException("Invalid length");

                var pageTlvs = TlvHelper.Decode(encodedResult.Span[1..]).ToList();

                var moreData = pageTlvs.SingleOrDefault(t => t.Tag == TagMoreDeviceInfo);
                hasMoreData = moreData?.Length == 1 && moreData.GetValueSpan()[0] == 1;
                allPagesTlvs = allPagesTlvs.Concat(pageTlvs);
                ++page;
            }
            else
            {
                throw new NotSupportedException("Protocol not supported");
            }
        }

        return DeviceInfo.CreateFromTlvs([.. allPagesTlvs], _version);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _protocol.Dispose();

        base.Dispose(disposing);
    }
}