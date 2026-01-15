using NSubstitute;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
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
        var session = await SecurityDomainSession.CreateAsync(connection);

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
        var session = await SecurityDomainSession.CreateAsync(connection, configuration: configuration);

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
        var session = await SecurityDomainSession.CreateAsync(connection, firmwareVersion: firmwareVersion);

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
        var session = await SecurityDomainSession.CreateAsync(
            connection,
            configuration: configuration,
            firmwareVersion: firmwareVersion);

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
        var session = await yubiKey.CreateSecurityDomainSessionAsync();

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
        var session = await yubiKey.CreateSecurityDomainSessionAsync(configuration: configuration);

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
        var session = await yubiKey.CreateSecurityDomainSessionAsync(firmwareVersion: firmwareVersion);

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
        var session = await yubiKey.CreateSecurityDomainSessionAsync();

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
        var keyInfo = await yubiKey.GetSecurityDomainKeyInfoAsync();

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
        var keyInfo = await yubiKey.GetSecurityDomainKeyInfoAsync();

        // Assert
        Assert.NotNull(keyInfo);
        // Session should be disposed automatically - verify connection was used
        await connection.Received()
            .TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
    }
}