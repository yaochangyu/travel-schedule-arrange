Feature: 多日行程推薦景點 API

  驗證 GET /api/attractions/nearby-multianchor 的真實行為（走完整 HTTP pipeline），
  外部 TDX API 呼叫以測試替身取代，不涉及任何資料庫。

  Scenario: 多個錨點皆查詢成功時應回傳合併去重後的候選景點
    Given TDX 對緯度 25 的錨點回傳景點 "臺北車站"
    And TDX 對緯度 24 的錨點回傳景點 "高雄車站"
    When 使用者以錨點 "25,121;24,120" 查詢附近景點
    Then 回應狀態碼應為 200
    And 回應應包含景點 "臺北車站"
    And 回應應包含景點 "高雄車站"

  Scenario: anchors 參數為空字串應回傳 400
    When 使用者以錨點 "" 查詢附近景點
    Then 回應狀態碼應為 400

  Scenario: anchors 格式錯誤應回傳 400
    When 使用者以錨點 "not-a-coordinate" 查詢附近景點
    Then 回應狀態碼應為 400

  Scenario: radius 小於等於 0 應回傳 400
    Given TDX 對緯度 25 的錨點回傳景點 "臺北車站"
    When 使用者以錨點 "25,121" 與半徑 0 查詢附近景點
    Then 回應狀態碼應為 400

  Scenario: 單一錨點查詢失敗時其餘錨點結果仍應正常回傳
    Given TDX 對緯度 25 的錨點回傳景點 "臺北車站"
    And TDX 對緯度 24 的錨點查詢會失敗
    When 使用者以錨點 "25,121;24,120" 查詢附近景點
    Then 回應狀態碼應為 200
    And 回應應包含景點 "臺北車站"
    And 回應景點數量應為 1
