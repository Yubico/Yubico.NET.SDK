// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

namespace Yubico.YubiKit.Core.Sessions;

/// <summary>
///     Configures cross-cutting behavior when an application session is created.
/// </summary>
/// <remarks>
///     Factories pass the settings to initialization. An initializer can retain this options object while
///     creation is pending; returned sessions retain the selected values and references rather than the carrier.
///     Referenced keys, certificates, and services are shared objects, not deep copies. Sessions that use
///     <see cref="UserPresencePrompt" /> retain it without taking ownership; Management and Security Domain
///     ignore it. The caller owns the prompt and <see cref="ScpKeyParameters" />.
/// </remarks>
public sealed class SessionCreationOptions
{
    /// <summary>Gets optional protocol configuration overrides.</summary>
    public ProtocolConfiguration? ProtocolConfiguration { get; init; }

    /// <summary>
    ///     Gets optional Secure Channel Protocol key parameters. The parameters are borrowed for initialization;
    ///     the caller retains ownership and disposal responsibility. Security Domain also retains them for
    ///     reset and reinitialization. Keep the parameters alive until that session has been disposed.
    /// </summary>
    public ScpKeyParameters? ScpKeyParameters { get; init; }

    /// <summary>
    ///     Gets the connection type selected from a device factory or required by a direct connection factory.
    /// </summary>
    public ConnectionType? PreferredConnectionType { get; init; }

    /// <summary>
    ///     Gets an explicit override for the effective firmware version used to configure the protocol and
    ///     evaluate feature support after required applet initialization exchanges complete. Security Domain
    ///     is the exception: it cannot detect firmware, so this value is its only exact version source and
    ///     directly controls feature gates; without it, that session conservatively assumes version 5.3.0.
    /// </summary>
    public FirmwareVersion? FirmwareVersionOverride { get; init; }

    /// <summary>
    ///     Gets the optional service notified while session operations require or may require physical user
    ///     presence.
    /// </summary>
    /// <remarks>
    ///     Sessions that use this service retain it for their lifetime without disposing it. Management and
    ///     Security Domain ignore it. Keep callbacks non-reentrant: disposing the session from its own callback,
    ///     synchronously or asynchronously, can wait on the exchange that is awaiting that callback.
    /// </remarks>
    public IUserPresencePrompt? UserPresencePrompt { get; init; }

    /// <summary>
    ///     Creates a copy with the specified preferred connection type.
    /// </summary>
    /// <param name="connectionType">The preferred connection type for the copy.</param>
    /// <returns>A new instance containing the copied settings and specified preferred connection type.</returns>
    /// <remarks>
    ///     This method is the single place that must be updated when a property is added to this type.
    /// </remarks>
    public SessionCreationOptions WithPreferredConnectionType(ConnectionType connectionType) =>
        new()
        {
            ProtocolConfiguration = ProtocolConfiguration,
            ScpKeyParameters = ScpKeyParameters,
            PreferredConnectionType = connectionType,
            FirmwareVersionOverride = FirmwareVersionOverride,
            UserPresencePrompt = UserPresencePrompt
        };
}