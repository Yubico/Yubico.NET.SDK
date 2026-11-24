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

using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Cbor;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;

namespace Yubico.YubiKit.Ctap2.Extensions;

/// <summary>
/// Represents the generated signing key from previewSign registration.
/// </summary>
/// <remarks>
/// Corresponds to <see href="https://yubicolabs.github.io/webauthn-sign-extension/4/#dictdef-authenticationextensionssigngeneratedkey">
/// AuthenticationExtensionsSignGeneratedKey</see> in the experimental previewSign draft.
/// Use <see cref="TryFromMakeCredentialResponse"/> to extract it from a registration response.
/// Extraction does not verify attestation, device identity or key binding.
/// </remarks>
[System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
public record class PreviewSignGeneratedKey
{
    /// <summary>Initializes the common fields of a generated signing key.</summary>
    private protected PreviewSignGeneratedKey(
        ReadOnlyMemory<byte> keyHandle,
        CoseKey publicKey,
        CoseAlgorithm algorithm,
        ReadOnlyMemory<byte> attestationObject)
    {
        if (attestationObject.IsEmpty)
        {
            throw new ArgumentException("The generated-key attestation object is required.", nameof(attestationObject));
        }

        KeyHandle = keyHandle;
        PublicKey = publicKey;
        Algorithm = algorithm;
        AttestationObject = attestationObject;
    }

    /// <summary>Gets the generated signing-key handle. It may be empty.</summary>
    public ReadOnlyMemory<byte> KeyHandle { get; }

    /// <summary>Gets the decoded signing public key from the inner attested credential data.</summary>
    public CoseKey PublicKey { get; }

    /// <summary>Gets the signing algorithm reported in the outer authenticator data.</summary>
    /// <remarks>This can differ from the algorithm identifier in <see cref="PublicKey"/>.</remarks>
    public CoseAlgorithm Algorithm { get; }

    /// <summary>Gets the generated-key attestation object as WebAuthn-shaped CBOR with text keys.</summary>
    public ReadOnlyMemory<byte> AttestationObject { get; }

    /// <summary>Attempts to extract the previewSign generated key from a registration response.</summary>
    /// <param name="response">The MakeCredential response.</param>
    /// <param name="generatedKey">The extracted key on success; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if both outputs and the inner credential data can be decoded.</returns>
    /// <remarks>
    /// Reads the signing algorithm from the signed outer output and the handle and public key from
    /// the inner credential data. Unmodeled public keys are preserved on the generic result.
    /// Attestation is repacked, not verified; this does not establish device identity, algorithm
    /// agreement, key binding or inner signing policy. Malformed output is diagnosed without logging its contents.
    /// </remarks>
    public static bool TryFromMakeCredentialResponse(
        MakeCredentialResponse response,
        [NotNullWhen(true)] out PreviewSignGeneratedKey? generatedKey)
    {
        generatedKey = null;

        try
        {
            if (!TryGetRegistrationParts(response, out var signed, out var unsigned))
            {
                return false;
            }

            var signedReader = new CborReader(signed, CborConformanceMode.Ctap2Canonical);
            (int algorithm, _) = PreviewSignCbor.DecodeRegistrationOutput(signedReader);
            var inner = PreviewSignCbor.DecodeUnsignedRegistrationOutput(unsigned);
            var credential = AuthenticatorData.Parse(inner.AuthData).AttestedCredentialData;
            if (credential is null)
            {
                LogMalformedRegistration();
                return false;
            }

            generatedKey = Create(
                credential.CredentialId,
                CoseKey.Decode(credential.CredentialPublicKey),
                new CoseAlgorithm(algorithm),
                PreviewSignCbor.EncodeWebAuthnAttestationObject(inner));

            return true;
        }
        catch (Exception ex) when (ex is CborContentException or InvalidOperationException or ArgumentException or OverflowException)
        {
            LogMalformedRegistration();
            return false;
        }
    }

    internal static PreviewSignGeneratedKey Create(
        ReadOnlyMemory<byte> keyHandle,
        CoseKey publicKey,
        CoseAlgorithm algorithm,
        ReadOnlyMemory<byte> attestationObject) => publicKey switch
        {
            CoseArkgP256SeedKey seedKey => new ArkgP256GeneratedKey(keyHandle, seedKey, algorithm, attestationObject),
            _ => new PreviewSignGeneratedKey(keyHandle, publicKey, algorithm, attestationObject)
        };

    private static bool TryGetRegistrationParts(
        MakeCredentialResponse response,
        out ReadOnlyMemory<byte> signed,
        out Memory<byte> unsigned)
    {
        unsigned = default;
        bool hasUnsigned = response.UnsignedExtensionOutputs?.TryGetValue(ExtensionIdentifiers.PreviewSign, out unsigned) == true;
        signed = default;
        bool hasSigned = response.ExtensionOutputs is { } signedOutputs &&
            ExtensionOutput.Decode(signedOutputs).TryGetRawExtension(ExtensionIdentifiers.PreviewSign, out signed);
        if (hasSigned != hasUnsigned)
        {
            LogMalformedRegistration();
        }
        return hasSigned && hasUnsigned;
    }

    private static void LogMalformedRegistration()
    {
        try
        {
            RegistrationDiagnostics.Logger.LogWarning("Malformed previewSign registration output was ignored.");
        }
        catch (Exception)
        {
            // Diagnostics are best-effort; a provider must not break the Try contract.
        }
    }

    private static class RegistrationDiagnostics
    {
        internal static readonly ILogger Logger = YubiKitLogging.CreateLogger<PreviewSignGeneratedKey>();
    }
}