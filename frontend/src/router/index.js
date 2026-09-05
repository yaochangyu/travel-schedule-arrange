import { createRouter, createWebHistory } from 'vue-router'
import TargetInputView from '../views/TargetInputView.vue'
import RecommendationListView from '../views/RecommendationListView.vue'
import ItineraryResultView from '../views/ItineraryResultView.vue'
import MultiDayInputView from '../views/MultiDayInputView.vue'
import MultiDayRecommendationListView from '../views/MultiDayRecommendationListView.vue'
import EditItineraryView from '../views/EditItineraryView.vue'
import MultiDayItineraryResultView from '../views/MultiDayItineraryResultView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'target-input', component: TargetInputView },
    { path: '/recommendations', name: 'recommendations', component: RecommendationListView },
    { path: '/itinerary', name: 'itinerary-result', component: ItineraryResultView },
    // 多日行程規劃：獨立流程，與上方單日流程互不影響。
    { path: '/multiday', name: 'multiday-input', component: MultiDayInputView },
    {
      path: '/multiday/recommendations',
      name: 'multiday-recommendations',
      component: MultiDayRecommendationListView,
    },
    { path: '/multiday/edit', name: 'multiday-edit', component: EditItineraryView },
    { path: '/multiday/result', name: 'multiday-result', component: MultiDayItineraryResultView },
  ],
})

export default router
