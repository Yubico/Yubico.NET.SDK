using System.Reflection;
using System.Runtime.CompilerServices;
using Yubico.YubiKit.Core.Abstractions;
using System.Buffers;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.YubiHsm;
using Yubico.YubiKit.WebAuthn;
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.PublicApi.UnitTests;

public sealed class FactoryShapeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HsmAuthFactories_AcceptAndRetainCallerProvider(bool deviceFactory)
    {
        var connection = new AcceptingHsmConnection();
        var options = new SessionCreationOptions { CredentialPrompt = new DecliningPrompt(), MaxCredentialPromptAttempts = 1 };
        await using HsmAuthSession session = deviceFactory
            ? await new AcceptingHsmDevice(connection).CreateHsmAuthSessionAsync(options, TestContext.Current.CancellationToken)
            : await HsmAuthSession.CreateAsync(connection, options, TestContext.Current.CancellationToken);
        IHsmAuthSession contract = session;
        await Assert.ThrowsAsync<CredentialPromptDeclinedException>(() => contract.DeleteCredentialWithPromptAsync("cred", TestContext.Current.CancellationToken));
        Assert.Equal(2, connection.Transmissions);
    }

    private sealed class AcceptingHsmDevice(ISmartCardConnection connection) : IYubiKey
    {
        public string DeviceId => "test";
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;
        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken token = default) where TConnection : class, IConnection =>
            Task.FromResult((TConnection)connection);
    }

    private sealed class AcceptingHsmConnection : ISmartCardConnection
    {
        public int Transmissions { get; private set; }
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;
        public bool SupportsExtendedApdu() => false;
        public IDisposable BeginTransaction(CancellationToken token = default) => new EmptyTransaction();
        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken token = default)
        {
            Transmissions++;
            return Task.FromResult<ReadOnlyMemory<byte>>(command.Span[1] == 9 ? (byte[])[8, 0x90, 0] : (byte[])[0x90, 0]);
        }
        public void Dispose() { }
        public ValueTask DisposeAsync() => default;
        private sealed class EmptyTransaction : IDisposable { public void Dispose() { } }
    }
    [Fact]
    public async Task NonAdoptingDeviceFactories_RejectCredentialPromptBeforeConnecting()
    {
        var options = new SessionCreationOptions { CredentialPrompt = new DecliningPrompt() };
        foreach (var (session, _, factoryName) in AppletSessionShapeTests.Sessions)
        {
            if (session.Name is "PivSession" or "HsmAuthSession")
                continue;
            MethodInfo factory = AppletSessionShapeTests.GetDeviceExtensionMethods(session)
                .Single(method => method.Name == factoryName);
            var task = (Task)(factory.Invoke(null, [null, options, CancellationToken.None])
                ?? throw new InvalidOperationException("Factory returned no task"));
            await Assert.ThrowsAsync<ArgumentException>(() => task);
        }
    }

    private sealed class DecliningPrompt : ICredentialPrompt
    {
        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IMemoryOwner<byte>?>(null);
    }

    [Fact]
    public async Task NonAdoptingDirectFactories_RejectCredentialPromptWithoutTransportExchange()
    {
        var connection = DispatchProxy.Create<ISmartCardConnection, NoTransportCalls>();
        var options = new SessionCreationOptions { CredentialPrompt = new DecliningPrompt() };
        foreach (var (session, _, factoryName) in AppletSessionShapeTests.Sessions)
        {
            if (session.Name is "PivSession" or "HsmAuthSession")
                continue;
            MethodInfo method = session.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Single(m => m.Name == "CreateAsync");
            var task = (Task)(method.Invoke(null, [connection, options, CancellationToken.None])
                ?? throw new InvalidOperationException("Factory returned no task"));
            var directError = await Assert.ThrowsAsync<ArgumentException>(() => task);
            MethodInfo extension = AppletSessionShapeTests.GetDeviceExtensionMethods(session)
                .Single(method => method.Name == factoryName);
            var deviceTask = (Task)(extension.Invoke(null, [null, options, CancellationToken.None])
                ?? throw new InvalidOperationException("Factory returned no task"));
            var deviceError = await Assert.ThrowsAsync<ArgumentException>(() => deviceTask);
            Assert.Equal(directError.Message, deviceError.Message);
            if (session.Name == "FidoSession")
                Assert.Contains("WebAuthnClientOptions.CredentialPrompt", directError.Message);
        }
    }

    public class NoTransportCalls : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected connection access: {targetMethod?.Name}");
    }
    [Fact]
    public void AppletFactories_UseUniformOptionsAndCancellationShape()
    {
        var violations = new List<string>();

        foreach (var (session, _, factoryName) in AppletSessionShapeTests.Sessions)
        {
            MethodInfo? directFactory = session.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .SingleOrDefault(method => method.Name == "CreateAsync");
            ValidateFactory(directFactory, session, receiverType: null, $"{session.Name}.CreateAsync", violations);

            MethodInfo? deviceFactory = AppletSessionShapeTests.GetDeviceExtensionMethods(session)
                .SingleOrDefault(method => method.Name == factoryName);
            ValidateFactory(deviceFactory, session, typeof(IYubiKey), $"IYubiKeyExtensions.{factoryName}", violations);
        }

        Assert.Empty(violations);
    }

    /// <summary>
    /// The WebAuthn factory carries two independent options objects, and they must stay separate.
    /// </summary>
    /// <remarks>
    /// <see cref="WebAuthnClientOptions"/> configures the client that is returned (public-suffix checker,
    /// enterprise RP IDs, credential prompt, prompt-attempt cap) and is equally meaningful on the public
    /// <c>WebAuthnClient</c> constructor, where the caller supplies their own session.
    /// <see cref="SessionCreationOptions"/> configures the session this factory creates on the
    /// caller's behalf, so it is meaningful only here. Folding the latter into the former would put a
    /// property on <see cref="WebAuthnClientOptions"/> that is silently ignored whenever a caller
    /// constructs the client directly - with nothing from the compiler or at runtime to say so.
    /// Neither is named plain <c>options</c>: <see cref="ValidateFactory"/> reserves that name for
    /// <see cref="SessionCreationOptions"/> on every other factory, so using it here for either type
    /// would make one name mean two things across the SDK.
    /// </remarks>
    [Fact]
    public void OneShotDeviceExtensions_UseUniformOptionsAndCancellationShape()
    {
        var violations = new List<string>();
        var oneShotMethods = new List<MethodInfo>();
        var nullability = new NullabilityInfoContext();

        foreach (var (session, _, factoryName) in AppletSessionShapeTests.Sessions)
        {
            // One-shot conveniences are public, non-special IYubiKey extensions in an applet's root namespace
            // other than its Create*SessionAsync factory. The non-special check excludes the C# 14 extension
            // block marker; WebAuthn is not an applet session and has its own factory test.
            oneShotMethods.AddRange(AppletSessionShapeTests.GetDeviceExtensionMethods(session)
                .Where(method => !method.IsSpecialName &&
                    method.DeclaringType?.Namespace == session.Namespace &&
                    method.Name != factoryName));
        }

        string[] expectedNames =
        [
            "CalculateAllOathCodesAsync",
            "CalculateHmacSha1Async",
            "GetConfigStateAsync",
            "GetDeviceInfoAsync",
            "GetFidoInfoAsync",
            "ListHsmAuthCredentialsAsync",
            "ListKeyInformationAsync",
            "ListOathCredentialsAsync",
            "PutConfigurationAsync",
            "SetDeviceConfigAsync"
        ];
        Assert.Equal(expectedNames, oneShotMethods.Select(static method => method.Name).Order(StringComparer.Ordinal));

        foreach (MethodInfo method in oneShotMethods)
        {
            ParameterInfo[] parameters = method.GetParameters();
            string displayName = $"{method.DeclaringType!.FullName}.{method.Name}";

            if (parameters.Length < 3)
            {
                violations.Add($"{displayName} does not have receiver, session options, and cancellation parameters");
                continue;
            }

            ParameterInfo options = parameters[^2];
            if (options.ParameterType != typeof(SessionCreationOptions) ||
                nullability.Create(options).ReadState != NullabilityState.Nullable ||
                !options.IsOptional || options.RawDefaultValue is not null)
                violations.Add($"{displayName} does not take optional SessionCreationOptions? before cancellation");

            ParameterInfo cancellationToken = parameters[^1];
            if (cancellationToken.Name != "cancellationToken" ||
                cancellationToken.ParameterType != typeof(CancellationToken) ||
                !cancellationToken.IsOptional || cancellationToken.RawDefaultValue is not null)
                violations.Add($"{displayName} does not end with CancellationToken cancellationToken = default");
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void WebAuthnDeviceFactory_UsesSessionOptionsAndCancellationShape()
    {
        MethodInfo method = typeof(Yubico.YubiKit.WebAuthn.IYubiKeyExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == "CreateWebAuthnClientAsync");
        ParameterInfo[] parameters = method.GetParameters();
        var nullability = new NullabilityInfoContext();

        Assert.Collection(
            parameters,
            receiver => Assert.Equal(typeof(IYubiKey), receiver.ParameterType),
            origin => Assert.Equal("origin", origin.Name),
            clientOptions =>
            {
                Assert.Equal("clientOptions", clientOptions.Name);
                Assert.Equal(typeof(WebAuthnClientOptions), clientOptions.ParameterType);
                Assert.Equal(NullabilityState.NotNull, nullability.Create(clientOptions).ReadState);
                Assert.False(clientOptions.IsOptional);
            },
            sessionOptions =>
            {
                Assert.Equal("sessionOptions", sessionOptions.Name);
                Assert.Equal(typeof(SessionCreationOptions), sessionOptions.ParameterType);
                Assert.True(sessionOptions.IsOptional);
                Assert.Null(sessionOptions.RawDefaultValue);
            },
            cancellationToken =>
            {
                Assert.Equal("cancellationToken", cancellationToken.Name);
                Assert.Equal(typeof(CancellationToken), cancellationToken.ParameterType);
                Assert.True(cancellationToken.IsOptional);
                Assert.Null(cancellationToken.RawDefaultValue);
            });
    }

    [Fact]
    public void WebAuthnClientOptions_PublicSuffixChecker_IsRequiredAndNonNullable()
    {
        PropertyInfo checker = typeof(WebAuthnClientOptions).GetProperty(nameof(WebAuthnClientOptions.PublicSuffixChecker))
            ?? throw new InvalidOperationException("WebAuthnClientOptions.PublicSuffixChecker is missing.");
        var nullability = new NullabilityInfoContext();

        Assert.NotNull(checker.GetCustomAttribute<RequiredMemberAttribute>());
        Assert.Equal(NullabilityState.NotNull, nullability.Create(checker).ReadState);
    }

    private static void ValidateFactory(
        MethodInfo? method,
        Type session,
        Type? receiverType,
        string displayName,
        ICollection<string> violations)
    {
        if (method is null)
        {
            violations.Add($"{displayName} is missing");
            return;
        }

        if (method.ReturnType != typeof(Task<>).MakeGenericType(session))
            violations.Add($"{displayName} does not return Task<{session.Name}>");

        ParameterInfo[] parameters = method.GetParameters();
        const int optionsIndex = 1;
        const int expectedCount = 3;

        if (parameters.Length != expectedCount)
        {
            violations.Add($"{displayName} has {parameters.Length} parameters, expected {expectedCount}");
            return;
        }

        if (receiverType is not null && parameters[0].ParameterType != receiverType)
            violations.Add($"{displayName} does not extend IYubiKey");

        ParameterInfo options = parameters[optionsIndex];
        if (options.Name != "options" || options.ParameterType != typeof(SessionCreationOptions) ||
            !options.IsOptional || options.RawDefaultValue is not null)
            violations.Add($"{displayName} does not take optional SessionCreationOptions? options = null");

        ParameterInfo cancellationToken = parameters[^1];
        if (cancellationToken.Name != "cancellationToken" || cancellationToken.ParameterType != typeof(CancellationToken) ||
            !cancellationToken.IsOptional || cancellationToken.RawDefaultValue is not null)
            violations.Add($"{displayName} does not end with CancellationToken cancellationToken = default");
    }
}