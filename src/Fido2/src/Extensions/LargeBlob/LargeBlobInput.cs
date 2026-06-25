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
/// Input for the largeBlob extension during makeCredential.
/// </summary>
/// <remarks>
/// <para>
/// The largeBlob extension enables credentials to be associated with large blob data.
/// During makeCredential, this input indicates whether the credential should support
/// large blob storage.
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#sctn-largeBlob-extension
/// </para>
/// </remarks>
public sealed class LargeBlobInput
{
    /// <summary>
    /// Gets or sets whether large blob support is required for this credential.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used during makeCredential:
    /// <list type="bullet">
    ///   <item><description>"support": "required" - Credential creation fails if large blob not supported</description></item>
    ///   <item><description>"support": "preferred" - Large blob support preferred but not required</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// At CTAP level, this is simplified to a single "support" key in the extension input.
    /// </para>
    /// </remarks>
    public LargeBlobSupport Support { get; init; } = LargeBlobSupport.Preferred;

    /// <summary>
    /// Encodes this largeBlob input as CBOR.
    /// </summary>
    /// <param name="writer">The CBOR writer.</param>
    public void Encode(CborWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        // largeBlob input for makeCredential: { "support": "required" | "preferred" }
        writer.WriteStartMap(1);
        writer.WriteTextString("support");
        writer.WriteTextString(Support == LargeBlobSupport.Required ? "required" : "preferred");
        writer.WriteEndMap();
    }

    /// <summary>
    /// Encodes this largeBlob input as a CBOR byte array.
    /// </summary>
    /// <returns>The CBOR-encoded input.</returns>
    public byte[] Encode()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        Encode(writer);
        return writer.Encode();
    }
}
