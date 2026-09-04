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

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>
/// The kind of secret being requested by an <see cref="ICredentialPrompt"/>.
/// </summary>
/// <remarks>
/// Implementations should treat an unrecognized value as a generic secret
/// request rather than failing, so that new kinds can be added without
/// breaking existing implementations.
/// </remarks>
public enum CredentialKind
{
    /// <summary>A PIN (personal identification number).</summary>
    Pin,

    /// <summary>A PUK (PIN unblocking key).</summary>
    Puk,

    /// <summary>A password or passphrase.</summary>
    Password,

    /// <summary>A management key, conventionally entered as hexadecimal.</summary>
    ManagementKey,

    /// <summary>A new PIN being established by a change or initialize flow.</summary>
    NewPin,

    /// <summary>A new PUK being established.</summary>
    NewPuk,

    /// <summary>A new password being established.</summary>
    NewPassword,

    /// <summary>A reset code, for example the OpenPGP resetting code.</summary>
    ResetCode
}