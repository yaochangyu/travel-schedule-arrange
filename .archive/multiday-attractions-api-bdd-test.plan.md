# 多日行程推薦景點 API 的 BDD 測試 - 實作計畫

## 需求摘要
`GET /api/attractions/nearby-multianchor`（多日行程的多錨點候選景點推薦）目前只有 service 層單元測試，
Controller 這一層（HTTP 參數解析、驗證、完整請求/回應行為）沒有測試。改用 BDD 方式測試「API 行為」本身
（不寫傳統 controller 單元測試），涵蓋正常情境與邊界情況。

## 技術選擇
- BDD 框架：**Reqnroll**（SpecFlow 已商業化，社群主流接手方案，開源免費、Gherkin 語法相容）
- 測試範圍：僅 `nearby-multianchor` 這一個 API（不含 `plan-multiday`）
- 測試替身層級：替換 `ITdxTourismService`（比 `IMultiAnchorAttractionService` 更底層），讓多錨點合併/去重/
  單錨點失敗隔離的真實邏輯仍在測試路徑中被涵蓋，只在最外層 TDX 呼叫做替身，避免測試打真實外部 API。
- 走真實 HTTP pipeline：使用 `WebApplicationFactory<Program>`（in-memory server），驗證路由/模型綁定/
  驗證邏輯皆為真實行為，非直接呼叫 controller 方法。

## 實作步驟

- [x] **步驟 1：建立 BDD 測試專案骨架**
  為什麼：BDD 測試（.feature 檔案、Step Definitions）需要獨立專案，避免與現有 xUnit 單元測試混在一起。
  內容：新增 `backend.Bdd.Tests` 專案，加入 Reqnroll.xUnit、Microsoft.AspNetCore.Mvc.Testing 套件，
  加入 `travel-schedule-arrange.sln`。

- [x] **步驟 2：建立可控的測試替身與 WebApplicationFactory**
  為什麼：需要走完整 HTTP pipeline 驗證真實 API 行為，但不能打真實 TDX API（不可控、受真實速率限制影響）。
  內容：新增 `StubTdxTourismService`（可依座標設定回傳結果或拋出例外，模擬正常/失敗情境）；新增
  `CustomWebApplicationFactory : WebApplicationFactory<Program>`，於測試環境中替換 `ITdxTourismService` 為 stub。

- [x] **步驟 3：撰寫 Feature 檔案（Gherkin，保留字英文、步驟中文）**
  為什麼：以人類可讀情境驗證 API 行為，符合 BDD 精神與既有 Cucumber 規範（保留字英文、步驟中文）。
  內容：新增 `NearbyMultiAnchor.feature`，涵蓋：
  - 正常情境：多個錨點皆查詢成功，應回傳合併去重後的候選景點清單
  - `anchors` 參數為空字串應回傳 400
  - `anchors` 格式錯誤（例如缺逗號、非數字）應回傳 400
  - `radius` 小於等於 0 應回傳 400
  - 單一錨點查詢失敗時，其餘錨點結果仍應正常回傳（呼應先前的 429 重試/失敗隔離修復）

- [x] **步驟 4：撰寫 Step Definitions**
  為什麼：串接 Gherkin 步驟與實際 HTTP 呼叫、斷言邏輯。
  內容：新增對應的 Step Definitions 類別，使用 `CustomWebApplicationFactory.CreateClient()` 實際發送
  HTTP 請求並驗證回應狀態碼/內容。

- [x] **步驟 5：整合驗證**
  為什麼：確保新 BDD 測試專案可正常執行，且不影響既有 xUnit 測試與功能。
  內容：`dotnet build`、`dotnet test`（含新舊兩個測試專案皆需通過）。

## 狀態紀錄
- 2026-09-06：計畫建立，使用者確認採用 Reqnroll、範圍僅 `nearby-multianchor`。
- 2026-09-06：使用者確認「資料庫不可 mock」規則僅適用於「若有涉及資料庫」的情況；`nearby-multianchor`
  本身完全不經過資料庫（純呼叫外部 TDX API），故此規則不適用，原計畫（stub 外部 TDX API）維持不變。
- 2026-09-06：使用者確認「自動執行」，依序完成步驟 1-5。
  - **步驟 1**：新增 `backend.Bdd.Tests` 專案（`dotnet new classlib`），加入套件 `Reqnroll.xUnit`
    （3.3.4）、`Microsoft.AspNetCore.Mvc.Testing`（10.0.11）、`Microsoft.NET.Test.Sdk`（18.9.0）、
    `xunit.runner.visualstudio`（4.0.0）、`xunit`（2.9.3，與 `backend.Tests` 版本一致）；加入
    `travel-schedule-arrange.sln`；`backend/Program.cs` 底部新增 `public partial class Program {}`
    供 `WebApplicationFactory<Program>` 從其他組件參考。
  - **步驟 2**：新增 `Fakes/StubTdxTourismService.cs`（以緯度作為錨點識別鍵，可設定回傳景點或設為失敗）；
    新增 `CustomWebApplicationFactory.cs`（`WebApplicationFactory<Program>`，於 `ConfigureWebHost` 以
    `RemoveAll<ITdxTourismService>()` + `AddSingleton` 替換為 stub，未動任何資料庫相關註冊）。
  - **步驟 3**：新增 `Features/NearbyMultiAnchor.feature`，5 個情境涵蓋：多錨點合併成功、`anchors` 空字串
    400、`anchors` 格式錯誤 400、`radius<=0` 400、單錨點失敗隔離。Reqnroll 的 MSBuild 目標自動處理
    `.feature` 檔案產生程式碼（無需額外設定 custom tool），建置時確認正常運作。
  - **步驟 4**：新增 `Steps/NearbyMultiAnchorSteps.cs`，以 `CustomWebApplicationFactory.CreateClient()`
    實際發送 HTTP 請求至 in-memory server 驗證回應狀態碼與內容。過程中發現 `Assert` 類別找不到
    （`xunit.runner.visualstudio` 不含 assertion library），額外加入 `xunit` 套件解決。
  - **步驟 5**：`dotnet build` 成功（0 error）；`dotnet test` 全數通過：BDD 5 個情境 + 既有 xUnit
    41 個測試，共 46 個測試全數通過，未影響既有功能。
  - **技術取捨**：測試替身選在 `ITdxTourismService`（比 `IMultiAnchorAttractionService` 更底層），
    讓 `MultiAnchorAttractionService` 的多錨點合併/去重/單錨點失敗隔離邏輯（先前 429 bug fix 的核心）
    仍在 BDD 測試路徑中被真實涵蓋，而非繞過它直接假造最終結果。
  - **整個計畫（步驟 1-5）已全部完成。**
