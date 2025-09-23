using Microsoft.Extensions.DependencyInjection;

namespace Yubico.YubiKit.UnitTests;

public class YubiKeyManagerDiTests
{
    [Fact]
    public void AddYubiKeyManager_RegistersYubiKeyManagerWithLogging()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddYubiKeyManager();
        var provider = services.BuildServiceProvider();

        // Act
        var manager = provider.GetService<YubiKeyManager>();

        // Assert
        Assert.NotNull(manager);
    }
}