using System.Net.Http.Json;
using System.Text.Json;
using Reqnroll;
using TravelScheduleArrange.Api.Bdd.Tests.Support;
using Xunit;

namespace TravelScheduleArrange.Api.Bdd.Tests.Steps;

[Binding]
public class PlanMultiDaySteps : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly ApiTestContext _context;
    private double _startLatitude;
    private double _endLatitude;
    private List<double> _overnightLatitudes = new();
    private List<int> _dailyMinutes = new();
    private readonly List<(string Name, double Latitude, int StayMinutes)> _candidates = new();

    public PlanMultiDaySteps(ApiTestContext context)
    {
        _context = context;
    }

    [Given(@"起點緯度為 (.*)、訖點緯度為 (.*)")]
    public void GivenStartAndEndLatitude(double start, double end)
    {
        _startLatitude = start;
        _endLatitude = end;
    }

    [Given(@"住宿緯度依序為 ""(.*)""")]
    public void GivenOvernightLatitudes(string csv)
    {
        _overnightLatitudes = ParseDoubleList(csv);
    }

    [Given(@"每日可用時數依序為 ""(.*)""")]
    public void GivenDailyMinutes(string csv)
    {
        _dailyMinutes = ParseIntList(csv);
    }

    [Given(@"候選景點 ""(.*)"" 位於緯度 (.*)、停留 (.*) 分鐘")]
    public void GivenCandidate(string name, double latitude, int stayMinutes)
    {
        _candidates.Add((name, latitude, stayMinutes));
    }

    [Given(@"候選景點數量為 (\d+) 筆")]
    public void GivenCandidateCount(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _candidates.Add(($"候選{i}", i, 10));
        }
    }

    [When(@"使用者送出多日行程排程請求")]
    public async Task WhenUserSubmitsPlanRequest()
    {
        var client = _factory.CreateClient();
        var body = new
        {
            startLatitude = _startLatitude,
            startLongitude = 0,
            endLatitude = _endLatitude,
            endLongitude = 0,
            overnightStays = _overnightLatitudes.Select(lat => new { latitude = lat, longitude = 0 }),
            dailyAvailableMinutes = _dailyMinutes,
            candidates = _candidates.Select(c => new
            {
                sourceId = c.Name,
                name = c.Name,
                category = (string?)null,
                address = (string?)null,
                latitude = c.Latitude,
                longitude = 0,
                stayDurationMinutes = c.StayMinutes,
            }),
        };

        _context.Response = await client.PostAsJsonAsync("/api/itinerary/plan-multiday", body);
        _context.ResponseBody = await _context.Response.Content.ReadAsStringAsync();
    }

    [Then(@"第 (\d+) 天應包含景點 ""(.*)""")]
    public void ThenDayShouldContainAttraction(int dayNumber, string name)
    {
        Assert.Contains(name, StopNamesOnDay(dayNumber));
    }

    [Then(@"第 (\d+) 天不應包含景點 ""(.*)""")]
    public void ThenDayShouldNotContainAttraction(int dayNumber, string name)
    {
        Assert.DoesNotContain(name, StopNamesOnDay(dayNumber));
    }

    [Then(@"候補清單應包含景點 ""(.*)""")]
    public void ThenWaitlistShouldContainAttraction(string name)
    {
        using var doc = JsonDocument.Parse(_context.ResponseBody);
        var waitlistNames = doc.RootElement.GetProperty("waitlist").EnumerateArray()
            .Select(w => w.GetProperty("name").GetString());
        Assert.Contains(name, waitlistNames);
    }

    private IEnumerable<string?> StopNamesOnDay(int dayNumber)
    {
        using var doc = JsonDocument.Parse(_context.ResponseBody);
        var day = doc.RootElement.GetProperty("days").EnumerateArray()
            .First(d => d.GetProperty("dayNumber").GetInt32() == dayNumber);
        return day.GetProperty("stops").EnumerateArray().Select(s => s.GetProperty("name").GetString()).ToList();
    }

    private static List<double> ParseDoubleList(string csv) =>
        string.IsNullOrWhiteSpace(csv) ? new List<double>() : csv.Split(',').Select(double.Parse).ToList();

    private static List<int> ParseIntList(string csv) =>
        string.IsNullOrWhiteSpace(csv) ? new List<int>() : csv.Split(',').Select(int.Parse).ToList();

    public void Dispose()
    {
        _factory.Dispose();
    }
}
