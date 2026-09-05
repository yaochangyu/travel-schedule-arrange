# 台灣旅遊行程安排 - 實作計畫

## 需求摘要
1. 用戶輸入目標（地點名稱或座標皆可）
2. 推薦附近景點、美食（資料來源：TDX 觀光資訊資料庫）
3. 按照交通距離安排行程（資料來源：Google Maps API）

## 技術棧
- 前端：Vue
- 後端：.NET Core Web API
- 資料庫：SQLite（儲存行程、快取外部 API 資料）

## 前置需求（阻塞項目，需使用者提供）
- [ ] TDX API Key（尚未申請，需至 https://tdx.transportdata.tw 註冊會員取得 Client ID/Secret）
- [ ] Google Maps API Key（尚未申請，需啟用 Places API + Distance Matrix API / Routes API）

## 實作步驟

- [x] **步驟 1：專案骨架建立**
  為什麼：建立可執行、可版控的基礎結構，作為後續功能開發的起點。
  內容：.NET Core Web API 專案（含分層：Controller/Service/Repository）、Vue 前端專案（Vite）、SQLite 資料庫初始化、.editorconfig 套用。

- [x] **步驟 2：TDX 景點/美食資料介接服務**
  為什麼：取得台灣官方景點與美食資料，是「推薦附近景點美食」功能的資料來源。
  內容：實作 TDX OAuth2 認證、`ScenicSpot`、`Restaurant` API 包裝服務，依地點座標查詢週邊資料。

- [x] **步驟 3：Google Maps 交通距離介接服務**
  為什麼：計算用戶目標與各景點/美食之間的交通距離與時間，作為排序依據。
  內容：整合 Google Distance Matrix API（或 Places Nearby Search 的 routingSummaries），取得距離矩陣。

- [x] **步驟 4：行程排序演算法**
  為什麼：將推薦結果依交通距離做合理排序，形成可執行的一日/多日行程。
  內容：依距離矩陣做貪婪排序或簡易 TSP 近似排序，產出排序後的景點/美食清單。

- [x] **步驟 5：SQLite 資料持久化**
  為什麼：避免重複呼叫外部 API（節省 API 額度/加速回應），並保存使用者產生的行程。
  內容：設計資料表（景點快取、行程紀錄），實作 Repository。

- [x] **步驟 6：前端頁面開發**
  為什麼：提供使用者輸入目標、瀏覽推薦結果、查看排序後行程的介面。
  內容：輸入頁（地點/座標）、推薦清單頁、行程排序結果頁。

- [x] **步驟 7：整合驗證**
  為什麼：確保前後端串接與外部 API 呼叫皆正常運作。
  內容：後端 `dotnet build`、前端 `npm run build`，並詢問使用者是否需要撰寫/執行測試。

## 狀態紀錄
- 2026-09-05：計畫建立，等待使用者確認技術棧（Vue + .NET Core + SQLite，已確認），前置 API Key 尚未取得。
- 2026-09-05：步驟 1 完成。
  - backend：`dotnet new webapi`（.NET 10, --use-controllers）建立於 `backend/`，加入 `Controllers/`、`Services/`、`Repositories/` 空殼資料夾，新增 `Data/AppDbContext.cs`（空 DbContext）並在 `Program.cs` 註冊 `AddDbContext<AppDbContext>(UseSqlite)`，套件：`Microsoft.EntityFrameworkCore.Sqlite`、`Microsoft.EntityFrameworkCore.Design`（皆 10.0.11）。`dotnet build` 成功（0 error，2 個既有的 NU1903 套件漏洞警告，與骨架建立無關）。
  - frontend：`npm create vite@latest . -- --template vue` 建立於 `frontend/`，`npm install` 與 `npm run build` 皆成功。
  - `.editorconfig` 套用：跳過。來源路徑 `C:\Users\yao\.claude\editorconfig\.net\.editorconfig` 不存在（詳見 `.issues/travel-schedule-arrange.issues.md`）。
  - 已建立/更新 `tree.md` 反映目前結構。
- 2026-09-05：步驟 5、4、6 完成（因步驟 2、3 需 TDX / Google Maps API Key 尚未取得，先完成不依賴外部 API Key 的部分）。
  - **步驟 5（SQLite 資料持久化）**：
    - 新增 `backend/Models/Attraction.cs`（含 `AttractionCategory` enum：景點/美食）、`Itinerary.cs`、`ItineraryItem.cs`。
    - 更新 `backend/Data/AppDbContext.cs`，加入 `Attractions`、`Itineraries`、`ItineraryItems` 三個 `DbSet`，並設定外鍵/索引（`Itinerary 1-N ItineraryItem` cascade delete、`ItineraryItem N-1 Attraction` restrict delete、`Attraction.SourceId` 索引、`ItineraryItem (ItineraryId, Order)` 複合索引）。
    - 執行 `dotnet ef migrations add InitialCreate`，產生於 `backend/Migrations/`；執行 `dotnet ef database update`，成功建立 `backend/travel-schedule-arrange.db`，以 `.tables` 確認 3 張資料表 + `__EFMigrationsHistory` 皆存在。
    - Repository 層：本次未實作（計畫原文提及但步驟 4/6 皆未直接依賴，暫緩至串接真實 TDX/Google Maps API 時一併設計，避免過早決定介面形狀）。
  - **步驟 4（行程排序演算法）**：
    - 新增 `backend/Services/Coordinate.cs`（座標值物件）、`IDistanceProvider.cs`（距離計算抽象介面）、`MockDistanceProvider.cs`（以 Haversine 公式計算球面距離，作為步驟 3 前的近似模擬）。
    - 新增 `IItineraryPlannerService.cs` / `ItineraryPlannerService.cs`：以貪婪最近鄰演算法排序，`IDistanceProvider` 抽換即可切換到未來的 Google Maps 實作，排序邏輯不需修改。
    - 於 `Program.cs` 註冊 `IDistanceProvider -> MockDistanceProvider`、`IItineraryPlannerService -> ItineraryPlannerService`（Scoped）。
    - 新增測試專案 `backend.Tests`（xUnit，`dotnet new xunit`），含 `ItineraryPlannerServiceTests.cs`：驗證多點排序順序正確性、空清單、單一候選點、距離為「與前一站距離」而非累積值，以及 `MockDistanceProvider` 的零距離與對稱性。共 6 個測試。
    - 新增 `travel-schedule-arrange.sln`，整合 `backend` 與 `backend.Tests` 兩個專案，方便一次 `dotnet build` / `dotnet test`。
  - **步驟 6（前端頁面開發）**：
    - 安裝 `vue-router@4`。
    - 新增 `frontend/src/mock/attractions.js`（假的景點/美食推薦資料）與 `frontend/src/mock/planner.js`（前端假資料版貪婪最近鄰排序，模擬未來後端 API 回應）。
    - 新增 `frontend/src/stores/planStore.js`：簡易 `reactive` 共享狀態（尚未安裝 Pinia，純前端 mock 流程足夠使用最簡單方案）。
    - 新增三個頁面：`views/TargetInputView.vue`（輸入目標地點）、`views/RecommendationListView.vue`（假資料景點/美食卡片清單，可勾選）、`views/ItineraryResultView.vue`（顯示排序後行程）。
    - 新增 `router/index.js`，以 Vue Router 串接三頁（`/`、`/recommendations`、`/itinerary`），更新 `main.js` 掛載 router、`App.vue` 改為 `<router-view />`，移除不再使用的 `HelloWorld.vue`。
  - **收尾驗證**：`dotnet build`（含 `backend.Tests`）成功，0 error；`dotnet test` 6 個測試全數通過；`npm run build` 成功。
  - **技術取捨**：
    - 未實作 Repository 層（原步驟 5 描述有提及），因步驟 4、6 目前皆透過假資料/直接注入 DbContext 的方式即可驗證，且尚不確定步驟 2/3 串接後的查詢介面形狀，故暫緩，待步驟 2/3 開始串接時再一併設計 Repository 介面，避免過早設計錯誤的抽象。
    - `MockDistanceProvider` 選擇 Haversine 公式（球面直線距離）而非隨機模擬，理由：讓單元測試結果具備確定性（deterministic），且做為近似值比純隨機更貼近未來 Google Maps 實際距離的量級。
    - 前端未使用 Pinia，改用單一 `reactive` 物件作為跨頁狀態，理由：目前僅 3 個頁面、單向資料流即可滿足需求，符合「選擇最簡單可行的方式」的指示；若後續頁面/狀態變複雜，再評估導入 Pinia。
  - 已更新 `tree.md` 反映新增檔案結構。
- 2026-09-05：步驟 2（TDX 景點/美食資料介接服務）完成（程式碼與測試面），實際 TDX API 資料查詢是否打通則有保留（詳見下方說明）。
  - **TDX OAuth2 認證服務**：
    - 新增 `backend/Services/Tdx/TdxOptions.cs`（綁定 `Tdx:ClientId` / `Tdx:ClientSecret` / `Tdx:AuthUrl` / `Tdx:BaseUrl`，皆透過 `IConfiguration`/Options pattern 讀取，未在程式碼中印出明碼值）。
    - 新增 `ITdxAuthService.cs` / `TdxAuthService.cs`：以 `client_credentials` 向 TDX OAuth2 端點換取 Access Token，並以 `SemaphoreSlim` + 到期時間（含 5 分鐘緩衝）做記憶體快取，避免每次呼叫都重新要 Token。註冊為 Singleton（透過 `IHttpClientFactory` 動態取得具名 HttpClient，非直接持有單一 HttpClient 實例，避免 DNS 變更問題），讓 Token 快取能跨請求共用。
  - **TDX 觀光資料查詢服務**：
    - 新增 `ITdxTourismService.cs` / `TdxTourismService.cs`，提供 `GetNearbyScenicSpotsAsync` / `GetNearbyRestaurantsAsync`，內部呼叫 `GET Tourism/{ScenicSpot|Restaurant}?$spatialFilter=nearby(lat,lon,radius)&$format=JSON`（TDX v2 進階查詢參數，依座標+半徑做「附近」查詢），並帶 `Authorization: Bearer {token}`。
    - 新增 `TdxTourismDtos.cs`（`TdxPositionDto`/`TdxScenicSpotDto`/`TdxRestaurantDto`），及 `TdxTourismService.MapScenicSpot` / `MapRestaurant` 兩個 `public static` mapping 方法，將 TDX 回傳 JSON 轉換為既有的 `Attraction` entity（`Category` 依來源分別設為 `ScenicSpot`/`Restaurant`，`Source="TDX"`、`SourceId` 對應 TDX 原始 ID）。
  - **API Controller**：新增 `backend/Controllers/AttractionsController.cs`，提供 `GET /api/attractions/nearby?lat=..&lng=..&radius=..`（radius 預設 3000 公尺），內部並行呼叫景點+美食查詢後合併回傳，暫不寫入資料庫快取（進階優化，本次未做）。
  - **測試**：新增 `backend.Tests/Tdx/TdxTourismServiceMappingTests.cs`，以固定 JSON 樣本字串測試「TDX JSON → Attraction」mapping 邏輯（景點/美食皆正常轉換、缺少 `Position` 時以 0 座標處理而不丟例外），未呼叫真實 TDX API。測試總數由 6 個增加為 9 個，全數通過。
  - **`Program.cs`**：新增 `Configure<TdxOptions>`、`AddHttpClient("Tdx")`、`AddSingleton<ITdxAuthService, TdxAuthService>`、`AddScoped<ITdxTourismService, TdxTourismService>` 註冊。
  - **手動驗證結果（重要，有所保留）**：
    - 啟動後端並實際呼叫 `/api/attractions/nearby`，**OAuth2 認證多次測試皆成功**（TDX Token 端點回傳 200 + access_token），證明 Client ID/Secret 正確、認證流程實作無誤。
    - 但後續呼叫 `Tourism/ScenicSpot`、`Tourism/Restaurant` 查詢 API 時，`$spatialFilter` 版本、拿掉 `$spatialFilter` 的陽春版本、`/{City}` 路徑版本，三種 URL 皆一致回傳 `404 Not Found`（`{"message":"Resouce Not Found"}`）。由於認證成功但查詢皆 404（非 401/403），初步判斷可能是這組 TDX 帳號尚未核准/訂閱「觀光資訊資料庫」這個資料集（TDX 部分資料集需額外申請），而非程式碼 URL 語法錯誤。詳細診斷過程與後續處理建議記錄於 `.issues/travel-schedule-arrange.issues.md`。
    - 因此：**TDX 認證已打通，但實際觀光資料查詢尚未確認打通**，需使用者至 TDX 會員中心確認資料集訂閱狀態後才能進一步驗證。Mapping 邏輯本身已用固定樣本測試驗證正確，不受此問題影響。
  - **技術取捨**：
    - Token 快取選擇 Singleton + `IHttpClientFactory`（而非讓 Singleton 直接持有 HttpClient），兼顧快取狀態需要跨請求共用、又不違反「不要讓 Singleton 長期持有單一 HttpClient」的最佳實踐。
    - `TdxTourismService` 註冊為 Scoped（本身無跨請求需共用的狀態，Token 快取已交給 `ITdxAuthService` 處理）。
    - `AttractionsController` 暫不寫入 SQLite 快取（步驟 5 已有的 `Attraction` 資料表），先滿足「查詢並回傳」的基本需求；快取寫入/命中邏輯留待後續視需求評估（避免過早設計 Repository，呼應步驟 5 當時的技術判斷）。
- 2026-09-05：步驟 3（Google Maps 交通距離介接服務）完成，**Google API 已實際打通並驗證成功**。
  - **GoogleMapsOptions**：新增 `backend/Services/GoogleMaps/GoogleMapsOptions.cs`，綁定 `Google:ApiKey`（透過 user-secrets 設定，未寫入 appsettings.json）與 `Google:BaseUrl`（預設 Distance Matrix API 端點）。於 `Program.cs` 以 `builder.Services.Configure<GoogleMapsOptions>(builder.Configuration.GetSection("Google"))` 註冊。
  - **GoogleDistanceProvider**：新增 `backend/Services/GoogleMaps/GoogleDistanceProvider.cs`，實作既有的 `IDistanceProvider` 介面（方法簽章與 `MockDistanceProvider` 一致，回傳單位維持公里，與 `MockDistanceProvider` 相同），可無縫替換。
    - 以 `IHttpClientFactory`（具名 client `"GoogleMaps"`）呼叫 `https://maps.googleapis.com/maps/api/distancematrix/json`，固定 `mode=driving`（`IDistanceProvider` 介面目前未支援交通模式參數，故先固定開車模式）。
    - 新增 `GoogleDistanceMatrixDtos.cs`（`GoogleDistanceMatrixResponse`/`Row`/`Element`/`Value`），JSON 解析邏輯獨立為 `GoogleDistanceProvider.ParseDistanceMeters(string json, out string failureReason)` public static 方法，方便單元測試以固定 JSON 樣本驗證，不需真的呼叫 Google API（呼應 `TdxTourismService.MapScenicSpot`/`MapRestaurant` 的既有風格）。
    - **Fallback 機制（decorator pattern）**：`GoogleDistanceProvider` 內部持有一個 `MockDistanceProvider` 實例。當 HTTP 非成功狀態、頂層 `status` 非 `OK`（例如 `OVER_QUERY_LIMIT`、`REQUEST_DENIED`）、`element.status` 非 `OK`、JSON 格式無法解析，或呼叫過程拋出例外時，皆以 `ILogger<GoogleDistanceProvider>.LogWarning` 記錄明確標註「fallback」的訊息（不含 API Key），並改呼叫 `MockDistanceProvider` 計算 Haversine 近似距離，確保系統仍可運作。
  - **`Program.cs`**：新增 `AddHttpClient("GoogleMaps")`；將 `IDistanceProvider` 註冊由 `MockDistanceProvider` 換成 `GoogleDistanceProvider`（`MockDistanceProvider` 類別本身保留，供單元測試與 fallback 使用）。
  - **測試**：新增 `backend.Tests/GoogleMaps/GoogleDistanceProviderTests.cs`，共 9 個測試：
    - `ParseDistanceMeters` 靜態方法：成功回應解析、`OVER_QUERY_LIMIT`、`REQUEST_DENIED`（含 `error_message`）、element 狀態非 OK、JSON 格式錯誤，共 5 個案例。
    - `GetDistanceAsync` 端對端（以自訂 `HttpMessageHandler` + 自訂 `IHttpClientFactory` 模擬回應，未使用額外 Mock 套件）：API 成功時回傳正確公里數、`REQUEST_DENIED` 時 fallback 到 `MockDistanceProvider`（比對數值與 Mock 直接計算結果一致）、HTTP 500 時 fallback、呼叫拋出例外時 fallback，共 4 個案例。
    - 測試總數由 9 個增加為 18 個，全數通過，未呼叫真實 Google API。
  - **手動驗證結果（重要）**：
    - 以臨時測試檔（驗證後已刪除，未留在程式庫中）讀取 user-secrets 中的 `Google:ApiKey`，實際呼叫 `GoogleDistanceProvider.GetDistanceAsync` 查詢「台北車站 → 台北101」的開車距離，**Google Distance Matrix API 呼叫成功**，解析出距離為 **5.146 公里**（符合實際地理常識，非 fallback 近似值），證明 API Key 有效、Distance Matrix API 已啟用、程式碼串接邏輯正確。過程中全程未印出 API Key 明碼。
    - 驗證完成後，臨時測試檔與其專用的 `Microsoft.Extensions.Configuration.UserSecrets` 套件參考皆已移除，`backend.Tests.csproj` 已還原為驗證前狀態，確認不影響最終 `dotnet build`/`dotnet test` 結果。
  - **技術取捨**：
    - 距離解析邏輯抽成 public static 方法而非留在實例方法內，理由與 `TdxTourismService` 的 mapping 方法一致：讓單元測試能用固定 JSON 樣本直接驗證，不需要 mock `HttpClient`。
    - Fallback 測試未引入 Moq 等 mocking 套件，改用自訂 `HttpMessageHandler` + `IHttpClientFactory` 實作，理由：`backend.Tests` 目前無 mocking 套件依賴，避免為單一功能新增套件，且 `HttpMessageHandler` 為 BCL 內建型別即可滿足需求。
    - `GoogleDistanceProvider` 內部直接 `new MockDistanceProvider()` 而非透過 DI 注入第二個 `IDistanceProvider`，理由：避免 DI 容器對同一介面的多重註冊造成混淆（`IDistanceProvider` 的 DI 註冊本身就是給 `ItineraryPlannerService` 用的唯一實作 `GoogleDistanceProvider`），fallback 屬於 `GoogleDistanceProvider` 內部實作細節。
  - **收尾驗證**：`dotnet build`（含 `backend.Tests`）成功，0 error；`dotnet test` 18 個測試全數通過。
  - **後續步驟 7（整合驗證）現況**：步驟 2、3、4、5、6 皆已完成程式碼與單元測試層級的驗證，Google Maps 部分已實際打通並確認可用；TDX 部分認證打通但資料查詢因帳號資料集訂閱問題暫時 404（不影響本次任務範圍）。**現在已可進行步驟 7 的整合驗證**（後端 `dotnet build`、前端 `npm run build`），但若要端對端測試「輸入目標 → 取得推薦景點/美食 → 排序行程」的完整流程，會因 TDX 資料查詢 404 而在推薦景點/美食這一段卡住，需視當時 TDX 訂閱狀態決定是否用假資料繞過或等待 TDX 帳號問題解決。
- 2026-09-05：**步驟 2 修正：推翻「帳號未訂閱資料集」的猜測，找到真正根本原因並打通 TDX 觀光資料查詢**。
  - **根本原因確認**：先前判斷「TDX 帳號可能未訂閱觀光資訊資料庫」是錯誤猜測。使用者從 TDX Swagger 頁面「Try it out」實際產生的 curl 指令證實，TDX 觀光資料 API 真正的協定是 **OData v2**，正確 base URL 為
    `https://tdx.transportdata.tw/api/tourism/service/odata/V2/Tourism/{Resource}`，
    而非原本程式碼使用的 `https://tdx.transportdata.tw/api/basic/v2/Tourism/{Resource}`（REST 端點路徑，會回傳 404，這才是先前查詢一直失敗的真正原因，跟資料集訂閱狀態無關）。
  - **實際呼叫驗證過程**（皆以自己的 OAuth2 Token + curl 測試，未印出 Client ID/Secret 明碼）：
    1. 直接沿用 Swagger 產生的 URL（`Tourism/TourismServiceSite`）測試，確認新 base URL 搭配 Bearer Token 可拿到 200 + 真實資料。
    2. 沿用文件常見的資源名稱 `Tourism/ScenicSpot` 測試，**仍為 404**；改用逐一探測其他資源名稱後，發現正確資源名稱其實是 **`Tourism/Attraction`**（非 `ScenicSpot`），`Tourism/Restaurant` 則與原假設相同、驗證正確。
    3. 取得 `Attraction`、`Restaurant` 的實際回應 JSON 樣本，發現：
       - 回應信封為 `{"value": [...]}`（非原本假設的裸陣列）。
       - 座標欄位是頂層的 `PositionLat`/`PositionLon`（double），並非原本 DTO 假設的巢狀 `Position.PositionLat/PositionLon`。
       - 沒有單一 `Address` 字串欄位，地址資訊在 `PostalAddress`（`City`/`Town`/`StreetAddress` 等子欄位），需自行組合。
    4. 研究「附近查詢」語法：測試 `Tourism/{Resource}/$metadata` 端點回傳 404（無法直接列出 EDM schema 佐證是否支援空間函式）；由於座標欄位本身是扁平的 `PositionLat`/`PositionLon`（非 geography 空間型別，樣本中 `Geometry` 欄位皆為 `null`），研判此 OData 端點不支援 `geo.distance` 等空間函式，只支援標準 OData 比較運算子。實測以 `$filter=PositionLat ge .. and PositionLat le .. and PositionLon ge .. and PositionLon le ..`（經緯度矩形範圍）查詢，**確認可正常運作（200 + 正確資料）**。因此採用保底方案：OData `$filter` 先以矩形範圍縮小候選集合，取回後在應用層以 Haversine 公式做二次過濾（排除矩形角落超出實際圓形半徑的點）並依距離排序。
  - **程式碼修正內容**：
    - `TdxOptions.cs`：`BaseUrl` 改為 `https://tdx.transportdata.tw/api/tourism/service/odata/V2`。
    - `TdxTourismDtos.cs`：新增 `TdxODataResponse<T>`（信封 `{"value": [...]}`）、`TdxPostalAddressDto`；`TdxScenicSpotDto` 更名為 `TdxAttractionDto`（對應正確資源名稱 `Attraction`），座標欄位改為頂層 `PositionLat`/`PositionLon`，移除巢狀 `Position`/`TdxPositionDto`。
    - `TdxTourismService.cs`：查詢資源名稱由 `ScenicSpot` 改為 `Attraction`；`QueryNearbyBoundingBoxAsync<T>` 改用 `$filter` 矩形範圍查詢（`CalculateBoundingBox` 計算矩形）、`Accept: application/json;odata.metadata=none` header、解析 `TdxODataResponse<T>` 信封；新增 `FilterByRadius`（public static，方便單元測試）以 Haversine 二次過濾 + 依距離排序；`MapScenicSpot`/`MapRestaurant` 改讀取頂層座標欄位，新增 `BuildAddress` 組合 `PostalAddress` 為地址字串。
    - `backend.Tests/Tdx/TdxTourismServiceMappingTests.cs`：測試樣本 JSON 改為符合實際 OData 回應格式（`{"value":[...]}`、頂層座標、`PostalAddress`），新增 `FilterByRadius_應排除半徑外的點並依距離排序` 測試案例。
  - **重新驗證結果（重要，這次是真的打通）**：
    - 啟動後端，實際呼叫 `GET /api/attractions/nearby?lat=25.0478&lng=121.5170&radius=3000`（台北車站，半徑 3 公里），**回傳 HTTP 200 + 187 筆真實景點資料**（例如「臺鐵臺北車站」「台北當代藝術館」「台北地下街」等），證明景點查詢已完全打通。
    - 同一座標的美食（Restaurant）查詢回傳 0 筆。經直接以 curl 對相同經緯度矩形範圍呼叫 TDX Restaurant API 確認，**TDX 本身在該矩形範圍內就是回傳空陣列**（非程式碼問題）；改用桃園車站座標（`lat=24.9923&lng=121.3139&radius=3000`）重新測試，**回傳 27 筆景點 + 28 筆美食**，證明美食查詢邏輯、mapping、半徑過濾皆正常運作，台北車站附近 0 筆單純是 TDX 這個政府資料集在該小範圍內剛好沒有美食資料點（資料涵蓋率問題，非串接問題）。
    - `dotnet build`（含 `backend.Tests`）成功，0 error；`dotnet test` **19 個測試全數通過**（原 18 個，新增 1 個 `FilterByRadius` 測試）。
  - **技術取捨**：
    - Bounding box 用 `$top=200` 限制候選集合大小，避免大範圍查詢時payload過大；若未來需要更精準的分頁策略（例如城市人口密集區候選點超過 200 筆導致漏收），可再評估改用 `$skip`/`$top` 分頁抓取或縮小矩形再合併多次查詢。
    - `FilterByRadius` 做成 `public static`（而非 `internal`），因為 `backend.Tests` 是獨立組件（無 `InternalsVisibleTo` 設定），沿用既有 `MapScenicSpot`/`MapRestaurant` 的風格（public static 供測試直接呼叫）。
  - **後續步驟 7（整合驗證）現況更新**：TDX 觀光資料查詢已確認實際打通（非僅認證打通），推薦景點/美食功能的資料來源問題已排除，前一版計畫中「等待 TDX 訂閱狀態」的阻塞項目可移除。步驟 7 整合驗證可以正常進行完整端對端測試（輸入目標 → 取得真實推薦景點/美食 → 依距離排序）。
- 2026-09-05：**步驟 7（整合驗證）完成**，前端已改為呼叫真實後端 API，並完成一次完整端對端手動驗證。

  - **後端新增：排序端點 + 地理編碼服務**
    - 新增 `backend/Controllers/ItineraryController.cs`：`POST /api/itinerary/plan`，接受 `{ startLatitude, startLongitude, attractions: [{sourceId, name, category, address, latitude, longitude}] }`，內部呼叫既有 `IItineraryPlannerService.PlanAsync` 排序，並用 `(Name, Latitude, Longitude)` 對回原始請求項目補上 `sourceId`/`category`/`address`，回傳含 `order`/`distanceFromPreviousKm`（四捨五入至小數 3 位）的排序結果。
    - 新增 `backend/Controllers/GeocodingController.cs`：`GET /api/geocoding/search?address=`，呼叫新增的 `IGoogleGeocodingService`（`backend/Services/GoogleMaps/GoogleGeocodingService.cs`、`IGoogleGeocodingService.cs`、`GoogleGeocodeDtos.cs`），共用既有 `Google:ApiKey`，將地點名稱轉換為座標，找不到則回傳 404。`ParseResult` 比照 `GoogleDistanceProvider.ParseDistanceMeters` 風格，獨立為 public static 方法。
    - `Program.cs`：新增 `AddScoped<IGoogleGeocodingService, GoogleGeocodingService>()`；為了讓前端可直接讀懂 `Category` 欄位，`AddControllers()` 加上 `JsonStringEnumConverter`（回應由數字 enum 改為 `"ScenicSpot"`/`"Restaurant"` 字串）；新增 CORS policy `FrontendDev`，允許 `http://localhost:5173` 呼叫本機 API（僅開發用途，未對正式環境設定）。
  - **前端改為呼叫真實 API**
    - 新增 `frontend/src/api/client.js`：`geocodeAddress`、`fetchNearbyAttractions`、`planItinerary` 三個函式，統一處理 `fetch` 與錯誤（`ApiError` 帶 HTTP 狀態碼）。
    - 新增 `frontend/.env.development`：`VITE_API_BASE_URL=http://localhost:5231`（對應後端 `http` launch profile，開發環境避免自簽憑證問題）。
    - `TargetInputView.vue`：改為 async 提交，若輸入符合「緯度,經度」格式（正規表示式）直接使用，否則呼叫 `/api/geocoding/search`；成功後呼叫 `/api/attractions/nearby`，將座標與景點清單存入 `planStore`，含 loading/錯誤狀態顯示。
    - `RecommendationListView.vue`：改用 `planStore.attractions`（真實 TDX 資料）渲染卡片，因後端未寫入資料庫、`Attraction.Id` 恆為 0，改以 `sourceId`（找不到時退回索引）作為 Vue `key` 與勾選狀態依據；`category`（`"ScenicSpot"`/`"Restaurant"`）對照為中文「景點」/「美食」；「產生行程排序」改為呼叫 `/api/itinerary/plan`，含 loading/錯誤狀態。
    - `ItineraryResultView.vue`：改為顯示 `/api/itinerary/plan` 的真實回應欄位（`sourceId`/`name`/`category`/`address`/`order`/`distanceFromPreviousKm`）。
    - `planStore.js`：新增 `targetCoordinate`、`attractions` 欄位與對應 setter。
    - 刪除 `frontend/src/mock/`（`attractions.js`、`planner.js`）：三個頁面皆已改走真實 API，假資料與假排序邏輯不再被任何檔案引用，故移除而非保留（避免死碼）。
  - **端對端手動驗證（實際結果）**：
    1. `curl GET /api/attractions/nearby?lat=25.0478&lng=121.5170&radius=1000`：回傳 HTTP 200，187 筆真實景點（`category` 已正確序列化為 `"ScenicSpot"` 字串，欄位含 `sourceId`/`address`/`latitude`/`longitude`），與前端實際渲染結果一致。
    2. `curl GET /api/geocoding/search?address=台北101`：回傳 HTTP 404。查後端 log 為 Google `REQUEST_DENIED`（`This API is not activated on your API project`）——這組 API Key 尚未在 Google Cloud Console 啟用 Geocoding API（僅啟用了步驟 3 用到的 Distance Matrix API），屬於平台設定問題非程式碼問題，詳見 `.issues/travel-schedule-arrange.issues.md` 第 7 點。
    3. `curl POST /api/itinerary/plan`（起點：台北車站 25.0478,121.5170；候選：臺鐵臺北車站、臺北市交通資訊中心、逸仙公園 3 筆真實 TDX 資料）：回傳 HTTP 200，排序結果 `[臺鐵臺北車站(0.007km) → 臺北市交通資訊中心(0.238km) → 逸仙公園(0.215km)]`，距離皆為 Google Distance Matrix 真實開車距離（非 fallback 近似值）。
    4. **瀏覽器實測完整流程**（`dotnet run` + `npm run dev`，透過瀏覽器自動化工具實際操作）：於輸入頁輸入座標 `25.0478,121.5170`（因 Geocoding API 未啟用，改用座標輸入路徑）→ 推薦清單頁即時顯示 187 筆真實 TDX 景點資料（無 fallback/假資料）→ 勾選其中 3 筆（臺鐵臺北車站、臺北市交通資訊中心、逸仙公園）→ 點擊「產生行程排序」→ 行程結果頁顯示排序後行程，距離數值與上述 curl 驗證結果完全一致（`0.007`/`0.238`/`0.215` 公里），證明前端資料串接、欄位對應（`category`/`address`/`sourceId`/`order`/`distanceFromPreviousKm`）皆正確無誤。
  - **技術取捨與已知限制**：
    - **Google Geocoding API 尚未啟用**：這是本次任務範圍外的平台設定（需使用者至 Google Cloud Console 啟用），程式碼（`GoogleGeocodingService`/`GeocodingController`/前端呼叫邏輯）已完整實作並確認錯誤處理正確；啟用後地點名稱查詢應可直接生效，不需改程式碼。目前使用者可用「緯度,經度」格式直接輸入座標繞過此限制，前端已提供對應的錯誤訊息引導。
    - **排序端點以 `(Name, 座標)` 反查原始請求項目**：因既有 `ItineraryPlanItem`（`backend/Services/ItineraryPlanItem.cs`，步驟 4 已定義）只帶 `Name`/`Location`，未修改既有介面/演算法（避免影響步驟 4 既有測試與抽象），改在 Controller 層以 `(Name, Latitude, Longitude)` 三元組對照回補 `sourceId`/`category`/`address`。此法假設同一批候選點不會有「名稱與座標完全相同」的重複資料，實務上 TDX 真實資料不會有此情況。
    - **未寫入 SQLite 快取**：`AttractionsController` 沿用步驟 2 的技術判斷（查詢並回傳，不寫入 `Attraction` 資料表），`Attraction.Id` 恆為 0，前端已改用 `sourceId` 作為前端唯一鍵因應。
    - **CORS 僅設定開發環境網域**（`http://localhost:5173`），未考慮正式部署網域，屬於本次「整合驗證」範圍內的最小可行設定。
    - **Mock 資料檔案已刪除**（`frontend/src/mock/`），非保留當 fallback：三個頁面皆已完全改走真實 API，繼續保留會變成無人引用的死碼，故依指示自行判斷後移除。
  - **收尾驗證**：`dotnet build`（含 `backend.Tests`）成功，0 error；`dotnet test` **19 個測試全數通過**（本步驟未新增後端測試，因新增的 `ItineraryController`/`GeocodingController` 皆為對既有已測試服務的組裝邏輯，端對端行為已透過上述 curl + 瀏覽器實測涵蓋）；`npm run build` 成功。
  - **整個計畫（步驟 1-7）已全部完成。**
- 2026-09-05：使用者於 Google Cloud Console 啟用 Geocoding API 並將其加入該 API Key 的 API restrictions 允許清單（與既有 Distance Matrix API 並列）後，重新驗證：
  - `curl GET /api/geocoding/search?address=台北101` 改為回傳 HTTP 200，`{"latitude":25.0332276,"longitude":121.5648681,"formattedAddress":"110台灣臺北市信義區西村里信義路五段No. 7號"}`，地名查詢正式打通。
  - 接續呼叫 `curl GET /api/attractions/nearby?lat=25.0332276&lng=121.5648681&radius=1000`，正確查到「台北101」「幾米月亮公車」等真實 TDX 景點，證明「輸入地名 → 地理編碼 → 查詢附近景點」完整鏈路可用。
  - 第 161 行所述「Google Geocoding API 尚未啟用」限制已解除，原本的座標輸入備用路徑（`TargetInputView.vue` 的正規表示式判斷）仍保留作為備援，不影響功能。
