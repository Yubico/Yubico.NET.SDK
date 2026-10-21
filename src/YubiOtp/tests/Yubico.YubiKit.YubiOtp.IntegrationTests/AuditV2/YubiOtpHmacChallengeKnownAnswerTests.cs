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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.YubiOtp.IntegrationTests.AuditV2;

/// <summary>
///     Which bytes the YubiKey actually HMACs for a given challenge, by slot mode. The slot holds a known
///     key, so each response is compared with the host's HMAC-SHA1 over the message the device is
///     expected to have used.
/// </summary>
public sealed class YubiOtpHmacChallengeKnownAnswerTests
{
    private static readonly byte[] Key = [.. Enumerable.Repeat((byte)0x31, 20)];
    private static readonly byte[] SixtyFourDistinct = [.. Enumerable.Range(0, 64).Select(i => (byte)i)];

    // (label, challenge given to CalculateHmacSha1Async, message the device hashes)
    private static readonly (string Label, byte[] Challenge, byte[] HashedMessage)[] ShortModeVectors =
    [
        ("one byte", [0x41], [0x41]),
        ("explicitly zero-padded one byte", Pad([0x41], 0x00), [0x41]),
        ("three bytes", [0x41, 0x42, 0x42], [0x41, 0x42, 0x42]),
        ("trailing zero, padded with 0x01", [0x41, 0x00], [0x41, 0x00]),
        ("64 distinct bytes: the last is the marker", SixtyFourDistinct, SixtyFourDistinct[..63]),
        ("empty: sent as 64 x 0x00, hashed as one 0x00", [], [0x00]),
        ("64 x 0x41: hashed as one 0x41", Pad([], 0x41), [0x41]),
        ("trailing zero padded with zeros (the legacy zero-fill) loses the zero", Pad([0x01, 0x00], 0x00), [0x01]),
    ];

    private static readonly (string Label, byte[] Challenge, byte[] HashedMessage)[] FixedModeVectors =
    [
        ("one byte: hashed with its padding", [0x41], Pad([0x41], 0x00)),
        ("64 distinct bytes: all hashed", SixtyFourDistinct, SixtyFourDistinct),
        ("trailing zero: padded with 0x01, all 64 hashed", [0x41, 0x00], Pad([0x41, 0x00], 0x01)),
        ("64 x 0x00: all hashed", Pad([], 0x00), Pad([], 0x00)),
    ];

    [SkippableTheory]
    [WithYubiKey(MinFirmware = "2.2.0", ConnectionType = ConnectionType.HidOtp)]
    [Trait("Audit", "YESDK-1634")]
    public Task ShortChallengeSlot_HashesExactlyTheChallenge_WithTheLastByteAsMarker(YubiKeyTestState state) =>
        AssertResponsesAsync(state, shortChallenge: true, ShortModeVectors);

    [SkippableTheory]
    [WithYubiKey(MinFirmware = "2.2.0", ConnectionType = ConnectionType.HidOtp)]
    [Trait("Audit", "YESDK-1634")]
    public Task FixedChallengeSlot_HashesAll64BytesAsSent(YubiKeyTestState state) =>
        AssertResponsesAsync(state, shortChallenge: false, FixedModeVectors);

    private static async Task AssertResponsesAsync(
        YubiKeyTestState state, bool shortChallenge, (string Label, byte[] Challenge, byte[] HashedMessage)[] vectors)
    {
        await using var session = await state.Device.CreateYubiOtpSessionAsync(
            new SessionCreationOptions { PreferredConnectionType = state.ConnectionType });
        using var config = new HmacSha1SlotConfiguration(Key).UseShortChallenge(shortChallenge);
        await session.PutConfigurationAsync(Slot.Two, config);
        try
        {
            foreach (var (label, challenge, hashedMessage) in vectors)
            {
                var response = await session.CalculateHmacSha1Async(Slot.Two, challenge);

                Assert.True(
                    HMACSHA1.HashData(Key, hashedMessage).AsSpan().SequenceEqual(response.Span),
                    $"{label}: the device did not HMAC the expected {hashedMessage.Length}-byte message.");
            }
        }
        finally
        {
            await session.DeleteSlotAsync(Slot.Two);
        }
    }

    private static byte[] Pad(byte[] challenge, byte padByte) =>
        [.. challenge, .. Enumerable.Repeat(padByte, 64 - challenge.Length)];
}