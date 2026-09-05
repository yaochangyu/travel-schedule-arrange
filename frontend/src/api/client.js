// 串接後端真實 API（步驟 7：整合驗證）。
// 後端端點：
//   GET  /api/geocoding/search?address=       地點名稱/地址 -> 座標（Google Geocoding API）
//   GET  /api/attractions/nearby?lat=&lng=&radius=   附近景點/美食（TDX 觀光資訊資料庫）
//   POST /api/itinerary/plan                  依起點座標排序候選景點/美食（Google Distance Matrix API）

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5231'

class ApiError extends Error {
  constructor(message, status) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function handleResponse(response) {
  if (!response.ok) {
    const text = await response.text().catch(() => '')
    throw new ApiError(text || `API 呼叫失敗（HTTP ${response.status}）`, response.status)
  }
  return response.json()
}

/**
 * 將地點名稱/地址轉換為座標。
 * @param {string} address
 * @returns {Promise<{latitude: number, longitude: number, formattedAddress: string}>}
 */
export async function geocodeAddress(address) {
  const url = `${API_BASE_URL}/api/geocoding/search?address=${encodeURIComponent(address)}`
  const response = await fetch(url)
  return handleResponse(response)
}

/**
 * 查詢指定座標附近的景點/美食。
 * @param {number} latitude
 * @param {number} longitude
 * @param {number} [radius] 公尺，預設沿用後端預設值（3000）
 * @returns {Promise<Array<{id:number, name:string, category:string, latitude:number, longitude:number, address:string|null, sourceId:string|null, source:string|null}>>}
 */
export async function fetchNearbyAttractions(latitude, longitude, radius) {
  const params = new URLSearchParams({ lat: latitude, lng: longitude })
  if (radius) {
    params.set('radius', radius)
  }
  const url = `${API_BASE_URL}/api/attractions/nearby?${params.toString()}`
  const response = await fetch(url)
  return handleResponse(response)
}

/**
 * 依起點座標，對候選景點/美食清單排出拜訪順序（含每站與前一站的交通距離）。
 * @param {{latitude:number, longitude:number}} start
 * @param {Array} attractions 使用者勾選的景點/美食（需含 name/latitude/longitude，可選 sourceId/category/address）
 */
export async function planItinerary(start, attractions) {
  const body = {
    startLatitude: start.latitude,
    startLongitude: start.longitude,
    attractions: attractions.map((a) => ({
      sourceId: a.sourceId ?? null,
      name: a.name,
      category: a.category ?? null,
      address: a.address ?? null,
      latitude: a.latitude,
      longitude: a.longitude,
    })),
  }

  const response = await fetch(`${API_BASE_URL}/api/itinerary/plan`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  return handleResponse(response)
}

/**
 * 查詢多個錨點（多日行程的起點/每晚住宿/訖點）附近的景點/美食，後端合併去重後回傳單一候選池。
 * @param {Array<{latitude:number, longitude:number}>} anchors
 * @param {number} [radius] 公尺，預設沿用後端預設值（3000）
 */
export async function fetchNearbyAttractionsMultiAnchor(anchors, radius) {
  const anchorsParam = anchors.map((a) => `${a.latitude},${a.longitude}`).join(';')
  const params = new URLSearchParams({ anchors: anchorsParam })
  if (radius) {
    params.set('radius', radius)
  }
  const url = `${API_BASE_URL}/api/attractions/nearby-multianchor?${params.toString()}`
  const response = await fetch(url)
  return handleResponse(response)
}

/**
 * 多日行程排程：依起訖點、每晚住宿、每日可用時數，將已選景點（含使用者設定的停留時間）分天排序。
 * @param {object} params
 * @param {{latitude:number, longitude:number}} params.start
 * @param {{latitude:number, longitude:number}} params.end
 * @param {Array<{latitude:number, longitude:number}>} params.overnightStays
 * @param {number[]} params.dailyAvailableMinutes
 * @param {Array} params.candidates 需含 name/latitude/longitude/stayDurationMinutes，可選 sourceId/category/address
 */
export async function planMultiDayItinerary({ start, end, overnightStays, dailyAvailableMinutes, candidates }) {
  const body = {
    startLatitude: start.latitude,
    startLongitude: start.longitude,
    endLatitude: end.latitude,
    endLongitude: end.longitude,
    overnightStays: overnightStays.map((s) => ({ latitude: s.latitude, longitude: s.longitude })),
    dailyAvailableMinutes,
    candidates: candidates.map((c) => ({
      sourceId: c.sourceId ?? null,
      name: c.name,
      category: c.category ?? null,
      address: c.address ?? null,
      latitude: c.latitude,
      longitude: c.longitude,
      stayDurationMinutes: c.stayDurationMinutes,
    })),
  }

  const response = await fetch(`${API_BASE_URL}/api/itinerary/plan-multiday`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  return handleResponse(response)
}

export { ApiError }
