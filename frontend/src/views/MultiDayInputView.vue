<script setup>
import { ref, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { resolveCoordinate } from '../utils/geo'
import { fetchNearbyAttractionsMultiAnchor, ApiError } from '../api/client'
import { setMultiDayInputs, setMultiDayCandidates } from '../stores/multiDayStore'

const router = useRouter()

const startInput = ref('')
const endInput = ref('')
const days = ref(2)
// overnightInputs 長度 = days - 1；dailyHoursInputs 長度 = days
const overnightInputs = ref(['']) // 天數預設 2 天，需要 1 個住宿欄位
const dailyHoursInputs = ref([8, 8]) // 每天預設可用 8 小時

const error = ref('')
const loading = ref(false)

const overnightCount = computed(() => Math.max(0, days.value - 1))

// 天數變動時，動態調整住宿欄位與每日時數欄位的數量，盡量保留使用者已輸入的內容。
watch(days, (newDays) => {
  const n = Math.max(1, Math.floor(newDays) || 1)
  const targetOvernightCount = Math.max(0, n - 1)

  if (overnightInputs.value.length < targetOvernightCount) {
    overnightInputs.value = [
      ...overnightInputs.value,
      ...Array(targetOvernightCount - overnightInputs.value.length).fill(''),
    ]
  } else {
    overnightInputs.value = overnightInputs.value.slice(0, targetOvernightCount)
  }

  if (dailyHoursInputs.value.length < n) {
    dailyHoursInputs.value = [
      ...dailyHoursInputs.value,
      ...Array(n - dailyHoursInputs.value.length).fill(8),
    ]
  } else {
    dailyHoursInputs.value = dailyHoursInputs.value.slice(0, n)
  }
})

async function handleSubmit() {
  const startText = startInput.value.trim()
  const endText = endInput.value.trim()

  if (!startText || !endText) {
    error.value = '請輸入起點與訖點'
    return
  }
  if (overnightInputs.value.some((v) => !v.trim())) {
    error.value = '請輸入每一晚的住宿地點'
    return
  }
  if (dailyHoursInputs.value.some((h) => !h || h <= 0)) {
    error.value = '每天可用時數必須大於 0'
    return
  }

  error.value = ''
  loading.value = true
  try {
    const start = await resolveCoordinate(startText)
    const end = await resolveCoordinate(endText)
    const overnightStays = []
    for (const text of overnightInputs.value) {
      overnightStays.push(await resolveCoordinate(text.trim()))
    }
    const dailyAvailableMinutes = dailyHoursInputs.value.map((h) => Math.round(Number(h) * 60))

    setMultiDayInputs({ start, end, overnightStays, dailyAvailableMinutes })

    const anchors = [start, ...overnightStays, end]
    const candidates = await fetchNearbyAttractionsMultiAnchor(anchors)
    setMultiDayCandidates(candidates)

    router.push({ name: 'multiday-recommendations' })
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) {
      error.value = '找不到某個地點對應的座標，請確認地點名稱是否正確，或改用「緯度,經度」格式輸入。'
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
    <h1>多日行程規劃</h1>
    <p class="hint">輸入起訖點、天數、每晚住宿地點與每日可用時數，系統會依交通時間與您設定的停留時間排出每天的行程。</p>

    <form class="input-form" @submit.prevent="handleSubmit">
      <label for="start">起點</label>
      <input id="start" v-model="startInput" type="text" placeholder="例如：台北車站" :disabled="loading" />

      <label for="end">訖點</label>
      <input id="end" v-model="endInput" type="text" placeholder="例如：桃園機場" :disabled="loading" />

      <label for="days">天數</label>
      <input id="days" v-model.number="days" type="number" min="1" :disabled="loading" />

      <template v-if="overnightCount > 0">
        <div v-for="(_, index) in overnightInputs" :key="`stay-${index}`" class="field-group">
          <label :for="`stay-${index}`">第 {{ index + 1 }} 晚住宿地點</label>
          <input
            :id="`stay-${index}`"
            v-model="overnightInputs[index]"
            type="text"
            placeholder="例如：台中住宿飯店"
            :disabled="loading"
          />
        </div>
      </template>

      <div v-for="(_, index) in dailyHoursInputs" :key="`hours-${index}`" class="field-group">
        <label :for="`hours-${index}`">第 {{ index + 1 }} 天可用時數（小時）</label>
        <input
          :id="`hours-${index}`"
          v-model.number="dailyHoursInputs[index]"
          type="number"
          min="0.5"
          step="0.5"
          :disabled="loading"
        />
      </div>

      <p v-if="error" class="error">{{ error }}</p>
      <button type="submit" :disabled="loading">{{ loading ? '查詢中...' : '查詢附近景點/美食' }}</button>
    </form>
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
.input-form {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.field-group {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  margin-top: 0.25rem;
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
  margin-top: 1rem;
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
</style>
