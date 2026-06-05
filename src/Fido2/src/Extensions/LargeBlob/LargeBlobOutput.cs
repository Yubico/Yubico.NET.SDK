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
