using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Devices.SmartCard;

public interface ISmartCardConnectionFactory
{
    Task<ISmartCardConnection> CreateAsync(ISmartCardDevice smartCardDevice);
}

public class SmartCardConnectionFactory : ISmartCardConnectionFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public SmartCardConnectionFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    #region ISmartCardConnectionFactory Members

    public async Task<ISmartCardConnection> CreateAsync(ISmartCardDevice smartCardDevice) =>
        await PcscSmartCardConnection.CreateAsync(
            _loggerFactory.CreateLogger<PcscSmartCardConnection>(),
            smartCardDevice);

    #endregion
}