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

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backends;
using Yubico.YubiKit.Piv.Cryptography;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.Cryptography;

/// <summary>
/// Security regression tests: <c>ParseCryptoResponse</c> (used by both <c>SignOrDecryptAsync</c>/
/// <c>DecryptAsync</c> and <c>CalculateSecretAsync</c>) copies the needed TLV value out of the raw
/// AUTHENTICATE response but must also zero the original response buffer, since it carries the raw
/// decrypted plaintext / ECDH shared secret in the clear. DecryptAsync also clears its owned
/// rawDecrypted intermediate after host unpadding. Transferred mutable output belongs to the caller.
/// </summary>
public class PrivateKeyOperationsTests
{
    [Fact]
    public async Task ResolveAndTransferCryptoResultAsync_SuccessTransfersOwnedBufferWithoutCopyingOrClearing()
    {
        byte[] source = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        byte[] sourceSnapshot = (byte[])source.Clone();

        var result = await PrivateKeyOperations.ResolveAndTransferCryptoResultAsync(source, UserPresenceNotification.None);
        Assert.Same(source, result);
        Assert.Equal(sourceSnapshot, result);
        // Ownership has transferred; only the receiver clears this mutable result now.
        CryptographicOperations.ZeroMemory(result);
        Assert.All(source, b => Assert.Equal(0, b));
    }

    [Theory]
    [InlineData("7C0A82083006020101020102")]
    [InlineData("7C82000C828200083006020101020102")]
    public async Task SignOrDecryptAsync_ZeroesOriginalResponseBuffer(string encoded)
    {
        // 7C LL [ 82 LL2 <DER signature> ].
        byte[] payload = Convert.FromHexString("3006020101020102");
        var backend = new CryptoCapturingBackend(Convert.FromHexString(encoded));

        var result = await PrivateKeyOperations.SignOrDecryptAsync(
            backend,

            Slot.Authentication,
            KeyType.P256,
            new byte[32],
            UserPresenceNotification.None,
            cancellationToken: TestContext.Current.CancellationToken);

        // Sanity: the returned (distinct, correctly-unwrapped) copy still has the real data.
        Assert.Equal(payload, result.ToArray());

        Assert.NotNull(backend.CapturedRawDataArray);
        Assert.All(
            backend.CapturedRawDataArray!.AsSpan(backend.CapturedOffset, backend.CapturedCount).ToArray(),
            b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task CalculateSecretAsync_ZeroesOriginalResponseBuffer()
    {
        // Simulates an ECDH response: 7C LL [ 82 LL2 <raw shared secret> ].
        byte[] sharedSecret = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        byte[] inner = [0x82, (byte)sharedSecret.Length, .. sharedSecret];
        byte[] wrapped = [0x7C, (byte)inner.Length, .. inner];

        var backend = new CryptoCapturingBackend(wrapped);

        using var peer = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var peerPublicKey = Yubico.YubiKit.Core.Cryptography.ECPublicKey.CreateFromParameters(peer.PublicKey.ExportParameters());

        var result = await PrivateKeyOperations.CalculateSecretAsync(
            backend,

            Slot.KeyManagement,
            peerPublicKey,
            UserPresenceNotification.None,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(sharedSecret, result.ToArray());

        Assert.NotNull(backend.CapturedRawDataArray);
        Assert.All(
            backend.CapturedRawDataArray!.AsSpan(backend.CapturedOffset, backend.CapturedCount).ToArray(),
            b => Assert.Equal(0, b));
    }

    [Theory]
    [InlineData("7C0A820830060201010201025300")]
    [InlineData("7C0C820830060201010201025300")]
    public async Task SignOrDecryptAsync_RejectsSuffixAndClearsRawOwner(string encoded)
    {
        var backend = new CryptoCapturingBackend(Convert.FromHexString(encoded));
        await Assert.ThrowsAsync<ApduException>(() => PrivateKeyOperations.SignOrDecryptAsync(
            backend, Slot.Authentication, KeyType.P256, new byte[32], UserPresenceNotification.None,
            TestContext.Current.CancellationToken));

        Assert.NotNull(backend.CapturedRawDataArray);
        Assert.All(backend.CapturedRawDataArray.AsSpan(backend.CapturedOffset, backend.CapturedCount).ToArray(),
            value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task SignOrDecryptAsync_WhenResolutionThrows_ZeroesCommandAndResponseBuffers()
    {
        byte[] payload = Convert.FromHexString("3006020101020102");
        byte[] inner = [0x82, (byte)payload.Length, .. payload];
        byte[] wrapped = [0x7C, (byte)inner.Length, .. inner];
        var backend = new CryptoCapturingBackend(wrapped);
        var expected = new InvalidOperationException("resolution failed");
        UserPresenceNotification notification = UserPresenceNotification.Create(
            new ThrowingResolutionPrompt(expected),
            CreateContext());

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PrivateKeyOperations.SignOrDecryptAsync(
                backend,

                Slot.Authentication,
                KeyType.P256,
                new byte[32],
                notification,
                TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.All(backend.CapturedCommandDataArray!, b => Assert.Equal(0, b));
        Assert.All(
            backend.CapturedRawDataArray!
                .AsSpan(backend.CapturedOffset, backend.CapturedCount)
                .ToArray(),
            b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task CalculateSecretAsync_WhenResolutionThrows_ZeroesCommandAndResponseBuffers()
    {
        byte[] sharedSecret = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        byte[] inner = [0x82, (byte)sharedSecret.Length, .. sharedSecret];
        byte[] wrapped = [0x7C, (byte)inner.Length, .. inner];
        var backend = new CryptoCapturingBackend(wrapped);
        var expected = new InvalidOperationException("resolution failed");
        UserPresenceNotification notification = UserPresenceNotification.Create(
            new ThrowingResolutionPrompt(expected),
            CreateContext());
        using var peer = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var peerPublicKey = Yubico.YubiKit.Core.Cryptography.ECPublicKey.CreateFromParameters(
            peer.PublicKey.ExportParameters());

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PrivateKeyOperations.CalculateSecretAsync(
                backend,

                Slot.KeyManagement,
                peerPublicKey,
                notification,
                TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.All(backend.CapturedCommandDataArray!, b => Assert.Equal(0, b));
        Assert.All(
            backend.CapturedRawDataArray!
                .AsSpan(backend.CapturedOffset, backend.CapturedCount)
                .ToArray(),
            b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task ResolveAndTransferCryptoResultAsync_WhenResolutionThrows_ZeroesParsedResult()
    {
        byte[] parsedResult = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        var expected = new InvalidOperationException("resolution failed");
        UserPresenceNotification notification = UserPresenceNotification.Create(
            new ThrowingResolutionPrompt(expected),
            CreateContext());
        await notification.RequestAsync(TestContext.Current.CancellationToken);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PrivateKeyOperations.ResolveAndTransferCryptoResultAsync(parsedResult, notification));

        Assert.Same(expected, actual);
        Assert.All(parsedResult, value => Assert.Equal(0, value));
    }

    [Theory]
    [InlineData(KeyType.Rsa1024, 128)]
    [InlineData(KeyType.Rsa2048, 256)]
    [InlineData(KeyType.Rsa3072, 384)]
    [InlineData(KeyType.Rsa4096, 512)]
    [InlineData(KeyType.Ed25519, 64)]
    public async Task SignOrDecryptAsync_RejectsShortAndOversizedResults(KeyType keyType, int width)
    {
        byte[] input = keyType == KeyType.Ed25519 ? "message"u8.ToArray() : new byte[width];
        await AssertWrongResultWidthsAsync(width, $"Invalid PIV sign/decrypt result for {keyType}.",
            (backend, notification, token) => PrivateKeyOperations.SignOrDecryptAsync(
                backend, Slot.Authentication, keyType, input, notification, token));
    }

    [Theory]
    [InlineData(KeyType.P256, 32)]
    [InlineData(KeyType.P384, 48)]
    [InlineData(KeyType.X25519, 32)]
    public async Task CalculateSecretAsync_RejectsShortAndOversizedResults(KeyType keyType, int width)
    {
        using var peer = keyType == KeyType.X25519 ? null : ECDiffieHellman.Create(
            keyType == KeyType.P256 ? ECCurve.NamedCurves.nistP256 : ECCurve.NamedCurves.nistP384);
        byte[] x25519BasePoint = new byte[32];
        x25519BasePoint[0] = 9;
        IPublicKey publicKey = peer is not null
            ? ECPublicKey.CreateFromParameters(peer.ExportParameters(false))
            : Curve25519PublicKey.CreateFromValue(x25519BasePoint, KeyType.X25519);
        await AssertWrongResultWidthsAsync(width, $"Invalid PIV agreement result width for {keyType}.",
            (backend, notification, token) => PrivateKeyOperations.CalculateSecretAsync(
                backend, Slot.KeyManagement, publicKey, notification, token));
    }

    private static async Task AssertWrongResultWidthsAsync(int width, string message,
        Func<IBackend, UserPresenceNotification, CancellationToken, Task<byte[]>> operation)
    {
        var token = TestContext.Current.CancellationToken;
        await using var connection = new RecordingSmartCardConnection([0x90, 0x00],
            [0, 0, 1, 0x90, 0x00], [1, 1, 3, 2, 2, 0, 1, 5, 1, 1, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: token);
        foreach (int invalidWidth in new[] { width - 1, width + 1 })
        {
            var payload = new byte[invalidWidth];
            payload.AsSpan().Fill(0xA5);
            using var inner = new Tlv(0x82, payload.AsSpan());
            using var outer = new Tlv(0x7C, inner.AsSpan());
            byte[] encoded = outer.AsSpan().ToArray();
            var backend = new CryptoCapturingBackend(encoded);
            var prompt = new WidthTestPrompt();
            var notification = UserPresenceNotification.Create(prompt, CreateContext());

            // PivSession uses this production wrapper to resolve failures from the APDU helper.
            var error = await Assert.ThrowsAsync<ApduException>(() => session.RunWithUserPresenceResolutionAsync(
                notification, ct => operation(backend, notification, ct), token));

            Assert.Equal(message, error.Message);
            Assert.Equal(UserPresenceOutcome.Failed, Assert.Single(prompt.Outcomes));
            Assert.NotNull(backend.CapturedRawDataArray);
            Assert.Equal(encoded.Length + 2, backend.CapturedCount);
            Assert.True(backend.CapturedRawDataArray.AsSpan(backend.CapturedOffset, backend.CapturedCount).IndexOfAnyExcept((byte)0) < 0);
        }
    }

    private sealed class WidthTestPrompt : IUserPresencePrompt
    {
        public List<UserPresenceOutcome> Outcomes { get; } = [];

        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) => default;

        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome, CancellationToken cancellationToken)
        {
            Outcomes.Add(outcome);
            return default;
        }
    }

    private static UserPresenceContext CreateContext() => new()
    {
        Basis = UserPresenceBasis.PolicyRequires,
        Application = "PIV",
        Scope = Slot.Authentication.ToString()
    };

    private sealed class CryptoCapturingBackend(byte[] responseWithoutSw) : IBackend
    {
        public byte[]? CapturedCommandDataArray { get; private set; }

        public byte[]? CapturedRawDataArray { get; private set; }

        public int CapturedOffset { get; private set; }

        public int CapturedCount { get; private set; }

        public Task<Initialization> ReadInitialStateAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not needed for these tests.");

        public Task<ApduResponse> SendAsync(
            ApduCommand command,
            bool throwOnError = true,
            CancellationToken cancellationToken = default)
        {
            Assert.True(MemoryMarshal.TryGetArray(command.Data, out var commandSegment));
            CapturedCommandDataArray = commandSegment.Array;

            var response = new ApduResponse(responseWithoutSw, unchecked((short)0x9000));

            Assert.True(MemoryMarshal.TryGetArray(response.RawData, out var segment));
            CapturedRawDataArray = segment.Array;
            CapturedOffset = segment.Offset;
            CapturedCount = segment.Count;

            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingResolutionPrompt(Exception exception) : IUserPresencePrompt
    {
        public ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken) => default;

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken) => ValueTask.FromException(exception);
    }
}