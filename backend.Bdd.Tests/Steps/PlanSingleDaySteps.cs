using System.Net.Http.Json;
using System.Text.Json;
using Reqnroll;
using TravelScheduleArrange.Api.Bdd.Tests.Support;
using Xunit;

namespace TravelScheduleArrange.Api.Bdd.Tests.Steps;

[Binding]
public class PlanSingleDaySteps : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly ApiTestContext _context;
    private double _startLatitude;
    private readonly List<(string Name, double Latitude)> _candidates = new();

    public PlanSingleDaySteps(ApiTestContext context)
    {
        _context = context;
    }

    [Given(@"單日起點緯度為 (.*)")]
    public void GivenStartLatitude(double latitude)
    {
        _startLatitude = latitude;
    }

    [Given(@"單日候選景點 ""(.*)"" 位於緯度 (.*)")]
    public void GivenCandidate(string name, double latitude)
    {
        _candidates.Add((name, latitude));
    }

    [Given(@"單日候選景點數量為 (\d+) 筆")]
    public void GivenCandidateCount(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _candidates.Add(($"候選{i}", i));
        }
    }

    [When(@"使用者送出單日行程排序請求")]
    public async Task WhenUserSubmitsPlanRequest()
    {
        var client = _factory.CreateClient();
        var body = new
        {
            startLatitude = _startLatitude,
            startLongitude = 0,
            attractions = _candidates.Select(c => new
            {
                sourceId = c.Name,
                name = c.Name,
                category = (string?)null,
                address = (string?)null,
                latitude = c.Latitude,
                longitude = 0,
            }),
        };

        _context.Response = await client.PostAsJsonAsync("/api/itinerary/plan", body);
        _context.ResponseBody = await _context.Response.Content.ReadAsStringAsync();
    }

    [Then(@"排序結果第 (\d+) 筆應為 ""(.*)""")]
    public void ThenResultAtPositionShouldBe(int order, string name)
    {
        using var doc = JsonDocument.Parse(_context.ResponseBody);
        var item = doc.RootElement.EnumerateArray().First(e => e.GetProperty("order").GetInt32() == order);
        Assert.Equal(name, item.GetProperty("name").GetString());
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
