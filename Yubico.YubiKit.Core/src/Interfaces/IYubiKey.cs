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

using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.Interfaces;

public interface IYubiKey
{
    string DeviceId { get; }
    ConnectionType ConnectionType { get; }

    Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection;
    async Task<IConnection> ConnectAsync(CancellationToken cancellationToken = default)
    =>
        ConnectionType switch
        {
            ConnectionType.SmartCard => await ConnectAsync<ISmartCardConnection>(cancellationToken)
                .ConfigureAwait(false),
            ConnectionType.HidFido => await ConnectAsync<IFidoHidConnection>(cancellationToken)
                .ConfigureAwait(false),
            ConnectionType.HidOtp => await ConnectAsync<IOtpHidConnection>(cancellationToken)
                .ConfigureAwait(false),
            _ => throw new NotSupportedException(
                $"Connection type {ConnectionType} is not supported for management session creation."),
        };
}