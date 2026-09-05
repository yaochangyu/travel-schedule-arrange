<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import {
  multiDayStore,
  setStayDuration,
  setMultiDayPlanResult,
  candidateKeyOf,
} from '../stores/multiDayStore'
import { planMultiDayItinerary, ApiError } from '../api/client'

const router = useRouter()

const DEFAULT_STAY_MINUTES = 60

const CATEGORY_LABELS = {
  ScenicSpot: '景點',
  Restaurant: '美食',
}

const selectedCandidates = computed(() =>
  multiDayStore.candidates
    .map((c, index) => ({ candidate: c, key: candidateKeyOf(c, index) }))
    .filter(({ key }) => multiDayStore.selectedCandidateKeys.includes(key)),
)

const durations = ref(
  Object.fromEntries(
    selectedCandidates.value.map(({ key }) => [
      key,
      multiDayStore.stayDurations[key] ?? DEFAULT_STAY_MINUTES,
    ]),
  ),
)

const error = ref('')
const loading = ref(false)

function categoryLabel(category) {
  return CATEGORY_LABELS[category] || category
}

function handleBack() {
  router.push({ name: 'multiday-recommendations' })
}

async function handleSubmit() {
  if (selectedCandidates.value.some(({ key }) => !durations.value[key] || durations.value[key] <= 0)) {
    error.value = '每個景點的停留時間必須大於 0 分鐘'
    return
  }

  error.value = ''
  loading.value = true
  try {
    for (const { key } of selectedCandidates.value) {
      setStayDuration(key, durations.value[key])
    }

    const candidates = selectedCandidates.value.map(({ candidate, key }) => ({
      ...candidate,
      stayDurationMinutes: durations.value[key],
    }))

    const result = await planMultiDayItinerary({
      start: multiDayStore.start,
      end: multiDayStore.end,
      overnightStays: multiDayStore.overnightStays,
      dailyAvailableMinutes: multiDayStore.dailyAvailableMinutes,
      candidates,
    })
    setMultiDayPlanResult(result)
    router.push({ name: 'multiday-result' })
  } catch (err) {
    error.value = `產生行程排序失敗：${err instanceof ApiError ? err.message : err.message || '請稍後再試'}`
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="page">
    <h1>編輯行程 — 設定停留時間</h1>
    <p class="hint">請為每個已選景點設定預計停留時間（分鐘），可自行調整。</p>

    <ul v-if="selectedCandidates.length > 0" class="card-list">
      <li v-for="{ candidate, key } in selectedCandidates" :key="key" class="card">
        <div class="card-body">
          <div class="card-title">
            <span>{{ candidate.name }}</span>
            <span class="badge">{{ categoryLabel(candidate.category) }}</span>
          </div>
          <div class="card-address">{{ candidate.address || '（無地址資料）' }}</div>
        </div>
        <div class="duration-field">
          <input v-model.number="durations[key]" type="number" min="1" step="5" :disabled="loading" />
          <span>分鐘</span>
        </div>
      </li>
    </ul>
    <p v-else class="empty">尚未選擇任何景點，請回到上一步選擇。</p>

    <p v-if="error" class="error">{{ error }}</p>

    <div class="actions">
      <button class="secondary" type="button" @click="handleBack" :disabled="loading">上一步</button>
      <button type="button" :disabled="selectedCandidates.length === 0 || loading" @click="handleSubmit">
        {{ loading ? '排序中...' : '產生多日行程' }}
      </button>
    </div>
  </section>
</template>

<style scoped>
.page {
  max-width: 560px;
  margin: 0 auto;
  padding: 2rem 1rem;
  text-align: left;
}
.hint {
  color: #666;
  margin-bottom: 1.5rem;
}
.card-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  max-height: 60vh;
  overflow-y: auto;
}
.card {
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 0.9rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
}
.card-body {
  flex: 1;
}
.card-title {
  display: flex;
  justify-content: space-between;
  gap: 0.5rem;
  font-weight: 600;
}
.badge {
  font-size: 0.75rem;
  background: #eef7f2;
  color: #2f8a5f;
  border-radius: 999px;
  padding: 0.1rem 0.6rem;
}
.card-address {
  color: #666;
  font-size: 0.85rem;
  margin-top: 0.25rem;
}
.duration-field {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  flex-shrink: 0;
}
.duration-field input {
  width: 4.5rem;
  padding: 0.4rem;
  font-size: 1rem;
  border: 1px solid #ccc;
  border-radius: 6px;
}
.empty {
  color: #999;
}
.error {
  color: #d33;
}
.actions {
  margin-top: 1.5rem;
  display: flex;
  gap: 0.75rem;
}
button {
  padding: 0.7rem 1.2rem;
  font-size: 1rem;
  border: none;
  border-radius: 6px;
  background-color: #42b883;
  color: white;
  cursor: pointer;
}
button:disabled {
  background-color: #aaa;
  cursor: not-allowed;
}
button.secondary {
  background-color: #eee;
  color: #333;
}
</style>
