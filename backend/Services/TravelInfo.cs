namespace TravelScheduleArrange.Api.Services;

/// <summary>
/// 兩點間的交通資訊：距離（公里）與預估交通時間（分鐘）。
/// </summary>
public readonly record struct TravelInfo(double DistanceKm, double DurationMinutes);
