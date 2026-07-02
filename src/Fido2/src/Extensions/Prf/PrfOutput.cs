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
/// Output from the PRF extension.
/// </summary>
/// <remarks>
/// Contains the derived secrets from the PRF evaluation.
/// </remarks>
public sealed class PrfOutput
{
    /// <summary>
    /// Gets whether the authenticator supports PRF.
    /// </summary>
    /// <remarks>
    /// During makeCredential registration, this indicates PRF capability.
    /// </remarks>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets the first derived output.
    /// </summary>
    /// <remarks>
    /// 32-byte secret derived from the first PRF input.
    /// </remarks>
    public ReadOnlyMemory<byte>? First { get; init; }

    /// <summary>
    /// Gets the second derived output.
    /// </summary>
    /// <remarks>
    /// 32-byte secret derived from the second PRF input (if provided).
    /// </remarks>
    public ReadOnlyMemory<byte>? Second { get; init; }

    /// <summary>
    /// Decodes PRF output from decrypted hmac-secret outputs.
    /// </summary>
    /// <param name="decryptedOutput">The decrypted output from hmac-secret.</param>
    /// <param name="hasTwoOutputs">Whether two outputs were requested.</param>
    /// <returns>The decoded PRF output.</returns>
    public static PrfOutput FromHmacSecretOutput(
        ReadOnlySpan<byte> decryptedOutput,
        bool hasTwoOutputs = false)
    {
        if (decryptedOutput.Length < 32)
        {
            throw new ArgumentException(
                "Decrypted output must be at least 32 bytes.",
                nameof(decryptedOutput));
        }

        var first = decryptedOutput[..32].ToArray();
        byte[]? second = null;

        if (hasTwoOutputs && decryptedOutput.Length >= 64)
        {
            second = decryptedOutput[32..64].ToArray();
        }

        return new PrfOutput
        {
            Enabled = true,
            First = first,
            Second = second
        };
    }

    /// <summary>
    /// Decodes PRF output from a CBOR reader (authentication response).
    /// </summary>
    /// <param name="reader">The CBOR reader positioned at the PRF output map.</param>
    /// <returns>The decoded PRF output, or null if the output is malformed.</returns>
    public static PrfOutput? Decode(CborReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var mapLength = reader.ReadStartMap();
        if (mapLength is null or 0)
        {
            return null;
        }

        ReadOnlyMemory<byte>? first = null;
        ReadOnlyMemory<byte>? second = null;

        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadTextString();
            if (key == "first")
            {
                first = reader.ReadByteString();
            }
            else if (key == "second")
            {
                second = reader.ReadByteString();
            }
            else
            {
                reader.SkipValue();
            }
        }

        reader.ReadEndMap();

        if (!first.HasValue)
        {
            return null;
        }

        return new PrfOutput
        {
            Enabled = true,
            First = first.Value,
            Second = second
        };
    }
}