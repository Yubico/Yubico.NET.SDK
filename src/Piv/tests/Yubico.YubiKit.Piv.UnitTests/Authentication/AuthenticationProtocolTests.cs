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
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Piv.Authentication;
using Yubico.YubiKit.Piv.Backends;

namespace Yubico.YubiKit.Piv.UnitTests.Authentication;

public class AuthenticationProtocolTests
{
    public enum AuthenticationOutcome
    {
        Success,
        WitnessRejected,
        ChallengeRejected,
        ResponseMismatch,
        ChallengeCancelled
    }

    public static TheoryData<KeyType, AuthenticationOutcome> CleanupCases => new(
        from keyType in new[] { KeyType.TripleDes, KeyType.Aes128, KeyType.Aes192, KeyType.Aes256 }
        from outcome in Enum.GetValues<AuthenticationOutcome>()
        select (keyType, outcome));

    [Theory]
    [MemberData(nameof(CleanupCases))]
    public async Task AuthenticateAsync_WhenCompletedOrFailed_ClearsUsedRangesBeforeReturnAndSurvivesPoolReuse(
        KeyType keyType, AuthenticationOutcome outcome)
    {
        byte[] key = Enumerable.Range(1, keyType.GetKeyInfo().KeySizeBytes).Select(value => (byte)value).ToArray();
        int blockLength = keyType == KeyType.TripleDes ? 8 : 16;
        var pool = new ReusingAuthenticationPool();
        int[] expectedReturnLengths = outcome switch
        {
            AuthenticationOutcome.WitnessRejected => [key.Length],
            AuthenticationOutcome.ChallengeRejected or AuthenticationOutcome.ChallengeCancelled =>
                [blockLength, blockLength, 6 + (2 * blockLength), key.Length],
            _ => [blockLength, blockLength, 6 + (2 * blockLength), blockLength, key.Length]
        };

        try
        {
            // Repeat the real exchange using exactly the returned storage, overwritten by the pool.
            // A late observation of command.Data would see the sentinel, not the cleanup result.
            for (int attempt = 0; attempt < 2; attempt++)
            {
                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
                using var backend = new AuthenticationBackend(key, keyType, blockLength, outcome, cancellation);
                var authentication = AuthenticationProtocol.AuthenticateAsync(
                    backend, keyType, key, cancellation.Token, pool);
                if (outcome == AuthenticationOutcome.Success)
                    await authentication;
                else if (outcome == AuthenticationOutcome.ChallengeCancelled)
                    await Assert.ThrowsAsync<OperationCanceledException>(() => authentication);
                else
                {
                    var exception = await Assert.ThrowsAsync<ApduException>(() => authentication);
                    if (outcome != AuthenticationOutcome.ResponseMismatch)
                        Assert.True(exception.SW == 0x6982);
                }

                pool.AssertReturnedCleared(expectedReturnLengths);
                backend.AssertResponsesCleared();
                Assert.Equal(outcome == AuthenticationOutcome.WitnessRejected ? 0 : 1, backend.WitnessSubmissions);
                Assert.Equal(Enumerable.Range(1, key.Length).Select(value => (byte)value), key);
            }

            Assert.Equal(expectedReturnLengths.Length, pool.ReusedRentCount);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private sealed class ReusingAuthenticationPool : ArrayPool<byte>
    {
        private const byte Sentinel = 0xA5;
        private readonly Dictionary<byte[], int> _rented = [];
        private readonly List<(byte[] Buffer, int Length)> _available = [];
        private readonly List<(int Length, bool Cleared, bool TailUnchanged)> _returns = [];

        public int ReusedRentCount { get; private set; }

        public override byte[] Rent(int minimumLength)
        {
            int index = _available.FindIndex(entry => entry.Length == minimumLength);
            byte[] buffer;
            if (index >= 0)
            {
                buffer = _available[index].Buffer;
                _available.RemoveAt(index);
                ReusedRentCount++;
            }
            else
            {
                buffer = new byte[minimumLength + 7];
                buffer.AsSpan().Fill(Sentinel);
            }
            _rented.Add(buffer, minimumLength);
            return buffer;
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
            Assert.False(clearArray); // The protocol must zero its used range itself, before Return.
            Assert.True(_rented.Remove(array, out int length));
            // Record only observations made while ownership transfers to the pool. Do not retain
            // sensitive snapshots or assert against memory after another consumer can rent it.
            _returns.Add((length,
                array.AsSpan(0, length).IndexOfAnyExcept((byte)0) < 0,
                array.AsSpan(length).IndexOfAnyExcept(Sentinel) < 0));
            array.AsSpan().Fill(Sentinel);
            _available.Add((array, length));
        }

        public void AssertReturnedCleared(int[] expectedLengths)
        {
            Assert.Empty(_rented);
            Assert.Equal(expectedLengths, _returns.Select(entry => entry.Length));
            Assert.All(_returns, entry =>
            {
                Assert.True(entry.Cleared, $"The {entry.Length}-byte used range was not zero at Return.");
                Assert.True(entry.TailUnchanged, "Cleanup changed bytes outside the used range.");
            });
            Assert.All(_available, entry =>
                Assert.True(entry.Buffer.AsSpan().IndexOfAnyExcept(Sentinel) < 0));
            _returns.Clear();
        }
    }

    private sealed class AuthenticationBackend(
        byte[] key, KeyType keyType, int blockLength, AuthenticationOutcome outcome, CancellationTokenSource cancellation) : IBackend, IDisposable
    {
        private readonly byte[] _witness = Enumerable.Repeat((byte)0x3C, blockLength).ToArray();
        private readonly List<ApduResponse> _responses = [];

        public int WitnessSubmissions { get; private set; }

        public Task<Initialization> ReadInitialStateAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ApduResponse> SendAsync(ApduCommand command, bool throwOnError = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(0x87, command.Ins);
            Assert.Equal(KeyTypeEncoding.EncodeManagement(keyType), command.P1);
            Assert.Equal(0x9B, command.P2);
            Assert.False(throwOnError);

            if (command.Data.Length == 4)
            {
                Assert.Equal(new byte[] { 0x7C, 0x02, 0x80, 0x00 }, command.Data.ToArray());
                return Task.FromResult(outcome == AuthenticationOutcome.WitnessRejected
                    ? StatusRejection()
                    : EncryptedResponse(0x80, _witness));
            }

            WitnessSubmissions++;
            ReadOnlySpan<byte> payload = command.Data.Span;
            Assert.Equal(6 + (2 * blockLength), payload.Length);
            Assert.Equal(new byte[] { 0x7C, (byte)(payload.Length - 2), 0x80, (byte)blockLength }, payload[..4].ToArray());
            Assert.True(CryptographicOperations.FixedTimeEquals(_witness, payload.Slice(4, blockLength)));
            Assert.Equal(0x81, payload[4 + blockLength]);
            Assert.Equal(blockLength, payload[5 + blockLength]);

            if (outcome == AuthenticationOutcome.ChallengeCancelled)
            {
                cancellation.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }
            if (outcome == AuthenticationOutcome.ChallengeRejected)
                return Task.FromResult(StatusRejection());

            return Task.FromResult(EncryptedResponse(0x82, payload[(6 + blockLength)..]));
        }

        private ApduResponse StatusRejection() => RecordResponse([0x69, 0x82]);

        private ApduResponse EncryptedResponse(byte tag, ReadOnlySpan<byte> plaintext)
        {
            using SymmetricAlgorithm cipher = keyType == KeyType.TripleDes ? TripleDES.Create() : Aes.Create();
            cipher.Key = key;
            byte[] encrypted = cipher.EncryptEcb(plaintext, PaddingMode.None);
            try
            {
                if (tag == 0x82 && outcome == AuthenticationOutcome.ResponseMismatch)
                    encrypted[0] ^= 0xFF;
                return RecordResponse([0x7C, (byte)(blockLength + 2), tag, (byte)blockLength, .. encrypted, 0x90, 0x00]);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(encrypted);
            }
        }

        private ApduResponse RecordResponse(byte[] bytes)
        {
            var response = new ApduResponse(bytes);
            _responses.Add(response);
            return response;
        }

        public void AssertResponsesCleared()
        {
            Assert.Equal(outcome is AuthenticationOutcome.WitnessRejected or AuthenticationOutcome.ChallengeCancelled ? 1 : 2, _responses.Count);
            Assert.All(_responses, response => Assert.True(response.RawData.Span.IndexOfAnyExcept((byte)0) < 0));
            _responses.Clear();
        }

        public void Dispose()
        {
            CryptographicOperations.ZeroMemory(_witness);
            foreach (var response in _responses)
                response.Clear();
        }
    }
}