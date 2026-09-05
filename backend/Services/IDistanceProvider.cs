namespace TravelScheduleArrange.Api.Services;

/// <summary>
/// 兩點間距離計算的抽象介面。
/// 步驟 3 將實作一個呼叫 Google Maps Distance Matrix API 的版本；
/// 目前先以 <see cref="MockDistanceProvider"/> 提供簡易計算，讓排序邏輯可先行開發與測試。
/// </summary>
public interface IDistanceProvider
{
    /// <summary>
    /// 取得座標 a 到座標 b 的距離（單位：公里）。
    /// </summary>
    Task<double> GetDistanceAsync(Coordinate a, Coordinate b);
}
