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
using Microsoft.VisualBasic;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Iso7816;

namespace Yubico.YubiKit;

public class ManagementSession : ApplicationSession
{
    private const byte INS_READ_CONFIG = 0x1d;

    private readonly ILogger<ManagementSession> _logger;
    private readonly ISmartCardConnection _smartCardConnection;

    public ManagementSession(
        ILogger<ManagementSession> logger,
        ISmartCardConnection smartCardConnection)
    {
        _logger = logger;
        _smartCardConnection = smartCardConnection;
    }

    public DeviceInfo GetDeviceInfo() => new();

    public async Task<DeviceInfo> GetDeviceInfoAsync()
    {
        EnsureSupports(FeatureDeviceInfo);
        byte page = 0;
        var hasMoreData = true;
        var tlvs = new Dictionary<int, Memory<byte>>();
        
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
            
            var result = await _smartCardConnection.TransmitAndReceiveAsync(apdu);
            
            // decode tlv
            // get hasMoreData 0x10 tag
            // store tlv in tlvs, 
        }

            // create deviceInfo with vls

        return DeviceInfo.CreateFromData(tlvs);
    }

    private void EnsureSupports(Feature feature)
    {
        if (!IsSupported(feature))
        {
            throw new NotSupportedException($"{feature.Name} is not supported on this YubiKey.");
        }
    }

    private bool IsSupported(Feature feature)
    {
        return true; // TODO get from Management Session, select, and parse version info
    }

    private static readonly Feature FeatureDeviceInfo = new(){ Name = "Device Info", VersionMajor = 4, VersionMinor = 1, VersionRevision = 0 };
}

internal class Feature
{
    public required string Name { get; init; }
    public int VersionMajor { get; set; }
    public int VersionMinor { get; set; }
    public int VersionRevision { get; set; }
}

// Protocol