using Microsoft.AspNetCore.Mvc;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace TestProject.Controllers;

/// <summary>
///     Example controller demonstrating YubiKey integration with ASP.NET Core DI.
///     Shows proper resource management, error handling, and cancellation support.
/// </summary>
[ApiController]
[Route("di/controller")]
public class DiTestController(
    IYubiKeyManager yubiKeyManager)
    : ControllerBase
{
    /// <summary>
    ///     Gets device information from the first available YubiKey.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>YubiKey serial number and firmware version.</returns>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var yubiKeys = await yubiKeyManager.FindAllAsync(cancellationToken: cancellationToken);
        if (yubiKeys.Count == 0)
            return Problem("No YubiKey detected. Please connect a YubiKey and try again.", statusCode: 503);

        var yubiKey = yubiKeys[0];
        var deviceInfo = await yubiKey.GetDeviceInfoAsync(cancellationToken);

        var yubiInfo = new YubiInfo(
            deviceInfo.SerialNumber?.ToString() ?? "Unknown", 
            deviceInfo.FirmwareVersion.ToString()
        );
        
        return Ok(new { 
            Message = $"Session type is ManagementSessionSimple",
            YubiKey = yubiInfo 
        });
    }
}