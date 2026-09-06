<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { planStore, setSelectedAttractionIds, setSortedItinerary } from '../stores/planStore'
import { planItinerary } from '../api/client'

const router = useRouter()

const CATEGORY_LABELS = {
  ScenicSpot: '景點',
  Restaurant: '美食',
}

// 單次排序請求允許的候選數量上限，需與後端 ItineraryController.MaxCandidateCount 保持一致。
// 排序演算法對每個候選點都會呼叫外部交通資訊 API，呼叫次數隨候選數量呈平方成長，
// 曾發生使用者一次選取上百筆候選、導致長時間無法完成的情況。
const MAX_CANDIDATE_COUNT = 20

// 後端景點/美食未落地資料庫，Id 皆為 0，改用 sourceId（找不到時退回索引）作為前端唯一鍵。
function keyOf(attraction, index) {
  return attraction.sourceId ?? `idx-${index}`
}

const attractions = computed(() => planStore.attractions)
// 預設不勾選任何候選，避免使用者在景點眾多時不慎一次送出過量候選做排序。
const selectedKeys = ref(new Set())
const isOverLimit = computed(() => selectedKeys.value.size > MAX_CANDIDATE_COUNT)

const loading = ref(false)
const error = ref('')

function categoryLabel(category) {
  return CATEGORY_LABELS[category] || category
}

function toggle(key) {
  if (selectedKeys.value.has(key)) {
    selectedKeys.value.delete(key)
  } else {
    selectedKeys.value.add(key)
  }
  selectedKeys.value = new Set(selectedKeys.value)
}

function handleBack() {
  router.push({ name: 'target-input' })
}

async function handleGeneratePlan() {
  const selected = attractions.value.filter((a, i) => selectedKeys.value.has(keyOf(a, i)))
  if (selected.length === 0 || selected.length > MAX_CANDIDATE_COUNT || !planStore.targetCoordinate) {
    return
  }

  error.value = ''
  loading.value = true
  try {
    setSelectedAttractionIds(selected.map((a, i) => keyOf(a, i)))
    const sorted = await planItinerary(planStore.targetCoordinate, selected)
    setSortedItinerary(sorted)
    router.push({ name: 'itinerary-result' })
  } catch (err) {
    error.value = `產生行程排序失敗：${err.message || '請稍後再試'}`
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="page">
    <h1>附近景點/美食推薦</h1>
    <p class="hint">目標地點：<strong>{{ planStore.targetLocation || '（尚未輸入）' }}</strong></p>
    <p class="note">資料來源：TDX 觀光資訊資料庫（即時查詢）。</p>

    <p v-if="attractions.length === 0" class="empty">
      查無附近景點/美食，請回到上一步重新輸入目標地點。
    </p>

    <ul v-else class="card-list">
      <li v-for="(attraction, index) in attractions" :key="keyOf(attraction, index)" class="card">
        <label class="card-label">
          <input
            type="checkbox"
            :checked="selectedKeys.has(keyOf(attraction, index))"
            @change="toggle(keyOf(attraction, index))"
          />
          <div class="card-body">
            <div class="card-title">
              <span>{{ attraction.name }}</span>
              <span class="badge">{{ categoryLabel(attraction.category) }}</span>
            </div>
            <div class="card-address">{{ attraction.address || '（無地址資料）' }}</div>
          </div>
        </label>
      </li>
    </ul>

    <p class="count-hint" :class="{ 'count-hint--over': isOverLimit }">
      已勾選 {{ selectedKeys.size }} / {{ MAX_CANDIDATE_COUNT }} 筆
    </p>
    <p v-if="isOverLimit" class="error">
      已超過單次排序上限（{{ MAX_CANDIDATE_COUNT }} 筆），請取消勾選部分景點後再試。
    </p>
    <p v-if="error" class="error">{{ error }}</p>

    <div class="actions">
      <button class="secondary" type="button" @click="handleBack" :disabled="loading">上一步</button>
      <button
        type="button"
        :disabled="selectedKeys.size === 0 || isOverLimit || loading"
        @click="handleGeneratePlan"
      >
        {{ loading ? '排序中...' : '產生行程排序' }}
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
  margin-bottom: 0.25rem;
}
.note {
  font-size: 0.85rem;
  color: #999;
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
}
.card-label {
  display: flex;
  gap: 0.75rem;
  align-items: flex-start;
  cursor: pointer;
}
.card-body {
  flex: 1;
}
.card-title {
  display: flex;
  justify-content: space-between;
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
.empty {
  color: #999;
}
.count-hint {
  margin-top: 1rem;
  margin-bottom: 0;
  font-size: 0.85rem;
  color: #666;
}
.count-hint--over {
  color: #d33;
  font-weight: 600;
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
