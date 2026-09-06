<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { multiDayStore, setSelectedCandidateKeys, candidateKeyOf } from '../stores/multiDayStore'

const router = useRouter()

const CATEGORY_LABELS = {
  ScenicSpot: '景點',
  Restaurant: '美食',
}

// 單次排序請求允許的候選數量上限，需與後端 ItineraryController.MaxCandidateCount 保持一致。
const MAX_CANDIDATE_COUNT = 20

const candidates = computed(() => multiDayStore.candidates)
const selectedKeys = ref(new Set(multiDayStore.selectedCandidateKeys))
const isOverLimit = computed(() => selectedKeys.value.size > MAX_CANDIDATE_COUNT)

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
  router.push({ name: 'multiday-input' })
}

function handleNext() {
  if (selectedKeys.value.size === 0 || isOverLimit.value) {
    return
  }
  setSelectedCandidateKeys([...selectedKeys.value])
  router.push({ name: 'multiday-edit' })
}
</script>

<template>
  <section class="page">
    <h1>附近景點/美食推薦（多日）</h1>
    <p class="note">
      資料來源：TDX 觀光資訊資料庫（依起點、每晚住宿、訖點合併查詢後去重）。
    </p>

    <p v-if="candidates.length === 0" class="empty">
      查無附近景點/美食，請回到上一步重新輸入行程資訊。
    </p>

    <ul v-else class="card-list">
      <li v-for="(candidate, index) in candidates" :key="candidateKeyOf(candidate, index)" class="card">
        <label class="card-label">
          <input
            type="checkbox"
            :checked="selectedKeys.has(candidateKeyOf(candidate, index))"
            @change="toggle(candidateKeyOf(candidate, index))"
          />
          <div class="card-body">
            <div class="card-title">
              <span>{{ candidate.name }}</span>
              <span class="badge">{{ categoryLabel(candidate.category) }}</span>
            </div>
            <div class="card-address">{{ candidate.address || '（無地址資料）' }}</div>
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

    <div class="actions">
      <button class="secondary" type="button" @click="handleBack">上一步</button>
      <button type="button" :disabled="selectedKeys.size === 0 || isOverLimit" @click="handleNext">
        下一步：設定停留時間
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
