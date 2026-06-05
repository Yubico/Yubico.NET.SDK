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
/// Input for the credBlob extension during makeCredential.
/// </summary>
/// <remarks>
/// <para>
/// The credBlob extension allows storing a small blob of data with a credential.
/// The maximum size is determined by the authenticator's maxCredBlobLength (up to 32 bytes minimum).
/// </para>
/// <para>
/// This is useful for storing small amounts of credential-specific data like:
/// <list type="bullet">
///   <item><description>Account identifiers</description></item>
///   <item><description>Configuration flags</description></item>
///   <item><description>Small metadata</description></item>
/// </list>
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#sctn-credBlob-extension
/// </para>
/// </remarks>
public sealed class CredBlobInput
{
    private ReadOnlyMemory<byte> _blob;

    /// <summary>
    /// Gets or sets the blob data to store with the credential.
    /// </summary>
    /// <remarks>
    /// Must be between 1 and 32 bytes per CTAP2.1.
    /// </remarks>
    public required ReadOnlyMemory<byte> Blob
    {
        get => _blob;
        init
        {
            if (value.Length is < 1 or > 32)
            {
                throw new ArgumentException("CredBlob must be between 1 and 32 bytes", nameof(Blob));
            }
            _blob = value;
        }
    }

    /// <summary>
    /// Encodes this credBlob input as CBOR (the raw blob bytes).
    /// </summary>
    /// <param name="writer">The CBOR writer.</param>
    public void Encode(CborWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        // credBlob input is just the byte string
        writer.WriteByteString(Blob.Span);
    }

    /// <summary>
    /// Encodes this credBlob input as a CBOR byte array.
    /// </summary>
    /// <returns>The CBOR-encoded input.</returns>
    public byte[] Encode()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        Encode(writer);
        return writer.Encode();
    }
}
