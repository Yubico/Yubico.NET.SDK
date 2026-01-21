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

/// <summary>
/// Large blob support level for makeCredential.
/// </summary>
public enum LargeBlobSupport
{
    /// <summary>
    /// Large blob support is preferred but not required.
    /// </summary>
    Preferred,
    
    /// <summary>
    /// Large blob support is required; credential creation fails without it.
    /// </summary>
    Required
}

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

/// <summary>
/// Output from the largeBlob extension during getAssertion.
/// </summary>
/// <remarks>
/// <para>
/// Contains the large blob key which can be used to encrypt/decrypt the large blob data
/// stored via authenticatorLargeBlobs.
/// </para>
/// </remarks>
public sealed class LargeBlobOutput
{
    /// <summary>
    /// Gets the large blob key.
    /// </summary>
    /// <remarks>
    /// A 32-byte key used to encrypt/decrypt large blob data for this credential.
    /// Derived from the credential's private key.
    /// </remarks>
    public ReadOnlyMemory<byte>? LargeBlobKey { get; init; }
    
    /// <summary>
    /// Gets the decrypted blob data (if read was requested and data exists).
    /// </summary>
    /// <remarks>
    /// Only present if the client requested "read" and successfully decrypted
    /// the large blob data. At CTAP level, only largeBlobKey is returned;
    /// the actual data retrieval is done separately.
    /// </remarks>
    public ReadOnlyMemory<byte>? Blob { get; init; }
    
    /// <summary>
    /// Gets whether the write operation was successful.
    /// </summary>
    /// <remarks>
    /// Only present if write was requested. Indicates whether the large blob
    /// was successfully stored.
    /// </remarks>
    public bool? Written { get; init; }
    
    /// <summary>
    /// Decodes largeBlob output from a CBOR reader.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The decoded output.</returns>
    public static LargeBlobOutput Decode(CborReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        
        ReadOnlyMemory<byte>? key = null;
        ReadOnlyMemory<byte>? blob = null;
        bool? written = null;
        
        var mapCount = reader.ReadStartMap() ?? 0;
        for (var i = 0; i < mapCount; i++)
        {
            var keyName = reader.ReadTextString();
            switch (keyName)
            {
                case "largeBlobKey":
                    key = reader.ReadByteString();
                    break;
                case "blob":
                    blob = reader.ReadByteString();
                    break;
                case "written":
                    written = reader.ReadBoolean();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        reader.ReadEndMap();
        
        return new LargeBlobOutput
        {
            LargeBlobKey = key,
            Blob = blob,
            Written = written
        };
    }
}
