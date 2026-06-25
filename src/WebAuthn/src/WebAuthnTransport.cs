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

namespace Yubico.YubiKit.WebAuthn;

/// <summary>
/// Represents an authenticator transport hint for WebAuthn credential descriptors.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#enum-transport">
/// WebAuthn AuthenticatorTransport</see>.
/// </remarks>
public readonly record struct WebAuthnTransport
{
    /// <summary>
    /// USB transport.
    /// </summary>
    public static readonly WebAuthnTransport Usb = new("usb");

    /// <summary>
    /// NFC transport.
    /// </summary>
    public static readonly WebAuthnTransport Nfc = new("nfc");

    /// <summary>
    /// Bluetooth Low Energy transport.
    /// </summary>
    public static readonly WebAuthnTransport Ble = new("ble");

    /// <summary>
    /// Smart card transport.
    /// </summary>
    public static readonly WebAuthnTransport SmartCard = new("smart-card");

    /// <summary>
    /// Hybrid transport (QR code + BLE).
    /// </summary>
    public static readonly WebAuthnTransport Hybrid = new("hybrid");

    /// <summary>
    /// Internal platform authenticator.
    /// </summary>
    public static readonly WebAuthnTransport Internal = new("internal");

    /// <summary>
    /// Gets the transport value as a string.
    /// </summary>
    public string Value { get; }

    private WebAuthnTransport(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a transport from an unknown string value.
    /// </summary>
    /// <param name="value">The transport string.</param>
    /// <returns>A <see cref="WebAuthnTransport"/> with the specified value.</returns>
    public static WebAuthnTransport Unknown(string value) => new(value);

    /// <summary>
    /// Returns the transport value as a string.
    /// </summary>
    public override string ToString() => Value;
}
