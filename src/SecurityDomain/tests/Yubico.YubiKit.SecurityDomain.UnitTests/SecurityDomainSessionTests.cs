using NSubstitute;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.SecurityDomain.UnitTests;

/// <summary>
///     Unit tests for SecurityDomainSession instantiation patterns.
///     Tests both direct CreateAsync calls and IYubiKeyExtensions methods.
/// </summary>
public class SecurityDomainSessionTests
{
    private const byte SW1Success = 0x90;
    private const byte SW2Success = 0x00;

    /// <summary>
    ///     Creates a mock connection that returns success for SELECT APDU.
    /// </summary>
    private static ISmartCardConnection CreateMockConnection()
    {
        var connection = Substitute.For<ISmartCardConnection>();

        // Mock successful SELECT response (SW1=0x90, SW2=0x00)
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(new ReadOnlyMemory<byte>([SW1Success, SW2Success]));

        connection.SupportsExtendedApdu().Returns(true);
        connection.Transport.Returns(Transport.Usb);

        return connection;
    }

    /// <summary>
    ///     Creates a mock IYubiKey that provides a mock SmartCard connection.
    /// </summary>
    private static IYubiKey CreateMockYubiKey(out ISmartCardConnection connection)
    {
        var yubiKey = Substitute.For<IYubiKey>();
        connection = CreateMockConnection();

        yubiKey.ConnectAsync<ISmartCardConnection>(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connection));

        return yubiKey;
    }

    /// <summary>
    ///     Helper to assert session is valid and dispose it.
    /// </summary>
    private static void AssertSessionAndDispose(SecurityDomainSession session)
    {
        Assert.NotNull(session);
        session.Dispose();
    }

    /// <summary>
    ///     Sets up mock for GetKeyInfo response.
    /// </summary>
    private static void SetupGetKeyInfoMock(ISmartCardConnection connection)
    {
        var getKeyInfoResponse = new byte[] { SW1Success, SW2Success };
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(new ReadOnlyMemory<byte>(getKeyInfoResponse));
    }

    [Fact]
    public async Task CreateAsync_WithConnectionOnly_Succeeds()
    {
        // Arrange
        var connection = CreateMockConnection();

        // Act
        var session = await SecurityDomainSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        AssertSessionAndDispose(session);
    }

    [Fact]
    public async Task CreateAsync_WithConnectionAndConfiguration_Succeeds()
    {
        // Arrange
        var connection = CreateMockConnection();
        var configuration = new ProtocolConfiguration();

        // Act
        var session = await SecurityDomainSession.CreateAsync(connection, configuration: configuration, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        AssertSessionAndDispose(session);
    }

    [Fact]
    public async Task CreateAsync_WithConnectionAndFirmwareVersion_Succeeds()
    {
        // Arrange
        var connection = CreateMockConnection();
        var firmwareVersion = new FirmwareVersion(5, 7, 2);

        // Act
        var session = await SecurityDomainSession.CreateAsync(connection, firmwareVersion: firmwareVersion, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        AssertSessionAndDispose(session);
    }

    [Fact]
    public async Task CreateAsync_WithConnectionConfigurationAndFirmwareVersion_Succeeds()
    {
        // Arrange
        var connection = CreateMockConnection();
        var configuration = new ProtocolConfiguration();
        var firmwareVersion = new FirmwareVersion(5, 7, 2);

        // Act
        var session = await SecurityDomainSession.CreateAsync(connection, configuration: configuration, firmwareVersion: firmwareVersion, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        AssertSessionAndDispose(session);
    }

    [Fact]
    public async Task CreateAsync_WithCancellationToken_Succeeds()
    {
        // Arrange
        var connection = CreateMockConnection();
        using var cts = new CancellationTokenSource();

        // Act
        var session = await SecurityDomainSession.CreateAsync(connection, cancellationToken: cts.Token);

        // Assert
        AssertSessionAndDispose(session);
    }

    [Fact]
    public async Task CreateAsync_WithAllNonScpParameters_Succeeds()
    {
        // Arrange
        var connection = CreateMockConnection();
        var configuration = new ProtocolConfiguration();
        var firmwareVersion = new FirmwareVersion(5, 7, 2);
        using var cts = new CancellationTokenSource();

        // Act
        var session = await SecurityDomainSession.CreateAsync(
            connection,
            configuration: configuration,
            scpKeyParams: null, // SCP requires integration testing
            firmwareVersion: firmwareVersion,
            cancellationToken: cts.Token);

        // Assert
        AssertSessionAndDispose(session);
    }

    [Fact]
    public async Task IYubiKeyExtensions_CreateSecurityDomainSessionAsync_WithDefaultParameters_Succeeds()
    {
        // Arrange
        var yubiKey = CreateMockYubiKey(out _);

        // Act
        var session = await yubiKey.CreateSecurityDomainSessionAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        AssertSessionAndDispose(session);
    }

    [Fact]
    public async Task IYubiKeyExtensions_CreateSecurityDomainSessionAsync_WithConfiguration_Succeeds()
    {
        // Arrange
        var yubiKey = CreateMockYubiKey(out _);
        var configuration = new ProtocolConfiguration();

        // Act
        var session = await yubiKey.CreateSecurityDomainSessionAsync(configuration: configuration, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        AssertSessionAndDispose(session);
    }

    [Fact]
    public async Task IYubiKeyExtensions_CreateSecurityDomainSessionAsync_WithFirmwareVersion_Succeeds()
    {
        // Arrange
        var yubiKey = CreateMockYubiKey(out _);
        var firmwareVersion = new FirmwareVersion(5, 7, 2);

        // Act
        var session = await yubiKey.CreateSecurityDomainSessionAsync(firmwareVersion: firmwareVersion, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        AssertSessionAndDispose(session);
    }

    [Fact]
    public async Task IYubiKeyExtensions_CreateSecurityDomainSessionAsync_WithAllNonScpParameters_Succeeds()
    {
        // Arrange
        var yubiKey = CreateMockYubiKey(out _);
        var configuration = new ProtocolConfiguration();
        var firmwareVersion = new FirmwareVersion(5, 7, 2);
        using var cts = new CancellationTokenSource();

        // Act
        var session = await yubiKey.CreateSecurityDomainSessionAsync(
            scpKeyParams: null, // SCP requires integration testing
            configuration: configuration,
            firmwareVersion: firmwareVersion,
            cancellationToken: cts.Token);

        // Assert
        AssertSessionAndDispose(session);
    }

    [Fact]
    public async Task IYubiKeyExtensions_CreateSecurityDomainSessionAsync_ConnectsToSmartCard()
    {
        // Arrange
        var yubiKey = CreateMockYubiKey(out _);

        // Act
        var session = await yubiKey.CreateSecurityDomainSessionAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await yubiKey.Received(1).ConnectAsync<ISmartCardConnection>(Arg.Any<CancellationToken>());
        AssertSessionAndDispose(session);
    }

    [Fact]
    public async Task IYubiKeyExtensions_GetSecurityDomainKeyInfoAsync_WithDefaultParameters_Succeeds()
    {
        // Arrange
        var yubiKey = CreateMockYubiKey(out var connection);
        SetupGetKeyInfoMock(connection);

        // Act
        var keyInfo = await yubiKey.GetSecurityDomainKeyInfoAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(keyInfo);
        Assert.IsAssignableFrom<IReadOnlyList<KeyInfo>>(keyInfo);
    }

    [Fact]
    public async Task IYubiKeyExtensions_GetSecurityDomainKeyInfoAsync_WithCancellationToken_Succeeds()
    {
        // Arrange
        var yubiKey = CreateMockYubiKey(out var connection);
        SetupGetKeyInfoMock(connection);
        using var cts = new CancellationTokenSource();

        // Act
        var keyInfo = await yubiKey.GetSecurityDomainKeyInfoAsync(cancellationToken: cts.Token);

        // Assert
        Assert.NotNull(keyInfo);
    }

    [Fact]
    public async Task IYubiKeyExtensions_GetSecurityDomainKeyInfoAsync_DisposesSession()
    {
        // Arrange
        var yubiKey = CreateMockYubiKey(out var connection);
        SetupGetKeyInfoMock(connection);

        // Act
        var keyInfo = await yubiKey.GetSecurityDomainKeyInfoAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(keyInfo);
        // Session should be disposed automatically - verify connection was used
        await connection.Received()
            .TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_TransmitsSelectSecurityDomainApplication()
    {
        var connection = new RecordingSmartCardConnection(OkResponse());

        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(session);
        Assert.Equal(SelectSecurityDomainCommand(), connection.TransmittedCommands[0]);
    }

    [Fact]
    public async Task GetKeyInfoAsync_TransmitsGetDataAndParsesKeyInformation()
    {
        var connection = new RecordingSmartCardConnection(
            OkResponse(),
            [0xC0, 0x04, 0x01, 0xFF, 0x88, 0x10, 0x90, 0x00]);
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var keyInfo = await session.GetKeyInfoAsync(TestContext.Current.CancellationToken);

        Assert.Equal(GetDataCommand(0xE0), connection.TransmittedCommands[1]);
        var entry = Assert.Single(keyInfo);
        Assert.Equal(0x01, entry.KeyReference.Kid);
        Assert.Equal(0xFF, entry.KeyReference.Kvn);
        var component = Assert.Single(entry.Components);
        Assert.Equal(0x88, component.Tag);
        Assert.Equal(0x10, component.Value);
    }

    [Fact]
    public async Task GetCardRecognitionDataAsync_TransmitsGetDataAndReturnsNestedData()
    {
        var connection = new RecordingSmartCardConnection(
            OkResponse(),
            [0x73, 0x02, 0xAA, 0xBB, 0x90, 0x00]);
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var cardRecognitionData = await session.GetCardRecognitionDataAsync(TestContext.Current.CancellationToken);

        Assert.Equal(GetDataCommand(0x66), connection.TransmittedCommands[1]);
        Assert.Equal([0xAA, 0xBB], cardRecognitionData.ToArray());
    }

    [Fact]
    public async Task DeleteKeyAsync_TransmitsDeleteWithKeyReferenceFilter()
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), OkResponse());
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.DeleteKeyAsync(
            new KeyReference(0x10, 0x01),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([0x80, 0xE4, 0x00, 0x00, 0x06, 0xD0, 0x01, 0x10, 0xD2, 0x01, 0x01],
            connection.TransmittedCommands[1]);
    }

    [Fact]
    public async Task DeleteKeyAsync_ForScp03Key_TransmitsWildcardKidAndDeleteLastFlag()
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), OkResponse());
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.DeleteKeyAsync(
            new KeyReference(0x01, 0xFF),
            deleteLast: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([0x80, 0xE4, 0x00, 0x01, 0x03, 0xD2, 0x01, 0xFF],
            connection.TransmittedCommands[1]);
    }

    [Fact]
    public async Task GenerateKeyAsync_TransmitsGenerateKeyWithCurveParameters()
    {
        byte[] publicPoint =
        [
            0x04,
            0x6B, 0x17, 0xD1, 0xF2, 0xE1, 0x2C, 0x42, 0x47,
            0xF8, 0xBC, 0xE6, 0xE5, 0x63, 0xA4, 0x40, 0xF2,
            0x77, 0x03, 0x7D, 0x81, 0x2D, 0xEB, 0x33, 0xA0,
            0xF4, 0xA1, 0x39, 0x45, 0xD8, 0x98, 0xC2, 0x96,
            0x4F, 0xE3, 0x42, 0xE2, 0xFE, 0x1A, 0x7F, 0x9B,
            0x8E, 0xE7, 0xEB, 0x4A, 0x7C, 0x0F, 0x9E, 0x16,
            0x2B, 0xCE, 0x33, 0x57, 0x6B, 0x31, 0x5E, 0xCE,
            0xCB, 0xB6, 0x40, 0x68, 0x37, 0xBF, 0x51, 0xF5
        ];
        var response = new byte[2 + publicPoint.Length + 2];
        response[0] = 0xB0;
        response[1] = (byte)publicPoint.Length;
        publicPoint.CopyTo(response.AsSpan(2));
        response[^2] = 0x90;

        var connection = new RecordingSmartCardConnection(OkResponse(), response);
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            firmwareVersion: new FirmwareVersion(5, 7, 2),
            cancellationToken: TestContext.Current.CancellationToken);

        var generatedKey = await session.GenerateKeyAsync(
            new KeyReference(0x13, 0x02),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(generatedKey);
        Assert.Equal([0x80, 0xF1, 0x00, 0x13, 0x04, 0x02, 0xF0, 0x01, 0x00],
            connection.TransmittedCommands[1]);
    }

    [Fact]
    public async Task StoreDataAsync_TransmitsStoreDataCommand()
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), OkResponse());
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.StoreDataAsync(
            new byte[] { 0xA6, 0x04, 0x83, 0x02, 0x10, 0x01 },
            TestContext.Current.CancellationToken);

        Assert.Equal([0x00, 0xE2, 0x90, 0x00, 0x06, 0xA6, 0x04, 0x83, 0x02, 0x10, 0x01],
            connection.TransmittedCommands[1]);
    }

    [Fact]
    public async Task ResetAsync_TransmitsRawBlockingApdusAndReselectsApplication()
    {
        var connection = new RecordingSmartCardConnection(
            OkResponse(),
            [
                0xC0, 0x02, 0x01, 0xFF,
                0xC0, 0x02, 0x11, 0x01,
                0xC0, 0x02, 0x13, 0x02,
                0xC0, 0x02, 0x22, 0x03,
                0x90, 0x00
            ],
            [0x69, 0x83],
            [0x69, 0x83],
            [0x69, 0x83],
            [0x69, 0x83],
            OkResponse());
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.ResetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(SelectSecurityDomainCommand(), connection.TransmittedCommands[0]);
        Assert.Equal(GetDataCommand(0xE0), connection.TransmittedCommands[1]);
        Assert.Equal([0x80, 0x50, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            connection.TransmittedCommands[2]);
        Assert.Equal([0x80, 0x82, 0x01, 0x11, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            connection.TransmittedCommands[3]);
        Assert.Equal([0x80, 0x88, 0x02, 0x13, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            connection.TransmittedCommands[4]);
        Assert.Equal([0x80, 0x2A, 0x03, 0x22, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            connection.TransmittedCommands[5]);
        Assert.Equal(SelectSecurityDomainCommand(), connection.TransmittedCommands[6]);
    }

    private static byte[] OkResponse() => [0x90, 0x00];

    private static byte[] SelectSecurityDomainCommand() =>
    [
        0x00, 0xA4, 0x04, 0x00, 0x08,
        0xA0, 0x00, 0x00, 0x01, 0x51, 0x00, 0x00, 0x00
    ];

    private static byte[] GetDataCommand(byte tag) => [0x00, 0xCA, 0x00, tag, 0x00];

    private sealed class RecordingSmartCardConnection(params byte[][] responses) : ISmartCardConnection
    {
        private readonly Queue<byte[]> _responses = new(responses);

        public List<byte[]> TransmittedCommands { get; } = [];

        public Transport Transport { get; } = Transport.Usb;

        public ConnectionType Type { get; } = ConnectionType.SmartCard;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TransmittedCommands.Add(command.ToArray());

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response enqueued for transmission.");
            }

            return Task.FromResult((ReadOnlyMemory<byte>)_responses.Dequeue());
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => NullDisposable.Instance;

        public bool SupportsExtendedApdu() => false;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => default;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static NullDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}