import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

Given(/^使用者開啟首頁$/, async ({ page }) => {
  await page.goto('/')
})

When(/^使用者輸入目標座標 "([^"]+)" 並送出查詢$/, async ({ page }, coordinate) => {
  await page.locator('#location').fill(coordinate)
  await page.getByRole('button', { name: '查詢附近景點/美食' }).click()
  await page.waitForURL('**/recommendations')
})

Then(/^附近景點\/美食推薦清單筆數應大於 0$/, async ({ page }) => {
  const count = await page.locator('.card-list .card').count()
  expect(count).toBeGreaterThan(0)
})

When(/^使用者勾選前 (\d+) 筆推薦景點並產生行程排序$/, async ({ page }, n) => {
  const count = Number(n)
  const checkboxes = page.locator('.card-list .card input[type="checkbox"]')

  // 推薦清單預設不勾選任何候選（避免一次送出過量候選做真實排序），這裡勾選前 count 筆。
  for (let i = 0; i < count; i++) {
    await checkboxes.nth(i).check()
  }

  await page.getByRole('button', { name: '產生行程排序' }).click()
  await page.waitForURL('**/itinerary')
})

Then(/^行程結果應包含 (\d+) 筆站點$/, async ({ page }, n) => {
  const count = await page.locator('.itinerary-list .itinerary-item').count()
  expect(count).toBe(Number(n))
})

Then(/^行程結果的順序編號應為 1 到 (\d+) 依序遞增$/, async ({ page }, n) => {
  const badges = page.locator('.itinerary-list .order-badge')
  const total = Number(n)
  for (let i = 0; i < total; i++) {
    await expect(badges.nth(i)).toHaveText(String(i + 1))
  }
})
