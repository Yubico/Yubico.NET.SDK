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
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Client.Status;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client.Status;

/// <summary>
/// The status stream reports ceremony progress. It never gathers input: a PIN comes from the
/// caller's bytes or the configured <see cref="ICredentialPrompt"/>, and abandoning the stream
/// cancels the ceremony rather than stranding it.
/// </summary>
public class WebAuthnStatusStreamTests
{
    private sealed class FixedPrompt(byte[]? pin) : ICredentialPrompt
    {
        public int CallCount { get; private set; }

        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(
            CredentialPromptContext context, CancellationToken cancellationToken)
        {
            CallCount++;
            return ValueTask.FromResult<IMemoryOwner<byte>?>(
                pin is null ? null : DisposableArrayPoolBuffer.CreateFromSpan(pin));
        }
    }

    /// <summary>Worst-case prompt: never answers and ignores the token it is handed.</summary>
    private sealed class StuckPrompt : ICredentialPrompt
    {
        private readonly TaskCompletionSource<IMemoryOwner<byte>?> _never = new();

        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(
            CredentialPromptContext context, CancellationToken cancellationToken) => new(_never.Task);
    }

    private static WebAuthnOrigin Origin()
    {
        if (!WebAuthnOrigin.TryParse("https://example.com", out var origin))
            throw new InvalidOperationException("Failed to parse origin");
        return origin;
    }

    private static RegistrationOptions RegOptions(
        UserVerificationPreference uv = UserVerificationPreference.Discouraged) => new()
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(
            RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            UserVerification = uv
        };

    private static AuthenticationOptions AuthOptions() => new()
    {
        Challenge = RandomNumberGenerator.GetBytes(32),
        RpId = "example.com"
    };

    private static IWebAuthnBackend CreateBackend(bool pinSupported = false)
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockAuthenticatorInfo(
                clientPinSupported: pinSupported, uvSupported: false));

        backend.GetPinUvTokenAsync(
                Arg.Any<PinUvAuthMethod>(), Arg.Any<PinUvAuthTokenPermissions>(), Arg.Any<string?>(),
                Arg.Any<ReadOnlyMemory<byte>?>(), Arg.Any<IProgress<CtapStatus>?>(), Arg.Any<CancellationToken>())
            .Returns(new PinUvAuthTokenSession(new PinUvAuthProtocolV2(), RandomNumberGenerator.GetBytes(32)));

        backend.MakeCredentialAsync(
                Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<IProgress<CtapStatus>?>(), Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());

        return backend;
    }

    [Fact(Timeout = 10000)]
    public async Task MakeCredentialStream_HappyPath_EmitsProcessing_ThenFinished()
    {
        await using var client = new WebAuthnClient(CreateBackend(), Origin(), _ => false);

        var statuses = new List<WebAuthnStatus>();
        await foreach (var status in client.MakeCredentialStreamAsync(
            RegOptions(), cancellationToken: TestContext.Current.CancellationToken))
        {
            statuses.Add(status);
        }

        Assert.Contains(statuses, s => s is WebAuthnStatusProcessing);
        var finished = statuses.OfType<WebAuthnStatusFinished<RegistrationResponse>>().Single();
        Assert.False(finished.Result.CredentialId.IsEmpty);
    }

    [Fact(Timeout = 10000)]
    public async Task GetAssertionStream_NoMatchingCredentials_ReachesFinishedWithEmptyList()
    {
        var backend = CreateBackend();
        backend.GetAssertionAsync(
                Arg.Any<BackendGetAssertionRequest>(), Arg.Any<IProgress<CtapStatus>?>(), Arg.Any<CancellationToken>())
            .Returns<GetAssertionResponse>(_ => throw new CtapException(CtapStatus.NoCredentials));

        await using var client = new WebAuthnClient(backend, Origin(), _ => false);

        var statuses = new List<WebAuthnStatus>();
        await foreach (var status in client.GetAssertionStreamAsync(
            AuthOptions(), cancellationToken: TestContext.Current.CancellationToken))
        {
            statuses.Add(status);
        }

        // An empty match is a successful ceremony, not a failure.
        var finished = statuses.OfType<WebAuthnStatusFinished<IReadOnlyList<MatchedCredential>>>().Single();
        Assert.Empty(finished.Result);
    }

    [Fact(Timeout = 10000)]
    public async Task Stream_WhenAuthenticatorAwaitsTouch_EmitsWaitingForUser()
    {
        var backend = CreateBackend();
        backend.MakeCredentialAsync(
                Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<IProgress<CtapStatus>?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                // Mirrors a CTAP HID keep-alive reporting that a touch is awaited.
                callInfo.ArgAt<IProgress<CtapStatus>?>(1)?.Report(CtapStatus.UserActionPending);
                return MockFido2Responses.CreateMockMakeCredentialResponse();
            });

        await using var client = new WebAuthnClient(backend, Origin(), _ => false);

        var statuses = new List<WebAuthnStatus>();
        await foreach (var status in client.MakeCredentialStreamAsync(
            RegOptions(), cancellationToken: TestContext.Current.CancellationToken))
        {
            statuses.Add(status);
        }

        Assert.Contains(statuses, s => s is WebAuthnStatusWaitingForUser);
    }

    [Fact(Timeout = 10000)]
    public async Task Stream_WhenBackendFails_EmitsFailedRatherThanThrowing()
    {
        var backend = CreateBackend();
        backend.MakeCredentialAsync(
                Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<IProgress<CtapStatus>?>(), Arg.Any<CancellationToken>())
            .Returns<MakeCredentialResponse>(_ => throw new CtapException(CtapStatus.OperationDenied));

        await using var client = new WebAuthnClient(backend, Origin(), _ => false);

        var statuses = new List<WebAuthnStatus>();
        await foreach (var status in client.MakeCredentialStreamAsync(
            RegOptions(), cancellationToken: TestContext.Current.CancellationToken))
        {
            statuses.Add(status);
        }

        Assert.Contains(statuses, s => s is WebAuthnStatusFailed);
    }

    [Fact(Timeout = 10000)]
    public async Task Stream_WhenPinNeeded_PromptSuppliesItWithoutAnyStatusInteraction()
    {
        var prompt = new FixedPrompt(Encoding.UTF8.GetBytes("123456"));
        await using var client = new WebAuthnClient(
            CreateBackend(pinSupported: true), Origin(), _ => false, prompt: prompt);

        var statuses = new List<WebAuthnStatus>();
        await foreach (var status in client.MakeCredentialStreamAsync(
            RegOptions(UserVerificationPreference.Required),
            cancellationToken: TestContext.Current.CancellationToken))
        {
            statuses.Add(status);
        }

        Assert.Equal(1, prompt.CallCount);
        Assert.Contains(statuses, s => s is WebAuthnStatusFinished<RegistrationResponse>);
    }

    [Fact(Timeout = 10000)]
    public async Task Stream_WhenPinNeededAndNoPromptConfigured_EmitsFailed()
    {
        await using var client = new WebAuthnClient(
            CreateBackend(pinSupported: true), Origin(), _ => false);

        var statuses = new List<WebAuthnStatus>();
        await foreach (var status in client.MakeCredentialStreamAsync(
            RegOptions(UserVerificationPreference.Required),
            cancellationToken: TestContext.Current.CancellationToken))
        {
            statuses.Add(status);
        }

        var failed = statuses.OfType<WebAuthnStatusFailed>().Single();
        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, failed.Error.Code);
    }

    /// <summary>
    /// Regression for the deadlock that the interactive-status design allowed: a consumer that
    /// abandons the stream while the ceremony waits on a prompt must not hang on disposal.
    /// </summary>
    [Fact(Timeout = 20000)]
    public async Task Stream_ConsumerBreaksWhileWaitingOnPrompt_DisposalCompletes()
    {
        await using var client = new WebAuthnClient(
            CreateBackend(pinSupported: true), Origin(), _ => false, prompt: new StuckPrompt());

        var enumeration = Task.Run(async () =>
        {
            await foreach (var status in client.MakeCredentialStreamAsync(
                RegOptions(UserVerificationPreference.Required), cancellationToken: CancellationToken.None))
            {
                if (status is WebAuthnStatusProcessing)
                {
                    await Task.Delay(300);
                    break;
                }
            }
        }, TestContext.Current.CancellationToken);

        var completed = await Task.WhenAny(
            enumeration, Task.Delay(10000, TestContext.Current.CancellationToken));

        Assert.True(ReferenceEquals(enumeration, completed),
            "Abandoning the stream while a prompt is outstanding must not deadlock disposal.");
        await enumeration;
    }

    /// <summary>
    /// Regression for the harsher variant: a well-behaved consumer keeps enumerating and cancels
    /// its own token. Cancellation must reach the pending prompt.
    /// </summary>
    [Fact(Timeout = 20000)]
    public async Task Stream_ExternalCancellationWhileWaitingOnPrompt_Terminates()
    {
        await using var client = new WebAuthnClient(
            CreateBackend(pinSupported: true), Origin(), _ => false, prompt: new StuckPrompt());

        using var cts = new CancellationTokenSource();

        var enumeration = Task.Run(async () =>
        {
            try
            {
                await foreach (var status in client.MakeCredentialStreamAsync(
                    RegOptions(UserVerificationPreference.Required), cancellationToken: cts.Token))
                {
                    if (status is WebAuthnStatusProcessing)
                    {
                        cts.CancelAfter(300);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Acceptable terminal outcome.
            }
        }, TestContext.Current.CancellationToken);

        var completed = await Task.WhenAny(
            enumeration, Task.Delay(10000, TestContext.Current.CancellationToken));

        Assert.True(ReferenceEquals(enumeration, completed),
            "External cancellation must release a ceremony parked on a prompt.");
        await enumeration;
    }

    [Fact(Timeout = 10000)]
    public async Task Stream_ReEnumeration_StartsANewCeremony()
    {
        var backend = CreateBackend();
        await using var client = new WebAuthnClient(backend, Origin(), _ => false);

        var stream = client.MakeCredentialStreamAsync(
            RegOptions(), cancellationToken: TestContext.Current.CancellationToken);

        await foreach (var _ in stream) { }
        await foreach (var _ in stream) { }

        // Standard IAsyncEnumerable semantics: each enumeration re-runs the operation.
        await backend.Received(2).MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>());
    }
}