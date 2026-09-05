using TravelScheduleArrange.Api.Services;
using Xunit;

namespace TravelScheduleArrange.Api.Tests;

public class ItineraryPlannerServiceTests
{
    private readonly ItineraryPlannerService _sut = new(new MockDistanceProvider());

    [Fact]
    public async Task PlanAsync_排序結果應依貪婪最近鄰依序排列()
    {
        // 起點：台北車站 (25.0478, 121.5170)
        var start = new Coordinate(25.0478, 121.5170);

        // 候選點：由近至遠依序為 台北101 -> 士林夜市 -> 淡水老街
        var taipei101 = new ItineraryPlanItem("台北101", new Coordinate(25.0330, 121.5654));
        var shilin = new ItineraryPlanItem("士林夜市", new Coordinate(25.0879, 121.5240));
        var tamsui = new ItineraryPlanItem("淡水老街", new Coordinate(25.1700, 121.4400));

        // 刻意打亂輸入順序，驗證演算法仍會依實際距離排序
        var candidates = new List<ItineraryPlanItem> { tamsui, taipei101, shilin };

        var result = await _sut.PlanAsync(start, candidates);

        Assert.Equal(3, result.Count);
        Assert.Equal("士林夜市", result[0].Name);
        Assert.Equal("台北101", result[1].Name);
        Assert.Equal("淡水老街", result[2].Name);

        Assert.Equal(1, result[0].Order);
        Assert.Equal(2, result[1].Order);
        Assert.Equal(3, result[2].Order);
    }

    [Fact]
    public async Task PlanAsync_空候選清單應回傳空結果()
    {
        var start = new Coordinate(25.0478, 121.5170);
        var candidates = new List<ItineraryPlanItem>();

        var result = await _sut.PlanAsync(start, candidates);

        Assert.Empty(result);
    }

    [Fact]
    public async Task PlanAsync_單一候選點應直接排在第一站()
    {
        var start = new Coordinate(0, 0);
        var only = new ItineraryPlanItem("唯一景點", new Coordinate(1, 1));

        var result = await _sut.PlanAsync(start, new List<ItineraryPlanItem> { only });

        Assert.Single(result);
        Assert.Equal("唯一景點", result[0].Name);
        Assert.Equal(1, result[0].Order);
        Assert.True(result[0].DistanceFromPreviousKm > 0);
    }

    [Fact]
    public async Task PlanAsync_每站距離應為與前一站的距離而非累積距離()
    {
        // 三點共線，間距皆為緯度 0.1 度，確保每一段距離大致相等
        var start = new Coordinate(0.0, 0.0);
        var a = new ItineraryPlanItem("A", new Coordinate(0.1, 0.0));
        var b = new ItineraryPlanItem("B", new Coordinate(0.2, 0.0));

        var result = await _sut.PlanAsync(start, new List<ItineraryPlanItem> { a, b });

        Assert.Equal(2, result.Count);
        // 兩段距離應接近（誤差在合理範圍內），而非第二段是累加值
        Assert.True(Math.Abs(result[0].DistanceFromPreviousKm - result[1].DistanceFromPreviousKm) < 0.5);
    }
}

public class MockDistanceProviderTests
{
    [Fact]
    public async Task GetDistanceAsync_相同座標距離應為零()
    {
        var provider = new MockDistanceProvider();
        var point = new Coordinate(25.0478, 121.5170);

        var distance = await provider.GetDistanceAsync(point, point);

        Assert.Equal(0, distance, precision: 6);
    }

    [Fact]
    public async Task GetDistanceAsync_距離應具對稱性()
    {
        var provider = new MockDistanceProvider();
        var a = new Coordinate(25.0478, 121.5170);
        var b = new Coordinate(25.0330, 121.5654);

        var distanceAtoB = await provider.GetDistanceAsync(a, b);
        var distanceBtoA = await provider.GetDistanceAsync(b, a);

        Assert.Equal(distanceAtoB, distanceBtoA, precision: 9);
    }

    [Fact]
    public async Task GetTravelInfoAsync_相同座標距離與時間應為零()
    {
        var provider = new MockDistanceProvider();
        var point = new Coordinate(25.0478, 121.5170);

        var info = await provider.GetTravelInfoAsync(point, point);

        Assert.Equal(0, info.DistanceKm, precision: 6);
        Assert.Equal(0, info.DurationMinutes, precision: 6);
    }

    [Fact]
    public async Task GetTravelInfoAsync_時間應與GetDistanceAsync距離一致換算()
    {
        var provider = new MockDistanceProvider();
        var a = new Coordinate(25.0478, 121.5170);
        var b = new Coordinate(25.0330, 121.5654);

        var distance = await provider.GetDistanceAsync(a, b);
        var info = await provider.GetTravelInfoAsync(a, b);

        Assert.Equal(distance, info.DistanceKm, precision: 9);
        Assert.True(info.DurationMinutes > 0);
    }
}
