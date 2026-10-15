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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

namespace Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

internal partial class ScpState
{
    private const byte InsInitializeUpdate = 0x50; // Amd D §7.1.1

    // Data derivation constants (Amd D §4.1.5, Table 4-1). The session keys use 0x04/0x06/0x07, see StaticKeys.
    private const byte DerivationTypeCardCryptogram = 0x00;
    private const byte DerivationTypeHostCryptogram = 0x01;
    private const byte DerivationContextLength = 0x40; // Cryptogram length in bits: 64 = 8 bytes
    private const int InitializeUpdateResponseLength = 29; // Amd D §7.1.1.3: 10 div data + 3 key info + 8 challenge + 8 cryptogram

    /// <summary>
    ///     Runs the host side of the SCP03 INITIALIZE UPDATE step (Amd D §7.1.1): derives the session
    ///     keys from the host and card challenges and verifies the card cryptogram.
    /// </summary>
    /// <returns>
    ///     The session state, plus the host cryptogram the caller must send in EXTERNAL AUTHENTICATE
    ///     (Amd D §7.1.2) to complete the handshake. The MAC chain starts at zero; EXTERNAL AUTHENTICATE
    ///     is the first command MAC'd through it.
    /// </returns>
    /// <exception cref="BadResponseException">
    ///     The response is shorter than 29 bytes, or the card cryptogram does not match (wrong key set).
    /// </exception>
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
        if (responseData.Length < InitializeUpdateResponseLength)
            throw new BadResponseException(
                $"INITIALIZE UPDATE response too short: expected {InitializeUpdateResponseLength} bytes, got {responseData.Length}");

        var diversificationData = responseData[..10];
        var keyInfo = responseData[10..13];
        var cardChallenge = responseData[13..21];
        var cardCryptogram = responseData[21..29];

        // Derivation context is host challenge || card challenge (Amd D §6.2.2.1)
        Span<byte> context = stackalloc byte[16];
        hostChallenge.AsSpan().CopyTo(context);
        cardChallenge.CopyTo(context[8..]);

        var sessionKeys = keyParams.Keys.Derive(context);

        Span<byte> genCardCryptogram = stackalloc byte[8];
        StaticKeys.DeriveKey(sessionKeys.Smac, DerivationTypeCardCryptogram, context, DerivationContextLength,
            genCardCryptogram);

        if (!CryptographicOperations.FixedTimeEquals(genCardCryptogram, cardCryptogram))
        {
            sessionKeys.Dispose();
            throw new BadResponseException("Wrong SCP03 key set - card cryptogram verification failed");
        }

        Span<byte> hostCryptogramBytes = stackalloc byte[8];
        StaticKeys.DeriveKey(sessionKeys.Smac, DerivationTypeHostCryptogram, context, DerivationContextLength,
            hostCryptogramBytes);

        return (new ScpState(sessionKeys, new byte[16]), hostCryptogramBytes.ToArray());
    }
}