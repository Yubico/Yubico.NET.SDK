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

namespace Yubico.YubiKit.Ctap2.Cbor;

/// <summary>
/// Helper for converting between big-endian AAGUID bytes and .NET Guid.
/// </summary>
/// <remarks>
/// CTAP2/WebAuthn encode AAGUIDs in network byte order, independently of native machine byte order.
/// </remarks>
public static class AaguidConverter
{
    /// <summary>
    /// Converts a big-endian AAGUID byte array to a .NET Guid.
    /// </summary>
    /// <param name="bigEndianBytes">Exactly 16 big-endian AAGUID bytes; borrowed and left unchanged.</param>
    /// <returns>The corresponding Guid.</returns>
    public static Guid FromBigEndianBytes(ReadOnlySpan<byte> bigEndianBytes)
    {
        if (bigEndianBytes.Length != 16)
        {
            throw new ArgumentException("AAGUID must be exactly 16 bytes", nameof(bigEndianBytes));
        }

        return new Guid(bigEndianBytes, bigEndian: true);
    }

    /// <summary>
    /// Produces an AAGUID byte array from a .NET Guid.
    /// </summary>
    /// <param name="guid">The Guid to convert.</param>
    /// <returns>A newly allocated, caller-owned 16-byte array in network byte order.</returns>
    public static byte[] ToBigEndianBytes(Guid guid) => guid.ToByteArray(bigEndian: true);
}