using Microsoft.Extensions.DependencyInjection;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.Management.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddYubiKeyManager_RegistersManagementSessionFactoryDelegate()
    {
        var services = new ServiceCollection();

        services.AddYubiKeyManager();
        var provider = services.BuildServiceProvider();

        var factory = provider.GetService<ManagementSessionFactoryDelegate>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void AddYubiKeyManager_RegistersSmartCardManagementSessionFactoryDelegate()
    {
        var services = new ServiceCollection();

        services.AddYubiKeyManager();
        var provider = services.BuildServiceProvider();

        var factory = provider.GetService<SmartCardManagementSessionFactoryDelegate>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void AddYubiKeyManager_RegistersFactories_AsSingletons()
    {
        var services = new ServiceCollection();

        services.AddYubiKeyManager();
        var provider = services.BuildServiceProvider();

        var factory1 = provider.GetRequiredService<ManagementSessionFactoryDelegate>();
        var factory2 = provider.GetRequiredService<ManagementSessionFactoryDelegate>();
        Assert.Same(factory1, factory2);

        var scFactory1 = provider.GetRequiredService<SmartCardManagementSessionFactoryDelegate>();
        var scFactory2 = provider.GetRequiredService<SmartCardManagementSessionFactoryDelegate>();
        Assert.Same(scFactory1, scFactory2);
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

        Assert.NotNull(provider.GetService<ManagementSessionFactoryDelegate>());
        Assert.NotNull(provider.GetService<SmartCardManagementSessionFactoryDelegate>());
    }

    [Fact]
    public void AddYubiKeyManager_ManagementSessionFactoryDelegate_HasExpectedSignature()
    {
        var method = typeof(ManagementSessionFactoryDelegate).GetMethod("Invoke");
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

    [Fact]
    public void AddYubiKeyManager_SmartCardManagementSessionFactoryDelegate_HasExpectedSignature()
    {
        var method = typeof(SmartCardManagementSessionFactoryDelegate).GetMethod("Invoke");
        Assert.NotNull(method);

        Assert.Equal(typeof(Task<ManagementSession>), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(4, parameters.Length);
        Assert.Equal(typeof(ISmartCardConnection), parameters[0].ParameterType);
        Assert.Equal(typeof(ProtocolConfiguration?), parameters[1].ParameterType);
        Assert.Equal(typeof(ScpKeyParameters), parameters[2].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[3].ParameterType);
        Assert.False(parameters[1].IsOptional);
        Assert.True(parameters[2].IsOptional);
        Assert.True(parameters[3].IsOptional);
    }
}
