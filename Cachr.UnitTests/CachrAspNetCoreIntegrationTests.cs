using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Cachr.AspNetCore;
using Cachr.Core;
using Xunit;
using Xunit.Abstractions;

namespace Cachr.UnitTests;

public sealed class CachrAspNetCoreIntegrationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CachrAspNetCoreIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }
    [Fact]
    public async Task CachrAspNetCoreGreetingMiddlewareRespondsToRequests()
    {
        var client = TestHostBuilder.GetTestApplication().CreateClient();
        const HttpStatusCode ExpectedResponse = HttpStatusCode.OK;
        var response = await client.GetAsync("/$cachr/$greet").ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        Assert.Equal(ExpectedResponse, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        _testOutputHelper.WriteLine($"Response: \n{responseBody}");
    }

    [Fact]
    public async Task CachrAspNetCoreGreetingMiddlewareReturnsNodeIdentityData()
    {
        var client = TestHostBuilder.GetTestApplication().CreateClient();
        const HttpStatusCode ExpectedResponse = HttpStatusCode.OK;
        var response = await client.GetAsync("/$cachr/$greet").ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        Assert.Equal(ExpectedResponse, response.StatusCode);
        var responseBody = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var deserializedBody = await JsonSerializer.DeserializeAsync<GreetingResponse>(responseBody).ConfigureAwait(false);
        Assert.NotNull(deserializedBody);
        Assert.Equal(NodeIdentity.Name, deserializedBody.Name);
        Assert.Equal(NodeIdentity.Id, deserializedBody.Id);
        Assert.Equal(IPAddress.Loopback.ToString(), deserializedBody.DetectedAddress);
    }

    [Fact]
    public async Task CachrAspNetCoreGreetingMiddlewareIgnoresUnknownPaths()
    {
        var client = TestHostBuilder.GetTestApplication().CreateClient();
        const HttpStatusCode ExpectedResponse = HttpStatusCode.NotFound;
        var response = await client.GetAsync("/$cachr/$greet/$addedPath").ConfigureAwait(false);
        Assert.Equal(ExpectedResponse, response.StatusCode);
    }
}
