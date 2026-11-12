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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backends;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.Authentication;

/// <summary>
/// End-to-end (public API) coverage proving <see cref="PivSession"/> wires PIN-only mode through
/// to the underlying protocol helpers (ISC-14, 14.1, 15, 15.1).
/// </summary>
public class PivPinOnlyModeSessionTests
{
    [Fact]
    public async Task RecoverPinOnlyModeAsync_ProtectedReadRequiresPin_VerifiesOnceAndReusesForDerivedCandidate()
    {
        await using var connection = new MixedPinOnlyRecoveryConnection(true, requirePinForPrinted: true);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var result = await session.RecoverPinOnlyModeAsync("123456"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.Equal(PinOnlyMode.PinProtected, result);
        Assert.True(session.IsManagementKeyAuthenticated);
        Assert.Single(connection.TransmittedCommands, command => command[1] == 0x20);
        Assert.Equal(3, connection.TransmittedCommands.Count(command => command[1] == 0xCB));
    }
    [Fact]
    public async Task RecoverPinOnlyModeAsync_CancelledDerivedAuthentication_DoesNotAttemptRestoration()
    {
        await using var connection = new MixedPinOnlyRecoveryConnection(true, cancelDerived: true);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var captured = CaptureAuthenticationResponses(session);

        await Assert.ThrowsAsync<OperationCanceledException>(() => session.RecoverPinOnlyModeAsync(
            "123456"u8.ToArray(), TestContext.Current.CancellationToken));

        Assert.Equal(2, connection.AuthenticationAttempts);
        Assert.False(session.IsManagementKeyAuthenticated);
        Assert.Equal([1], connection.WitnessSubmissionAttempts);
        captured.AssertResponsesCleared();
    }
    [Theory]
    [InlineData(0x6A80)]
    [InlineData(0x6D00)]
    public async Task RecoverPinOnlyModeAsync_UnexpectedDerivedAuthenticationFailure_PropagatesWithoutRestoration(int status)
    {
        await using var connection = new MixedPinOnlyRecoveryConnection(true, (ushort)status);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var captured = CaptureAuthenticationResponses(session);

        var exception = await Assert.ThrowsAsync<ApduException>(() => session.RecoverPinOnlyModeAsync(
            "123456"u8.ToArray(), TestContext.Current.CancellationToken));

        Assert.True(status == exception.SW);
        Assert.Equal(2, connection.AuthenticationAttempts);
        Assert.False(session.IsManagementKeyAuthenticated);
        Assert.Equal([1], connection.WitnessSubmissionAttempts);
        captured.AssertResponsesCleared();
    }
    [Fact]
    public async Task GetPinOnlyModeAsync_NoAdminData_ReturnsNone()
    {
        var connection = CreateInitializedConnection([0x6A, 0x82]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var mode = await session.GetPinOnlyModeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(PinOnlyMode.None, mode);
        Assert.Contains(connection.TransmittedCommands, c => c[1] == 0xCB); // GET DATA
    }

    [Fact]
    public async Task SetPinOnlyModeAsync_NotAuthenticated_ThrowsInvalidOperationException()
    {
        var connection = CreateInitializedConnection();
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SetPinOnlyModeAsync(
            PinOnlyMode.PinProtected,
            "123456"u8.ToArray(),
            new byte[24],
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetPinOnlyModeAsync_PinDerivedRequested_ThrowsArgumentException()
    {
        var connection = CreateInitializedConnection();
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);

        await Assert.ThrowsAsync<ArgumentException>(() => session.SetPinOnlyModeAsync(
            PinOnlyMode.PinDerived,
            "123456"u8.ToArray(),
            new byte[24],
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetPinOnlyModeAsync_AfterSuccessfulAuthentication_WhenSuppliedKeyFailsAuthentication_ClearsSessionStateBeforeMutation()
    {
        var connection = CreateInitializedConnection([0x69, 0x82]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        // Represent a prior successful management-key authentication. The subsequent authentication
        // attempt runs through the real public-session and protocol flow using the queued APDU response.
        MarkAuthenticated(session);
        Assert.True(session.IsManagementKeyAuthenticated);
        int initializationCommandCount = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ApduException>(() => session.SetPinOnlyModeAsync(
            PinOnlyMode.PinProtected,
            "123456"u8.ToArray(),
            new byte[24],
            TestContext.Current.CancellationToken));

        Assert.True(exception.SW == 0x6982);
        Assert.False(session.IsManagementKeyAuthenticated);

        var operationCommands = connection.TransmittedCommands.Skip(initializationCommandCount).ToList();
        Assert.Single(operationCommands);
        Assert.Equal(0x87, operationCommands[0][1]); // GENERAL AUTHENTICATE
        Assert.DoesNotContain(operationCommands, command => command[1] == 0x20); // no VERIFY PIN
        Assert.DoesNotContain(operationCommands, command => command[1] is 0x2C or 0xDB or 0xFF); // no persistent mutation
    }

    [Fact]
    public async Task RecoverPinOnlyModeAsync_NoPinOnlyDataPresent_ReturnsNoneWithoutAuthenticating()
    {
        // Neither PRINTED nor ADMIN DATA present -> Recover should short-circuit before ever
        // attempting management-key authentication (no GENERAL AUTHENTICATE APDU transmitted).
        var connection = CreateInitializedConnection([0x6A, 0x82], [0x6A, 0x82]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var mode = await session.RecoverPinOnlyModeAsync("123456"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.Equal(PinOnlyMode.None, mode);
        Assert.DoesNotContain(connection.TransmittedCommands, c => c[1] == 0x87); // no GENERAL AUTHENTICATE
        Assert.DoesNotContain(connection.TransmittedCommands, c => c[1] == 0x20); // no VERIFY
    }

    [Fact]
    public async Task RecoverPinOnlyModeAsync_WhenDerivedCandidateFails_RestoresProtectedAuthenticationBeforeReturningSuccess()
    {
        await using var connection = new MixedPinOnlyRecoveryConnection(restoreProtectedAuthentication: true);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var captured = CaptureAuthenticationResponses(session);

        var mode = await session.RecoverPinOnlyModeAsync("123456"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.Equal(PinOnlyMode.PinProtected, mode);
        Assert.True(session.IsManagementKeyAuthenticated);
        Assert.Equal(3, connection.AuthenticationAttempts);
        Assert.Equal([1, 2, 3], connection.WitnessSubmissionAttempts);
        Assert.True(connection.DerivedWitnessValidated);
        captured.AssertResponsesCleared();
    }

    [Fact]
    public async Task RecoverPinOnlyModeAsync_WhenDerivedAndProtectedRestorationFail_ReturnsNoneWithUnauthenticatedSession()
    {
        await using var connection = new MixedPinOnlyRecoveryConnection(restoreProtectedAuthentication: false);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var captured = CaptureAuthenticationResponses(session);

        var mode = await session.RecoverPinOnlyModeAsync("123456"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.Equal(PinOnlyMode.None, mode);
        Assert.False(session.IsManagementKeyAuthenticated);
        Assert.Equal(3, connection.AuthenticationAttempts);
        Assert.Equal([1, 2, 3], connection.WitnessSubmissionAttempts);
        Assert.True(connection.DerivedWitnessValidated);
        captured.AssertResponsesCleared();
    }

    [Fact]
    public async Task AuthenticateAsync_IncorrectSubmittedWitness_IsRejectedByDeviceAtSecondStage()
    {
        await using var connection = new MixedPinOnlyRecoveryConnection(true);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var captured = CaptureAuthenticationResponses(session);
        byte[] wrongKey = Enumerable.Range(1, 24).Select(value => (byte)value).ToArray();
        try
        {
            var exception = await Assert.ThrowsAsync<ApduException>(() => session.AuthenticateAsync(wrongKey, TestContext.Current.CancellationToken));

            Assert.True(exception.SW == 0x6982);
            Assert.False(session.IsManagementKeyAuthenticated);
            Assert.Equal([1], connection.WitnessSubmissionAttempts);
            captured.AssertResponsesCleared();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(wrongKey);
        }
    }

    private static AuthenticationResponseCapture CaptureAuthenticationResponses(PivSession session)
    {
        var field = typeof(PivSession).GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PIV backend field not found.");
        var captured = new AuthenticationResponseCapture(Assert.IsAssignableFrom<IBackend>(field.GetValue(session)));
        field.SetValue(session, captured);
        return captured;
    }

    private sealed class AuthenticationResponseCapture(IBackend backend) : IBackend
    {
        private readonly List<ReadOnlyMemory<byte>> _responses = [];

        public Task<Initialization> ReadInitialStateAsync(CancellationToken cancellationToken = default) =>
            backend.ReadInitialStateAsync(cancellationToken);

        public async Task<ApduResponse> SendAsync(ApduCommand command, bool throwOnError = true, CancellationToken cancellationToken = default)
        {
            // Responses own non-pooled storage. Command payload storage is returned to the pool
            // before recovery completes; AuthenticationProtocolTests checks it at Return instead.
            var response = await backend.SendAsync(command, throwOnError, cancellationToken);
            if (command.Ins == 0x87)
                _responses.Add(response.RawData);
            return response;
        }

        public void AssertResponsesCleared()
        {
            Assert.NotEmpty(_responses);
            Assert.All(_responses, bytes => Assert.True(bytes.Span.IndexOfAnyExcept((byte)0) < 0));
        }
    }

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), VersionResponse(), ManagementKeyMetadataResponse(), .. trailingResponses]);

    private static void MarkAuthenticated(PivSession session) =>
        typeof(PivSession)
            .GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, true);

    private static byte[] OkResponse() => [0x90, 0x00];

    private static byte[] VersionResponse() => [0x00, 0x00, 0x01, 0x90, 0x00];

    private static byte[] ManagementKeyMetadataResponse() =>
    [
        0x01, 0x01, 0x03,
        0x02, 0x02, 0x00, (byte)TouchPolicy.Default,
        0x05, 0x01, 0x01,
        0x90, 0x00
    ];

    private sealed class MixedPinOnlyRecoveryConnection(bool restoreProtectedAuthentication, ushort rejectionStatus = 0x6982, bool cancelDerived = false, bool requirePinForPrinted = false) : ISmartCardConnection
    {
        private readonly byte[] _managementKey =
        [
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
            0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37
        ];

        private static readonly byte[] Salt =
        [
            0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
            0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F
        ];

        private readonly bool _restoreProtectedAuthentication = restoreProtectedAuthentication;
        private bool _printedReadRejected;
        private readonly byte[] _witness = new byte[8];
        private byte[] _encryptedWitness = [];
        private readonly byte[] _replacementKey = Enumerable.Range(1, 24).Select(value => (byte)value).ToArray();

        public int AuthenticationAttempts { get; private set; }
        public List<int> WitnessSubmissionAttempts { get; } = [];
        public bool DerivedWitnessValidated { get; private set; }

        public Transport Transport => Transport.Usb;

        public ConnectionType Type => ConnectionType.SmartCard;

        public List<byte[]> TransmittedCommands { get; } = [];

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] commandBytes = command.ToArray();
            TransmittedCommands.Add(commandBytes);

            byte[] response = commandBytes[1] switch
            {
                0xA4 => OkResponse(),
                0xFD => VersionResponse(),
                0xF7 => ManagementKeyMetadataResponse(),
                0xCB => GetDataResponse(commandBytes),
                0x20 => OkResponse(),
                0x87 => AuthenticateResponse(commandBytes),
                _ => throw new InvalidOperationException($"Unexpected PIV command INS 0x{commandBytes[1]:X2}.")
            };

            return Task.FromResult((ReadOnlyMemory<byte>)response);
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => NullDisposable.Instance;

        public bool SupportsExtendedApdu() => false;

        public void Dispose()
        {
            ClearSensitiveState();
        }

        public ValueTask DisposeAsync()
        {
            ClearSensitiveState();
            return default;
        }

        private byte[] GetDataResponse(byte[] command)
        {
            ReadOnlySpan<byte> commandData = command.AsSpan(5, command[4]);
            if (commandData.IndexOf(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x09 }) >= 0)
            {
                if (requirePinForPrinted && !_printedReadRejected)
                {
                    _printedReadRejected = true;
                    return [0x69, 0x82];
                }
                return [0x53, 0x1C, 0x88, 0x1A, 0x89, 0x18, .. _managementKey, 0x90, 0x00];
            }

            if (commandData.IndexOf(new byte[] { 0x5C, 0x03, 0x5F, 0xFF, 0x00 }) >= 0)
            {
                byte[] adminData = [0x80, 0x15, 0x81, 0x01, 0x02, 0x82, 0x10, .. Salt];
                return [0x53, (byte)adminData.Length, .. adminData, 0x90, 0x00];
            }

            throw new InvalidOperationException("Unexpected PIV data object request.");
        }

        private byte[] AuthenticateResponse(byte[] command)
        {
            ReadOnlySpan<byte> commandData = command.AsSpan(5, command[4]);
            if (commandData.SequenceEqual(new byte[] { 0x7C, 0x02, 0x80, 0x00 }))
            {
                AuthenticationAttempts++;
                if (cancelDerived && AuthenticationAttempts == 2)
                    throw new OperationCanceledException();
                // Cancellation and unrelated status failures deliberately retain first-stage coverage.
                if (AuthenticationAttempts == 2 && rejectionStatus != 0x6982)
                {
                    return [(byte)(rejectionStatus >> 8), (byte)rejectionStatus];
                }

                RandomNumberGenerator.Fill(_witness);
                CryptographicOperations.ZeroMemory(_encryptedWitness);
                _encryptedWitness = TransformBlock(ActiveManagementKey, _witness, encrypt: true);
                return [0x7C, 0x0A, 0x80, 0x08, .. _encryptedWitness, 0x90, 0x00];
            }

            return CompleteAuthentication(commandData);
        }

        private ReadOnlySpan<byte> ActiveManagementKey =>
            AuthenticationAttempts == 3 && !_restoreProtectedAuthentication ? _replacementKey : _managementKey;

        private byte[] CompleteAuthentication(ReadOnlySpan<byte> commandData)
        {
            WitnessSubmissionAttempts.Add(AuthenticationAttempts);
            try
            {
                using var outer = Tlv.Create(commandData);
                Assert.Equal(0x7C, outer.Tag);
                using var fields = TlvHelper.DecodeList(outer.Value.Span);
                var submittedWitness = Assert.Single(fields, field => field.Tag == 0x80).Value;
                var challenge = Assert.Single(fields, field => field.Tag == 0x81).Value;
                Assert.Equal(8, submittedWitness.Length);
                Assert.Equal(8, challenge.Length);

                if (AuthenticationAttempts == 2)
                    AssertDerivedWitness(submittedWitness.Span);

                // A stale candidate is rejected only after proving it cannot decrypt the device witness.
                // Failed restoration uses a changed physical key, not an unconditional canned rejection.
                if (!CryptographicOperations.FixedTimeEquals(submittedWitness.Span, _witness))
                    return [0x69, 0x82];

                byte[] encryptedChallenge = TransformBlock(ActiveManagementKey, challenge.Span, encrypt: true);
                try
                {
                    return [0x7C, 0x0A, 0x82, 0x08, .. encryptedChallenge, 0x90, 0x00];
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(encryptedChallenge);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(_witness);
                CryptographicOperations.ZeroMemory(_encryptedWitness);
            }
        }

        private void AssertDerivedWitness(ReadOnlySpan<byte> submittedWitness)
        {
            Span<byte> derived = stackalloc byte[24];
            try
            {
                // Independently check the stale derived candidate reached the wire, without a production hook.
                // SHA1/10,000 is the legacy PIN-derived compatibility format.
#pragma warning disable CA5379, CA5387
                Rfc2898DeriveBytes.Pbkdf2("123456"u8, Salt, derived, 10_000, HashAlgorithmName.SHA1);
#pragma warning restore CA5379, CA5387
                byte[] expected = TransformBlock(derived, _encryptedWitness, encrypt: false);
                try
                {
                    Assert.True(CryptographicOperations.FixedTimeEquals(submittedWitness, expected));
                    DerivedWitnessValidated = true;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(expected);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(derived);
            }
        }

        private static byte[] TransformBlock(ReadOnlySpan<byte> key, ReadOnlySpan<byte> input, bool encrypt)
        {
            byte[] keyBytes = key.ToArray();
            try
            {
                using var tripleDes = TripleDES.Create();
                tripleDes.Key = keyBytes;
                return encrypt
                    ? tripleDes.EncryptEcb(input, PaddingMode.None)
                    : tripleDes.DecryptEcb(input, PaddingMode.None);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyBytes);
            }
        }

        private void ClearSensitiveState()
        {
            CryptographicOperations.ZeroMemory(_managementKey);
            CryptographicOperations.ZeroMemory(_replacementKey);
            CryptographicOperations.ZeroMemory(_witness);
            CryptographicOperations.ZeroMemory(_encryptedWitness);
            foreach (byte[] command in TransmittedCommands)
            {
                CryptographicOperations.ZeroMemory(command);
            }
        }

        private sealed class NullDisposable : IDisposable
        {
            public static NullDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}