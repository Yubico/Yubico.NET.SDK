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

using Yubico.YubiKit.Core.Core.Connections;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Management;

public static class IYubiKeyExtensions
{
    #region Nested type: <extension>

    extension(IYubiKey yubiKey)
    {
        public async Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
        {
            using var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
            using var mgmtSession = await yubiKey.CreateManagementSessionAsync(cancellationToken);

            return await mgmtSession.GetDeviceInfoAsync(cancellationToken);
        }

        public async Task SetDeviceConfigAsync(
            DeviceConfig config,
            bool reboot,
            byte[]? currentLockCode = null,
            byte[]? newLockCode = null,
            CancellationToken cancellationToken = default)
        {
            using var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
            using var mgmtSession = await yubiKey.CreateManagementSessionAsync(cancellationToken);

            await mgmtSession.SetDeviceConfigAsync(config, reboot, currentLockCode, newLockCode, cancellationToken);
        }


        public async Task<ManagementSession<ISmartCardConnection>> CreateManagementSessionAsync(
            CancellationToken cancellationToken = default)
        {
            var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
            return await ManagementSession<ISmartCardConnection>.CreateAsync(connection,
                cancellationToken: cancellationToken);
        }
    }

    #endregion
}