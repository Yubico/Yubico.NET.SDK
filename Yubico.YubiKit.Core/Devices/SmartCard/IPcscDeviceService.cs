using Yubico.YubiKit.Core.Devices.SmartCard;

public interface IPcscDeviceService
{
    public Task<IReadOnlyList<IPcscDevice>> GetAllAsync();
    public IReadOnlyList<IPcscDevice> GetAll();
}