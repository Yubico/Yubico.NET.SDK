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

using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.WebAuthn.Attestation;
using Yubico.YubiKit.WebAuthn.Cose;

namespace Yubico.YubiKit.WebAuthn.Client.Registration;

/// <summary>
/// Response from WebAuthn credential registration (MakeCredential).
/// </summary>
public sealed record class RegistrationResponse
{
    /// <summary>
    /// Gets the credential ID.
    /// </summary>
    public required ReadOnlyMemory<byte> CredentialId { get; init; }

    /// <summary>
    /// Gets the parsed attestation object.
    /// </summary>
    public required WebAuthnAttestationObject AttestationObject { get; init; }

    /// <summary>
    /// Gets the raw CBOR-encoded attestation object bytes.
    /// </summary>
    public required ReadOnlyMemory<byte> RawAttestationObject { get; init; }

    /// <summary>
    /// Gets the parsed authenticator data.
    /// </summary>
    public required WebAuthnAuthenticatorData AuthenticatorData { get; init; }

    /// <summary>
    /// Gets the raw authenticator data bytes.
    /// </summary>
    public required ReadOnlyMemory<byte> RawAuthenticatorData { get; init; }

    /// <summary>
    /// Gets the attestation statement.
    /// </summary>
    public required AttestationStatement AttestationStatement { get; init; }

    /// <summary>
    /// Gets the available transports for this credential.
    /// </summary>
    public IReadOnlyList<WebAuthnTransport>? Transports { get; init; }

    /// <summary>
    /// Gets the public key for the created credential.
    /// </summary>
    public required CoseKey PublicKey { get; init; }

    /// <summary>
    /// Gets the AAGUID from the authenticator data.
    /// </summary>
    public required Aaguid Aaguid { get; init; }

    /// <summary>
    /// Gets the signature counter value.
    /// </summary>
    public required uint SignCount { get; init; }

    /// <summary>
    /// Gets the client data that was hashed for this operation.
    /// </summary>
    public required WebAuthnClientData ClientData { get; init; }

    /// <summary>
    /// Gets the client extension results.
    /// </summary>
    public WebAuthn.Extensions.RegistrationExtensionOutputs? ClientExtensionResults { get; init; }
}