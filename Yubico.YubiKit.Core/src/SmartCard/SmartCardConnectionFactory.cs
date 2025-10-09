using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Yubico.YubiKit.Core.SmartCard;

public interface ISmartCardConnectionFactory
{
    Task<ISmartCardConnection> CreateAsync(IPcscDevice smartCardDevice, CancellationToken cancellationToken = default);
}

public class SmartCardConnectionFactory(ILoggerFactory loggerFactory) : ISmartCardConnectionFactory
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

    #region ISmartCardConnectionFactory Members

    public async Task<ISmartCardConnection> CreateAsync(IPcscDevice smartCardDevice,
        CancellationToken cancellationToken = default)
    {
        var connection = new SmartCardConnection(smartCardDevice, _loggerFactory.CreateLogger<SmartCardConnection>());
        await connection.InitializeAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    #endregion

    public static SmartCardConnectionFactory CreateDefault(ILoggerFactory? loggerFactory = null) =>
        new(loggerFactory ?? NullLoggerFactory.Instance);
}