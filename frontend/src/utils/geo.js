import { geocodeAddress } from '../api/client'

// 允許使用者直接輸入「緯度,經度」（例如 25.0478,121.5170），略過地理編碼；
// 其餘輸入視為地點名稱/地址，呼叫後端 /api/geocoding/search 轉換為座標。
// 與 TargetInputView.vue 的同名邏輯獨立維護（多日流程與單日流程刻意保持互不影響）。
const COORDINATE_PATTERN = /^\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*$/

/**
 * 將使用者輸入（地點名稱/地址，或「緯度,經度」座標）解析為座標。
 * @param {string} input
 * @returns {Promise<{latitude:number, longitude:number, formattedAddress:string}>}
 */
export async function resolveCoordinate(input) {
  const match = input.match(COORDINATE_PATTERN)
  if (match) {
    return {
      latitude: Number(match[1]),
      longitude: Number(match[2]),
      formattedAddress: input,
    }
  }
  return geocodeAddress(input)
}
