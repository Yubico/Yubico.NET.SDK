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
///     Represents an ISO 7816 application command.
/// </summary>
/// <remarks>
///     <para>
///     <b>Ownership and zeroing:</b> <see cref="ApduCommand"/> stores a reference to the caller-provided
///     <see cref="Data"/> buffer — it does <b>not</b> clone it. When the payload carries sensitive material
///     such as PIN bytes, key material, or cryptograms, the <b>caller</b> is responsible for zeroing the
///     source buffer after the command has been transmitted.
///     </para>
///     <code>
///     // ✅ Correct — caller zeroes the source buffer after transmission
///     var command = new ApduCommand(0x00, InsVerify, 0x00, 0x80, pinnedPin.AsMemory(0, 8));
///     await protocol.TransmitAndReceiveAsync(command, ct);
///     CryptographicOperations.ZeroMemory(pinnedPin); // zeroes the buffer command.Data referenced
///
///     // ✅ Non-sensitive commands need no special handling
///     var command = new ApduCommand(0x00, InsSelect, 0x04, 0x00, appId);
///     var response = await protocol.TransmitAndReceiveAsync(command, ct);
///     </code>
/// </remarks>
public readonly record struct ApduCommand
{
    /// <summary>Initializes a new <see cref="ApduCommand"/> with explicit header bytes and optional data payload.</summary>
    public ApduCommand(int cla, int ins, int p1, int p2, ReadOnlyMemory<byte> data = default, int le = 0)
    {
        Cla = ByteUtils.ValidateByte(cla, nameof(cla));
        Ins = ByteUtils.ValidateByte(ins, nameof(ins));
        P1  = ByteUtils.ValidateByte(p1,  nameof(p1));
        P2  = ByteUtils.ValidateByte(p2,  nameof(p2));
        Data = data;
        Le   = le;
    }

    /// <summary>Gets the CLA byte.</summary>
    public byte Cla { get; init; }

    /// <summary>Gets the INS byte.</summary>
    public byte Ins { get; init; }

    /// <summary>Gets the P1 byte.</summary>
    public byte P1 { get; init; }

    /// <summary>Gets the P2 byte.</summary>
    public byte P2 { get; init; }

    /// <summary>Gets the Le value.</summary>
    public int Le { get; init; }

    /// <summary>
    ///     Gets the optional command data payload.
    /// </summary>
    /// <remarks>
    ///     This is a direct reference to the caller-provided memory. The caller is responsible
    ///     for zeroing the underlying buffer after transmission if it contains sensitive material.
    /// </remarks>
    public ReadOnlyMemory<byte> Data { get; init; }

    /// <inheritdoc />
    public override string ToString() =>
        $"CLA: 0x{Cla:X2} INS: 0x{Ins:X2} P1: 0x{P1:X2} P2: 0x{P2:X2} Le: {Le} Data: {Data.Length} bytes";

    // NOTE: record struct auto-generates Equals/GetHashCode over all properties.
    // ReadOnlyMemory<byte> uses reference equality (pointer + offset + length), not content equality.
    // Do not use ApduCommand in Dictionary, HashSet, or == comparisons that expect content-based equality.
}
