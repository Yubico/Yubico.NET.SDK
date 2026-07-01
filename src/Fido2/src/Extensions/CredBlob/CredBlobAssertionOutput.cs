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
/// Output from the credBlob extension during getAssertion.
/// </summary>
/// <remarks>
/// Contains the blob data stored with the credential, if any.
/// </remarks>
public sealed class CredBlobAssertionOutput
{
    /// <summary>
    /// Gets the stored blob data.
    /// </summary>
    /// <remarks>
    /// The blob data that was stored during makeCredential.
    /// Empty if no blob was stored.
    /// </remarks>
    public ReadOnlyMemory<byte> Blob { get; init; }

    /// <summary>
    /// Decodes credBlob output from a CBOR reader (getAssertion response).
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The decoded output.</returns>
    public static CredBlobAssertionOutput Decode(CborReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        // Output is the byte string blob
        var blob = reader.ReadByteString();

        return new CredBlobAssertionOutput { Blob = blob };
    }
}