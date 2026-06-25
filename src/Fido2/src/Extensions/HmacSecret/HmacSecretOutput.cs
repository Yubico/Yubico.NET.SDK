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
/// Output from the hmac-secret extension during getAssertion.
/// </summary>
/// <remarks>
/// <para>
/// Contains the encrypted derived secret(s). The client must decrypt using
/// the shared secret established during key agreement.
/// </para>
/// </remarks>
public sealed class HmacSecretOutput
{
    /// <summary>
    /// Gets the encrypted output value(s).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contains one or two 32-byte derived secrets, encrypted:
    /// <list type="bullet">
    ///   <item><description>For one salt: encrypt(sharedSecret, output1) = 32+ bytes</description></item>
    ///   <item><description>For two salts: encrypt(sharedSecret, output1 || output2) = 64+ bytes</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For PIN protocol 2, includes the IV prefix (16 bytes).
    /// </para>
    /// </remarks>
    public required ReadOnlyMemory<byte> Output { get; init; }

    /// <summary>
    /// Decodes hmac-secret output from CBOR bytes.
    /// </summary>
    /// <param name="data">The CBOR-encoded output.</param>
    /// <returns>The decoded output.</returns>
    public static HmacSecretOutput Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Lax);
        return Decode(reader);
    }

    /// <summary>
    /// Decodes hmac-secret output from a CBOR reader.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The decoded output.</returns>
    public static HmacSecretOutput Decode(CborReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        // The output is just the encrypted byte string
        var output = reader.ReadByteString();

        return new HmacSecretOutput { Output = output };
    }
}
