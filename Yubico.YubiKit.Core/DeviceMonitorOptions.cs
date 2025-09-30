
namespace Yubico.YubiKit.Core;
public class DeviceMonitorOptions
{
    public DeviceMonitorOptions()
    {

    }

    #region Transports enum

    [Flags]
    public enum Transports
    {
        None = 0,
        Usb = 1,
        Nfc = 2,
        All = Usb | Nfc
    }

    #endregion

    public bool EnableAutoDiscovery { get; set; } = true;
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromMilliseconds(1000);
    public Transports EnabledTransports { get; set; } = Transports.All;
}