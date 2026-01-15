// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.DependencyInjection;

namespace Yubico.YubiKit.SecurityDomain.UnitTests;

/// <summary>
///     Unit tests for SecurityDomain dependency injection registration.
///     These tests verify DI registration mechanics without invoking the factory
///     (which would require actual connections and is better suited for integration tests).
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void AddYubiKeySecurityDomain_RegistersFactory_AsSuccess()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddYubiKeySecurityDomain();
        var provider = services.BuildServiceProvider();

        // Assert
        var factory = provider.GetService<SecurityDomainSessionFactory>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void AddYubiKeySecurityDomain_RegistersFactory_AsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddYubiKeySecurityDomain();
        var provider = services.BuildServiceProvider();

        // Assert - resolve twice and verify same instance
        var factory1 = provider.GetRequiredService<SecurityDomainSessionFactory>();
        var factory2 = provider.GetRequiredService<SecurityDomainSessionFactory>();
        
        Assert.Same(factory1, factory2);
    }

    [Fact]
    public void AddYubiKeySecurityDomain_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddYubiKeySecurityDomain();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddYubiKeySecurityDomain_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - should not throw
        services.AddYubiKeySecurityDomain();
        services.AddYubiKeySecurityDomain();
        services.AddYubiKeySecurityDomain();
        
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<SecurityDomainSessionFactory>();
        
        Assert.NotNull(factory);
    }

    [Fact]
    public void AddYubiKeySecurityDomain_FactoryDelegate_HasCorrectSignature()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddYubiKeySecurityDomain();
        var provider = services.BuildServiceProvider();

        // Act
        var factory = provider.GetRequiredService<SecurityDomainSessionFactory>();

        // Assert - verify delegate signature by checking method info
        var method = factory.Method;
        Assert.Equal(typeof(Task<SecurityDomainSession>), method.ReturnType);
        
        var parameters = method.GetParameters();
        Assert.Equal(4, parameters.Length);
        Assert.Equal("conn", parameters[0].Name);
        Assert.Equal("cfg", parameters[1].Name);
        Assert.Equal("scp", parameters[2].Name);
        Assert.Equal("ct", parameters[3].Name);
    }

    [Fact]
    public void AddYubiKeySecurityDomain_WithExistingServices_IntegratesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestService, TestService>();

        // Act
        services.AddYubiKeySecurityDomain();
        var provider = services.BuildServiceProvider();

        // Assert - both services should be available
        Assert.NotNull(provider.GetService<ITestService>());
        Assert.NotNull(provider.GetService<SecurityDomainSessionFactory>());
    }

    // Helper interface/class for testing service integration
    private interface ITestService { }
    private class TestService : ITestService { }
}
