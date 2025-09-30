using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class DiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Use the real application and DI setup, no service overrides or mocks
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/di/minimal")]
    [InlineData("/di/serviceprovider")]
    [InlineData("/di/controller")]
    public async Task DiEndpoints_ReturnSessionType(string url)
    {
        // NOTE: This test requires a real YubiKey to be present and accessible.
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Session type is", content);
    }
}