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

using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.Interfaces;

/// <summary>
/// Represents a physical YubiKey device aggregating all available transport connections.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="IYubiKeyReference"/> which represents a single transport endpoint,
/// <see cref="IYubiKey"/> represents the physical device and provides access to all
/// available transports (SmartCard, HID FIDO, HID OTP) through a unified interface.
/// </para>
/// <para>
/// The composite device is created by correlating multiple transport references that
/// belong to the same physical device using serial number or configuration fingerprint.
/// </para>
/// <para>
/// <b>Key concepts:</b>
/// <list type="bullet">
/// <item><b>DeviceId:</b> Unique identifier - serial number when available, or "fp:{fingerprint}"</item>
/// <item><b>Identity:</b> Device information including version, capabilities, form factor</item>
/// <item><b>AvailableConnections:</b> All transport types currently accessible</item>
/// </list>
/// </para>
/// </remarks>
public interface IYubiKey
{
    /// <summary>
    /// Gets the unique device identifier.
    /// </summary>
    /// <remarks>
    /// Format is either the serial number as string (e.g., "12345678") when available,
    /// or "fp:{8-char-hex}" fingerprint when serial is unavailable.
    /// </remarks>
    string DeviceId { get; }

    /// <summary>
    /// Gets the device identity information.
    /// </summary>
    IDeviceIdentity Identity { get; }

    /// <summary>
    /// Gets the connection types currently available for this device.
    /// </summary>
    /// <remarks>
    /// This reflects the transports that were discovered during device enumeration.
    /// Available connections may change if the device is physically reconnected.
    /// </remarks>
    IReadOnlyList<ConnectionType> AvailableConnections { get; }

    /// <summary>
    /// Determines whether the specified connection type is supported by this device.
    /// </summary>
    /// <typeparam name="TConnection">The connection interface type to check.</typeparam>
    /// <returns><c>true</c> if the connection type is available; otherwise, <c>false</c>.</returns>
    bool SupportsConnection<TConnection>() where TConnection : class, IConnection;

    /// <summary>
    /// Determines whether the specified connection type is supported by this device.
    /// </summary>
    /// <param name="connectionType">The connection type to check.</param>
    /// <returns><c>true</c> if the connection type is available; otherwise, <c>false</c>.</returns>
    bool SupportsConnection(ConnectionType connectionType);

    /// <summary>
    /// Opens a connection of the specified type to the device.
    /// </summary>
    /// <typeparam name="TConnection">The type of connection to open.</typeparam>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the connection.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the requested connection type is not available.
    /// </exception>
    Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection;
}
