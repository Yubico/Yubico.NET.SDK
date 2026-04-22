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

using System.Collections.ObjectModel;
using System.Formats.Cbor;
using Yubico.YubiKit.Fido2.Credentials;

namespace Yubico.YubiKit.WebAuthn;

/// <summary>
/// WebAuthn authenticator data wrapper.
/// </summary>
/// <remarks>
/// Wraps the Fido2-level AuthenticatorData and adds parsed extension outputs.
/// </remarks>
public sealed class WebAuthnAuthenticatorData
{
    private readonly AuthenticatorData _inner;

    /// <summary>
    /// Gets the SHA-256 hash of the RP ID.
    /// </summary>
    public ReadOnlyMemory<byte> RpIdHash => _inner.RpIdHash;

    /// <summary>
    /// Gets whether user presence was verified.
    /// </summary>
    public bool UserPresent => _inner.UserPresent;

    /// <summary>
    /// Gets whether user verification was performed.
    /// </summary>
    public bool UserVerified => _inner.UserVerified;

    /// <summary>
    /// Gets the signature counter.
    /// </summary>
    public uint SignCount => _inner.SignCount;

    /// <summary>
    /// Gets the attested credential data, if present.
    /// </summary>
    public AttestedCredentialData? AttestedCredentialData => _inner.AttestedCredentialData;

    /// <summary>
    /// Gets the raw authenticator data bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Raw => _inner.RawData;

    /// <summary>
    /// Gets the parsed extensions map (extension identifier → raw CBOR value).
    /// </summary>
    public IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ParsedExtensions { get; }

    private WebAuthnAuthenticatorData(
        AuthenticatorData inner,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> parsedExtensions)
    {
        _inner = inner;
        ParsedExtensions = parsedExtensions;
    }

    /// <summary>
    /// Decodes authenticator data from raw bytes.
    /// </summary>
    /// <param name="rawAuthData">The raw authenticator data bytes.</param>
    /// <returns>The decoded authenticator data with parsed extensions.</returns>
    public static WebAuthnAuthenticatorData Decode(ReadOnlyMemory<byte> rawAuthData)
    {
        var inner = AuthenticatorData.Parse(rawAuthData);

        var parsedExtensions = ParseExtensions(inner.Extensions);

        return new WebAuthnAuthenticatorData(inner, parsedExtensions);
    }

    /// <summary>
    /// Parses the extensions CBOR map into identifier → raw CBOR slice pairs.
    /// </summary>
    private static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ParseExtensions(
        ReadOnlyMemory<byte>? extensionsCbor)
    {
        if (!extensionsCbor.HasValue || extensionsCbor.Value.IsEmpty)
        {
            return new ReadOnlyDictionary<string, ReadOnlyMemory<byte>>(
                new Dictionary<string, ReadOnlyMemory<byte>>());
        }

        var result = new Dictionary<string, ReadOnlyMemory<byte>>();

        var reader = new CborReader(extensionsCbor.Value, CborConformanceMode.Lax);
        var mapLength = reader.ReadStartMap();

        for (var i = 0; i < mapLength; i++)
        {
            var identifier = reader.ReadTextString();

            // Capture the raw CBOR value for this extension
            var bytesRemainingBefore = reader.BytesRemaining;
            reader.SkipValue();
            var bytesConsumed = bytesRemainingBefore - reader.BytesRemaining;
            var offset = extensionsCbor.Value.Length - bytesRemainingBefore;
            var rawValue = extensionsCbor.Value.Slice(offset, bytesConsumed);

            result[identifier] = rawValue;
        }

        reader.ReadEndMap();

        return new ReadOnlyDictionary<string, ReadOnlyMemory<byte>>(result);
    }
}
