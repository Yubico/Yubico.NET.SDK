using Yubico.YubiKit.Core;

public interface IPcscService
{
    public Task<IReadOnlyList<IYubiKey>> GetAllAsync();
    public IReadOnlyList<IYubiKey> GetAll();
}