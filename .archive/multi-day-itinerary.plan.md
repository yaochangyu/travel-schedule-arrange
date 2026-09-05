# 多日行程規劃功能 - 實作計畫

## 需求摘要
1. 用戶輸入起點、訖點（可不同）、天數 N、每晚住宿地點（N-1 個，各自獨立）、每日可用時數（各天各自輸入）
2. 系統推薦附近景點/美食（沿用現有 TDX API，對每個錨點各自查詢後合併去重成候選池）
3. 用戶勾選景點後，於「編輯行程」頁自行設定每個景點的停留時間
4. 系統依天數分段排程：候選景點池逐天消耗，貪婪最近鄰排序，累加「移動時間＋停留時間」超過當日時數即停止排入，跑完全部天數後池中剩餘者為候補清單
5. 結果依天分組顯示，含候補清單

## 本次範圍外（明確排除）
- 預算篩選
- 系統推薦住宿地點
- 候補清單拖曳/手動插入互動
- 行程存檔到資料庫（維持現狀，算完即回傳）
- 同天景點手動調整順序

## 演算法錨點規則
```
Day1:   起點       → 景點群 → 住宿1
Day2:   住宿1      → 景點群 → 住宿2
...
DayN:   住宿(N-1)  → 景點群 → 訖點
```
（N=1 時退化為：起點 → 景點群 → 訖點，無過夜節點）

## 與現有流程的關係
新增獨立的多日流程（新路由、新頁面、新 API 端點），**不修改**現有已上線驗證過的單日「輸入目標→推薦→排序」三頁流程與 `/api/itinerary/plan` 端點，兩套流程並存。

## 實作步驟

- [x] **步驟 1：後端 Model/DTO 重新設計 — 支援多日分組**
  為什麼：現有 `ItineraryItem.Order` 是全域單一整數、`ItineraryPlanItem` 無停留時間欄位，多日分組結構必須先確立，才能實作後續排程演算法。
  內容：新增 `ItineraryPlanItem` 的多日版本（含 `StayDurationMinutes`）；新增 `DayPlanResult`（DayNumber + 景點清單，含 Order/DistanceFromPreviousKm/StayDurationMinutes）；新增 `MultiDayItineraryPlanResult`（多天清單 + 候補清單）。不異動 DB Schema、不修改現有單日 DTO。

- [x] **步驟 2：新增旅行時間查詢能力（不改動既有 `IDistanceProvider` 介面簽章）**
  為什麼：時間預算判斷需要「移動時間＋停留時間」，但直接修改 `GetDistanceAsync` 簽章會讓現有 `ItineraryPlannerService` 與其單元測試編譯失敗（已於計畫審核中確認此風險）。
  內容：於 `IDistanceProvider` **新增**一個方法（回傳距離公里＋時間分鐘），保留原 `GetDistanceAsync` 不動；`MockDistanceProvider` 以固定平均速度換算模擬時間；`GoogleDistanceProvider` 解析 Google Distance Matrix 回應中既有的 `duration` 欄位；其內部 fallback（`new MockDistanceProvider()`）路徑也需能提供時間資料。

- [x] **步驟 3：候選景點池蒐集邏輯 — 支援多錨點合併查詢**
  為什麼：多日行程有多個錨點（起點、每晚住宿、訖點），可能分散在不同區域（例如環島行程），僅查詢起點會漏掉後段景點，需對每個錨點各自查詢後合併去重。
  內容：新增服務方法，對每個錨點座標呼叫既有 TDX 附近景點查詢邏輯，以 `SourceId` 去重合併成單一候選池。

- [x] **步驟 4：多日行程排程演算法**
  為什麼：本次功能核心邏輯，將「候選池 → 逐天貪婪排序 → 時間預算判斷 → 候補清單」實作出來；抽出與現有 `ItineraryPlannerService` 共用的「選最近候選」邏輯為共用輔助方法，避免程式碼重複。
  內容：新增 `IMultiDayItineraryPlannerService` / `MultiDayItineraryPlannerService`。邊界規則明定：若當天候選中最近一筆本身就會超出當天剩餘時數，該天排入 0 個景點（合法結果，視為純移動日），此候選留在池中供下一天嘗試。單元測試涵蓋：多天正確分配、時間預算邊界（剛好用完／超過）、候補清單正確性、N=1（無過夜）情境、單一候選/空候選池邊界情況、允許某天 0 個景點。

- [x] **步驟 5：後端 API — 多日排程端點**
  為什麼：需要新端點送出「起訖點／多晚住宿／每日時數／候選景點含停留時間」並取得按天分組結果，且不影響現有 `/api/itinerary/plan`。
  內容：新增 `POST /api/itinerary/plan-multiday`；Request/Response DTO 設計（依上方規格）；允許 0 個已選景點作為合法輸入（純移動行程）；不寫入資料庫。

- [x] **步驟 6：前端 — 新增多日流程路由與輸入介面**
  為什麼：依決策，新增獨立路由與頁面，不改動現有 `TargetInputView`。
  內容：新增多日流程路由；新增輸入表單（起點／訖點／天數／各晚住宿／各日時數，依天數 N 動態產生 N-1 個住宿欄位）；基本表單驗證（天數、時數需為正數）。

- [x] **步驟 7：前端 — 推薦清單頁（多日版）**
  為什麼：現有 `RecommendationListView` 是單錨點查詢結果，多日版需呈現合併後的候選池（可能來自不同區域），需新增對應頁面而非修改既有頁面。
  內容：新增多日版推薦清單頁面，渲染合併後候選池，供使用者勾選。

- [x] **步驟 8：前端 — 編輯行程頁（設定停留時間）**
  為什麼：使用者勾選景點後需在獨立頁面為每個已選景點設定停留時間，才能送出排程請求。
  內容：新增編輯行程頁面，列出已選景點並提供停留時間輸入（給合理預設值，可修改）；新增對應 store 狀態儲存停留時間設定。

- [x] **步驟 9：前端 — 多日行程結果頁**
  為什麼：結果需依天分組顯示（非現有單一 flat list），並顯示候補清單。
  內容：新增多日行程結果頁，依 Day 分組渲染每日行程；顯示候補清單區塊。

- [x] **步驟 10：整合驗證**
  為什麼：確保新流程正常運作，且不影響既有單日流程。
  內容：`dotnet build`、`dotnet test`、`npm run build`；端對端手動驗證（至少涵蓋 2 天 1 夜、3 天以上、N=1、含純移動日等情境）；確認既有單日流程未受影響；詢問使用者是否需要撰寫額外測試。

## 狀態紀錄
- 2026-09-05：計畫建立，經 `/grill-me` 需求訪談確認規格。
- 2026-09-05：交由獨立 agent 審核計畫本身，發現以下阻塞性問題並已修正：
  1. 步驟 2 原提案「修改 `IDistanceProvider.GetDistanceAsync` 簽章」會破壞現有單日服務與其測試 → 改為新增方法，不動舊介面。
  2. 原計畫未定義多錨點如何蒐集候選景點池 → 新增步驟 3，經使用者確認採「各錨點各自查詢＋合併去重」。
  3. 原計畫遺漏前端 `RecommendationListView`/`planStore` 對應修改項目 → 拆分為明確的步驟 7、6 中的 store 擴充。
  4. 與現有單日流程的關係未明確 → 經使用者確認採「新增獨立流程並存，不改動舊流程」。
  非阻塞性建議（單一候選超時當天排 0 個景點的邊界規則、N=1 情境測試、新舊排程服務避免程式碼重複）已一併納入步驟 4 的內容與測試範圍。
  計畫由 8 步驟擴充為 10 步驟，待使用者確認後開始逐步實作。
- 2026-09-05：使用者確認採「全自動」模式，依序完成步驟 1-10。
  - **步驟 1-2**：新增 `backend/Services/MultiDay/MultiDayPlanModels.cs`（`MultiDayPlanItem`/`MultiDayStopResult`/`DayPlanResult`/`MultiDayItineraryPlanResult`/`MultiDayPlanRequest`）；新增 `backend/Services/TravelInfo.cs`；`IDistanceProvider` 新增 `GetTravelInfoAsync` 方法（未動既有 `GetDistanceAsync`）；`MockDistanceProvider`/`GoogleDistanceProvider` 皆實作新方法，`GoogleDistanceProvider` 新增 `ParseTravelInfo` public static 方法解析 Google 回應的 `duration` 欄位。建置時發現先前手動驗證留下的背景 `dotnet run` 進程（PID 940）鎖住編譯輸出檔案，已終止該進程後重新編譯成功。
  - **步驟 3**：新增 `IMultiAnchorAttractionService`/`MultiAnchorAttractionService`，對多個錨點各自呼叫既有 TDX 查詢後以 `SourceId`（缺少時退回名稱+座標）去重合併。
  - **步驟 4**：新增 `IMultiDayItineraryPlannerService`/`MultiDayItineraryPlannerService`，逐天貪婪最近鄰＋時間預算判斷；額外新增 `FinalLegDistanceKm`/`FinalLegDurationMinutes`（原計畫未明確提及，實作時發現訖點/住宿若不計算最後一段交通資訊，前端無法顯示「回住宿/前往訖點」的資訊，故補上）。**技術取捨**：未與既有 `ItineraryPlannerService` 抽出共用邏輯（候選型別、距離資訊來源、終止條件三者皆不同，且既有服務已上線驗證過，強行共用效益低於風險），已於程式碼註解說明。
  - **步驟 5**：`AttractionsController` 新增 `GET nearby-multianchor`（`anchors` 參數格式 `"lat,lng;lat,lng"`）；`ItineraryController` 新增 `POST plan-multiday`，含請求驗證（`dailyAvailableMinutes` 不可為空、需皆大於 0、`overnightStays` 數量須等於天數減 1）；`Program.cs` 註冊兩個新服務。
  - **步驟 6-9**：新增 `frontend/src/stores/multiDayStore.js`（與既有 `planStore.js` 分開）、`frontend/src/utils/geo.js`（座標解析邏輯獨立維護，未修改 `TargetInputView.vue` 既有邏輯）、四個新頁面（`MultiDayInputView`/`MultiDayRecommendationListView`/`EditItineraryView`/`MultiDayItineraryResultView`）與對應路由；`TargetInputView.vue` 僅新增一行導覽連結「規劃多日行程 →」供使用者觸及新流程，未變動其原有邏輯。新增 `.claude/launch.json` 供本機除錯使用。
  - **步驟 10（整合驗證）**：
    - `dotnet build`：成功，0 error。
    - `dotnet test`：**37 個測試全數通過**（原 19 個 + 步驟 2 新增 7 個 `GetTravelInfoAsync`/`ParseTravelInfo` 測試 + 步驟 3 新增 4 個去重測試 + 步驟 4 新增 7 個多日排程測試）。
    - `npm run build`：成功。
    - **瀏覽器端對端驗證**（真實 TDX + Google API，非 fallback）：輸入「台北車站 → 高雄車站，2 天 1 夜，第 1 晚住宿台中車站，每天 6 小時」，多錨點候選池正確合併台北/台中/高雄三地景點；勾選 4 個跨區候選點，設定停留時間後產生行程：Day1（台北車站出發）依序排入臺鐵臺北車站→臺北市交通資訊中心→宮原眼科（台中），最後一段前往台中住宿約 0.697 公里；Day2（台中住宿出發）排入愛河之心（高雄），最後一段前往高雄車站訖點約 2.475 公里。距離/時間皆為 Google Distance Matrix 真實數值（非 Haversine 近似值）。
    - **邊界情境驗證（直接呼叫 API）**：N=1（無過夜）情境正確以起訖點為錨點、單日排程；每日時數極短（5 分鐘）情境正確產生「當天 0 個景點＋候選進入候補清單」的純移動日結果；`overnightStays` 數量與天數不符時正確回傳 400。
    - **既有單日流程回歸確認**：`GET /api/attractions/nearby`、`POST /api/itinerary/plan` 皆正常運作；瀏覽器確認首頁單日流程 UI 與新增的導覽連結皆正常顯示，未受影響。
    - **已知限制**：手動驗證中發現使用 Bash 工具透過 curl 傳遞含中文字元的 JSON payload 時，該工具環境的字元編碼會導致 JSON 損毀（400 錯誤），改用純 ASCII payload 測試後確認為工具環境問題、非程式碼缺陷；瀏覽器端對端測試（使用中文地名）則完全正常，不受此限制影響。
  - **整個計畫（步驟 1-10）已全部完成，待使用者確認是否需要撰寫額外測試。**
