using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using TestProject;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

var builder = WebApplication.CreateSlimBuilder(args);
AddJsonOptions(builder);

builder.Services.AddYubiKeyManager(options =>
{
    options.EnableAutoDiscovery = true;
});

var app = builder.Build();
app.MapGet("/di-demo/minimal", async (
    [FromServices] IYubiKeyManager yubiKeyManager,
    [FromServices] DependencyInjection.SmartCardManagementSessionFactoryDelegate sessionFactory
) =>
{
    var yubiKeys = await yubiKeyManager.FindAllAsync();
    var yubiKey = yubiKeys[0];

    using var smartCardConnection = await yubiKey.ConnectAsync<ISmartCardConnection>();
    using var session = await sessionFactory(smartCardConnection);

    var deviceInfo = await session.GetDeviceInfoAsync();
    var yubiInfo = new YubiInfo(deviceInfo.SerialNumber.ToString("D8"), deviceInfo.FirmwareVersion.ToString());
    return Results.Text($"YubiKey on Server:: {yubiInfo}");
});

app.Run();

return;

void AddJsonOptions(WebApplicationBuilder webApplicationBuilder)
{
    webApplicationBuilder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    });
}

namespace TestProject
{
    public record YubiInfo(string serialNumber, string firmwareVersion);

    [JsonSerializable(typeof(YubiInfo))]
    [JsonSerializable(typeof(ProblemDetails))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }

    public class Program
    {
    }
}