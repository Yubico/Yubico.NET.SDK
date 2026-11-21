using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Reflection;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.SecurityDomain.UnitTests;

/// <summary>
///     Unit tests for SecurityDomainSession instantiation patterns.
///     Tests both direct CreateAsync calls and IYubiKeyExtensions methods.
/// </summary>
public class SecurityDomainSessionTests
{
    private const byte SW1Success = 0x90;
    private const byte SW2Success = 0x00;

    [Fact]
    public async Task CreateAsync_AppletProbeFailure_DoesNotDisposeTheBorrowedConnection()
    {
        var connection = new RecordingSmartCardConnection();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SecurityDomainSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));

        // Borrowed: the session did not create this connection, so disposal is the caller's.
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task CreateAsync_Success_RetainsProtocolUntilOneSessionDisposal()
    {
        var connection = CreateMockConnection();
        var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        connection.DidNotReceive().Dispose();

        session.Dispose();
        session.Dispose();

        // Borrowed: the session did not create this connection, so disposal is the caller's.
        connection.DidNotReceive().Dispose();
    }

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
        connection.Type.Returns(ConnectionType.SmartCard);

        return connection;
    }

    /// <summary>
    ///     Creates a mock IYubiKey that provides a mock SmartCard connection.
    /// </summary>
    private static IYubiKey CreateMockYubiKey(out ISmartCardConnection connection)
    {
        var yubiKey = Substitute.For<IYubiKey>();
        connection = CreateMockConnection();
        yubiKey.AvailableConnections.Returns(ConnectionType.SmartCard);
        yubiKey.SupportsConnection(ConnectionType.SmartCard).Returns(true);

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
    public async Task DisposeAsync_ClearsManagedSessionState()
    {
        var connection = CreateMockConnection();
        var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(session.IsInitialized);

        await session.DisposeAsync();

        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
    }

    [Fact]
    public async Task Dispose_BaseTeardownThrows_ClearsDerivedStateAndDetachesConnection()
    {
        var expected = new InvalidOperationException("protocol dispose failed");
        var connection = new RecordingSmartCardConnection(OkResponse(), OkResponse());
        var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        typeof(ApplicationSession)
            .GetProperty("Protocol", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, new ThrowingProtocol(expected));

        Exception? exception = Record.Exception(session.Dispose);

        Assert.Same(expected, exception);
        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
        Assert.Null(typeof(SecurityDomainSession)
            .GetField("_protocol", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(session));
        Assert.Null(typeof(SecurityDomainSession)
            .GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(session));

        await using var subsequent = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(subsequent);
    }

    [Fact]
    public async Task ListKeyInformationAsync_AfterDisposal_ThrowsObjectDisposedException()
    {
        var connection = CreateMockConnection();
        var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();
        int transmissionsBeforeCall = connection.ReceivedCalls().Count();

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.ListKeyInformationAsync(TestContext.Current.CancellationToken));

        Assert.Equal(typeof(SecurityDomainSession).FullName, exception.ObjectName);
        Assert.Equal(transmissionsBeforeCall, connection.ReceivedCalls().Count());
    }

    [Fact]
    public async Task ResetAsync_AfterDisposal_ThrowsObjectDisposedExceptionWithoutTransmitting()
    {
        var connection = new RecordingSmartCardConnection(OkResponse());
        var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();
        int transmissionsBeforeReset = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.ResetAsync(TestContext.Current.CancellationToken));

        Assert.Equal(typeof(SecurityDomainSession).FullName, exception.ObjectName);
        Assert.Equal(transmissionsBeforeReset, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task GetDataAsync_AfterDisposal_InvalidExpectedLengthThrowsObjectDisposedBeforeValidation()
    {
        var connection = new RecordingSmartCardConnection(OkResponse());
        var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();
        int transmissionsBeforeCall = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.GetDataAsync(0x66, expectedResponseLength: -1, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(typeof(SecurityDomainSession).FullName, exception.ObjectName);
        Assert.Equal(transmissionsBeforeCall, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task CreateAsync_WithConnectionAndConfiguration_Succeeds()
    {
        var connection = CreateMockConnection();
        var commands = new List<byte[]>();
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                commands.Add(call.Arg<ReadOnlyMemory<byte>>().ToArray());
                return new ReadOnlyMemory<byte>(OkResponse());
            });
        var configuration = new ProtocolConfiguration { ForceShortApdus = true };

        await using var session = await SecurityDomainSession.CreateAsync(
            connection,
            new SessionCreationOptions { ProtocolConfiguration = configuration },
            TestContext.Current.CancellationToken);

        await session.GetDataAsync(0x66, new byte[] { 0xAA }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([0x00, 0xCA, 0x00, 0x66, 0x01, 0xAA, 0x00], commands[1]);
    }

    [Fact]
    public async Task CreateAsync_WithConnectionAndFirmwareVersion_Succeeds()
    {
        // Arrange
        var connection = CreateMockConnection();
        var firmwareVersion = new FirmwareVersion(5, 7, 2);

        // Act
        var session = await SecurityDomainSession.CreateAsync(
            connection,
            new SessionCreationOptions { FirmwareVersionOverride = firmwareVersion },
            TestContext.Current.CancellationToken);

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
            new SessionCreationOptions
            {
                ProtocolConfiguration = configuration,
                FirmwareVersionOverride = firmwareVersion
            },
            TestContext.Current.CancellationToken);

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
            new SessionCreationOptions
            {
                ProtocolConfiguration = configuration,
                FirmwareVersionOverride = firmwareVersion
            },
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
        var session = await yubiKey.CreateSecurityDomainSessionAsync(
            new SessionCreationOptions { ProtocolConfiguration = configuration },
            TestContext.Current.CancellationToken);

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
        var session = await yubiKey.CreateSecurityDomainSessionAsync(
            new SessionCreationOptions { FirmwareVersionOverride = firmwareVersion },
            TestContext.Current.CancellationToken);

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
            new SessionCreationOptions
            {
                ProtocolConfiguration = configuration,
                FirmwareVersionOverride = firmwareVersion
            },
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
    public async Task IYubiKeyExtensions_ListKeyInformationAsync_WithDefaultParameters_Succeeds()
    {
        // Arrange
        var yubiKey = CreateMockYubiKey(out var connection);
        SetupGetKeyInfoMock(connection);

        // Act
        var keyInfo = await yubiKey.ListKeyInformationAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(keyInfo);
        Assert.IsAssignableFrom<IReadOnlyList<KeyInfo>>(keyInfo);
    }

    [Fact]
    public async Task IYubiKeyExtensions_ListKeyInformationAsync_WithCancellationToken_Succeeds()
    {
        // Arrange
        var yubiKey = CreateMockYubiKey(out var connection);
        SetupGetKeyInfoMock(connection);
        using var cts = new CancellationTokenSource();

        // Act
        var keyInfo = await yubiKey.ListKeyInformationAsync(cancellationToken: cts.Token);

        // Assert
        Assert.NotNull(keyInfo);
    }

    [Fact]
    public async Task IYubiKeyExtensions_ListKeyInformationAsync_DisposesSession()
    {
        var yubiKey = CreateMockYubiKey(out _);
        var connection = new RecordingSmartCardConnection(OkResponse(), [0xC0, 0x02, 0x01, 0xFF, 0x90, 0x00]);
        yubiKey.ConnectAsync<ISmartCardConnection>(Arg.Any<CancellationToken>()).Returns(
            Task.FromResult<ISmartCardConnection>(connection));

        var keyInfo = await yubiKey.ListKeyInformationAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0xFF, Assert.Single(keyInfo).KeyReference.Kvn);
        Assert.Equal(1, connection.DisposeCount);
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
    public async Task ListKeyInformationAsync_TransmitsGetDataAndParsesKeyInformation()
    {
        var connection = new RecordingSmartCardConnection(
            OkResponse(),
            [0xC0, 0x04, 0x01, 0xFF, 0xFE, 0x10, 0x90, 0x00]);
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var keyInfo = await session.ListKeyInformationAsync(TestContext.Current.CancellationToken);

        Assert.Equal(GetDataCommand(0xE0), connection.TransmittedCommands[1]);
        var entry = Assert.Single(keyInfo);
        Assert.Equal(0x01, entry.KeyReference.Kid);
        Assert.Equal(0xFF, entry.KeyReference.Kvn);
        var component = Assert.Single(entry.Components);
        Assert.Equal(0xFE, component.Tag); // An unknown, well-formed descriptor is retained.
        Assert.Equal(0x10, component.Value);
    }

    [Theory]
    [InlineData(new byte[] { 0xC0, 0x03, 0x01, 0xFF, 0x88 })]
    [InlineData(new byte[] { 0xC0, 0x01, 0x01 })]
    [InlineData(new byte[] { 0xC1, 0x02, 0x01, 0xFF })]
    [InlineData(new byte[] { 0xC0, 0x04, 0x01, 0xFF })]
    public async Task ListKeyInformationAsync_MalformedEntry_RejectsWholeListing(byte[] tail)
    {
        var connection = new RecordingSmartCardConnection(OkResponse(),
            [0xC0, 0x02, 0x13, 0x01, .. tail, 0x90, 0x00]);
        await using var session = await SecurityDomainSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<BadResponseException>(() => session.ListKeyInformationAsync(
            TestContext.Current.CancellationToken));
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

    [Theory]
    [InlineData(CaIdentifierType.None)]
    [InlineData((CaIdentifierType)4)]
    [InlineData(CaIdentifierType.Kloc | (CaIdentifierType)4)]
    public async Task GetCaIdentifiersAsync_UnsupportedSelection_ThrowsBeforeTransmitting(
        CaIdentifierType identifierTypes)
    {
        var connection = new RecordingSmartCardConnection(OkResponse());
        await using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);
        int transmissionsBeforeCall = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            session.GetCaIdentifiersAsync(identifierTypes, TestContext.Current.CancellationToken));

        Assert.Equal(nameof(identifierTypes), exception.ParamName);
        Assert.Equal(transmissionsBeforeCall, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task GetCaIdentifiersAsync_BothGroups_RequestsKlocBeforeKlccAndPreservesResultOrder()
    {
        byte[] kloc = [0x42, 0x01, 0xAA, 0x83, 0x02, 0x11, 0x01, 0x90, 0x00];
        byte[] klcc = [0x42, 0x01, 0xBB, 0x83, 0x02, 0x12, 0x02, 0x90, 0x00];
        var connection = new RecordingSmartCardConnection(OkResponse(), kloc, klcc);
        await using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var identifiers = await session.GetCaIdentifiersAsync(
            CaIdentifierType.Kloc | CaIdentifierType.Klcc,
            TestContext.Current.CancellationToken);

        Assert.Equal([0x00, 0xCA, 0xFF, 0x33, 0x00], connection.TransmittedCommands[1]);
        Assert.Equal([0x00, 0xCA, 0xFF, 0x34, 0x00], connection.TransmittedCommands[2]);
        Assert.Equal(2, identifiers.Count);
        Assert.Equal(0x11, identifiers[0].KeyReference.Kid);
        Assert.Equal([0xAA], identifiers[0].Identifier.ToArray());
        Assert.Equal(0x12, identifiers[1].KeyReference.Kid);
        Assert.Equal([0xBB], identifiers[1].Identifier.ToArray());
    }

    [Theory]
    [InlineData(new byte[] { 0x42, 0x01, 0xAA })]
    [InlineData(new byte[] { 0x42, 0x01, 0xAA, 0x83, 0x01, 0x13 })]
    [InlineData(new byte[] { 0x83, 0x02, 0x13, 0x01, 0x42, 0x01, 0xAA })]
    [InlineData(new byte[] { 0x42, 0x01, 0xAA, 0x83, 0x02, 0x13 })]
    public async Task GetCaIdentifiersAsync_MalformedGroup_DoesNotPairAcrossResponses(byte[] malformed)
    {
        var connection = new RecordingSmartCardConnection(OkResponse(),
            [.. malformed, 0x90, 0x00], [0x83, 0x02, 0x23, 0x01, 0x90, 0x00]);
        await using var session = await SecurityDomainSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<BadResponseException>(() => session.GetCaIdentifiersAsync(
            CaIdentifierType.Kloc | CaIdentifierType.Klcc, TestContext.Current.CancellationToken));
        Assert.Equal(2, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task GetCaIdentifiersAsync_KlocAbsent_ParsesKlccWithUnknownKeyReference()
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), [0x6A, 0x88],
            [0x42, 0x01, 0xAA, 0x83, 0x02, 0xFE, 0x12, 0x90, 0x00]);
        await using var session = await SecurityDomainSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var identifiers = await session.GetCaIdentifiersAsync(CaIdentifierType.Kloc | CaIdentifierType.Klcc,
            TestContext.Current.CancellationToken);

        var reference = Assert.Single(identifiers).KeyReference;
        Assert.Equal(0xFE, reference.Kid);
        Assert.Equal(0x12, reference.Kvn);
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

        Assert.Equal([0x80, 0xE4, 0x00, 0x00, 0x06, 0xD0, 0x01, 0x10, 0xD2, 0x01, 0x01, 0x00],
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

        Assert.Equal([0x80, 0xE4, 0x00, 0x01, 0x03, 0xD2, 0x01, 0xFF, 0x00],
            connection.TransmittedCommands[1]);
    }

    [Theory]
    [InlineData(0x01)]
    [InlineData(0x02)]
    [InlineData(0x03)]
    public async Task DeleteKeyAsync_ForScp03WithZeroVersion_DoesNotTransmitDelete(byte kid)
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), OkResponse());
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() => session.DeleteKeyAsync(
            new KeyReference(kid, 0), deleteLast: true, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Single(connection.TransmittedCommands);
    }

    [Theory]
    [InlineData(0x11, 0, 0xD0, 0x11)]
    [InlineData(0, 0x21, 0xD2, 0x21)]
    public async Task DeleteKeyAsync_WithOneWildcard_TransmitsRemainingFilter(byte kid, byte kvn, byte tag, byte value)
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), OkResponse());
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.DeleteKeyAsync(new KeyReference(kid, kvn), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([0x80, 0xE4, 0x00, 0x00, 0x03, tag, 0x01, value, 0x00], connection.TransmittedCommands[1]);
    }

    [Theory]
    [InlineData(0x11)]
    [InlineData(0x13)]
    [InlineData(0x15)]
    public async Task GenerateAndImportEcKey_TransmitExplicitP256CurveParameters(byte kid)
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

        var connection = new RecordingSmartCardConnection(OkResponse(), response, [0x03, 0x90, 0x00]);
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 7, 2) },
            cancellationToken: TestContext.Current.CancellationToken);

        var generatedKey = await session.GenerateKeyAsync(
            new KeyReference(kid, 0x02),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(generatedKey);
        Assert.Equal([0x80, 0xF1, 0x00, kid, 0x04, 0x02, 0xF0, 0x01, 0x00, 0x00],
            connection.TransmittedCommands[1]);

        await session.PutKeyAsync(new KeyReference(0x10, 0x03), generatedKey,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([0x80, 0xD8, 0x00, 0x10, 0x48, 0x03, 0xB0, 0x41, .. publicPoint,
            0xF0, 0x01, 0x00, 0x00, 0x00], connection.TransmittedCommands[2]);
    }

    [Theory]
    [InlineData(0x01)]
    [InlineData(0x10)]
    [InlineData(0x12)]
    [InlineData(0x20)]
    public async Task GenerateKeyAsync_ForNonScp11Key_DoesNotTransmitGenerate(byte kid)
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), [0x6A, 0x86]);
        await using var session = await SecurityDomainSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() => session.GenerateKeyAsync(new KeyReference(kid, 0x02),
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Single(connection.TransmittedCommands);
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

        Assert.Equal([0x00, 0xE2, 0x90, 0x00, 0x06, 0xA6, 0x04, 0x83, 0x02, 0x10, 0x01, 0x00],
            connection.TransmittedCommands[1]);
    }

    [Fact]
    public async Task StoreAllowListAsync_TransmitsStoreDataWithSerialList()
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), OkResponse());
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.StoreAllowListAsync(
            new KeyReference(0x11, 0x01),
            ["A1B2", "0001", "00"],
            TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                0x00, 0xE2, 0x90, 0x00, 0x13,
                0xA6, 0x04, 0x83, 0x02, 0x11, 0x01,
                0x70, 0x0B,
                0x93, 0x03, 0x00, 0xA1, 0xB2,
                0x93, 0x01, 0x01,
                0x93, 0x01, 0x00,
                0x00
            ],
            connection.TransmittedCommands[1]);
    }

    [Fact]
    public async Task StoreAllowListAsync_WithEmptySerial_DoesNotTransmitStoreData()
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), OkResponse());
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() => session.StoreAllowListAsync(
            new KeyReference(0x11, 0x01), ["01", ""], TestContext.Current.CancellationToken));

        Assert.Single(connection.TransmittedCommands);
    }

    [Fact]
    public async Task ClearAllowListAsync_TransmitsStoreDataWithEmptySerialList()
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), OkResponse());
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.ClearAllowListAsync(
            new KeyReference(0x11, 0x01),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                0x00, 0xE2, 0x90, 0x00, 0x08,
                0xA6, 0x04, 0x83, 0x02, 0x11, 0x01,
                0x70, 0x00,
                0x00
            ],
            connection.TransmittedCommands[1]);
    }

    [Fact]
    public async Task StoreCaIssuerAsync_TransmitsStoreDataWithKlccSkiAndKeyReference()
    {
        var connection = new RecordingSmartCardConnection(OkResponse(), OkResponse());
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        await session.StoreCaIssuerAsync(
            new KeyReference(0x13, 0x02),
            new byte[] { 0x01, 0x02, 0x03, 0x04 },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                0x00, 0xE2, 0x90, 0x00, 0x0F,
                0xA6, 0x0D,
                0x80, 0x01, 0x01,
                0x42, 0x04, 0x01, 0x02, 0x03, 0x04,
                0x83, 0x02, 0x13, 0x02,
                0x00
            ],
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
            OkResponse(),
            [0x69, 0x82]); // Even a previously plain session must attempt default SCP03.
        using var session = await SecurityDomainSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var refreshError = await Assert.ThrowsAsync<SecureChannelException>(() => session.ResetAsync(
            TestContext.Current.CancellationToken));
        Assert.Equal(unchecked((short)0x6982), refreshError.StatusWord);

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
        Assert.Equal(0x50, connection.TransmittedCommands[7][1]);
        Assert.Equal(KeyReference.Default.Kvn, connection.TransmittedCommands[7][2]);
        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
    }

    private static byte[] OkResponse() => [0x90, 0x00];

    [Fact]
    public async Task CreateSecurityDomainSessionAsync_ExplicitOverrideDoesNotReadCache()
    {
        var yubiKey = CreateMockYubiKey(out _);
        var connection = new RecordingSmartCardConnection(OkResponse(), [0x69, 0x82]);
        yubiKey.ConnectAsync<ISmartCardConnection>(Arg.Any<CancellationToken>()).Returns(
            Task.FromResult<ISmartCardConnection>(connection));
        yubiKey.CachedDeviceInfo.Returns(_ => throw new InvalidOperationException("Cache must not be read."));
        using var keys = Scp03KeyParameters.Default;

        var failure = await Assert.ThrowsAsync<SecureChannelException>(() => yubiKey.CreateSecurityDomainSessionAsync(
            new SessionCreationOptions { FirmwareVersionOverride = new(5, 3, 0), ScpKeyParameters = keys },
            TestContext.Current.CancellationToken));

        Assert.Equal(unchecked((short)0x6982), failure.StatusWord);
        Assert.Equal(0x50, connection.TransmittedCommands[1][1]);
    }

    [Fact]
    public async Task ResetAsync_PreflightTransportFailureSurvivesThrowingWarningLogger()
    {
        var connection = CreateMockConnection();
        var transportFailure = new IOException("Test key listing failed.");
        int transmissions = 0;
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++transmissions == 1
                ? Task.FromResult<ReadOnlyMemory<byte>>(OkResponse())
                : Task.FromException<ReadOnlyMemory<byte>>(transportFailure));
        await using var session = await SecurityDomainSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        // Change only this fixture's logger; do not mutate the process-wide logger factory.
        var logger = new ThrowingWarningLogger();
        var loggerField = typeof(SecurityDomainSession).GetField("_logger", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(loggerField);
        loggerField.SetValue(session, logger);

        var failure = await Assert.ThrowsAsync<IOException>(() => session.ResetAsync(TestContext.Current.CancellationToken));

        Assert.Same(transportFailure, failure);
        Assert.Equal(1, logger.WarningCount);
        Assert.Equal(2, transmissions);
        Assert.True(session.IsInitialized);
    }

    private sealed class ThrowingWarningLogger : ILogger
    {
        public int WarningCount { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                WarningCount++;
                throw new InvalidOperationException("Test warning failed.");
            }
        }
    }

    [Theory]
    [InlineData(5, VersionQualifierType.Beta, true)]
    [InlineData(5, VersionQualifierType.Final, false)]
    [InlineData(5, null, false)]
    [InlineData(0, VersionQualifierType.Final, true)]
    [InlineData(null, VersionQualifierType.Final, true)]
    public async Task CreateSecurityDomainSessionAsync_UsesFirmwareEligibilityWithoutChangingNumericFraming(
        int? major, VersionQualifierType? qualifier, bool expectHandshake)
    {
        var yubiKey = CreateMockYubiKey(out var connection);
        if (major is int numericMajor)
        {
            var version = new FirmwareVersion(numericMajor, 7, 0);
            var info = default(DeviceInfo) with { FirmwareVersion = version };
            if (qualifier is { } type)
                info = info with { VersionQualifier = new VersionQualifier(version, type, 0) };
            yubiKey.CachedDeviceInfo.Returns(info);
        }

        var commands = new List<byte[]>();
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                commands.Add(call.Arg<ReadOnlyMemory<byte>>().ToArray());
                return new ReadOnlyMemory<byte>(commands.Count == 1 ? OkResponse() : [0x69, 0x82]);
            });

        var point = Convert.FromHexString("046B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296" +
            "4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5");
        using var keys = new Scp11KeyParameters(new KeyReference(0x13, 0xFF),
            ECPublicKey.CreateFromValue(point, KeyType.P256));
        var options = new SessionCreationOptions
        {
            ProtocolConfiguration = new ProtocolConfiguration { ForceShortApdus = true },
            ScpKeyParameters = keys
        };

        var error = await Assert.ThrowsAsync<SecureChannelException>(() => yubiKey.CreateSecurityDomainSessionAsync(
            options, TestContext.Current.CancellationToken));
        if (expectHandshake)
        {
            Assert.Equal(2, commands.Count);
            Assert.Equal(unchecked((short)0x6982), error.StatusWord);
            Assert.Equal(0x88, commands[1][1]);
            if (major != 0) // The existing genuine major-zero PC/SC mode is not being changed here.
                Assert.Equal(commands[1].Length - 6, commands[1][4]);
        }
        else
        {
            Assert.Single(commands);
            Assert.IsType<NotSupportedException>(error.InnerException);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(256)]
    public async Task PutScp03KeyAsync_RejectsOutOfRangeReplacementBeforeUsingChannel(int replaceKvn)
    {
        using var keys = StaticKeys.GetDefaultKeys();
        var connection = new RecordingSmartCardConnection(OkResponse());
        await using var session = await SecurityDomainSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var failure = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.PutKeyAsync(
            new KeyReference(0x01, 0x09), keys, replaceKvn, TestContext.Current.CancellationToken));

        Assert.Equal(nameof(replaceKvn), failure.ParamName);
        Assert.Single(connection.TransmittedCommands);
    }

    [Fact]
    public async Task ResetAsync_FailedRefreshAndTeardown_PreservesRefreshErrorThroughAwaitUsing()
    {
        var yubiKey = CreateMockYubiKey(out var connection);
        var responses = new RecordingSmartCardConnection(OkResponse(),
            [0xC0, 0x02, 0x01, 0xFF, 0x90, 0x00], [0x69, 0x83], OkResponse(), [0x69, 0x82]);
        connection.TransmitAndReceiveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call => responses.TransmitAndReceiveAsync(call.Arg<ReadOnlyMemory<byte>>(),
                call.Arg<CancellationToken>()));
        connection.DisposeAsync().Returns(_ => ValueTask.FromException(new IOException("Test teardown failed.")));

        var failure = await Assert.ThrowsAsync<SecureChannelException>(async () =>
        {
            await using var session = await yubiKey.CreateSecurityDomainSessionAsync(
                cancellationToken: TestContext.Current.CancellationToken);
            await session.ResetAsync(TestContext.Current.CancellationToken);
        });

        Assert.Equal(unchecked((short)0x6982), failure.StatusWord);
        await connection.Received(1).DisposeAsync();
    }

    [Theory]
    [InlineData(9, 0x19, -1, false)]
    [InlineData(11, 0x19, -1, false)]
    [InlineData(10, 0x18, -1, false)]
    [InlineData(10, 0x19, 1, false)]
    [InlineData(10, 0x19, 4, false)]
    [InlineData(10, 0x19, 7, false)]
    [InlineData(10, 0x19, -1, true)]
    public void Scp03Kcv_RequiresExactVersionAndAllThreeValues(int responseLength, byte version, int mismatch, bool valid)
    {
        using var keys = StaticKeys.GetDefaultKeys(); // Public factory test material.
        using var aes = Aes.Create();
        aes.SetKey(keys.Enc);
        Span<byte> input = stackalloc byte[16];
        input.Fill(1);
        byte[] kcv = aes.EncryptEcb(input, PaddingMode.None);
        byte[] response = [version, .. kcv[..3], .. kcv[..3], .. kcv[..3], 0];
        if (mismatch >= 0)
            response[mismatch] ^= 0xFF;

        var exception = Record.Exception(() =>
            SecurityDomainKeyMaterial.ValidateKcv(keys, 0x19, response.AsSpan(0, responseLength)));

        if (valid)
            Assert.Null(exception);
        else
            Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public async Task ResetAsync_WithIncompleteReferences_ReportsAllOutcomesWithoutReselecting()
    {
        byte[][] responses =
        [
            OkResponse(),
            [0xC0, 0x02, 0x11, 0x01, 0xC0, 0x02, 0x13, 0x02,
                0xC0, 0x02, 0x02, 0x03, 0xC0, 0x02, 0x15, 0x04, 0x90, 0x00],
            .. Enumerable.Repeat(OkResponse(), 65),
            [0x6A, 0x86],
            [0x69, 0x82]
        ];
        var connection = new RecordingSmartCardConnection(responses);
        await using var session = await SecurityDomainSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var failure = await Assert.ThrowsAsync<AggregateException>(() => session.ResetAsync(
            TestContext.Current.CancellationToken));

        Assert.Collection(failure.InnerExceptions,
            exhausted => Assert.IsType<InvalidOperationException>(exhausted.InnerException),
            unexpected => Assert.Equal(unchecked((short)0x6A86),
                Assert.IsType<ApduException>(unexpected.InnerException).SW));
        Assert.Contains("skipped", failure.Message);
        Assert.Contains("0x6982", failure.Message);
        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
        Assert.Equal(responses.Length, connection.TransmittedCommands.Count);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ListKeyInformationAsync(
            TestContext.Current.CancellationToken));
    }

    private static byte[] SelectSecurityDomainCommand() =>
    [
        0x00, 0xA4, 0x04, 0x00, 0x08,
        0xA0, 0x00, 0x00, 0x01, 0x51, 0x00, 0x00, 0x00,
        0x00
    ];

    private static byte[] GetDataCommand(byte tag) => [0x00, 0xCA, 0x00, tag, 0x00];

    private sealed class ThrowingProtocol(Exception exception) : IProtocol
    {
        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
        {
        }

        public void Dispose() => throw exception;
    }

}