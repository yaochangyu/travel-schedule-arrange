<script setup>
import { useRouter } from 'vue-router'
import { planStore } from '../stores/planStore'

const router = useRouter()

const CATEGORY_LABELS = {
  ScenicSpot: '景點',
  Restaurant: '美食',
}

function categoryLabel(category) {
  return CATEGORY_LABELS[category] || category
}

function handleRestart() {
  router.push({ name: 'target-input' })
}
</script>

<template>
  <section class="page">
    <h1>行程排序結果</h1>
    <p class="hint">目標地點：<strong>{{ planStore.targetLocation || '（尚未輸入）' }}</strong></p>
    <p class="note">
      排序邏輯：以起點為出發點，依貪婪最近鄰演算法計算，距離為 Google Distance Matrix API 回傳的實際開車距離
      （若 Google API 暫時無法使用，則自動改用直線距離近似值）。
    </p>

    <ol v-if="planStore.sortedItinerary.length > 0" class="itinerary-list">
      <li
        v-for="item in planStore.sortedItinerary"
        :key="item.sourceId ?? `${item.name}-${item.order}`"
        class="itinerary-item"
      >
        <div class="order-badge">{{ item.order }}</div>
        <div class="item-body">
          <div class="item-title">
            <span>{{ item.name }}</span>
            <span class="badge">{{ categoryLabel(item.category) }}</span>
          </div>
          <div class="item-address">{{ item.address || '（無地址資料）' }}</div>
          <div class="item-distance">距離前一站約 {{ item.distanceFromPreviousKm }} 公里</div>
        </div>
      </li>
    </ol>
    <p v-else class="empty">尚未產生行程，請先回到推薦清單頁選擇景點/美食。</p>

    <div class="actions">
      <button type="button" @click="handleRestart">重新規劃行程</button>
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
.itinerary-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}
.itinerary-item {
  display: flex;
  gap: 0.9rem;
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 0.9rem;
  align-items: flex-start;
}
.order-badge {
  flex-shrink: 0;
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  background-color: #42b883;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
}
.item-body {
  flex: 1;
}
.item-title {
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
.item-address {
  color: #666;
  font-size: 0.85rem;
  margin-top: 0.25rem;
}
.item-distance {
  color: #999;
  font-size: 0.8rem;
  margin-top: 0.25rem;
}
.empty {
  color: #999;
}
.actions {
  margin-top: 1.5rem;
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
</style>
