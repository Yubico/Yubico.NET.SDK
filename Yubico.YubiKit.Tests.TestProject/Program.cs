using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using TestProject;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

var builder = WebApplication.CreateSlimBuilder(args);
AddJsonOptions(builder);

// Register YubiKey services with DI
builder.Services.AddYubiKeyManager(options =>
{
    options.EnableAutoDiscovery = true;
});

// Register controllers for the DiTestController
builder.Services.AddControllers();

var app = builder.Build();

// Minimal API endpoint - demonstrates the simplest integration pattern
app.MapGet("/di-demo/minimal", async (
    [FromServices] IYubiKeyManager yubiKeyManager,
    CancellationToken cancellationToken
) =>
{
    // Find all available YubiKeys
    var yubiKeys = await yubiKeyManager.FindAllAsync(cancellationToken: cancellationToken);
    if (yubiKeys.Count == 0)
        return Results.Problem("No YubiKey detected. Please connect a YubiKey and try again.", statusCode: 503);

    var deviceInfo = await yubiKeys[0].GetDeviceInfoAsync(cancellationToken);
    var yubiInfo = new YubiInfo(
        deviceInfo.SerialNumber?.ToString() ?? "Unknown", 
        deviceInfo.FirmwareVersion.ToString()
    );
    
    return Results.Ok(new { 
        Message = "Session type is ManagementSessionSimple",
        YubiKey = yubiInfo 
    });
});

// Controller-based endpoint is registered automatically via ASP.NET Core conventions
app.MapControllers();

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
    /// <summary>
    ///     Data transfer object for YubiKey information.
    /// </summary>
    public record YubiInfo(string serialNumber, string firmwareVersion);

    /// <summary>
    ///     JSON serialization context for AOT compilation support.
    /// </summary>
    [JsonSerializable(typeof(YubiInfo))]
    [JsonSerializable(typeof(ProblemDetails))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }

    /// <summary>
    ///     Entry point class required for WebApplicationFactory in tests.
    /// </summary>
    public class Program
    {
    }
}