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

using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

/// <summary>
///     Represents an ISO 7816 application command.
/// </summary>
/// <remarks>
///     <para>
///     <b>Ownership and zeroing:</b> <see cref="ApduCommand"/> stores a reference to the caller-provided
///     <see cref="Data"/> buffer — it does <b>not</b> clone it. When the payload carries sensitive material
///     such as PIN bytes, key material, or cryptograms, the <b>caller</b> is responsible for zeroing the
///     source buffer in finally after the actual exchange task is terminal. Keep the data unchanged until then;
///     a canceled outer wait does not end the borrow.
///     </para>
///     <code>
///     // The caller supplies an open connection, an applet-specific header, and owned payload bytes.
///     // This helper borrows the connection and consumes the payload, clearing it even if creation fails.
///     static async Task&lt;short&gt; SendAndClearAsync(
///         Yubico.YubiKit.Core.Transports.SmartCard.ISmartCardConnection connection,
///         byte cla, byte ins, byte p1, byte p2, byte[] payload, CancellationToken ct)
///     {
///         try
///         {
///             await using var session = await Yubico.YubiKit.Core.Sessions.RawSmartCardSession.CreateAsync(connection, ct);
///             var command = new ApduCommand(cla, ins, p1, p2, payload);
///             var response = await session.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken: ct);
///             try
///             {
///                 return response.SW; // Inspect status before clearing the response owner.
///             }
///             finally
///             {
///                 response.Clear();
///             }
///         }
///         finally
///         {
///             System.Security.Cryptography.CryptographicOperations.ZeroMemory(payload);
///         }
///     }
///     </code>
/// </remarks>
public readonly record struct ApduCommand
{
    /// <summary>Initializes a new <see cref="ApduCommand"/> with explicit header bytes and optional data payload.</summary>
    public ApduCommand(int cla, int ins, int p1, int p2, ReadOnlyMemory<byte> data = default, int le = 0)
    {
        Cla = ByteUtils.ValidateByte(cla, nameof(cla));
        Ins = ByteUtils.ValidateByte(ins, nameof(ins));
        P1 = ByteUtils.ValidateByte(p1, nameof(p1));
        P2 = ByteUtils.ValidateByte(p2, nameof(p2));
        Data = data;
        Le = le;
    }

    /// <summary>Gets the CLA byte.</summary>
    public byte Cla { get; init; }

    /// <summary>Gets the INS byte.</summary>
    public byte Ins { get; init; }

    /// <summary>Gets the P1 byte.</summary>
    public byte P1 { get; init; }

    /// <summary>Gets the P2 byte.</summary>
    public byte P2 { get; init; }

    /// <summary>Gets the expected response-length value passed to the selected formatter.</summary>
    /// <remarks>
    ///     Both SDK formatters append Le; zero encodes a zero field rather than omitting it. Short formatting
    ///     accepts 0 through 255 (use zero for the maximum wire encoding); extended formatting accepts 0 through
    ///     65536, with both zero and 65536 encoded as two zero bytes. There is no absent-Le representation here.
    /// </remarks>
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