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

using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Oath.UnitTests;

/// <summary>
///     Tests for OATH persistent password-protection state (ISC-23), the dedicated
///     <see cref="OathException" /> contract (ISC-25/25.1/25.2/39), and the
///     authenticate-and-retry helper (ISC-24/26/26.1).
/// </summary>
public class OathAuthenticationTests
{
    private static readonly byte[] Salt = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
    private static readonly byte[] ServerChallenge = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22];

    private static byte[] SelectResponseUnprotected() =>
    [
        0x79, 0x03, 0x05, 0x07, 0x00,
        0x71, (byte)Salt.Length, .. Salt,
        0x90, 0x00
    ];

    private static byte[] SelectResponseProtected() =>
    [
        0x79, 0x03, 0x05, 0x07, 0x00,
        0x71, (byte)Salt.Length, .. Salt,
        0x74, (byte)ServerChallenge.Length, .. ServerChallenge,
        0x90, 0x00
    ];

    // --- ISC-23: persistent password-protection state, independent of unlock state ---

    [Fact]
    public async Task IsPasswordProtected_NoPasswordConfigured_IsFalse()
    {
        var connection = new RecordingSmartCardConnection(SelectResponseUnprotected());

        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(session.IsPasswordProtected);
        Assert.False(session.IsLocked);
    }

    [Fact]
    public async Task IsPasswordProtected_PasswordConfiguredAndNotValidated_IsTrue()
    {
        var connection = new RecordingSmartCardConnection(SelectResponseProtected());

        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(session.IsPasswordProtected);
        Assert.True(session.IsLocked);
    }

    [Fact]
    public async Task IsPasswordProtected_RemainsTrueAfterSuccessfulValidate_WhileIsLockedBecomesFalse()
    {
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            "correct horse"u8, Salt, iterations: 1000, HashAlgorithmName.SHA1, outputLength: 16);
        var connection = new ValidatingConnection(ServerChallenge, key);

        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(session.IsPasswordProtected);
        Assert.True(session.IsLocked);

        await session.ValidateAsync(key, TestContext.Current.CancellationToken);

        // The device is unlocked for this session, but it is still password-protected:
        // callers must still be able to tell "no password" apart from "already unlocked".
        Assert.False(session.IsLocked);
        Assert.True(session.IsPasswordProtected);
    }

    [Fact]
    public async Task IsPasswordProtected_AfterSetKeyAsync_BecomesTrue()
    {
        var connection = new RecordingSmartCardConnection(SelectResponseUnprotected(), [0x90, 0x00]);

        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(session.IsPasswordProtected);

        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            "new password"u8, Salt, iterations: 1000, HashAlgorithmName.SHA1, outputLength: 16);
        await session.SetKeyAsync(key, TestContext.Current.CancellationToken);

        Assert.True(session.IsPasswordProtected);
    }

    [Fact]
    public async Task IsPasswordProtected_AfterUnsetKeyAsync_BecomesFalse()
    {
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            "correct horse"u8, Salt, iterations: 1000, HashAlgorithmName.SHA1, outputLength: 16);
        var connection = new ValidatingConnection(ServerChallenge, key);

        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        await session.ValidateAsync(key, TestContext.Current.CancellationToken);
        Assert.True(session.IsPasswordProtected);

        await session.UnsetKeyAsync(TestContext.Current.CancellationToken);

        Assert.False(session.IsPasswordProtected);
    }

    // --- ISC-25/25.1/25.2/39: dedicated OathException with structured status info ---

    [Fact]
    public async Task ListCredentialsAsync_DeviceLocked_ThrowsOathExceptionWithLockedReasonAndStatusWord()
    {
        var connection = new RecordingSmartCardConnection(SelectResponseProtected(), [0x69, 0x82]);

        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<OathException>(() =>
            session.ListCredentialsAsync(TestContext.Current.CancellationToken));

        Assert.Equal(OathFailureReason.Locked, ex.Reason);
        Assert.Equal(unchecked((short)0x6982), ex.StatusWord);
    }

    [Fact]
    public async Task PutCredentialAsync_DeviceLocked_ThrowsOathExceptionWithLockedReason()
    {
        var connection = new RecordingSmartCardConnection(SelectResponseProtected(), [0x69, 0x82]);

        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        using var credentialData = CredentialData.ParseUri(
            "otpauth://totp/issuer:alice?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY&digits=6");

        var ex = await Assert.ThrowsAsync<OathException>(() =>
            session.PutCredentialAsync(credentialData, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(OathFailureReason.Locked, ex.Reason);
    }

    [Fact]
    public async Task ValidateAsync_WrongPassword_ThrowsOathExceptionWithWrongPasswordReasonAndStatusWord()
    {
        byte[] correctKey = Rfc2898DeriveBytes.Pbkdf2(
            "correct horse"u8, Salt, iterations: 1000, HashAlgorithmName.SHA1, outputLength: 16);
        byte[] wrongKey = Rfc2898DeriveBytes.Pbkdf2(
            "wrong password"u8, Salt, iterations: 1000, HashAlgorithmName.SHA1, outputLength: 16);
        var connection = new ValidatingConnection(ServerChallenge, correctKey);

        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<OathException>(() =>
            session.ValidateAsync(wrongKey, TestContext.Current.CancellationToken));

        Assert.Equal(OathFailureReason.WrongPassword, ex.Reason);
        Assert.NotNull(ex.StatusWord);

        // The device must still report itself as locked; a failed attempt does not unlock it.
        Assert.True(session.IsLocked);
    }

    // --- ISC-24/26/26.1: authenticate-and-retry helper ---

    [Theory]
    [InlineData(OathHashAlgorithm.Sha1, 14)]
    [InlineData(OathHashAlgorithm.Sha1, 20)]
    [InlineData(OathHashAlgorithm.Sha1, 64)]
    [InlineData(OathHashAlgorithm.Sha256, 64)]
    [InlineData(OathHashAlgorithm.Sha512, 128)]
    [InlineData(OathHashAlgorithm.Sha1, 13)]
    [InlineData(OathHashAlgorithm.Sha1, 65)]
    [InlineData(OathHashAlgorithm.Sha512, 129)]
    public async Task AuthenticateAndRetryAsync_PutLocked_PreservesSeedThroughValidateAndRetry(
        OathHashAlgorithm algorithm, int seedLength)
    {
        byte[] password = "correct horse"u8.ToArray();
        byte[] accessKey = Rfc2898DeriveBytes.Pbkdf2(
            password, Salt, iterations: 1000, HashAlgorithmName.SHA1, outputLength: 16);
        byte[] seed = new byte[seedLength];
        Array.Fill(seed, (byte)0xA5);
        int blockSize = algorithm == OathHashAlgorithm.Sha512 ? 128 : 64;
        int processedLength = seedLength > blockSize
            ? algorithm switch
            {
                OathHashAlgorithm.Sha1 => 20,
                OathHashAlgorithm.Sha256 => 32,
                OathHashAlgorithm.Sha512 => 64,
                _ => throw new InvalidOperationException()
            }
            : Math.Max(14, seedLength);
        byte[] expectedSeed = new byte[processedLength];
        byte[] expectedPayload = [];
        using var connection = new ValidatingConnection(ServerChallenge, accessKey);
        try
        {
            if (seedLength > blockSize)
            {
                _ = algorithm switch
                {
                    OathHashAlgorithm.Sha1 => SHA1.HashData(seed, expectedSeed),
                    OathHashAlgorithm.Sha256 => SHA256.HashData(seed, expectedSeed),
                    OathHashAlgorithm.Sha512 => SHA512.HashData(seed, expectedSeed),
                    _ => throw new InvalidOperationException()
                };
            }
            else
            {
                seed.CopyTo(expectedSeed.AsSpan());
            }

            // Exact TAG_NAME("seed") + TAG_KEY([type, digits, processed seed]). The 128-byte
            // SHA-512 seed makes TAG_KEY's value 130 bytes, requiring BER length 81 82.
            byte type = (byte)((byte)OathType.Totp | (byte)algorithm);
            expectedPayload = processedLength + 2 < 128
                ? [0x71, 0x04, 0x73, 0x65, 0x65, 0x64, 0x73, (byte)(processedLength + 2), type, 0x06, .. expectedSeed]
                : [0x71, 0x04, 0x73, 0x65, 0x65, 0x64, 0x73, 0x81, (byte)(processedLength + 2), type, 0x06, .. expectedSeed];

            await using var session = await OathSession.CreateAsync(
                connection, cancellationToken: TestContext.Current.CancellationToken);
            using var credential = new CredentialData
            {
                Name = "seed",
                OathType = OathType.Totp,
                HashAlgorithm = algorithm,
                Secret = seed
            };
            int passwordRequests = 0;

            await session.AuthenticateAndRetryAsync(
                token => session.PutCredentialAsync(credential, token),
                _ =>
                {
                    passwordRequests++;
                    return Task.FromResult((ReadOnlyMemory<byte>)password);
                },
                TestContext.Current.CancellationToken);

            Assert.False(session.IsLocked);
            Assert.Equal(1, passwordRequests);
            Assert.Equal([OathConstants.InsPut, OathConstants.InsValidate, OathConstants.InsPut],
                connection.Instructions);
            Assert.Equal(2, connection.PutPayloads.Count);
            Assert.True(CryptographicOperations.FixedTimeEquals(expectedPayload, connection.PutPayloads[0]));
            Assert.True(CryptographicOperations.FixedTimeEquals(
                expectedPayload, connection.PutPayloads[1]),
                "The PUT after successful VALIDATE must carry the same seed as the rejected PUT.");
            ReadOnlyMemory<byte> borrowed = credential.Secret;
            Assert.True(CryptographicOperations.FixedTimeEquals(seed, borrowed.Span));
            Assert.All(seed, value => Assert.Equal(0xA5, value));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(seed);
            CryptographicOperations.ZeroMemory(password);
            CryptographicOperations.ZeroMemory(accessKey);
            CryptographicOperations.ZeroMemory(expectedSeed);
            CryptographicOperations.ZeroMemory(expectedPayload);
        }
    }

    [Fact]
    public async Task AuthenticateAndRetryAsync_OperationSucceedsImmediately_DoesNotInvokePasswordProvider()
    {
        var connection = new RecordingSmartCardConnection(SelectResponseUnprotected());
        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        bool providerCalled = false;

        int result = await session.AuthenticateAndRetryAsync(
            _ => Task.FromResult(42),
            _ =>
            {
                providerCalled = true;
                return Task.FromResult((ReadOnlyMemory<byte>)"unused"u8.ToArray());
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(42, result);
        Assert.False(providerCalled);
    }

    [Fact]
    public async Task AuthenticateAndRetryAsync_OperationLocked_AuthenticatesThenRetriesOnce()
    {
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            "correct horse"u8, Salt, iterations: 1000, HashAlgorithmName.SHA1, outputLength: 16);
        var connection = new ValidatingConnection(ServerChallenge, key);

        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        int attempt = 0;

        int result = await session.AuthenticateAndRetryAsync(
            _ =>
            {
                attempt++;
                if (attempt == 1)
                    throw new OathException(OathFailureReason.Locked, unchecked((short)0x6982));

                return Task.FromResult(99);
            },
            _ => Task.FromResult((ReadOnlyMemory<byte>)"correct horse"u8.ToArray()),
            TestContext.Current.CancellationToken);

        Assert.Equal(99, result);
        Assert.Equal(2, attempt);
        Assert.False(session.IsLocked);
    }

    [Fact]
    public async Task AuthenticateAndRetryAsync_PasswordProviderSuppliesWrongPassword_PropagatesWrongPasswordAndDoesNotRetryOperation()
    {
        byte[] correctKey = Rfc2898DeriveBytes.Pbkdf2(
            "correct horse"u8, Salt, iterations: 1000, HashAlgorithmName.SHA1, outputLength: 16);
        var connection = new ValidatingConnection(ServerChallenge, correctKey);

        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        int attempt = 0;

        var ex = await Assert.ThrowsAsync<OathException>(() =>
            session.AuthenticateAndRetryAsync<int>(
                _ =>
                {
                    attempt++;
                    throw new OathException(OathFailureReason.Locked, unchecked((short)0x6982));
                },
                // The most realistic real-world failure mode: the user mistypes the password, so the
                // access key AuthenticateAndRetryAsync derives internally does not match the device's.
                _ => Task.FromResult((ReadOnlyMemory<byte>)"wrong password"u8.ToArray()),
                TestContext.Current.CancellationToken));

        Assert.Equal(OathFailureReason.WrongPassword, ex.Reason);

        // A failed authentication attempt must not silently retry the operation a second time —
        // that would mask the wrong-password failure and (if it looped) would reintroduce the
        // v1-style KeyCollector retry-until-correct behavior this design deliberately avoids.
        Assert.Equal(1, attempt);

        // The session must still report itself as locked; a failed attempt does not unlock it.
        Assert.True(session.IsLocked);
    }

    [Fact]
    public async Task AuthenticateWithDerivedKeyAsync_WrongPassword_ZeroesDerivedKeyOnTheFailureBranch()
    {
        // AuthenticateAndRetryAsync's Locked-catch delegates the derive+validate+zero sequence to
        // this internal helper; exercising it directly with a caller-owned array lets the test
        // assert zeroing by reference. This tests the authentication key, not the credential seed;
        // the real PUT/VALIDATE/retry regression above covers seed ownership independently.
        byte[] deviceKey = Rfc2898DeriveBytes.Pbkdf2(
            "correct horse"u8, Salt, iterations: 1000, HashAlgorithmName.SHA1, outputLength: 16);
        var connection = new ValidatingConnection(ServerChallenge, deviceKey);

        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        byte[] wrongKey = Rfc2898DeriveBytes.Pbkdf2(
            "wrong password"u8, Salt, iterations: 1000, HashAlgorithmName.SHA1, outputLength: 16);
        Assert.Contains(wrongKey, b => b != 0);

        var ex = await Assert.ThrowsAsync<OathException>(() =>
            session.AuthenticateWithDerivedKeyAsync(wrongKey, TestContext.Current.CancellationToken));

        Assert.Equal(OathFailureReason.WrongPassword, ex.Reason);
        Assert.All(wrongKey, b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task AuthenticateWithDerivedKeyAsync_CorrectPassword_ZeroesDerivedKeyOnTheSuccessBranch()
    {
        byte[] deviceKey = Rfc2898DeriveBytes.Pbkdf2(
            "correct horse"u8, Salt, iterations: 1000, HashAlgorithmName.SHA1, outputLength: 16);
        var connection = new ValidatingConnection(ServerChallenge, deviceKey);

        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        // Independent copy: `deviceKey` backs the fake connection's mutual-auth math and must not
        // itself be zeroed as a side effect of zeroing the caller's key.
        byte[] callerKey = deviceKey.ToArray();
        Assert.Contains(callerKey, b => b != 0);

        await session.AuthenticateWithDerivedKeyAsync(callerKey, TestContext.Current.CancellationToken);

        Assert.False(session.IsLocked);
        Assert.All(callerKey, b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task AuthenticateAndRetryAsync_NonLockedFailure_PropagatesWithoutInvokingPasswordProvider()
    {
        var connection = new RecordingSmartCardConnection(SelectResponseUnprotected());
        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        bool providerCalled = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.AuthenticateAndRetryAsync<int>(
                _ => throw new InvalidOperationException("unrelated failure"),
                _ =>
                {
                    providerCalled = true;
                    return Task.FromResult((ReadOnlyMemory<byte>)"unused"u8.ToArray());
                },
                TestContext.Current.CancellationToken));

        Assert.False(providerCalled);
    }

    [Fact]
    public async Task AuthenticateAndRetryAsync_TaskReturningOperation_AuthenticatesThenRetries()
    {
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            "correct horse"u8, Salt, iterations: 1000, HashAlgorithmName.SHA1, outputLength: 16);
        var connection = new ValidatingConnection(ServerChallenge, key);

        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        int attempt = 0;

        await session.AuthenticateAndRetryAsync(
            _ =>
            {
                attempt++;
                if (attempt == 1)
                    throw new OathException(OathFailureReason.Locked, unchecked((short)0x6982));

                return Task.CompletedTask;
            },
            _ => Task.FromResult((ReadOnlyMemory<byte>)"correct horse"u8.ToArray()),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, attempt);
    }

    [Fact]
    public async Task AuthenticateAndRetryAsync_CancelledBeforeAuthenticating_ThrowsOperationCanceledAndDoesNotCallProvider()
    {
        var connection = new RecordingSmartCardConnection(SelectResponseUnprotected());
        await using var session = await OathSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        bool providerCalled = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            session.AuthenticateAndRetryAsync<int>(
                _ => throw new OathException(OathFailureReason.Locked, unchecked((short)0x6982)),
                _ =>
                {
                    providerCalled = true;
                    return Task.FromResult((ReadOnlyMemory<byte>)"correct horse"u8.ToArray());
                },
                cts.Token));

        Assert.False(providerCalled);
    }

    [Fact]
    public async Task AuthenticateAndRetryAsync_AfterDisposal_ThrowsObjectDisposedBeforeCallbackValidation()
    {
        var connection = new RecordingSmartCardConnection(SelectResponseUnprotected());
        var session = await OathSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();
        int transmissionsBeforeCall = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.AuthenticateAndRetryAsync(
                null!,
                null!,
                TestContext.Current.CancellationToken));

        Assert.Equal(typeof(OathSession).FullName, exception.ObjectName);
        Assert.Equal(transmissionsBeforeCall, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task DeriveKey_AfterDisposal_ThrowsObjectDisposedBeforeSaltRead()
    {
        var connection = new RecordingSmartCardConnection(SelectResponseUnprotected());
        var session = await OathSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();

        var exception = Assert.Throws<ObjectDisposedException>(
            () => session.DeriveKey("password"u8.ToArray()));

        Assert.Equal(typeof(OathSession).FullName, exception.ObjectName);
    }

    /// <summary>
    ///     A SmartCard connection fake that simulates OATH SELECT and VALIDATE mutual
    ///     authentication using a real device-side access key, so tests can exercise the
    ///     actual client-side HMAC verification logic instead of replaying canned bytes.
    /// </summary>
    private sealed class ValidatingConnection(byte[] serverChallenge, byte[] accessKey) : ISmartCardConnection
    {
        private bool _selected;
        private bool _validated;
        private readonly List<byte[]> _validationResponses = [];

        public List<byte> Instructions { get; } = [];
        public List<byte[]> PutPayloads { get; } = [];

        public Transport Transport => Transport.Usb;

        public ConnectionType Type => ConnectionType.SmartCard;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytes = command.Span;

            if (!_selected)
            {
                _selected = true;
                byte[] resp =
                [
                    0x79, 0x03, 0x05, 0x07, 0x00,
                    0x71, (byte)Salt.Length, .. Salt,
                    0x74, (byte)serverChallenge.Length, .. serverChallenge,
                    0x90, 0x00
                ];
                return Task.FromResult((ReadOnlyMemory<byte>)resp);
            }

            byte ins = bytes[1];
            Instructions.Add(ins);
            if (ins == OathConstants.InsPut)
            {
                // Snapshot at the transport boundary, before SDK cleanup clears its temporary APDU.
                PutPayloads.Add(bytes.Slice(5, bytes[4]).ToArray());
                return Task.FromResult((ReadOnlyMemory<byte>)(byte[])
                    (_validated ? [0x90, 0x00] : [0x69, 0x82]));
            }

            if (ins == OathConstants.InsSetCode)
            {
                // SET CODE (used by SetKeyAsync/UnsetKeyAsync) always succeeds in this fake;
                // mutual-auth simulation is only needed for VALIDATE.
                return Task.FromResult((ReadOnlyMemory<byte>)(byte[])[0x90, 0x00]);
            }

            if (ins != OathConstants.InsValidate)
                throw new InvalidOperationException($"ValidatingConnection only simulates VALIDATE, got INS=0x{ins:X2}.");

            int offset = 5;
            byte responseTag = bytes[offset]; offset++;
            byte responseLen = bytes[offset]; offset++;
            ReadOnlySpan<byte> clientResponse = bytes.Slice(offset, responseLen);
            offset += responseLen;

            byte challengeTag = bytes[offset]; offset++;
            byte challengeLen = bytes[offset]; offset++;
            ReadOnlySpan<byte> clientChallenge = bytes.Slice(offset, challengeLen);

            Assert.Equal(OathConstants.TagResponse, responseTag);
            Assert.Equal(OathConstants.TagChallenge, challengeTag);

            Span<byte> expectedClientResponse = stackalloc byte[20];
            Span<byte> deviceResponse = stackalloc byte[20];
            try
            {
                HMACSHA1.HashData(accessKey, serverChallenge, expectedClientResponse);
                if (!CryptographicOperations.FixedTimeEquals(expectedClientResponse, clientResponse))
                {
                    // Wrong key: device rejects with "reference data not usable".
                    return Task.FromResult((ReadOnlyMemory<byte>)(byte[])[0x69, 0x84]);
                }

                HMACSHA1.HashData(accessKey, clientChallenge, deviceResponse);
                byte[] okResponse = [OathConstants.TagResponse, (byte)deviceResponse.Length, .. deviceResponse, 0x90, 0x00];
                _validationResponses.Add(okResponse);
                _validated = true;
                return Task.FromResult((ReadOnlyMemory<byte>)okResponse);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(expectedClientResponse);
                CryptographicOperations.ZeroMemory(deviceResponse);
            }
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => NullDisposable.Instance;

        public bool SupportsExtendedApdu() => false;

        public void Dispose()
        {
            foreach (byte[] payload in PutPayloads)
                CryptographicOperations.ZeroMemory(payload);
            foreach (byte[] response in _validationResponses)
                CryptographicOperations.ZeroMemory(response);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}