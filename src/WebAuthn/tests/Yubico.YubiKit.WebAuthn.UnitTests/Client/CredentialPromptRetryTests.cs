// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using NSubstitute;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

/// <summary>
/// Covers the SDK-owned PIN retry loop: the SDK re-prompts after a rejected PIN,
/// never resubmits a cached secret, surfaces retries-remaining, and stops on
/// blocked/declined/attempt-cap.
/// </summary>
public class CredentialPromptRetryTests
{
    /// <summary>Prompt that returns a scripted sequence of answers.</summary>
    private sealed class ScriptedPrompt : ICredentialPrompt
    {
        private readonly Queue<byte[]?> _answers;

        public ScriptedPrompt(params byte[]?[] answers) => _answers = new Queue<byte[]?>(answers);

        public List<CredentialPromptContext> Contexts { get; } = [];

        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(
            CredentialPromptContext context, CancellationToken cancellationToken)
        {
            Contexts.Add(context);
            var answer = _answers.Count > 0 ? _answers.Dequeue() : null;
            return ValueTask.FromResult<IMemoryOwner<byte>?>(
                answer is null ? null : DisposableArrayPoolBuffer.CreateFromSpan(answer));
        }
    }

    private static readonly byte[] CorrectPin = Encoding.UTF8.GetBytes("123456");
    private static readonly byte[] WrongPin = Encoding.UTF8.GetBytes("000000");

    /// <summary>
    /// Backend whose GetPinUvToken rejects any PIN that is not <see cref="CorrectPin"/>
    /// with the supplied status, and reports a fixed retries-remaining count.
    /// </summary>
    private static IWebAuthnBackend CreateBackend(
        CtapStatus rejectionStatus = CtapStatus.PinInvalid,
        int? retriesRemaining = 7,
        List<byte[]?>? submittedPins = null)
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo(
                clientPinSupported: true, uvSupported: false));

        backend.GetPinRetriesAsync(Arg.Any<CancellationToken>()).Returns(retriesRemaining);

        backend.GetPinUvTokenAsync(
                Arg.Any<PinUvAuthMethod>(),
                Arg.Any<PinUvAuthTokenPermissions>(),
                Arg.Any<string?>(),
                Arg.Any<ReadOnlyMemory<byte>?>(),
                Arg.Any<IProgress<CtapStatus>?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var pin = callInfo.ArgAt<ReadOnlyMemory<byte>?>(3);
                submittedPins?.Add(pin?.ToArray());

                if (pin is null || !pin.Value.Span.SequenceEqual(CorrectPin))
                {
                    throw new CtapException(rejectionStatus);
                }

                return Task.FromResult(new PinUvAuthTokenSession(
                    new PinUvAuthProtocolV2(), RandomNumberGenerator.GetBytes(32)));
            });

        backend.MakeCredentialAsync(
                Arg.Any<BackendMakeCredentialRequest>(),
                Arg.Any<IProgress<CtapStatus>?>(),
                Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());

        return backend;
    }

    private static RegistrationOptions CreateOptions() => new()
    {
        Challenge = RandomNumberGenerator.GetBytes(32),
        Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
        User = new PublicKeyCredentialUserEntity(
            RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
        PubKeyCredParams = [new CoseAlgorithm(-7)],
        UserVerification = UserVerificationPreference.Required
    };

    private static WebAuthnOrigin Origin()
    {
        if (!WebAuthnOrigin.TryParse("https://example.com", out var origin))
            throw new InvalidOperationException("Failed to parse origin");
        return origin;
    }

    [Fact(Timeout = 10000)]
    public async Task WrongPinThenCorrect_RePromptsWithRetryContext_ThenSucceeds()
    {
        var prompt = new ScriptedPrompt(WrongPin, CorrectPin);
        await using var client = new WebAuthnClient(
            CreateBackend(), Origin(), _ => false, prompt: prompt);

        var result = await client.MakeCredentialAsync(
            CreateOptions(), pinBytes: null, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(2, prompt.Contexts.Count);

        Assert.False(prompt.Contexts[0].IsRetry);
        Assert.Equal(CredentialKind.Pin, prompt.Contexts[0].Kind);
        Assert.Equal("example.com", prompt.Contexts[0].Scope);

        Assert.True(prompt.Contexts[1].IsRetry);
        Assert.Equal(7, prompt.Contexts[1].RetriesRemaining);
    }

    [Fact(Timeout = 10000)]
    public async Task NeverResubmitsCachedSecret_EachAttemptComesFromAFreshPrompt()
    {
        var submitted = new List<byte[]?>();
        var prompt = new ScriptedPrompt(WrongPin, CorrectPin);
        await using var client = new WebAuthnClient(
            CreateBackend(submittedPins: submitted), Origin(), _ => false, prompt: prompt);

        await client.MakeCredentialAsync(
            CreateOptions(), pinBytes: null, TestContext.Current.CancellationToken);

        // Exactly one submission per prompt answer - no silent replay of the rejected PIN.
        Assert.Equal(prompt.Contexts.Count, submitted.Count);
        Assert.Equal(WrongPin, submitted[0]);
        Assert.Equal(CorrectPin, submitted[1]);
    }

    [Fact(Timeout = 10000)]
    public async Task PromptDeclinesOnRetry_ThrowsNotAllowed()
    {
        var prompt = new ScriptedPrompt(WrongPin, null);
        await using var client = new WebAuthnClient(
            CreateBackend(), Origin(), _ => false, prompt: prompt);

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            client.MakeCredentialAsync(CreateOptions(), pinBytes: null, TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, error.Code);
        Assert.Equal(2, prompt.Contexts.Count);
    }

    [Fact(Timeout = 10000)]
    public async Task PinBlocked_StopsImmediately_WithoutRePrompting()
    {
        var prompt = new ScriptedPrompt(WrongPin, CorrectPin, CorrectPin);
        await using var client = new WebAuthnClient(
            CreateBackend(CtapStatus.PinBlocked), Origin(), _ => false, prompt: prompt);

        await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            client.MakeCredentialAsync(CreateOptions(), pinBytes: null, TestContext.Current.CancellationToken));

        Assert.Single(prompt.Contexts);
    }

    [Fact(Timeout = 10000)]
    public async Task PinAuthInvalid_DoesNotRetry_PreservingPinAttempts()
    {
        var prompt = new ScriptedPrompt(WrongPin, CorrectPin, CorrectPin);
        await using var client = new WebAuthnClient(
            CreateBackend(CtapStatus.PinAuthInvalid), Origin(), _ => false, prompt: prompt);

        await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            client.MakeCredentialAsync(CreateOptions(), pinBytes: null, TestContext.Current.CancellationToken));

        Assert.Single(prompt.Contexts);
    }

    [Fact(Timeout = 10000)]
    public async Task RepeatedWrongPin_StopsAtAttemptCap()
    {
        // A prompt that always answers wrongly must not burn every hardware attempt.
        var prompt = new ScriptedPrompt(WrongPin, WrongPin, WrongPin, WrongPin, WrongPin, WrongPin);
        await using var client = new WebAuthnClient(
            CreateBackend(), Origin(), _ => false, prompt: prompt);

        await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            client.MakeCredentialAsync(CreateOptions(), pinBytes: null, TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClient.MaxPromptAttempts, prompt.Contexts.Count);
    }

    [Fact(Timeout = 10000)]
    public async Task ExplicitPinBytes_PromptNeverInvoked()
    {
        var prompt = new ScriptedPrompt(CorrectPin);
        await using var client = new WebAuthnClient(
            CreateBackend(), Origin(), _ => false, prompt: prompt);

        var result = await client.MakeCredentialAsync(
            CreateOptions(), CorrectPin, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Empty(prompt.Contexts);
    }

    [Fact(Timeout = 10000)]
    public async Task ExplicitWrongPinBytes_DoesNotRetryViaPrompt()
    {
        // A caller-supplied secret is the caller's to retry; the SDK must not
        // silently substitute a prompt for it.
        var prompt = new ScriptedPrompt(CorrectPin);
        await using var client = new WebAuthnClient(
            CreateBackend(), Origin(), _ => false, prompt: prompt);

        await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            client.MakeCredentialAsync(CreateOptions(), WrongPin, TestContext.Current.CancellationToken));

        Assert.Empty(prompt.Contexts);
    }
}
