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

using System.Globalization;

namespace Yubico.YubiKit.Core.SmartCard;

/// <summary>
///     Represents an ISO 7816 application response.
/// </summary>
public readonly record struct ApduResponse
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApduResponse" /> class.
    /// </summary>
    /// <param name="data">The raw data returned by the ISO 7816 smart card.</param>
    public ApduResponse(ReadOnlyMemory<byte> data)
    {
        if (data.Length < 2)
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                "ExceptionMessages.ResponseApduNotEnoughBytes, data.Length"));

        RawData = data.ToArray();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ApduResponse" /> class.
    /// </summary>
    /// <param name="dataWithoutSW">
    ///     The raw data returned by the ISO 7816 smart card without the
    ///     trailing status bytes.
    /// </param>
    /// <param name="sw">The status word, 'SW', for the APDU response.</param>
    public ApduResponse(byte[] dataWithoutSW, short sw)
    {
        ArgumentNullException.ThrowIfNull(dataWithoutSW);

        var raw = new byte[dataWithoutSW.Length + 2];
        dataWithoutSW.AsSpan().CopyTo(raw);
        raw[^2] = (byte)(sw >> 8);
        raw[^1] = (byte)(sw & 0xFF);
        RawData = raw;
    }

    /// <summary>
    ///     Gets the raw response data including the status word bytes.
    /// </summary>
    public ReadOnlyMemory<byte> RawData { get; }

    /// <summary>
    ///     The status word (two byte) code which represents the overall result of a CCID interaction.
    ///     The most common value is 0x9000 which represents a successful result.
    /// </summary>
    public short SW => (short)((SW1 << 8) | SW2);

    /// <summary>
    ///     A convenience property accessor for the high byte of SW
    /// </summary>
    public byte SW1 => RawData.Span[^2];

    /// <summary>
    ///     A convenience property accessor for the low byte of SW
    /// </summary>
    public byte SW2 => RawData.Span[^1];

    /// <summary>
    ///     Gets the data part of the response.
    /// </summary>
    /// <value>
    ///     The raw bytes not including the ending status word.
    /// </value>
    public ReadOnlyMemory<byte> Data => RawData[..^2];

    /// <summary>
    ///     Prints SW1, SW2, and the length of the Data field in a formatted string.
    /// </summary>
    public override string ToString() => $"SW1: 0x{SW1:X2} SW2: 0x{SW2:X2} Data: {Data.Span.Length} bytes";

    public bool IsOK() => SW == SWConstants.Success;
}