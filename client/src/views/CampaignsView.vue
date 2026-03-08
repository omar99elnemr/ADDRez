<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import type { Campaign, PaginatedResponse } from '@/types'

const toast = useToast()
const campaigns = ref<Campaign[]>([])
const total = ref(0)
const page = ref(1)
const loading = ref(true)

const statusColors: Record<string, string> = {
  Draft: '#6b7280', Scheduled: '#3b82f6', Sending: '#f59e0b', Sent: '#10b981', Cancelled: '#ef4444'
}

async function loadCampaigns() {
  loading.value = true
  try {
    const { data } = await api.get<PaginatedResponse<Campaign>>('/campaigns', { params: { page: page.value, per_page: 15 } })
    campaigns.value = data.data
    total.value = data.total
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load campaigns', life: 3000 })
  } finally {
    loading.value = false
  }
}

onMounted(loadCampaigns)
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">Campaigns</h1>
    </div>

    <div v-if="loading" class="text-center py-12">
      <i class="pi pi-spin pi-spinner text-3xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
    </div>

    <div v-else-if="campaigns.length === 0" class="card text-center py-12">
      <i class="pi pi-megaphone text-4xl mb-3" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
      <p :style="{ color: 'var(--addrez-text-secondary)' }">No campaigns yet. Create your first campaign to reach your customers.</p>
    </div>

    <div v-else class="space-y-3">
      <div v-for="c in campaigns" :key="c.id" class="card">
        <div class="flex items-start justify-between">
          <div>
            <h3 class="font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ c.name }}</h3>
            <p v-if="c.subject" class="text-sm mt-0.5" :style="{ color: 'var(--addrez-text-secondary)' }">{{ c.subject }}</p>
          </div>
          <span class="text-xs px-2 py-1 rounded-full font-medium" :style="{ backgroundColor: (statusColors[c.status] || '#6b7280') + '20', color: statusColors[c.status] || '#6b7280' }">
            {{ c.status }}
          </span>
        </div>
        <div class="flex gap-6 mt-3 text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">
          <span><i class="pi pi-send mr-1"></i>{{ c.channel }}</span>
          <span><i class="pi pi-users mr-1"></i>{{ c.total_recipients }} recipients</span>
          <span><i class="pi pi-check mr-1"></i>{{ c.sent_count }} sent</span>
          <span v-if="c.sent_at"><i class="pi pi-calendar mr-1"></i>{{ new Date(c.sent_at).toLocaleDateString() }}</span>
        </div>
      </div>
    </div>
  </div>
</template>
