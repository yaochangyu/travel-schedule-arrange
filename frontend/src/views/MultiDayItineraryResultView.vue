<script setup>
import { useRouter } from 'vue-router'
import { multiDayStore } from '../stores/multiDayStore'

const router = useRouter()

const CATEGORY_LABELS = {
  ScenicSpot: '景點',
  Restaurant: '美食',
}

function categoryLabel(category) {
  return CATEGORY_LABELS[category] || category
}

function handleRestart() {
  router.push({ name: 'multiday-input' })
}
</script>

<template>
  <section class="page">
    <h1>多日行程排序結果</h1>
    <p class="note">
      排序邏輯：每天以當天出發錨點（起點／前一晚住宿）為起點，依貪婪最近鄰演算法排入候選景點，
      超過當日可用時數的候選會留給下一天，全部天數處理完後仍未排入的即為候補清單。
    </p>

    <div v-if="multiDayStore.planResult" class="day-list">
      <section v-for="day in multiDayStore.planResult.days" :key="day.dayNumber" class="day-block">
        <h2>第 {{ day.dayNumber }} 天</h2>

        <p v-if="day.stops.length === 0" class="empty-day">
          當天可用時數不足以排入任何景點（純移動日）。
        </p>

        <ol v-else class="itinerary-list">
          <li v-for="stop in day.stops" :key="`${day.dayNumber}-${stop.order}`" class="itinerary-item">
            <div class="order-badge">{{ stop.order }}</div>
            <div class="item-body">
              <div class="item-title">
                <span>{{ stop.name }}</span>
                <span class="badge">{{ categoryLabel(stop.category) }}</span>
              </div>
              <div class="item-address">{{ stop.address || '（無地址資料）' }}</div>
              <div class="item-meta">
                距離前一站約 {{ stop.distanceFromPreviousKm }} 公里（約 {{ stop.travelMinutesFromPrevious }} 分鐘），
                停留 {{ stop.stayDurationMinutes }} 分鐘
              </div>
            </div>
          </li>
        </ol>

        <p class="final-leg">
          最後一站前往{{ day.dayNumber === multiDayStore.planResult.days.length ? '訖點' : '當晚住宿' }}：
          約 {{ day.finalLegDistanceKm }} 公里（約 {{ day.finalLegDurationMinutes }} 分鐘）
        </p>
      </section>

      <section v-if="multiDayStore.planResult.waitlist.length > 0" class="waitlist-block">
        <h2>候補清單</h2>
        <p class="hint">以下景點因所有天數時間皆已用滿，未排入行程：</p>
        <ul class="waitlist">
          <li v-for="item in multiDayStore.planResult.waitlist" :key="item.name">
            {{ item.name }}（{{ categoryLabel(item.category) }}）
          </li>
        </ul>
      </section>
    </div>
    <p v-else class="empty">尚未產生行程，請先回到輸入頁重新規劃。</p>

    <div class="actions">
      <button type="button" @click="handleRestart">重新規劃多日行程</button>
    </div>
  </section>
</template>

<style scoped>
.page {
  max-width: 640px;
  margin: 0 auto;
  padding: 2rem 1rem;
  text-align: left;
}
.note {
  font-size: 0.85rem;
  color: #999;
  margin-bottom: 1.5rem;
}
.day-list {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}
.day-block {
  border: 1px solid #ddd;
  border-radius: 10px;
  padding: 1rem;
}
.day-block h2 {
  margin-top: 0;
}
.empty-day {
  color: #999;
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
  border: 1px solid #eee;
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
.item-meta {
  color: #999;
  font-size: 0.8rem;
  margin-top: 0.25rem;
}
.final-leg {
  margin-top: 0.75rem;
  margin-bottom: 0;
  font-size: 0.85rem;
  color: #666;
}
.waitlist-block {
  border: 1px dashed #ccc;
  border-radius: 10px;
  padding: 1rem;
}
.waitlist-block h2 {
  margin-top: 0;
}
.waitlist {
  margin: 0;
  padding-left: 1.2rem;
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
