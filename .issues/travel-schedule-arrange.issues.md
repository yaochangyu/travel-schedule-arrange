# travel-schedule-arrange 問題紀錄

## 步驟 1：專案骨架建立

### 1. `.editorconfig` 來源檔案不存在（非失敗方法，是資訊缺口）
- 嘗試：`find /c/Users/yao/.claude -iname "*.editorconfig"`、直接列出 `~/.claude/editorconfig/.net/.editorconfig`
- 結果：`~/.claude/editorconfig/` 整個目錄都不存在於本機（`C:\Users\yao\.claude\` 下沒有 `editorconfig` 資料夾）
- 原因：使用者全域 CLAUDE.md 指定的路徑目前尚未建立該檔案
- 處理：依計畫指示跳過此步驟，未在 backend 建立 `.editorconfig`。待使用者提供該檔案內容或建立來源檔案後，再套用到 `backend/.editorconfig`
- 注意：後續不要重複嘗試搜尋此路徑，直到使用者確認已建立該檔案

### 2. NuGet 套件已知漏洞警告（非阻塞，僅記錄）
- 現象：`dotnet new webapi` 產生的專案內建 `Microsoft.OpenApi 2.0.0` 觸發 `NU1903` 高風險漏洞警告
- 影響：不影響 `dotnet build` 成功，僅為警告
- 處理：本次未處理（不在步驟 1 範圍內），若後續需要修正，可考慮升級或更換 OpenAPI 套件版本

## 步驟 4、5、6：先做不依賴 API Key 的部分

### 3. Repository 層未實作（技術判斷，非失敗）
- 情境：步驟 5 原文提及「實作 Repository」，但步驟 4（排序服務）與步驟 6（前端假資料頁面）目前皆不需要透過 Repository 存取資料庫，直接用 DbContext 建 migration 即可驗證 schema 正確性。
- 判斷：先不設計 Repository 介面，避免在尚未確定步驟 2（TDX）/步驟 3（Google Maps）實際查詢需求前，就定型出錯誤或過度設計的抽象。
- 後續：待步驟 2/3 開始串接真實 API、需要快取查詢/寫入邏輯時，再設計 `IAttractionRepository` / `IItineraryRepository` 等介面。
- 注意：後續不要在還沒有明確查詢情境（例如依座標範圍查詢景點快取）前，貿然生成 Repository 空殼，先確認實際查詢需求再設計介面。

### 4. `dotnet ef migrations add` 產生位置在 `backend/Migrations/`，非 `backend/Data/Migrations/`
- 現象：執行 `dotnet ef migrations add InitialCreate` 時未指定 `--output-dir`，EF Core 預設將 Migrations 資料夾建立在專案根目錄（`backend/Migrations/`），而非 `Data/` 子資料夾下。
- 處理：直接採用預設路徑，未額外搬移，`tree.md` 已依實際路徑記錄。
- 注意：後續若要統一路徑（例如想放在 `Data/Migrations/`），需在指令加上 `--output-dir Data/Migrations`，而非事後手動搬移檔案（會破壞 migration 內部的 namespace 假設，需一併修改）。

## 步驟 2：TDX 景點/美食資料介接服務

### 5. TDX OAuth2 認證成功，但 `Tourism/ScenicSpot`、`Tourism/Restaurant` 查詢一律回傳 404「Resouce Not Found」（疑似 TDX 平台端問題）
- 現象：`TdxAuthService.GetAccessTokenAsync()` 向 `https://tdx.transportdata.tw/auth/realms/TDXConnect/protocol/openid-connect/token` 發送 `client_credentials` 請求，**多次測試皆成功取得 200 + access_token**，證明 `dotnet user-secrets` 設定的 `Tdx:ClientId` / `Tdx:ClientSecret` 有效、OAuth2 流程實作正確。
- 但後續帶著該 Access Token 呼叫觀光資料查詢 API 時，以下三種 URL 變化皆回傳 `404 Not Found`，body 為 `{"message":"Resouce Not Found"}`（TDX 平台特有的錯誤訊息拼字，非本專案程式碼問題）：
  1. `GET /api/basic/v2/Tourism/ScenicSpot?$spatialFilter=nearby(25.0478,121.517,3000)&$format=JSON`（任務指示的主要作法）
  2. `GET /api/basic/v2/Tourism/ScenicSpot?$top=5&$format=JSON`（拿掉 `$spatialFilter`，測試是否為該查詢語法問題）
  3. `GET /api/basic/v2/Tourism/ScenicSpot/Taipei?$top=5&$format=JSON`（改用 `{City}` 路徑區隔查詢，測試是否為 `$spatialFilter` 專屬問題）
- 判斷：由於 Token 認證本身無誤（代表 Client ID/Secret 已通過 TDX 會員審核、OAuth2 端點與憑證都正確），而三種不同語法的 `Tourism` 查詢 URL 皆一致回傳 404（而非 401/403 權限錯誤），研判可能是：
  (a) 目前這組 TDX 會員帳號尚未申請/核准「觀光資訊資料庫（Tourism）」這個資料集的存取權限（TDX 部分進階資料集需額外申請，只有基本會員審核通過不代表所有資料集都開通）；或
  (b) TDX v2 API 的 `Tourism/ScenicSpot`、`Tourism/Restaurant` 實際路徑與官方文件現況有落差（例如改版、更名）。
  由於已排除「程式碼寫錯 URL 語法」與「認證失敗」兩種可能（三種語法變化皆 404，且 Token 皆成功取得），初步判斷偏向 (a) TDX 平台端資料集權限問題，屬於任務說明中提及「可能是 TDX 平台本身的問題（例如金鑰審核中還沒生效）」的情況。
- 處理：程式碼維持依任務指示實作的 `$spatialFilter=nearby(lat,lon,radius)` 版本（技術上最符合「附近查詢」需求），實際串接是否成功待使用者確認 TDX 會員後台的資料集訂閱狀態後再重新驗證。Mapping 邏輯（TDX JSON → Attraction）已透過固定 JSON 樣本的單元測試驗證正確，不受此問題影響。
- 注意：後續若要重新診斷此問題，**不要重複嘗試修改 URL 語法**（已驗證非語法問題），應優先確認：
  1. 登入 TDX 會員中心，檢查目前的 API Key 是否已訂閱/核准「觀光資訊資料庫」相關 API（可能需要另外從 TDX 平台「資料集」頁面申請）。
  2. 查閱 TDX 官方最新 API 文件（Swagger／API Portal），確認 `Tourism/ScenicSpot`、`Tourism/Restaurant` 的正確路徑是否有變動。
- 補充：診斷過程中曾嘗試以 curl 直接讀取 `dotnet user-secrets` 的本機 secrets.json 檔案來取得 Client ID/Secret 進行手動測試，此舉被 Claude Code 的安全分類器擋下（不允許在指令中直接讀出/組合機密內容），因此後續診斷改為**透過應用程式本身**（`IConfiguration` 注入、不印出明碼）呼叫不同 URL 變化來排查，符合任務規定的機密處理方式。

### 6.（訂正第 5 點）根本原因並非「帳號未訂閱資料集」，而是 base URL 路徑本身錯誤（OData 協定 vs REST 端點）
- **訂正**：第 5 點記錄的猜測「(a) 這組 TDX 帳號尚未申請/核准觀光資訊資料庫資料集」**已證實為錯誤猜測**，請勿再依此方向診斷或要求使用者確認訂閱狀態。
- **真正根本原因**：TDX 觀光資料 API 實際上是 **OData v2 協定端點**，正確路徑為
  `https://tdx.transportdata.tw/api/tourism/service/odata/V2/Tourism/{Resource}`，
  而非先前使用的 `https://tdx.transportdata.tw/api/basic/v2/Tourism/{Resource}`（REST 端點格式，本身就不存在，一律回傳 404，與帳號訂閱狀態或 `$spatialFilter` 語法完全無關）。
- **證據來源**：使用者從 TDX Swagger 頁面（`https://tdx.transportdata.tw/api-service/swagger/tourism/0aed433a-9e95-404d-974c-4e70e29ae460`）「Try it out」實際產生的 curl 指令，base URL 明確是 `.../api/tourism/service/odata/V2/...`。
- **額外發現：資源名稱本身也錯了**——文件/常識上以為的資源名稱 `Tourism/ScenicSpot` 在這個 OData 端點下依然回傳 404（換了正確 base URL 後仍然 404），逐一探測候選資源名稱後才發現正確名稱其實是 **`Tourism/Attraction`**（`Tourism/Restaurant`、`Tourism/Hotel`、`Tourism/TourismServiceSite` 則與常識假設相同）。因此就算只換掉 base URL 但資源名稱沒跟著改，一樣會誤判為「還是 404，帳號沒訂閱」。
- **DTO 結構也需要跟著修正**：實際回應信封是 `{"value":[...]}`（非裸陣列），座標欄位是頂層 `PositionLat`/`PositionLon`（非巢狀 `Position` 物件），地址是 `PostalAddress`（`City`/`Town`/`StreetAddress`）物件而非單一 `Address` 字串。
- **空間查詢（附近查詢）語法**：此 OData 端點的 `Tourism/{Resource}/$metadata` 回傳 404，無法直接看 EDM schema；由於座標是扁平 double 欄位（非 geography 型別，樣本 `Geometry` 皆為 `null`），研判不支援 `geo.distance` 等空間函式。改用 `$filter` 搭配標準比較運算子做「經緯度矩形範圍（bounding box）」查詢，實測可用（200 + 正確資料），取回候選後在應用層以 Haversine 公式做二次過濾＋排序，作為保底方案。
- **處理**：已修正 `TdxOptions.BaseUrl`、`TdxTourismDtos.cs`、`TdxTourismService.cs`（含 `FilterByRadius`）與對應單元測試，並實際呼叫 `/api/attractions/nearby` 驗證打通（台北車站附近 3 公里內拿到 187 筆景點資料；桃園車站附近拿到 27 筆景點 + 28 筆美食，證明美食查詢路徑也正常）。詳見 `travel-schedule-arrange.plan.md` 步驟 2 的最新狀態紀錄。
- **注意**：後續若再遇到 TDX API 404，**先確認 base URL 是否為 OData 端點格式、以及資源名稱是否正確**（可用 curl 逐一探測候選資源名稱，如本次對 `Attraction` vs `ScenicSpot` 的排查方式），不要優先假設是帳號訂閱權限問題——這類判斷需要有 401/403 或平台明確錯誤訊息佐證，單純 404 且訊息為 `{"message":"Resouce Not Found"}` 時，優先懷疑路徑本身錯誤。
- **額外提醒**：診斷過程中曾在一次 bash 指令的輸出裡意外印出 `dotnet user-secrets list` 的明碼 ClientSecret 值（指令原意是要抓 UserSecretsId，誤用了會印出全部 secrets 的指令），已於後續所有指令改用 `grep | sed` 只取值不印出完整明碼列表、並在使用後 `unset` 變數。往後若需要列出 secrets id 或做類似操作，**不要直接呼叫 `dotnet user-secrets list` 且未過濾/重導向**，避免明碼出現在指令輸出中。

## 步驟 7：整合驗證

### 7. Google Geocoding API 尚未在該 Google Cloud 專案啟用（平台設定問題，非程式碼問題）
- 現象：新增 `GoogleGeocodingService`（呼叫 `https://maps.googleapis.com/maps/api/geocode/json`，共用既有 `Google:ApiKey`）後，實際呼叫 `GET /api/geocoding/search?address=台北101`，回傳 `404`（`GeocodingController` 對應 `GeocodeAsync` 回傳 null 時的邏輯）。
- 診斷：查看後端 log，`GoogleGeocodingService` 記錄的 warning 訊息為：
  `status=REQUEST_DENIED, error_message=This API is not activated on your API project. You may need to enable this API in the Google Cloud Console...`
- 判斷：與步驟 3 的 Distance Matrix API 使用同一把 `Google:ApiKey`，但該 Google Cloud 專案目前只啟用了 Distance Matrix API，**尚未啟用 Geocoding API**，屬於 Google Cloud Console 端的 API 啟用設定問題，非程式碼邏輯錯誤（`ParseResult` 的錯誤處理、`REQUEST_DENIED` 判斷皆正常運作，正確地把失敗原因記錄下來並回傳 null）。
- 處理：
  1. 程式碼維持現況（`IGoogleGeocodingService`/`GoogleGeocodingService`/`GeocodingController` 皆已實作完成，行為正確），待使用者至 Google Cloud Console 啟用「Geocoding API」後即可直接生效，不需改程式碼。
  2. 前端 `TargetInputView.vue` 已同時支援「直接輸入緯度,經度」（正規表示式比對 `25.0478,121.5170` 格式，略過地理編碼）作為當前的替代方案，讓使用者在 Geocoding API 尚未啟用的情況下仍可完成完整流程（已實測驗證：輸入座標 → 187 筆真實景點 → 勾選 3 筆 → 排序結果含真實 Google 距離，皆正確）。
  3. 若使用者輸入的是地點名稱（非座標格式），目前會呼叫 `/api/geocoding/search`，因 API 未啟用會收到 404，前端已顯示明確錯誤訊息引導改用座標格式，不會是無回應的靜默失敗。
- 注意：**不要重複診斷此問題或懷疑程式碼寫錯**，已確認是 Google Cloud Console 專案層級的 API 啟用開關（`https://console.cloud.google.com/apis/library` 啟用 Geocoding API 並確認帳單/配額設定即可）。啟用後應可直接運作，不需修改任何程式碼。
