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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests;

/// <summary>
///     Attempts session creation with mismatched SCP keys and expects an exception.
///     Broad exception assertions do not distinguish authentication rejection from setup or transport failure.
/// </summary>
public class SecurityDomainSession_NegativeTests : SecurityDomainStateRestoringTests
{
    private static readonly CancellationTokenSource CancellationTokenSource = new(TimeSpan.FromSeconds(100));

    /// <summary>
    ///     Expects an exception when creating SCP11b with a random public key that does not match
    ///     the generated device key; the assertion does not identify the failure's cause.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.2")]
    public async Task Scp11b_WithWrongPublicKey_FailsAuthentication(YubiKeyTestState state)
    {
        Track(state);

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
    ///     Expects an exception from an SCP03 creation attempt with wrong symmetric keys after a reset request.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task Scp03_WithWrongKeys_FailsAuthentication(YubiKeyTestState state)
    {
        var ct = CancellationTokenSource.Token;

        // Request reset before attempting default-reference authentication with wrong key bytes.
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
        // The assertion accepts any exception; it does not pin the wrapper or underlying failure type.
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await state.WithSecurityDomainSessionAsync(false,
                session => Task.CompletedTask,
                scpKeyParams: wrongKeyParams,
                cancellationToken: ct);
        });
    }
}