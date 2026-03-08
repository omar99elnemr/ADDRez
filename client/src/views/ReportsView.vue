<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'

const toast = useToast()
const loading = ref(true)
const summary = ref<any>(null)
const dateFrom = ref('')
const dateTo = ref('')

async function loadSummary() {
  loading.value = true
  try {
    const params: Record<string, string> = {}
    if (dateFrom.value) params.dateFrom = dateFrom.value
    if (dateTo.value) params.dateTo = dateTo.value
    const { data } = await api.get('/reports/summary', { params })
    summary.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load report', life: 3000 })
  } finally {
    loading.value = false
  }
}

onMounted(loadSummary)
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">Reports</h1>
    </div>

    <!-- Date filter -->
    <div class="card mb-4">
      <div class="flex gap-3 items-center">
        <label class="text-sm" :style="{ color: 'var(--addrez-text-secondary)' }">From:</label>
        <input v-model="dateFrom" type="date" class="px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
        <label class="text-sm" :style="{ color: 'var(--addrez-text-secondary)' }">To:</label>
        <input v-model="dateTo" type="date" class="px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
        <button class="btn-gold text-sm" @click="loadSummary">Apply</button>
      </div>
    </div>

    <div v-if="loading" class="text-center py-12">
      <i class="pi pi-spin pi-spinner text-3xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
    </div>

    <template v-else-if="summary">
      <!-- KPIs -->
      <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4 mb-6">
        <div class="card text-center">
          <div class="text-2xl font-bold" style="color: var(--addrez-gold)">{{ summary.kpis.totalReservations }}</div>
          <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">Total Reservations</div>
        </div>
        <div class="card text-center">
          <div class="text-2xl font-bold" style="color: var(--addrez-gold)">{{ summary.kpis.totalCovers }}</div>
          <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">Total Covers</div>
        </div>
        <div class="card text-center">
          <div class="text-2xl font-bold" style="color: #10b981">{{ summary.kpis.confirmed }}</div>
          <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">Confirmed+</div>
        </div>
        <div class="card text-center">
          <div class="text-2xl font-bold" style="color: #ef4444">{{ summary.kpis.noShows }}</div>
          <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">No Shows</div>
        </div>
        <div class="card text-center">
          <div class="text-2xl font-bold" style="color: #f59e0b">{{ summary.kpis.cancelled }}</div>
          <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">Cancelled</div>
        </div>
        <div class="card text-center">
          <div class="text-2xl font-bold" style="color: var(--addrez-gold)">{{ summary.kpis.avgCovers }}</div>
          <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">Avg Covers</div>
        </div>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Status breakdown -->
        <div class="card">
          <h3 class="text-lg font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">Status Breakdown</h3>
          <div class="space-y-2">
            <div v-for="s in summary.status_breakdown" :key="s.status" class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <div class="w-3 h-3 rounded-full" :style="{ backgroundColor: s.color }"></div>
                <span class="text-sm" :style="{ color: 'var(--addrez-text-primary)' }">{{ s.status }}</span>
              </div>
              <span class="text-sm font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ s.count }}</span>
            </div>
          </div>
        </div>

        <!-- Type breakdown -->
        <div class="card">
          <h3 class="text-lg font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">Reservation Types</h3>
          <div class="space-y-2">
            <div v-for="t in summary.type_breakdown" :key="t.type" class="flex items-center justify-between">
              <span class="text-sm" :style="{ color: 'var(--addrez-text-primary)' }">{{ t.type }}</span>
              <span class="text-sm font-medium" style="color: var(--addrez-gold)">{{ t.count }}</span>
            </div>
          </div>
        </div>
      </div>
    </template>

    <div v-else class="card text-center py-12">
      <i class="pi pi-chart-bar text-4xl mb-3" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
      <p :style="{ color: 'var(--addrez-text-secondary)' }">Select an outlet to view reports</p>
    </div>
  </div>
</template>
