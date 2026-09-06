import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

Given(/^使用者開啟多日行程規劃頁$/, async ({ page }) => {
  await page.goto('/multiday')
})

When(
  /^使用者輸入起點 "([^"]+)"、訖點 "([^"]+)"、第 1 晚住宿 "([^"]+)" 並送出查詢$/,
  async ({ page }, start, end, stay) => {
    await page.locator('#start').fill(start)
    await page.locator('#end').fill(end)
    await page.locator('#stay-0').fill(stay)
    await page.getByRole('button', { name: '查詢附近景點/美食' }).click()
    await page.waitForURL('**/multiday/recommendations')
  },
)

Then(/^多日候選景點清單筆數應大於 0$/, async ({ page }) => {
  const count = await page.locator('.card-list .card').count()
  expect(count).toBeGreaterThan(0)
})

When(/^使用者勾選前 (\d+) 筆候選景點並前往設定停留時間$/, async ({ page }, n) => {
  const count = Number(n)
  const checkboxes = page.locator('.card-list .card input[type="checkbox"]')
  for (let i = 0; i < count; i++) {
    await checkboxes.nth(i).check()
  }
  await page.getByRole('button', { name: '下一步：設定停留時間' }).click()
  await page.waitForURL('**/multiday/edit')
})

Then(/^編輯行程頁應顯示 (\d+) 筆已選景點$/, async ({ page }, n) => {
  const count = await page.locator('.card-list .card').count()
  expect(count).toBe(Number(n))
})

When(/^使用者將所有已選景點的停留時間設為 (\d+) 分鐘並產生多日行程$/, async ({ page }, minutes) => {
  const inputs = page.locator('.card-list .card .duration-field input')
  const total = await inputs.count()
  for (let i = 0; i < total; i++) {
    await inputs.nth(i).fill(String(minutes))
  }
  await page.getByRole('button', { name: '產生多日行程' }).click()
  await page.waitForURL('**/multiday/result')
})

Then(/^多日行程結果應包含 (\d+) 天$/, async ({ page }, n) => {
  const count = await page.locator('.day-block').count()
  expect(count).toBe(Number(n))
})

Then(/^至少一天應包含站點$/, async ({ page }) => {
  const count = await page.locator('.day-block .itinerary-item').count()
  expect(count).toBeGreaterThan(0)
})
