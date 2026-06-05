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
using System.Security.Cryptography;
using System.Text;
using Xunit;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Client.Status;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client.Status;

/// <summary>
/// Phase 5 tests for WebAuthn status streaming APIs.
/// </summary>
public class WebAuthnStatusStreamTests
{
    [Fact(Timeout = 5000)]
    public async Task MakeCredentialStream_HappyPath_EmitsProcessing_ThenFinished()
    {
        // Arrange - Mock backend that returns success without needing PIN/UV
        var mockBackend = Substitute.For<IWebAuthnBackend>();
        var mockInfo = MockFido2Responses.CreateMockAuthenticatorInfo(
            clientPinSupported: false,
            uvSupported: false);
        mockBackend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(mockInfo);
        mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());

        if (!WebAuthnOrigin.TryParse("https://example.com", out var origin))
            throw new InvalidOperationException("Failed to parse origin");

        await using var client = new WebAuthnClient(mockBackend, origin, _ => false);

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(
                RandomNumberGenerator.GetBytes(16),
                "user@example.com",
                "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            UserVerification = UserVerificationPreference.Discouraged
        };

        // Act - Iterate the stream and collect statuses
        var statuses = new List<WebAuthnStatus>();
        await foreach (var status in client.MakeCredentialStreamAsync(options, TestContext.Current.CancellationToken))
        {
            statuses.Add(status);
        }

        // Assert - Sequence pattern: starts with Processing, ends with Finished
        Assert.NotEmpty(statuses);
        Assert.Contains(statuses, s => s is WebAuthnStatusProcessing);
        Assert.Contains(statuses, s => s is WebAuthnStatusFinished<RegistrationResponse>);

        var finished = statuses.OfType<WebAuthnStatusFinished<RegistrationResponse>>().Single();
        Assert.NotNull(finished.Result);
        Assert.False(finished.Result.CredentialId.IsEmpty);
        Assert.NotNull(finished.Result.PublicKey);
    }

    [Fact(Timeout = 5000)]
    public async Task MakeCredentialStream_NoPin_EmitsRequestingPin_AndResumesAfterSubmit()
    {
        // Arrange - Mock backend whose UvDecision wants PIN
        var mockBackend = Substitute.For<IWebAuthnBackend>();
        var mockInfo = MockFido2Responses.CreateMockAuthenticatorInfo(
            clientPinSupported: true,
            uvSupported: false);
        mockBackend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(mockInfo);

        // Mock GetPinUvTokenAsync to capture the submitted PIN
        byte[]? capturedPinBytes = null;
        var mockTokenSession = new PinUvAuthTokenSession(
            new PinUvAuthProtocolV2(),
            RandomNumberGenerator.GetBytes(32));

        mockBackend.GetPinUvTokenAsync(
            Arg.Any<PinUvAuthMethod>(),
            Arg.Any<PinUvAuthTokenPermissions>(),
            Arg.Any<string?>(),
            Arg.Do<ReadOnlyMemory<byte>?>(pin => capturedPinBytes = pin.HasValue ? pin.Value.ToArray() : null),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Returns(mockTokenSession);

        mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());

        if (!WebAuthnOrigin.TryParse("https://example.com", out var origin))
            throw new InvalidOperationException("Failed to parse origin");

        await using var client = new WebAuthnClient(mockBackend, origin, _ => false);

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(
                RandomNumberGenerator.GetBytes(16),
                "user@example.com",
                "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            UserVerification = UserVerificationPreference.Required
        };

        // Act - Iterate stream and respond to RequestingPin
        bool pinRequested = false;
        RegistrationResponse? result = null;

        await foreach (var status in client.MakeCredentialStreamAsync(options, TestContext.Current.CancellationToken))
        {
            switch (status)
            {
                case WebAuthnStatusRequestingPin requestingPin:
                    pinRequested = true;
                    var pinBytes = Encoding.UTF8.GetBytes("123456");
                    await requestingPin.SubmitPin(pinBytes);
                    break;

                case WebAuthnStatusFinished<RegistrationResponse> finished:
                    result = finished.Result;
                    break;
            }
        }

        // Assert
        Assert.True(pinRequested, "RequestingPin should have been emitted");
        Assert.NotNull(result);
        Assert.False(result.CredentialId.IsEmpty);

        // Verify PIN was submitted to backend
        Assert.NotNull(capturedPinBytes);
        var expectedPin = Encoding.UTF8.GetBytes("123456");
        Assert.Equal(expectedPin, capturedPinBytes);
    }

    [Fact(Timeout = 5000)]
    public async Task MakeCredentialStream_DeduplicatesConsecutiveProcessing()
    {
        // Focused unit test on StatusChannel itself to verify deduplication
        // (Full integration would require wiring IProgress<CtapStatus> - Phase 6)

        var channel = new StatusChannel<int>();

        // Act - Write multiple identical Processing statuses, then a Finished
        var writeTask = Task.Run(async () =>
        {
            await channel.WriteAsync(new WebAuthnStatusProcessing(), TestContext.Current.CancellationToken);
            await channel.WriteAsync(new WebAuthnStatusProcessing(), TestContext.Current.CancellationToken); // Should be deduplicated
            await channel.WriteAsync(new WebAuthnStatusProcessing(), TestContext.Current.CancellationToken); // Should be deduplicated
            await channel.WriteAsync(new WebAuthnStatusFinished<int>(42), TestContext.Current.CancellationToken);
            channel.Complete();
        }, TestContext.Current.CancellationToken);

        // Collect statuses from reader
        var statuses = new List<WebAuthnStatus>();
        await foreach (var status in channel.Reader(TestContext.Current.CancellationToken))
        {
            statuses.Add(status);
        }

        await writeTask;

        // Assert - No two adjacent Processing records
        Assert.Equal(2, statuses.Count); // Processing, Finished (duplicates removed)
        Assert.IsType<WebAuthnStatusProcessing>(statuses[0]);
        Assert.IsType<WebAuthnStatusFinished<int>>(statuses[1]);

        // Double-check: no consecutive duplicates
        for (int i = 1; i < statuses.Count; i++)
        {
            Assert.False(
                statuses[i].Equals(statuses[i - 1]),
                $"Found consecutive duplicate statuses at index {i}");
        }
    }

    [Fact(Timeout = 5000)]
    public async Task MakeCredentialDrainConvenience_AutoRespondsWithProvidedPin()
    {
        // Arrange - Backend wants PIN
        var mockBackend = Substitute.For<IWebAuthnBackend>();
        var mockInfo = MockFido2Responses.CreateMockAuthenticatorInfo(
            clientPinSupported: true,
            uvSupported: false);
        mockBackend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(mockInfo);

        byte[]? capturedPinBytes = null;
        var mockTokenSession = new PinUvAuthTokenSession(
            new PinUvAuthProtocolV2(),
            RandomNumberGenerator.GetBytes(32));

        mockBackend.GetPinUvTokenAsync(
            Arg.Any<PinUvAuthMethod>(),
            Arg.Any<PinUvAuthTokenPermissions>(),
            Arg.Any<string?>(),
            Arg.Do<ReadOnlyMemory<byte>?>(pin => capturedPinBytes = pin.HasValue ? pin.Value.ToArray() : null),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Returns(mockTokenSession);

        mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());

        if (!WebAuthnOrigin.TryParse("https://example.com", out var origin))
            throw new InvalidOperationException("Failed to parse origin");

        await using var client = new WebAuthnClient(mockBackend, origin, _ => false);

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(
                RandomNumberGenerator.GetBytes(16),
                "user@example.com",
                "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            UserVerification = UserVerificationPreference.Required
        };

        // Act - Use convenience overload with string PIN
        var result = await client.MakeCredentialAsync(options, pin: "654321", useUv: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.CredentialId.IsEmpty);

        // Verify backend.GetPinUvTokenAsync was called with UTF-8 "654321"
        await mockBackend.Received(1).GetPinUvTokenAsync(
            Arg.Any<PinUvAuthMethod>(),
            Arg.Any<PinUvAuthTokenPermissions>(),
            Arg.Any<string?>(),
            Arg.Any<ReadOnlyMemory<byte>?>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>());

        Assert.NotNull(capturedPinBytes);
        var expectedPin = Encoding.UTF8.GetBytes("654321");
        Assert.Equal(expectedPin, capturedPinBytes);
    }

    [Fact(Timeout = 5000)]
    public async Task MakeCredentialDrainConvenience_NullPinWhenRequired_ThrowsNotAllowed()
    {
        // Arrange - Backend wants PIN
        var mockBackend = Substitute.For<IWebAuthnBackend>();
        var mockInfo = MockFido2Responses.CreateMockAuthenticatorInfo(
            clientPinSupported: true,
            uvSupported: false);
        mockBackend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(mockInfo);

        // Should never reach MakeCredentialAsync
        mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockMakeCredentialResponse());

        if (!WebAuthnOrigin.TryParse("https://example.com", out var origin))
            throw new InvalidOperationException("Failed to parse origin");

        await using var client = new WebAuthnClient(mockBackend, origin, _ => false);

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(
                RandomNumberGenerator.GetBytes(16),
                "user@example.com",
                "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            UserVerification = UserVerificationPreference.Required
        };

        // Act & Assert - Expect WebAuthnClientError with NotAllowed
        var ex = await Assert.ThrowsAsync<WebAuthnClientError>(
            async () => await client.MakeCredentialAsync(options, pin: null, useUv: false, TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, ex.Code);

        // Verify backend.MakeCredentialAsync was NOT invoked
        await mockBackend.DidNotReceive().MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 5000)]
    public async Task MakeCredentialStream_ConsumerBreaks_ProducerCancelledQuickly()
    {
        // Arrange - Mock backend with cancellable long-running operation
        var mockBackend = Substitute.For<IWebAuthnBackend>();
        var mockInfo = MockFido2Responses.CreateMockAuthenticatorInfo(
            clientPinSupported: false,
            uvSupported: false);
        mockBackend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(mockInfo);

        // Track whether MakeCredentialAsync received a cancellation request
        var receivedCancellation = false;
        mockBackend.MakeCredentialAsync(
            Arg.Any<BackendMakeCredentialRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var ct = callInfo.ArgAt<CancellationToken>(2);
                try
                {
                    // Wait indefinitely OR until cancelled
                    await Task.Delay(Timeout.Infinite, ct);
                    return MockFido2Responses.CreateMockMakeCredentialResponse();
                }
                catch (OperationCanceledException)
                {
                    receivedCancellation = true;
                    throw;
                }
            });

        if (!WebAuthnOrigin.TryParse("https://example.com", out var origin))
            throw new InvalidOperationException("Failed to parse origin");

        await using var client = new WebAuthnClient(mockBackend, origin, _ => false);

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
            User = new PublicKeyCredentialUserEntity(
                RandomNumberGenerator.GetBytes(16),
                "user@example.com",
                "User"),
            PubKeyCredParams = [new CoseAlgorithm(-7)],
            UserVerification = UserVerificationPreference.Discouraged
        };

        // Act - Consumer breaks after first Processing status
        var sawProcessing = false;
        await foreach (var status in client.MakeCredentialStreamAsync(options, TestContext.Current.CancellationToken))
        {
            if (status is WebAuthnStatusProcessing)
            {
                sawProcessing = true;
                break; // Consumer breaks early (iterator disposed → linked CTS cancelled)
            }
        }

        // Assert - Consumer saw Processing before breaking
        Assert.True(sawProcessing);

        // Give producer a small window to receive cancellation
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Verify producer received cancellation (not stuck waiting)
        Assert.True(receivedCancellation, "Producer should have received cancellation when consumer broke");
    }
}