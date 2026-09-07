using System.Reflection;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.WebAuthn;
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.PublicApi.UnitTests;

public sealed class FactoryShapeTests
{
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
    /// <see cref="WebAuthnClientOptions"/> configures the client that is returned (enterprise RP IDs,
    /// credential prompt, prompt-attempt cap) and is equally meaningful on the public
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
    public void WebAuthnDeviceFactory_UsesSessionOptionsAndCancellationShape()
    {
        MethodInfo method = typeof(IYubiKeyExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == "CreateWebAuthnClientAsync");
        ParameterInfo[] parameters = method.GetParameters();

        Assert.Collection(
            parameters,
            receiver => Assert.Equal(typeof(IYubiKey), receiver.ParameterType),
            origin => Assert.Equal("origin", origin.Name),
            suffixChecker => Assert.Equal("isPublicSuffix", suffixChecker.Name),
            clientOptions =>
            {
                Assert.Equal("clientOptions", clientOptions.Name);
                Assert.Equal(typeof(WebAuthnClientOptions), clientOptions.ParameterType);
                Assert.True(clientOptions.IsOptional);
                Assert.Null(clientOptions.RawDefaultValue);
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