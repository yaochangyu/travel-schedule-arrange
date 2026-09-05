using Microsoft.Extensions.Logging;
using TravelScheduleArrange.Api.Models;
using TravelScheduleArrange.Api.Services;
using TravelScheduleArrange.Api.Services.MultiDay;
using TravelScheduleArrange.Api.Services.Tdx;
using Xunit;

namespace TravelScheduleArrange.Api.Tests.MultiDay;

public class MultiAnchorAttractionServiceTests
{
    [Fact]
    public async Task GetNearbyAttractionsAsync_單一錨點查詢失敗時其餘錨點結果不應受影響()
    {
        var okAnchor = new Coordinate(25.0, 121.0);
        var failingAnchor = new Coordinate(24.0, 120.0);
        var expected = new Attraction { Name = "正常錨點景點", SourceId = "1", Latitude = 25.0, Longitude = 121.0 };

        var tourismService = new StubTdxTourismService(failingAnchorLatitude: failingAnchor.Latitude, okResult: expected);
        var service = new MultiAnchorAttractionService(tourismService, NoOpLogger<MultiAnchorAttractionService>.Instance);

        var result = await service.GetNearbyAttractionsAsync(new[] { okAnchor, failingAnchor }, 3000);

        var single = Assert.Single(result);
        Assert.Equal("正常錨點景點", single.Name);
    }

    private class StubTdxTourismService : ITdxTourismService
    {
        private readonly double _failingAnchorLatitude;
        private readonly Attraction _okResult;

        public StubTdxTourismService(double failingAnchorLatitude, Attraction okResult)
        {
            _failingAnchorLatitude = failingAnchorLatitude;
            _okResult = okResult;
        }

        public Task<IReadOnlyList<Attraction>> GetNearbyScenicSpotsAsync(
            double latitude, double longitude, int radiusMeters, CancellationToken cancellationToken = default)
        {
            if (latitude == _failingAnchorLatitude)
            {
                throw new HttpRequestException("模擬 TDX API 429 重試後仍失敗");
            }

            return Task.FromResult<IReadOnlyList<Attraction>>(new List<Attraction> { _okResult });
        }

        public Task<IReadOnlyList<Attraction>> GetNearbyRestaurantsAsync(
            double latitude, double longitude, int radiusMeters, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Attraction>>(new List<Attraction>());
    }

    /// <summary>簡易 no-op ILogger，避免測試專案額外依賴 Microsoft.Extensions.Logging.Abstractions 套件版本
    /// （沿用 GoogleDistanceProviderTests 的既有作法）。</summary>
    private class NoOpLogger<T> : ILogger<T>
    {
        public static readonly NoOpLogger<T> Instance = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }

    [Fact]
    public void DeduplicateByKey_相同SourceId應只保留一筆()
    {
        var a = new Attraction { Name = "台北101", SourceId = "TDX-001", Latitude = 25.03, Longitude = 121.56 };
        var duplicate = new Attraction { Name = "台北101", SourceId = "TDX-001", Latitude = 25.03, Longitude = 121.56 };
        var other = new Attraction { Name = "士林夜市", SourceId = "TDX-002", Latitude = 25.08, Longitude = 121.52 };

        var result = MultiAnchorAttractionService.DeduplicateByKey(new[] { a, duplicate, other });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.SourceId == "TDX-001");
        Assert.Contains(result, x => x.SourceId == "TDX-002");
    }

    [Fact]
    public void DeduplicateByKey_無SourceId時應以名稱與座標作為去重鍵()
    {
        var a = new Attraction { Name = "無來源ID景點", SourceId = null, Latitude = 25.0, Longitude = 121.0 };
        var duplicate = new Attraction { Name = "無來源ID景點", SourceId = null, Latitude = 25.0, Longitude = 121.0 };

        var result = MultiAnchorAttractionService.DeduplicateByKey(new[] { a, duplicate });

        Assert.Single(result);
    }

    [Fact]
    public void DeduplicateByKey_不同錨點查無重疊時應全數保留()
    {
        var a = new Attraction { Name = "A", SourceId = "1", Latitude = 25.0, Longitude = 121.0 };
        var b = new Attraction { Name = "B", SourceId = "2", Latitude = 24.0, Longitude = 120.0 };

        var result = MultiAnchorAttractionService.DeduplicateByKey(new[] { a, b });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void DeduplicateByKey_空清單應回傳空清單()
    {
        var result = MultiAnchorAttractionService.DeduplicateByKey(Array.Empty<Attraction>());

        Assert.Empty(result);
    }
}
