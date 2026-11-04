using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Ctap2;
using System.Linq;

namespace AuditCt001.Prepared.guide_info;

public static class Example
{
public static async Task ExampleAsync(ICtap2Session session)
{
// Get authenticator capabilities
var info = await session.GetInfoAsync();

// Check versions
bool ctap2_1 = info.Versions.Contains("FIDO_2_1");

// Check supported extensions
bool hasHmacSecret = info.Extensions.Contains("hmac-secret");

// Check options
bool hasRk = info.Options.TryGetValue("rk", out var rk) && rk;
}
}
