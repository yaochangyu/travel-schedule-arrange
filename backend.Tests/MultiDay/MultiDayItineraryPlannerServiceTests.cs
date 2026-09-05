using TravelScheduleArrange.Api.Services;
using TravelScheduleArrange.Api.Services.MultiDay;
using Xunit;

namespace TravelScheduleArrange.Api.Tests.MultiDay;

public class MultiDayItineraryPlannerServiceTests
{
    private readonly MultiDayItineraryPlannerService _sut = new(new StubDistanceProvider());

    [Fact]
    public async Task PlanAsync_候選應依每日時間預算跨天分配()
    {
        // 座標的緯度值直接代表 StubDistanceProvider 計算出的公里數（見類別註解），方便精準驗證。
        var start = new Coordinate(0, 0);
        var overnightStay = new Coordinate(10, 0);
        var end = new Coordinate(20, 0);

        var a = new MultiDayPlanItem("A", new Coordinate(2, 0), 10);
        var b = new MultiDayPlanItem("B", new Coordinate(5, 0), 10);
        var c = new MultiDayPlanItem("C", new Coordinate(12, 0), 10);
        var d = new MultiDayPlanItem("D", new Coordinate(15, 0), 10);

        var request = new MultiDayPlanRequest(
            start, end,
            OvernightStays: new List<Coordinate> { overnightStay },
            DailyAvailableMinutes: new List<int> { 50, 1000 },
            Candidates: new List<MultiDayPlanItem> { a, b, c, d });

        var result = await _sut.PlanAsync(request);

        Assert.Equal(2, result.Days.Count);

        // Day1（budget=50）：依序排入 A(14min累計)、B(30min累計)；C 因累計會變 54>50 被擋下
        Assert.Equal(1, result.Days[0].DayNumber);
        Assert.Equal(new[] { "A", "B" }, result.Days[0].Stops.Select(s => s.Name));
        Assert.Equal(1, result.Days[0].Stops[0].Order);
        Assert.Equal(2, result.Days[0].Stops[1].Order);
        Assert.Equal(5, result.Days[0].FinalLegDistanceKm, precision: 6); // 最後站(5,0) -> 住宿(10,0)

        // Day2（budget=1000，起點為當晚住宿）：依序排入 C、D
        Assert.Equal(2, result.Days[1].DayNumber);
        Assert.Equal(new[] { "C", "D" }, result.Days[1].Stops.Select(s => s.Name));
        Assert.Equal(5, result.Days[1].FinalLegDistanceKm, precision: 6); // 最後站(15,0) -> 訖點(20,0)

        Assert.Empty(result.Waitlist);
    }

    [Fact]
    public async Task PlanAsync_累計時間剛好等於預算應該排入()
    {
        var start = new Coordinate(0, 0);
        var candidate = new MultiDayPlanItem("A", new Coordinate(2, 0), 10); // 交通4分鐘 + 停留10分鐘 = 14分鐘

        var request = new MultiDayPlanRequest(
            start, start,
            OvernightStays: new List<Coordinate>(),
            DailyAvailableMinutes: new List<int> { 14 },
            Candidates: new List<MultiDayPlanItem> { candidate });

        var result = await _sut.PlanAsync(request);

        Assert.Single(result.Days[0].Stops);
        Assert.Equal("A", result.Days[0].Stops[0].Name);
        Assert.Empty(result.Waitlist);
    }

    [Fact]
    public async Task PlanAsync_累計時間超過預算應排除並留在候補清單()
    {
        var start = new Coordinate(0, 0);
        var candidate = new MultiDayPlanItem("A", new Coordinate(2, 0), 10); // 需要 14 分鐘

        var request = new MultiDayPlanRequest(
            start, start,
            OvernightStays: new List<Coordinate>(),
            DailyAvailableMinutes: new List<int> { 13 },
            Candidates: new List<MultiDayPlanItem> { candidate });

        var result = await _sut.PlanAsync(request);

        Assert.Empty(result.Days[0].Stops);
        Assert.Single(result.Waitlist);
        Assert.Equal("A", result.Waitlist[0].Name);
    }

    [Fact]
    public async Task PlanAsync_N等於1時應以起點出發並以訖點作為當天終點()
    {
        var start = new Coordinate(0, 0);
        var end = new Coordinate(20, 0);
        var candidate = new MultiDayPlanItem("A", new Coordinate(5, 0), 10);

        var request = new MultiDayPlanRequest(
            start, end,
            OvernightStays: new List<Coordinate>(), // N=1，無過夜節點
            DailyAvailableMinutes: new List<int> { 1000 },
            Candidates: new List<MultiDayPlanItem> { candidate });

        var result = await _sut.PlanAsync(request);

        Assert.Single(result.Days);
        Assert.Single(result.Days[0].Stops);
        Assert.Equal(15, result.Days[0].FinalLegDistanceKm, precision: 6); // 最後站(5,0) -> 訖點(20,0)
    }

    [Fact]
    public async Task PlanAsync_空候選池時每天皆應為空清單但FinalLeg仍應計算()
    {
        var start = new Coordinate(0, 0);
        var overnightStay = new Coordinate(10, 0);
        var end = new Coordinate(20, 0);

        var request = new MultiDayPlanRequest(
            start, end,
            OvernightStays: new List<Coordinate> { overnightStay },
            DailyAvailableMinutes: new List<int> { 100, 100 },
            Candidates: new List<MultiDayPlanItem>());

        var result = await _sut.PlanAsync(request);

        Assert.Equal(2, result.Days.Count);
        Assert.Empty(result.Days[0].Stops);
        Assert.Empty(result.Days[1].Stops);
        Assert.Equal(10, result.Days[0].FinalLegDistanceKm, precision: 6); // 起點 -> 住宿
        Assert.Equal(10, result.Days[1].FinalLegDistanceKm, precision: 6); // 住宿 -> 訖點
        Assert.Empty(result.Waitlist);
    }

    [Fact]
    public async Task PlanAsync_單一候選點應直接排入()
    {
        var start = new Coordinate(0, 0);
        var candidate = new MultiDayPlanItem("唯一景點", new Coordinate(1, 0), 30);

        var request = new MultiDayPlanRequest(
            start, start,
            OvernightStays: new List<Coordinate>(),
            DailyAvailableMinutes: new List<int> { 1000 },
            Candidates: new List<MultiDayPlanItem> { candidate });

        var result = await _sut.PlanAsync(request);

        Assert.Single(result.Days[0].Stops);
        Assert.Equal(1, result.Days[0].Stops[0].Order);
    }

    [Fact]
    public async Task PlanAsync_當天時間不足時應排入0個景點且候選留給下一天()
    {
        var start = new Coordinate(0, 0);
        var overnightStay = new Coordinate(5, 0);
        var end = new Coordinate(10, 0);
        // 候選點座標與當晚住宿相同，代表隔天從住宿出發時交通時間為 0。
        var candidate = new MultiDayPlanItem("A", new Coordinate(5, 0), 5);

        var request = new MultiDayPlanRequest(
            start, end,
            OvernightStays: new List<Coordinate> { overnightStay },
            DailyAvailableMinutes: new List<int> { 1, 1000 }, // Day1 時數極短，不足以排入任何候選
            Candidates: new List<MultiDayPlanItem> { candidate });

        var result = await _sut.PlanAsync(request);

        Assert.Empty(result.Days[0].Stops); // Day1 為純移動日（合法結果）
        Assert.Single(result.Days[1].Stops); // 候選留到 Day2 才被排入
        Assert.Equal("A", result.Days[1].Stops[0].Name);
        Assert.Empty(result.Waitlist);
    }

    /// <summary>
    /// 測試專用的固定式距離提供者：座標的緯度值直接視為一維位置（公里），
    /// 交通時間固定以「2 分鐘/公里」換算（對應 30km/h 平均車速），讓測試可以手算並精準驗證累計時間。
    /// </summary>
    private class StubDistanceProvider : IDistanceProvider
    {
        public Task<double> GetDistanceAsync(Coordinate a, Coordinate b) =>
            Task.FromResult(Distance(a, b));

        public Task<TravelInfo> GetTravelInfoAsync(Coordinate a, Coordinate b)
        {
            var distance = Distance(a, b);
            return Task.FromResult(new TravelInfo(distance, distance * 2));
        }

        private static double Distance(Coordinate a, Coordinate b) => Math.Abs(a.Latitude - b.Latitude);
    }
}
