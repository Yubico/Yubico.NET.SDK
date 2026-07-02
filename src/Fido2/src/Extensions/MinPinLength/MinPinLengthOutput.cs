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
/// Output from the minPinLength extension.
/// </summary>
/// <remarks>
/// Contains the minimum PIN length required by the authenticator.
/// </remarks>
public sealed class MinPinLengthOutput
{
    /// <summary>
    /// Gets the minimum PIN length in Unicode code points.
    /// </summary>
    /// <remarks>
    /// The minimum number of Unicode code points required for a PIN.
    /// Default minimum is 4, maximum is 63.
    /// </remarks>
    public int MinPinLength { get; init; }

    /// <summary>
    /// Decodes minPinLength output from a CBOR reader.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The decoded output.</returns>
    public static MinPinLengthOutput Decode(CborReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        // Output is an unsigned integer
        var minLength = reader.ReadInt32();

        return new MinPinLengthOutput { MinPinLength = minLength };
    }
}