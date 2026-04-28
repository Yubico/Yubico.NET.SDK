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
/// Input for the previewSign extension registration (key generation).
/// </summary>
/// <remarks>
/// <para>
/// The previewSign extension allows a FIDO2 credential to sign arbitrary data using a separate
/// signing key bound to the same authenticator. This input specifies the acceptable signing
/// algorithms for registration (key generation).
/// </para>
/// <para>
/// See: CTAP v4 draft Web Authentication sign extension
/// Reference: Plans/cnh-authenticator-rs-previewsign-parity.md
/// </para>
/// </remarks>
public sealed class PreviewSignRegistrationInput
{
    /// <summary>
    /// Gets the ordered list of acceptable COSE algorithms, from most to least preferred.
    /// The authenticator will select the first algorithm it supports.
    /// </summary>
    public IReadOnlyList<int> Algorithms { get; init; }

    /// <summary>
    /// Gets the user presence and verification policy for signing operations.
    /// </summary>
    /// <remarks>
    /// Flags: 0x01 = RequireUserPresence, 0x05 = RequireUserVerification.
    /// Per CTAP v4 draft specification, flags default to 0x01 if not specified.
    /// </remarks>
    public byte Flags { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignRegistrationInput"/>.
    /// </summary>
    /// <param name="algorithms">Ordered list of COSE algorithm identifiers.</param>
    /// <param name="flags">User presence/verification flags (default: 0x01).</param>
    public PreviewSignRegistrationInput(IReadOnlyList<int> algorithms, byte flags = 0x01)
    {
        ArgumentNullException.ThrowIfNull(algorithms);

        if (algorithms.Count == 0)
        {
            throw new ArgumentException("Algorithms list must contain at least one entry.", nameof(algorithms));
        }

        Algorithms = algorithms;
        Flags = flags;
    }
}

/// <summary>
/// Input for the previewSign extension authentication (signing arbitrary data).
/// </summary>
/// <remarks>
/// <para>
/// Maps credential IDs to their corresponding signing parameters. Each entry specifies
/// the key handle, data to sign, and optional algorithm-specific arguments.
/// </para>
/// </remarks>
public sealed class PreviewSignAuthenticationInput
{
    /// <summary>
    /// Gets the dictionary mapping credential IDs to signing parameters.
    /// </summary>
    public IReadOnlyDictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams> SignByCredential { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignAuthenticationInput"/>.
    /// </summary>
    /// <param name="signByCredential">Dictionary mapping credential IDs to signing parameters.</param>
    public PreviewSignAuthenticationInput(
        IReadOnlyDictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams> signByCredential)
    {
        ArgumentNullException.ThrowIfNull(signByCredential);

        if (signByCredential.Count == 0)
        {
            throw new ArgumentException(
                "SignByCredential must contain at least one credential mapping.",
                nameof(signByCredential));
        }

        SignByCredential = signByCredential;
    }
}

/// <summary>
/// Parameters for signing arbitrary data with a previewSign credential.
/// </summary>
/// <remarks>
/// <para>
/// Specifies the key handle, data to be signed, and optional algorithm-specific arguments
/// for a single signing operation.
/// </para>
/// <para>
/// Per CTAP v4 draft specification:
/// - KeyHandle identifies which signing key to use (from prior registration)
/// - Tbs (to-be-signed) is the raw data to sign
/// - CoseSignArgs is the typed, optional COSE_Sign_Args for two-party signing algorithms (e.g. ARKG)
/// </para>
/// </remarks>
public sealed class PreviewSignSigningParams
{
    /// <summary>
    /// Gets the key handle from registration output.
    /// </summary>
    public ReadOnlyMemory<byte> KeyHandle { get; init; }

    /// <summary>
    /// Gets the raw data to be signed.
    /// </summary>
    public ReadOnlyMemory<byte> Tbs { get; init; }

    /// <summary>
    /// Gets the optional typed <c>COSE_Sign_Args</c> for algorithms requiring additional parameters
    /// (e.g. ARKG). When present, the encoder emits canonical CBOR under authentication input
    /// key 7 (wrapped as bstr).
    /// </summary>
    public CoseSignArgs? CoseSignArgs { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignSigningParams"/>.
    /// </summary>
    /// <param name="keyHandle">The key handle for the signing key.</param>
    /// <param name="tbs">Data to be signed.</param>
    /// <param name="coseSignArgs">Optional typed <c>COSE_Sign_Args</c> (required for ARKG algorithms).</param>
    public PreviewSignSigningParams(
        ReadOnlyMemory<byte> keyHandle,
        ReadOnlyMemory<byte> tbs,
        CoseSignArgs? coseSignArgs = null)
    {
        if (keyHandle.Length == 0)
        {
            throw new ArgumentException("KeyHandle must not be empty.", nameof(keyHandle));
        }

        if (tbs.Length == 0)
        {
            throw new ArgumentException("Tbs must not be empty.", nameof(tbs));
        }

        KeyHandle = keyHandle;
        Tbs = tbs;
        CoseSignArgs = coseSignArgs;
    }
}

/// <summary>
/// Output from the previewSign extension registration.
/// </summary>
/// <remarks>
/// Contains the generated signing key information returned by the authenticator.
/// </remarks>
public sealed class PreviewSignRegistrationOutput
{
    /// <summary>
    /// Gets the key handle of the generated signing key.
    /// </summary>
    public ReadOnlyMemory<byte> KeyHandle { get; init; }

    /// <summary>
    /// Gets the COSE public key of the generated signing key.
    /// </summary>
    public ReadOnlyMemory<byte> PublicKey { get; init; }

    /// <summary>
    /// Gets the COSE algorithm identifier of the generated key.
    /// </summary>
    public int Algorithm { get; init; }

    /// <summary>
    /// Gets the attestation object containing the signing key.
    /// </summary>
    /// <remarks>
    /// May be null if authenticator did not provide unsigned extension outputs.
    /// </remarks>
    public ReadOnlyMemory<byte>? AttestationObject { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignRegistrationOutput"/>.
    /// </summary>
    public PreviewSignRegistrationOutput(
        ReadOnlyMemory<byte> keyHandle,
        ReadOnlyMemory<byte> publicKey,
        int algorithm,
        ReadOnlyMemory<byte>? attestationObject = null)
    {
        KeyHandle = keyHandle;
        PublicKey = publicKey;
        Algorithm = algorithm;
        AttestationObject = attestationObject;
    }
}

/// <summary>
/// Output from the previewSign extension authentication.
/// </summary>
/// <remarks>
/// Contains the signature over the to-be-signed data.
/// </remarks>
public sealed class PreviewSignAuthenticationOutput
{
    /// <summary>
    /// Gets the signature bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Signature { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignAuthenticationOutput"/>.
    /// </summary>
    /// <param name="signature">The signature bytes.</param>
    public PreviewSignAuthenticationOutput(ReadOnlyMemory<byte> signature)
    {
        if (signature.Length == 0)
        {
            throw new ArgumentException("Signature must not be empty.", nameof(signature));
        }

        Signature = signature;
    }
}

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
    /// are the inner payload that <see cref="EncodeAuthenticationInput"/> wraps as a CBOR
    /// byte-string under authentication input key 7.
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

        int paramCount = signingParams.CoseSignArgs is not null ? 3 : 2;
        writer.WriteStartMap(paramCount);

        // Key 2: keyHandle
        writer.WriteInt32(AuthenticationInputKeys.KeyHandle);
        writer.WriteByteString(signingParams.KeyHandle.Span);

        // Key 6: tbs
        writer.WriteInt32(AuthenticationInputKeys.ToBeSigned);
        writer.WriteByteString(signingParams.Tbs.Span);

        // Key 7: typed COSE_Sign_Args (optional, wrapped as bstr)
        if (signingParams.CoseSignArgs is not null)
        {
            writer.WriteInt32(AuthenticationInputKeys.AdditionalArgs);
            writer.WriteByteString(EncodeCoseSignArgs(signingParams.CoseSignArgs));
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
    /// Decodes unsigned registration output from unsignedExtensionOutputs["previewSign"].
    /// </summary>
    /// <param name="cbor">CBOR-encoded map with key {7: att-obj}.</param>
    /// <returns>
    /// The raw attestation object bytes.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when CBOR is malformed or attestation object is missing.
    /// </exception>
    /// <remarks>
    /// Per CTAP v4 draft, the unsigned output contains key 7 (att-obj) with a nested attestation object.
    /// Caller is responsible for decoding the nested attestation object.
    /// </remarks>
    public static ReadOnlyMemory<byte> DecodeUnsignedRegistrationOutput(ReadOnlyMemory<byte> cbor)
    {
        var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
        int? mapSize = reader.ReadStartMap();

        ReadOnlyMemory<byte>? attestationObjectBytes = null;

        for (int i = 0; i < mapSize; i++)
        {
            int key = reader.ReadInt32();
            if (key == RegistrationOutputKeys.AttestationObject)
            {
                attestationObjectBytes = reader.ReadByteString();
            }
            else
            {
                reader.SkipValue();
            }
        }

        reader.ReadEndMap();

        if (!attestationObjectBytes.HasValue)
        {
            throw new InvalidOperationException("previewSign unsigned output missing attestation object (key 7)");
        }

        return attestationObjectBytes.Value;
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
}