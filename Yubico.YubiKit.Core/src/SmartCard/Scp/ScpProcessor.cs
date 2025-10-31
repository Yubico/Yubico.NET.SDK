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
    // SCP Constants
    private const byte ClaBitSecureMessaging = 0x04;  // Bit 2 in CLA byte indicates secure messaging
    private const int MacLength = 8;                   // AES-CMAC output truncated to 8 bytes

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
    public Task<ResponseApdu> TransmitAsync(CommandApdu command, bool useScp = true,
        CancellationToken cancellationToken = default)
        => TransmitAsync(command, useScp, true, cancellationToken);

    #endregion

    /// <summary>
    ///     Transmits a command APDU with optional SCP encryption and MAC.
    /// </summary>
    /// <param name="command">The command to transmit.</param>
    /// <param name="useScp">Whether to apply SCP (MAC, encryption, SCP bit). If false, passes through to delegate.</param>
    /// <param name="encrypt">Whether to encrypt the command data (only applies if useScp is true).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response APDU with MAC verified and data decrypted if applicable.</returns>
    public async Task<ResponseApdu> TransmitAsync(CommandApdu command, bool useScp, bool encrypt,
        CancellationToken cancellationToken)
    {
        // If SCP is not requested, pass through to delegate directly
        if (!useScp) return await @delegate.TransmitAsync(command, false, cancellationToken).ConfigureAwait(false);

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

            // Step 2: Ensure CLA has SM bit set (bit 2)
            var cla = (byte)(command.Cla | ClaBitSecureMessaging);

            // Step 3: Allocate buffer for data + MAC (matching Java approach)
            rentedMacData = ArrayPool<byte>.Shared.Rent(commandData.Length + MacLength);
            var macedData = rentedMacData.AsSpan(0, commandData.Length + MacLength);
            commandData.Span.CopyTo(macedData);
            // Leave last 8 bytes zeroed for now

            // Step 4: Create command with FULL length (data + MAC space)
            // This ensures Lc in formatted APDU = data.length + 8
            CommandApdu scpCommand = new(cla, command.Ins, command.P1, command.P2, macedData.ToArray(), command.Le);

            // Step 5: Format the APDU with full length
            ReadOnlyMemory<byte> formattedApdu;
            bool isExtendedApdu;
            if (macedData.Length > SmartCardMaxApduSizes.ShortApduMaxChunkSize)
            {
                formattedApdu = _extendedApduFormatter.Format(scpCommand);
                isExtendedApdu = true;
            }
            else
            {
                formattedApdu = Formatter.Format(scpCommand);
                isExtendedApdu = Formatter is ExtendedApduFormatter;
            }

            // Step 6: Compute MAC over formatted APDU minus last MacLength bytes (the MAC space)
            // Exclude Le field if present
            var apduToMac = formattedApdu.Span;
            var macLength = apduToMac.Length - MacLength; // Don't MAC the MAC space
            if (scpCommand.Le > 0)
                // Extended APDU has 3-byte Le, short APDU has 1-byte Le
                macLength -= isExtendedApdu ? 3 : 1;

            var mac = State.Mac(apduToMac[..macLength]);

            Console.WriteLine($"[SCP DEBUG] Original command: CLA={command.Cla:X2} INS={command.Ins:X2} P1={command.P1:X2} P2={command.P2:X2}");
            Console.WriteLine($"[SCP DEBUG] Original data ({commandData.Length} bytes): {Convert.ToHexString(commandData.Span)}");
            Console.WriteLine($"[SCP DEBUG] MACed data length (with space): {macedData.Length} bytes");
            Console.WriteLine($"[SCP DEBUG] APDU to MAC ({macLength} bytes): {Convert.ToHexString(apduToMac[..macLength])}");
            Console.WriteLine($"[SCP DEBUG] Computed MAC: {Convert.ToHexString(mac.AsSpan())}");

            // Step 7: Fill in the MAC in the last 8 bytes
            mac.AsSpan().CopyTo(macedData[commandData.Length..]);

            Console.WriteLine($"[SCP DEBUG] Final data with MAC ({macedData.Length} bytes): {Convert.ToHexString(macedData)}");

            // Step 8: Create final command with MAC filled in
            CommandApdu finalCommand = new(cla, command.Ins, command.P1, command.P2, macedData.ToArray(), command.Le);
            Console.WriteLine($"[SCP DEBUG] Final command: CLA={cla:X2} INS={finalCommand.Ins:X2} P1={finalCommand.P1:X2} P2={finalCommand.P2:X2} Data={Convert.ToHexString(finalCommand.Data.Span)}");

            // Step 9: Transmit the command (useScp=false because we already wrapped it with SCP)
            var response = await @delegate.TransmitAsync(finalCommand, false, cancellationToken).ConfigureAwait(false);

            // Step 10: Verify and remove MAC from response
            Console.WriteLine($"[SCP DEBUG] Response SW: 0x{response.SW:X4}");
            Console.WriteLine($"[SCP DEBUG] Response data length: {response.Data.Length}");
            Console.WriteLine($"[SCP DEBUG] Response data: {Convert.ToHexString(response.Data.Span)}");

            if (response.Data.Length > 0)
            {
                var unmacdData = State.Unmac(response.Data.Span, response.SW);
                Console.WriteLine($"[SCP DEBUG] Unmacd data length: {unmacdData.Length}");
                Console.WriteLine($"[SCP DEBUG] Unmacd data: {Convert.ToHexString(unmacdData)}");
                Console.WriteLine($"[SCP DEBUG] First byte (length field): {unmacdData[0]}");

                // Step 11: Decrypt response data if it was encrypted
                Console.WriteLine($"[SCP DEBUG] encrypt={encrypt}, unmacdData.Length={unmacdData.Length}");
                if (encrypt && unmacdData.Length > 0)
                {
                    Console.WriteLine($"[SCP DEBUG] Decrypting response data...");
                    var decryptedData = State.Decrypt(unmacdData);
                    Console.WriteLine($"[SCP DEBUG] Decrypted data length: {decryptedData.Length}");
                    Console.WriteLine($"[SCP DEBUG] Decrypted data: {Convert.ToHexString(decryptedData)}");
                    return new ResponseApdu(decryptedData, response.SW);
                }

                Console.WriteLine($"[SCP DEBUG] NOT decrypting (encrypt={encrypt})");
                return new ResponseApdu(unmacdData, response.SW);
            }

            return response;
        }
        finally
        {
            if (rentedMacData != null) ArrayPool<byte>.Shared.Return(rentedMacData);
        }
    }
}