import { createRouter, createWebHistory } from 'vue-router'
import RunsPage from './pages/RunsPage.vue'
import RunPage from './pages/RunPage.vue'
import CasePage from './pages/CasePage.vue'
export default createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/runs' },
    { path: '/runs', component: RunsPage },
    { path: '/runs/:run', component: RunPage },
    { path: '/runs/:run/cases/:case', component: CasePage },
  ],
})
