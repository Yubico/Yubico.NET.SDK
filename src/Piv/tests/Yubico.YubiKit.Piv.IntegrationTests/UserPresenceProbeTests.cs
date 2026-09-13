// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class UserPresenceProbeTests(ITestOutputHelper output)
{
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.7.0", ConnectionType = ConnectionType.SmartCard)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task InspectPrerequisites_ReadOnly_PrintsNonSecretState(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync(
            new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard });

        PivManagementKeyMetadata managementKey = await session.GetManagementKeyMetadataAsync();
        PivPinMetadata pin = await session.GetPinMetadataAsync();
        PivSlotMetadata? retired20 = await session.GetSlotMetadataAsync(PivSlot.Retired20);

        output.WriteLine($"Firmware={state.FirmwareVersion}");
        output.WriteLine(
            $"ManagementKey: IsDefault={managementKey.IsDefault}, KeyType={managementKey.KeyType}, " +
            $"TouchPolicy={managementKey.TouchPolicy}");
        output.WriteLine(
            $"PIN: IsDefault={pin.IsDefault}, TotalRetries={pin.TotalRetries}, " +
            $"RetriesRemaining={pin.RetriesRemaining}");
        output.WriteLine($"Retired20: Occupied={retired20 is not null}");
    }

    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.7.0", ConnectionType = ConnectionType.SmartCard)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task SignOrDecryptAsync_TouchPolicyAlways_CompletesOnceAndSessionRemainsUsable(
        YubiKeyTestState state)
    {
        var prompt = new RecordingUserPresencePrompt(output);
        await using var session = await state.Device.CreatePivSessionAsync(
            new SessionCreationOptions
            {
                PreferredConnectionType = ConnectionType.SmartCard,
                UserPresencePrompt = prompt
            });

        PivManagementKeyMetadata managementKeyMetadata = await session.GetManagementKeyMetadataAsync();
        PivSlotMetadata? existingSlotMetadata = await session.GetSlotMetadataAsync(PivSlot.Retired20);

        output.WriteLine(
            $"Prerequisites: Firmware={state.FirmwareVersion}, ManagementKeyIsDefault={managementKeyMetadata.IsDefault}, " +
            $"ManagementKeyType={managementKeyMetadata.KeyType}, " +
            $"ManagementKeyTouchPolicy={managementKeyMetadata.TouchPolicy}, " +
            $"Retired20Occupied={existingSlotMetadata is not null}");

        Skip.If(
            !managementKeyMetadata.IsDefault,
            "The PIV management key is not the known default; refusing to authenticate or mutate the applet.");
        Skip.If(
            managementKeyMetadata.TouchPolicy is not PivTouchPolicy.Default and not PivTouchPolicy.Never,
            $"The management key touch policy is {managementKeyMetadata.TouchPolicy}; refusing an unrelated touch " +
            "during prerequisite authentication.");
        Skip.If(
            managementKeyMetadata.KeyType is not PivManagementKeyType.TripleDes and not PivManagementKeyType.Aes192,
            $"The default management key cannot be used with reported key type {managementKeyMetadata.KeyType}.");
        Skip.If(
            session.ManagementKeyType != managementKeyMetadata.KeyType,
            $"The session key type {session.ManagementKeyType} does not match metadata " +
            $"{managementKeyMetadata.KeyType}; refusing authentication.");
        Skip.If(
            existingSlotMetadata is not null,
            "PIV slot Retired20 is occupied; refusing to overwrite it. Clear it deliberately before running this probe.");

        byte[] managementKey = PivSession.DefaultManagementKey.ToArray();
        try
        {
            await session.AuthenticateAsync(managementKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(managementKey);
        }

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);

        var keyCreated = false;
        byte[] digest = SHA256.HashData("PIV user-presence probe"u8);
        ReadOnlyMemory<byte> signature = default;
        try
        {
            IPublicKey generatedPublicKey = await session.GenerateKeyAsync(
                PivSlot.Retired20,
                PivAlgorithm.EccP256,
                new PivKeyCreationOptions
                {
                    PinPolicy = PivPinPolicy.Never,
                    TouchPolicy = PivTouchPolicy.Always
                });
            keyCreated = true;
            ECPublicKey publicKey = Assert.IsType<ECPublicKey>(generatedPublicKey);

            Assert.Empty(prompt.Requested);
            Assert.Empty(prompt.Resolved);
            output.WriteLine(
                "The signing operation uses a 30-second cooperative cancellation token. It does not guarantee " +
                "interruption of an in-flight SmartCard exchange.");

            using var operationCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            signature = await session.SignOrDecryptAsync(
                PivSlot.Retired20,
                PivAlgorithm.EccP256,
                digest,
                operationCts.Token);

            using ECDsa verifier = ECDsa.Create(publicKey.Parameters);
            Assert.True(verifier.VerifyHash(
                digest,
                signature.Span,
                DSASignatureFormat.Rfc3279DerSequence));

            UserPresenceContext requested = Assert.Single(prompt.Requested);
            Assert.Equal(UserPresenceBasis.PolicyRequires, requested.Basis);
            Assert.Equal("PIV", requested.Application);
            Assert.Equal(PivSlot.Retired20.ToString(), requested.Scope);

            (UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken CancellationToken) resolved =
                Assert.Single(prompt.Resolved);
            Assert.Same(requested, resolved.Context);
            Assert.Equal(UserPresenceOutcome.Completed, resolved.Outcome);
            Assert.Equal(CancellationToken.None, resolved.CancellationToken);

            using var followUpCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            PivSlotMetadata? slotMetadata = await session.GetSlotMetadataAsync(
                PivSlot.Retired20,
                followUpCts.Token);
            Assert.NotNull(slotMetadata);
            Assert.Equal(PivAlgorithm.EccP256, slotMetadata.Value.Algorithm);
            Assert.Equal(PivPinPolicy.Never, slotMetadata.Value.PinPolicy);
            Assert.Equal(PivTouchPolicy.Always, slotMetadata.Value.TouchPolicy);
            Assert.Equal(state.SerialNumber, await session.GetSerialNumberAsync(followUpCts.Token));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(digest);
            if (!signature.IsEmpty)
            {
                CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(signature).Span);
            }

            if (keyCreated)
            {
                await session.DeleteKeyAsync(PivSlot.Retired20);
                Assert.Null(await session.GetSlotMetadataAsync(PivSlot.Retired20));
                output.WriteLine("Cleanup: deleted the test-created key from PIV slot Retired20.");
            }
        }
    }

    private sealed class RecordingUserPresencePrompt(ITestOutputHelper output) : IUserPresencePrompt
    {
        public List<UserPresenceContext> Requested { get; } = [];

        public List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken CancellationToken)>
            Resolved
        { get; } = [];

        public ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken)
        {
            Requested.Add(context);
            output.WriteLine(
                $"[{DateTimeOffset.UtcNow:O}] PIV user-presence request: Basis={context.Basis}, " +
                $"Slot={context.Scope}. Touch the YubiKey now.");
            return default;
        }

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolved.Add((context, outcome, cancellationToken));
            output.WriteLine(
                $"[{DateTimeOffset.UtcNow:O}] PIV user-presence resolution: Outcome={outcome}.");
            return default;
        }
    }
}