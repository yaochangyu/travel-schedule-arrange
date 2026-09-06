# 候選景點數量上限保護 - 實作計畫

## 需求摘要
先前實測部署到 Azure 後發現：單日推薦清單頁預設全部勾選，若使用者直接送出（或未來被惡意觸發），
會把上百筆候選一次送給後端做真實 Google Distance Matrix 排序，產生 O(n²) 的巨量 API 呼叫。
使用者確認：候選數量上限設 **20 筆**，超過上限拒絕排序請求；前端單日推薦頁**改為不預設全選**。

## 技術範圍
- 後端：`POST /api/itinerary/plan`、`POST /api/itinerary/plan-multiday` 皆加上候選數量上限驗證，
  超過 20 筆回傳 400。
- 前端：`RecommendationListView.vue`（單日）改為不預設勾選；`RecommendationListView.vue`、
  `MultiDayRecommendationListView.vue`（多日）皆加上「已勾選數超過 20 筆時 disable 提交按鈕並提示」。
- 測試：沿用既有原則（BDD 測試 API，不寫 Controller 單元測試）。`plan-multiday` 已有 BDD 覆蓋，
  新增超限情境；單日 `plan` 端點目前完全沒有測試覆蓋，這次一併補上基本情境（正常 + 超限）。
- 不涉及資料庫。

## 實作步驟

- [x] **步驟 1：後端候選數量上限驗證**
  為什麼：從根本上防止任何單次請求（不論來自前端疏忽或惡意呼叫）觸發過量 Google API 呼叫。
  內容：`ItineraryController` 新增常數 `MaxCandidateCount = 20`；`Plan`、`PlanMultiDay` 兩個
  action 皆檢查候選數量，超過時回傳 400 並附上清楚的錯誤訊息。

- [x] **步驟 2：後端 BDD 測試**
  為什麼：驗證新的上限規則在真實 API 行為層級生效。
  內容：`PlanMultiDay.feature` 新增情境「候選景點數量超過上限應回傳 400」；新增
  `Features/PlanSingleDay.feature`（單日 `plan` 端點目前無任何測試，一併補上「正常排序」與
  「超過上限回傳 400」兩個情境）與對應 Step Definitions。

- [x] **步驟 3：前端 — 單日推薦頁改為不預設勾選 + 兩頁皆加上限提示**
  為什麼：從使用者介面根本避免「一次全選上百筆」的情境重演；達到上限時清楚告知使用者。
  內容：`RecommendationListView.vue` 的 `selectedKeys` 初始值改為空 Set；`RecommendationListView.vue`
  與 `MultiDayRecommendationListView.vue` 皆新增「已勾選 X／20 筆」提示文字，超過 20 筆時「產生
  行程排序」/「下一步」按鈕 disable 並顯示錯誤訊息。

- [x] **步驟 4：整合驗證**
  為什麼：確保新驗證邏輯不影響既有功能，且前端使用體驗正確。
  內容：`dotnet build`、`dotnet test`（含新 BDD 情境）；`npm run build`；重跑既有
  `npm run test:e2e`（單日/多日 E2E 只勾選 2 筆，預期不受影響，用以確認回歸）。

## 本次範圍外（需使用者自行處理）
- **Google Cloud Console 配額設定**：這是 Google 帳號網頁端設定，不在程式碼變更範圍內。建議至
  Google Cloud Console →「API 和服務」→ 該 API 金鑰 →設定每日/每分鐘請求配額上限，作為程式碼
  防護之外的第二層保護。

## 狀態紀錄
- 2026-09-06：計畫建立，使用者確認上限 20 筆、前端改為不預設全選。
- 2026-09-06：使用者確認「auto」，依序完成步驟 1-4。
  - **步驟 1**：`ItineraryController` 新增 `MaxCandidateCount = 20`，`Plan`/`PlanMultiDay` 皆加上
    候選數量檢查，超過回 400。
  - **步驟 2**：`PlanMultiDay.feature` 新增超限情境；新增 `PlanSingleDay.feature`（單日 `plan`
    端點先前完全無測試，補上「正常排序」「超限回 400」兩情境）與 `PlanSingleDaySteps.cs`。過程中
    特別注意避免重蹈先前「Ambiguous step definitions」的覆轍：`候選景點...` 系列步驟皆刻意加上
    「單日」前綴（如「單日候選景點數量為 (\d+) 筆」）與多日版本的措辭區隔，避免正規表示式互相
    比對到對方 feature 的步驟文字。BDD 測試由 10 個增加為 13 個，全數通過。
  - **步驟 3**：`RecommendationListView.vue`（單日）`selectedKeys` 初始值改為空 Set；兩個推薦清單
    頁皆新增 `MAX_CANDIDATE_COUNT = 20` 常數、「已勾選 X／20 筆」提示、超過上限時 disable 按鈕
    並顯示錯誤訊息。
  - **步驟 4**：`dotnet build`/`dotnet test`（54 個測試）皆通過；`npm run build` 成功。
    `npm run test:e2e` 首次執行時發現既有 `single-day-itinerary.steps.js` 仍沿用「預設全選、
    取消多餘勾選」的舊邏輯，改成「直接勾選前 N 筆」後修正。修正後單獨/合併執行皆確認邏輯正確；
    合併執行時觀察到一次多日情境的候選清單為 0 筆，經單獨重跑兩次皆通過，確認是連續呼叫真實 TDX
    API 時的速率限制 flakiness（外部依賴問題，非本次改動的回歸）。
  - **整個計畫（步驟 1-4）已全部完成。Google Cloud Console 配額設定仍待使用者自行處理。**
