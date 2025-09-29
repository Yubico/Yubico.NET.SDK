using Yubico.YubiKit.Core.Devices.SmartCard;

public interface IPcscService
{
    public Task<IReadOnlyList<PcscDevice>> GetAllAsync();
    public IReadOnlyList<PcscDevice> GetAll();
}