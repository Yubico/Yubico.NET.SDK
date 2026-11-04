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

namespace Yubico.YubiKit.Ctap2.Extensions;

/// <summary>
/// Legacy convenience container for key, blob and write-result values.
/// </summary>
/// <remarks>
/// <para>
/// This decoder consumes a synthetic map with largeBlobKey, blob and written members; that combined
/// shape is not a standard CTAP extension output. The implemented largeBlobKey/storage route returns
/// keys on MakeCredentialResponse.LargeBlobKey or GetAssertionResponse.LargeBlobKey, outside authData
/// extensions. CTAP 2.3 §12.4's alternative direct largeBlob route uses unsigned supported/read/write
/// outputs and compressed blob plus originalSize, not this container or WebAuthn's decompressed result.
/// This DTO performs no storage read, decompression, decryption or authenticity verification.
/// Direct init borrows supplied memory; keep it stable during use. Decoded byte strings are
/// independent copies with no disposal/zeroing boundary.
/// </para>
/// </remarks>
public sealed class LargeBlobOutput
{
    /// <summary>
    /// Gets the large blob key.
    /// </summary>
    /// <remarks>
    /// A 32-byte key used to encrypt/decrypt large blob data for this credential.
    /// A key supplied to this container, not a guarantee about how an authenticator generated it.
    /// This is secret material, not public metadata; do not log or persist it unprotected.
    /// </remarks>
    public ReadOnlyMemory<byte>? LargeBlobKey { get; init; }

    /// <summary>
    /// Gets blob bytes supplied to this convenience container, without a decryption guarantee.
    /// </summary>
    /// <remarks>
    /// The container does not establish that these bytes were decrypted. Direct CTAP read output
    /// is compressed and also requires originalSize, which this DTO does not model. Use LargeBlobStorage
    /// for the implemented key/storage route; callers own plaintext returned by that helper.
    /// </remarks>
    public ReadOnlyMemory<byte>? Blob { get; init; }

    /// <summary>
    /// Gets the supplied written value, without independently establishing a completed storage write.
    /// </summary>
    /// <remarks>
    /// Null means no written value was supplied or decoded. True and false reflect the supplied
    /// boolean, not a tracked request or independently established storage success.
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