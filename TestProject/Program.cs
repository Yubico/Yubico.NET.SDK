using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Yubico.YubiKit;
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.Protocols;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddYubiKeyManager(options =>
{
    options.EnableAutoDiscovery = true;
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});


var app = builder.Build();

// Minimal API endpoint: DI via [FromServices] for IYubiKeyManager
app.MapGet("/di/minimal", async ([FromServices] IYubiKeyManager yubiKeyManager,
    [FromServices] ILogger<ManagementSession<ISmartCardConnection>> logger,
    [FromServices] IProtocolFactory<ISmartCardConnection, IProtocol> protocolFactory) =>
{
    var yubiKeys = await yubiKeyManager.GetYubiKeys();
    var yubiKey = yubiKeys.FirstOrDefault();
    if (yubiKey == null)
        return Results.Problem("No YubiKey found.");
    var smartCardConnection = await yubiKey.ConnectAsync<ISmartCardConnection>();
    var session = new ManagementSession<ISmartCardConnection>(logger, smartCardConnection, protocolFactory);
    return Results.Ok($"Minimal API DI: Session type is {session.GetType().Name}");
});

// Minimal API endpoint: DI via IServiceProvider for IYubiKeyManager
app.MapGet("/di/serviceprovider", async (IServiceProvider sp) =>
{
    var yubiKeyManager = sp.GetRequiredService<IYubiKeyManager>();
    var logger = sp.GetRequiredService<ILogger<ManagementSession<ISmartCardConnection>>>();
    var protocolFactory = sp.GetRequiredService<IProtocolFactory<ISmartCardConnection, IProtocol>>();
    var yubiKeys = await yubiKeyManager.GetYubiKeys();
    var yubiKey = yubiKeys.FirstOrDefault();
    if (yubiKey == null)
        return Results.Problem("No YubiKey found.");
    var smartCardConnection = await yubiKey.ConnectAsync<ISmartCardConnection>();
    var session = new ManagementSession<ISmartCardConnection>(logger, smartCardConnection, protocolFactory);
    return Results.Ok($"ServiceProvider DI: Session type is {session.GetType().Name}");
});

var sampleTodos = new Todo[]
{
    new(1, "Walk the dog"), new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))), new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
};

var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", () => sampleTodos);
todosApi.MapGet("/{id}", (int id) =>
    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

// Add controller endpoints
// app.MapControllers();

app.Run();

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}

public partial class Program
{
}