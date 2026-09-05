# 專案資料夾結構

> 排除項目：`bin/`、`obj/`、`node_modules/`、`dist/`（build 產出）
> 最後更新：2026-09-05（步驟 7 完成：整合驗證，計畫全部完成）

```
travel-schedule-arrange/
├── .issues/
│   └── travel-schedule-arrange.issues.md   # 問題與決策紀錄
├── .omc/
│   └── state/
├── backend/                                 # .NET Core Web API 專案
│   ├── Controllers/
│   │   ├── AttractionsController.cs         # GET /api/attractions/nearby（步驟 2，合併景點+美食）
│   │   ├── GeocodingController.cs           # 步驟 7：GET /api/geocoding/search（地點名稱 -> 座標）
│   │   └── ItineraryController.cs           # 步驟 7：POST /api/itinerary/plan（排序後行程，含距離）
│   ├── Data/
│   │   └── AppDbContext.cs                  # EF Core DbContext（SQLite），含 Attraction/Itinerary/ItineraryItem DbSet
│   ├── Migrations/                          # EF Core Migrations（dotnet ef migrations add 產生於此）
│   │   ├── AppDbContextModelSnapshot.cs
│   │   ├── 20260905033432_InitialCreate.cs
│   │   └── 20260905033432_InitialCreate.Designer.cs
│   ├── Models/                              # 資料表對應的 Entity 模型（步驟 5）
│   │   ├── Attraction.cs                    # 景點/美食快取（含 AttractionCategory enum）
│   │   ├── Itinerary.cs                     # 使用者產生的行程
│   │   └── ItineraryItem.cs                 # 行程中排序後的項目
│   ├── Repositories/                        # 空殼，待後續步驟補上 Repository
│   │   └── .gitkeep
│   ├── Services/                            # 行程排序服務（步驟 4）+ TDX 介接服務（步驟 2）+ Google Maps 介接服務（步驟 3）
│   │   ├── Coordinate.cs                    # 座標值物件
│   │   ├── IDistanceProvider.cs             # 距離計算抽象介面（GoogleDistanceProvider 為正式實作）
│   │   ├── MockDistanceProvider.cs          # 以 Haversine 公式模擬距離計算（保留供測試與 fallback 使用）
│   │   ├── ItineraryPlanItem.cs             # 排序輸入/輸出資料模型
│   │   ├── IItineraryPlannerService.cs      # 行程排序服務介面
│   │   ├── ItineraryPlannerService.cs       # 貪婪最近鄰排序邏輯
│   │   ├── Tdx/                             # 步驟 2：TDX 觀光資訊資料庫介接
│   │   │   ├── TdxOptions.cs                # 綁定 Tdx:ClientId/ClientSecret/AuthUrl/BaseUrl（user-secrets）
│   │   │   ├── ITdxAuthService.cs           # TDX OAuth2 Access Token 介面
│   │   │   ├── TdxAuthService.cs            # client_credentials 換 Token + 記憶體快取（Singleton）
│   │   │   ├── ITdxTourismService.cs        # 景點/美食查詢介面
│   │   │   ├── TdxTourismService.cs         # 呼叫 OData Tourism/Attraction、Tourism/Restaurant（bounding box + 應用層 Haversine 過濾）+ mapping
│   │   │   └── TdxTourismDtos.cs            # TDX OData JSON 回應對應的 DTO（Attraction/Restaurant/PostalAddress/ODataResponse）
│   │   └── GoogleMaps/                      # 步驟 3：Google Maps Distance Matrix 介接 + 步驟 7：Geocoding
│   │       ├── GoogleMapsOptions.cs         # 綁定 Google:ApiKey/BaseUrl（user-secrets）
│   │       ├── GoogleDistanceMatrixDtos.cs  # Distance Matrix API JSON 回應對應的 DTO
│   │       ├── GoogleDistanceProvider.cs    # IDistanceProvider 正式實作，內含 MockDistanceProvider fallback
│   │       ├── GoogleGeocodeDtos.cs         # 步驟 7：Geocoding API JSON 回應對應的 DTO
│   │       ├── IGoogleGeocodingService.cs   # 步驟 7：地點名稱 -> 座標 介面
│   │       └── GoogleGeocodingService.cs    # 步驟 7：呼叫 Google Geocoding API 正式實作
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── appsettings.json                     # 含 SQLite ConnectionStrings:DefaultConnection
│   ├── appsettings.Development.json
│   ├── Program.cs                           # 已註冊 AddDbContext、IDistanceProvider(GoogleDistanceProvider)、IItineraryPlannerService、Tdx/GoogleMaps 相關服務
│   ├── travel-schedule-arrange.db           # 本地 SQLite 資料庫檔（已套用 InitialCreate migration）
│   └── TravelScheduleArrange.Api.csproj     # 含 EFCore.Sqlite / EFCore.Design 套件
├── backend.Tests/                           # xUnit 單元測試專案（步驟 4、2、3）
│   ├── ItineraryPlannerServiceTests.cs       # 驗證貪婪最近鄰排序邏輯與 MockDistanceProvider
│   ├── Tdx/
│   │   └── TdxTourismServiceMappingTests.cs  # 驗證「TDX JSON → Attraction」mapping 邏輯（固定樣本，不呼叫真實 API）
│   ├── GoogleMaps/
│   │   └── GoogleDistanceProviderTests.cs    # 驗證「Google Distance Matrix JSON → 距離」解析邏輯與 fallback 邏輯（固定樣本，不呼叫真實 API）
│   └── TravelScheduleArrange.Api.Tests.csproj
├── frontend/                                 # Vue 3 + Vite 專案
│   ├── public/
│   │   ├── favicon.svg
│   │   └── icons.svg
│   ├── src/
│   │   ├── api/
│   │   │   └── client.js                    # 步驟 7：後端 API 呼叫封裝（geocode/nearby/plan）
│   │   ├── assets/
│   │   │   ├── hero.png
│   │   │   ├── vite.svg
│   │   │   └── vue.svg
│   │   ├── router/
│   │   │   └── index.js                     # Vue Router 設定（3 個頁面路由）
│   │   ├── stores/
│   │   │   └── planStore.js                 # 跨頁共享狀態（reactive，含真實 API 資料，尚未用 Pinia）
│   │   ├── views/                           # 三個主要頁面（步驟 6 建立，步驟 7 改接真實 API）
│   │   │   ├── TargetInputView.vue           # 輸入頁：地點名稱/座標 -> 呼叫 geocoding + nearby
│   │   │   ├── RecommendationListView.vue    # 推薦清單頁：真實 TDX 景點/美食卡片清單，可勾選
│   │   │   └── ItineraryResultView.vue       # 行程排序結果頁：顯示 /api/itinerary/plan 真實排序結果
│   │   ├── App.vue                          # 改為 <router-view />
│   │   ├── main.js                          # 已掛載 vue-router
│   │   └── style.css
│   ├── .vscode/
│   │   └── extensions.json
│   ├── .env.development                      # 步驟 7：VITE_API_BASE_URL（後端開發網址）
│   ├── index.html
│   ├── package.json                          # 新增 vue-router 依賴
│   ├── package-lock.json
│   ├── README.md
│   └── vite.config.js
├── tree.md                                   # 本檔案
├── travel-schedule-arrange.sln                # 新增：整合 backend + backend.Tests 的方案檔
└── travel-schedule-arrange.plan.md           # 實作計畫
```
