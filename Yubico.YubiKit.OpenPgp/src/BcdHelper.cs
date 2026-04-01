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

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Binary-Coded Decimal (BCD) conversion utilities used by the OpenPGP applet.
/// </summary>
internal static class BcdHelper
{
    /// <summary>
    ///     Decodes a single BCD-encoded byte to its decimal value.
    ///     For example, 0x57 → 57.
    /// </summary>
    internal static int DecodeByte(byte bcdByte) =>
        10 * (bcdByte >> 4) + (bcdByte & 0x0F);

    /// <summary>
    ///     Attempts to decode a span of BCD-encoded bytes to a decimal integer.
    ///     Each byte is treated as two BCD digits. For example, [0x12, 0x34] → 1234.
    /// </summary>
    /// <returns>
    ///     <c>true</c> if all bytes are valid BCD (digits 0-9 only);
    ///     <c>false</c> if any nibble contains A-F.
    /// </returns>
    internal static bool TryDecodeSerial(ReadOnlySpan<byte> bcdBytes, out int result)
    {
        result = 0;
        foreach (var b in bcdBytes)
        {
            var high = (b >> 4) & 0x0F;
            var low = b & 0x0F;

            if (high > 9 || low > 9)
            {
                result = 0;
                return false;
            }

            result = result * 100 + high * 10 + low;
        }

        return true;
    }
}
