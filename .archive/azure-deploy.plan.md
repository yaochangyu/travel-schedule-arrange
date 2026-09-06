# 部署到 Azure - 實作計畫

## 需求摘要
前後端皆部署到 Azure：後端用 Azure App Service（.NET Web API），前端用 Azure Static Web Apps。
手動一次性部署（非 CI/CD）。已確認：Azure CLI 已安裝並登入，訂閱為「Windows Azure MSDN -
Visual Studio Ultimate」（`63eaf14f-00fa-434d-8ae7-069e0f03b25a`，MSDN 訂閱有月度額度、非完全免費）。

## ⚠️ 重要風險提醒（請確認後再繼續）
- **後端公開部署後，任何人都能呼叫這些 API，進而消耗您自己的 Google Maps API 用量（會計費！）與
  TDX 呼叫次數配額**。目前程式碼沒有任何身分驗證或使用量限制機制。
- 建議至少：(a) 到 Google Cloud Console 為該 API Key 設定每日配額上限，避免被濫用而產生高額費用；
  (b) 部署後盡量不要公開分享網址給非預期對象。這兩點屬於 Azure/Google Console 端的設定，不在本次
  程式碼變更範圍內，需要您自行至對應 Console 設定。
- 選用免費/低成本方案（App Service **F1 免費層**、Static Web Apps **免費層**）降低 Azure 本身的費用
  風險，但 Google/TDX 的 API 用量費用/配額是獨立計算的，不受 Azure 方案影響。

## 技術選擇
- Resource Group：`rg-travel-schedule-arrange`，Region：`eastasia`（沿用您現有資源的命名慣例
  `rg-<專案名>`，東亞區域對台灣延遲較低）。
- 後端：Azure App Service（Linux，.NET 10 runtime，**F1 免費層**）。機密（`Tdx:ClientId`/
  `Tdx:ClientSecret`/`Google:ApiKey`）寫入 App Service 的 Application Settings（從本機
  user-secrets 讀值寫入，過程不印出明碼）。
- 前端：Azure Static Web Apps（**免費層**），build 時注入 `VITE_API_BASE_URL` 指向後端網址。
- CORS：`Program.cs` 目前寫死只允許 `localhost:5173`，需要改為可設定（讀取 Azure App Service
  的 Application Settings 或環境變數），正式環境新增 Static Web Apps 的網域。

## 實作步驟

- [x] **步驟 1：建立 Resource Group**
  為什麼：後續所有資源都歸屬在同一個 Resource Group，方便管理與之後整組刪除。
  內容：`az group create --name rg-travel-schedule-arrange --location eastasia`。

- [x] **步驟 2：`backend/Program.cs` 的 CORS 改為可設定**
  為什麼：現有 CORS 政策寫死 `localhost:5173`，正式環境需要允許 Static Web Apps 的網域，若還是
  寫死會導致前端呼叫後端時被 CORS 擋下。
  內容：CORS 允許的 origin 改由設定讀取（例如 `AllowedOrigins` 設定值，開發環境維持
  `localhost:5173`，正式環境於 Azure Application Settings 設定實際的 Static Web Apps 網域）。

- [x] **步驟 3：建立並部署後端 App Service**
  為什麼：需要一個公開可連線的後端服務供前端與 Playwright/使用者呼叫。
  內容：建立 App Service Plan（F1）與 Web App（.NET 10）；`dotnet publish` 後部署程式碼；於
  Application Settings 設定 `Tdx__ClientId`、`Tdx__ClientSecret`、`Google__ApiKey`（從本機
  user-secrets 讀值寫入，不印出明碼）與 `AllowedOrigins`（先設暫定值，待步驟 4 建立好 Static
  Web Apps 網址後回來更新）。

- [x] **步驟 4：建立並部署前端 Azure Static Web Apps**
  為什麼：需要公開可連線的前端頁面。
  內容：`npm run build` 時設定 `VITE_API_BASE_URL` 為步驟 3 的後端網址；建立 Static Web App 並
  部署 `dist/` 內容；確認 SPA 路由（History 模式）的 fallback 設定正確。

- [x] **步驟 5：回頭更新後端 CORS 設定並整合驗證**
  為什麼：步驟 3 建立後端時還不知道前端網址，需要回頭補上；並實際驗證公開網址上的完整流程可用。
  內容：更新 App Service 的 `AllowedOrigins` 為步驟 4 的 Static Web Apps 網址；重啟 App Service；
  實際開啟前端公開網址，跑一次單日行程流程（輸入座標 → 推薦 → 排序）確認正常運作。

## 狀態紀錄
- 2026-09-06：計畫建立。過程中發現本機未安裝 Azure CLI，已用 scoop 安裝（`azure-cli` 2.90.0）。
  `az login` 一開始因租戶的條件式存取（MFA）政策失敗、顯示「No subscriptions found」；使用者提供
  Azure Portal 網頁截圖確認確實有訂閱「Windows Azure MSDN - Visual Studio Ultimate」，改用
  `az login --tenant eee5a651-e304-4a7d-bc04-1b3fb2718735` 重試後成功登入並選中該訂閱。
  待使用者確認風險提醒與技術選擇後開始實作。
- 2026-09-06：使用者確認「先部署再處理應用程式限額使用 api」，依序完成步驟 1-5。
  - **步驟 1**：`rg-travel-schedule-arrange` 建立於 `eastasia` 成功。
  - **步驟 2**：`Program.cs` CORS 改為讀取設定 `AllowedOrigins`（未設定時預設本機開發網域），
    `dotnet build`/`dotnet test`（51 個測試）皆確認不受影響。
  - **步驟 3**：`eastasia` 的 F1 App Service Plan 建立時回報「No available instances」（容量不足），
    改用 `southeastasia` 成功建立。Web App `app-travel-schedule-arrange`（.NET 10 Linux）建立成功；
    機密透過 PowerShell 腳本讀取本機 user-secrets 檔案內容、以變數方式傳給
    `az webapp config appsettings set`（`--output none` 抑制回傳內容），全程未印出任何明碼值。
    `dotnet publish` + zip 部署成功，實測 `GET /api/attractions/nearby` 回傳 200。
  - **步驟 4**：Static Web App `swa-travel-schedule-arrange`（`eastasia`，Free）建立成功，網址
    `wonderful-island-00abc2400.5.azurestaticapps.net`。前端以 `VITE_API_BASE_URL` 指向後端網址
    build 後，用 `@azure/static-web-apps-cli`（`swa deploy`）搭配部署 token 上傳（token 透過變數
    傳遞、未印出）。**首次部署後直接訪問 `/multiday` 等子路徑回傳 404**——Azure Static Web Apps
    預設不會自動 fallback 所有子路徑到 `index.html`；修正方式：新增
    `frontend/public/staticwebapp.config.json`（`navigationFallback` 規則），重新 build + 部署後
    子路徑正確回傳 200。
  - **步驟 5**：後端 `AllowedOrigins` 補上 Static Web Apps 網址。**端對端瀏覽器實測**：輸入座標
    → 187 筆真實 TDX 景點 → 勾選 2 筆 → 產生行程 → 結果頁正確顯示排序結果（含真實 Google
    Distance Matrix 開車距離 1.152 公里），CORS、SPA 路由、後端串接皆正常。
  - **意外發現（驗證過程中真實觸發）**：`RecommendationListView.vue` 預設全部勾選（既有單日流程
    行為），若不先取消多餘勾選就直接送出，會把上百筆候選一次送去後端做真實 Google Distance Matrix
    排序，產生巨量 API 呼叫並長時間卡住（實測送出 185 筆後數十秒未完成）。這與先前風險提醒中
    「公開部署後任何使用情境都可能大量消耗 Google API 用量」的疑慮相符，**建議後續評估是否要在
    前端限制單次可勾選的候選數量上限**，避免使用者不慎（或惡意）觸發過量 API 呼叫。此為觀察到的
    真實風險，本次未著手修改（超出「先部署」的範圍），留待「處理應用程式限額使用 API」時一併處理。
  - **整個計畫（步驟 1-5）已全部完成。**

## 已部署資源（供後續清理/管理參考）
- Resource Group：`rg-travel-schedule-arrange`
- App Service Plan：`plan-travel-schedule-arrange`（F1，Southeast Asia）
- Web App（後端）：`app-travel-schedule-arrange` → https://app-travel-schedule-arrange.azurewebsites.net
- Static Web App（前端）：`swa-travel-schedule-arrange` → https://wonderful-island-00abc2400.5.azurestaticapps.net
