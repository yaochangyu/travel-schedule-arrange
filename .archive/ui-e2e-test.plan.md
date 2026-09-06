# UI E2E 測試 - 實作計畫

## 需求摘要
前端目前完全沒有測試（package.json 只有 vue/vue-router/vite）。使用者確認：以 **Playwright E2E 測試**
實際驅動瀏覽器走過完整使用流程，接**真實運行中的後端**，且**不可使用任何替身/mock**——後端呼叫
真實 TDX 觀光資訊資料庫與真實 Google Maps API，本機 user-secrets 已確認仍保有有效憑證
（`Tdx:ClientId`/`Tdx:ClientSecret`/`Google:ApiKey`，值未印出）。

## 技術取捨（因不使用替身而產生，請確認）
- **斷言方式改為「結構性」驗證，不檢查具體地名文字**：TDX 觀光資料集內容會隨時間變動（例如景點被
  下架、新增資料），若斷言寫死特定地名（如「臺鐵臺北車站」），日後 TDX 資料異動可能讓測試無故失敗。
  改為驗證「推薦清單筆數 > 0」「勾選後的行程結果依序編號且筆數與勾選數一致」等結構性條件。
- **測試可能較慢且偶有不穩定**：每次測試都是真實網路呼叫（TDX + Google Distance Matrix/Geocoding），
  受外部服務延遲、額度影響（先前已修復 429 重試機制，但仍非 100% 保證穩定）。
- **需要真實憑證才能執行**：CI 環境或其他開發者的機器若無對應 user-secrets，測試會直接失敗
  （非本次範圍要解決的問題，僅在此提醒）。

## BDD 框架
前端 E2E 採用 **playwright-bdd**（社群最活躍的 Playwright BDD 方案，`.feature` 檔案自動轉譯成
Playwright 測試，保留 Playwright 原生能力：自動等待、trace、報表）。Gherkin 保留字英文、步驟中文，
與後端 `backend.Bdd.Tests`（Reqnroll）的規範一致。

## 實作步驟

- [x] **步驟 1：建立 Playwright + playwright-bdd 專案與設定**
  為什麼：需要 BDD 測試執行環境，並自動啟動/關閉「真實」前後端進程（不做任何 DI 替換）。
  內容：`frontend/` 下安裝 `@playwright/test`、`playwright-bdd`；設定 `playwright.config.ts`
  以 `defineBddConfig` 指定 `features`/`steps` 路徑，`webServer` 設定自動啟動後端
  （`dotnet run --project ../backend`）與前端（`npm run dev`），皆為正常啟動、不帶特殊環境變數。

- [x] **步驟 2：撰寫單日流程 Feature 檔案與 Step Definitions（Golden Path）**
  為什麼：驗證使用者輸入目標地點 → 推薦景點/美食 → 勾選 → 產生排序行程的完整流程，串接真實
  TDX + Google API。
  內容：新增 `e2e/features/single-day-itinerary.feature`（情境：輸入固定座標 → 推薦清單至少
  1 筆 → 勾選前幾筆 → 產生行程 → 結果頁筆數與順序編號正確，不檢查具體地名）與對應
  `e2e/steps/single-day-itinerary.steps.ts`。

- [x] **步驟 3：撰寫多日流程 Feature 檔案與 Step Definitions（Golden Path）**
  為什麼：驗證多日行程規劃的完整流程（輸入起訖點/天數/住宿/時數 → 合併候選 → 勾選 → 設定停留
  時間 → 多日結果）。
  內容：新增 `e2e/features/multi-day-itinerary.feature`（情境：2 天 1 夜固定座標輸入 → 合併候選
  清單非空 → 勾選後進入編輯頁設定停留時間 → 結果頁依天分組顯示且筆數合理，不檢查具體地名）與對應
  `e2e/steps/multi-day-itinerary.steps.ts`。

- [x] **步驟 4：整合驗證**
  為什麼：確保新增的 BDD E2E 測試可正常執行，且不影響既有 `dotnet build`/`dotnet test`/
  `npm run build`。
  內容：`npx bddgen && npx playwright test` 全數通過（真實呼叫 TDX/Google API）；重新確認既有
  建置與測試不受影響。

## 狀態紀錄
- 2026-09-06：計畫建立，使用者最初選擇「可搭配替身」，經確認後改為「不可以用替身」，計畫已調整為
  完全使用真實 TDX/Google API，斷言方式改為結構性驗證以降低外部資料變動造成的脆弱性。
  已確認本機 user-secrets 仍保有 TDX/Google 有效憑證。
- 2026-09-06：使用者確認 E2E 測試也要用 BDD 風格，選定 `playwright-bdd`。計畫已調整為 Feature +
  Step Definitions 架構。
- 2026-09-06：使用者確認「自動執行」，依序完成步驟 1-4。
  - **步驟 1**：`frontend/` 安裝 `@playwright/test`（1.63.0）、`playwright-bdd`（9.2.0），並執行
    `npx playwright install chromium`。新增 `playwright.config.js`（專案為純 JS，未引入
    TypeScript），以 `defineBddConfig` 指定 `e2e/features/*.feature`、`e2e/steps/*.js`；
    `webServer` 設定同時啟動 `dotnet run --project ../backend` 與 `npm run dev`，皆為正常啟動、
    不做任何 DI 替換。新增 `package.json` script `test:e2e`（`bddgen && playwright test`）。
    `.gitignore` 新增 Playwright/playwright-bdd 產生的暫存目錄。
  - **步驟 2**：新增 `e2e/features/single-day-itinerary.feature` 與
    `e2e/steps/single-day-itinerary.steps.js`。實作時發現既有 `RecommendationListView.vue`
    預設會勾選「全部」推薦景點（非本次新增邏輯，是既有單日流程行為），若照原計畫「勾選前 2 筆」，
    需改為「保留前 2 筆勾選、取消其餘」，避免真的把上百筆候選一次送去後端做 O(n²) 的 Google
    Distance Matrix 排序（會非常慢、耗用額度）。
  - **步驟 3**：新增 `e2e/features/multi-day-itinerary.feature` 與
    `e2e/steps/multi-day-itinerary.steps.js`。`MultiDayRecommendationListView.vue` 預設「不」
    勾選任何候選（與單日流程行為不同），故此情境直接勾選前 2 筆即可，不需取消其餘。天數/每日時數
    沿用頁面預設值（2 天、每天 8 小時），僅需填起訖點與第 1 晚住宿。
  - **步驟 4**：`npx bddgen && npx playwright test` **2 個情境全數通過**（26.7 秒，真實呼叫
    TDX/Google API）。過程中多日情境的多錨點查詢（3 個相同座標）**真的觸發了一次 TDX 429**，
    但先前修復的重試/失敗隔離機制讓該錨點優雅降級、其餘結果正常回傳，測試依然通過——等同於對
    先前 429 bug fix 的一次真實世界驗證。確認 Playwright 測試結束後正確關閉其啟動的 `dotnet`
    進程（無殘留）。重新執行 `dotnet build`（0 error）、`dotnet test`（51 個測試全數通過）、
    `npm run build`，皆確認不受影響。
  - **整個計畫（步驟 1-4）已全部完成。**
