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

using NSubstitute;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Commands;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

namespace Yubico.YubiKit.Piv.UnitTests;

public class PivToolTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RetryInfoCancellation_ReachesCliCancellationExit(bool cancelPinQuery)
    {
        var session = Substitute.For<IPivSession>();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        source.Cancel();
        var token = source.Token;
        session.GetPinAttemptsAsync(token).Returns(cancelPinQuery ? Task.FromCanceled<int>(token) : Task.FromResult(2));
        session.GetPukMetadataAsync(token).Returns(Task.FromCanceled<PukMetadata>(token));
        using var errors = new StringWriter();

        int exitCode = await CliRunner.RunWithCancellationExitAsync(async () =>
        {
            var result = await DeviceInfoQuery.GetPivRetryInfoAsync(session, token);
            return result.Success ? 0 : 1;
        }, errors);

        Assert.Equal(130, exitCode);
        Assert.Contains("Operation cancelled.", errors.ToString());
        await session.Received(cancelPinQuery ? 0 : 1).GetPukMetadataAsync(token);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RetryInfoNonCancellationFailure_PreservesAvailableCounter(bool pinQueryFails)
    {
        var session = Substitute.For<IPivSession>();
        var token = TestContext.Current.CancellationToken;
        session.GetPinAttemptsAsync(token).Returns(pinQueryFails
            ? Task.FromException<int>(new InvalidOperationException("PIN counter unavailable"))
            : Task.FromResult(2));
        session.GetPukMetadataAsync(token).Returns(pinQueryFails
            ? Task.FromResult(new PukMetadata(false, 3, 1))
            : Task.FromException<PukMetadata>(new NotSupportedException()));

        var result = await DeviceInfoQuery.GetPivRetryInfoAsync(session, token);

        Assert.True(result.Success);
        Assert.Equal(pinQueryFails ? (int?)null : 2, result.PinRetriesRemaining);
        Assert.Equal(pinQueryFails ? 1 : (int?)null, result.PukRetriesRemaining);
    }

    [Theory]
    [InlineData(false, null, true, null)]
    [InlineData(true, null, false, null)]
    [InlineData(true, "unsupported", false, null)]
    [InlineData(true, "SHA384", true, "SHA384")]
    public void SigningHashFlag_DistinguishesOmittedFromInvalid(bool present, string? value, bool valid, string? name)
    {
        var flags = new Dictionary<string, string?>();
        if (present)
            flags["hash"] = value;

        Assert.Equal(valid, DeviceHelper.TryParseSigningHash(flags, out var hash));
        Assert.Equal(name, hash.Name);
    }

    [Fact]
    public async Task RsaSigning_VerifiesAgainstOriginalMessage()
    {
        using var key = RSA.Create(2048);
        var parameters = key.ExportParameters(true);
        var modulus = new BigInteger(parameters.Modulus, isUnsigned: true, isBigEndian: true);
        var exponent = new BigInteger(parameters.D, isUnsigned: true, isBigEndian: true);
        var token = TestContext.Current.CancellationToken;
        var session = SigningSession(KeyType.Rsa2048, token);
        session.SignOrDecryptAsync(Slot.Signature, KeyType.Rsa2048, Arg.Any<ReadOnlyMemory<byte>>(), token)
            .Returns(call =>
            {
                var input = new BigInteger(call.ArgAt<ReadOnlyMemory<byte>>(2).Span, isUnsigned: true, isBigEndian: true);
                byte[] raw = BigInteger.ModPow(input, exponent, modulus).ToByteArray(isUnsigned: true, isBigEndian: true);
                byte[] signature = new byte[256];
                raw.CopyTo(signature.AsSpan(256 - raw.Length));
                return (Memory<byte>)signature;
            });
        byte[] message = "PivTool signs this original message"u8.ToArray();

        var result = await Signing.SignDataAsync(session, Slot.Signature, message, HashAlgorithmName.SHA256, token);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.True(key.VerifyData(message, result.Signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [Fact]
    public async Task Ed25519Signing_PassesRawMessageAndRejectsExplicitHash()
    {
        // RFC 8032 §7.1, test 2: the fake signs only the original one-byte message.
        byte[] signature = Convert.FromHexString("92A009A9F0D4CAB8720E820B5F642540A2B27B5416503F8FB3762223EBDB69DA085AC1E43E15996E458F3613D0F11D8C387B2EAEB4302AEEB00D291612BB0C00");
        var token = TestContext.Current.CancellationToken;
        var session = SigningSession(KeyType.Ed25519, token);
        session.SignOrDecryptAsync(Slot.Signature, KeyType.Ed25519, Arg.Any<ReadOnlyMemory<byte>>(), token)
            .Returns(call =>
            {
                Assert.Equal(new byte[] { 0x72 }, call.ArgAt<ReadOnlyMemory<byte>>(2).ToArray());
                return (Memory<byte>)signature;
            });
        byte[] message = [0x72];

        var result = await Signing.SignDataAsync(session, Slot.Signature, message, default, token);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(signature, result.Signature.ToArray());

        var rejected = await Signing.SignDataAsync(session, Slot.Signature, message, HashAlgorithmName.SHA256, token);
        Assert.False(rejected.Success);
        await session.Received(1).SignOrDecryptAsync(Slot.Signature, KeyType.Ed25519, Arg.Any<ReadOnlyMemory<byte>>(), token);
    }

    private static IPivSession SigningSession(KeyType type, CancellationToken token)
    {
        var session = Substitute.For<IPivSession>();
        session.GetSlotMetadataAsync(Slot.Signature, token)
            .Returns(new SlotMetadata(type, PinPolicy.Default, TouchPolicy.Default, true, ReadOnlyMemory<byte>.Empty));
        return session;
    }

    [Fact]
    public async Task ResetCancellation_ReachesCliCancellationExit()
    {
        var session = Substitute.For<IPivSession>();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        source.Cancel();
        session.ResetAsync(source.Token).Returns(Task.FromCanceled(source.Token));
        using var errors = new StringWriter();

        int exitCode = await CliRunner.RunWithCancellationExitAsync(async () =>
        {
            var result = await Reset.ResetPivApplicationAsync(session, source.Token);
            return result.Success ? 0 : 1;
        }, errors);

        Assert.Equal(130, exitCode);
        Assert.Contains("Operation cancelled.", errors.ToString());
    }

    [Fact]
    public async Task ResetConfirmationCancellation_ExitsWhileInputIsBlocked()
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        budget.CancelAfter(TimeSpan.FromSeconds(10));
        using var signal = CancellationTokenSource.CreateLinkedTokenSource(budget.Token);
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reader = Substitute.For<TextReader>();
        reader.ReadLine().Returns(_ =>
        {
            started.TrySetResult();
            try
            {
                release.Wait(budget.Token);
                return "not confirmed";
            }
            finally
            {
                finished.TrySetResult();
            }
        });
        using var errors = new StringWriter();
        var run = CliRunner.RunWithCancellationExitAsync(() => CliRunner.RunAsync(["reset"], signal.Token, reader), errors);
        try
        {
            await started.Task.WaitAsync(budget.Token);
            signal.Cancel();
            Assert.Equal(130, await run.WaitAsync(budget.Token));
            Assert.False(finished.Task.IsCompleted);
            Assert.Contains("Operation cancelled.", errors.ToString());
        }
        finally
        {
            release.Set();
            // Independent bounded drain: the fake must release its reader before disposal.
            await run.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            if (started.Task.IsCompleted)
                await finished.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SlotOverview_DisposesPartialResultsButTransfersSuccessfulCertificates(bool failLater)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var certificate = new CertificateRequest("CN=PIV unit test", key, HashAlgorithmName.SHA256)
            .CreateSelfSigned(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1));
        var session = Substitute.For<IPivSession>();
        var token = TestContext.Current.CancellationToken;
        session.GetCertificateAsync(Slot.Authentication, token).Returns(certificate);
        if (failLater)
            session.GetCertificateAsync(Slot.Signature, token)
                .Returns(Task.FromException<X509Certificate2?>(new InvalidOperationException("later slot failed")));

        var result = await SlotInfoQuery.GetAllSlotsInfoAsync(session, token);

        Assert.Equal(!failLater, result.Success);
        if (failLater)
        {
            Assert.Empty(result.Slots);
            Assert.ThrowsAny<CryptographicException>(() => _ = certificate.RawData);
        }
        else
        {
            Assert.Same(certificate, result.Slots[0].Certificate);
            Assert.NotEmpty(certificate.RawData);
        }
    }
}