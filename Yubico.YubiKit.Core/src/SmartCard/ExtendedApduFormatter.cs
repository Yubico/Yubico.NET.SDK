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

internal class ExtendedApduFormatter(int maxApduSize) : IApduFormatter
{
    #region IApduFormatter Members

    public ReadOnlyMemory<byte> Format(ApduCommand apdu) =>
        Format(apdu.Cla, apdu.Ins, apdu.P1, apdu.P2, apdu.Data, apdu.Le); // TODO bug? LE send separately?

    public ReadOnlyMemory<byte> Format(byte cla, byte ins, byte p1, byte p2, ReadOnlyMemory<byte> data, int le)
    {
        var totalLength = 5 + (data.Length > 0 ? 2 + data.Length : 0) + (le > 0 ? 2 : 0);
        if (totalLength > maxApduSize)
            throw new NotSupportedException("APDU length exceeds YubiKey capability.");

        Span<byte> buffer = stackalloc byte[totalLength];
        var position = 0;

        buffer[0] = cla;
        buffer[1] = ins;
        buffer[2] = p1;
        buffer[3] = p2;
        buffer[4] = 0x00;
        position += 5;

        if (data.Length > 0)
        {
            BinaryPrimitives.WriteInt16BigEndian(buffer[position..], (short)data.Length);
            position += 2;
            data.Span.CopyTo(buffer[position..]);
            position += data.Length;
        }

        if (le > 0)
            BinaryPrimitives.WriteInt16BigEndian(buffer[position..], (short)le);

        return buffer.ToArray(); // TODO allocation. Can it be avoided?
    }

    #endregion
}