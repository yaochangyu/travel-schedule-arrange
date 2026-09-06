using System.Text.Json;
using Reqnroll;
using TravelScheduleArrange.Api.Bdd.Tests;
using TravelScheduleArrange.Api.Models;
using Xunit;

namespace TravelScheduleArrange.Api.Bdd.Tests.Steps;

[Binding]
public class NearbyMultiAnchorSteps : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private HttpResponseMessage? _response;
    private string _responseBody = string.Empty;

    [Given(@"TDX 對緯度 (.*) 的錨點回傳景點 ""(.*)""")]
    public void GivenTdxReturnsAttraction(double latitude, string attractionName)
    {
        _factory.TourismServiceStub.SetScenicSpots(latitude, new List<Attraction>
        {
            new()
            {
                Name = attractionName,
                Category = AttractionCategory.ScenicSpot,
                SourceId = attractionName,
                Latitude = latitude,
                Longitude = 0,
            },
        });
    }

    [Given(@"TDX 對緯度 (.*) 的錨點查詢會失敗")]
    public void GivenTdxFailsForAnchor(double latitude)
    {
        _factory.TourismServiceStub.SetFailing(latitude);
    }

    [When(@"使用者以錨點 ""(.*)"" 查詢附近景點")]
    public async Task WhenUserQueriesNearbyAttractions(string anchors)
    {
        await SendRequestAsync(anchors, radius: null);
    }

    [When(@"使用者以錨點 ""(.*)"" 與半徑 (.*) 查詢附近景點")]
    public async Task WhenUserQueriesNearbyAttractionsWithRadius(string anchors, int radius)
    {
        await SendRequestAsync(anchors, radius);
    }

    private async Task SendRequestAsync(string anchors, int? radius)
    {
        var client = _factory.CreateClient();
        var query = $"anchors={Uri.EscapeDataString(anchors)}";
        if (radius.HasValue)
        {
            query += $"&radius={radius.Value}";
        }

        _response = await client.GetAsync($"/api/attractions/nearby-multianchor?{query}");
        _responseBody = await _response.Content.ReadAsStringAsync();
    }

    [Then(@"回應狀態碼應為 (\d+)")]
    public void ThenResponseStatusCodeShouldBe(int statusCode)
    {
        Assert.Equal(statusCode, (int)_response!.StatusCode);
    }

    [Then(@"回應應包含景點 ""(.*)""")]
    public void ThenResponseShouldContainAttraction(string name)
    {
        Assert.Contains(name, _responseBody);
    }

    [Then(@"回應景點數量應為 (\d+)")]
    public void ThenResponseAttractionCountShouldBe(int count)
    {
        var items = JsonSerializer.Deserialize<List<JsonElement>>(_responseBody);
        Assert.Equal(count, items!.Count);
    }

    public void Dispose()
    {
        _response?.Dispose();
        _factory.Dispose();
    }
}
