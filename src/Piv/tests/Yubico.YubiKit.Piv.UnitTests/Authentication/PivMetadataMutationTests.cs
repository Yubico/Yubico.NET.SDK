// Copyright 2026 Yubico AB
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Piv.Backends;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.Authentication;

public class PivMetadataMutationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static byte[] Ok => [0x90, 0x00];
    private static byte[] Missing => [0x6A, 0x82];
    private static byte[] Salt => [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
    private static byte[] Replacement => [.. Enumerable.Repeat((byte)0x45, 24)];
    // Unknown BER tag and nonminimal lengths; timestamp is opaque, not decoded as a date.
    private static byte[] Unrelated => [0x9F, 0x81, 0x01, 1, 0x55, 0x83, 0x81, 4, 0xA1, 0xB2, 0xC3, 0xD4];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RotateUnstored_DoesNotReadPrintedOrQueryPin(bool derived)
    {
        byte[] admin = Admin(flags: 0x41, derived);
        await using var connection = Connect(derived ? [admin, Ok, Ok] : [admin, Ok]);
        await using var session = await OpenAsync(connection, authenticated: true);

        await session.SetManagementKeyWithMetadataAsync(KeyType.TripleDes, Replacement, ManagementKeyStorage.NotStored, cancellationToken: Ct);

        Assert.Equal(derived ? [0xCB, 0xFF, 0xDB] : new byte[] { 0xCB, 0xFF }, Instructions(connection));
        if (derived)
            AssertPut(connection.TransmittedCommands[^1], 0x5FFF00, [0x80, (byte)(3 + Unrelated.Length), 0x81, 1, 0x41, .. Unrelated]);
    }

    [Theory]
    [InlineData(false, ManagementKeyStorage.PinProtected)]
    [InlineData(true, ManagementKeyStorage.NotStored)]
    [InlineData(true, ManagementKeyStorage.PinProtected)]
    public async Task RotateProtectedOrMixed_ReconcilesOnlyOwnedFields(bool derived, ManagementKeyStorage storage)
    {
        byte[] unknown = [0x8B, 0x81, 1, 0x61];
        byte[] printed = Printed(unknown);
        await using var connection = Connect(derived ? [Admin(0x43, true), printed, Ok, Ok, Ok] : [Admin(0x43, false), printed, Ok, Ok]);
        await using var session = await OpenAsync(connection, authenticated: true);
        var observer = Observe(session);
        byte[] replacement = Replacement;

        await session.SetManagementKeyWithMetadataAsync(KeyType.TripleDes, replacement, storage, cancellationToken: Ct);

        Assert.Equal(derived ? [0xCB, 0xCB, 0xFF, 0xDB, 0xDB] : new byte[] { 0xCB, 0xCB, 0xFF, 0xDB }, Instructions(connection));
        if (derived)
            AssertPut(connection.TransmittedCommands[^2], 0x5FFF00, [0x80, (byte)(3 + Unrelated.Length), 0x81, 1,
                storage == ManagementKeyStorage.NotStored ? (byte)0x41 : (byte)0x43, .. Unrelated]);
        byte[] expected = storage == ManagementKeyStorage.NotStored ? unknown : [0x89, 24, .. replacement, .. unknown];
        AssertPut(connection.TransmittedCommands[^1], 0x5FC109, [0x88, (byte)expected.Length, .. expected]);
        Assert.Equal(Replacement, replacement);
        observer.AssertPrintedResponseAndWriteCleared();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task CreateProtected_WhenPrintedIsEmpty_RequiresPositiveNoDataVerify(bool verified, bool emptyWrapper)
    {
        byte[] printed = emptyWrapper ? [0x53, 2, 0x88, 0, .. Ok] : Missing;
        await using var connection = Connect(verified ? [Missing, printed, Ok, Ok, Ok, Ok] : [Missing, printed, [0x63, 0xC3]]);
        await using var session = await OpenAsync(connection, authenticated: true);

        Task Rotate() => session.SetManagementKeyWithMetadataAsync(KeyType.TripleDes, Replacement, ManagementKeyStorage.PinProtected, cancellationToken: Ct);
        if (verified)
        {
            await Rotate();
            Assert.Equal(new byte[] { 0xCB, 0xCB, 0x20, 0xFF, 0xDB, 0xDB }, Instructions(connection));
            AssertPut(connection.TransmittedCommands[^2], 0x5FFF00, [0x80, 3, 0x81, 1, 2]);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(Rotate);
            Assert.Equal(new byte[] { 0xCB, 0xCB, 0x20 }, Instructions(connection));
        }
        // The short formatter adds Le=0, not Lc or submitted PIN data.
        Assert.Equal(new byte[] { 0x00, 0x20, 0x00, 0x80, 0x00 }, connection.TransmittedCommands[5]);
    }

    [Theory]
    [InlineData(false, "53800580009000")]
    [InlineData(false, "53038000009000")]
    [InlineData(false, "80068101028101029000")]
    [InlineData(false, "80038201009000")]
    [InlineData(true, "6982")]
    [InlineData(true, "53030101419000")]
    [InlineData(true, "88038901009000")]
    public async Task Rotate_StrictReadFailureStopsBeforeSettingKey(bool printed, string responseHex)
    {
        byte[] response = Convert.FromHexString(responseHex);
        await using var connection = Connect(printed ? [Admin(2, false), response] : [response]);
        await using var session = await OpenAsync(connection, authenticated: true);
        var observer = Observe(session);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => session.SetManagementKeyWithMetadataAsync(
            KeyType.TripleDes, Replacement, ManagementKeyStorage.NotStored, cancellationToken: Ct));

        Assert.Contains(printed ? "read PRINTED" : "read ADMIN DATA", error.Message);
        Assert.Contains("writes: none", error.Message);
        Assert.Contains("not attempted", error.Message);
        Assert.IsAssignableFrom<ApduException>(error.InnerException);
        Assert.Equal(printed ? [0xCB, 0xCB] : new byte[] { 0xCB }, Instructions(connection));
        if (printed)
            observer.AssertPrintedResponseCleared();
    }

    [Fact]
    public async Task Rotate_LostPrintedResponseReportsTwoAcknowledgementsAndClearsBuffers()
    {
        await using var connection = Connect(Admin(0x43, true), Printed([]), Ok, Ok, Ok);
        await using var session = await OpenAsync(connection, authenticated: true);
        // The fake drops the PRINTED reply at the backend boundary. Even an ApduException from the
        // provider does not establish that the card rejected this particular write.
        var cause = ApduException.FromStatusWord(unchecked((short)0x6982), "simulated provider failure");
        var observer = Observe(session);
        observer.AfterSend = command =>
        {
            if (IsPrintedPut(command))
                throw cause;
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => session.SetManagementKeyWithMetadataAsync(
            KeyType.TripleDes, Replacement, ManagementKeyStorage.PinProtected, cancellationToken: Ct));

        Assert.Same(cause, error.InnerException);
        Assert.Contains("write PRINTED", error.Message);
        Assert.Contains("management key set; ADMIN DATA written", error.Message);
        Assert.Contains("unconfirmed", error.Message);
        Assert.Equal(new byte[] { 0xCB, 0xCB, 0xFF, 0xDB, 0xDB }, Instructions(connection));
        observer.AssertPrintedResponseAndWriteCleared();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangeIndependentPin_DoesNotTouchRecoveryMetadata(bool stored)
    {
        await using var connection = Connect(Admin(stored ? (byte)2 : (byte)0, false), Ok);
        await using var session = await OpenAsync(connection);

        await session.ChangePinWithMetadataAsync("123456"u8.ToArray(), "654321"u8.ToArray(), Ct);

        Assert.Equal(new byte[] { 0xCB, 0x24 }, Instructions(connection));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangeDerivedPin_UsesOldKeyAndFreshSaltOrStopsAfterFailedVerify(bool verifyFails)
    {
        byte[] currentPin = "123456"u8.ToArray();
        byte[] newPin = "654321"u8.ToArray();
        byte[] oldKey = Derive(currentPin, Salt);
        await using var connection = Connect(verifyFails ? [Admin(1, true), Ok, [0x63, 0xC2]] : [Admin(1, true), Ok, Ok, Ok, Ok]);
        await using var session = await OpenAsync(connection);
        var observer = Observe(session);
        using var tripleDes = TripleDES.Create();
        tripleDes.Key = oldKey;
        CryptographicOperations.ZeroMemory(oldKey);
        List<byte> stages = [];
        observer.BeforeSend = command =>
        {
            stages.Add(command.Ins);
            return command.Ins == 0x87 ? ReplyToAuthentication(tripleDes, command.Data.Span) : null;
        };

        if (verifyFails)
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ChangePinWithMetadataAsync(currentPin, newPin, Ct));
            Assert.IsType<InvalidPinException>(error.InnerException);
            Assert.Contains("verify new PIN", error.Message);
            Assert.Contains("writes: PIN changed", error.Message);
            Assert.Contains("not attempted", error.Message);
            Assert.Equal(new byte[] { 0xCB, 0x24, 0x87, 0x87, 0x20 }, stages);
        }
        else
        {
            await session.ChangePinWithMetadataAsync(currentPin, newPin, Ct);
            Assert.Equal(new byte[] { 0xCB, 0x24, 0x87, 0x87, 0x20, 0xFF, 0xDB }, stages);
            byte[] updatedSalt = connection.TransmittedCommands[^1].AsSpan(19, 16).ToArray();
            Assert.False(Salt.AsSpan().SequenceEqual(updatedSalt)); // Salts are public metadata.
            byte[] expectedKey = Derive(newPin, updatedSalt);
            try
            {
                Assert.Equal<byte>([0, 0xFF, 0xFF, 0xFF, 27, 3, 0x9B, 24, .. expectedKey, 0], connection.TransmittedCommands[^2]);
                AssertPut(connection.TransmittedCommands[^1], 0x5FFF00, [0x80, (byte)(21 + Unrelated.Length), 0x81, 1, 1, 0x82, 16, .. updatedSalt, .. Unrelated]);
                observer.AssertKeyWriteCleared();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(expectedKey);
            }
        }
        Assert.Equal("123456"u8.ToArray(), currentPin);
        Assert.Equal("654321"u8.ToArray(), newPin);
    }

    [Theory]
    [InlineData(true, false, 3)]
    [InlineData(true, true, 3)]
    [InlineData(false, true, 3)]
    [InlineData(false, false, 8)]
    public async Task UnsupportedDerivedCombinations_DoNotMutate(bool retries, bool stored, byte algorithm)
    {
        await using var connection = Connect([Admin(stored ? (byte)3 : (byte)1, true)], algorithm);
        await using var session = await OpenAsync(connection, authenticated: retries);
        await Assert.ThrowsAsync<NotSupportedException>(() => retries ? session.SetPinAttemptsWithMetadataAsync(5, 7, Ct) :
            session.ChangePinWithMetadataAsync("123456"u8.ToArray(), "654321"u8.ToArray(), Ct));
        Assert.Equal(new byte[] { 0xCB }, Instructions(connection));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task RetryChange_RequiresPinResetsCredentialsBeforeClearingOnlyPukFlag(bool cancelAfterSet, bool verified)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        await using var connection = Connect(Admin(0x43, false), verified ? Ok : [0x63, 0xC3], Ok, Ok);
        await using var session = await OpenAsync(connection, authenticated: true);
        var observer = Observe(session);
        observer.AfterSend = command =>
        {
            if (cancelAfterSet && command.Ins == 0xFA)
                cts.Cancel();
        };

        Task Set() => session.SetPinAttemptsWithMetadataAsync(5, 7, cts.Token);
        if (!verified)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(Set);
            Assert.Equal(new byte[] { 0xCB, 0x20 }, Instructions(connection));
            return;
        }
        if (cancelAfterSet)
        {
            var error = await Assert.ThrowsAsync<OperationCanceledException>(Set);
            Assert.Equal(cts.Token, error.CancellationToken);
            Assert.IsType<OperationCanceledException>(error.InnerException);
            Assert.Contains("factory PIN and PUK restored", error.Message);
            Assert.Contains("write ADMIN DATA", error.Message);
            Assert.Contains("not attempted", error.Message);
            Assert.Equal(new byte[] { 0xCB, 0x20, 0xFA }, Instructions(connection));
        }
        else
        {
            await Set();
            Assert.Equal(new byte[] { 0xCB, 0x20, 0xFA, 0xDB }, Instructions(connection));
            AssertPut(connection.TransmittedCommands[^1], 0x5FFF00, [0x80, (byte)(3 + Unrelated.Length), 0x81, 1, 0x42, .. Unrelated]);
        }
        Assert.Equal(new byte[] { 0, 0xFA, 5, 7, 0 }, connection.TransmittedCommands[5]);
    }

    private static byte[] Admin(byte flags, bool derived)
    {
        byte[] content = [0x81, 1, flags, .. (derived ? [0x82, (byte)16, .. Salt] : Array.Empty<byte>()), .. Unrelated];
        return [0x53, 0x81, (byte)(3 + content.Length), 0x80, 0x81, (byte)content.Length, .. content, .. Ok];
    }

    private static byte[] Printed(byte[] trailing) => [0x53, (byte)(28 + trailing.Length), 0x88, (byte)(26 + trailing.Length),
        0x89, 24, .. Enumerable.Repeat((byte)0x37, 24), .. trailing, .. Ok];

    private static RecordingSmartCardConnection Connect(params byte[][] responses) => Connect(responses, 3);

    private static RecordingSmartCardConnection Connect(byte[][] responses, byte algorithm) =>
        new([Ok, [5, 7, 0, .. Ok], [1, 1, algorithm, 2, 2, 0, 1, 5, 1, 1, .. Ok], .. responses]);

    private static async Task<PivSession> OpenAsync(RecordingSmartCardConnection connection, bool authenticated = false)
    {
        var session = await PivSession.CreateAsync(connection, cancellationToken: Ct);
        if (authenticated)
            Field("_isAuthenticated").SetValue(session, true);
        return session;
    }

    private static FieldInfo Field(string name) => typeof(PivSession).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("PIV test seam not found.");

    private static Observation Observe(PivSession session)
    {
        var field = Field("_backend");
        var observer = new Observation(Assert.IsAssignableFrom<IBackend>(field.GetValue(session)));
        field.SetValue(session, observer);
        return observer;
    }

    private static byte[] Instructions(RecordingSmartCardConnection connection) =>
        [.. connection.TransmittedCommands.Skip(3).Select(command => command[1])];

    private static void AssertPut(byte[] command, int objectId, byte[] contents) =>
        Assert.Equal<byte>([0, 0xDB, 0x3F, 0xFF, (byte)(7 + contents.Length), 0x5C, 3,
            (byte)(objectId >> 16), (byte)(objectId >> 8), (byte)objectId, 0x53, (byte)contents.Length, .. contents, 0], command);

    private static byte[] Derive(byte[] pin, byte[] salt) => Rfc2898DeriveBytes.Pbkdf2(pin, salt, 10_000, HashAlgorithmName.SHA1, 24);

    private static ApduResponse ReplyToAuthentication(TripleDES cipher, ReadOnlySpan<byte> command)
    {
        ReadOnlySpan<byte> witness = [1, 2, 3, 4, 5, 6, 7, 8];
        Span<byte> response = [0x7C, 10, 0x80, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0x90, 0];
        if (command.Length == 4)
            cipher.EncryptEcb(witness, response.Slice(4, 8), PaddingMode.None);
        else
        {
            Assert.True(CryptographicOperations.FixedTimeEquals(witness, command.Slice(4, 8)));
            response[2] = 0x82;
            cipher.EncryptEcb(command.Slice(14, 8), response.Slice(4, 8), PaddingMode.None);
        }
        // The fake creates its own staging array; ApduResponse owns a distinct copy.
        byte[] staging = response.ToArray();
        try
        {
            return new ApduResponse(staging);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(staging);
            CryptographicOperations.ZeroMemory(response);
        }
    }

    private static bool IsPrintedPut(ApduCommand command) => command.Ins == 0xDB && command.Data.Span[4] == 9;

    private sealed class Observation(IBackend backend) : IBackend
    {
        private ReadOnlyMemory<byte> _printedResponse;
        private ReadOnlyMemory<byte> _printedWrite;
        private ReadOnlyMemory<byte> _keyWrite;
        public Func<ApduCommand, ApduResponse?>? BeforeSend { get; set; }
        public Action<ApduCommand>? AfterSend { get; set; }

        public Task<Initialization> ReadInitialStateAsync(CancellationToken cancellationToken = default) =>
            backend.ReadInitialStateAsync(cancellationToken);

        public async Task<ApduResponse> SendAsync(ApduCommand command, bool throwOnError = true, CancellationToken cancellationToken = default)
        {
            // These aliases refer to actual non-pooled storage, not a disposed API projection or fake copy.
            if (IsPrintedPut(command))
                _printedWrite = command.Data;
            if (command.Ins == 0xFF)
                _keyWrite = command.Data;
            var response = BeforeSend?.Invoke(command) ?? await backend.SendAsync(command, throwOnError, cancellationToken);
            if (command.Ins == 0xCB && command.Data.Span[4] == 9)
                _printedResponse = response.RawData;
            AfterSend?.Invoke(command);
            return response;
        }

        public void AssertPrintedResponseCleared() => AssertCleared(_printedResponse);
        public void AssertKeyWriteCleared() => AssertCleared(_keyWrite);
        public void AssertPrintedResponseAndWriteCleared()
        {
            AssertCleared(_printedResponse);
            AssertCleared(_printedWrite);
        }

        private static void AssertCleared(ReadOnlyMemory<byte> memory)
        {
            Assert.False(memory.IsEmpty);
            Assert.True(memory.Span.IndexOfAnyExcept((byte)0) < 0);
        }
    }
}