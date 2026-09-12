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

using System.Text;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.YubiHsm.UnitTests;

public class TouchNotificationTests
{
    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenCredentialRequiresTouch_RequestsBeforeCalculateAndResolvesCompleted()
    {
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x01, counter: 8),
            SessionKeyResponse());
        var prompt = new RecordingUserPresencePrompt(() => connection.TransmittedCommands.Count);
        await using var session = await CreateSessionAsync(connection, prompt, new FirmwareVersion(5, 4, 3));

        using var keys = await CalculateSymmetricAsync(session, TestContext.Current.CancellationToken);

        Assert.Equal(Sequence(0xA0, 16), keys.SEnc.ToArray());
        Assert.Equal(2, prompt.CommandCountAtRequest);
        Assert.Equal(0x03, connection.TransmittedCommands[^1][1]);
        AssertNotification(prompt, UserPresenceBasis.PolicyRequires, UserPresenceOutcome.Completed);
    }

    [Fact]
    public async Task CalculateSessionKeysAsymmetricAsync_WhenCredentialRequiresTouch_RequestsAndResolvesCompleted()
    {
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.EcP256YubicoAuthentication, touchByte: 0x01, counter: 3),
            SessionKeyResponse());
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await CreateSessionAsync(connection, prompt, new FirmwareVersion(5, 6, 0));

        using var keys = await session.CalculateSessionKeysAsymmetricAsync(
            "cred",
            Sequence(0x40, 130),
            Sequence(0x60, 65),
            "pass"u8.ToArray(),
            Sequence(0x70, 8),
            TestContext.Current.CancellationToken);

        Assert.Equal(Sequence(0xA0, 16), keys.SEnc.ToArray());
        AssertNotification(prompt, UserPresenceBasis.PolicyRequires, UserPresenceOutcome.Completed);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenCredentialDoesNotRequireTouch_RemainsSilent()
    {
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x00, counter: 8),
            SessionKeyResponse());
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await CreateSessionAsync(connection, prompt, new FirmwareVersion(5, 4, 3));

        using var keys = await CalculateSymmetricAsync(session, TestContext.Current.CancellationToken);

        Assert.Empty(prompt.Requests);
        Assert.Empty(prompt.Resolutions);
        Assert.Equal(3, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WithoutPrompt_DoesNotListCredentials()
    {
        var connection = CreateInitializedConnection(SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 4, 3) },
            TestContext.Current.CancellationToken);

        using var keys = await CalculateSymmetricAsync(session, TestContext.Current.CancellationToken);

        Assert.Equal(2, connection.TransmittedCommands.Count);
        Assert.Equal(0x03, connection.TransmittedCommands[^1][1]);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenCredentialTouchPolicyIsUnknown_RequestsConservatively()
    {
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x02, counter: 8),
            SessionKeyResponse());
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await CreateSessionAsync(connection, prompt, new FirmwareVersion(5, 4, 3));

        using var keys = await CalculateSymmetricAsync(session, TestContext.Current.CancellationToken);

        AssertNotification(prompt, UserPresenceBasis.PolicyMayRequire, UserPresenceOutcome.Completed);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenCredentialIsMissing_RemainsSilent()
    {
        var connection = CreateInitializedConnection(
            ListResponse("other", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x01, counter: 8),
            SessionKeyResponse());
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await CreateSessionAsync(connection, prompt, new FirmwareVersion(5, 4, 3));

        using var keys = await CalculateSymmetricAsync(session, TestContext.Current.CancellationToken);

        Assert.Empty(prompt.Requests);
        Assert.Empty(prompt.Resolutions);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenListFails_RequestsConservatively()
    {
        var connection = CreateInitializedConnection([0x6A, 0x80], SessionKeyResponse());
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await CreateSessionAsync(connection, prompt, new FirmwareVersion(5, 4, 3));

        using var keys = await CalculateSymmetricAsync(session, TestContext.Current.CancellationToken);

        AssertNotification(prompt, UserPresenceBasis.PolicyMayRequire, UserPresenceOutcome.Completed);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenCalculateFails_ResolvesFailed()
    {
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x01, counter: 8),
            [0x69, 0x82]);
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await CreateSessionAsync(connection, prompt, new FirmwareVersion(5, 4, 3));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            CalculateSymmetricAsync(session, TestContext.Current.CancellationToken));

        AssertNotification(prompt, UserPresenceBasis.PolicyRequires, UserPresenceOutcome.Failed);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenPromptThrows_DoesNotCalculateOrResolve()
    {
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x01, counter: 8),
            SessionKeyResponse());
        var prompt = new RecordingUserPresencePrompt
        {
            RequestException = new InvalidOperationException("prompt failed")
        };
        await using var session = await CreateSessionAsync(connection, prompt, new FirmwareVersion(5, 4, 3));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CalculateSymmetricAsync(session, TestContext.Current.CancellationToken));

        Assert.Equal("prompt failed", exception.Message);
        Assert.Equal(2, connection.TransmittedCommands.Count);
        Assert.Single(prompt.Requests);
        Assert.Empty(prompt.Resolutions);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenCancelledAfterRequest_ResolvesCancelledWithoutCalculating()
    {
        using var cancellationSource = new CancellationTokenSource();
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x01, counter: 8),
            SessionKeyResponse());
        var prompt = new RecordingUserPresencePrompt
        {
            Requested = cancellationSource.Cancel
        };
        await using var session = await CreateSessionAsync(connection, prompt, new FirmwareVersion(5, 4, 3));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CalculateSymmetricAsync(session, cancellationSource.Token));

        Assert.Equal(2, connection.TransmittedCommands.Count);
        AssertNotification(prompt, UserPresenceBasis.PolicyRequires, UserPresenceOutcome.Cancelled);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenResolutionFailsAfterSuccess_PropagatesResolutionException()
    {
        var expected = new InvalidOperationException("resolution failed");
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x01, counter: 8),
            SessionKeyResponse());
        var prompt = new RecordingUserPresencePrompt { ResolutionException = expected };
        await using var session = await CreateSessionAsync(connection, prompt, new FirmwareVersion(5, 4, 3));

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CalculateSymmetricAsync(session, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task CalculateSessionKeysSymmetricAsync_WhenCalculateAndResolutionFail_PreservesCalculateException()
    {
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.Aes128YubicoAuthentication, touchByte: 0x01, counter: 8),
            [0x69, 0x82]);
        var prompt = new RecordingUserPresencePrompt
        {
            ResolutionException = new InvalidOperationException("resolution failed")
        };
        await using var session = await CreateSessionAsync(connection, prompt, new FirmwareVersion(5, 4, 3));

        ApduException actual = await Assert.ThrowsAsync<ApduException>(() =>
            CalculateSymmetricAsync(session, TestContext.Current.CancellationToken));

        Assert.Equal((short)0x6982, actual.SW);
        Assert.Single(prompt.Resolutions);
    }

    private static Task<HsmAuthSession> CreateSessionAsync(
        RecordingSmartCardConnection connection,
        IUserPresencePrompt prompt,
        FirmwareVersion firmwareVersion) =>
        HsmAuthSession.CreateAsync(
            connection,
            new SessionCreationOptions
            {
                FirmwareVersionOverride = firmwareVersion,
                UserPresencePrompt = prompt
            },
            TestContext.Current.CancellationToken);

    private static Task<SessionKeys> CalculateSymmetricAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken) =>
        session.CalculateSessionKeysSymmetricAsync(
            "cred",
            Sequence(0x40, 16),
            "pass"u8.ToArray(),
            cancellationToken: cancellationToken);

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), .. trailingResponses]);

    private static byte[] OkResponse() => [0x90, 0x00];

    private static byte[] Sequence(byte start, int length)
    {
        var bytes = new byte[length];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)(start + i);

        return bytes;
    }

    private static byte[] SessionKeyResponse() =>
    [
        .. Sequence(0xA0, 16),
        .. Sequence(0xB0, 16),
        .. Sequence(0xC0, 16),
        0x90, 0x00
    ];

    private static byte[] ListResponse(string label, HsmAuthAlgorithm algorithm, byte touchByte, byte counter)
    {
        var labelBytes = Encoding.UTF8.GetBytes(label);
        byte[] value = [(byte)algorithm, touchByte, .. labelBytes, counter];

        return [0x72, (byte)value.Length, .. value, 0x90, 0x00];
    }

    private static void AssertNotification(
        RecordingUserPresencePrompt prompt,
        UserPresenceBasis basis,
        UserPresenceOutcome outcome)
    {
        var request = Assert.Single(prompt.Requests);
        Assert.Equal(basis, request.Context.Basis);
        Assert.Equal("YubiHSM Auth", request.Context.Application);
        Assert.Equal("cred", request.Context.Scope);

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
            return ResolutionException is null
                ? default
                : ValueTask.FromException(ResolutionException);
        }
    }
}