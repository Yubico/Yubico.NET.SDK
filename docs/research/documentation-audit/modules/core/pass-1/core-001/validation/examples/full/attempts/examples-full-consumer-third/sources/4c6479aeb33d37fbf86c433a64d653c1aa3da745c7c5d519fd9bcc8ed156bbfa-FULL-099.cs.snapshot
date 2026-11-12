using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Oath;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace Audit20261111.DocExamples.Full;
internal static class Body99
{
internal static void CompileOnly()
{
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Yubico.YubiKit", Serilog.Events.LogEventLevel.Verbose)
    .WriteTo.Console()
    .WriteTo.File("yubikit.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

using var host = Host.CreateDefaultBuilder()
    .UseSerilog()
    .Build();

YubiKitLogging.Configure(host.Services.GetRequiredService<ILoggerFactory>());
}

}
