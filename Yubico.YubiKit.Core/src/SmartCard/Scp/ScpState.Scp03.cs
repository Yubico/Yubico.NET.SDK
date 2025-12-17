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

namespace Yubico.YubiKit.Core.SmartCard.Scp;

internal partial class ScpState
{
    public static async Task<(ScpState State, byte[] HostCryptogram)> Scp03InitAsync(
        IApduProcessor processor,
        Scp03KeyParameters keyParams,
        byte[]? hostChallenge = null,
        CancellationToken cancellationToken = default)
    {
        hostChallenge ??= RandomNumberGenerator.GetBytes(8);

        var initCommand = new ApduCommand(
            0x80,
            InsInitializeUpdate,
            keyParams.KeyReference.Kvn,
            0x00,
            hostChallenge);

        var resp = await processor.TransmitAsync(initCommand, false, cancellationToken).ConfigureAwait(false);
        if (!resp.IsOK())
            throw ApduException.FromResponse(resp, initCommand, "SCP03 INITIALIZE UPDATE failed");

        var responseData = resp.Data.Span;
        var diversificationData = responseData[..10];
        var keyInfo = responseData[10..13];
        var cardChallenge = responseData[13..21];
        var cardCryptogram = responseData[21..29];

        Console.WriteLine("[DEBUG] INITIALIZE UPDATE response:");
        Console.WriteLine($"[DEBUG]   Key Info: {Convert.ToHexString(keyInfo)}");
        Console.WriteLine($"[DEBUG]   Key Info[0] (Key Diversification): 0x{keyInfo[0]:X2}");
        Console.WriteLine($"[DEBUG]   Key Info[1] (Key Version): 0x{keyInfo[1]:X2}");
        Console.WriteLine($"[DEBUG]   Key Info[2] (SCP ID): 0x{keyInfo[2]:X2}");

        Span<byte> context = stackalloc byte[16];
        hostChallenge.AsSpan().CopyTo(context);
        cardChallenge.CopyTo(context[8..]);

        Console.WriteLine($"[DEBUG] Host challenge: {Convert.ToHexString(hostChallenge)}");
        Console.WriteLine($"[DEBUG] Card challenge: {Convert.ToHexString(cardChallenge)}");
        Console.WriteLine($"[DEBUG] Context: {Convert.ToHexString(context)}");

        var sessionKeys = keyParams.Keys.Derive(context);

        Console.WriteLine($"[DEBUG] S-ENC:  {Convert.ToHexString(sessionKeys.Senc)}");
        Console.WriteLine($"[DEBUG] S-MAC:  {Convert.ToHexString(sessionKeys.Smac)}");
        Console.WriteLine($"[DEBUG] S-RMAC: {Convert.ToHexString(sessionKeys.Srmac)}");

        Span<byte> genCardCryptogram = stackalloc byte[8];
        StaticKeys.DeriveKey(sessionKeys.Smac, DerivationTypeCardCryptogram, context, DerivationContextLength,
            genCardCryptogram);

        Console.WriteLine($"[DEBUG] Generated card cryptogram: {Convert.ToHexString(genCardCryptogram)}");
        Console.WriteLine($"[DEBUG] Received card cryptogram:  {Convert.ToHexString(cardCryptogram)}");

        if (!CryptographicOperations.FixedTimeEquals(genCardCryptogram, cardCryptogram))
            throw new BadResponseException(
                $"Wrong SCP03 key set - Expected: {Convert.ToHexString(cardCryptogram)}, Got: {Convert.ToHexString(genCardCryptogram)}");

        Span<byte> hostCryptogramBytes = stackalloc byte[8];
        StaticKeys.DeriveKey(sessionKeys.Smac, DerivationTypeHostCryptogram, context, DerivationContextLength,
            hostCryptogramBytes);

        return (new ScpState(sessionKeys, new byte[16]), hostCryptogramBytes.ToArray());
    }
}