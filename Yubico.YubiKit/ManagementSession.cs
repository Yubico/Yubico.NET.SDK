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
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Iso7816;
using Yubico.YubiKit.Core.Protocols;
using Version = Yubico.YubiKit.Core.Version;

namespace Yubico.YubiKit;

public class ManagementSession<TConnection> : ApplicationSession
    where TConnection : IConnection
{
    private const byte INS_READ_CONFIG = 0x1d;
    private const int TagMoreDeviceInfo = 0x10;

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

        if (_protocol is ISmartCardProtocol smartCardProtocol)
        {
            var versionBytes = smartCardProtocol.SelectAsync(ApplicationIds.Management).GetAwaiter().GetResult();
            // Version = Version.Parse(Encoding.UTF8.GetString(versionBytes.Span));
        }
    }

    private Version Version { get; set; }

    public DeviceInfo GetDeviceInfo() => new() // todo fake
    {
        IsSky = false,
        IsFips = false,
        FormFactor = FormFactor.Unknown,
        SerialNumber = 0,
        IsLocked = false,
        UsbEnabled = 0,
        UsbSupported = 0,
        NfcEnabled = 0,
        NfcSupported = 0,
        ResetBlocked = 0,
        FipsCapabilities = 0,
        FipsApproved = 0,
        HasPinComplexity = false,
        PartNumber = null,
        IsNfcRestricted = false,
        AutoEjectTimeout = 0,
        ChallengeResponseTimeout = default,
        DeviceFlags = DeviceFlags.None,
        FirmwareVersion = null,
        VersionQualifier = null
    };

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
                Ins = INS_READ_CONFIG,
                P1 = page,
                P2 = 0,
                Data = null
            };

            if (_protocol is ISmartCardProtocol smartCardProtocol)
            {
                var encodedResult =
                    await smartCardProtocol
                        .TransmitAndReceiveAsync(apdu); // TODO Getting weird non valid TLV value here?
                if (encodedResult.Length - 1 != encodedResult.Span[0])
                    throw new BadResponseException("Invalid length");

                var pageTlvs = TlvHelper.Decode(encodedResult.Span).ToList();

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

        // create deviceInfo with vls
        Version = Version.V5_8_0; // todo get from selectapplication
        return DeviceInfo.CreateFromTlvs(allPagesTlvs.ToList(), null);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _protocol.Dispose();

        base.Dispose(disposing);
    }
}