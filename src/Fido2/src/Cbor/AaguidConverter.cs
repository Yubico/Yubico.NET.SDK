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

namespace Yubico.YubiKit.Fido2.Cbor;

/// <summary>
/// Helper for converting between big-endian AAGUID bytes and .NET Guid.
/// </summary>
/// <remarks>
/// FIDO2/WebAuthn store AAGUIDs in big-endian (network byte order) format,
/// while .NET Guid uses mixed-endian representation. This helper handles the conversion.
/// </remarks>
public static class AaguidConverter
{
    /// <summary>
    /// Converts a big-endian AAGUID byte array to a .NET Guid.
    /// </summary>
    /// <param name="bigEndianBytes">The 16-byte big-endian AAGUID.</param>
    /// <returns>The corresponding Guid.</returns>
    public static Guid FromBigEndianBytes(ReadOnlySpan<byte> bigEndianBytes)
    {
        if (bigEndianBytes.Length != 16)
        {
            throw new ArgumentException("AAGUID must be exactly 16 bytes", nameof(bigEndianBytes));
        }

        // AAGUID is stored in big-endian (network byte order) format
        // .NET Guid constructor expects mixed-endian (first 3 components little-endian on little-endian systems)
        Span<byte> guidBytes = stackalloc byte[16];
        bigEndianBytes.CopyTo(guidBytes);

        // Convert from big-endian to little-endian for first 3 components on little-endian systems
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
    /// Converts a .NET Guid to a big-endian AAGUID byte array.
    /// </summary>
    /// <param name="guid">The Guid to convert.</param>
    /// <returns>A 16-byte array in big-endian format.</returns>
    public static byte[] ToBigEndianBytes(Guid guid)
    {
        // .NET Guid.ToByteArray() produces mixed endianness on little-endian systems:
        // - First 4 bytes (time_low): little-endian
        // - Next 2 bytes (time_mid): little-endian
        // - Next 2 bytes (time_hi_and_version): little-endian
        // - Last 8 bytes (clock_seq_and_node): big-endian
        // WebAuthn/FIDO2 AAGUID is fully big-endian, so we reverse the first three components
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);

        if (BitConverter.IsLittleEndian)
        {
            // Reverse first 3 components to convert to big-endian
            bytes[0..4].Reverse();  // time_low
            bytes[4..6].Reverse();  // time_mid
            bytes[6..8].Reverse();  // time_hi_and_version
            // bytes[8..16] already big-endian
        }

        return bytes.ToArray();
    }
}