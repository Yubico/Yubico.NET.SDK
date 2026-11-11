using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace AuditIntegration20261111.Example08;
public static class Example
{
public static async Task RunAsync(string[] args)
{

await using var monitor = await YubiKeyManager.CreateMonitorAsync();
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(monitor); // Externally created instance: container does not own disposal.
builder.Services.AddHostedService<MonitorLifetime>();
// Register services that receive YubiKeyMonitor and call monitor.WatchAsync(theirStoppingToken).
using var host = builder.Build();
await host.RunAsync();

}
}

sealed class MonitorLifetime(YubiKeyMonitor monitor) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => monitor.DisposeAsync().AsTask();
}
