using Microsoft.AspNetCore.Mvc;
using Yubico.YubiKit.Core.Core.Connections;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace TestProject.Controllers;

[ApiController]
[Route("di/controller")]
public class DiTestController(
    IYubiKeyManager yubiKeyManager,
    IManagementSessionFactory<ISmartCardConnection> sessionFactory)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var yubiKeys = await yubiKeyManager.FindAllAsync();
        var yubiKey = yubiKeys[0];

        var smartCardConnection = await yubiKey.ConnectAsync<ISmartCardConnection>();
        var session = await sessionFactory.CreateAsync(smartCardConnection);
        var deviceInfo = await session.GetDeviceInfoAsync();

        var yubiInfo = new YubiInfo(deviceInfo.SerialNumber.ToString("D8"), deviceInfo.FirmwareVersion.ToString());
        return Ok($"YubiKey on Server:: {yubiInfo}");
    }
}