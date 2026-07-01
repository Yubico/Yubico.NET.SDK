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
/// Input for the largeBlob extension during getAssertion.
/// </summary>
/// <remarks>
/// <para>
/// During getAssertion, the largeBlob extension can be used to read or write
/// the large blob associated with a credential.
/// </para>
/// </remarks>
public sealed class LargeBlobAssertionInput
{
    /// <summary>
    /// Gets or sets whether to read the large blob during assertion.
    /// </summary>
    /// <remarks>
    /// If true, the authenticator returns the largeBlobKey in the assertion response.
    /// The client then uses this key to decrypt the large blob data.
    /// </remarks>
    public bool Read { get; init; }

    /// <summary>
    /// Gets or sets the data to write to the large blob.
    /// </summary>
    /// <remarks>
    /// If provided, the authenticator will return the largeBlobKey which the client
    /// uses to encrypt and store the data via authenticatorLargeBlobs.
    /// </remarks>
    public ReadOnlyMemory<byte>? Write { get; init; }

    /// <summary>
    /// Encodes this largeBlob assertion input as CBOR.
    /// </summary>
    /// <param name="writer">The CBOR writer.</param>
    public void Encode(CborWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var mapSize = 0;
        if (Read) mapSize++;
        if (Write.HasValue) mapSize++;

        if (mapSize == 0)
        {
            throw new InvalidOperationException(
                "LargeBlobAssertionInput must have either Read=true or Write data.");
        }

        writer.WriteStartMap(mapSize);

        // Keys must be sorted for canonical CBOR: "read" < "write"
        if (Read)
        {
            writer.WriteTextString("read");
            writer.WriteBoolean(true);
        }

        if (Write.HasValue)
        {
            writer.WriteTextString("write");
            writer.WriteByteString(Write.Value.Span);
        }

        writer.WriteEndMap();
    }

    /// <summary>
    /// Encodes this input as a CBOR byte array.
    /// </summary>
    /// <returns>The CBOR-encoded input.</returns>
    public byte[] Encode()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        Encode(writer);
        return writer.Encode();
    }
}