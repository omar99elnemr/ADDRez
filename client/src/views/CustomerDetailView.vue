<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import type { CustomerDetail, CustomerReservation, CustomerActivityLog } from '@/types'
import maleAvatar from '@/assets/avatars/male.svg'
import femaleAvatar from '@/assets/avatars/female.svg'
import unspecifiedAvatar from '@/assets/avatars/unspecified.svg'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const customer = ref<CustomerDetail | null>(null)
const loading = ref(true)
const newNote = ref('')

// Tabs
const activeTab = ref<'overview' | 'reservations' | 'activity' | 'reviews'>('overview')

// Reservations tab data
const customerReservations = ref<CustomerReservation[]>([])
const loadingReservations = ref(false)

// Activity log tab data
const activityLogs = ref<CustomerActivityLog[]>([])
const loadingActivity = ref(false)

// Professional fixed avatars by gender
const avatarLoadFailed = ref(false)

const avatarUrl = computed(() => {
  if (!customer.value || avatarLoadFailed.value) return unspecifiedAvatar
  const gender = customer.value.gender?.toLowerCase()
  if (gender === 'female') return femaleAvatar
  if (gender === 'male') return maleAvatar
  return unspecifiedAvatar
})

function onAvatarError() {
  avatarLoadFailed.value = true
}

// Manage Tags
const showManageTags = ref(false)
const allCustomerTags = ref<{ id: number; name: string; color: string }[]>([])
const customerTagIds = ref<number[]>([])
const savingTags = ref(false)

const showEditDialog = ref(false)
const editSaving = ref(false)
const categories = ref<{ id: number; name: string; color: string }[]>([])
const editForm = ref({
  first_name: '', last_name: '', email: '', phone: '', phone_country_code: '',
  date_of_birth: '', gender: '', nationality: '', address: '', city: '', country: '',
  instagram: '', client_category_id: null as number | null, tag_ids: [] as number[]
})

const statusColors: Record<string, string> = {
  Active: '#10b981', VIP: '#d4a853', Blacklisted: '#ef4444', Inactive: '#6b7280'
}

const reservationStatusColors: Record<string, string> = {
  Pending: '#f59e0b', Confirmed: '#3b82f6', CheckedIn: '#8b5cf6', Seated: '#10b981', CheckedOut: '#6b7280', Cancelled: '#ef4444', NoShow: '#ef4444'
}

// Split reservations into upcoming and past
const today = computed(() => new Date().toISOString().split('T')[0])
const upcomingReservations = computed(() => customerReservations.value.filter(r => r.date >= today.value && r.status !== 'Cancelled' && r.status !== 'CheckedOut'))
const pastReservations = computed(() => customerReservations.value.filter(r => r.date < today.value || r.status === 'Cancelled' || r.status === 'CheckedOut'))

async function loadCustomer() {
  loading.value = true
  avatarLoadFailed.value = false
  try {
    const { data } = await api.get<CustomerDetail>(`/customers/${route.params.id}`)
    customer.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Customer not found', life: 3000 })
    router.push('/customers')
  } finally {
    loading.value = false
  }
}

async function loadReservations() {
  loadingReservations.value = true
  try {
    const { data } = await api.get<CustomerReservation[]>(`/customers/${route.params.id}/reservations`)
    customerReservations.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load reservations', life: 3000 })
  } finally { loadingReservations.value = false }
}

async function loadActivityLog() {
  loadingActivity.value = true
  try {
    const { data } = await api.get<CustomerActivityLog[]>(`/customers/${route.params.id}/activity-log`)
    activityLogs.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load activity log', life: 3000 })
  } finally { loadingActivity.value = false }
}

function switchTab(tab: typeof activeTab.value) {
  activeTab.value = tab
  if (tab === 'reservations' && customerReservations.value.length === 0) loadReservations()
  if (tab === 'activity' && activityLogs.value.length === 0) loadActivityLog()
}

async function addNote() {
  if (!newNote.value.trim()) return
  try {
    await api.post(`/customers/${route.params.id}/notes`, { note: newNote.value })
    newNote.value = ''
    loadCustomer()
    toast.add({ severity: 'success', summary: 'Added', detail: 'Note added', life: 2000 })
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to add note', life: 3000 })
  }
}

const showBlacklistDialog = ref(false)
const blacklistReason = ref('')

async function toggleBlacklist() {
  try {
    const body = customer.value?.status !== 'Blacklisted' ? { reason: blacklistReason.value || null } : {}
    await api.post(`/customers/${route.params.id}/blacklist`, body)
    showBlacklistDialog.value = false
    blacklistReason.value = ''
    loadCustomer()
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to update status', life: 3000 })
  }
}

function onBlacklistClick() {
  if (customer.value?.status === 'Blacklisted') {
    toggleBlacklist()
  } else {
    showBlacklistDialog.value = true
  }
}

async function openEditProfile() {
  if (!customer.value) return
  try {
    if (categories.value.length === 0) {
      const { data } = await api.get('/settings/categories')
      categories.value = data
    }
    editForm.value = {
      first_name: customer.value.first_name,
      last_name: customer.value.last_name,
      email: customer.value.email || '',
      phone: customer.value.phone || '',
      phone_country_code: customer.value.phone_country_code || '',
      date_of_birth: customer.value.date_of_birth ? customer.value.date_of_birth.substring(0, 10) : '',
      gender: customer.value.gender || '',
      nationality: customer.value.nationality || '',
      address: customer.value.address || '',
      city: customer.value.city || '',
      country: customer.value.country || '',
      instagram: customer.value.instagram || '',
      client_category_id: customer.value.client_category_id || null,
      tag_ids: customer.value.tags.map(t => t.id)
    }
    showEditDialog.value = true
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load categories', life: 3000 })
  }
}

async function saveEdit() {
  editSaving.value = true
  try {
    await api.put(`/customers/${route.params.id}`, {
      ...editForm.value,
      date_of_birth: editForm.value.date_of_birth || null
    })
    showEditDialog.value = false
    toast.add({ severity: 'success', summary: 'Updated', detail: 'Customer profile updated', life: 2000 })
    loadCustomer()
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to update customer', life: 3000 })
  } finally { editSaving.value = false }
}

async function openManageTags() {
  try {
    const { data } = await api.get('/settings/tags', { params: { type: 'Customer' } })
    allCustomerTags.value = data
    customerTagIds.value = customer.value?.tags.map(t => t.id) || []
    showManageTags.value = true
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load tags', life: 3000 })
  }
}

function toggleCustomerTag(tagId: number) {
  const idx = customerTagIds.value.indexOf(tagId)
  if (idx >= 0) customerTagIds.value.splice(idx, 1)
  else customerTagIds.value.push(tagId)
}

async function saveTags() {
  savingTags.value = true
  try {
    await api.put(`/customers/${route.params.id}`, {
      first_name: customer.value!.first_name,
      last_name: customer.value!.last_name,
      email: customer.value!.email,
      phone: customer.value!.phone,
      tag_ids: customerTagIds.value
    })
    showManageTags.value = false
    toast.add({ severity: 'success', summary: 'Updated', detail: 'Tags updated', life: 2000 })
    loadCustomer()
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to save tags', life: 3000 })
  } finally { savingTags.value = false }
}

function formatDateTime(dt: string) {
  return new Date(dt).toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: 'numeric', minute: '2-digit', hour12: true })
}

onMounted(loadCustomer)
</script>

<template>
  <div>
    <button @click="router.push('/customers')" class="mb-4 text-sm bg-transparent border-0 cursor-pointer flex items-center gap-1" :style="{ color: 'var(--addrez-text-secondary)' }">
      <i class="pi pi-arrow-left"></i> Back to Customers
    </button>

    <div v-if="loading" class="text-center py-12">
      <i class="pi pi-spin pi-spinner text-3xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
    </div>

    <template v-else-if="customer">
      <!-- ═══ Header: Avatar + Name + Category + Stats ═══ -->
      <div class="card mb-0 rounded-b-none border-b" :style="{ borderColor: 'var(--addrez-border)' }">
        <div class="flex flex-col md:flex-row gap-6 items-start">
          <!-- Gender-based Avatar -->
          <div class="w-24 h-24 rounded-full overflow-hidden shrink-0 border-2" :style="{ borderColor: 'var(--addrez-gold)' }">
            <img :src="avatarUrl" :alt="customer.full_name" class="w-full h-full object-cover" @error="onAvatarError" />
          </div>
          <div class="flex-1 min-w-0">
            <!-- Name + Category Badge -->
            <div class="flex items-center gap-3 flex-wrap">
              <h1 class="text-2xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">{{ customer.full_name }}</h1>
              <span v-if="customer.category_name" class="px-3 py-1 rounded-full text-xs font-semibold" style="background: #0ea5e920; color: #0ea5e9">
                {{ customer.category_name }}
              </span>
              <span v-else class="px-3 py-1 rounded-full text-xs font-semibold" :style="{ backgroundColor: '#6b728020', color: '#6b7280' }">No Category</span>
            </div>
            <!-- Phone + Edit -->
            <div class="flex items-center gap-2 mt-1">
              <span v-if="customer.phone" class="text-sm font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ customer.phone }}</span>
              <button @click="openEditProfile" class="bg-transparent border-0 cursor-pointer p-0" style="color: #3b82f6"><i class="pi pi-pencil text-xs"></i></button>
            </div>
            <!-- Quick stats line -->
            <div class="flex items-center gap-4 mt-2 text-sm">
              <span style="color: #0ea5e9">{{ customer.total_visits }} Reservations</span>
              <span style="color: #ef4444">{{ customer.cancellation_count }} Cancelled</span>
              <span style="color: #f59e0b">{{ customer.no_show_count }} No Show</span>
            </div>
            <div class="text-sm mt-1" :style="{ color: 'var(--addrez-text-secondary)' }">
              Total Spent: <span style="color: var(--addrez-gold); font-weight: 600">{{ customer.total_spend.toFixed(0) }} EGP</span>
            </div>
            <!-- Tags + Manage Tags -->
            <div class="flex flex-wrap items-center gap-1.5 mt-3">
              <span v-for="t in customer.tags" :key="t.id" class="text-xs px-2.5 py-1 rounded-full font-medium"
                :style="{ backgroundColor: t.color + '20', color: t.color, border: '1px solid ' + t.color + '40' }">{{ t.name }}</span>
              <button @click="openManageTags" class="text-xs px-3 py-1 rounded-full font-medium border cursor-pointer bg-transparent transition-colors"
                :style="{ borderColor: 'var(--addrez-gold)', color: 'var(--addrez-gold)' }">
                <i class="pi pi-tag mr-1" style="font-size: 10px"></i>Manage Tags
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- ═══ Tab Navigation ═══ -->
      <div class="flex gap-0 border-b mb-6" :style="{ borderColor: 'var(--addrez-border)', backgroundColor: 'var(--addrez-bg-card)' }">
        <button v-for="tab in [
          { key: 'overview', label: 'Overview' },
          { key: 'reservations', label: 'Reservations' },
          { key: 'activity', label: 'Activity Log' },
          { key: 'reviews', label: 'Customer Reviews' }
        ]" :key="tab.key"
          @click="switchTab(tab.key as any)"
          class="px-5 py-3 text-sm font-medium border-0 cursor-pointer transition-all relative"
          :style="activeTab === tab.key
            ? { color: '#0ea5e9', backgroundColor: 'transparent', borderBottom: '2px solid #0ea5e9' }
            : { color: 'var(--addrez-text-secondary)', backgroundColor: 'transparent', borderBottom: '2px solid transparent' }">
          {{ tab.label }}
        </button>
      </div>

      <!-- ═══════════ OVERVIEW TAB ═══════════ -->
      <template v-if="activeTab === 'overview'">
        <!-- Visit Details -->
        <div class="card mb-6">
          <h3 class="text-base font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">Visit Details:</h3>
          <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div class="text-center py-3 border-r" :style="{ borderColor: 'var(--addrez-border)' }">
              <div class="text-2xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">{{ customer.total_spend.toFixed(0) }}</div>
              <div class="text-xs mt-1" :style="{ color: 'var(--addrez-text-secondary)' }">Total Spent</div>
            </div>
            <div class="text-center py-3 border-r" :style="{ borderColor: 'var(--addrez-border)' }">
              <div class="text-2xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">{{ customer.average_spend.toFixed(0) }}</div>
              <div class="text-xs mt-1" :style="{ color: 'var(--addrez-text-secondary)' }">Average Spent</div>
            </div>
            <div class="text-center py-3 border-r" :style="{ borderColor: 'var(--addrez-border)' }">
              <div class="text-lg font-bold" :style="{ color: 'var(--addrez-text-primary)' }">{{ customer.last_visit_at ? new Date(customer.last_visit_at).toLocaleDateString() : '—' }}</div>
              <div class="text-xs mt-1" :style="{ color: 'var(--addrez-text-secondary)' }">Last Reservation</div>
            </div>
            <div class="text-center py-3">
              <div class="text-2xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">{{ customer.total_visits }}</div>
              <div class="text-xs mt-1" :style="{ color: 'var(--addrez-text-secondary)' }">Total Reservations</div>
            </div>
          </div>
        </div>

        <!-- Personal Details -->
        <div class="card mb-6">
          <div class="flex items-center gap-2 mb-4">
            <h3 class="text-base font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">Personal Details:</h3>
            <button @click="openEditProfile" class="bg-transparent border-0 cursor-pointer p-0" style="color: #3b82f6"><i class="pi pi-pencil text-sm"></i></button>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-x-8 gap-y-3 text-sm">
            <div class="flex items-start gap-2">
              <span class="font-medium min-w-[140px]" :style="{ color: 'var(--addrez-text-secondary)' }">Position:</span>
              <span :style="{ color: 'var(--addrez-text-primary)' }">{{ customer.position || '—' }}</span>
            </div>
            <div class="flex items-start gap-2">
              <span class="font-medium min-w-[140px]" :style="{ color: 'var(--addrez-text-secondary)' }">Gender:</span>
              <span :style="{ color: 'var(--addrez-text-primary)' }">{{ customer.gender || '—' }}</span>
            </div>
            <div class="flex items-start gap-2">
              <span class="font-medium min-w-[140px]" :style="{ color: 'var(--addrez-text-secondary)' }">Birthday:</span>
              <span :style="{ color: 'var(--addrez-text-primary)' }">{{ customer.date_of_birth ? new Date(customer.date_of_birth).toLocaleDateString() : '—' }}</span>
            </div>
            <div class="flex items-start gap-2">
              <span class="font-medium min-w-[140px]" :style="{ color: 'var(--addrez-text-secondary)' }">FB Link:</span>
              <a v-if="customer.facebook_url" :href="customer.facebook_url" target="_blank" class="underline" style="color: #3b82f6">{{ customer.facebook_url }}</a>
              <span v-else :style="{ color: 'var(--addrez-text-primary)' }">—</span>
            </div>
            <div class="flex items-start gap-2">
              <span class="font-medium min-w-[140px]" :style="{ color: 'var(--addrez-text-secondary)' }">Email:</span>
              <span :style="{ color: 'var(--addrez-text-primary)' }">{{ customer.email || '—' }}</span>
            </div>
            <div class="flex items-start gap-2">
              <span class="font-medium min-w-[140px]" :style="{ color: 'var(--addrez-text-secondary)' }">Creation Date:</span>
              <span :style="{ color: 'var(--addrez-text-primary)' }">{{ formatDateTime(customer.created_at) }}</span>
            </div>
            <div class="flex items-start gap-2">
              <span class="font-medium min-w-[140px]" :style="{ color: 'var(--addrez-text-secondary)' }">Address:</span>
              <span :style="{ color: 'var(--addrez-text-primary)' }">{{ [customer.address, customer.city, customer.country].filter(Boolean).join(', ') || '—' }}</span>
            </div>
            <div class="flex items-start gap-2">
              <span class="font-medium min-w-[140px]" :style="{ color: 'var(--addrez-text-secondary)' }">Membership Code:</span>
              <span :style="{ color: 'var(--addrez-text-primary)' }">—</span>
            </div>
            <div class="flex items-start gap-2">
              <span class="font-medium min-w-[140px]" :style="{ color: 'var(--addrez-text-secondary)' }">Instagram:</span>
              <span :style="{ color: 'var(--addrez-text-primary)' }">{{ customer.instagram || '—' }}</span>
            </div>
            <div class="flex items-start gap-2">
              <span class="font-medium min-w-[140px]" :style="{ color: 'var(--addrez-text-secondary)' }">Nationality:</span>
              <span :style="{ color: 'var(--addrez-text-primary)' }">{{ customer.nationality || '—' }}</span>
            </div>
          </div>
          <div class="mt-4 pt-4 border-t" :style="{ borderColor: 'var(--addrez-border)' }">
            <div class="flex items-start gap-2 text-sm">
              <span class="font-medium min-w-[140px]" :style="{ color: 'var(--addrez-text-secondary)' }">Notes:</span>
              <span :style="{ color: 'var(--addrez-text-primary)' }">{{ customer.notes.length > 0 ? customer.notes[0].note : '—' }}</span>
            </div>
          </div>
        </div>

        <!-- Notes section -->
        <div class="card mb-6">
          <h3 class="text-base font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">Notes</h3>
          <div class="flex gap-2 mb-4">
            <input v-model="newNote" placeholder="Add a note..." class="flex-1 px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" @keyup.enter="addNote" />
            <button @click="addNote" class="btn-gold text-sm px-4">Add</button>
          </div>
          <div v-if="customer.notes.length === 0" class="text-center py-4 text-sm" :style="{ color: 'var(--addrez-text-secondary)' }">No notes yet</div>
          <div v-else class="space-y-2">
            <div v-for="n in customer.notes" :key="n.id" class="p-3 rounded-lg" :style="{ backgroundColor: 'var(--addrez-bg-hover)' }">
              <p class="text-sm" :style="{ color: 'var(--addrez-text-primary)' }">{{ n.note }}</p>
              <div class="text-xs mt-1" :style="{ color: 'var(--addrez-text-secondary)' }">{{ n.user_name || 'System' }} · {{ formatDateTime(n.created_at) }}</div>
            </div>
          </div>
        </div>

        <!-- Status & Actions -->
        <div class="card">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-3">
              <h3 class="text-base font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">Status:</h3>
              <span class="px-3 py-1 rounded-full text-xs font-semibold" :style="{ backgroundColor: (statusColors[customer.status] || '#6b7280') + '20', color: statusColors[customer.status] }">{{ customer.status }}</span>
            </div>
            <div class="flex items-center gap-2">
              <button @click="onBlacklistClick" class="px-4 py-2 rounded-lg text-xs font-medium bg-transparent border cursor-pointer"
                :style="{ borderColor: customer.status === 'Blacklisted' ? '#10b981' : '#ef4444', color: customer.status === 'Blacklisted' ? '#10b981' : '#ef4444' }">
                {{ customer.status === 'Blacklisted' ? 'Remove from Blacklist' : 'Blacklist Customer' }}
              </button>
            </div>
          </div>
          <div v-if="customer.status === 'Blacklisted'" class="mt-3 p-3 rounded-lg" style="background: #ef444415; border: 1px solid #ef444440">
            <div class="text-xs font-medium" style="color: #ef4444"><i class="pi pi-ban mr-1"></i>Blacklisted</div>
            <div v-if="customer.blacklist_reason" class="text-xs mt-1" :style="{ color: 'var(--addrez-text-secondary)' }">{{ customer.blacklist_reason }}</div>
            <div v-if="customer.blacklisted_at" class="text-[10px] mt-1" :style="{ color: 'var(--addrez-text-secondary)' }">Since {{ new Date(customer.blacklisted_at).toLocaleDateString() }}</div>
          </div>
        </div>
      </template>

      <!-- ═══════════ RESERVATIONS TAB ═══════════ -->
      <template v-else-if="activeTab === 'reservations'">
        <div v-if="loadingReservations" class="text-center py-12"><i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i></div>
        <template v-else>
          <!-- Upcoming Reservations -->
          <div class="card mb-6">
            <h3 class="text-lg font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">Upcoming Reservations</h3>
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead><tr :style="{ color: 'var(--addrez-text-secondary)' }">
                  <th class="text-left py-2 px-3 font-medium">Date (Slot)</th>
                  <th class="text-left py-2 px-3 font-medium">Branch</th>
                  <th class="text-left py-2 px-3 font-medium">Status</th>
                  <th class="text-left py-2 px-3 font-medium">Type</th>
                  <th class="text-center py-2 px-3 font-medium"># Of Pax</th>
                  <th class="text-left py-2 px-3 font-medium">Table</th>
                  <th class="text-left py-2 px-3 font-medium">Notes</th>
                  <th class="text-right py-2 px-3 font-medium">Amount Spent</th>
                </tr></thead>
                <tbody>
                  <tr v-for="r in upcomingReservations" :key="r.id" class="border-t" style="transition: background-color 180ms ease" :style="{ borderColor: 'var(--addrez-border)' }" onmouseenter="this.style.backgroundColor='var(--addrez-table-row-hover)'" onmouseleave="this.style.backgroundColor='transparent'">
                    <td class="py-2 px-3" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.date }} {{ r.time }}<br><span class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.time_slot_name || '—' }}</span></td>
                    <td class="py-2 px-3" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.outlet_name || '—' }}</td>
                    <td class="py-2 px-3"><span class="text-xs px-2 py-0.5 rounded-full font-medium" :style="{ backgroundColor: (reservationStatusColors[r.status] || '#6b7280') + '20', color: reservationStatusColors[r.status] || '#6b7280' }">{{ r.status }}</span></td>
                    <td class="py-2 px-3" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.type }}</td>
                    <td class="py-2 px-3 text-center" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.covers }}</td>
                    <td class="py-2 px-3" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.table_name || '—' }}</td>
                    <td class="py-2 px-3 text-xs max-w-[120px] truncate" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.notes || '—' }}</td>
                    <td class="py-2 px-3 text-right font-medium" style="color: var(--addrez-gold)">{{ r.amount_spent ? r.amount_spent.toFixed(0) : '—' }}</td>
                  </tr>
                  <tr v-if="upcomingReservations.length === 0"><td colspan="8" class="text-center py-6 text-sm" :style="{ color: 'var(--addrez-text-secondary)' }">No upcoming reservations</td></tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Past Reservations -->
          <div class="card">
            <h3 class="text-lg font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">Past Reservations</h3>
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead><tr :style="{ color: 'var(--addrez-text-secondary)' }">
                  <th class="text-left py-2 px-3 font-medium">Date (Slot)</th>
                  <th class="text-left py-2 px-3 font-medium">Branch</th>
                  <th class="text-left py-2 px-3 font-medium">Status</th>
                  <th class="text-left py-2 px-3 font-medium">Type</th>
                  <th class="text-center py-2 px-3 font-medium"># Of Pax</th>
                  <th class="text-left py-2 px-3 font-medium">Table</th>
                  <th class="text-left py-2 px-3 font-medium">Notes</th>
                  <th class="text-right py-2 px-3 font-medium">Amount Spent</th>
                </tr></thead>
                <tbody>
                  <tr v-for="r in pastReservations" :key="r.id" class="border-t" style="transition: background-color 180ms ease" :style="{ borderColor: 'var(--addrez-border)' }" onmouseenter="this.style.backgroundColor='var(--addrez-table-row-hover)'" onmouseleave="this.style.backgroundColor='transparent'">
                    <td class="py-2 px-3" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.date }} {{ r.time }}<br><span class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.time_slot_name || '—' }}</span></td>
                    <td class="py-2 px-3" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.outlet_name || '—' }}</td>
                    <td class="py-2 px-3"><span class="text-xs px-2 py-0.5 rounded-full font-medium" :style="{ backgroundColor: (reservationStatusColors[r.status] || '#6b7280') + '20', color: reservationStatusColors[r.status] || '#6b7280' }">{{ r.status }}</span></td>
                    <td class="py-2 px-3" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.type }}</td>
                    <td class="py-2 px-3 text-center" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.covers }}</td>
                    <td class="py-2 px-3" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.table_name || '—' }}</td>
                    <td class="py-2 px-3 text-xs max-w-[120px] truncate" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.notes || '—' }}</td>
                    <td class="py-2 px-3 text-right font-medium" style="color: var(--addrez-gold)">{{ r.amount_spent ? r.amount_spent.toFixed(0) : '—' }}</td>
                  </tr>
                  <tr v-if="pastReservations.length === 0"><td colspan="8" class="text-center py-6 text-sm" :style="{ color: 'var(--addrez-text-secondary)' }">No past reservations</td></tr>
                </tbody>
              </table>
            </div>
          </div>
        </template>
      </template>

      <!-- ═══════════ ACTIVITY LOG TAB ═══════════ -->
      <template v-else-if="activeTab === 'activity'">
        <div v-if="loadingActivity" class="text-center py-12"><i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i></div>
        <div v-else class="card">
          <h3 class="text-lg font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">Log Details</h3>
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead><tr :style="{ color: 'var(--addrez-text-secondary)' }">
                <th class="text-left py-3 px-3 font-medium">Date - Time</th>
                <th class="text-left py-3 px-3 font-medium">Action</th>
                <th class="text-left py-3 px-3 font-medium">Description</th>
                <th class="text-left py-3 px-3 font-medium">Done By</th>
              </tr></thead>
              <tbody>
                <tr v-for="log in activityLogs" :key="log.id" class="border-t" style="transition: background-color 180ms ease" :style="{ borderColor: 'var(--addrez-border)' }" onmouseenter="this.style.backgroundColor='var(--addrez-table-row-hover)'" onmouseleave="this.style.backgroundColor='transparent'">
                  <td class="py-3 px-3" :style="{ color: 'var(--addrez-text-primary)' }">{{ formatDateTime(log.created_at) }}</td>
                  <td class="py-3 px-3" :style="{ color: 'var(--addrez-text-primary)' }">{{ log.action }}</td>
                  <td class="py-3 px-3" :style="{ color: 'var(--addrez-text-secondary)' }">{{ log.description || '—' }}</td>
                  <td class="py-3 px-3" :style="{ color: 'var(--addrez-text-primary)' }">{{ log.done_by || 'System' }}</td>
                </tr>
                <tr v-if="activityLogs.length === 0"><td colspan="4" class="text-center py-8" :style="{ color: 'var(--addrez-text-secondary)' }">No activity logs found</td></tr>
              </tbody>
            </table>
          </div>
        </div>
      </template>

      <!-- ═══════════ CUSTOMER REVIEWS TAB ═══════════ -->
      <template v-else-if="activeTab === 'reviews'">
        <div class="card">
          <h3 class="text-lg font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">Customer Reviews</h3>
          <div class="text-center py-12">
            <i class="pi pi-star text-4xl mb-3" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
            <p class="text-sm" :style="{ color: 'var(--addrez-text-secondary)' }">No reviews or feedback from this customer yet.</p>
          </div>
        </div>
      </template>
    </template>

    <!-- Manage Tags Dialog -->
    <div v-if="showManageTags" class="fixed inset-0 z-50 flex items-center justify-center" style="background: rgba(0,0,0,0.5)">
      <div class="card w-full max-w-md mx-4" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }"><i class="pi pi-tag mr-2" style="color: var(--addrez-gold)"></i>Manage Tags</h3>
          <button @click="showManageTags = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <p class="text-xs mb-3" :style="{ color: 'var(--addrez-text-secondary)' }">Select tags for <strong :style="{ color: 'var(--addrez-text-primary)' }">{{ customer?.full_name }}</strong></p>
        <div v-if="allCustomerTags.length === 0" class="py-4 text-center text-sm" :style="{ color: 'var(--addrez-text-secondary)' }">
          No customer tags configured. Go to Settings → Tags & Categories to add some.
        </div>
        <div v-else class="flex flex-wrap gap-2 mb-4 max-h-64 overflow-y-auto">
          <button v-for="t in allCustomerTags" :key="t.id" @click="toggleCustomerTag(t.id)"
            class="px-3 py-1.5 rounded-full text-sm font-medium border-0 cursor-pointer transition-all"
            :style="customerTagIds.includes(t.id)
              ? { backgroundColor: t.color, color: '#fff', boxShadow: '0 0 0 2px ' + t.color + '60' }
              : { backgroundColor: t.color + '20', color: t.color }">
            <i v-if="customerTagIds.includes(t.id)" class="pi pi-check mr-1" style="font-size: 10px"></i>{{ t.name }}
          </button>
        </div>
        <div class="flex justify-end gap-2 pt-2 border-t" :style="{ borderColor: 'var(--addrez-border)' }">
          <button @click="showManageTags = false" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
          <button @click="saveTags" :disabled="savingTags" class="btn-gold text-sm">{{ savingTags ? 'Saving...' : 'Save Tags' }}</button>
        </div>
      </div>
    </div>

    <!-- Blacklist Reason Dialog -->
    <div v-if="showBlacklistDialog" class="fixed inset-0 z-50 flex items-center justify-center" style="background: rgba(0,0,0,0.5)">
      <div class="card w-full max-w-sm mx-4" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" style="color: #ef4444"><i class="pi pi-ban mr-2"></i>Blacklist Customer</h3>
          <button @click="showBlacklistDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }">
            <i class="pi pi-times"></i>
          </button>
        </div>
        <p class="text-sm mb-3" :style="{ color: 'var(--addrez-text-secondary)' }">Are you sure you want to blacklist <strong :style="{ color: 'var(--addrez-text-primary)' }">{{ customer?.full_name }}</strong>?</p>
        <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Reason (optional)</label>
        <textarea v-model="blacklistReason" rows="3" placeholder="Enter reason for blacklisting..." class="w-full px-3 py-2 rounded-lg border text-sm resize-none mb-4" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"></textarea>
        <div class="flex justify-end gap-2">
          <button @click="showBlacklistDialog = false" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
          <button @click="toggleBlacklist" class="px-4 py-2 rounded-lg text-sm font-medium border-0 cursor-pointer" style="background: #ef4444; color: #fff">Blacklist</button>
        </div>
      </div>
    </div>
    <!-- Edit Customer Profile Dialog -->
    <div v-if="showEditDialog" class="fixed inset-0 z-50 flex items-center justify-center" style="background: rgba(0,0,0,0.5)">
      <div class="card w-full max-w-2xl mx-4 max-h-[85vh] overflow-y-auto" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }"><i class="pi pi-pencil mr-2" style="color: #3b82f6"></i>Edit Customer Profile</h3>
          <button @click="showEditDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="saveEdit" class="space-y-3">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">First Name *</label>
              <input v-model="editForm.first_name" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Last Name *</label>
              <input v-model="editForm.last_name" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Email</label>
              <input v-model="editForm.email" type="email" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Phone</label>
              <input v-model="editForm.phone" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Date of Birth</label>
              <input v-model="editForm.date_of_birth" type="date" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Gender</label>
              <select v-model="editForm.gender" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
                <option value="">Not specified</option>
                <option value="Male">Male</option>
                <option value="Female">Female</option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Nationality</label>
              <input v-model="editForm.nationality" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Instagram</label>
              <input v-model="editForm.instagram" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Address</label>
              <input v-model="editForm.address" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">City</label>
              <input v-model="editForm.city" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Country</label>
              <input v-model="editForm.country" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Category</label>
              <select v-model="editForm.client_category_id" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
                <option :value="null">No Category</option>
                <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
              </select>
            </div>
          </div>
          <div class="flex justify-end gap-2 pt-3 border-t" :style="{ borderColor: 'var(--addrez-border)' }">
            <button type="button" @click="showEditDialog = false" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
            <button type="submit" :disabled="editSaving" class="btn-gold text-sm">{{ editSaving ? 'Saving...' : 'Save Changes' }}</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
