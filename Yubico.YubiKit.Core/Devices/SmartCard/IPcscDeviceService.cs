using Yubico.YubiKit.Core.Devices.SmartCard;

public interface IPcscDeviceService
{
    public Task<IReadOnlyList<PcscDevice>> GetAllAsync();
    public IReadOnlyList<PcscDevice> GetAll();
}