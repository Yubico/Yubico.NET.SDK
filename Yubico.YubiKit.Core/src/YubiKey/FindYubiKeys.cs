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

using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

public interface IFindYubiKeys
{
    Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type, CancellationToken cancellationToken = default);
}

public class FindYubiKeys(
    IFindPcscDevices findPcscService,
    IFindHidDevices findHidService,
    IYubiKeyFactory yubiKeyFactory) : IFindYubiKeys
{
    #region IFindYubiKeys Members

    public async Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType type = ConnectionType.All,
        CancellationToken cancellationToken = default)
    {
        var yubiKeys = new List<IYubiKey>();

        if (type.HasFlag(ConnectionType.Smartcard))
        {
            var ccidKeys = await FindAllCcid(cancellationToken);
            yubiKeys.AddRange(ccidKeys);
        }

        if (type.HasFlag(ConnectionType.Hid))
        {
            var hidKeys = await FindAllHid(cancellationToken);
            yubiKeys.AddRange(hidKeys);
        }

        return yubiKeys;
    }

    #endregion

    private async Task<IReadOnlyList<IYubiKey>> FindAllHid(CancellationToken cancellationToken = default)
    {
        var hidDevices = await findHidService.FindAllAsync(cancellationToken);
        return hidDevices.Select(yubiKeyFactory.Create).ToList();
    }

    private async Task<IReadOnlyList<IYubiKey>> FindAllCcid(CancellationToken cancellationToken = default)
    {
        var pcscDevices = await findPcscService.FindAllAsync(cancellationToken);
        return pcscDevices.Select(yubiKeyFactory.Create).ToList();
    }

    public static FindYubiKeys Create() =>
        new(FindPcscDevices.Create(), FindHidDevices.Create(), YubiKeyFactory.Create());
}