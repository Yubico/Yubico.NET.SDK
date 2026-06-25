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

namespace Yubico.YubiKit.Core.Hid.Otp;

/// <summary>
/// Utility methods for calculating and verifying the CRC13239 checksum used by YubiKeys
/// in the OTP HID protocol.
/// </summary>
public static class ChecksumUtils
{
    /// <summary>
    /// When verifying a checksum, the CRC_OK_RESIDUAL should be the remainder.
    /// </summary>
    public const ushort ValidResidue = 0xF0B8;

    /// <summary>
    /// CRC13239 polynomial used by YubiKey.
    /// </summary>
    private const ushort CrcPolynomial = 0x8408;

    /// <summary>
    /// Calculate the CRC13239 checksum for a byte buffer.
    /// </summary>
    /// <param name="data">The data to checksum.</param>
    /// <param name="length">How much of the buffer should be checksummed.</param>
    /// <returns>The calculated checksum.</returns>
    public static ushort CalculateCrc(ReadOnlySpan<byte> data, int length)
    {
        var crc = 0xFFFF;

        for (var index = 0; index < length; index++)
        {
            crc ^= data[index];
            for (var i = 0; i < 8; i++)
            {
                var j = crc & 1;
                crc >>= 1;
                if (j == 1)
                {
                    crc ^= CrcPolynomial;
                }
            }
        }

        return (ushort)(crc & 0xFFFF);
    }

    /// <summary>
    /// Verifies a checksum.
    /// </summary>
    /// <param name="data">The data, ending in the 2-byte CRC checksum to verify.</param>
    /// <param name="length">The length of the data, including the checksum at the end.</param>
    /// <returns>True if the checksum is correct, false otherwise.</returns>
    public static bool CheckCrc(ReadOnlySpan<byte> data, int length) =>
        CalculateCrc(data, length) == ValidResidue;
}
