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

namespace Yubico.YubiKit.Core.SmartCard;

internal class ApduFormatterShort : IApduFormatter
{
    private const int ShortApduMaxChunk = SmartCardMaxApduSizes.ShortApduMaxChunkSize;

    #region IApduFormatter Members

    public ReadOnlyMemory<byte> Format(byte cla, byte ins, byte p1, byte p2, ReadOnlyMemory<byte> data,
        int le)
    {
        var length = data.Length;
        if (length > ShortApduMaxChunk)
            throw new InvalidOperationException($"Length must be no greater than {ShortApduMaxChunk}");

        if (le is < 0 or > ShortApduMaxChunk)
            throw new ArgumentException($"Le must be between 0 and {ShortApduMaxChunk}", nameof(le));

        var totalLength = 4 + (length > 0 ? 1 + length : 0) + (le > 0 ? 1 : 0) + (length == 0 && le == 0 ? 1 : 0);
        Span<byte> buffer = stackalloc byte[totalLength];
        var position = 0;

        buffer[0] = cla;
        buffer[1] = ins;
        buffer[2] = p1;
        buffer[3] = p2;
        position += 4;

        if (length > 0)
        {
            buffer[position] = (byte)length;
            position += 1;
            data.Span[..length].CopyTo(buffer[position..]);
            position += length;
        }

        if (le > 0)
            buffer[position] = (byte)le;
        else if (length == 0)
            buffer[position] = 0;

        return buffer.ToArray(); // TODO allocation. Can it be avoided?
    }

    public ReadOnlyMemory<byte> Format(ApduCommand apdu) =>
        Format(apdu.Cla, apdu.Ins, apdu.P1, apdu.P2, apdu.Data, apdu.Le);

    #endregion
}