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
/// Output from the credBlob extension during makeCredential.
/// </summary>
/// <remarks>
/// Indicates whether the blob was successfully stored.
/// </remarks>
public sealed class CredBlobMakeCredentialOutput
{
    /// <summary>
    /// Gets whether the credBlob was successfully stored.
    /// </summary>
    /// <remarks>
    /// True if the authenticator accepted and stored the blob.
    /// False if storage failed (e.g., blob too large).
    /// </remarks>
    public bool Stored { get; init; }

    /// <summary>
    /// Decodes credBlob output from a CBOR reader (makeCredential response).
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The decoded output.</returns>
    public static CredBlobMakeCredentialOutput Decode(CborReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        // Output is a boolean indicating success
        var stored = reader.ReadBoolean();

        return new CredBlobMakeCredentialOutput { Stored = stored };
    }
}
