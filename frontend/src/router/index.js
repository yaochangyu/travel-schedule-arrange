import { createRouter, createWebHistory } from 'vue-router'
import TargetInputView from '../views/TargetInputView.vue'
import RecommendationListView from '../views/RecommendationListView.vue'
import ItineraryResultView from '../views/ItineraryResultView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'target-input', component: TargetInputView },
    { path: '/recommendations', name: 'recommendations', component: RecommendationListView },
    { path: '/itinerary', name: 'itinerary-result', component: ItineraryResultView },
  ],
})

export default router
