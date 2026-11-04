using System.Reflection;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Piv.Backends;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests;

public class SessionConsistencyTests
{
    [Fact]
    public async Task Generate_MatchPolicy_ProbesCapabilityWithoutRequiringEnrollment()
    {
        using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = ec.ExportParameters(false);
        byte[] publicResponse = [0x7F, 0x49, 0x43, 0x86, 0x41, 0x04,
            .. parameters.Q.X ?? throw new InvalidOperationException(), .. parameters.Q.Y ?? throw new InvalidOperationException(), 0x90, 0x00];
        var connection = Connection([0x06, 0x01, 0x04, 0x07, 0x01, 0x00, 0x90, 0x00], publicResponse);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        typeof(PivSession).GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(session, true);

        var publicKey = await session.GenerateKeyAsync(Slot.Authentication, KeyType.ECP256,
            new KeyCreationOptions { PinPolicy = PinPolicy.MatchOnce }, TestContext.Current.CancellationToken);

        Assert.Equal(KeyType.ECP256, publicKey.KeyType);
        Assert.Equal(0xF7, connection.TransmittedCommands[^2][1]);
        Assert.Equal(0x96, connection.TransmittedCommands[^2][3]);
        Assert.Equal(0x47, connection.TransmittedCommands[^1][1]);
    }
    [Fact]
    public async Task Authenticate_CancelledAttempt_ClearsPriorAuthenticationWithoutTraffic()
    {
        var connection = Connection();
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        typeof(PivSession).GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(session, true);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => session.AuthenticateAsync(new byte[24], cancellation.Token));

        Assert.False(session.IsManagementKeyAuthenticated);
        Assert.Equal(3, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData("generate")]
    [InlineData("move")]
    [InlineData("delete")]
    [InlineData("attest")]
    public async Task KeyOperation_UnsupportedInstruction_IsNotConfusedWithOtherDeviceErrors(string operation)
    {
        var connection = Connection([0x6D, 0x00], [0x6A, 0x80]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        typeof(PivSession).GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(session, true);
        Task Invoke() => operation switch
        {
            "generate" => session.GenerateKeyAsync(Slot.Authentication, KeyType.ECP256, cancellationToken: TestContext.Current.CancellationToken),
            "move" => session.MoveKeyAsync(Slot.Authentication, Slot.Signature, TestContext.Current.CancellationToken),
            "delete" => session.DeleteKeyAsync(Slot.Authentication, TestContext.Current.CancellationToken),
            _ => session.AttestKeyAsync(Slot.Authentication, TestContext.Current.CancellationToken)
        };

        await Assert.ThrowsAsync<NotSupportedException>(Invoke);
        var exception = await Assert.ThrowsAsync<ApduException>(Invoke);
        Assert.True(exception.SW == 0x6A80);
    }

    [Fact]
    public async Task RetryQuery_ValidUnknownOptionalFields_ReturnsActualReportedCount()
    {
        var connection = Connection([0x9F, 0x33, 0x01, 0x42, 0x05, 0x01, 0x00, 0x06, 0x02, 0x09, 0x04, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(4, await session.GetPinAttemptsAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0xF7, connection.TransmittedCommands[^1][1]);
    }

    [Theory]
    [InlineData(new byte[] { 0x05, 0x01, 0x01 })]
    [InlineData(new byte[] { 0x01, 0x02, 0x0A, 0x00, 0x02, 0x02, 0x00, 0x00, 0x05, 0x01, 0x01 })]
    public async Task Initialize_MalformedManagementMetadata_DoesNotFallback(byte[] metadata)
    {
        var connection = new RecordingSmartCardConnection([0x90, 0x00], [5, 7, 0, 0x90, 0x00], [.. metadata, 0x90, 0x00]);

        await Assert.ThrowsAnyAsync<Exception>(() => PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(3, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(PinPolicy.MatchOnce)]
    [InlineData(PinPolicy.MatchAlways)]
    public async Task Generate_BiometricPolicyOnNonBioKey_DoesNotMutate(PinPolicy policy)
    {
        var connection = Connection([0x6A, 0x88]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        typeof(PivSession).GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(session, true);

        await Assert.ThrowsAsync<NotSupportedException>(() => session.GenerateKeyAsync(Slot.Authentication, KeyType.ECP256,
            new KeyCreationOptions { PinPolicy = policy }, TestContext.Current.CancellationToken));

        Assert.Equal(0xF7, connection.TransmittedCommands[^1][1]);
        Assert.Equal(0x96, connection.TransmittedCommands[^1][3]);
        Assert.DoesNotContain(connection.TransmittedCommands, command => command[1] == 0x47);
    }

    [Theory]
    [InlineData("puk")]
    [InlineData("unblock")]
    [InlineData("retries")]
    public async Task UnsupportedAuthenticationInstruction_IsDistinctFromAuthorizationFailure(string operation)
    {
        var connection = Connection([0x6D, 0x00], [0x69, 0x82]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        typeof(PivSession).GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(session, true);
        Task Invoke() => operation switch
        {
            "puk" => session.ChangePukAsync("12345678"u8.ToArray(), "87654321"u8.ToArray(), TestContext.Current.CancellationToken),
            "unblock" => session.UnblockPinAsync("12345678"u8.ToArray(), "123456"u8.ToArray(), TestContext.Current.CancellationToken),
            _ => session.SetPinAttemptsAsync(3, 3, TestContext.Current.CancellationToken)
        };

        await Assert.ThrowsAsync<NotSupportedException>(Invoke);
        var exception = await Assert.ThrowsAsync<ApduException>(Invoke);
        Assert.True(exception.SW == 0x6982);
    }

    [Theory]
    [InlineData("pin")]
    [InlineData("puk")]
    [InlineData("management")]
    [InlineData("slot")]
    [InlineData("aes")]
    public async Task FirmwareGatedOperation_OldFirmware_RejectsBeforeTransmission(string operation)
    {
        var connection = Connection();
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { FirmwareVersionOverride = new(4, 3, 0) }, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<NotSupportedException>(() => operation switch
        {
            "pin" => session.GetPinMetadataAsync(TestContext.Current.CancellationToken),
            "puk" => session.GetPukMetadataAsync(TestContext.Current.CancellationToken),
            "management" => session.GetManagementKeyMetadataAsync(TestContext.Current.CancellationToken),
            "slot" => session.GetSlotMetadataAsync(Slot.Authentication, TestContext.Current.CancellationToken),
            _ => session.SetManagementKeyAsync(KeyType.AES192, new byte[24], cancellationToken: TestContext.Current.CancellationToken)
        });

        Assert.False(session.IsSupported(Features.Metadata));
        Assert.Equal(3, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(3, 4, 0, KeyType.ECP384, PinPolicy.Default, TouchPolicy.Default)]
    [InlineData(3, 4, 0, KeyType.ECP256, PinPolicy.Once, TouchPolicy.Default)]
    [InlineData(4, 2, 9, KeyType.ECP256, PinPolicy.Default, TouchPolicy.Cached)]
    [InlineData(5, 6, 9, KeyType.Ed25519, PinPolicy.Default, TouchPolicy.Default)]
    [InlineData(5, 6, 9, KeyType.X25519, PinPolicy.Default, TouchPolicy.Default)]
    [InlineData(5, 6, 9, KeyType.RSA3072, PinPolicy.Default, TouchPolicy.Default)]
    [InlineData(5, 6, 9, KeyType.RSA4096, PinPolicy.Default, TouchPolicy.Default)]
    [InlineData(5, 7, 0, KeyType.ECP521, PinPolicy.Default, TouchPolicy.Default)]
    [InlineData(5, 7, 0, KeyType.AES256, PinPolicy.Default, TouchPolicy.Default)]
    public async Task Generate_UnsupportedKeyOrPolicy_DoesNotTransmit(int major, int minor, int patch,
        KeyType keyType, PinPolicy pinPolicy, TouchPolicy touchPolicy)
    {
        var connection = Connection();
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { FirmwareVersionOverride = new(major, minor, patch) }, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<NotSupportedException>(() => session.GenerateKeyAsync(Slot.Authentication, keyType,
            new KeyCreationOptions { PinPolicy = pinPolicy, TouchPolicy = touchPolicy }, TestContext.Current.CancellationToken));

        Assert.Equal(3, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task Import_RocaFirmware_RemainsAllowedAndUsesPivWireIdentifier()
    {
        var connection = Connection([0x90, 0x00], [0x90, 0x00], [0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { FirmwareVersionOverride = new(4, 3, 4) }, TestContext.Current.CancellationToken);
        typeof(PivSession).GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(session, true);
        using var rsa = RSA.Create(2048);
        using var privateKey = RSAPrivateKey.CreateFromParameters(rsa.ExportParameters(true));

        await session.ImportKeyAsync(Slot.Authentication, privateKey,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(connection.TransmittedCommands, command => command[1] == 0xFE && command[2] == 0x07);
    }

    [Fact]
    public async Task RetryQuery_CancelledMetadata_DoesNotSendFallback()
    {
        var connection = Connection();
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var backend = new CancelledBackend();
        typeof(PivSession).GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(session, backend);

        await Assert.ThrowsAsync<OperationCanceledException>(() => session.GetPinAttemptsAsync(TestContext.Current.CancellationToken));

        Assert.Single(backend.Commands);
        Assert.Equal(0xF7, backend.Commands[0].Ins);
    }

    [Fact]
    public async Task UnknownAlgorithm_MetadataIsInspectable_ButCannotSign()
    {
        byte[] raw = [0x01, 0x01, 0xF1, 0x02, 0x02, 0x02, 0x01, 0x03, 0x01, 0x01, 0x04, 0x00, 0x09, 0x01, 0x42];
        var connection = Connection([.. raw, 0x90, 0x00], [.. raw, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var metadata = Assert.IsType<SlotMetadata>(await session.GetSlotMetadataAsync(Slot.Authentication, TestContext.Current.CancellationToken));

        Assert.Null(metadata.KeyType);
        Assert.Equal(0xF1, metadata.RawAlgorithmId);
        Assert.Equal(raw, metadata.RawData.ToArray());
        await Assert.ThrowsAsync<NotSupportedException>(() => session.SignOrDecryptAsync(Slot.Authentication,
            ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken));
        Assert.DoesNotContain(connection.TransmittedCommands, command => command[1] == 0x87);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UnknownAlgorithm_ParsedMetadataExtraction_ReportsUnsupportedAlgorithm(bool extractRsa)
    {
        byte[] raw = [0x01, 0x01, 0xF1, 0x02, 0x02, 0x02, 0x01, 0x03, 0x01, 0x01, 0x04, 0x00];
        var connection = Connection([.. raw, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var metadata = Assert.IsType<SlotMetadata>(await session.GetSlotMetadataAsync(Slot.Authentication, TestContext.Current.CancellationToken));

        var exception = Assert.Throws<NotSupportedException>(() =>
        {
            using AsymmetricAlgorithm key = extractRsa ? metadata.GetRsaPublicKey() : metadata.GetECDsaPublicKey();
        });

        Assert.Contains("0xF1", exception.Message);
        Assert.Equal(raw, metadata.RawData.ToArray());
        Assert.Equal(4, connection.TransmittedCommands.Count);
    }

    [Theory]
    [InlineData(new byte[] { 0x02, 0x02, 0x02, 0x01 })]
    [InlineData(new byte[] { 0x01, 0x02, 0x11, 0x00 })]
    [InlineData(new byte[] { 0x01, 0x01, 0x11, 0x02, 0x01, 0x02 })]
    [InlineData(new byte[] { 0x01, 0x01, 0x11 })]
    public async Task SlotMetadata_MalformedKnownFields_AreNotInterpretedAsDefaults(byte[] raw)
    {
        var connection = Connection([.. raw, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<Exception>(() => session.GetSlotMetadataAsync(Slot.Authentication, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(KeyType.RSA1024, 0x06)]
    [InlineData(KeyType.RSA2048, 0x07)]
    [InlineData(KeyType.RSA3072, 0x05)]
    [InlineData(KeyType.RSA4096, 0x16)]
    [InlineData(KeyType.ECP256, 0x11)]
    [InlineData(KeyType.ECP384, 0x14)]
    [InlineData(KeyType.Ed25519, 0xE0)]
    [InlineData(KeyType.X25519, 0xE1)]
    public async Task Generate_UsesExplicitPivAlgorithmBytes(KeyType keyType, int algorithmId)
    {
        var connection = Connection([0x6A, 0x80]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        typeof(PivSession).GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(session, true);

        await Assert.ThrowsAsync<ApduException>(() => session.GenerateKeyAsync(Slot.Authentication, keyType,
            cancellationToken: TestContext.Current.CancellationToken));

        byte[] command = connection.TransmittedCommands[^1];
        Assert.Equal(0x47, command[1]);
        Assert.Equal(new byte[] { 0xAC, 0x03, 0x80, 0x01, (byte)algorithmId }, command.AsSpan(5, 5).ToArray());
    }

    [Theory]
    [InlineData(0x90, 0x00)]
    [InlineData(0x69, 0x82)]
    [InlineData(0x6A, 0x80)]
    public async Task RetryQuery_WithoutReportedCount_DoesNotInventThree(int sw1, int sw2)
    {
        var connection = Connection([0x6D, 0x00], [(byte)sw1, (byte)sw2]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Record.ExceptionAsync(() => session.GetPinAttemptsAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<Exception>(exception);
        Assert.Equal(0x20, connection.TransmittedCommands[^1][1]);
        Assert.True(connection.TransmittedCommands[^1].Length <= 5); // No credential data.
    }

    [Theory]
    [InlineData(new byte[] { 0x05, 0x01, 0x01, 0x06, 0x02, 0x03 })]
    [InlineData(new byte[] { 0x05, 0x01, 0x01 })]
    [InlineData(new byte[] { 0x05, 0x01, 0x01, 0x06, 0x02, 0x03, 0x04 })]
    public async Task RetryQuery_MalformedMetadata_NeverFallsBack(byte[] metadata)
    {
        var connection = Connection([.. metadata, 0x90, 0x00], [0x63, 0xC3]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApduException>(() => session.GetPinAttemptsAsync(TestContext.Current.CancellationToken));
        Assert.Null(exception.SW); // A malformed success payload is not an unsupported instruction.

        Assert.Equal(4, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task Generate_RocaFirmware_IsRejectedBeforeMutation()
    {
        var connection = Connection();
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { FirmwareVersionOverride = new(4, 3, 4) }, TestContext.Current.CancellationToken);
        typeof(PivSession).GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(session, true);

        await Assert.ThrowsAsync<NotSupportedException>(() => session.GenerateKeyAsync(
            Slot.Authentication, KeyType.RSA2048, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(3, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task Reset_OverrideDoesNotRedefinePhysicalDefaultManagementKey()
    {
        var connection = new RecordingSmartCardConnection(
            [0x90, 0x00], [0x05, 0x07, 0x00, 0x90, 0x00], [0x6D, 0x00],
            [0x69, 0x83], [0x69, 0x83], [0x90, 0x00], [0x6D, 0x00]);
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { FirmwareVersionOverride = new(4, 3, 0) }, TestContext.Current.CancellationToken);

        await session.ResetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(KeyType.AES192, session.ManagementKeyType);
    }

    private static RecordingSmartCardConnection Connection(params byte[][] responses) => new(
        [new byte[] { 0x90, 0x00 }, new byte[] { 0x05, 0x07, 0x00, 0x90, 0x00 },
            new byte[] { 0x01, 0x01, 0x0A, 0x02, 0x02, 0x00, 0x00, 0x05, 0x01, 0x01, 0x90, 0x00 }, .. responses]);

    private sealed class CancelledBackend : IBackend
    {
        public List<ApduCommand> Commands { get; } = [];
        public Task<Initialization> ReadInitialStateAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ReadOnlyMemory<byte>> SelectAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ApduResponse> SendAsync(ApduCommand command, bool throwOnError = true, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return Task.FromException<ApduResponse>(new OperationCanceledException());
        }
    }
}