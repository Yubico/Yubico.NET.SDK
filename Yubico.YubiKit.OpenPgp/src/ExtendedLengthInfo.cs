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
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Extended length information (DO 7F66) indicating the maximum command and response APDU sizes.
/// </summary>
/// <remarks>
///     Wire format: Two nested TLV objects with tag 0x02, each containing a big-endian uint16.
/// </remarks>
public sealed class ExtendedLengthInfo
{
    /// <summary>
    ///     Maximum number of bytes in a command APDU.
    /// </summary>
    public int RequestMaxBytes { get; init; }

    /// <summary>
    ///     Maximum number of bytes in a response APDU.
    /// </summary>
    public int ResponseMaxBytes { get; init; }

    /// <summary>
    ///     Parses extended length info from the encoded TLV data.
    /// </summary>
    public static ExtendedLengthInfo Parse(ReadOnlySpan<byte> encoded)
    {
        using var tlvs = TlvHelper.DecodeList(encoded);

        var requestMax = 0;
        var responseMax = 0;
        var index = 0;

        foreach (var tlv in tlvs)
        {
            if (tlv.Tag == 0x02)
            {
                var value = BinaryPrimitives.ReadUInt16BigEndian(tlv.Value.Span);
                if (index == 0)
                {
                    requestMax = value;
                }
                else
                {
                    responseMax = value;
                }

                index++;
            }
        }

        return new ExtendedLengthInfo
        {
            RequestMaxBytes = requestMax,
            ResponseMaxBytes = responseMax,
        };
    }
}
