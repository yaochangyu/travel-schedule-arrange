namespace TravelScheduleArrange.Api.Services.MultiDay;

/// <summary>
/// 多日排程演算法的候選項目：使用者已勾選並自行設定停留時間的景點/美食。
/// </summary>
public record MultiDayPlanItem(string Name, Coordinate Location, int StayDurationMinutes);

/// <summary>
/// 多日排程演算法中，單一站點在某一天行程內的排定結果。
/// </summary>
public record MultiDayStopResult(
    string Name,
    Coordinate Location,
    int Order,
    double DistanceFromPreviousKm,
    int TravelMinutesFromPrevious,
    int StayDurationMinutes);

/// <summary>
/// 單一天的行程結果。DayNumber 由 1 開始。Stops 為空清單代表該天因時間不足未排入任何景點（純移動日）。
/// FinalLeg 為「最後一站（或若無停靠點則為當天出發錨點）→ 當天終點錨點」的交通資訊：
/// 第 1~(N-1) 天終點為當晚住宿，第 N 天終點為使用者輸入的訖點。
/// </summary>
public record DayPlanResult(
    int DayNumber,
    IReadOnlyList<MultiDayStopResult> Stops,
    double FinalLegDistanceKm,
    int FinalLegDurationMinutes);

/// <summary>
/// 多日排程演算法的完整輸出：依天分組的行程，以及所有天數排完後仍留在候選池中未排入任何一天的候補清單。
/// </summary>
public record MultiDayItineraryPlanResult(
    IReadOnlyList<DayPlanResult> Days,
    IReadOnlyList<MultiDayPlanItem> Waitlist);

/// <summary>
/// 多日排程演算法的輸入請求。
/// 錨點序列為 [Start, ...OvernightStays, End]，天數 N = DailyAvailableMinutes.Count，
/// 對應 OvernightStays.Count 必須等於 N - 1（第 1 天以 Start 為起點，第 N 天以最後一晚住宿為起點、End 為終點）。
/// </summary>
public record MultiDayPlanRequest(
    Coordinate Start,
    Coordinate End,
    IReadOnlyList<Coordinate> OvernightStays,
    IReadOnlyList<int> DailyAvailableMinutes,
    IReadOnlyList<MultiDayPlanItem> Candidates);
