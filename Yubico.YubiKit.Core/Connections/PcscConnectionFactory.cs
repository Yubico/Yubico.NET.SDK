using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Devices.SmartCard;

namespace Yubico.YubiKit.Core.Connections;

public interface ISmartCardConnectionFactory
{
    Task<ISmartCardConnection> CreateAsync(IPcscDevice smartCardDevice, CancellationToken cancellationToken = default);
}

public class PcscConnectionFactory : ISmartCardConnectionFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public PcscConnectionFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    #region ISmartCardConnectionFactory Members

    public async Task<ISmartCardConnection> CreateAsync(IPcscDevice smartCardDevice,
        CancellationToken cancellationToken = default)
    {
        var connection = new PcscConnection(
            _loggerFactory.CreateLogger<PcscConnection>(),
            smartCardDevice);

        await connection.InitializeAsync(cancellationToken).ConfigureAwait(false);

        return connection;
    }

    #endregion
}