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

using System.Buffers;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
///     APDU processor that wraps commands and responses with SCP encryption and MAC.
/// </summary>
internal class ScpProcessor(IApduProcessor @delegate, IApduFormatter formatter, ScpState state) : IApduProcessor
{
    private readonly ExtendedApduFormatter _extendedApduFormatter = new(SmartCardMaxApduSizes.Yk43);

    /// <summary>
    ///     Gets the SCP state for this processor.
    /// </summary>
    internal ScpState State { get; } = state;

    #region IApduProcessor Members

    /// <summary>
    ///     Gets the APDU formatter.
    /// </summary>
    public IApduFormatter Formatter { get; } = formatter;

    /// <summary>
    ///     Transmits a command APDU with SCP encryption and MAC, and processes the response.
    /// </summary>
    public Task<ResponseApdu> TransmitAsync(CommandApdu command, CancellationToken cancellationToken = default)
        => TransmitAsync(command, true, cancellationToken);

    #endregion

    /// <summary>
    ///     Transmits a command APDU with optional SCP encryption and MAC.
    /// </summary>
    /// <param name="command">The command to transmit.</param>
    /// <param name="encrypt">Whether to encrypt the command data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response APDU with MAC verified and data decrypted if applicable.</returns>
    public async Task<ResponseApdu> TransmitAsync(CommandApdu command, bool encrypt,
        CancellationToken cancellationToken)
    {
        byte[]? rentedData = null;
        byte[]? rentedMacData = null;

        try
        {
            // Step 1: Encrypt command data if requested
            var commandData = command.Data;
            if (encrypt && commandData.Length > 0)
            {
                var encryptedData = State.Encrypt(commandData.Span);
                commandData = encryptedData;
            }

            // Step 2: Build APDU with encrypted data and SCP CLA bit
            var cla = (byte)(command.Cla | 0x04); // Set SCP bit in CLA
            CommandApdu scpCommand = new(cla, command.Ins, command.P1, command.P2, commandData, command.Le);

            // Step 3: Format the APDU for transmission
            ReadOnlyMemory<byte> formattedApdu;
            if (commandData.Length > SmartCardMaxApduSizes.ShortApduMaxChunkSize)
                formattedApdu = _extendedApduFormatter.Format(scpCommand);
            else
                formattedApdu = Formatter.Format(scpCommand);

            // Step 4: Compute MAC over the formatted APDU (excluding Le field if present)
            // The MAC is computed over CLA, INS, P1, P2, Lc, Data
            var apduToMac = formattedApdu.Span;
            // If Le is present, exclude it from MAC calculation
            var macLength = apduToMac.Length;
            if (scpCommand.Le > 0)
                // Extended APDU has 3-byte Le, short APDU has 1-byte Le
                macLength -= commandData.Length > SmartCardMaxApduSizes.ShortApduMaxChunkSize ? 3 : 1;

            var mac = State.Mac(apduToMac[..macLength]);

            // Step 5: Append MAC to command data
            rentedMacData = ArrayPool<byte>.Shared.Rent(commandData.Length + 8);
            var dataWithMac = rentedMacData.AsSpan(0, commandData.Length + 8);
            commandData.Span.CopyTo(dataWithMac);
            mac.AsSpan().CopyTo(dataWithMac[commandData.Length..]);

            // Step 6: Create final command with MAC appended
            CommandApdu finalCommand = new(cla, command.Ins, command.P1, command.P2, dataWithMac.ToArray(), command.Le);

            // Step 7: Transmit the command
            var response = await @delegate.TransmitAsync(finalCommand, cancellationToken).ConfigureAwait(false);

            // Step 8: Verify and remove MAC from response
            if (response.Data.Length > 0)
            {
                var unmacdData = State.Unmac(response.Data.Span, response.SW);

                // Step 9: Decrypt response data if it was encrypted
                if (encrypt && unmacdData.Length > 0)
                {
                    var decryptedData = State.Decrypt(unmacdData);
                    return new ResponseApdu(decryptedData, response.SW);
                }

                return new ResponseApdu(unmacdData, response.SW);
            }

            return response;
        }
        finally
        {
            if (rentedData != null) ArrayPool<byte>.Shared.Return(rentedData);
            if (rentedMacData != null) ArrayPool<byte>.Shared.Return(rentedMacData);
        }
    }
}