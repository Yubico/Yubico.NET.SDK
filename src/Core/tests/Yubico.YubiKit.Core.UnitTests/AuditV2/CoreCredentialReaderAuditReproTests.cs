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

using System.Buffers;
using Yubico.YubiKit.Core.Credentials;

namespace Yubico.YubiKit.Core.UnitTests.AuditV2;

/// <summary>
///     Audit #26 (YESDK-1634) Core credential-reader sub-items g and h.
/// </summary>
public class CoreCredentialReaderAuditReproTests
{
    /// <summary>
    ///     g) Redirected (non-interactive) input must obey the same <see cref="CredentialReaderOptions" />
    ///     policy as interactive input. ForPin() is documented as "6-8 numeric digits".
    /// </summary>
    [Theory]
    [Trait("Audit", "YESDK-1634")]
    [InlineData("abcdef")] // violates CharacterFilter (digits only)
    [InlineData("123")] // below MinLength 6
    [InlineData("123456789")] // above MaxLength 8
    public void YESDK1634_NonInteractivePinViolatingPolicy_IsRejected(string line)
    {
        var console = new MockConsoleInput { IsInteractive = false };
        console.EnqueueLine(line);
        var reader = new ConsoleCredentialReader(console);

        using var result = reader.ReadCredential(CredentialReaderOptions.ForPin(), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>
    ///     h) If the confirmation read is cancelled, the already-read first credential must still be
    ///     disposed (zeroed), per the class contract "Clears intermediate buffers on all code paths".
    ///     The first credential's backing array is identified by pre-seeding this thread's
    ///     <see cref="ArrayPool{T}.Shared" /> slot with a sentinel-filled array.
    /// </summary>
    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public void YESDK1634_ConfirmationCancelled_FirstCredentialIsCleared()
    {
        // Seed: the next Rent(6) on this thread returns this exact array (thread-local bucket 16).
        var seeded = ArrayPool<byte>.Shared.Rent(16);
        seeded.AsSpan().Fill(0xEE);
        ArrayPool<byte>.Shared.Return(seeded);

        var options = CredentialReaderOptions.ForPin();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var console = new CancelOnConfirmPromptConsole(options.ConfirmPrompt, cts);
        console.Inner.EnqueueKeys("123456");
        console.Inner.EnqueueKey(ConsoleKey.Enter);
        var reader = new ConsoleCredentialReader(console);

        Assert.Throws<OperationCanceledException>(
            () => reader.ReadCredentialWithConfirmation(options, cts.Token));

        Assert.False(
            seeded.AsSpan(0, 16).IndexOfAnyExcept((byte)0xEE) < 0,
            "Precondition failed: the first credential did not use the seeded pool array.");
        Assert.True(
            seeded.AsSpan().IndexOfAnyExcept((byte)0) < 0,
            $"First credential buffer was not cleared: {Convert.ToHexString(seeded.AsSpan(0, 8))}");
    }

    private sealed class CancelOnConfirmPromptConsole(string confirmPrompt, CancellationTokenSource cts)
        : IConsoleInputSource
    {
        public MockConsoleInput Inner { get; } = new();

        public bool IsInteractive => true;

        public bool KeyAvailable => Inner.KeyAvailable;

        public ConsoleKeyInfo ReadKey(bool intercept) => Inner.ReadKey(intercept);

        public string? ReadLine() => Inner.ReadLine();

        public void Write(string text)
        {
            if (text == confirmPrompt)
            {
                cts.Cancel();
            }

            Inner.Write(text);
        }

        public void WriteLine(string text) => Inner.WriteLine(text);
    }
}
