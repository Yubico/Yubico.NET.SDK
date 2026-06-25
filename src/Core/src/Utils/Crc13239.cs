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

namespace Yubico.YubiKit.Core.Utils;

/// <summary>
///     Utility class for calculating and verifying the CRC13239 checksum used in YubiKey products.
/// </summary>
public static class Crc13239
{
    private const ushort InitialValue = 0xFFFF;
    private const ushort GeneratorPolynomial = 0x8408;

    /// <summary>
    ///     Calculates a CRC13239 checksum over a byte buffer.
    /// </summary>
    /// <param name="buffer">The buffer to be checksummed.</param>
    /// <returns>A two byte CRC checksum.</returns>
    public static short Calculate(ReadOnlySpan<byte> buffer)
    {
        var remainderPolynomial = InitialValue;

        foreach (var currentByte in buffer)
        {
            remainderPolynomial ^= currentByte;
            for (var bitCounter = 0; bitCounter < 8; bitCounter++)
            {
                var leastSignificantBit = (byte)(remainderPolynomial & 1);
                remainderPolynomial >>= 1;

                if (leastSignificantBit != 0) remainderPolynomial ^= GeneratorPolynomial;
            }
        }

        return unchecked((short)remainderPolynomial);
    }
}