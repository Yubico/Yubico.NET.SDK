using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Devices.SmartCard;

namespace Yubico.YubiKit.Core.Connections;

public interface ISmartCardConnectionFactory
{
    Task<ISmartCardConnection> CreateAsync(IPcscDevice smartCardDevice, CancellationToken cancellationToken = default);
}

public class SmartCardConnectionFactory : ISmartCardConnectionFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public SmartCardConnectionFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    #region ISmartCardConnectionFactory Members

    public async Task<ISmartCardConnection> CreateAsync(IPcscDevice smartCardDevice, CancellationToken cancellationToken = default)
    {
        var connection = new PcscSmartCardConnection(
            _loggerFactory.CreateLogger<PcscSmartCardConnection>(),
            smartCardDevice);

        await connection.InitializeAsync(cancellationToken).ConfigureAwait(false);

        return connection;
    }

    #endregion
}