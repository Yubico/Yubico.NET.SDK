using System.Reflection;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backend;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.Authentication;

public class PinOnlyWorkflowTests
{
    [Fact]
    public async Task Enable_InvalidPin_DoesNotStartAuthenticationOrPersistentWorkflow()
    {
        using var backend = new WorkflowBackend();
        await using var session = await CreateSessionAsync(backend);

        await Assert.ThrowsAsync<ArgumentException>(() => session.SetPinOnlyModeAsync(PivPinOnlyMode.PinProtected,
            "123"u8.ToArray(), backend.Key, TestContext.Current.CancellationToken));

        Assert.Empty(backend.Commands);
        Assert.True(session.IsManagementKeyAuthenticated);
    }

    [Fact]
    public async Task Enable_AuthenticatesThenVerifiesThenStoresThenBlocksThenUpdatesAdmin()
    {
        using var backend = new WorkflowBackend();
        await using var session = await CreateSessionAsync(backend);

        await session.SetPinOnlyModeAsync(PivPinOnlyMode.PinProtected, "123456"u8.ToArray(), backend.Key,
            TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 0x87, 0x87, 0x20, 0xDB, 0x2C, 0xCB, 0xDB }, backend.Commands.Select(c => c.Ins));
        Assert.True(session.IsManagementKeyAuthenticated);
        Assert.All(backend.SecretBuffers, buffer => Assert.All(buffer.ToArray(), b => Assert.Equal(0, b)));
        Assert.All(backend.AuthenticationResponses, response => Assert.All(response.RawData.ToArray(), b => Assert.Equal(0, b)));
    }

    [Theory]
    [InlineData(0x20)]
    [InlineData(0x2C)]
    public async Task Enable_FailureStopsPersistentWorkflowAtFailedStep(int failedInstruction)
    {
        using var backend = new WorkflowBackend { FailedInstruction = (byte)failedInstruction };
        await using var session = await CreateSessionAsync(backend);

        await Assert.ThrowsAsync<ApduException>(() => session.SetPinOnlyModeAsync(PivPinOnlyMode.PinProtected,
            "123456"u8.ToArray(), backend.Key, TestContext.Current.CancellationToken));

        Assert.Equal(failedInstruction, backend.Commands[^1].Ins);
        Assert.DoesNotContain(backend.Commands, command => command.Ins == 0xCB);
        Assert.All(backend.SecretBuffers, buffer => Assert.All(buffer.ToArray(), b => Assert.Equal(0, b)));
    }

    [Theory]
    [InlineData(PivManagementKeyType.Aes128, 16)]
    [InlineData(PivManagementKeyType.Aes192, 24)]
    [InlineData(PivManagementKeyType.Aes256, 32)]
    [InlineData(PivManagementKeyType.TripleDes, 24)]
    public async Task Disable_ReplacesTypeAppropriateDefaultBeforeDeletingRecoveryObjects(PivManagementKeyType type, int length)
    {
        using var backend = new WorkflowBackend { HasProtectedMode = true };
        await using var session = await CreateSessionAsync(backend, type);

        await session.SetPinOnlyModeAsync(PivPinOnlyMode.None, ReadOnlyMemory<byte>.Empty,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 0xCB, 0xFF, 0xDB, 0xDB }, backend.Commands.Select(c => c.Ins));
        byte[] replacement = backend.CommandData[1];
        Assert.Equal((byte)type, replacement[0]);
        Assert.Equal(length, replacement[2]);
        for (int i = 0; i < length; i++)
            Assert.Equal((byte)(i % 8 + 1), replacement[i + 3]);
        Assert.Equal(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x09, 0x53, 0x00 }, backend.CommandData[2]);
        Assert.Equal(new byte[] { 0x5C, 0x03, 0x5F, 0xFF, 0x00, 0x53, 0x00 }, backend.CommandData[3]);
        Assert.True(session.IsManagementKeyAuthenticated);
        Assert.All(backend.SecretBuffers, buffer => Assert.All(buffer.ToArray(), b => Assert.Equal(0, b)));
    }

    [Theory]
    [InlineData(0xFF, 2)]
    [InlineData(0xDB, 3)]
    [InlineData(0xDB, 4)]
    public async Task Disable_FailedReplacementOrPrintedDeletion_PreservesRemainingRecoveryObjects(int failedInstruction, int commandCount)
    {
        using var backend = new WorkflowBackend { HasProtectedMode = true, FailedInstruction = (byte)failedInstruction, FailedCommandNumber = commandCount };
        await using var session = await CreateSessionAsync(backend);

        await Assert.ThrowsAsync<ApduException>(() => session.SetPinOnlyModeAsync(PivPinOnlyMode.None,
            ReadOnlyMemory<byte>.Empty, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(commandCount, backend.Commands.Count);
    }

    [Fact]
    public async Task Disable_NoMode_DoesNotMutate()
    {
        using var backend = new WorkflowBackend();
        await using var session = await CreateSessionAsync(backend);

        await session.SetPinOnlyModeAsync(PivPinOnlyMode.None, ReadOnlyMemory<byte>.Empty,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0xCB, Assert.Single(backend.Commands).Ins);
    }

    [Fact]
    public async Task Recover_DerivedCandidate_VerifiesPinAuthenticatesAndClearsSecretPayloads()
    {
        using var backend = new WorkflowBackend { HasDerivedMode = true };
        await using var session = await CreateSessionAsync(backend);
        // Legacy PIN-derived SHA1/10,000 is a compatibility format, not a new KDF.
#pragma warning disable CA5379, CA5387
        Rfc2898DeriveBytes.Pbkdf2("123456"u8, WorkflowBackend.Salt, backend.Key, 10_000, HashAlgorithmName.SHA1);
#pragma warning restore CA5379, CA5387

        var result = await session.RecoverPinOnlyModeAsync("123456"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.PinDerived, result);
        Assert.True(session.IsManagementKeyAuthenticated);
        Assert.Equal(new byte[] { 0xCB, 0xCB, 0x20, 0x87, 0x87 }, backend.Commands.Select(c => c.Ins));
        Assert.All(backend.SecretBuffers, buffer => Assert.All(buffer.ToArray(), b => Assert.Equal(0, b)));
    }

    private static async Task<PivSession> CreateSessionAsync(WorkflowBackend backend, PivManagementKeyType type = PivManagementKeyType.Aes192)
    {
        var connection = new RecordingSmartCardConnection([0x90, 0x00], [5, 7, 0, 0x90, 0x00],
            [0x01, 0x01, (byte)type, 0x02, 0x02, 0x00, 0x00, 0x05, 0x01, 0x01, 0x90, 0x00]);
        var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        typeof(PivSession).GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(session, backend);
        typeof(PivSession).GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(session, true);
        return session;
    }

    private sealed class WorkflowBackend : IPivBackend, IDisposable
    {
        public static ReadOnlySpan<byte> Salt => [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        public byte[] Key { get; } = Enumerable.Range(1, 24).Select(i => (byte)i).ToArray();
        public bool HasProtectedMode { get; init; }
        public bool HasDerivedMode { get; init; }
        public byte FailedInstruction { get; init; }
        public int FailedCommandNumber { get; init; }
        public List<ApduCommand> Commands { get; } = [];
        public List<byte[]> CommandData { get; } = [];
        public List<ReadOnlyMemory<byte>> SecretBuffers { get; } = [];
        public List<ApduResponse> AuthenticationResponses { get; } = [];
        public Task<PivInitialization> ReadInitialStateAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<ApduResponse> SendAsync(ApduCommand command, bool throwOnError = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Commands.Add(command);
            CommandData.Add(command.Data.ToArray());
            if ((command.Ins == 0x87 && command.Data.Length > 4) || command.Ins is 0x20 or 0xFF or 0xDB or 0x2C)
                SecretBuffers.Add(command.Data);
            var response = command.Ins == FailedInstruction && (FailedCommandNumber == 0 || Commands.Count == FailedCommandNumber)
                ? Response([0x6A, 0x80])
                : Respond(command);
            if (command.Ins == 0x87)
                AuthenticationResponses.Add(response);
            return Task.FromResult(response);
        }

        private ApduResponse Respond(ApduCommand command) => command.Ins switch
        {
            0x87 => AuthenticationResponse(command.Data),
            0x2C => Response([0x69, 0x83]),
            0xCB => DataResponse(command.Data),
            _ => Response([0x90, 0x00])
        };

        private ApduResponse AuthenticationResponse(ReadOnlyMemory<byte> data)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            if (data.Length == 4)
            {
                var witness = aes.EncryptEcb(new byte[16], PaddingMode.None);
                return Response([0x7C, 0x12, 0x80, 0x10, .. witness, 0x90, 0x00]);
            }
            using var outer = Tlv.Create(data.Span);
            using var fields = TlvHelper.DecodeList(outer.Value.Span);
            var challenge = fields.Single(field => field.Tag == 0x81).Value;
            var encrypted = aes.EncryptEcb(challenge.Span, PaddingMode.None);
            return Response([0x7C, 0x12, 0x82, 0x10, .. encrypted, 0x90, 0x00]);
        }

        private ApduResponse DataResponse(ReadOnlyMemory<byte> data)
        {
            if (data.Span[^1] == 0x09)
                return Response([0x6A, 0x82]); // No PRINTED object.
            if (HasDerivedMode)
                return Response([0x53, 0x17, 0x80, 0x15, 0x81, 0x01, 0x00, 0x82, 0x10, .. Salt, 0x90, 0x00]);
            return HasProtectedMode ? Response([0x53, 0x05, 0x80, 0x03, 0x81, 0x01, 0x02, 0x90, 0x00]) : Response([0x6A, 0x82]);
        }

        private static ApduResponse Response(byte[] bytes) => new(bytes);

        public void Dispose()
        {
            CryptographicOperations.ZeroMemory(Key);
            foreach (byte[] bytes in CommandData)
                CryptographicOperations.ZeroMemory(bytes);
        }
    }
}