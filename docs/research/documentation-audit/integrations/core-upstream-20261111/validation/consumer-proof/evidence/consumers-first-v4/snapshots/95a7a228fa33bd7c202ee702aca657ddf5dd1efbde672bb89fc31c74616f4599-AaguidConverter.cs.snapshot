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
/// CTAP2/WebAuthn encode AAGUIDs in network byte order. The default Guid byte APIs instead use
/// little-endian order for the first 4-byte, 2-byte and 2-byte components, leaving the last eight
/// bytes unchanged. This serialization convention is independent of native machine byte order.
/// This implementation applies the required component reversal only on little-endian hosts;
/// on a big-endian host it leaves those components in the wrong byte convention.
/// </remarks>
public static class AaguidConverter
{
    /// <summary>
    /// Converts a big-endian AAGUID byte array to a .NET Guid.
    /// </summary>
    /// <param name="bigEndianBytes">Exactly 16 big-endian AAGUID bytes; borrowed, copied and left unchanged.</param>
    /// <returns>The corresponding Guid.</returns>
    public static Guid FromBigEndianBytes(ReadOnlySpan<byte> bigEndianBytes)
    {
        if (bigEndianBytes.Length != 16)
        {
            throw new ArgumentException("AAGUID must be exactly 16 bytes", nameof(bigEndianBytes));
        }

        // The default Guid span constructor expects the first 4/2/2 component groups in little-endian
        // byte order, regardless of native machine order; AAGUID input uses network order.
        Span<byte> guidBytes = stackalloc byte[16];
        bigEndianBytes.CopyTo(guidBytes);

        // Required byte-convention reversal; this implementation applies it only on little-endian hosts.
        if (BitConverter.IsLittleEndian)
        {
            // Reverse bytes for Data1 (4 bytes)
            (guidBytes[0], guidBytes[1], guidBytes[2], guidBytes[3]) =
                (guidBytes[3], guidBytes[2], guidBytes[1], guidBytes[0]);

            // Reverse bytes for Data2 (2 bytes)
            (guidBytes[4], guidBytes[5]) = (guidBytes[5], guidBytes[4]);

            // Reverse bytes for Data3 (2 bytes)
            (guidBytes[6], guidBytes[7]) = (guidBytes[7], guidBytes[6]);
        }

        return new Guid(guidBytes);
    }

    /// <summary>
    /// Produces an AAGUID byte array from a .NET Guid.
    /// </summary>
    /// <param name="guid">The Guid to convert.</param>
    /// <returns>A newly allocated, caller-owned 16-byte array; network-order conversion has the
    /// host limitation described in the class remarks.</returns>
    public static byte[] ToBigEndianBytes(Guid guid)
    {
        // Default TryWriteBytes uses little-endian first 4/2/2 component groups and unchanged last8.
        // This is a serialization convention, not Guid's native in-memory layout.
        // Network-order output requires reversal of the first three groups.
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);

        if (BitConverter.IsLittleEndian)
        {
            // The required reversal is currently applied only on little-endian hosts.
            bytes[0..4].Reverse();  // time_low
            bytes[4..6].Reverse();  // time_mid
            bytes[6..8].Reverse();  // time_hi_and_version
            // The last eight bytes have the same order in both byte conventions.
        }

        return bytes.ToArray();
    }
}