Feature: 單日行程排序 API

  驗證 POST /api/itinerary/plan 的真實行為（走完整 HTTP pipeline）。外部 Google Maps 交通資訊
  以測試替身取代（座標緯度差即公里數），不涉及任何資料庫。此 API 先前完全沒有測試覆蓋，這裡補上
  基本情境，並驗證新加入的候選數量上限保護。

  Scenario: 候選景點應依貪婪最近鄰演算法排序
    Given 單日起點緯度為 0
    And 單日候選景點 "A" 位於緯度 5
    And 單日候選景點 "B" 位於緯度 2
    When 使用者送出單日行程排序請求
    Then 回應狀態碼應為 200
    And 排序結果第 1 筆應為 "B"
    And 排序結果第 2 筆應為 "A"

  Scenario: 候選景點數量超過上限應回傳400
    Given 單日起點緯度為 0
    And 單日候選景點數量為 21 筆
    When 使用者送出單日行程排序請求
    Then 回應狀態碼應為 400
