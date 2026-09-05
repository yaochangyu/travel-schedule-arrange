namespace TravelScheduleArrange.Api.Services;

/// <summary>
/// 座標值物件（緯度/經度），供距離計算與排序演算法使用。
/// </summary>
public readonly record struct Coordinate(double Latitude, double Longitude);
