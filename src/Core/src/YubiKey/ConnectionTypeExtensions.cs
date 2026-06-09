// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.Core.YubiKey;

internal static class ConnectionTypeExtensions
{
    /// <summary>Concrete openable connection bits (excludes the <see cref="ConnectionType.Hid"/> group and <see cref="ConnectionType.All"/>).</summary>
    public const ConnectionType ConcreteConnections =
        ConnectionType.HidFido | ConnectionType.HidOtp | ConnectionType.SmartCard;

    private const ConnectionType HidGroup =
        ConnectionType.Hid | ConnectionType.HidFido | ConnectionType.HidOtp;

    /// <summary>True when the filter requests scanning any HID interface.</summary>
    public static bool IncludesHidScan(this ConnectionType filter) => (filter & HidGroup) != 0;

    /// <summary>
    ///     Whether a device whose available connections are <paramref name="available"/> can open the
    ///     <paramref name="requested"/> connection. Only concrete openable types are valid requests;
    ///     <see cref="ConnectionType.Hid"/> means "HidFido or HidOtp present"; <see cref="ConnectionType.Unknown"/>,
    ///     <see cref="ConnectionType.All"/>, and any other multi-bit combination return <c>false</c>.
    /// </summary>
    public static bool SupportsConnection(this ConnectionType available, ConnectionType requested)
    {
        if (requested == ConnectionType.Hid)
            return (available & (ConnectionType.HidFido | ConnectionType.HidOtp)) != 0;

        if (requested is ConnectionType.HidFido or ConnectionType.HidOtp or ConnectionType.SmartCard)
            return (available & requested) == requested;

        // Unknown, All, and any other multi-bit combination are not concrete openable requests.
        return false;
    }

    /// <summary>
    ///     Whether a discovery <paramref name="filter"/> matches a device whose available connections are
    ///     <paramref name="available"/> (a capability SET). A device matches if it shares any requested concrete
    ///     connect bit; <see cref="ConnectionType.Hid"/> in the filter expands to <c>HidFido|HidOtp</c> and
    ///     <see cref="ConnectionType.All"/> matches any non-empty capability set.
    /// </summary>
    public static bool Matches(this ConnectionType filter, ConnectionType available)
    {
        if (filter == ConnectionType.Unknown || available == ConnectionType.Unknown)
            return false;

        if (filter == ConnectionType.All)
            return (available & ConcreteConnections) != 0;

        var wanted = filter;
        if ((filter & ConnectionType.Hid) != 0)
            wanted |= ConnectionType.HidFido | ConnectionType.HidOtp;

        return (wanted & available & ConcreteConnections) != 0;
    }

    /// <summary>
    ///     Returns the single concrete connection in <paramref name="available"/>, or <see cref="ConnectionType.Unknown"/>
    ///     when there are zero or more than one. Used by the ambiguity-safe default connect.
    /// </summary>
    public static ConnectionType SingleConcreteConnectionOrUnknown(this ConnectionType available)
    {
        var concrete = available & ConcreteConnections;
        return concrete switch
        {
            ConnectionType.SmartCard => ConnectionType.SmartCard,
            ConnectionType.HidFido => ConnectionType.HidFido,
            ConnectionType.HidOtp => ConnectionType.HidOtp,
            _ => ConnectionType.Unknown
        };
    }
}