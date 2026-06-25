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
///     Parsed extended capabilities (tag 0xC0) from the discretionary data objects.
/// </summary>
/// <remarks>
///     Wire format (10 bytes):
///     <code>
///     Byte 0:     Flags (ExtendedCapabilityFlags)
///     Byte 1:     SM algorithm
///     Bytes 2-3:  Challenge max length (big-endian uint16)
///     Bytes 4-5:  Certificate max length (big-endian uint16)
///     Bytes 6-7:  Special DO max length (big-endian uint16)
///     Byte 8:     PIN block 2 format supported (0x01 = true)
///     Byte 9:     MSE command supported (0x01 = true)
///     </code>
/// </remarks>
public sealed class ExtendedCapabilities
{
    /// <summary>
    ///     Capability flags indicating supported features.
    /// </summary>
    public ExtendedCapabilityFlags Flags { get; init; }

    /// <summary>
    ///     Secure Messaging algorithm identifier.
    /// </summary>
    public int SmAlgorithm { get; init; }

    /// <summary>
    ///     Maximum length for GET CHALLENGE responses.
    /// </summary>
    public int ChallengeMaxLength { get; init; }

    /// <summary>
    ///     Maximum length for certificate data objects.
    /// </summary>
    public int CertificateMaxLength { get; init; }

    /// <summary>
    ///     Maximum length for special data objects.
    /// </summary>
    public int SpecialDoMaxLength { get; init; }

    /// <summary>
    ///     Whether PIN block 2 format is supported.
    /// </summary>
    public bool PinBlock2Format { get; init; }

    /// <summary>
    ///     Whether the MSE (MANAGE SECURITY ENVIRONMENT) command is supported.
    /// </summary>
    public bool MseCommand { get; init; }

    /// <summary>
    ///     Parses extended capabilities from the encoded 10-byte data.
    /// </summary>
    public static ExtendedCapabilities Parse(ReadOnlySpan<byte> encoded)
    {
        if (encoded.Length < 10)
        {
            throw new ArgumentException("Extended capabilities must be at least 10 bytes.", nameof(encoded));
        }

        return new ExtendedCapabilities
        {
            Flags = (ExtendedCapabilityFlags)encoded[0],
            SmAlgorithm = encoded[1],
            ChallengeMaxLength = BinaryPrimitives.ReadUInt16BigEndian(encoded[2..4]),
            CertificateMaxLength = BinaryPrimitives.ReadUInt16BigEndian(encoded[4..6]),
            SpecialDoMaxLength = BinaryPrimitives.ReadUInt16BigEndian(encoded[6..8]),
            PinBlock2Format = encoded[8] == 1,
            MseCommand = encoded[9] == 1,
        };
    }
}