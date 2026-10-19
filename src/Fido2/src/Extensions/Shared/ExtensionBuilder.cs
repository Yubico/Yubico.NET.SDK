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
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Builder for constructing CTAP2 extension input maps.
/// </summary>
/// <remarks>
/// <para>
/// This builder creates the CBOR-encoded extensions map used in makeCredential
/// and getAssertion requests. Extensions are added using the fluent API and
/// encoded to CBOR when Build() is called.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var extensions = new ExtensionBuilder()
///     .WithCredProtect(CredProtectPolicy.UserVerificationRequired)
///     .WithCredBlob(myBlobData)
///     .Build();
/// </code>
/// </para>
/// </remarks>
public sealed class ExtensionBuilder
{
    private CredProtectPolicy? _credProtect;
    private bool _credProtectEnforcePolicy;
    private ReadOnlyMemory<byte>? _credBlob;
    private bool _getCredBlob;
    private LargeBlobInput? _largeBlob;
    private HmacSecretInput? _hmacSecret;
    private HmacSecretInput? _hmacSecretMc;
    private bool _minPinLength;
    private bool _largeBlobKey;
    private bool _hmacSecretRequested;
    private PreviewSignRegistrationInput? _previewSignRegistration;
    private PreviewSignAuthenticationInput? _previewSignAuthentication;

    /// <summary>
    /// Adds the credProtect extension with the specified policy.
    /// </summary>
    /// <param name="policy">The credential protection policy.</param>
    /// <param name="enforcePolicy">If true, credential creation fails if policy cannot be honored.</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithCredProtect(
        CredProtectPolicy policy,
        bool enforcePolicy = false)
    {
        _credProtect = policy;
        _credProtectEnforcePolicy = enforcePolicy;
        return this;
    }

    /// <summary>
    /// Adds the credBlob extension with data to store (authenticatorMakeCredential only).
    /// </summary>
    /// <remarks>
    /// CTAP 2.1-2.3 §12.2.1: the makeCredential input is a "Byte String containing the credBlob value".
    /// To read a stored blob back at getAssertion use <see cref="WithGetCredBlob"/>; a byte string is
    /// not the getAssertion input.
    /// </remarks>
    /// <param name="blob">The blob data to store (at most the authenticator's maxCredBlobLength).</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithCredBlob(ReadOnlyMemory<byte> blob)
    {
        _credBlob = blob;
        return this;
    }

    /// <summary>
    /// Requests the stored credBlob (authenticatorGetAssertion only).
    /// </summary>
    /// <remarks>
    /// CTAP 2.1-2.3 §12.2.1: the platform sends <c>"credBlob": true</c> in the getAssertion extensions
    /// when the relying party set the <c>getCredBlob</c> client input.
    /// </remarks>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithGetCredBlob()
    {
        _getCredBlob = true;
        return this;
    }

    /// <summary>
    /// Adds the largeBlob extension for makeCredential (<c>largeBlob: {support}</c>).
    /// </summary>
    /// <remarks>
    /// YubiKeys store large blobs through the <c>largeBlobKey</c> extension (CTAP 2.1-2.3 §12.3,
    /// <see cref="WithLargeBlobKey"/>) and the authenticatorLargeBlobs command (§6.10,
    /// <see cref="LargeBlobs.LargeBlobStorage"/>). The direct getAssertion <c>largeBlob</c> read/write
    /// extension of CTAP 2.2-2.3 §12.4 is not implemented: §12.4 says authenticators "MUST NOT support
    /// both extensions", so no YubiKey can exercise it.
    /// </remarks>
    /// <param name="support">The required support level.</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithLargeBlob(LargeBlobSupport support = LargeBlobSupport.Preferred)
    {
        _largeBlob = new LargeBlobInput { Support = support };
        return this;
    }

    /// <summary>
    /// Adds the largeBlobKey CTAP extension for makeCredential or getAssertion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the CTAP-level extension ("largeBlobKey": true) that instructs the
    /// authenticator to return a 32-byte large blob key with the credential response.
    /// This key is then used to encrypt/decrypt data in the large blob array.
    /// </para>
    /// <para>
    /// This is distinct from the WebAuthn-level "largeBlob" extension which uses
    /// "support": "required"/"preferred" semantics. Use <see cref="WithLargeBlob"/>
    /// for the WebAuthn-level extension.
    /// </para>
    /// </remarks>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithLargeBlobKey()
    {
        _largeBlobKey = true;
        return this;
    }

    /// <summary>
    /// Requests hmac-secret at authenticatorMakeCredential: CTAP 2.1-2.3 §12.7 <c>"hmac-secret": true</c>.
    /// WebAuthn's prf extension (L3 §10.1.4) sends this at registration.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithHmacSecret()
    {
        _hmacSecretRequested = true;
        return this;
    }

    /// <summary>
    /// Adds the hmac-secret extension for getAssertion.
    /// </summary>
    /// <param name="input">The hmac-secret input parameters.</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithHmacSecret(HmacSecretInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _hmacSecret = input;
        return this;
    }

    /// <summary>
    /// Adds the hmac-secret extension for getAssertion using protocol helpers.
    /// </summary>
    /// <param name="protocol">The PIN/UV auth protocol.</param>
    /// <param name="sharedSecret">The shared secret from key agreement.</param>
    /// <param name="keyAgreement">The platform's key agreement public key.</param>
    /// <param name="salt1">The first salt (32 bytes).</param>
    /// <param name="salt2">Optional second salt (32 bytes). Pass empty span for no second salt.</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithHmacSecret(
        IPinUvAuthProtocol protocol,
        ReadOnlySpan<byte> sharedSecret,
        IReadOnlyDictionary<int, object?> keyAgreement,
        ReadOnlySpan<byte> salt1,
        ReadOnlySpan<byte> salt2 = default) =>
        WithHmacSecret(BuildHmacSecretInput(protocol, sharedSecret, keyAgreement, salt1, salt2));

    /// <summary>CTAP §12.7: saltEnc = encrypt(sharedSecret, salt1 || salt2), saltAuth = authenticate(sharedSecret, saltEnc).</summary>
    private static HmacSecretInput BuildHmacSecretInput(
        IPinUvAuthProtocol protocol,
        ReadOnlySpan<byte> sharedSecret,
        IReadOnlyDictionary<int, object?> keyAgreement,
        ReadOnlySpan<byte> salt1,
        ReadOnlySpan<byte> salt2)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(keyAgreement);

        if (salt1.Length != 32)
        {
            throw new ArgumentException("Salt1 must be exactly 32 bytes.", nameof(salt1));
        }

        if (!salt2.IsEmpty && salt2.Length != 32)
        {
            throw new ArgumentException("Salt2 must be exactly 32 bytes if provided.", nameof(salt2));
        }

        // Prepare salt data
        byte[] saltData;
        if (!salt2.IsEmpty)
        {
            saltData = new byte[64];
            salt1.CopyTo(saltData);
            salt2.CopyTo(saltData.AsSpan(32));
        }
        else
        {
            saltData = salt1.ToArray();
        }

        // Encrypt salts
        var saltEnc = protocol.Encrypt(sharedSecret, saltData);

        // Compute auth tag
        var saltAuth = protocol.Authenticate(sharedSecret, saltEnc);

        return new HmacSecretInput
        {
            KeyAgreement = keyAgreement,
            SaltEnc = saltEnc,
            SaltAuth = saltAuth,
            PinUvAuthProtocol = protocol.Version
        };
    }

    /// <summary>
    /// Adds the hmac-secret-mc extension (CTAP 2.2-2.3 §12.8): evaluate hmac-secret during
    /// authenticatorMakeCredential.
    /// </summary>
    /// <remarks>
    /// §12.8: "This extension input is the same as the hmac secret extension's getAssertion input" and
    /// "the hmac-secret extension MUST also be present with the value of "hmac-secret" set to true", so
    /// this method also emits <c>"hmac-secret": true</c>. Advertised by YubiKey 5.8 and later.
    /// </remarks>
    /// <param name="input">The encrypted and authenticated salts, as for getAssertion.</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithHmacSecretMakeCredential(HmacSecretInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _hmacSecretMc = input;
        _hmacSecretRequested = true;
        return this;
    }

    /// <summary>
    /// Adds the hmac-secret-mc extension (CTAP 2.2-2.3 §12.8), encrypting the salts under the shared
    /// secret exactly as <see cref="WithHmacSecret(IPinUvAuthProtocol, ReadOnlySpan{byte}, IReadOnlyDictionary{int, object?}, ReadOnlySpan{byte}, ReadOnlySpan{byte})"/> does.
    /// </summary>
    public ExtensionBuilder WithHmacSecretMakeCredential(
        IPinUvAuthProtocol protocol,
        ReadOnlySpan<byte> sharedSecret,
        IReadOnlyDictionary<int, object?> keyAgreement,
        ReadOnlySpan<byte> salt1,
        ReadOnlySpan<byte> salt2 = default) =>
        WithHmacSecretMakeCredential(BuildHmacSecretInput(protocol, sharedSecret, keyAgreement, salt1, salt2));

    /// <summary>
    /// Adds the minPinLength extension.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithMinPinLength()
    {
        _minPinLength = true;
        return this;
    }



    /// <summary>
    /// Adds previewSign extension with key generation parameters for makeCredential.
    /// </summary>
    /// <param name="input">The previewSign registration input.</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithPreviewSign(PreviewSignRegistrationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _previewSignRegistration = input;
        return this;
    }

    /// <summary>
    /// Adds previewSign extension with signing parameters for getAssertion.
    /// </summary>
    /// <param name="input">The previewSign authentication input.</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithPreviewSign(PreviewSignAuthenticationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _previewSignAuthentication = input;
        return this;
    }

    /// <summary>
    /// Builds the CBOR-encoded extensions map.
    /// </summary>
    /// <returns>The CBOR-encoded extensions map, or null if no extensions.</returns>
    public ReadOnlyMemory<byte>? Build()
    {
        if (!HasExtensions())
        {
            return null;
        }

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        Encode(writer);
        return writer.Encode();
    }

    /// <summary>
    /// Encodes the extensions map to a CBOR writer.
    /// </summary>
    /// <param name="writer">The CBOR writer.</param>
    public void Encode(CborWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var count = CountExtensions();
        writer.WriteStartMap(count);

        // Extensions must be sorted by key for canonical CBOR
        // Sort order: "credBlob" < "credProtect" < "hmac-secret" < "hmac-secret-mc" < "largeBlob" < "minPinLength" < "previewSign"

        if (_credBlob.HasValue && _getCredBlob)
        {
            throw new InvalidOperationException(
                "WithCredBlob (makeCredential) and WithGetCredBlob (getAssertion) cannot be combined in one request.");
        }

        if (_credBlob.HasValue)
        {
            writer.WriteTextString(ExtensionIdentifiers.CredBlob);
            writer.WriteByteString(_credBlob.Value.Span);
        }
        else if (_getCredBlob)
        {
            writer.WriteTextString(ExtensionIdentifiers.CredBlob);
            writer.WriteBoolean(true);
        }

        if (_credProtect.HasValue)
        {
            writer.WriteTextString(ExtensionIdentifiers.CredProtect);
            writer.WriteInt32((int)_credProtect.Value);
        }

        if (_hmacSecret is not null && _hmacSecretRequested)
        {
            throw new InvalidOperationException(
                "WithHmacSecret() (makeCredential: \"hmac-secret\": true) and WithHmacSecret(input) (getAssertion map) cannot be combined in one request.");
        }

        if (_hmacSecret is not null)
        {
            writer.WriteTextString(ExtensionIdentifiers.HmacSecret);
            _hmacSecret.Encode(writer);
        }
        else if (_hmacSecretRequested)
        {
            writer.WriteTextString(ExtensionIdentifiers.HmacSecret);
            writer.WriteBoolean(true);
        }

        if (_hmacSecretMc is not null)
        {
            writer.WriteTextString(ExtensionIdentifiers.HmacSecretMakeCredential);
            _hmacSecretMc.Encode(writer);
        }

        if (_largeBlob is not null)
        {
            writer.WriteTextString(ExtensionIdentifiers.LargeBlob);
            _largeBlob.Encode(writer);
        }

        if (_largeBlobKey)
        {
            writer.WriteTextString(ExtensionIdentifiers.LargeBlobKey);
            writer.WriteBoolean(true);
        }

        if (_minPinLength)
        {
            writer.WriteTextString(ExtensionIdentifiers.MinPinLength);
            writer.WriteBoolean(true);
        }

        if (_previewSignRegistration is not null)
        {
            writer.WriteTextString(ExtensionIdentifiers.PreviewSign);
            EncodePreviewSignRegistrationInput(writer, _previewSignRegistration);
        }
        else if (_previewSignAuthentication is not null)
        {
            writer.WriteTextString(ExtensionIdentifiers.PreviewSign);
            EncodePreviewSignAuthenticationInput(writer, _previewSignAuthentication);
        }

        writer.WriteEndMap();
    }

    private static void EncodePreviewSignRegistrationInput(CborWriter writer, PreviewSignRegistrationInput input)
    {
        var encodedBytes = PreviewSignCbor.EncodeRegistrationInput(input);
        var reader = new CborReader(encodedBytes, CborConformanceMode.Ctap2Canonical);

        // Copy the CBOR map directly
        CopyCborValue(reader, writer);
    }

    private static void EncodePreviewSignAuthenticationInput(CborWriter writer, PreviewSignAuthenticationInput input)
    {
        // Extract the single credential's signing params (validated by the encoder)
        var firstEntry = input.SignByCredential.First();
        var signingParams = firstEntry.Value;

        var encodedBytes = PreviewSignCbor.EncodeAuthenticationInput(signingParams);
        var reader = new CborReader(encodedBytes, CborConformanceMode.Ctap2Canonical);

        // Copy the CBOR map directly
        CopyCborValue(reader, writer);
    }

    private static void CopyCborValue(CborReader reader, CborWriter writer)
    {
        var state = reader.PeekState();

        switch (state)
        {
            case CborReaderState.StartMap:
                int? mapSize = reader.ReadStartMap();
                writer.WriteStartMap(mapSize);
                for (int i = 0; i < mapSize; i++)
                {
                    CopyCborValue(reader, writer);
                    CopyCborValue(reader, writer);
                }
                reader.ReadEndMap();
                writer.WriteEndMap();
                break;

            case CborReaderState.StartArray:
                int? arraySize = reader.ReadStartArray();
                writer.WriteStartArray(arraySize);
                for (int i = 0; i < arraySize; i++)
                {
                    CopyCborValue(reader, writer);
                }
                reader.ReadEndArray();
                writer.WriteEndArray();
                break;

            case CborReaderState.UnsignedInteger:
                writer.WriteUInt64(reader.ReadUInt64());
                break;

            case CborReaderState.NegativeInteger:
                writer.WriteInt64(reader.ReadInt64());
                break;

            case CborReaderState.ByteString:
                writer.WriteByteString(reader.ReadByteString());
                break;

            case CborReaderState.TextString:
                writer.WriteTextString(reader.ReadTextString());
                break;

            case CborReaderState.Boolean:
                writer.WriteBoolean(reader.ReadBoolean());
                break;

            case CborReaderState.Null:
                reader.ReadNull();
                writer.WriteNull();
                break;

            default:
                throw new InvalidOperationException($"Unsupported CBOR state: {state}");
        }
    }

    private bool HasExtensions()
    {
        return _credProtect.HasValue ||
               _credBlob.HasValue ||
               _getCredBlob ||
               _largeBlob is not null ||
               _largeBlobKey ||
               _hmacSecret is not null ||
               _hmacSecretRequested ||
               _hmacSecretMc is not null ||
               _minPinLength ||
               _previewSignRegistration is not null ||
               _previewSignAuthentication is not null;
    }

    private int CountExtensions()
    {
        var count = 0;
        if (_credProtect.HasValue) count++;
        if (_credBlob.HasValue || _getCredBlob) count++;
        if (_largeBlob is not null) count++;
        if (_largeBlobKey) count++;
        if (_hmacSecret is not null || _hmacSecretRequested) count++;
        if (_hmacSecretMc is not null) count++;
        if (_minPinLength) count++;
        if (_previewSignRegistration is not null || _previewSignAuthentication is not null) count++;
        return count;
    }
}