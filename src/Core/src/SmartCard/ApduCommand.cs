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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Core.SmartCard;

/// <summary>
///     Represents an ISO 7816 application command.
/// </summary>
/// <remarks>
///     <para>
///     <b>Ownership and zeroing:</b> <see cref="ApduCommand"/> copies caller-provided data
///     internally via <c>data.ToArray()</c>. Implement <see cref="IDisposable"/> or call
///     <see cref="ZeroData"/> after transmission to zero the internal copy when the command
///     carries sensitive material (PIN bytes, key material, cryptograms).
///     </para>
///     <para>
///     Non-sensitive commands (SELECT, GET, RESET, etc.) do not require explicit disposal.
///     </para>
/// </remarks>
public sealed class ApduCommand : IDisposable
{
    // Backing field for Data — stored as byte[] so ZeroMemory can clear it.
    private byte[] _dataBytes = [];

    /// <summary>Initializes an empty <see cref="ApduCommand"/> for use with object-initializer syntax.</summary>
    public ApduCommand()
    {
    }

    private ApduCommand(byte cla, byte ins, byte p1, byte p2, ReadOnlyMemory<byte>? data = null, int le = 0)
    {
        Cla = cla;
        Ins = ins;
        P1 = p1;
        P2 = p2;
        Le = le;
        _dataBytes = data?.ToArray() ?? [];
    }

    /// <summary>Initializes a new <see cref="ApduCommand"/> with explicit header bytes and optional data payload.</summary>
    public ApduCommand(int cla, int ins, int p1, int p2, ReadOnlyMemory<byte>? data = null, int le = 0)
        : this(
            ByteUtils.ValidateByte(cla, nameof(cla)),
            ByteUtils.ValidateByte(ins, nameof(ins)),
            ByteUtils.ValidateByte(p1, nameof(p1)),
            ByteUtils.ValidateByte(p2, nameof(p2)),
            data,
            le)
    {
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
    ///     Gets or sets the optional command data payload.
    /// </summary>
    /// <remarks>
    ///     The setter copies the provided memory into an internally-owned <c>byte[]</c>.
    ///     Use <see cref="ZeroData"/> or <see cref="Dispose"/> to clear the internal copy
    ///     when the payload contains sensitive material.
    /// </remarks>
    public ReadOnlyMemory<byte> Data
    {
        get => _dataBytes;
        init => _dataBytes = value.ToArray();
    }

    /// <summary>
    ///     Zeroes the internal data buffer. Safe to call multiple times.
    ///     Does not affect the object's usability — <see cref="Data"/> will return zeroed bytes after this call.
    /// </summary>
    public void ZeroData() => CryptographicOperations.ZeroMemory(_dataBytes);

    /// <inheritdoc />
    public void Dispose()
    {
        ZeroData();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"CLA: 0x{Cla:X2} INS: 0x{Ins:X2} P1: 0x{P1:X2} P2: 0x{P2:X2} Le: {Le} Data: {Data.Length} bytes";
}
