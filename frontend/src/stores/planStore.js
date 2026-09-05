import { reactive } from 'vue'

// 跨頁面共享狀態（步驟 7：已改為儲存真實後端 API 回應資料）。
export const planStore = reactive({
  targetLocation: '',
  // 目標地點解析出的座標（來自地理編碼，或使用者直接輸入的經緯度）。
  targetCoordinate: null, // { latitude, longitude, formattedAddress }
  // 後端 /api/attractions/nearby 回傳的真實景點/美食清單。
  attractions: [],
  selectedAttractionIds: [],
  // 後端 /api/itinerary/plan 回傳的排序後行程。
  sortedItinerary: [],
})

export function setTargetLocation(location) {
  planStore.targetLocation = location
}

export function setTargetCoordinate(coordinate) {
  planStore.targetCoordinate = coordinate
}

export function setAttractions(attractions) {
  planStore.attractions = attractions
}

export function setSelectedAttractionIds(ids) {
  planStore.selectedAttractionIds = ids
}

export function setSortedItinerary(items) {
  planStore.sortedItinerary = items
}
