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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests;

public class TouchNotificationTests
{
    [Fact]
    public async Task SignOrDecryptAsync_WhenTouchPolicyAlways_RequestsBeforeApduAndResolvesCompleted()
    {
        var connection = CreateInitializedConnection(
            SlotMetadataResponse(PivAlgorithm.EccP256, PivTouchPolicy.Always),
            CryptoResponse(0xAA));
        var prompt = new RecordingUserPresencePrompt(() => connection.TransmittedCommands.Count);
        await using var session = await CreateSessionAsync(connection, prompt);

        var result = await session.SignOrDecryptAsync(
            PivSlot.Authentication,
            PivAlgorithm.EccP256,
            new byte[32],
            TestContext.Current.CancellationToken);

        Assert.Equal([0xAA], result.ToArray());
        Assert.Equal(4, prompt.CommandCountAtRequest);
        Assert.Equal(5, prompt.CommandCountAtResolution);
        Assert.Equal(0x87, connection.TransmittedCommands[^1][1]);
        AssertNotification(
            prompt,
            UserPresenceBasis.PolicyRequires,
            PivSlot.Authentication,
            UserPresenceOutcome.Completed);
    }

    [Fact]
    public async Task SignOrDecryptAsync_AutoDetect_ReusesLoadedMetadata()
    {
        var connection = CreateInitializedConnection(
            SlotMetadataResponse(PivAlgorithm.EccP256, PivTouchPolicy.Always),
            CryptoResponse(0xAA));
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await CreateSessionAsync(connection, prompt);

        _ = await session.SignOrDecryptAsync(
            PivSlot.Authentication,
            new byte[32],
            TestContext.Current.CancellationToken);

        Assert.Equal(
            1,
            connection.TransmittedCommands.Count(
                command => command[1] == 0xF7 && command[3] == (byte)PivSlot.Authentication));
        AssertNotification(
            prompt,
            UserPresenceBasis.PolicyRequires,
            PivSlot.Authentication,
            UserPresenceOutcome.Completed);
    }

    [Fact]
    public async Task DecryptAsync_ReusesMetadataAndResolvesCompletedBeforePostProcessingFails()
    {
        var connection = CreateInitializedConnection(
            SlotMetadataResponse(PivAlgorithm.Rsa1024, PivTouchPolicy.Always),
            [0x7C, 0x02, 0x82, 0x00, 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt(() => connection.TransmittedCommands.Count);
        await using var session = await CreateSessionAsync(connection, prompt);

        _ = await Assert.ThrowsAnyAsync<Exception>(() => session.DecryptAsync(
            PivSlot.KeyManagement,
            new byte[128],
            RSAEncryptionPadding.Pkcs1,
            TestContext.Current.CancellationToken));

        Assert.Equal(
            1,
            connection.TransmittedCommands.Count(
                command => command[1] == 0xF7 && command[3] == (byte)PivSlot.KeyManagement));
        AssertNotification(
            prompt,
            UserPresenceBasis.PolicyRequires,
            PivSlot.KeyManagement,
            UserPresenceOutcome.Completed);
        Assert.Equal(5, prompt.CommandCountAtResolution);
    }

    [Fact]
    public async Task CalculateSecretAsync_WhenTouchPolicyCached_RequestsConservatively()
    {
        var connection = CreateInitializedConnection(
            SlotMetadataResponse(PivAlgorithm.EccP256, PivTouchPolicy.Cached),
            [0x7C, 0x22, 0x82, 0x20, .. new byte[32], 0x90, 0x00]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await CreateSessionAsync(connection, prompt);
        using var peer = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var peerPublicKey = ECPublicKey.CreateFromParameters(peer.PublicKey.ExportParameters());

        _ = await session.CalculateSecretAsync(
            PivSlot.KeyManagement,
            peerPublicKey,
            TestContext.Current.CancellationToken);

        AssertNotification(
            prompt,
            UserPresenceBasis.PolicyMayRequire,
            PivSlot.KeyManagement,
            UserPresenceOutcome.Completed);
    }

    [Theory]
    [InlineData(PivTouchPolicy.Default)]
    [InlineData(PivTouchPolicy.Never)]
    public async Task SignOrDecryptAsync_WhenTouchPolicyDoesNotRequireTouch_RemainsSilent(PivTouchPolicy touchPolicy)
    {
        var connection = CreateInitializedConnection(
            SlotMetadataResponse(PivAlgorithm.EccP256, touchPolicy),
            CryptoResponse(0xAA));
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await CreateSessionAsync(connection, prompt);

        _ = await session.SignOrDecryptAsync(
            PivSlot.Signature,
            PivAlgorithm.EccP256,
            new byte[32],
            TestContext.Current.CancellationToken);

        Assert.Empty(prompt.Requests);
        Assert.Empty(prompt.Resolutions);
    }

    [Fact]
    public async Task SignOrDecryptAsync_WhenTouchPolicyIsUnknown_RequestsConservatively()
    {
        var connection = CreateInitializedConnection(
            SlotMetadataResponse(PivAlgorithm.EccP256, (PivTouchPolicy)0x7F),
            CryptoResponse(0xAA));
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await CreateSessionAsync(connection, prompt);

        _ = await session.SignOrDecryptAsync(
            PivSlot.Signature,
            PivAlgorithm.EccP256,
            new byte[32],
            TestContext.Current.CancellationToken);

        AssertNotification(
            prompt,
            UserPresenceBasis.PolicyMayRequire,
            PivSlot.Signature,
            UserPresenceOutcome.Completed);
    }

    [Fact]
    public async Task SignOrDecryptAsync_WhenMetadataQueryFails_RequestsConservatively()
    {
        var connection = CreateInitializedConnection(
            [0x6A, 0x80],
            CryptoResponse(0xAA));
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await CreateSessionAsync(connection, prompt);

        _ = await session.SignOrDecryptAsync(
            PivSlot.Signature,
            PivAlgorithm.EccP256,
            new byte[32],
            TestContext.Current.CancellationToken);

        AssertNotification(
            prompt,
            UserPresenceBasis.PolicyMayRequire,
            PivSlot.Signature,
            UserPresenceOutcome.Completed);
    }

    [Fact]
    public async Task SignOrDecryptAsync_WhenPromptThrows_DoesNotTransmitOrResolve()
    {
        var connection = CreateInitializedConnection(
            SlotMetadataResponse(PivAlgorithm.EccP256, PivTouchPolicy.Always),
            CryptoResponse(0xAA));
        var prompt = new RecordingUserPresencePrompt
        {
            RequestException = new InvalidOperationException("prompt failed")
        };
        await using var session = await CreateSessionAsync(connection, prompt);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => session.SignOrDecryptAsync(
            PivSlot.Authentication,
            PivAlgorithm.EccP256,
            new byte[32],
            TestContext.Current.CancellationToken));

        Assert.Equal("prompt failed", exception.Message);
        Assert.Equal(4, connection.TransmittedCommands.Count);
        Assert.Single(prompt.Requests);
        Assert.Empty(prompt.Resolutions);
    }

    [Fact]
    public async Task SignOrDecryptAsync_WhenCancelledAfterRequest_ResolvesCancelledWithoutTransmitting()
    {
        using var cancellationSource = new CancellationTokenSource();
        var connection = CreateInitializedConnection(
            SlotMetadataResponse(PivAlgorithm.EccP256, PivTouchPolicy.Always),
            CryptoResponse(0xAA));
        var prompt = new RecordingUserPresencePrompt
        {
            Requested = cancellationSource.Cancel
        };
        await using var session = await CreateSessionAsync(connection, prompt);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.SignOrDecryptAsync(
            PivSlot.Authentication,
            PivAlgorithm.EccP256,
            new byte[32],
            cancellationSource.Token));

        Assert.Equal(4, connection.TransmittedCommands.Count);
        AssertNotification(
            prompt,
            UserPresenceBasis.PolicyRequires,
            PivSlot.Authentication,
            UserPresenceOutcome.Cancelled);
    }

    [Fact]
    public async Task SignOrDecryptAsync_WhenResolutionFailsAfterApduSuccess_PropagatesResolutionException()
    {
        var expected = new InvalidOperationException("resolution failed");
        var connection = CreateInitializedConnection(
            SlotMetadataResponse(PivAlgorithm.EccP256, PivTouchPolicy.Always),
            CryptoResponse(0xAA));
        var prompt = new RecordingUserPresencePrompt { ResolutionException = expected };
        await using var session = await CreateSessionAsync(connection, prompt);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.SignOrDecryptAsync(
                PivSlot.Authentication,
                PivAlgorithm.EccP256,
                new byte[32],
                TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task SignOrDecryptAsync_WhenApduAndResolutionFail_PreservesApduException()
    {
        var connection = CreateInitializedConnection(
            SlotMetadataResponse(PivAlgorithm.EccP256, PivTouchPolicy.Always),
            [0x69, 0x82]);
        var prompt = new RecordingUserPresencePrompt
        {
            ResolutionException = new InvalidOperationException("resolution failed")
        };
        await using var session = await CreateSessionAsync(connection, prompt);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.SignOrDecryptAsync(
                PivSlot.Authentication,
                PivAlgorithm.EccP256,
                new byte[32],
                TestContext.Current.CancellationToken));

        Assert.Contains("Security status not satisfied", actual.Message, StringComparison.Ordinal);
        Assert.Single(prompt.Resolutions);
    }

    private static Task<PivSession> CreateSessionAsync(
        RecordingSmartCardConnection connection,
        IUserPresencePrompt prompt) =>
        PivSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), VersionResponse(), ManagementKeyMetadataResponse(), .. trailingResponses]);

    private static byte[] OkResponse() => [0x90, 0x00];

    private static byte[] VersionResponse() => [0x00, 0x00, 0x01, 0x90, 0x00];

    private static byte[] ManagementKeyMetadataResponse() =>
    [
        0x01, 0x01, (byte)PivManagementKeyType.TripleDes,
        0x02, 0x02, 0x00, (byte)PivTouchPolicy.Default,
        0x05, 0x01, 0x01,
        0x90, 0x00
    ];

    private static byte[] SlotMetadataResponse(PivAlgorithm algorithm, PivTouchPolicy touchPolicy) =>
    [
        0x01, 0x01, (byte)algorithm,
        0x02, 0x02, (byte)PivPinPolicy.Default, (byte)touchPolicy,
        0x03, 0x01, 0x01,
        0x90, 0x00
    ];

    private static byte[] CryptoResponse(byte result) =>
        [0x7C, 0x03, 0x82, 0x01, result, 0x90, 0x00];

    private static void AssertNotification(
        RecordingUserPresencePrompt prompt,
        UserPresenceBasis basis,
        PivSlot slot,
        UserPresenceOutcome outcome)
    {
        var request = Assert.Single(prompt.Requests);
        Assert.Equal(basis, request.Context.Basis);
        Assert.Equal("PIV", request.Context.Application);
        Assert.Equal(slot.ToString(), request.Context.Scope);

        var resolution = Assert.Single(prompt.Resolutions);
        Assert.Same(request.Context, resolution.Context);
        Assert.Equal(outcome, resolution.Outcome);
        Assert.Equal(CancellationToken.None, resolution.CancellationToken);
    }

    private sealed class RecordingUserPresencePrompt(Func<int>? getCommandCount = null) : IUserPresencePrompt
    {
        public List<(UserPresenceContext Context, CancellationToken CancellationToken)> Requests { get; } = [];
        public List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken CancellationToken)> Resolutions { get; } = [];
        public int? CommandCountAtRequest { get; private set; }
        public int? CommandCountAtResolution { get; private set; }
        public Action? Requested { get; init; }
        public Exception? RequestException { get; init; }
        public Exception? ResolutionException { get; init; }

        public ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken)
        {
            Requests.Add((context, cancellationToken));
            CommandCountAtRequest = getCommandCount?.Invoke();
            Requested?.Invoke();

            return RequestException is null
                ? default
                : ValueTask.FromException(RequestException);
        }

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolutions.Add((context, outcome, cancellationToken));
            CommandCountAtResolution = getCommandCount?.Invoke();
            return ResolutionException is null
                ? default
                : ValueTask.FromException(ResolutionException);
        }
    }
}