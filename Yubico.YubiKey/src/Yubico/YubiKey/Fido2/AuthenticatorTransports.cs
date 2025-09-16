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

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// This class contains the standard-specified strings that describe the
    /// possible FIDO2 transports an authenticator can use to communicate with
    /// clients. Use these strings when adding transports to transport lists in
    /// order to guarantee compliance with the standards.
    /// </summary>
    /// <remarks>
    /// The standards specify, in various places, a <c>transports</c> element
    /// that is an array of strings describing transports. The standards also
    /// define some specific strings for some specific transports. For example,
    /// to specify USB as a supported transport, the string is <c>"usb"</c>. The
    /// standards also allow for non-standard strings.
    /// <para>
    /// This static class contains defined strings so that when you add a
    /// transport to a list, you can guarantee using the correct value.
    /// </para>
    /// <para>
    /// For example,
    /// <code>
    ///    credentialId.AddTransport(AuthenticatorTransports1.Usb);
    /// </code>
    /// </para>
    /// </remarks>
    public static class AuthenticatorTransports
    {
        /// <summary>
        /// The string identifier that specifies the USB transport is supported.
        /// </summary>
        public const string Usb = "usb";

        /// <summary>
        /// The string identifier that specifies the NFC transport is supported.
        /// </summary>
        public const string Nfc = "nfc";

        /// <summary>
        /// The string identifier that specifies the bluetooth transport is
        /// supported.
        /// </summary>
        public const string Bluetooth = "ble";

        /// <summary>
        /// The string identifier that specifies a hybrid transport is supported.
        /// "Hybrid" indicates the respective authenticator can be contacted
        /// using a combination of (often separate) data-transport and proximity
        /// mechanisms. This supports, for example, authentication on a desktop
        /// computer using a smartphone.
        /// </summary>
        public const string Hybrid = "hybrid";

        /// <summary>
        /// The string identifier that specifies an internal transport is
        /// supported. "Internal" indicates the respective authenticator is
        /// contacted using a client device-specific transport, i.e., it is a
        /// platform authenticator. These authenticators are not removable from
        /// the client device.
        /// </summary>
        public const string Internal = "internal";
    }
}
