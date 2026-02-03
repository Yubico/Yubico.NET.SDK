using Microsoft.Extensions.DependencyInjection;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.Management.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddYubiKeyManager_RegistersManagementSessionFactory()
    {
        var services = new ServiceCollection();

        services.AddYubiKeyManager();
        var provider = services.BuildServiceProvider();

        var factory = provider.GetService<ManagementSessionFactory>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void AddYubiKeyManager_RegistersFactory_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddYubiKeyManager();
        var provider = services.BuildServiceProvider();

        var factory1 = provider.GetRequiredService<ManagementSessionFactory>();
        var factory2 = provider.GetRequiredService<ManagementSessionFactory>();
        Assert.Same(factory1, factory2);
    }

    [Fact]
    public void AddYubiKeyManager_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddYubiKeyManager();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddYubiKeyManager_CalledMultipleTimes_DoesNotThrow()
    {
        var services = new ServiceCollection();

        services.AddYubiKeyManager();
        services.AddYubiKeyManager();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ManagementSessionFactory>());
    }

    [Fact]
    public void AddYubiKeyManager_ManagementSessionFactory_HasExpectedSignature()
    {
        var method = typeof(ManagementSessionFactory).GetMethod("Invoke");
        Assert.NotNull(method);

        Assert.Equal(typeof(Task<ManagementSession>), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(4, parameters.Length);
        Assert.Equal(typeof(IConnection), parameters[0].ParameterType);
        Assert.Equal(typeof(ProtocolConfiguration?), parameters[1].ParameterType);
        Assert.Equal(typeof(ScpKeyParameters), parameters[2].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[3].ParameterType);
        Assert.False(parameters[1].IsOptional);
        Assert.True(parameters[2].IsOptional);
        Assert.True(parameters[3].IsOptional);
    }
}
