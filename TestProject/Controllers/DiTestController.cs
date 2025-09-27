using Microsoft.AspNetCore.Mvc;
using Yubico.YubiKit;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Protocols;

namespace TestProject.Controllers;

[ApiController]
[Route("di/controller")]
public class DiTestController : ControllerBase
{
    private readonly ILogger<ManagementSession<ISmartCardConnection>> _logger;
    private readonly IProtocolFactory<ISmartCardConnection, IProtocol> _protocolFactory;
    private readonly IYubiKeyManager _yubiKeyManager;

    public DiTestController(IYubiKeyManager yubiKeyManager, ILogger<ManagementSession<ISmartCardConnection>> logger,
        IProtocolFactory<ISmartCardConnection, IProtocol> protocolFactory)
    {
        _yubiKeyManager = yubiKeyManager;
        _logger = logger;
        _protocolFactory = protocolFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var yubiKeys = await _yubiKeyManager.GetYubiKeys();
        var yubiKey = yubiKeys.FirstOrDefault();
        if (yubiKey == null)
            return Problem("No YubiKey found.");
        var smartCardConnection = await yubiKey.ConnectAsync<ISmartCardConnection>();
        var session = new ManagementSession<ISmartCardConnection>(_logger, smartCardConnection, _protocolFactory);
        return Ok($"Controller DI: Session type is {session.GetType().Name}");
    }
}