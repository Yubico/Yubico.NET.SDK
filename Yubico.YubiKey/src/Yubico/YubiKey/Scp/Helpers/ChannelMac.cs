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

using System;
using System.Security.Cryptography;
using Yubico.Core.Cryptography;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;


namespace Yubico.YubiKey.Scp.Helpers
{
    /// <summary>
    /// Provides functionality for MAC (Message Authentication Code) operations in secure channel communications.
    /// This class handles the MAC generation and verification for APDUs in a secure channel.
    /// </summary>
    internal static class ChannelMac
    {
        /// <summary>
        /// Generates a MAC for a command APDU using the provided MAC key and chaining value.
        /// </summary>
        /// <param name="apdu">The command APDU to be MACed.</param>
        /// <param name="macKey">The key used for MAC generation.</param>
        /// <param name="macChainingValue">The MAC chaining value from previous operations (must be 16 bytes).</param>
        /// <returns>
        /// A tuple containing:
        /// - The command APDU with the generated MAC appended
        /// - The new MAC chaining value for use in subsequent operations
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the MAC chaining value is not 16 bytes long.</exception>
        public static (CommandApdu macdApdu, ReadOnlyMemory<byte> newMacChainingValue) MacApdu(
            CommandApdu apdu,
            ReadOnlySpan<byte> macKey,
            ReadOnlySpan<byte> macChainingValue)
        {
            // MAC chaining value must be 16 bytes to match AES block size
            if (macChainingValue.Length != 16)
            {
                throw new ArgumentException(ExceptionMessages.UnknownScpError, nameof(macChainingValue));
            }

            // Add 8 bytes of space for the MAC that will be calculated
            var apduWithLongerLen = AddDataToApdu(apdu, stackalloc byte[8]);
            Span<byte> apduBytesWithZeroMac = apduWithLongerLen.AsByteArray();

            // Prepare input for MAC calculation:
            // - First 16 bytes: MAC chaining value from previous operation
            // - Remaining bytes: APDU bytes (excluding the 8 zero bytes we added)
            Span<byte> macInputBuffer = stackalloc byte[16 + apduBytesWithZeroMac.Length - 8];
            macChainingValue.CopyTo(macInputBuffer);
            apduBytesWithZeroMac[..^8].CopyTo(macInputBuffer[16..]);

            // Calculate MAC using AES-CMAC (16 bytes)
            using var cmacObj = CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes128);

            Span<byte> newMacChainingValue = stackalloc byte[16];
            cmacObj.CmacInit(macKey);
            cmacObj.CmacUpdate(macInputBuffer);
            cmacObj.CmacFinal(newMacChainingValue);

            // Create final APDU with the first 8 bytes of the MAC appended
            var macdApdu = AddDataToApdu(apdu, newMacChainingValue[..8]);

            return (macdApdu, newMacChainingValue.ToArray());
        }

        /// <summary>
        /// Verifies the Response MAC (RMAC) of a response message using the provided RMAC key and chaining value.
        /// </summary>
        /// <param name="response">The response data containing the RMAC to verify.</param>
        /// <param name="rmacKey">The key used for RMAC verification.</param>
        /// <param name="macChainingValue">The MAC chaining value from previous operations.</param>
        /// <exception cref="SecureChannelException">
        /// Thrown when:
        /// - The response length is insufficient (less than 8 bytes)
        /// - The response length is incorrect for decryption
        /// - The RMAC verification fails
        /// </exception>
        public static void VerifyRmac(
            ReadOnlySpan<byte> response,
            ReadOnlySpan<byte> rmacKey,
            ReadOnlySpan<byte> macChainingValue)
        {
            // Response must be at least 8 bytes (minimum length for MAC)
            if (response.Length < 8)
            {
                throw new SecureChannelException(ExceptionMessages.InsufficientResponseLengthToVerifyRmac);
            }

            // Response length (excluding MAC) must be multiple of 16 for AES block alignment
            if ((response.Length - 8) % 16 != 0)
            {
                throw new SecureChannelException(ExceptionMessages.IncorrectResponseLengthToDecrypt);
            }

            // Extract the received MAC (last 8 bytes of response)
            var recvdRmac = response[^8..];

            // Prepare input for MAC verification:
            // - First 16 bytes: MAC chaining value
            // - Next bytes: Response data (excluding MAC)
            // - Last 2 bytes: Success status words (90 00)
            int respDataLen = response.Length - 8;
            Span<byte> macInput = stackalloc byte[16 + respDataLen + 2];
            macChainingValue.CopyTo(macInput);
            response[..respDataLen].CopyTo(macInput[16..]);

            macInput[16 + respDataLen] = SW1Constants.Success;
            macInput[16 + respDataLen + 1] = SWConstants.Success & 0xFF;

            // Calculate expected MAC using AES-CMAC
            using var cmacObj = CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes128);

            Span<byte> cmac = stackalloc byte[16];
            cmacObj.CmacInit(rmacKey);
            cmacObj.CmacUpdate(macInput);
            cmacObj.CmacFinal(cmac);

            // Use constant-time comparison to prevent timing attacks
            if (!CryptographicOperations.FixedTimeEquals(recvdRmac, cmac[..8]))
            {
                throw new SecureChannelException(ExceptionMessages.IncorrectRmac);
            }
        }

        private static CommandApdu AddDataToApdu(CommandApdu apdu, ReadOnlySpan<byte> data)
        {
            var newApdu = new CommandApdu
            {
                Cla = apdu.Cla,
                Ins = apdu.Ins,
                P1 = apdu.P1,
                P2 = apdu.P2
            };

            int currentDataLength = apdu.Data.Length;
            byte[] newData = new byte[currentDataLength + data.Length];

            if (!apdu.Data.IsEmpty)
            {
                apdu.Data.Span.CopyTo(newData);
            }

            data.CopyTo(newData.AsSpan(currentDataLength));
            newApdu.Data = newData;

            return newApdu;
        }
    }
}
