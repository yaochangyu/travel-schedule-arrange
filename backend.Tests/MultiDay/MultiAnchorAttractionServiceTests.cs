using TravelScheduleArrange.Api.Models;
using TravelScheduleArrange.Api.Services.MultiDay;
using Xunit;

namespace TravelScheduleArrange.Api.Tests.MultiDay;

public class MultiAnchorAttractionServiceTests
{
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
