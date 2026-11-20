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

using System.Reflection;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Oath.Backend;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Oath.UnitTests;

public sealed class OathResponseTests
{
    private static readonly byte[] Salt = [1, 2, 3, 4];
    private static byte[] Select() => [0x79, 3, 5, 7, 0, 0x71, 4, .. Salt, 0x90, 0];
    private static ApduResponse Response(byte[] raw)
    {
        try { return new ApduResponse(raw); }
        finally { CryptographicOperations.ZeroMemory(raw); }
    }

    [Theory]
    [InlineData(0x6984)]
    [InlineData(0x6A80)]
    public async Task ValidateAsync_DeviceRejection_PreservesStatusWithoutClaimingIncorrectPassword(int status)
    {
        var connection = new RecordingSmartCardConnection(
            [.. Select()[..^2], 0x74, 8, 1, 2, 3, 4, 5, 6, 7, 8, 0x90, 0],
            [(byte)(status >> 8), (byte)status]);
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        byte[] key = new byte[16];
        try
        {
            var failure = await Assert.ThrowsAsync<OathException>(() => session.ValidateAsync(key, TestContext.Current.CancellationToken));
            Assert.Equal(unchecked((short)status), failure.StatusWord);
            Assert.Equal(unchecked((short)status), Assert.IsAssignableFrom<ApduException>(failure.InnerException).SW);
            Assert.DoesNotContain("password is incorrect", failure.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            foreach (byte[] command in connection.TransmittedCommands)
                CryptographicOperations.ZeroMemory(command);
        }
    }

    [Theory]
    [InlineData(20)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(48)] // Structure must not be limited to the currently modeled algorithms.
    public async Task CalculateAsync_FullResponse_ReturnsOnlyOwnedHmac(int length)
    {
        byte[] hmac = new byte[length];
        Array.Fill(hmac, (byte)0xA5);
        var backend = new ResponseBackend(_ => Response([0x75, (byte)(length + 1), 6, .. hmac, 0x90, 0]));
        await using var session = await CreateAsync(backend);
        var credential = CredentialFor(session);
        byte[] challenge = [1, 2, 3];
        ReadOnlyMemory<byte> result = await session.CalculateAsync(credential, challenge, TestContext.Current.CancellationToken);
        try
        {
            Assert.True(CryptographicOperations.FixedTimeEquals(hmac, result.Span));
            Assert.Equal(new byte[] { 1, 2, 3 }, challenge);
            AssertCleared(backend.LastResponse);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(System.Runtime.InteropServices.MemoryMarshal.AsMemory(result).Span);
            CryptographicOperations.ZeroMemory(hmac);
        }
    }

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 6 })]
    public async Task CalculateAsync_MissingHeaderOrHmac_ResolvesFailed(byte[] value)
    {
        var backend = new ResponseBackend(_ => Response([0x75, (byte)value.Length, .. value, 0x90, 0]));
        var prompt = new PresencePrompt();
        await using var session = await CreateAsync(backend, prompt: prompt);

        await Assert.ThrowsAsync<BadResponseException>(() => session.CalculateAsync(
            CredentialFor(session, touch: true), new byte[8], TestContext.Current.CancellationToken));

        Assert.Equal(UserPresenceOutcome.Failed, Assert.Single(prompt.Outcomes));
        AssertCleared(backend.LastResponse);
    }

    [Theory]
    [InlineData("success")]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("mismatch")]
    public async Task ValidateAsync_AllResponsePaths_ClearOriginalWithoutClearingCallerKey(string mode)
    {
        byte[] key = new byte[16];
        Array.Fill(key, (byte)0xA5);
        var backend = new ResponseBackend(command =>
        {
            using var tlvs = TlvHelper.DecodeList(command.Data.Span);
            byte[] hmac = HMACSHA1.HashData(key, tlvs.Single(tlv => tlv.Tag == 0x74).Value.Span);
            try
            {
                return Response(mode switch
                {
                    "missing" => [0x78, 1, 1, 0x90, 0],
                    "malformed" => [0x75, 20, 1, 0x90, 0],
                    "mismatch" => [0x75, 20, .. new byte[20], 0x90, 0],
                    _ => [0x75, 20, .. hmac, 0x90, 0]
                });
            }
            finally { CryptographicOperations.ZeroMemory(hmac); }
        });
        await using var session = await CreateAsync(backend, locked: true);
        try
        {
            if (mode == "success")
            {
                await session.ValidateAsync(key, TestContext.Current.CancellationToken);
                Assert.False(session.IsLocked);
            }
            else if (mode == "mismatch")
            {
                await Assert.ThrowsAsync<BadResponseException>(() => session.ValidateAsync(key, TestContext.Current.CancellationToken));
                Assert.True(session.IsLocked);
            }
            else
            {
                await Assert.ThrowsAnyAsync<Exception>(() => session.ValidateAsync(key, TestContext.Current.CancellationToken));
                Assert.True(session.IsLocked);
            }
            Assert.All(key, value => Assert.Equal(0xA5, value));
            AssertCleared(backend.LastResponse);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [Theory]
    [InlineData(new byte[] { 0x72, 0 })]
    [InlineData(new byte[] { 0x72, 1, 0x21 })]
    public async Task ListCredentialsAsync_MissingRequiredEntryData_ThrowsBadResponse(byte[] data)
    {
        var backend = new ResponseBackend(_ => Response([.. data, 0x90, 0]));
        await using var session = await CreateAsync(backend);
        await Assert.ThrowsAsync<BadResponseException>(() => session.ListCredentialsAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(new byte[] { 0x71, 1, 0x61 })]
    [InlineData(new byte[] { 0x76, 5, 6, 0, 0, 0, 1 })]
    [InlineData(new byte[] { 0x71, 0, 0x77, 0 })]
    [InlineData(new byte[] { 0x71, 1, 0x61, 0x71, 1, 0x62 })]
    [InlineData(new byte[] { 0x71, 1, 0x61, 0x72, 2, 0x21, 0x62 })]
    public async Task CalculateAllAsync_IncompleteEntry_ThrowsAndClearsOriginal(byte[] data)
    {
        var backend = new ResponseBackend(_ => Response([.. data, 0x90, 0]));
        await using var session = await CreateAsync(backend);
        await Assert.ThrowsAsync<BadResponseException>(() => session.CalculateAllAsync(30, TestContext.Current.CancellationToken));
        AssertCleared(backend.LastResponse);
    }

    [Theory]
    [InlineData(new byte[] { 0x75, 0 })]
    [InlineData(new byte[] { 0x75, 1, 6 })]
    [InlineData(new byte[] { 0x73, 1, 1 })]
    [InlineData(new byte[] { 0x74, 1, 1 })]
    [InlineData(new byte[] { 0x78, 1, 1 })]
    [InlineData(new byte[] { 0x79, 3, 5, 7, 0 })]
    [InlineData(new byte[] { 0x7A, 4, 0, 0, 0, 0 })]
    [InlineData(new byte[] { 0x7B, 1, 1 })]
    public async Task CalculateAllAsync_KnownNonTruncatedResult_RejectsAndClearsOriginal(byte[] value)
    {
        var backend = new ResponseBackend(_ => Response([0x71, 1, 0x61, .. value, 0x90, 0]));
        await using var session = await CreateAsync(backend);
        await Assert.ThrowsAsync<BadResponseException>(() => session.CalculateAllAsync(30, TestContext.Current.CancellationToken));
        AssertCleared(backend.LastResponse);
    }

    [Theory]
    [InlineData(new byte[] { 0x79, 3, 5, 7, 0 })]
    [InlineData(new byte[] { 0x7B, 1, 1 })]
    public async Task CalculateAllAsync_MisplacedKnownMetadata_RejectsAndClearsOriginal(byte[] value)
    {
        var backend = new ResponseBackend(_ => Response([.. value, 0x90, 0]));
        await using var session = await CreateAsync(backend);
        await Assert.ThrowsAsync<BadResponseException>(() => session.CalculateAllAsync(30, TestContext.Current.CancellationToken));
        AssertCleared(backend.LastResponse);
    }

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 6, 0, 0, 1 })]
    [InlineData(new byte[] { 9, 0, 0, 0, 1 })]
    public async Task CalculateAllAsync_InvalidPeriodCannotHideMalformedWireValue(byte[] value)
    {
        var backend = new ResponseBackend(_ => Response([0x71, 3, .. "0/a"u8,
            0x76, (byte)value.Length, .. value, 0x90, 0]));
        await using var session = await CreateAsync(backend);
        await Assert.ThrowsAsync<BadResponseException>(() => session.CalculateAllAsync(30, TestContext.Current.CancellationToken));
        Assert.Equal(1, backend.SendCount);
        AssertCleared(backend.LastResponse);
    }

    [Fact]
    public async Task CalculateAllAsync_InvalidPeriodWithValidWireValue_IsStillOmitted()
    {
        var backend = new ResponseBackend(_ => Response([0x71, 3, .. "0/a"u8,
            0x76, 5, 6, 0, 0, 0, 1, 0x90, 0]));
        await using var session = await CreateAsync(backend);
        Assert.Empty(await session.CalculateAllAsync(30, TestContext.Current.CancellationToken));
        Assert.Equal(1, backend.SendCount);
        AssertCleared(backend.LastResponse);
    }

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 6 })] // Official CALCULATE ALL example: 77 01 06.
    public async Task CalculateAllAsync_HotpMarkerWithOptionalDigitValue_RemainsAHotpEntry(byte[] value)
    {
        var backend = new ResponseBackend(_ => Response([0x71, 1, 0x61,
            0x77, (byte)value.Length, .. value, 0x90, 0]));
        await using var session = await CreateAsync(backend);
        var result = Assert.Single(await session.CalculateAllAsync(30, TestContext.Current.CancellationToken));
        Assert.Equal(OathType.Hotp, result.Key.OathType);
        Assert.False(result.Key.TouchRequired);
        Assert.Null(result.Value);
        AssertCleared(backend.LastResponse);
    }

    [Theory]
    [InlineData("success")]
    [InlineData("malformed")]
    [InlineData("followup-failure")]
    [InlineData("followup-cancelled")]
    [InlineData("followup-success")]
    public async Task CalculateAllAsync_ParsingAndFollowupPaths_ClearOriginal(string mode)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        byte[] id = mode.StartsWith("followup", StringComparison.Ordinal) ? "60/a"u8.ToArray() : "a"u8.ToArray();
        var aggregate = Response([0x71, (byte)id.Length, .. id,
            0x76, 5, mode == "malformed" ? (byte)9 : (byte)6, 0, 0, 0, 1, 0x90, 0]);
        var backend = new ResponseBackend(command =>
        {
            if (command.Ins != OathConstants.InsCalculateAll)
                return mode == "followup-success"
                    ? Response([0x76, 5, 6, 0, 0, 0, 2, 0x90, 0])
                    : throw new InvalidOperationException("followup failed");
            if (mode == "followup-cancelled")
                cancellation.Cancel();
            return aggregate;
        });
        await using var session = await CreateAsync(backend);
        if (mode == "success")
        {
            var result = await session.CalculateAllAsync(30, cancellation.Token);
            Assert.Equal("000001", Assert.Single(result).Value?.Value);
        }
        else if (mode == "followup-success")
        {
            var result = await session.CalculateAllAsync(30, cancellation.Token);
            Assert.Equal("000002", Assert.Single(result).Value?.Value);
            Assert.Equal(2, backend.SendCount);
            AssertCleared(backend.LastResponse);
        }
        else if (mode == "followup-cancelled")
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.CalculateAllAsync(30, cancellation.Token));
            Assert.Equal(1, backend.SendCount);
        }
        else if (mode == "malformed")
            await Assert.ThrowsAsync<BadResponseException>(() => session.CalculateAllAsync(30, cancellation.Token));
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.CalculateAllAsync(30, cancellation.Token));
        AssertCleared(aggregate);
    }

    [Fact]
    public async Task CalculateAllAsync_InvalidCustomPeriodValue_DoesNotRecalculate()
    {
        var backend = new ResponseBackend(_ => Response([0x71, 4, .. "60/a"u8, 0x76, 0, 0x90, 0]));
        await using var session = await CreateAsync(backend);
        await Assert.ThrowsAsync<BadResponseException>(() => session.CalculateAllAsync(30, TestContext.Current.CancellationToken));
        Assert.Equal(1, backend.SendCount);
        AssertCleared(backend.LastResponse);
    }

    [Fact]
    public async Task CalculateAllAsync_UnknownExtensionBetweenEntries_DoesNotHideKnownResults()
    {
        var backend = new ResponseBackend(_ => Response([
            0x7D, 1, 0xAA,
            0x71, 1, 0x61, 0x76, 5, 6, 0, 0, 0, 1,
            0x7D, 0,
            0x71, 1, 0x62, 0x77, 0, 0x90, 0]));
        await using var session = await CreateAsync(backend);
        var results = await session.CalculateAllAsync(30, TestContext.Current.CancellationToken);
        Assert.Equal(2, results.Count);
        Assert.Equal("000001", results.Single(item => item.Key.Name == "a").Value?.Value);
        Assert.Null(results.Single(item => item.Key.Name == "b").Value);
        AssertCleared(backend.LastResponse);
    }

    [Fact]
    public async Task ListAndAggregate_EmptySuccessfulResponses_RepresentNoCredentials()
    {
        var backend = new ResponseBackend(_ => Response([0x90, 0]));
        await using var session = await CreateAsync(backend);
        Assert.Empty(await session.ListCredentialsAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await session.CalculateAllAsync(30, TestContext.Current.CancellationToken));
        AssertCleared(backend.LastResponse);
    }

    [Fact]
    public async Task ListCredentialsAsync_ExtensionsAndFutureType_PreserveRawIdentity()
    {
        var backend = new ResponseBackend(_ => Response([0x7D, 1, 1, 0x72, 2, 0x4F, 0x61, 0x90, 0]));
        await using var session = await CreateAsync(backend);
        var credential = Assert.Single(await session.ListCredentialsAsync(TestContext.Current.CancellationToken));
        Assert.Equal((OathType)0x40, credential.OathType);
        Assert.Equal("a"u8.ToArray(), credential.Id.ToArray());
    }

    [Fact]
    public async Task CreateAsync_SelectExtensionsAndVariableIdAndChallenge_AcceptsRequiredMetadata()
    {
        byte[] raw = [0x7D, 1, 1, 0x79, 3, 5, 7, 0, 0x71, 1, 1, 0x74, 1, 1, 0x90, 0];
        await using var session = await OathSession.CreateAsync(new RecordingSmartCardConnection(raw),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(session.IsLocked);
        Assert.Equal(new byte[] { 1 }, session.Salt.ToArray());
    }

    [Fact]
    public async Task DuplicateResponseFields_RetainExistingLastValueBehavior()
    {
        byte[] raw = [.. Select()[..^2], 0x71, 1, 0xBB, 0x90, 0];
        var connection = new RecordingSmartCardConnection(raw,
            [0x72, 2, 0x21, 0x61, 0x72, 2, 0x21, 0x61, 0x90, 0],
            [0x71, 1, 0x61, 0x76, 5, 6, 0, 0, 0, 1,
             0x71, 1, 0x61, 0x76, 5, 6, 0, 0, 0, 2, 0x90, 0]);
        await using var session = await OathSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(new byte[] { 0xBB }, session.Salt.ToArray());
        Assert.Equal(2, (await session.ListCredentialsAsync(TestContext.Current.CancellationToken)).Count);
        var result = Assert.Single(await session.CalculateAllAsync(30, TestContext.Current.CancellationToken));
        Assert.Equal("000002", result.Value?.Value);
    }

    [Fact]
    public async Task CalculateAllAsync_UnknownResultTag_RetainsExistingNullResultAndIdentity()
    {
        var backend = new ResponseBackend(_ => Response([0x71, 1, 0x61, 0x7D, 1, 0xAA, 0x90, 0]));
        await using var session = await CreateAsync(backend);
        var result = Assert.Single(await session.CalculateAllAsync(30, TestContext.Current.CancellationToken));
        Assert.Equal("a"u8.ToArray(), result.Key.Id.ToArray());
        Assert.Null(result.Value);
        AssertCleared(backend.LastResponse);
    }

    [Theory]
    [InlineData(new byte[] { 0x78, 0 })]
    [InlineData(new byte[] { 0x75, 20, 1 })]
    public async Task CalculateAsync_MissingOrMalformedTlv_ClearsOriginalAndResolvesFailed(byte[] data)
    {
        var backend = new ResponseBackend(_ => Response([.. data, 0x90, 0]));
        var prompt = new PresencePrompt();
        await using var session = await CreateAsync(backend, prompt: prompt);
        await Assert.ThrowsAnyAsync<Exception>(() => session.CalculateAsync(
            CredentialFor(session, touch: true), new byte[8], TestContext.Current.CancellationToken));
        Assert.Equal(UserPresenceOutcome.Failed, Assert.Single(prompt.Outcomes));
        AssertCleared(backend.LastResponse);
    }

    [Fact]
    public async Task CalculateAsync_ResolutionFailure_ClearsOriginalWithoutPublishingOutput()
    {
        var backend = new ResponseBackend(_ => Response([0x75, 21, 6, .. new byte[20], 0x90, 0]));
        var expected = new InvalidOperationException("resolution failed");
        var prompt = new PresencePrompt(expected);
        await using var session = await CreateAsync(backend, prompt: prompt);
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => session.CalculateAsync(
            CredentialFor(session, touch: true), new byte[8], TestContext.Current.CancellationToken));
        Assert.Same(expected, actual);
        AssertCleared(backend.LastResponse);
    }

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 0x71, 1, 1 })]
    [InlineData(new byte[] { 0x79, 3, 5, 7, 0 })]
    [InlineData(new byte[] { 0x79, 2, 5, 7, 0x71, 1, 1 })]
    [InlineData(new byte[] { 0x79, 3, 5, 7, 0, 0x71, 0 })]
    [InlineData(new byte[] { 0x79, 3, 5, 7, 0, 0x71, 1, 1, 0x74, 0 })]
    public async Task CreateAsync_MissingRequiredSelectMetadata_FailsWithoutClaimingConnection(byte[] data)
    {
        var connection = new RecordingSmartCardConnection([.. data, 0x90, 0], Select());
        await Assert.ThrowsAsync<BadResponseException>(() => OathSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken));
        await using var retry = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(retry.IsInitialized);
        Assert.Equal(Salt, retry.Salt.ToArray());
    }

    private static Credential CredentialFor(OathSession session, bool touch = false) =>
        new(session.DeviceId, "a"u8.ToArray(), null, "a", OathType.Totp, 30, touch);

    private static async Task<OathSession> CreateAsync(ResponseBackend backend, bool locked = false, PresencePrompt? prompt = null)
    {
        byte[] select = locked ? [.. Select()[..^2], 0x74, 8, 1, 2, 3, 4, 5, 6, 7, 8, 0x90, 0] : Select();
        var session = await OathSession.CreateAsync(new RecordingSmartCardConnection(select),
            new SessionCreationOptions { UserPresencePrompt = prompt }, TestContext.Current.CancellationToken);
        // Observe the response actually owned by the session through its existing backend seam.
        // Transport fakes cannot do this: ApduResponse deliberately copies transport storage.
        var field = typeof(OathSession).GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("OATH backend seam was not found.");
        field.SetValue(session, backend);
        return session;
    }

    private static void AssertCleared(ApduResponse? response)
    {
        Assert.NotNull(response);
        Assert.All(response.RawData.ToArray(), value => Assert.Equal(0, value));
    }

    private sealed class ResponseBackend(Func<ApduCommand, ApduResponse> respond) : IOathBackend
    {
        public ApduResponse? LastResponse { get; private set; }
        public int SendCount { get; private set; }
        public Task<OathInitialization> ReadInitialStateAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<ApduResponse> SendAsync(ApduCommand command, bool throwOnError = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SendCount++;
            LastResponse = respond(command);
            return Task.FromResult(LastResponse);
        }
    }

    private sealed class PresencePrompt(Exception? resolutionFailure = null) : IUserPresencePrompt
    {
        public List<UserPresenceOutcome> Outcomes { get; } = [];
        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome, CancellationToken cancellationToken)
        {
            Outcomes.Add(outcome);
            return resolutionFailure is null ? ValueTask.CompletedTask : ValueTask.FromException(resolutionFailure);
        }
    }
}