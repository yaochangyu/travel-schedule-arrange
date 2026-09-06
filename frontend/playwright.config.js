import { defineConfig, devices } from '@playwright/test'
import { defineBddConfig } from 'playwright-bdd'

// BDD：.feature 檔案（Gherkin，保留字英文、步驟中文）+ Step Definitions，
// playwright-bdd 會將其轉譯為 Playwright 測試（testDir 指向產生的暫存目錄）。
const testDir = defineBddConfig({
  features: 'e2e/features/*.feature',
  steps: 'e2e/steps/*.js',
})

// 刻意不替換任何外部服務：後端以正常方式啟動，實際呼叫真實 TDX 觀光資訊資料庫與
// 真實 Google Maps API（本機 user-secrets 已確認保有有效憑證）。
export default defineConfig({
  testDir,
  timeout: 60_000,
  fullyParallel: false,
  retries: 0,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'retain-on-failure',
  },
  webServer: [
    {
      command: 'dotnet run --project ../backend',
      url: 'http://localhost:5231/api/attractions/nearby?lat=25.0478&lng=121.5170&radius=500',
      reuseExistingServer: !process.env.CI,
      timeout: 60_000,
      stdout: 'pipe',
    },
    {
      command: 'npm run dev',
      url: 'http://localhost:5173',
      reuseExistingServer: !process.env.CI,
      timeout: 30_000,
      stdout: 'pipe',
    },
  ],
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
})
