<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import type { Customer, PaginatedResponse, BirthdayEntry, BlacklistedCustomer } from '@/types'

const router = useRouter()
const toast = useToast()

const customers = ref<Customer[]>([])
const total = ref(0)
const page = ref(1)
const perPage = ref(15)
const loading = ref(true)
const search = ref('')
const activeTab = ref<'list' | 'birthdays' | 'blacklist'>('list')
const birthdays = ref<BirthdayEntry[]>([])
const birthdayMonth = ref(new Date().getMonth() + 1)
const birthdayYear = ref(new Date().getFullYear())
const blacklisted = ref<BlacklistedCustomer[]>([])

// Filters
const selectedCategoryId = ref<number | null>(null)
const categories = ref<{ id: number; name: string; color: string }[]>([])
const allTags = ref<{ id: number; name: string; color: string; type: string }[]>([])
const selectedTagIds = ref<number[]>([])
const showTagFilter = ref(false)

const statusColors: Record<string, string> = {
  Active: '#10b981', VIP: '#d4a853', Blacklisted: '#ef4444', Inactive: '#6b7280'
}

const monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December']

const customerTags = computed(() => allTags.value.filter(t => t.type === 'Customer'))

async function loadFilters() {
  try {
    const [catRes, tagRes] = await Promise.all([
      api.get('/settings/categories'),
      api.get('/settings/tags', { params: { type: 'Customer' } })
    ])
    categories.value = catRes.data
    allTags.value = tagRes.data
  } catch { /* filters are optional */ }
}

function toggleTagFilter(tagId: number) {
  const idx = selectedTagIds.value.indexOf(tagId)
  if (idx >= 0) selectedTagIds.value.splice(idx, 1)
  else selectedTagIds.value.push(tagId)
}

function applyTagFilter() {
  showTagFilter.value = false
  page.value = 1
  loadCustomers()
}

function clearFilters() {
  selectedCategoryId.value = null
  selectedTagIds.value = []
  search.value = ''
  page.value = 1
  loadCustomers()
}

async function loadCustomers() {
  loading.value = true
  try {
    const params: Record<string, any> = { page: page.value, per_page: perPage.value }
    if (search.value) params.search = search.value
    if (selectedCategoryId.value) params.categoryId = selectedCategoryId.value
    if (selectedTagIds.value.length > 0) params.tagIds = selectedTagIds.value
    const { data } = await api.get<PaginatedResponse<Customer>>('/customers', { params })
    customers.value = data.data
    total.value = data.total
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load customers', life: 3000 })
  } finally {
    loading.value = false
  }
}

async function loadBirthdays() {
  try {
    const { data } = await api.get<{ year: number; month: number; birthdays: BirthdayEntry[] }>('/customers/birthdays', { params: { month: birthdayMonth.value, year: birthdayYear.value } })
    birthdays.value = data.birthdays
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load birthdays', life: 3000 })
  }
}

function prevMonth() {
  if (birthdayMonth.value <= 1) { birthdayMonth.value = 12; birthdayYear.value-- }
  else { birthdayMonth.value-- }
  loadBirthdays()
}

function nextMonth() {
  if (birthdayMonth.value >= 12) { birthdayMonth.value = 1; birthdayYear.value++ }
  else { birthdayMonth.value++ }
  loadBirthdays()
}

function goToday() {
  birthdayMonth.value = new Date().getMonth() + 1
  birthdayYear.value = new Date().getFullYear()
  loadBirthdays()
}

const calendarWeeks = computed(() => {
  const y = birthdayYear.value
  const m = birthdayMonth.value - 1
  const firstDay = new Date(y, m, 1).getDay()
  const daysInMonth = new Date(y, m + 1, 0).getDate()
  const today = new Date()
  const isCurrentMonth = today.getFullYear() === y && today.getMonth() === m
  const todayDate = today.getDate()

  const weeks: { day: number | null; isToday: boolean; birthdays: BirthdayEntry[] }[][] = []
  let week: { day: number | null; isToday: boolean; birthdays: BirthdayEntry[] }[] = []

  // Fill leading empty cells
  for (let i = 0; i < firstDay; i++) {
    week.push({ day: null, isToday: false, birthdays: [] })
  }

  for (let d = 1; d <= daysInMonth; d++) {
    const dayBirthdays = birthdays.value.filter(b => b.day === d)
    week.push({ day: d, isToday: isCurrentMonth && d === todayDate, birthdays: dayBirthdays })
    if (week.length === 7) {
      weeks.push(week)
      week = []
    }
  }

  // Fill trailing empty cells
  if (week.length > 0) {
    while (week.length < 7) week.push({ day: null, isToday: false, birthdays: [] })
    weeks.push(week)
  }

  return weeks
})

async function loadBlacklist() {
  try {
    const { data } = await api.get<BlacklistedCustomer[]>('/customers/blacklist')
    blacklisted.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load blacklist', life: 3000 })
  }
}

function switchTab(tab: 'list' | 'birthdays' | 'blacklist') {
  activeTab.value = tab
  if (tab === 'birthdays') loadBirthdays()
  else if (tab === 'blacklist') loadBlacklist()
}

function viewCustomer(id: number) {
  router.push(`/customers/${id}`)
}

onMounted(() => {
  loadFilters()
  loadCustomers()
})
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">Customers</h1>
    </div>

    <!-- Tabs -->
    <div class="flex gap-2 mb-4">
      <button
        v-for="tab in [{ key: 'list', label: 'All Customers', icon: 'pi-users' }, { key: 'birthdays', label: 'Birthdays', icon: 'pi-gift' }, { key: 'blacklist', label: 'Blacklist', icon: 'pi-ban' }]"
        :key="tab.key"
        @click="switchTab(tab.key as any)"
        class="px-4 py-2 rounded-lg text-sm font-medium border-0 cursor-pointer transition-colors"
        :style="activeTab === tab.key
          ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' }
          : { backgroundColor: 'var(--addrez-bg-card)', color: 'var(--addrez-text-secondary)' }"
      >
        <i :class="'pi ' + tab.icon" class="mr-1"></i>{{ tab.label }}
      </button>
    </div>

    <!-- ═══ LIST TAB ═══ -->
    <template v-if="activeTab === 'list'">
      <!-- Search & Filters -->
      <div class="card mb-4">
        <div class="flex flex-wrap gap-3 items-center">
          <input
            v-model="search"
            placeholder="Search Name/Mobile Number/Email"
            class="flex-1 min-w-[200px] px-3 py-2 rounded-lg border text-sm"
            :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
            @keyup.enter="page = 1; loadCustomers()"
          />
          <!-- Category Dropdown -->
          <select
            v-model="selectedCategoryId"
            class="px-3 py-2 rounded-lg border text-sm min-w-[180px]"
            :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
            @change="page = 1; loadCustomers()"
          >
            <option :value="null">All Client Categories</option>
            <option v-for="cat in categories" :key="cat.id" :value="cat.id">{{ cat.name }}</option>
          </select>
          <!-- Tag Filter -->
          <div class="relative">
            <button
              @click="showTagFilter = !showTagFilter"
              class="px-4 py-2 rounded-lg text-sm font-medium border cursor-pointer transition-colors"
              :style="selectedTagIds.length > 0
                ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24', borderColor: 'var(--addrez-gold)' }
                : { backgroundColor: 'transparent', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
            >
              <i class="pi pi-tag mr-1"></i>Search By Tags
              <span v-if="selectedTagIds.length" class="ml-1 text-xs">({{ selectedTagIds.length }})</span>
            </button>
            <!-- Tag dropdown -->
            <div v-if="showTagFilter" class="absolute right-0 mt-2 w-72 z-50 card shadow-lg" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
              <div class="text-xs font-semibold mb-2" :style="{ color: 'var(--addrez-text-secondary)' }">Select tags to filter</div>
              <div v-if="customerTags.length === 0" class="text-xs py-2" :style="{ color: 'var(--addrez-text-secondary)' }">No customer tags configured</div>
              <div v-else class="flex flex-wrap gap-1.5 mb-3 max-h-48 overflow-y-auto">
                <button
                  v-for="t in customerTags" :key="t.id"
                  @click="toggleTagFilter(t.id)"
                  class="px-2.5 py-1 rounded-full text-xs font-medium border-0 cursor-pointer transition-all"
                  :style="selectedTagIds.includes(t.id)
                    ? { backgroundColor: t.color, color: '#fff' }
                    : { backgroundColor: t.color + '20', color: t.color }"
                >{{ t.name }}</button>
              </div>
              <div class="flex gap-2 pt-2 border-t" :style="{ borderColor: 'var(--addrez-border)' }">
                <button @click="selectedTagIds = []; applyTagFilter()" class="flex-1 py-1.5 rounded text-xs bg-transparent border cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-secondary)' }">Clear</button>
                <button @click="applyTagFilter" class="flex-1 py-1.5 rounded text-xs btn-gold">Apply</button>
              </div>
            </div>
          </div>
          <button class="px-4 py-2 rounded-lg text-sm font-medium border-0 cursor-pointer" :style="{ backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-primary)' }" @click="page = 1; loadCustomers()">
            <i class="pi pi-search mr-1"></i>Search
          </button>
          <button v-if="selectedCategoryId || selectedTagIds.length || search" @click="clearFilters" class="px-3 py-2 rounded-lg text-xs border-0 cursor-pointer" style="color: #ef4444; background: transparent">
            <i class="pi pi-filter-slash mr-1"></i>Clear
          </button>
        </div>
      </div>

      <!-- Table -->
      <div class="card overflow-x-auto">
        <div v-if="loading" class="text-center py-8">
          <i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
        </div>
        <table v-else class="w-full text-sm">
          <thead>
            <tr :style="{ color: 'var(--addrez-text-secondary)' }">
              <th class="text-left py-3 px-3 font-medium">Name</th>
              <th class="text-left py-3 px-3 font-medium">Contact</th>
              <th class="text-left py-3 px-3 font-medium">Category</th>
              <th class="text-left py-3 px-3 font-medium">Status</th>
              <th class="text-center py-3 px-3 font-medium">Visits</th>
              <th class="text-right py-3 px-3 font-medium">Spend</th>
              <th class="text-left py-3 px-3 font-medium">Creation Date</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in customers" :key="c.id" @click="viewCustomer(c.id)" class="border-t cursor-pointer" style="transition: background-color 180ms ease" :style="{ borderColor: 'var(--addrez-border)' }" onmouseenter="this.style.backgroundColor='var(--addrez-table-row-hover)'" onmouseleave="this.style.backgroundColor='transparent'">
              <td class="py-3 px-3">
                <div class="font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ c.full_name }}</div>
                <div class="flex gap-1 mt-1">
                  <span v-for="t in c.tags" :key="t.id" class="text-xs px-1.5 py-0.5 rounded" :style="{ backgroundColor: t.color + '20', color: t.color }">{{ t.name }}</span>
                </div>
              </td>
              <td class="py-3 px-3" :style="{ color: 'var(--addrez-text-secondary)' }">
                <div v-if="c.email" class="text-xs">{{ c.email }}</div>
                <div v-if="c.phone" class="text-xs">{{ c.phone }}</div>
              </td>
              <td class="py-3 px-3">
                <span v-if="c.category_name" class="text-xs px-2 py-1 rounded" :style="{ backgroundColor: (c.category_color || '#6b7280') + '20', color: c.category_color || '#6b7280' }">{{ c.category_name }}</span>
                <span v-else class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">—</span>
              </td>
              <td class="py-3 px-3">
                <span class="text-xs px-2 py-1 rounded-full font-medium" :style="{ backgroundColor: (statusColors[c.status] || '#6b7280') + '20', color: statusColors[c.status] || '#6b7280' }">{{ c.status }}</span>
              </td>
              <td class="py-3 px-3 text-center" :style="{ color: 'var(--addrez-text-primary)' }">{{ c.total_visits }}</td>
              <td class="py-3 px-3 text-right font-medium" style="color: var(--addrez-gold)">EGP {{ c.total_spend.toFixed(0) }}</td>
              <td class="py-3 px-3 text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ new Date(c.created_at).toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: 'numeric', minute: '2-digit', hour12: true }) }}</td>
            </tr>
            <tr v-if="customers.length === 0">
              <td colspan="7" class="text-center py-8" :style="{ color: 'var(--addrez-text-secondary)' }">No customers found</td>
            </tr>
          </tbody>
        </table>

        <div v-if="total > perPage" class="flex items-center justify-between pt-4 mt-4 border-t" :style="{ borderColor: 'var(--addrez-border)' }">
          <span class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ total }} total</span>
          <div class="flex gap-1">
            <button @click="page--; loadCustomers()" :disabled="page <= 1" class="px-3 py-1 rounded text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Prev</button>
            <button @click="page++; loadCustomers()" :disabled="page * perPage >= total" class="px-3 py-1 rounded text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Next</button>
          </div>
        </div>
      </div>
    </template>

    <!-- ═══ BIRTHDAYS TAB ═══ -->
    <template v-else-if="activeTab === 'birthdays'">
      <!-- Header: Month/Year + Nav -->
      <div class="card mb-4">
        <div class="flex items-center justify-between">
          <h2 class="text-xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">
            {{ monthNames[birthdayMonth - 1] }} {{ birthdayYear }}
          </h2>
          <div class="flex items-center gap-1">
            <button @click="prevMonth" class="px-3 py-1.5 rounded-lg text-xs font-medium border-0 cursor-pointer" style="background: var(--addrez-gold); color: #1a1a24">Previous</button>
            <button @click="goToday" class="px-3 py-1.5 rounded-lg text-xs font-medium border cursor-pointer bg-transparent" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Today</button>
            <button @click="nextMonth" class="px-3 py-1.5 rounded-lg text-xs font-medium border-0 cursor-pointer" style="background: var(--addrez-gold); color: #1a1a24">Next</button>
          </div>
        </div>
      </div>

      <!-- Calendar Grid -->
      <div class="card overflow-hidden">
        <!-- Weekday Headers -->
        <div class="grid grid-cols-7 border-b" :style="{ borderColor: 'var(--addrez-border)' }">
          <div v-for="day in ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']" :key="day"
            class="py-2.5 px-2 text-center text-xs font-semibold"
            :style="{ color: day === 'Saturday' ? '#ef4444' : 'var(--addrez-text-secondary)' }">
            {{ day }}
          </div>
        </div>

        <!-- Calendar Rows -->
        <div v-for="(week, wi) in calendarWeeks" :key="wi" class="grid grid-cols-7" :style="wi < calendarWeeks.length - 1 ? { borderBottom: '1px solid var(--addrez-border)' } : {}">
          <div v-for="(cell, ci) in week" :key="ci"
            class="min-h-[100px] p-1.5 border-r last:border-r-0 relative"
            :style="{ borderColor: 'var(--addrez-border)', backgroundColor: cell.isToday ? 'var(--addrez-gold)' + '10' : 'transparent' }">
            <!-- Day Number -->
            <div v-if="cell.day" class="flex items-center gap-1 mb-1">
              <span
                class="text-xs font-semibold px-1.5 py-0.5 rounded-full"
                :style="cell.isToday
                  ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' }
                  : { color: ci === 6 ? '#ef4444' : 'var(--addrez-text-primary)' }">
                {{ cell.day }}
              </span>
              <span v-if="cell.birthdays.length" class="w-2 h-2 rounded-full" style="background: var(--addrez-gold)"></span>
            </div>
            <!-- Birthday entries -->
            <div v-for="b in cell.birthdays" :key="b.id"
              @click="viewCustomer(b.id)"
              class="text-[10px] leading-tight mb-0.5 px-1 py-0.5 rounded cursor-pointer truncate transition-colors hover:bg-[var(--addrez-bg-hover)]"
              :style="{ color: 'var(--addrez-text-primary)' }"
              :title="b.fullName + (b.categoryName ? ' (' + b.categoryName + ')' : '')">
              <span class="mr-0.5" style="color: var(--addrez-gold)">•</span>
              {{ b.fullName }}
              <span v-if="b.categoryName" class="opacity-60">({{ b.categoryName }})</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Birthday Summary below calendar -->
      <div v-if="birthdays.length > 0" class="card mt-4">
        <h3 class="text-sm font-semibold mb-3" :style="{ color: 'var(--addrez-text-primary)' }">
          <i class="pi pi-gift mr-1.5" style="color: var(--addrez-gold)"></i>{{ birthdays.length }} Birthday{{ birthdays.length > 1 ? 's' : '' }} in {{ monthNames[birthdayMonth - 1] }}
        </h3>
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-2">
          <div v-for="b in birthdays" :key="b.id" @click="viewCustomer(b.id)"
            class="flex items-center gap-3 p-2 rounded-lg cursor-pointer transition-colors hover:bg-[var(--addrez-bg-hover)]"
            :style="{ border: '1px solid var(--addrez-border)' }">
            <div class="w-9 h-9 rounded-full flex items-center justify-center text-sm font-bold shrink-0"
              style="background: linear-gradient(135deg, var(--addrez-gold), var(--addrez-gold-dark)); color: #1a1a24">
              {{ b.day }}
            </div>
            <div class="min-w-0 flex-1">
              <div class="text-sm font-medium truncate" :style="{ color: 'var(--addrez-text-primary)' }">{{ b.fullName }}</div>
              <div class="text-[10px] flex items-center gap-1.5" :style="{ color: 'var(--addrez-text-secondary)' }">
                <span>{{ b.phone || 'No phone' }}</span>
                <span v-if="b.categoryName" class="px-1 py-0 rounded" :style="{ backgroundColor: (b.categoryColor || '#6b7280') + '20', color: b.categoryColor || '#6b7280' }">{{ b.categoryName }}</span>
              </div>
            </div>
            <div class="text-right shrink-0">
              <div class="text-xs font-semibold" style="color: var(--addrez-gold)">{{ b.totalVisits }}</div>
              <div class="text-[9px]" :style="{ color: 'var(--addrez-text-secondary)' }">visits</div>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- ═══ BLACKLIST TAB ═══ -->
    <template v-else-if="activeTab === 'blacklist'">
      <div class="card">
        <div v-if="blacklisted.length === 0" class="text-center py-8">
          <i class="pi pi-check-circle text-3xl mb-2" style="color: #10b981"></i>
          <p :style="{ color: 'var(--addrez-text-secondary)' }">No blacklisted customers</p>
        </div>
        <table v-else class="w-full text-sm">
          <thead>
            <tr :style="{ color: 'var(--addrez-text-secondary)' }">
              <th class="text-left py-3 px-3 font-medium">Name</th>
              <th class="text-left py-3 px-3 font-medium">Contact</th>
              <th class="text-left py-3 px-3 font-medium">Reason</th>
              <th class="text-left py-3 px-3 font-medium">Blacklisted On</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in blacklisted" :key="c.id" @click="viewCustomer(c.id)" class="border-t cursor-pointer transition-colors hover:bg-[var(--addrez-bg-hover)]" :style="{ borderColor: 'var(--addrez-border)' }">
              <td class="py-3 px-3">
                <div class="font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ c.fullName }}</div>
              </td>
              <td class="py-3 px-3" :style="{ color: 'var(--addrez-text-secondary)' }">
                <div v-if="c.phone" class="text-xs">{{ c.phone }}</div>
                <div v-if="c.email" class="text-xs">{{ c.email }}</div>
              </td>
              <td class="py-3 px-3">
                <span class="text-xs" style="color: #ef4444">{{ c.blacklistReason || 'No reason provided' }}</span>
              </td>
              <td class="py-3 px-3 text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">
                {{ c.blacklistedAt ? new Date(c.blacklistedAt).toLocaleDateString() : '—' }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>
