using Microsoft.AspNetCore.Mvc;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management;

namespace TestProject.Controllers;

/// <summary>
///     Example controller demonstrating YubiKey integration with ASP.NET Core.
///     Uses the one-shot device-info extension, which disposes its temporary session/connection.
///     Handles no-device explicitly and forwards the request token; connection/query failures propagate
///     to the host. The response's legacy session-name string is not a current SDK type.
/// </summary>
[ApiController]
[Route("di/controller")]
public class DiTestController
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
        var yubiKey = await YubiKeyManager.FindFirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (yubiKey is null)
            return Problem("No YubiKey detected. Please connect a YubiKey and try again.", statusCode: 503);

        var deviceInfo = await yubiKey.GetDeviceInfoAsync(cancellationToken: cancellationToken);

        var yubiInfo = new YubiInfo(
            deviceInfo.SerialNumber?.ToString() ?? "Unknown",
            deviceInfo.FirmwareVersion.ToString()
        );

        return Ok(new
        {
            Message = $"Session type is ManagementSessionSimple",
            YubiKey = yubiInfo
        });
    }
}