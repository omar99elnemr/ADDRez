<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import api from '@/services/api'
import type { DashboardData } from '@/types'

const auth = useAuthStore()
const data = ref<DashboardData | null>(null)
const loading = ref(true)
const todaySortBy = ref<'time' | 'guest' | 'covers' | 'status'>('time')
const todaySortDir = ref<'asc' | 'desc'>('asc')

const greeting = computed(() => {
  const h = new Date().getHours()
  if (h < 12) return 'Good morning'
  if (h < 18) return 'Good afternoon'
  return 'Good evening'
})

const firstName = computed(() => auth.user?.first_name || auth.user?.username || '')

onMounted(async () => {
  try {
    const res = await api.get('/dashboard')
    data.value = res.data
  } catch {
    // Will show empty state
  } finally {
    loading.value = false
  }
})

// ── Helpers ──
function fmtChange(v: number): string {
  if (v === 0) return '0%'
  return (v > 0 ? '+' : '') + v.toFixed(1) + '%'
}

function fmtCurrency(v: number): string {
  if (v >= 1000000) return (v / 1000000).toFixed(1) + 'M'
  if (v >= 1000) return (v / 1000).toFixed(1) + 'K'
  return v.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })
}

// ── Week chart max for scaling bars ──
const chartMax = computed(() => {
  if (!data.value) return 1
  return Math.max(1, ...data.value.week_chart.map(p => p.reservations + p.walk_ins))
})

// ── Donut chart ──
const donutSegments = computed(() => {
  if (!data.value) return []
  const d = data.value.status_distribution
  const items = [
    { label: 'Confirmed', value: d.confirmed, color: '#22c55e' },
    { label: 'Pending', value: d.pending, color: '#f59e0b' },
    { label: 'Seated', value: d.seated, color: '#8b5cf6' },
    { label: 'Checked In', value: d.checked_in, color: '#3b82f6' },
    { label: 'Completed', value: d.checked_out, color: '#6b7280' },
    { label: 'No Show', value: d.no_show, color: '#ef4444' },
    { label: 'Cancelled', value: d.cancelled, color: '#dc2626' },
  ].filter(s => s.value > 0)
  const total = items.reduce((s, i) => s + i.value, 0)
  if (total === 0) return []
  let cumulative = 0
  return items.map(item => {
    const pct = (item.value / total) * 100
    const start = cumulative
    cumulative += pct
    return { ...item, pct: Math.round(pct), start, end: cumulative }
  })
})

const donutTotal = computed(() => {
  if (!data.value) return 0
  const d = data.value.status_distribution
  return d.confirmed + d.pending + d.seated + d.checked_in + d.checked_out + d.no_show + d.cancelled
})

function donutArc(start: number, end: number): string {
  const r = 15.9155
  const circumference = 100
  const length = end - start
  return `${length} ${circumference - length}`
}

// ── Today reservations sorting ──
const sortedTodayReservations = computed(() => {
  if (!data.value) return []
  const list = [...data.value.today_reservations]
  const dir = todaySortDir.value === 'asc' ? 1 : -1
  list.sort((a, b) => {
    switch (todaySortBy.value) {
      case 'time': return a.time.localeCompare(b.time) * dir
      case 'guest': return (a.guest_name || '').localeCompare(b.guest_name || '') * dir
      case 'covers': return (a.covers - b.covers) * dir
      case 'status': return a.status.localeCompare(b.status) * dir
      default: return 0
    }
  })
  return list
})

function toggleSort(col: 'time' | 'guest' | 'covers' | 'status') {
  if (todaySortBy.value === col) {
    todaySortDir.value = todaySortDir.value === 'asc' ? 'desc' : 'asc'
  } else {
    todaySortBy.value = col
    todaySortDir.value = 'asc'
  }
}

function sortIcon(col: string): string {
  if (todaySortBy.value !== col) return 'pi pi-sort-alt'
  return todaySortDir.value === 'asc' ? 'pi pi-sort-amount-up' : 'pi pi-sort-amount-down'
}
</script>

<template>
  <div>
    <!-- ═══ Welcome + General Insights heading ═══ -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">{{ greeting }}, {{ firstName }}</h1>
        <p class="text-sm mt-0.5" :style="{ color: 'var(--addrez-text-secondary)' }">General Insights</p>
      </div>
    </div>

    <div v-if="loading" class="text-center py-16" :style="{ color: 'var(--addrez-text-secondary)' }">
      <i class="pi pi-spin pi-spinner text-3xl"></i>
      <p class="mt-3">Loading dashboard...</p>
    </div>

    <template v-else-if="data">

      <!-- ═══════ ROW 1: 5 KPI Cards ═══════ -->
      <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4 mb-6">
        <!-- Total Reservations Today -->
        <div class="card !p-4">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl flex items-center justify-center" style="background: rgba(212,168,83,0.15)">
              <i class="pi pi-calendar text-lg" style="color: var(--addrez-gold)"></i>
            </div>
            <div class="flex-1 min-w-0">
              <div class="text-2xl font-bold" style="color: var(--addrez-gold)">{{ data.kpis.today_reservations }}</div>
              <div class="text-[11px] font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Reservations Today</div>
            </div>
          </div>
          <div class="mt-2 text-xs font-semibold" :style="{ color: data.kpis.today_reservations_change >= 0 ? '#22c55e' : '#ef4444' }">
            {{ fmtChange(data.kpis.today_reservations_change) }}
            <span class="font-normal" :style="{ color: 'var(--addrez-text-secondary)' }">vs yesterday</span>
          </div>
        </div>

        <!-- Expected Visitors Today -->
        <div class="card !p-4">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl flex items-center justify-center" style="background: rgba(59,130,246,0.15)">
              <i class="pi pi-users text-lg" style="color: #3b82f6"></i>
            </div>
            <div class="flex-1 min-w-0">
              <div class="text-2xl font-bold" style="color: #3b82f6">{{ data.kpis.today_expected_visitors }}</div>
              <div class="text-[11px] font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Expected Visitors</div>
            </div>
          </div>
          <div class="mt-2 text-xs font-semibold" :style="{ color: data.kpis.today_expected_visitors_change >= 0 ? '#22c55e' : '#ef4444' }">
            {{ fmtChange(data.kpis.today_expected_visitors_change) }}
            <span class="font-normal" :style="{ color: 'var(--addrez-text-secondary)' }">vs yesterday</span>
          </div>
        </div>

        <!-- New Customers This Week -->
        <div class="card !p-4">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl flex items-center justify-center" style="background: rgba(16,185,129,0.15)">
              <i class="pi pi-user-plus text-lg" style="color: #10b981"></i>
            </div>
            <div class="flex-1 min-w-0">
              <div class="text-2xl font-bold" style="color: #10b981">{{ data.kpis.new_customers_this_week }}</div>
              <div class="text-[11px] font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">New Customers</div>
            </div>
          </div>
          <div class="mt-2 text-xs font-semibold" :style="{ color: data.kpis.new_customers_change >= 0 ? '#22c55e' : '#ef4444' }">
            {{ fmtChange(data.kpis.new_customers_change) }}
            <span class="font-normal" :style="{ color: 'var(--addrez-text-secondary)' }">this week</span>
          </div>
        </div>

        <!-- Today Revenue -->
        <div class="card !p-4">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl flex items-center justify-center" style="background: rgba(245,158,11,0.15)">
              <i class="pi pi-wallet text-lg" style="color: #f59e0b"></i>
            </div>
            <div class="flex-1 min-w-0">
              <div class="text-2xl font-bold" style="color: #f59e0b">{{ fmtCurrency(data.kpis.today_revenue) }}</div>
              <div class="text-[11px] font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Revenue Today</div>
            </div>
          </div>
          <div class="mt-2 text-xs font-semibold" :style="{ color: data.kpis.today_revenue_change >= 0 ? '#22c55e' : '#ef4444' }">
            {{ fmtChange(data.kpis.today_revenue_change) }}
            <span class="font-normal" :style="{ color: 'var(--addrez-text-secondary)' }">vs yesterday</span>
          </div>
        </div>

        <!-- Week Revenue -->
        <div class="card !p-4">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl flex items-center justify-center" style="background: rgba(139,92,246,0.15)">
              <i class="pi pi-chart-line text-lg" style="color: #8b5cf6"></i>
            </div>
            <div class="flex-1 min-w-0">
              <div class="text-2xl font-bold" style="color: #8b5cf6">{{ fmtCurrency(data.kpis.week_revenue) }}</div>
              <div class="text-[11px] font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Revenue This Week</div>
            </div>
          </div>
          <div class="mt-2 text-xs font-semibold" :style="{ color: data.kpis.week_revenue_change >= 0 ? '#22c55e' : '#ef4444' }">
            {{ fmtChange(data.kpis.week_revenue_change) }}
            <span class="font-normal" :style="{ color: 'var(--addrez-text-secondary)' }">vs prev week</span>
          </div>
        </div>
      </div>

      <!-- ═══════ ROW 2: Summary stat pills ═══════ -->
      <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3 mb-6">
        <div class="card !py-2 !px-3 text-center">
          <div class="text-lg font-bold" style="color: var(--addrez-gold)">{{ data.kpis.total_reservations }}</div>
          <div class="text-[10px] uppercase tracking-wider font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Total Reservations</div>
        </div>
        <div class="card !py-2 !px-3 text-center">
          <div class="text-lg font-bold" style="color: #f59e0b">{{ data.kpis.pending_confirmation }}</div>
          <div class="text-[10px] uppercase tracking-wider font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Pending</div>
        </div>
        <div class="card !py-2 !px-3 text-center">
          <div class="text-lg font-bold" style="color: #3b82f6">{{ data.kpis.checked_in }}</div>
          <div class="text-[10px] uppercase tracking-wider font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Checked In</div>
        </div>
        <div class="card !py-2 !px-3 text-center">
          <div class="text-lg font-bold" style="color: #8b5cf6">{{ data.kpis.seated }}</div>
          <div class="text-[10px] uppercase tracking-wider font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Seated</div>
        </div>
        <div class="card !py-2 !px-3 text-center">
          <div class="text-lg font-bold" style="color: #ef4444">{{ data.kpis.no_shows }}</div>
          <div class="text-[10px] uppercase tracking-wider font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">No Shows</div>
        </div>
        <div class="card !py-2 !px-3 text-center">
          <div class="text-lg font-bold" style="color: #dc2626">{{ data.kpis.total_cancelled }}</div>
          <div class="text-[10px] uppercase tracking-wider font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Cancelled</div>
        </div>
      </div>

      <!-- ═══════ ROW 3: Per-Outlet Breakdown ═══════ -->
      <div v-if="data.outlet_stats.length > 1" class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4 mb-6">
        <div v-for="o in data.outlet_stats" :key="o.id" class="card !p-4">
          <div class="flex items-center gap-2 mb-2">
            <div class="w-8 h-8 rounded-lg flex items-center justify-center" style="background: rgba(59,130,246,0.12)">
              <i class="pi pi-building text-sm" style="color: #3b82f6"></i>
            </div>
            <div class="text-xs font-semibold truncate" :style="{ color: 'var(--addrez-text-primary)' }">{{ o.name }}</div>
          </div>
          <div class="text-2xl font-bold" style="color: #3b82f6">{{ o.total_reservations }}</div>
          <div class="text-[10px] uppercase tracking-wider" :style="{ color: 'var(--addrez-text-secondary)' }">Reservations</div>
          <div class="mt-1.5 flex items-center gap-1">
            <span class="text-[10px] font-bold px-1.5 py-0.5 rounded-md" style="background: rgba(212,168,83,0.15); color: var(--addrez-gold)">Today {{ o.today_reservations }}</span>
          </div>
        </div>
      </div>

      <!-- ═══════ ROW 4: Chart + Donut + Top Customers ═══════ -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-6 mb-6">
        <!-- Reservations This Week Bar Chart -->
        <div class="card lg:col-span-5">
          <h3 class="text-sm font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">
            <i class="pi pi-chart-bar mr-2" style="color: var(--addrez-gold)"></i>Reservations This Week
          </h3>
          <div class="flex items-center gap-4 mb-3 text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">
            <span class="flex items-center gap-1"><span class="w-2.5 h-2.5 rounded-sm inline-block" style="background: #3b82f6"></span> Reservations</span>
            <span class="flex items-center gap-1"><span class="w-2.5 h-2.5 rounded-sm inline-block" style="background: #10b981"></span> Walk-ins</span>
          </div>
          <div class="flex items-end gap-2" style="height: 160px">
            <div v-for="p in data.week_chart" :key="p.date" class="flex-1 flex flex-col items-center gap-1">
              <div class="w-full flex gap-0.5 items-end" style="height: 130px">
                <div class="flex-1 rounded-t-sm transition-all" style="background: #3b82f6; min-height: 2px"
                  :style="{ height: ((p.reservations / chartMax) * 100) + '%' }"
                  :title="p.reservations + ' reservations'"></div>
                <div class="flex-1 rounded-t-sm transition-all" style="background: #10b981; min-height: 2px"
                  :style="{ height: ((p.walk_ins / chartMax) * 100) + '%' }"
                  :title="p.walk_ins + ' walk-ins'"></div>
              </div>
              <div class="text-[10px] font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">{{ p.day_label }}</div>
            </div>
          </div>
        </div>

        <!-- Status Distribution Donut -->
        <div class="card lg:col-span-3">
          <h3 class="text-sm font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">
            <i class="pi pi-chart-pie mr-2" style="color: #8b5cf6"></i>Monthly Status
          </h3>
          <div class="flex flex-col items-center">
            <div class="relative" style="width: 130px; height: 130px">
              <svg viewBox="0 0 36 36" class="w-full h-full" style="transform: rotate(-90deg)">
                <circle cx="18" cy="18" r="15.9155" fill="none" stroke-width="3.5" :stroke="'var(--addrez-border)'" />
                <circle v-for="(seg, idx) in donutSegments" :key="idx"
                  cx="18" cy="18" r="15.9155" fill="none"
                  :stroke="seg.color" stroke-width="3.5"
                  :stroke-dasharray="donutArc(seg.start, seg.end)"
                  :stroke-dashoffset="-(seg.start)"
                  stroke-linecap="round"
                  style="transition: stroke-dasharray 0.5s, stroke-dashoffset 0.5s" />
              </svg>
              <div class="absolute inset-0 flex flex-col items-center justify-center">
                <div class="text-xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">{{ donutTotal }}</div>
                <div class="text-[9px] uppercase tracking-wider" :style="{ color: 'var(--addrez-text-secondary)' }">Total</div>
              </div>
            </div>
            <div class="flex flex-wrap gap-x-3 gap-y-1 mt-3 justify-center">
              <div v-for="seg in donutSegments" :key="seg.label" class="flex items-center gap-1 text-[10px]">
                <span class="w-2 h-2 rounded-full inline-block" :style="{ background: seg.color }"></span>
                <span :style="{ color: 'var(--addrez-text-secondary)' }">{{ seg.label }} {{ seg.pct }}%</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Top Paid Customers -->
        <div class="card lg:col-span-4">
          <h3 class="text-sm font-semibold mb-3" :style="{ color: 'var(--addrez-text-primary)' }">
            <i class="pi pi-star-fill mr-2" style="color: var(--addrez-gold)"></i>Top Paid Customers
          </h3>
          <div v-if="data.top_customers.length === 0" class="py-6 text-center text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">
            No customer data yet
          </div>
          <table v-else class="w-full text-xs">
            <thead>
              <tr class="border-b" :style="{ borderColor: 'var(--addrez-border)' }">
                <th class="text-left py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Name</th>
                <th class="text-center py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Visits</th>
                <th class="text-center py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Category</th>
                <th class="text-right py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Spent</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in data.top_customers" :key="c.id" class="border-b" :style="{ borderColor: 'var(--addrez-border)' }">
                <td class="py-2.5 font-medium" :style="{ color: '#3b82f6' }">{{ c.name }}</td>
                <td class="py-2.5 text-center" :style="{ color: 'var(--addrez-text-primary)' }">{{ c.total_visits }}</td>
                <td class="py-2.5 text-center">
                  <span v-if="c.category" class="px-2 py-0.5 rounded-full text-[10px] font-semibold"
                    :style="{ backgroundColor: (c.category_color || '#6b7280') + '20', color: c.category_color || '#6b7280', border: '1px solid ' + (c.category_color || '#6b7280') + '40' }">
                    {{ c.category }}
                  </span>
                  <span v-else class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Not Set</span>
                </td>
                <td class="py-2.5 text-right font-semibold" style="color: var(--addrez-gold)">{{ fmtCurrency(c.total_spend) }} EGP</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- ═══════ ROW 5: Today Reservations Table ═══════ -->
      <div class="card mb-6">
        <div class="flex items-center justify-between mb-3">
          <h3 class="text-sm font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">
            <i class="pi pi-list mr-2" style="color: #3b82f6"></i>Today Reservations
          </h3>
          <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ data.today_reservations.length }} reservations</div>
        </div>
        <div v-if="data.today_reservations.length === 0" class="py-8 text-center text-sm" :style="{ color: 'var(--addrez-text-secondary)' }">
          <i class="pi pi-calendar-times text-2xl mb-2" style="display:block"></i>
          No reservations for today
        </div>
        <div v-else class="overflow-x-auto">
          <table class="w-full text-xs">
            <thead>
              <tr class="border-b" :style="{ borderColor: 'var(--addrez-border)' }">
                <th class="text-left py-2 font-semibold cursor-pointer select-none" :style="{ color: 'var(--addrez-text-secondary)' }" @click="toggleSort('time')">
                  Time <i :class="sortIcon('time')" class="text-[10px] ml-1"></i>
                </th>
                <th class="text-left py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Code</th>
                <th class="text-left py-2 font-semibold cursor-pointer select-none" :style="{ color: 'var(--addrez-text-secondary)' }" @click="toggleSort('guest')">
                  Guest <i :class="sortIcon('guest')" class="text-[10px] ml-1"></i>
                </th>
                <th class="text-left py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Mobile</th>
                <th class="text-center py-2 font-semibold cursor-pointer select-none" :style="{ color: 'var(--addrez-text-secondary)' }" @click="toggleSort('covers')">
                  Covers <i :class="sortIcon('covers')" class="text-[10px] ml-1"></i>
                </th>
                <th class="text-center py-2 font-semibold cursor-pointer select-none" :style="{ color: 'var(--addrez-text-secondary)' }" @click="toggleSort('status')">
                  Status <i :class="sortIcon('status')" class="text-[10px] ml-1"></i>
                </th>
                <th class="text-left py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Table</th>
                <th class="text-left py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Method</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="r in sortedTodayReservations" :key="r.id" class="border-b transition-colors"
                :style="{ borderColor: 'var(--addrez-border)' }"
                @mouseenter="($event.currentTarget as HTMLElement).style.backgroundColor = 'var(--addrez-bg-hover)'"
                @mouseleave="($event.currentTarget as HTMLElement).style.backgroundColor = 'transparent'">
                <td class="py-2.5 font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.time }}</td>
                <td class="py-2.5" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.confirmation_code || '—' }}</td>
                <td class="py-2.5 font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.guest_name || 'Walk-in' }}</td>
                <td class="py-2.5" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.guest_phone || '—' }}</td>
                <td class="py-2.5 text-center font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.covers }}</td>
                <td class="py-2.5 text-center">
                  <span class="px-2 py-0.5 rounded-full text-[10px] font-semibold" :style="{ backgroundColor: r.status_color + '18', color: r.status_color, border: '1px solid ' + r.status_color + '30' }">
                    {{ r.status }}
                  </span>
                </td>
                <td class="py-2.5" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.table_name || '—' }}</td>
                <td class="py-2.5" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.method }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- ═══════ ROW 6: Pending Confirmation + Future Unconfirmed ═══════ -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Pending Confirmation (today) -->
        <div class="card">
          <h3 class="text-sm font-semibold mb-3" :style="{ color: 'var(--addrez-text-primary)' }">
            <i class="pi pi-clock mr-2" style="color: #f59e0b"></i>Pending Confirmation
          </h3>
          <div v-if="data.unconfirmed_queue.length === 0" class="py-6 text-center text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">
            No pending reservations today
          </div>
          <div v-else class="space-y-2">
            <div v-for="r in data.unconfirmed_queue" :key="r.id"
              class="flex items-center justify-between p-3 rounded-lg"
              :style="{ backgroundColor: 'var(--addrez-bg-hover)' }">
              <div>
                <div class="font-medium text-sm" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.guest_name || 'Walk-in' }}</div>
                <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.time }} · {{ r.covers }} covers</div>
              </div>
              <div v-if="r.table_name" class="text-xs px-2 py-1 rounded" :style="{ backgroundColor: 'var(--addrez-bg-card)', color: 'var(--addrez-text-secondary)' }">
                {{ r.table_name }}
              </div>
            </div>
          </div>
        </div>

        <!-- Future Unconfirmed Reservations -->
        <div class="card">
          <h3 class="text-sm font-semibold mb-3" :style="{ color: 'var(--addrez-text-primary)' }">
            <i class="pi pi-forward mr-2" style="color: #dc2626"></i>Future Unconfirmed Reservations
          </h3>
          <div v-if="data.future_unconfirmed.length === 0" class="py-6 text-center text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">
            No future unconfirmed reservations
          </div>
          <div v-else class="overflow-x-auto">
            <table class="w-full text-xs">
              <thead>
                <tr class="border-b" :style="{ borderColor: 'var(--addrez-border)' }">
                  <th class="text-left py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Date</th>
                  <th class="text-left py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Slot</th>
                  <th class="text-left py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Guest</th>
                  <th class="text-center py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Covers</th>
                  <th class="text-left py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Type</th>
                  <th class="text-left py-2 font-semibold" :style="{ color: 'var(--addrez-text-secondary)' }">Created By</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="f in data.future_unconfirmed" :key="f.id" class="border-b" :style="{ borderColor: 'var(--addrez-border)' }">
                  <td class="py-2" :style="{ color: 'var(--addrez-text-primary)' }">{{ f.date }}</td>
                  <td class="py-2" :style="{ color: 'var(--addrez-text-secondary)' }">{{ f.time_slot_name || '—' }}</td>
                  <td class="py-2">
                    <div class="font-medium" style="color: #3b82f6">{{ f.guest_name || 'Walk-in' }}</div>
                    <div v-if="f.guest_phone" :style="{ color: 'var(--addrez-text-secondary)' }">{{ f.guest_phone }}</div>
                  </td>
                  <td class="py-2 text-center" :style="{ color: 'var(--addrez-text-primary)' }">{{ f.covers }}</td>
                  <td class="py-2" :style="{ color: 'var(--addrez-text-secondary)' }">{{ f.type }}</td>
                  <td class="py-2" :style="{ color: 'var(--addrez-text-secondary)' }">{{ f.created_by }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

    </template>

    <div v-else class="text-center py-12 card">
      <i class="pi pi-chart-bar text-4xl mb-3" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
      <p :style="{ color: 'var(--addrez-text-secondary)' }">Select an outlet to view dashboard data</p>
    </div>
  </div>
</template>
