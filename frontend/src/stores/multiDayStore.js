import { reactive } from 'vue'

// 多日行程規劃流程的跨頁面共享狀態，與單日流程的 planStore 刻意分開（兩套流程互不影響）。
export const multiDayStore = reactive({
  // 起訖點與每晚住宿的解析結果：{ latitude, longitude, formattedAddress }
  start: null,
  end: null,
  overnightStays: [], // 長度 = 天數 - 1
  dailyAvailableMinutes: [], // 長度 = 天數，每天可用時數（分鐘）
  // 後端 /api/attractions/nearby-multianchor 回傳的合併候選景點池
  candidates: [],
  selectedCandidateKeys: [],
  // 使用者於編輯行程頁為每個已選景點設定的停留時間（分鐘），key -> minutes
  stayDurations: {},
  // 後端 /api/itinerary/plan-multiday 回傳的排程結果：{ days: [...], waitlist: [...] }
  planResult: null,
})

export function setMultiDayInputs({ start, end, overnightStays, dailyAvailableMinutes }) {
  multiDayStore.start = start
  multiDayStore.end = end
  multiDayStore.overnightStays = overnightStays
  multiDayStore.dailyAvailableMinutes = dailyAvailableMinutes
}

export function setMultiDayCandidates(candidates) {
  multiDayStore.candidates = candidates
  multiDayStore.selectedCandidateKeys = []
  multiDayStore.stayDurations = {}
}

export function setSelectedCandidateKeys(keys) {
  multiDayStore.selectedCandidateKeys = keys
}

export function setStayDuration(key, minutes) {
  multiDayStore.stayDurations[key] = minutes
}

export function setMultiDayPlanResult(result) {
  multiDayStore.planResult = result
}

/** 候選景點的前端唯一鍵：優先用 sourceId，缺少時退回索引。 */
export function candidateKeyOf(candidate, index) {
  return candidate.sourceId ?? `idx-${index}`
}
