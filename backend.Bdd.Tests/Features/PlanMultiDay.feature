Feature: 多日行程排程 API

  驗證 POST /api/itinerary/plan-multiday 的真實行為（走完整 HTTP pipeline）。
  外部 Google Maps 交通資訊以測試替身取代（座標緯度差即公里數，交通時間為距離的 2 倍分鐘數），
  不涉及任何資料庫。

  Scenario: 候選景點足夠時應依每日時間預算分天排入
    Given 起點緯度為 0、訖點緯度為 20
    And 住宿緯度依序為 "10"
    And 每日可用時數依序為 "30,1000"
    And 候選景點 "A" 位於緯度 2、停留 10 分鐘
    And 候選景點 "B" 位於緯度 12、停留 10 分鐘
    When 使用者送出多日行程排程請求
    Then 回應狀態碼應為 200
    And 第 1 天應包含景點 "A"
    And 第 2 天應包含景點 "B"

  Scenario: dailyAvailableMinutes 為空應回傳 400
    Given 起點緯度為 0、訖點緯度為 0
    And 每日可用時數依序為 ""
    When 使用者送出多日行程排程請求
    Then 回應狀態碼應為 400

  Scenario: dailyAvailableMinutes 含非正值應回傳 400
    Given 起點緯度為 0、訖點緯度為 0
    And 每日可用時數依序為 "0"
    When 使用者送出多日行程排程請求
    Then 回應狀態碼應為 400

  Scenario: overnightStays 數量與天數不符應回傳 400
    Given 起點緯度為 0、訖點緯度為 0
    And 住宿緯度依序為 "5,10"
    And 每日可用時數依序為 "480"
    When 使用者送出多日行程排程請求
    Then 回應狀態碼應為 400

  Scenario: 時間預算不足時應產生候補清單
    Given 起點緯度為 0、訖點緯度為 0
    And 每日可用時數依序為 "5"
    And 候選景點 "A" 位於緯度 2、停留 600 分鐘
    When 使用者送出多日行程排程請求
    Then 回應狀態碼應為 200
    And 第 1 天不應包含景點 "A"
    And 候補清單應包含景點 "A"
