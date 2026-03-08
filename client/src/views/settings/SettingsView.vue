<script setup lang="ts">
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { computed } from 'vue'

const route = useRoute()
const auth = useAuthStore()

const tabs = computed(() => [
  { label: 'Company', to: '/settings/company', permission: 'settings.company' },
  { label: 'Outlets', to: '/settings/outlets', permission: 'settings.branches' },
  { label: 'Users', to: '/settings/users', permission: 'settings.users' },
  { label: 'Roles', to: '/settings/roles', permission: 'settings.roles' },
  { label: 'Tags & Categories', to: '/settings/tags-categories', permission: 'settings.tags' },
  { label: 'Configurations', to: '/settings/configurations', permission: 'settings.general' },
  { label: 'Templates', to: '/settings/templates', permission: 'settings.templates' },
  { label: 'Terms', to: '/settings/terms', permission: 'settings.terms' },
  { label: 'Floor Plan', to: '/settings/floor-plan-editor', permission: 'floor_plan.edit' },
].filter(t => auth.hasPermission(t.permission)))

function isActive(path: string) {
  return route.path === path
}
</script>

<template>
  <div>
    <h1 class="text-2xl font-bold mb-6" :style="{ color: 'var(--addrez-text-primary)' }">Settings</h1>

    <!-- Tabs -->
    <div class="flex gap-1 mb-6 overflow-x-auto pb-1">
      <router-link
        v-for="tab in tabs" :key="tab.to"
        :to="tab.to"
        class="px-4 py-2 rounded-lg text-sm font-medium whitespace-nowrap no-underline transition-colors"
        :style="isActive(tab.to)
          ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' }
          : { backgroundColor: 'var(--addrez-bg-card)', color: 'var(--addrez-text-secondary)' }"
      >
        {{ tab.label }}
      </router-link>
    </div>

    <router-view />
  </div>
</template>
