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
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
/// Represents a physical YubiKey device aggregating all available transport references.
/// </summary>
/// <remarks>
/// <para>
/// The composite device is created by correlating multiple <see cref="IYubiKeyReference"/>
/// instances that belong to the same physical device using serial number or configuration
/// fingerprint matching.
/// </para>
/// <para>
/// Connection requests are routed to the appropriate underlying transport reference
/// based on the requested connection type.
/// </para>
/// </remarks>
internal sealed class CompositeYubiKey : IYubiKey
{
    private readonly IDeviceIdentity _identity;
    private readonly Dictionary<ConnectionType, IYubiKeyReference> _references;
    private readonly string _deviceId;

    /// <summary>
    /// Initializes a new instance of <see cref="CompositeYubiKey"/>.
    /// </summary>
    /// <param name="identity">The device identity information.</param>
    /// <param name="references">The transport references that belong to this device.</param>
    public CompositeYubiKey(IDeviceIdentity identity, IReadOnlyList<IYubiKeyReference> references)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(references);

        if (references.Count == 0)
        {
            throw new ArgumentException("At least one reference must be provided.", nameof(references));
        }

        _identity = identity;
        _references = references.ToDictionary(r => r.ConnectionType, r => r);
        _deviceId = ComputeDeviceId(identity);
    }

    /// <inheritdoc />
    public string DeviceId => _deviceId;

    /// <inheritdoc />
    public IDeviceIdentity Identity => _identity;

    /// <inheritdoc />
    public IReadOnlyList<ConnectionType> AvailableConnections =>
        _references.Keys.ToList();

    /// <inheritdoc />
    public bool SupportsConnection<TConnection>() where TConnection : class, IConnection
    {
        var connectionType = GetConnectionTypeFor<TConnection>();
        return connectionType.HasValue && _references.ContainsKey(connectionType.Value);
    }

    /// <inheritdoc />
    public bool SupportsConnection(ConnectionType connectionType) =>
        _references.ContainsKey(connectionType);

    /// <inheritdoc />
    public async Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection
    {
        var connectionType = GetConnectionTypeFor<TConnection>();

        if (!connectionType.HasValue || !_references.TryGetValue(connectionType.Value, out var reference))
        {
            throw new NotSupportedException(
                $"Connection type {typeof(TConnection).Name} is not available for this device.");
        }

        return await reference.ConnectAsync<TConnection>(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Computes the device ID from identity information.
    /// </summary>
    private static string ComputeDeviceId(IDeviceIdentity identity)
    {
        return identity.SerialNumber.HasValue
            ? identity.SerialNumber.Value.ToString()
            : $"fp:{identity.ComputeConfigFingerprint()}";
    }

    /// <summary>
    /// Maps a connection interface type to its <see cref="ConnectionType"/>.
    /// </summary>
    private static ConnectionType? GetConnectionTypeFor<TConnection>() where TConnection : class, IConnection
    {
        return typeof(TConnection) switch
        {
            var t when t == typeof(ISmartCardConnection) => ConnectionType.SmartCard,
            var t when t == typeof(IFidoHidConnection) => ConnectionType.HidFido,
            var t when t == typeof(IOtpHidConnection) => ConnectionType.HidOtp,
            _ => null
        };
    }
}
