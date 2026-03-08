<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import type { Outlet } from '@/types'

const toast = useToast()
const outlets = ref<Outlet[]>([])
const loading = ref(true)

async function load() {
  loading.value = true
  try {
    const { data } = await api.get<Outlet[]>('/settings/outlets')
    outlets.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load outlets', life: 3000 })
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div v-if="loading" class="text-center py-8">
    <i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
  </div>
  <div v-else class="space-y-3">
    <div v-for="b in outlets" :key="b.id" class="card">
      <div class="flex items-start justify-between">
        <div>
          <h3 class="font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ b.name }}</h3>
          <p v-if="b.address" class="text-sm mt-0.5" :style="{ color: 'var(--addrez-text-secondary)' }">{{ b.address }}</p>
        </div>
        <span class="text-xs px-2 py-1 rounded-full font-medium" :style="b.is_active ? { backgroundColor: '#10b98120', color: '#10b981' } : { backgroundColor: '#6b728020', color: '#6b7280' }">
          {{ b.is_active ? 'Active' : 'Inactive' }}
        </span>
      </div>
      <div class="flex gap-6 mt-3 text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">
        <span v-if="b.phone"><i class="pi pi-phone mr-1"></i>{{ b.phone }}</span>
        <span v-if="b.email"><i class="pi pi-envelope mr-1"></i>{{ b.email }}</span>
        <span><i class="pi pi-clock mr-1"></i>{{ b.default_turn_time_minutes }}min turn</span>
        <span><i class="pi pi-stopwatch mr-1"></i>{{ b.default_grace_period_minutes }}min grace</span>
      </div>
    </div>
    <div v-if="outlets.length === 0" class="text-center py-8" :style="{ color: 'var(--addrez-text-secondary)' }">No outlets configured</div>
  </div>
</template>
