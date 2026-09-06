# 多日行程排程 API 的 BDD 測試 - 實作計畫

## 需求摘要
`POST /api/itinerary/plan-multiday`（多日行程排程）目前只有 `MultiDayItineraryPlannerService` 的 service
層單元測試（7 個），Controller 層的請求驗證（`dailyAvailableMinutes` 為空/含非正值、`overnightStays`
數量與天數不符）沒有測試。比照 `nearby-multianchor` 的作法，補上 BDD 測試。

## 技術選擇
- 沿用既有 `backend.Bdd.Tests` 專案（Reqnroll + `WebApplicationFactory`），不需新建專案。
- 測試替身層級：替換 `IDistanceProvider`（呼叫 Google Maps 的最底層服務），讓
  `MultiDayItineraryPlannerService` 的真實排程邏輯（貪婪最近鄰、時間預算判斷、候補清單）仍在測試
  路徑中被涵蓋，只在最外層 Google API 呼叫做替身。
- `StubDistanceProvider` 採用與既有 `MultiDayItineraryPlannerServiceTests` 相同的確定性公式
  （座標緯度差即公里數，交通時間為距離的 2 倍分鐘數），方便在 Gherkin 情境中手算預期結果。
- 不涉及資料庫（`plan-multiday` 本身不寫入資料庫，先前已確認）。
- `CustomWebApplicationFactory` 擴充為同時可替換 `ITdxTourismService`（既有）與
  `IDistanceProvider`（新增），兩個既有/新增的 BDD 測試情境互不影響。

## 實作步驟

- [x] **步驟 1：擴充 `CustomWebApplicationFactory` 與新增 `StubDistanceProvider`**
  為什麼：需要在既有測試替身架構上，新增對 `IDistanceProvider` 的替換能力。
  內容：新增 `Fakes/StubDistanceProvider.cs`；`CustomWebApplicationFactory` 新增
  `DistanceProviderStub` 屬性並於 `ConfigureWebHost` 替換 `IDistanceProvider` 註冊。

- [x] **步驟 2：撰寫 Feature 檔案**
  為什麼：以情境驗證 `plan-multiday` 的請求驗證與正常排程行為。
  內容：新增 `Features/PlanMultiDay.feature`，涵蓋：候選足夠時依時間預算分天排入、
  `dailyAvailableMinutes` 為空回 400、含非正值回 400、`overnightStays` 數量與天數不符回 400、
  時間預算不足時產生候補清單。

- [x] **步驟 3：撰寫 Step Definitions**
  為什麼：串接 Gherkin 步驟與實際 HTTP 呼叫、JSON 回應斷言。
  內容：新增 `Steps/PlanMultiDaySteps.cs`。

- [x] **步驟 4：整合驗證**
  為什麼：確保新情境與既有 BDD/xUnit 測試皆可正常執行。
  內容：`dotnet build`、`dotnet test`（三個測試專案皆需通過）。

## 狀態紀錄
- 2026-09-06：計畫建立，使用者確認「也要」補這個 API 的 BDD 測試，沿用既有 Reqnroll 基礎設施與技術取捨。
- 2026-09-06：依計畫完成步驟 1-4。
  - 過程中發現 Reqnroll 的 Step Definitions 綁定是**全域**的：`NearbyMultiAnchorSteps` 與
    `PlanMultiDaySteps` 若各自定義同一句「回應狀態碼應為 (\d+)」，執行時會出現
    「Ambiguous step definitions」錯誤（不論當前 Scenario 屬於哪個 feature，Reqnroll 都會掃到兩個
    相符方法）。修正方式：新增 `Support/ApiTestContext.cs`（透過 Reqnroll context injection，
    在同一 Scenario 內跨 Steps 類別共享 HTTP 回應狀態）與 `Steps/CommonApiSteps.cs`（共用斷言步驟），
    兩個既有 Steps 類別改為建構子注入 `ApiTestContext` 並移除各自重複定義的步驟。
  - `dotnet build` 成功（0 error）；`dotnet test` 全數通過：BDD 10 個情境（`nearby-multianchor` 5 個
    + `plan-multiday` 5 個）+ 既有 xUnit 41 個測試，共 51 個測試全數通過。
  - **整個計畫（步驟 1-4）已全部完成。**
