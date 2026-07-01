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

using System.Formats.Cbor;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Encoding utilities for previewSign extension CBOR format.
/// </summary>
/// <remarks>
/// This is the canonical CBOR encoder for previewSign extension, shared by both
/// Fido2 and WebAuthn layers.
/// </remarks>
public static class PreviewSignCbor
{
    /// <summary>
    /// CBOR keys for registration input.
    /// </summary>
    private static class RegistrationInputKeys
    {
        internal const int Algorithm = 3;
        internal const int Flags = 4;
    }

    /// <summary>
    /// CBOR keys for authentication input.
    /// </summary>
    private static class AuthenticationInputKeys
    {
        internal const int KeyHandle = 2;
        internal const int ToBeSigned = 6;
        internal const int AdditionalArgs = 7;
    }

    /// <summary>
    /// CBOR keys for registration output.
    /// </summary>
    private static class RegistrationOutputKeys
    {
        internal const int Algorithm = 3;
        internal const int Flags = 4;
        internal const int AttestationObject = 7;
    }

    /// <summary>
    /// CBOR keys for authentication output.
    /// </summary>
    private static class AuthenticationOutputKeys
    {
        internal const int Signature = 6;
    }

    /// <summary>
    /// CBOR keys inside a <c>COSE_Sign_Args</c> map.
    /// </summary>
    /// <remarks>
    /// Key 3 (alg) is the request signing-op algorithm; algorithm-specific payload
    /// keys live in negative integer space (-1, -2, ...). For ARKG-P256:
    /// <c>-1 = arkg_kh</c>, <c>-2 = ctx</c>.
    /// </remarks>
    private static class CoseSignArgsKeys
    {
        internal const int Algorithm = 3;
        internal const int ArkgKeyHandle = -1;
        internal const int ArkgContext = -2;
    }

    /// <summary>
    /// Encodes a typed <see cref="CoseSignArgs"/> as CTAP2-canonical CBOR. The returned bytes
    /// are suitable for <see cref="PreviewSignSigningParams.AdditionalArgs"/> and are wrapped
    /// unchanged as a CBOR byte-string under authentication input key 7.
    /// </summary>
    /// <param name="args">The typed <c>COSE_Sign_Args</c> value to encode.</param>
    /// <returns>CTAP2-canonical CBOR bytes for the <c>COSE_Sign_Args</c> map.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the runtime <see cref="CoseSignArgs"/> subtype is not supported by this SDK
    /// build. Forward-compat trap: if a future Yubico-internal subtype is added without an
    /// encoder branch here, the call fails fast rather than silently emitting empty bytes.
    /// </exception>
    public static byte[] EncodeCoseSignArgs(CoseSignArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        return args switch
        {
            ArkgP256SignArgs arkg => EncodeArkgP256SignArgs(arkg),
            _ => throw new ArgumentOutOfRangeException(
                nameof(args),
                $"COSE_Sign_Args subtype '{args.GetType().FullName}' is not supported by this SDK build."),
        };
    }

    /// <summary>
    /// Encodes a typed <see cref="CoseSignArgs"/> helper into raw previewSign <c>additionalArgs</c> bytes.
    /// </summary>
    /// <remarks>
    /// This is an experimental v2 convenience bridge for ARKG and other typed helpers. The generic
    /// previewSign signing API accepts raw <c>additionalArgs</c> bytes and does not require callers
    /// to use typed ARKG helpers.
    /// </remarks>
    /// <param name="args">The typed algorithm-specific signing arguments to encode.</param>
    /// <returns>Raw bytes suitable for <see cref="PreviewSignSigningParams.AdditionalArgs"/>.</returns>
    public static byte[] EncodeAdditionalArgs(CoseSignArgs args) => EncodeCoseSignArgs(args);

    /// <summary>
    /// Encodes an <see cref="ArkgP256SignArgs"/> as the 3-key CBOR map
    /// <c>{3: -65539, -1: kh, -2: ctx}</c> in CTAP2-canonical order.
    /// </summary>
    /// <remarks>
    /// CTAP2-canonical orders integer keys by ascending unsigned encoding: positive ints
    /// (3) precede negative ints (-1, -2). Verified against
    /// <c>cnh-authenticator-rs/src/get_assertion.rs:290-323</c> and
    /// <c>Yubico.NET.SDK-Legacy/Yubico.YubiKey/src/Yubico/YubiKey/Fido2/GetAssertionParameters.cs:402-499</c>.
    /// </remarks>
    private static byte[] EncodeArkgP256SignArgs(ArkgP256SignArgs arkg)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);

        writer.WriteInt32(CoseSignArgsKeys.Algorithm);
        writer.WriteInt32(arkg.Algorithm);

        writer.WriteInt32(CoseSignArgsKeys.ArkgKeyHandle);
        writer.WriteByteString(arkg.KeyHandle.Span);

        writer.WriteInt32(CoseSignArgsKeys.ArkgContext);
        writer.WriteByteString(arkg.Context.Span);

        writer.WriteEndMap();
        return writer.Encode();
    }

    /// <summary>
    /// Encodes registration input (algorithm list + flags) as canonical CBOR.
    /// </summary>
    /// <param name="input">The registration input.</param>
    /// <returns>CBOR-encoded map with keys {3: [alg...], 4: flags}.</returns>
    public static byte[] EncodeRegistrationInput(PreviewSignRegistrationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2); // Two keys: alg (3) and flags (4)

        // Key 3: algorithms array
        writer.WriteInt32(RegistrationInputKeys.Algorithm);
        writer.WriteStartArray(input.Algorithms.Count);
        foreach (var alg in input.Algorithms)
        {
            writer.WriteInt32(alg);
        }
        writer.WriteEndArray();

        // Key 4: flags byte
        writer.WriteInt32(RegistrationInputKeys.Flags);
        writer.WriteInt32(input.Flags);

        writer.WriteEndMap();
        return writer.Encode();
    }

    /// <summary>
    /// Encodes authentication input for a single chosen credential as canonical CBOR.
    /// </summary>
    /// <param name="signingParams">The signing parameters for the chosen credential.</param>
    /// <returns>
    /// CBOR-encoded flat map {2: kh, 6: tbs, 7?: args} for the single credential.
    /// </returns>
    /// <remarks>
    /// Per spec §10.2.1 step 9, the client sends the chosen credential's params as a flat map:
    /// - 2 (kh): key handle (bstr)
    /// - 6 (tbs): to-be-signed data (bstr)
    /// - 7 (args): optional additional args wrapped as bstr (omitted if null)
    /// </remarks>
    public static byte[] EncodeAuthenticationInput(PreviewSignSigningParams signingParams)
    {
        ArgumentNullException.ThrowIfNull(signingParams);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);

        int paramCount = signingParams.AdditionalArgs.HasValue ? 3 : 2;
        writer.WriteStartMap(paramCount);

        // Key 2: keyHandle
        writer.WriteInt32(AuthenticationInputKeys.KeyHandle);
        writer.WriteByteString(signingParams.KeyHandle.Span);

        // Key 6: tbs
        writer.WriteInt32(AuthenticationInputKeys.ToBeSigned);
        writer.WriteByteString(signingParams.Tbs.Span);

        // Key 7: algorithm-specific additionalArgs (optional, wrapped as bstr)
        if (signingParams.AdditionalArgs.HasValue)
        {
            writer.WriteInt32(AuthenticationInputKeys.AdditionalArgs);
            writer.WriteByteString(signingParams.AdditionalArgs.Value.Span);
        }

        writer.WriteEndMap();
        return writer.Encode();
    }

    /// <summary>
    /// Decodes registration output from authData.extensions["previewSign"].
    /// </summary>
    /// <param name="reader">CBOR reader positioned at the start of the previewSign output map.</param>
    /// <returns>
    /// A tuple containing (algorithm, flags) where flags may be null (YubiKey 5.8.0-beta behavior).
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the output is missing the required algorithm key or is malformed.
    /// </exception>
    /// <remarks>
    /// Per CTAP v4 draft §10.2.1 step 5, the output contains keys 3 (alg) and optionally 4 (flags).
    /// Swift's implementation (PreviewSign.swift:132-176) treats flags as optional (absent on YubiKey 5.8.0-beta).
    /// </remarks>
    public static (int Algorithm, int? Flags) DecodeRegistrationOutput(CborReader reader)
    {
        int? mapSize = reader.ReadStartMap();

        int? algorithm = null;
        int? flags = null;

        for (int i = 0; i < mapSize; i++)
        {
            int key = reader.ReadInt32();
            switch (key)
            {
                case RegistrationOutputKeys.Algorithm:
                    algorithm = reader.ReadInt32();
                    break;
                case RegistrationOutputKeys.Flags:
                    flags = reader.ReadInt32();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        if (algorithm is null)
        {
            throw new InvalidOperationException("previewSign registration output missing required algorithm (key 3)");
        }

        return (algorithm.Value, flags);
    }

    /// <summary>
    /// CBOR keys inside the previewSign nested attestation object (CTAP-shaped, integer-keyed).
    /// </summary>
    /// <remarks>
    /// The previewSign unsigned-extension-output payload wraps an inner attestation object whose
    /// keys are CTAP-style integers ({1:fmt, 2:authData, 3:attStmt}), NOT WebAuthn-style text
    /// strings ({"fmt","authData","attStmt"}). This matches the legacy SDK
    /// (Yubico.NET.SDK-Legacy/Yubico/YubiKey/Fido2/PreviewSignExtension.cs:144-147 and 249-282)
    /// and is what YubiKey 5.8.0-beta firmware actually returns on the wire.
    /// </remarks>
    private static class InnerAttestationObjectKeys
    {
        internal const int Fmt = 1;
        internal const int AuthData = 2;
        internal const int AttStmt = 3;
    }

    /// <summary>
    /// Decoded components of the inner attestation object embedded in
    /// unsignedExtensionOutputs["previewSign"][7].
    /// </summary>
    /// <param name="Fmt">Attestation format identifier (e.g. "none", "packed").</param>
    /// <param name="AuthData">Raw CTAP authenticator-data bytes.</param>
    /// <param name="AttStmtRawCbor">Raw CBOR slice of the attStmt map (caller decodes with the appropriate AttestationStatement decoder).</param>
    public readonly record struct InnerAttestationObject(
        string Fmt,
        ReadOnlyMemory<byte> AuthData,
        ReadOnlyMemory<byte> AttStmtRawCbor);

    /// <summary>
    /// Decodes unsigned registration output from unsignedExtensionOutputs["previewSign"].
    /// </summary>
    /// <param name="cbor">CBOR-encoded outer map with key {7: inner-att-obj}.</param>
    /// <returns>
    /// The decoded inner attestation object components (fmt, authData, attStmt raw CBOR).
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when CBOR is malformed or required fields are missing.
    /// </exception>
    /// <remarks>
    /// Per CTAP v4 draft, the unsigned output contains key 7 (att-obj) wrapping a CTAP-shaped
    /// attestation object map {1:fmt, 2:authData, 3:attStmt}. NOTE: the inner map uses integer
    /// keys (not the WebAuthn text-string keys "fmt"/"authData"/"attStmt"). Callers that need a
    /// WebAuthn-spec attestation object must rebuild it from these components rather than feeding
    /// the inner CBOR directly to a WebAuthn decoder.
    /// </remarks>
    public static InnerAttestationObject DecodeUnsignedRegistrationOutput(ReadOnlyMemory<byte> cbor)
    {
        var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
        int? outerMapSize = reader.ReadStartMap();

        ReadOnlyMemory<byte>? innerCbor = null;

        for (int i = 0; i < outerMapSize; i++)
        {
            int key = reader.ReadInt32();
            if (key == RegistrationOutputKeys.AttestationObject)
            {
                innerCbor = reader.ReadByteString();
            }
            else
            {
                reader.SkipValue();
            }
        }

        reader.ReadEndMap();

        if (!innerCbor.HasValue)
        {
            throw new InvalidOperationException("previewSign unsigned output missing attestation object (key 7)");
        }

        return DecodeInnerAttestationObject(innerCbor.Value);
    }

    /// <summary>
    /// Decodes the CTAP-shaped inner attestation object: {1:fmt, 2:authData, 3:attStmt}.
    /// Captures the raw CBOR slice for attStmt so callers can route it to a format-specific decoder.
    /// </summary>
    private static InnerAttestationObject DecodeInnerAttestationObject(ReadOnlyMemory<byte> innerCbor)
    {
        var reader = new CborReader(innerCbor, CborConformanceMode.Ctap2Canonical);
        int? mapSize = reader.ReadStartMap();

        string? fmt = null;
        ReadOnlyMemory<byte>? authData = null;
        ReadOnlyMemory<byte>? attStmtRaw = null;

        for (int i = 0; i < mapSize; i++)
        {
            int key = reader.ReadInt32();
            switch (key)
            {
                case InnerAttestationObjectKeys.Fmt:
                    fmt = reader.ReadTextString();
                    break;
                case InnerAttestationObjectKeys.AuthData:
                    authData = reader.ReadByteString();
                    break;
                case InnerAttestationObjectKeys.AttStmt:
                    var bytesBefore = reader.BytesRemaining;
                    reader.SkipValue();
                    var bytesConsumed = bytesBefore - reader.BytesRemaining;
                    var offset = innerCbor.Length - bytesBefore;
                    attStmtRaw = innerCbor.Slice(offset, bytesConsumed);
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        if (fmt is null)
        {
            throw new InvalidOperationException("previewSign inner attestation object missing fmt (key 1)");
        }

        if (!authData.HasValue)
        {
            throw new InvalidOperationException("previewSign inner attestation object missing authData (key 2)");
        }

        if (!attStmtRaw.HasValue)
        {
            throw new InvalidOperationException("previewSign inner attestation object missing attStmt (key 3)");
        }

        return new InnerAttestationObject(fmt, authData.Value, attStmtRaw.Value);
    }

    /// <summary>
    /// Decodes authentication output from authData.extensions["previewSign"].
    /// </summary>
    /// <param name="cbor">CBOR-encoded map with key {6: sig}.</param>
    /// <returns>
    /// The signature bytes.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when CBOR is malformed or signature is missing.
    /// </exception>
    /// <remarks>
    /// Per CTAP v4 draft §10.2.1 step 10, the output contains key 6 (sig) with the signature.
    /// </remarks>
    public static ReadOnlyMemory<byte> DecodeAuthenticationOutput(ReadOnlyMemory<byte> cbor)
    {
        var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
        int? mapSize = reader.ReadStartMap();

        ReadOnlyMemory<byte>? signature = null;

        for (int i = 0; i < mapSize; i++)
        {
            int key = reader.ReadInt32();
            if (key == AuthenticationOutputKeys.Signature)
            {
                signature = reader.ReadByteString();
            }
            else
            {
                reader.SkipValue();
            }
        }

        reader.ReadEndMap();

        if (!signature.HasValue)
        {
            throw new InvalidOperationException("previewSign authentication output missing signature (key 6)");
        }

        return signature.Value;
    }

    /// <summary>
    /// Attempts to extract a typed <see cref="PreviewSignGeneratedKey"/> from a
    /// MakeCredential response that includes the previewSign extension.
    /// </summary>
    /// <param name="response">The MakeCredential response.</param>
    /// <param name="generatedKey">
    /// When this method returns <c>true</c>, contains the extracted generated key;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the response contains a valid previewSign ARKG-P256 seed key;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method:
    /// 1. Checks for unsignedExtensionOutputs["previewSign"]
    /// 2. Decodes the CTAP-shaped inner attestation object
    /// 3. Parses the authData to extract AttestedCredentialData (credential ID + COSE public key)
    /// 4. Decodes the COSE public key and validates it's a CoseArkgP256SeedKey
    /// 5. Constructs a PreviewSignGeneratedKey from the extracted components
    /// </para>
    /// <para>
    /// Returns <c>false</c> if:
    /// - The response lacks unsignedExtensionOutputs["previewSign"]
    /// - The inner attestation object is malformed
    /// - The authData lacks AttestedCredentialData
    /// - The COSE public key is not a CoseArkgP256SeedKey variant
    /// </para>
    /// </remarks>
    public static bool TryExtractGeneratedKey(
        Credentials.MakeCredentialResponse response,
        out PreviewSignGeneratedKey? generatedKey)
    {
        generatedKey = null;

        try
        {
            if (response.UnsignedExtensionOutputs?.ContainsKey("previewSign") != true)
            {
                return false;
            }

            var previewSignCbor = response.UnsignedExtensionOutputs["previewSign"];
            var innerAttestation = DecodeUnsignedRegistrationOutput(previewSignCbor);
            var authData = Credentials.AuthenticatorData.Parse(innerAttestation.AuthData);

            if (authData.AttestedCredentialData is null)
            {
                return false;
            }

            var coseKey = Cose.CoseKey.Decode(authData.AttestedCredentialData.CredentialPublicKey);
            if (coseKey is not Cose.CoseArkgP256SeedKey arkgSeedKey)
            {
                return false;
            }

            generatedKey = new PreviewSignGeneratedKey(
                authData.AttestedCredentialData.CredentialId,
                arkgSeedKey.BlPublicKey,
                arkgSeedKey.KemPublicKey,
                arkgSeedKey.DerivedKeyAlgorithm);  // -9 (Esp256), NOT -65700 (seed-key alg)

            return true;
        }
        catch (System.Formats.Cbor.CborContentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}