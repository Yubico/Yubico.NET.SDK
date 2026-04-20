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

using System.Buffers.Binary;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Parsed OpenPGP Application Identifier (AID) containing version, manufacturer, and serial number.
/// </summary>
/// <remarks>
///     The AID structure (minimum 14 bytes):
///     <code>
///     Bytes 0-5:   RID + PIX prefix
///     Bytes 6-7:   OpenPGP version (BCD encoded)
///     Bytes 8-9:   Manufacturer ID (big-endian uint16)
///     Bytes 10-13: Serial number (BCD encoded, or raw uint32 if invalid BCD)
///     </code>
/// </remarks>
public sealed class OpenPgpAid
{
    private readonly byte[] _raw;

    private OpenPgpAid(byte[] raw)
    {
        _raw = raw;
    }

    /// <summary>
    ///     The raw AID bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Raw => _raw;

    /// <summary>
    ///     The OpenPGP version as a tuple of (major, minor), decoded from BCD.
    /// </summary>
    public (int Major, int Minor) Version { get; private init; }

    /// <summary>
    ///     The 16-bit manufacturer identifier. Yubico devices use 6.
    /// </summary>
    public int Manufacturer { get; private init; }

    /// <summary>
    ///     The device serial number, decoded from BCD.
    ///     If the serial number bytes contain invalid BCD (hex A-F), the raw 4-byte value
    ///     is returned as a negative number.
    /// </summary>
    public int Serial { get; private init; }

    /// <summary>
    ///     Parses an OpenPGP AID from the raw byte representation.
    /// </summary>
    /// <param name="aidBytes">The raw AID bytes (minimum 14 bytes).</param>
    /// <exception cref="ArgumentException">Thrown when the AID is too short.</exception>
    public static OpenPgpAid Parse(ReadOnlySpan<byte> aidBytes)
    {
        if (aidBytes.Length < 14)
        {
            throw new ArgumentException("AID must be at least 14 bytes.", nameof(aidBytes));
        }

        var versionMajor = BcdHelper.DecodeByte(aidBytes[6]);
        var versionMinor = BcdHelper.DecodeByte(aidBytes[7]);
        var manufacturer = BinaryPrimitives.ReadUInt16BigEndian(aidBytes[8..10]);

        int serial;
        if (!BcdHelper.TryDecodeSerial(aidBytes[10..14], out serial))
        {
            // Invalid BCD: treat as raw uint32 and negate
            serial = -(int)BinaryPrimitives.ReadUInt32BigEndian(aidBytes[10..14]);
        }

        return new OpenPgpAid(aidBytes.ToArray())
        {
            Version = (versionMajor, versionMinor),
            Manufacturer = manufacturer,
            Serial = serial,
        };
    }
}