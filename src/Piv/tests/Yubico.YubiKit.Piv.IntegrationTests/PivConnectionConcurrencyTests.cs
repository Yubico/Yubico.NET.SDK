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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

/// <summary>
///     Demonstrates on hardware that a single session refuses concurrent calls before APDUs can
///     interleave on the shared connection. The deterministic gate for this behavior is
///     <c>Core.UnitTests PcscProtocolConcurrencyTests</c>; this test shows the end-to-end effect on a
///     real card.
/// </summary>
public class PivConnectionConcurrencyTests
{
    private static readonly byte[] DefaultTripleDesManagementKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    private static readonly byte[] DefaultAesManagementKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    private static readonly byte[] DefaultPin = "123456"u8.ToArray();

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    /// <summary>
    ///     Starts a large-object read, then verifies that a PIN-gated sign on the same session is refused
    ///     while the read owns the logical exchange.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ConcurrentSignAndObjectRead_OnOneSession_SecondOperationIsRefused(
        YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        _ = await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256, PivPinPolicy.Once);
        await session.VerifyPinAsync(DefaultPin);

        // A ~2.5KB object makes the read a large multi-frame exchange on the wire.
        var storedObject = new byte[2500];
        RandomNumberGenerator.Fill(storedObject);
        await session.PutObjectAsync(PivDataObject.Retired1, storedObject);

        var digest = SHA256.HashData("connection concurrency"u8);

        for (var iteration = 0; iteration < 10; iteration++)
        {
            var readTask = session.GetObjectAsync(PivDataObject.Retired1);
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await session.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest));
            var readBack = await readTask;
            Assert.Equal(storedObject, readBack.ToArray());
        }
    }
}