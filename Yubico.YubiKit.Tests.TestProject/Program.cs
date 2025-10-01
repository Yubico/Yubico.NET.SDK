using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Yubico.YubiKit;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Management;

var builder = WebApplication.CreateSlimBuilder(args);
AddJsonOptions(builder);

builder.Services.AddYubiKeyManager(options =>
{
    options.EnableAutoDiscovery = true;
});

var app = builder.Build();
app.MapGet("/di-demo", async (
    [FromServices] IYubiKeyManager yubiKeyManager,
    [FromServices] IManagementSessionFactory<ISmartCardConnection> sessionFactory
) =>
{
    var yubiKeys = await yubiKeyManager.GetYubiKeysAsync();
    var yubiKey = yubiKeys.FirstOrDefault();
    if (yubiKey == null)
        return Results.Problem("No YubiKey found.");

    using var smartCardConnection = await yubiKey.ConnectAsync<ISmartCardConnection>();
    using var session = await sessionFactory.CreateAsync(smartCardConnection);

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

public record YubiInfo(string serialNumber, string firmwareVersion);

[JsonSerializable(typeof(YubiInfo))]
[JsonSerializable(typeof(ProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}

public partial class Program
{
}