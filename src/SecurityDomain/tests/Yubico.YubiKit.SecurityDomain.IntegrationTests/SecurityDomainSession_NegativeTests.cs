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
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests;

/// <summary>
///     Negative integration tests verifying security boundary enforcement.
///     These tests confirm that authentication fails when using incorrect or blocked keys.
/// </summary>
public class SecurityDomainSession_NegativeTests
{
    private static readonly CancellationTokenSource CancellationTokenSource = new(TimeSpan.FromSeconds(100));

    /// <summary>
    ///     Verifies that SCP11b authentication fails when using an incorrect (random) public key
    ///     that does not match the key stored on the device.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.2")]
    public async Task Scp11b_WithWrongPublicKey_FailsAuthentication(YubiKeyTestState state)
    {
        var ct = CancellationTokenSource.Token;
        var keyReference = new KeyReference(ScpKid.SCP11b, 0x03);

        // Session 1: Generate a real SCP11b key on the device
        await state.WithSecurityDomainSessionAsync(true,
            async session =>
            {
                await session.GenerateKeyAsync(keyReference, 0, ct);
            }, scpKeyParams: Scp03KeyParameters.Default, cancellationToken: ct);

        // Create a random EC key pair that does NOT match the device's key
        using var wrongEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var wrongPublicKey = ECPublicKey.CreateFromEcdh(wrongEcdh);
        var wrongKeyParams = new Scp11KeyParameters(keyReference, wrongPublicKey);

        // Session 2: Attempt authentication with the wrong public key -- should fail
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await state.WithSecurityDomainSessionAsync(false,
                session => Task.CompletedTask,
                scpKeyParams: wrongKeyParams,
                cancellationToken: ct);
        });
    }

    /// <summary>
    ///     Verifies that SCP03 authentication fails with incorrect (non-default) symmetric keys
    ///     when only the default keys are loaded on the device.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task Scp03_WithWrongKeys_FailsAuthentication(YubiKeyTestState state)
    {
        var ct = CancellationTokenSource.Token;

        // Reset to ensure only default keys are loaded
        await state.WithSecurityDomainSessionAsync(true,
            session => Task.CompletedTask,
            cancellationToken: ct);

        // Create wrong SCP03 keys
        byte[] wrongKeyBytes =
        [
            0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE, 0xBA, 0xBE,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
        ];

        using var wrongStaticKeys = new StaticKeys(wrongKeyBytes, wrongKeyBytes, wrongKeyBytes);
        var wrongKeyRef = new KeyReference(0x01, 0xFF);
        var wrongKeyParams = new Scp03KeyParameters(wrongKeyRef, wrongStaticKeys);

        // Attempt authentication with wrong keys -- should fail.
        // Wrong SCP03 keys may cause either ApduException (device rejects) or
        // BadResponseException (SDK-level cryptographic verification failure).
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await state.WithSecurityDomainSessionAsync(false,
                session => Task.CompletedTask,
                scpKeyParams: wrongKeyParams,
                cancellationToken: ct);
        });
    }
}
