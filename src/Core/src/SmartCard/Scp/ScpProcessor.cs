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

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
///     APDU processor that wraps commands and responses with SCP encryption and MAC.
/// </summary>
internal class ScpProcessor(
    IApduProcessor @delegate,
    ScpState state) : IApduProcessor, IDisposable
{
    // SCP Constants
    private const byte ClaBitSecureMessaging = 0x04; // Bit 2 in CLA byte indicates secure messaging
    private const int MacLength = 8; // AES-CMAC output truncated to 8 bytes

    private readonly ApduFormatterExtended _apduFormatterExtended = new(SmartCardMaxApduSizes.Yk43);

    /// <summary>
    ///     Gets the SCP state for this processor.
    /// </summary>
    private ScpState State { get; } = state;

    /// <summary>
    ///     Disposes the SCP state, zeroing all session keys.
    /// </summary>
    public void Dispose() => State.Dispose();

    /// <summary>
    ///     Transmits a command APDU with optional SCP encryption and MAC.
    /// </summary>
    /// <param name="command">The command to transmit.</param>
    /// <param name="useScp">Whether to apply SCP (MAC, encryption, SCP bit). If false, passes through to delegate.</param>
    /// <param name="encrypt">Whether to encrypt the command data (only applies if useScp is true).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response APDU with MAC verified and data decrypted if applicable.</returns>
    public async Task<ApduResponse> TransmitAsync(ApduCommand command, bool useScp, bool encrypt,
        CancellationToken cancellationToken)
    {
        // If SCP is not requested, pass through to delegate directly
        if (!useScp)
            return await @delegate.TransmitAsync(command, false, cancellationToken).ConfigureAwait(false);

        byte[]? scpCommandData = null;
        byte[]? finalCommandData = null;
        byte[]? mac = null;
        byte[]? encryptedData = null; // Declared here so finally can zero it (T11)

        try
        {
            // Step 1: Encrypt command data if requested
            // Note: Even empty data must be encrypted (padded) to maintain counter synchronization
            var commandData = command.Data;
            if (encrypt)
            {
                encryptedData = State.Encrypt(commandData.Span);
                commandData = encryptedData;
            }

            // Step 2: Ensure CLA has SM bit set (bit 2)
            var cla = (byte)(command.Cla | ClaBitSecureMessaging);

            // Step 3: Allocate buffer for data + MAC (matching Java approach)
            using var macedData = new DisposableArrayPoolBuffer(commandData.Length + MacLength);
            commandData.Span.CopyTo(macedData.Span);

            // Step 4: Create command with FULL length (data + MAC space)
            // This ensures Lc in formatted APDU = data.length + 8
            scpCommandData = macedData.Span.ToArray();
            ApduCommand scpCommand =
                new(cla, command.Ins, command.P1, command.P2, scpCommandData, command.Le);

            // Step 5: Format the APDU with full length
            ReadOnlyMemory<byte> formattedApdu;
            bool isExtendedApdu;
            if (macedData.Length > SmartCardMaxApduSizes.ShortApduMaxChunkSize)
            {
                formattedApdu = _apduFormatterExtended.Format(scpCommand);
                isExtendedApdu = true;
            }
            else
            {
                formattedApdu = Formatter.Format(scpCommand);
                isExtendedApdu = Formatter is ApduFormatterExtended;
            }

            // Step 6: Compute MAC over formatted APDU minus last MacLength bytes (the MAC space)
            // Exclude Le field if present
            var apduToMac = formattedApdu.Span;
            var macLength = apduToMac.Length - MacLength; // Don't MAC the MAC space
            if (scpCommand.Le > 0)
                // Extended APDU has 3-byte Le, short APDU has 1-byte Le
                macLength -= isExtendedApdu ? 3 : 1;

            mac = State.Mac(apduToMac[..macLength]);

            // Step 7: Fill in the MAC in the last 8 bytes
            mac.AsSpan().CopyTo(macedData.Span[commandData.Length..]);

            // Step 8: Create final command with MAC filled in
            finalCommandData = macedData.Span.ToArray();
            ApduCommand finalCommand =
                new(cla, command.Ins, command.P1, command.P2, finalCommandData, command.Le);

            // Step 9: Transmit the command (useScp=false because we already wrapped it with SCP)
            var response = await @delegate.TransmitAsync(finalCommand, false, cancellationToken).ConfigureAwait(false);

            // Step 10: Verify and remove MAC from response
            if (response.Data.Length > 0)
            {
                var unmacdData = State.Unmac(response.Data.Span, response.SW);

                // Step 11: Decrypt response data
                // Note: Response is ALWAYS encrypted once SCP session is established,
                // regardless of whether the command data was encrypted
                if (unmacdData.Length > 0)
                {
                    var decryptedData = State.Decrypt(unmacdData);
                    return new ApduResponse(decryptedData, response.SW);
                }

                return new ApduResponse(unmacdData, response.SW);
            }

            return response;
        }
        finally
        {
            if (encryptedData is not null) CryptographicOperations.ZeroMemory(encryptedData);
            if (scpCommandData is not null) CryptographicOperations.ZeroMemory(scpCommandData);
            if (finalCommandData is not null) CryptographicOperations.ZeroMemory(finalCommandData);
            if (mac is not null) CryptographicOperations.ZeroMemory(mac);
        }
    }


    /// <summary>
    ///     Gets the APDU formatter.
    /// </summary>
    public IApduFormatter Formatter { get; } = @delegate.Formatter;

    /// <summary>
    ///     Transmits a command APDU with SCP encryption and MAC, and processes the response.
    /// </summary>
    public Task<ApduResponse> TransmitAsync(ApduCommand command, bool useScp = true,
        CancellationToken cancellationToken = default)
        => TransmitAsync(command, useScp, true, cancellationToken);

}