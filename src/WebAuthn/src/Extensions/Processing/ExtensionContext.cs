// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.WebAuthn.Extensions.Processing;

/// <summary>
/// What every extension processor may rely on for one ceremony: the authenticator's GetInfo (to decide
/// support), the negotiated PIN/UV protocol (available without a token; CTAP 2.0 hmac-secret needs it)
/// and the backend for follow-up authenticator calls (key agreement, large-blob storage).
/// </summary>
internal sealed record ExtensionContext(
    AuthenticatorInfo Info,
    IPinUvAuthProtocol Protocol,
    IWebAuthnBackend Backend)
{
    /// <summary>True when GetInfo advertises <paramref name="extension"/> (CTAP §6.4 "extensions").</summary>
    public bool Supports(string extension) => Info.Extensions.Contains(extension, StringComparer.Ordinal);

    /// <summary>True when GetInfo advertises the option as enabled.</summary>
    public bool OptionEnabled(string option) => Info.Options.TryGetValue(option, out var enabled) && enabled;
}