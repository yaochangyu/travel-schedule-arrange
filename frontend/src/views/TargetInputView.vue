<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { setTargetLocation, setTargetCoordinate, setAttractions, planStore } from '../stores/planStore'
import { geocodeAddress, fetchNearbyAttractions, ApiError } from '../api/client'

const router = useRouter()
const location = ref(planStore.targetLocation)
const error = ref('')
const loading = ref(false)

// 允許使用者直接輸入「緯度,經度」（例如 25.0478,121.5170），略過地理編碼；
// 其餘輸入視為地點名稱/地址，呼叫後端 /api/geocoding/search 轉換為座標。
const COORDINATE_PATTERN = /^\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*$/

async function resolveCoordinate(input) {
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

async function handleSubmit() {
  const input = location.value.trim()
  if (!input) {
    error.value = '請輸入目標地點（地址或座標）'
    return
  }

  error.value = ''
  loading.value = true
  try {
    const coordinate = await resolveCoordinate(input)
    setTargetLocation(input)
    setTargetCoordinate(coordinate)

    const attractions = await fetchNearbyAttractions(coordinate.latitude, coordinate.longitude)
    setAttractions(attractions)

    router.push({ name: 'recommendations' })
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) {
      error.value = `找不到「${input}」對應的座標，請確認地點名稱是否正確，或改用「緯度,經度」格式輸入（例如 25.0478,121.5170）。`
    } else {
      error.value = `查詢失敗：${err.message || '請稍後再試'}`
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="page">
    <h1>台灣旅遊行程安排</h1>
    <p class="hint">請輸入您想前往的目標地點（地址/地點名稱，或「緯度,經度」座標）。</p>

    <form class="target-form" @submit.prevent="handleSubmit">
      <label for="location">目標地點</label>
      <input
        id="location"
        v-model="location"
        type="text"
        placeholder="例如：台北車站 或 25.0478,121.5170"
        :disabled="loading"
      />
      <p v-if="error" class="error">{{ error }}</p>
      <button type="submit" :disabled="loading">
        {{ loading ? '查詢中...' : '查詢附近景點/美食' }}
      </button>
    </form>

    <p class="note">
      資料來源：TDX 觀光資訊資料庫（景點/美食）、Google Maps（地理編碼/交通距離）。
    </p>

    <RouterLink class="multiday-link" :to="{ name: 'multiday-input' }">規劃多日行程 →</RouterLink>
  </section>
</template>

<style scoped>
.page {
  max-width: 480px;
  margin: 0 auto;
  padding: 2rem 1rem;
  text-align: left;
}
.hint {
  color: #666;
  margin-bottom: 1.5rem;
}
.target-form {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
label {
  font-weight: 600;
}
input {
  padding: 0.6rem;
  font-size: 1rem;
  border: 1px solid #ccc;
  border-radius: 6px;
}
button {
  margin-top: 0.5rem;
  padding: 0.7rem;
  font-size: 1rem;
  border: none;
  border-radius: 6px;
  background-color: #42b883;
  color: white;
  cursor: pointer;
}
button:hover:not(:disabled) {
  background-color: #369870;
}
button:disabled {
  background-color: #aaa;
  cursor: not-allowed;
}
.error {
  color: #d33;
  margin: 0;
}
.note {
  margin-top: 2rem;
  font-size: 0.85rem;
  color: #999;
}
.multiday-link {
  display: inline-block;
  margin-top: 1rem;
  color: #42b883;
  font-weight: 600;
}
</style>
