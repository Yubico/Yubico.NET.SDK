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
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

internal class ApduFormatterExtended(int maxApduSize) : IApduFormatter
{

    public Memory<byte> Format(ApduCommand apdu) =>
        Format(apdu.Cla, apdu.Ins, apdu.P1, apdu.P2, apdu.Data, apdu.Le);

    public Memory<byte> Format(byte cla, byte ins, byte p1, byte p2, ReadOnlyMemory<byte> data, int le)
    {
        if (le is < 0 or > 65536)
            throw new ArgumentException("Le must be between 0 and 65536", nameof(le));

        // ISO 7816-4 extended APDU formats used here always include Le:
        // Case 2E (no data): CLA INS P1 P2 00 Le_hi Le_lo (7 bytes)
        // Case 4E (data): CLA INS P1 P2 00 Lc_hi Lc_lo [data] Le_hi Le_lo (9+ bytes)

        bool hasData = data.Length > 0;
        var totalLength = 4 + (hasData ? 1 + 2 + data.Length : 1) + 2;

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
            BinaryPrimitives.WriteUInt16BigEndian(buffer[position..], (ushort)data.Length);
            position += 2;
            data.Span.CopyTo(buffer[position..]);
            position += data.Length;
        }

        if (!hasData)
        {
            buffer[position++] = 0x00;
        }

        var actualLe = le == 65536 ? 0 : le;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[position..], (ushort)actualLe);

        return buffer.ToArray();
    }

}
