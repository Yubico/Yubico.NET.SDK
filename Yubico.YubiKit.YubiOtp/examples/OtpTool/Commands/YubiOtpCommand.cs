// Copyright 2026 Yubico AB
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

using Spectre.Console;
using System.Security.Cryptography;

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool.Commands;

/// <summary>
/// Programs Yubico OTP on a slot (ykman otp yubiotp SLOT).
/// </summary>
public static class YubiOtpCommand
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        var slot = options.Slot
                   ?? throw new ArgumentException("SLOT argument is required. Usage: OtpTool yubiotp <1|2>");

        byte[]? publicId = null;
        byte[]? privateId = null;
        byte[]? aesKey = null;
        byte[] accessCode = OutputHelper.ParseHex(options.AccessCode, "access-code") ?? [];

        try
        {
            // Public ID
            if (options.SerialPublicId)
            {
                // Will be resolved after session creation using device serial
                publicId = null; // Sentinel: resolve later
            }
            else
            {
                publicId = OutputHelper.ParseHex(options.PublicId, "public-id");

                if (publicId is null)
                {
                    throw new ArgumentException(
                        "--public-id or --serial-public-id is required for yubiotp command.");
                }
            }

            // Private ID
            if (options.GeneratePrivateId)
            {
                privateId = RandomNumberGenerator.GetBytes(6);

                if (!options.Json)
                {
                    OutputHelper.WriteHex("Generated private ID", privateId);
                }
            }
            else
            {
                privateId = OutputHelper.ParseHex(options.PrivateId, "private-id")
                            ?? throw new ArgumentException(
                                "--private-id or --generate-private-id is required for yubiotp command.");
            }

            // AES Key
            if (options.GenerateKey)
            {
                aesKey = RandomNumberGenerator.GetBytes(16);

                if (!options.Json)
                {
                    OutputHelper.WriteHex("Generated AES key", aesKey);
                }
            }
            else
            {
                aesKey = OutputHelper.ParseHex(options.Key, "key")
                         ?? throw new ArgumentException(
                             "--key or --generate-key is required for yubiotp command.");
            }

            if (!options.Force && !options.Json)
            {
                if (!AnsiConsole.Confirm(
                        $"Program slot {FormatSlot(slot)} with Yubico OTP?",
                        defaultValue: false))
                {
                    OutputHelper.WriteError("Aborted.");
                    return 1;
                }
            }

            await using var session = await DeviceHelper.CreateSessionAsync(ct);

            // Resolve serial-based public ID now that we have a session
            if (options.SerialPublicId)
            {
                int serial = await session.GetSerialAsync(ct);
                publicId = EncodeSerialAsPublicId(serial);

                if (!options.Json)
                {
                    OutputHelper.WriteHex("Serial-based public ID", publicId);
                }
            }

            using var config = new YubiOtpSlotConfiguration(publicId!, privateId, aesKey);

            await session.PutConfigurationAsync(slot, config, currentAccessCode: accessCode, cancellationToken: ct);

            if (options.Json)
            {
                OutputHelper.WriteJson(new
                {
                    status = "ok",
                    slot = FormatSlot(slot),
                    type = "yubiotp",
                    publicId = Convert.ToHexString(publicId!)
                });
            }
            else
            {
                OutputHelper.WriteSuccess($"Slot {FormatSlot(slot)} programmed with Yubico OTP.");
            }

            return 0;
        }
        finally
        {
            if (aesKey is not null) CryptographicOperations.ZeroMemory(aesKey);
            if (privateId is not null) CryptographicOperations.ZeroMemory(privateId);
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }

    /// <summary>
    /// Encodes a device serial number as a 6-byte public ID.
    /// The serial is stored as big-endian in the last 4 bytes, with 2 zero prefix bytes.
    /// </summary>
    private static byte[] EncodeSerialAsPublicId(int serial)
    {
        byte[] publicId = new byte[6];
        // Big-endian encoding of serial in bytes 2-5
        publicId[2] = (byte)((serial >> 24) & 0xFF);
        publicId[3] = (byte)((serial >> 16) & 0xFF);
        publicId[4] = (byte)((serial >> 8) & 0xFF);
        publicId[5] = (byte)(serial & 0xFF);
        return publicId;
    }

    private static string FormatSlot(Slot slot) =>
        slot == Slot.One ? "1" : "2";
}