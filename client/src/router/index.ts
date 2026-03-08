import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/LoginView.vue'),
    meta: { guest: true }
  },
  {
    path: '/',
    component: () => import('@/layouts/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      { path: '', redirect: '/dashboard' },
      { path: 'dashboard', name: 'dashboard', component: () => import('@/views/DashboardView.vue'), meta: { permission: 'dashboard.view' } },
      { path: 'reservations', name: 'reservations', component: () => import('@/views/ReservationsView.vue'), meta: { permission: 'reservations.view' } },
      { path: 'customers', name: 'customers', component: () => import('@/views/CustomersView.vue'), meta: { permission: 'customers.view' } },
      { path: 'customers/:id', name: 'customer-detail', component: () => import('@/views/CustomerDetailView.vue'), meta: { permission: 'customers.view' } },
      
      { path: 'time-slots', name: 'time-slots', component: () => import('@/views/TimeSlotsView.vue'), meta: { permission: 'time_slots.view' } },
      { path: 'campaigns', name: 'campaigns', component: () => import('@/views/CampaignsView.vue'), meta: { permission: 'campaigns.view' } },
      { path: 'reports', name: 'reports', component: () => import('@/views/ReportsView.vue'), meta: { permission: 'reports.view' } },
      { path: 'gate-checker', name: 'gate-checker', component: () => import('@/views/GateCheckerView.vue'), meta: { permission: 'reservations.view' } },
      {
        path: 'settings',
        name: 'settings',
        component: () => import('@/views/settings/SettingsView.vue'),
        children: [
          { path: '', redirect: '/settings/company' },
          { path: 'company', name: 'settings-company', component: () => import('@/views/settings/CompanySettings.vue') },
          { path: 'outlets', name: 'settings-outlets', component: () => import('@/views/settings/OutletsSettings.vue') },
          { path: 'users', name: 'settings-users', component: () => import('@/views/settings/UsersSettings.vue') },
          { path: 'roles', name: 'settings-roles', component: () => import('@/views/settings/RolesSettings.vue') },
          { path: 'tags-categories', name: 'settings-tags-categories', component: () => import('@/views/settings/TagsCategoriesSettings.vue') },
          { path: 'tags', redirect: '/settings/tags-categories' },
          { path: 'categories', redirect: '/settings/tags-categories' },
          { path: 'configurations', name: 'settings-configurations', component: () => import('@/views/settings/ConfigurationsSettings.vue') },
          { path: 'templates', name: 'settings-templates', component: () => import('@/views/settings/TemplatesSettings.vue') },
          { path: 'terms', name: 'settings-terms', component: () => import('@/views/settings/TermsSettings.vue') },
          { path: 'floor-plan-editor', name: 'settings-floor-plan', component: () => import('@/views/settings/FloorPlanEditor.vue') },
        ]
      },
    ]
  },
  {
    path: '/book',
    name: 'public-booking',
    component: () => import('@/views/PublicBookingView.vue'),
    meta: { guest: true, public: true }
  },
  { path: '/:pathMatch(.*)*', redirect: '/dashboard' }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, _from, next) => {
  const token = localStorage.getItem('addrez_token')

  if (to.meta.requiresAuth && !token) {
    return next('/login')
  }

  // Public pages (like /book) should always be accessible
  if (to.meta.public) {
    return next()
  }

  if (to.meta.guest && token) {
    return next('/dashboard')
  }

  next()
})

export default router
