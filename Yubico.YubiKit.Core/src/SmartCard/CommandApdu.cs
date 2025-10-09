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

using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Core.SmartCard;

/// <summary>
///     Represents an ISO 7816 application command
/// </summary>
public readonly record struct CommandApdu
{
    public CommandApdu()
    {
    }

    public CommandApdu(byte cla, byte ins, byte p1, byte p2, ReadOnlyMemory<byte>? data = null, int le = 0)
    {
        Cla = cla;
        Ins = ins;
        P1 = p1;
        P2 = p2;
        Le = le;
        Data = data?.ToArray() ?? ReadOnlyMemory<byte>.Empty;
    }

    public CommandApdu(int cla, int ins, int p1, int p2, ReadOnlyMemory<byte>? data = null, int le = 0)
    : this(
        ByteUtils.ValidateByte(cla, nameof(cla)),
        ByteUtils.ValidateByte(ins, nameof(ins)),
        ByteUtils.ValidateByte(p1, nameof(p1)),
        ByteUtils.ValidateByte(p2, nameof(p2)),
        data,
        le)
    {
    }

    public byte Cla { get; init; }
    public byte Ins { get; init; }
    public byte P1 { get; init; }
    public byte P2 { get; init; }
    public int Le { get; init; }

    /// <summary>
    ///     Gets or sets the optional command data payload.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; init; }


    /// <summary>
    ///     Prints CLA, INS, P1, P2, Lc, Le, and the length of the Data field in a formatted string.
    /// </summary>
    public override string ToString() =>
        $"CLA: 0x{Cla:X2} INS: 0x{Ins:X2} P1: 0x{P1:X2} P2: 0x{P2:X2} Le: {Le} Data: {Data.Length} bytes";
}