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

namespace Yubico.YubiKit.Core.SmartCard;

internal class ApduFormatterExtended(int maxApduSize) : IApduFormatter
{
    #region IApduFormatter Members

    public ReadOnlyMemory<byte> Format(ApduCommand apdu) =>
        Format(apdu.Cla, apdu.Ins, apdu.P1, apdu.P2, apdu.Data, apdu.Le);

    public ReadOnlyMemory<byte> Format(byte cla, byte ins, byte p1, byte p2, ReadOnlyMemory<byte> data, int le)
    {
        // ISO 7816-4 Extended APDU Formats:
        // Case 1 (no data, no Le): CLA INS P1 P2 (4 bytes)
        // Case 2E (no data, extended Le): CLA INS P1 P2 00 00 Le_hi Le_lo (7 bytes) -- simplified to Le_hi Le_lo for <=256
        // Case 3E (data, no Le): CLA INS P1 P2 00 Lc_hi Lc_lo [data] (7+ bytes)
        // Case 4E (data, extended Le): CLA INS P1 P2 00 Lc_hi Lc_lo [data] Le_hi Le_lo (9+ bytes)
        
        bool hasData = data.Length > 0;
        bool hasLe = le > 0;
        
        int totalLength;
        if (!hasData && !hasLe)
        {
            // Case 1: Just header
            totalLength = 4;
        }
        else if (!hasData && hasLe)
        {
            // Case 2: Header + short Le (use short format for simplicity when Le <= 256)
            totalLength = 5;
        }
        else if (hasData && !hasLe)
        {
            // Case 3E: Header + 00 + 2-byte Lc + data
            totalLength = 4 + 1 + 2 + data.Length;
        }
        else
        {
            // Case 4E: Header + 00 + 2-byte Lc + data + 2-byte Le
            totalLength = 4 + 1 + 2 + data.Length + 2;
        }
        
        if (totalLength > maxApduSize)
            throw new NotSupportedException("APDU length exceeds YubiKey capability.");

        Span<byte> buffer = stackalloc byte[totalLength];
        var position = 0;

        // Header
        buffer[0] = cla;
        buffer[1] = ins;
        buffer[2] = p1;
        buffer[3] = p2;
        position += 4;

        if (hasData)
        {
            // Extended Lc encoding: 00 Lc_hi Lc_lo
            buffer[position++] = 0x00;
            BinaryPrimitives.WriteInt16BigEndian(buffer[position..], (short)data.Length);
            position += 2;
            data.Span.CopyTo(buffer[position..]);
            position += data.Length;
        }

        if (hasLe)
        {
            if (hasData)
            {
                // Extended Le after data: 2 bytes
                BinaryPrimitives.WriteInt16BigEndian(buffer[position..], (short)le);
            }
            else
            {
                // Case 2 without data: use short Le (single byte) for compatibility
                // Le=0 means 256 in short APDU
                buffer[position] = (byte)(le == 256 ? 0 : le);
            }
        }

        return buffer.ToArray();
    }

    #endregion
}