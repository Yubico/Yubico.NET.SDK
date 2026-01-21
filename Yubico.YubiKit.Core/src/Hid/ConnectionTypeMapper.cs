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

using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// Maps between YubiKey HID interface types and public ConnectionType API.
/// </summary>
public static class ConnectionTypeMapper
{
    /// <summary>
    /// Maps YubiKey HID interface type to the public ConnectionType API.
    /// </summary>
    /// <param name="interfaceType">The YubiKey HID interface type.</param>
    /// <returns>The corresponding <see cref="ConnectionType"/>.</returns>
    public static ConnectionType ToConnectionType(HidInterfaceType interfaceType) =>
        interfaceType switch
        {
            HidInterfaceType.Fido => ConnectionType.HidFido,
            HidInterfaceType.Otp => ConnectionType.HidOtp,
            _ => ConnectionType.Hid
        };
    
    /// <summary>
    /// Checks if a YubiKey HID interface type supports a specific connection type.
    /// </summary>
    /// <param name="interfaceType">The YubiKey HID interface type.</param>
    /// <param name="connectionType">The requested connection type.</param>
    /// <returns><c>true</c> if the interface supports the connection type; otherwise, <c>false</c>.</returns>
    public static bool SupportsConnectionType(
        HidInterfaceType interfaceType, 
        ConnectionType connectionType) =>
        (interfaceType, connectionType) switch
        {
            (HidInterfaceType.Fido, ConnectionType.HidFido) => true,
            (HidInterfaceType.Otp, ConnectionType.HidOtp) => true,
            _ => false
        };
}
