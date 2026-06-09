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
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.Interfaces;

/// <summary>
///     Represents a physical YubiKey and the set of connections (interfaces) it exposes.
/// </summary>
public interface IYubiKey
{
    string DeviceId { get; }

    /// <summary>
    ///     The set of concrete connections this physical device exposes (any combination of
    ///     <see cref="ConnectionType.SmartCard"/>, <see cref="ConnectionType.HidFido"/>, and
    ///     <see cref="ConnectionType.HidOtp"/>). Never contains the <see cref="ConnectionType.Hid"/>
    ///     group flag or <see cref="ConnectionType.All"/>.
    /// </summary>
    ConnectionType AvailableConnections { get; }

    /// <summary>
    ///     Whether this device can open the requested connection. Only concrete openable types are valid
    ///     (<see cref="ConnectionType.SmartCard"/>, <see cref="ConnectionType.HidFido"/>,
    ///     <see cref="ConnectionType.HidOtp"/>); <see cref="ConnectionType.Hid"/> means a HID interface is present;
    ///     <see cref="ConnectionType.Unknown"/>, <see cref="ConnectionType.All"/>, and other combinations return <c>false</c>.
    /// </summary>
    bool SupportsConnection(ConnectionType connectionType) =>
        AvailableConnections.SupportsConnection(connectionType);

    Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection;

    /// <summary>
    ///     Opens the device's connection when it exposes exactly one. For a physical device that exposes
    ///     several connections this is ambiguous and throws; callers must use <see cref="ConnectAsync{TConnection}"/>
    ///     or an application-specific extension that selects a transport intentionally.
    /// </summary>
    async Task<IConnection> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var single = AvailableConnections.SingleConcreteConnectionOrUnknown();
        return single switch
        {
            ConnectionType.SmartCard => await ConnectAsync<ISmartCardConnection>(cancellationToken)
                .ConfigureAwait(false),
            ConnectionType.HidFido => await ConnectAsync<IFidoHidConnection>(cancellationToken)
                .ConfigureAwait(false),
            ConnectionType.HidOtp => await ConnectAsync<IOtpHidConnection>(cancellationToken)
                .ConfigureAwait(false),
            _ when (AvailableConnections & ConnectionTypeExtensions.ConcreteConnections) == ConnectionType.Unknown =>
                throw new NotSupportedException(
                    "This YubiKey exposes no openable connection."),
            _ => throw new InvalidOperationException(
                $"This YubiKey exposes multiple connections ({AvailableConnections}); the default connect is ambiguous. " +
                "Use ConnectAsync<TConnection>() or an application-specific session extension to choose a transport.")
        };
    }
}