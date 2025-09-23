using Microsoft.Extensions.DependencyInjection;

namespace Yubico.YubiKit.UnitTests;

public class YubiKeyManagerDiTests
{
    [Fact]
    public void AddYubiKeyManager_RegistersYubiKeyManagerWithLogging()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddLogging();
        services.AddYubiKeyManager();
        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        YubiKeyManager? manager = provider.GetService<YubiKeyManager>();

        // Assert
        Assert.NotNull(manager);
    }
}