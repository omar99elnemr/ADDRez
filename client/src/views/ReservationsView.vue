<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, computed, watch, nextTick } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'
import api from '@/services/api'
import type { Reservation, PaginatedResponse, TimeSlot, GuestSearchResult, SeatingType, Tag, Category, Layout, TableItem, CapacityData, Outlet } from '@/types'

const toast = useToast()
const auth = useAuthStore()
const theme = useThemeStore()

const canvasBg = computed(() => theme.mode === 'dark' ? '#1a1f2e' : '#e9edf2')

// ── Auto-fit canvas scaling ──
const mapContainerRef = ref<HTMLElement | null>(null)
const mapContainerWidth = ref(600)
let mapResizeObserver: ResizeObserver | null = null

const mapScale = computed(() => {
  if (!activeFloorPlan.value) return 1
  const cw = activeFloorPlan.value.width
  const ch = activeFloorPlan.value.height
  const availW = mapContainerWidth.value
  const maxH = Math.max(300, window.innerHeight - 280)
  const scaleW = availW / cw
  const scaleH = maxH / ch
  return Math.min(scaleW, scaleH, 1)
})

function initMapResize() {
  if (mapContainerRef.value) {
    mapContainerWidth.value = mapContainerRef.value.clientWidth
    mapResizeObserver = new ResizeObserver(entries => {
      for (const entry of entries) {
        mapContainerWidth.value = entry.contentRect.width
      }
    })
    mapResizeObserver.observe(mapContainerRef.value)
  }
}

// ── Outlet selector ──
const showOutletDropdown = ref(false)
const outletName = computed(() => {
  // Prefer fresh API data, fall back to auth cache
  const outlets = allOutlets.value.length > 0 ? allOutlets.value : (auth.user?.outlets ?? [])
  const b = outlets.find(o => o.id === auth.currentOutletId)
  return b?.name ?? outlets[0]?.name ?? 'Outlet'
})
const userOutlets = computed(() => allOutlets.value.length > 0 ? allOutlets.value : (auth.user?.outlets ?? []))

function switchOutlet(outletId: number) {
  auth.setOutlet(outletId)
  showOutletDropdown.value = false
  loadAll()
}

function toggleOutletDropdown() {
  showOutletDropdown.value = !showOutletDropdown.value
}

function closeOutletDropdown() {
  showOutletDropdown.value = false
}

// ── View mode ──
const viewMode = ref<'list' | 'map'>('map')
const rightPanelMode = ref<'list' | 'form'>('list')

// ── Shared state ──
const loading = ref(true)
const timeSlots = ref<TimeSlot[]>([])
const allTags = ref<Tag[]>([])
const allCategories = ref<Category[]>([])
const allOutlets = ref<Outlet[]>([])
const dateFilter = ref(new Date().toISOString().split('T')[0])
const timeSlotFilter = ref<number | null>(null)

// ── List state ──
const reservations = ref<Reservation[]>([])
const total = ref(0)
const page = ref(1)
const perPage = ref(15)
const search = ref('')
const statusFilter = ref('')
const slotScope = ref<'current' | 'all'>('current')

// ── Map state ──
const layouts = ref<Layout[]>([])
const activeFloorPlanIdx = ref(0)
const capacity = ref<CapacityData | null>(null)
// The current outlet's layout (first/default). An outlet typically has one layout.
const currentLayout = computed(() => {
  const def = layouts.value.find(l => l.is_default)
  return def ?? layouts.value[0] ?? null
})
// Floor plans = areas within that layout (e.g. Indoor/Outdoor, Floor1/Floor2)
const floorPlanAreas = computed(() => currentLayout.value?.floor_plans ?? [])
const activeFloorPlan = computed(() => floorPlanAreas.value[activeFloorPlanIdx.value])
const mapStatusColors: Record<string, string> = { Confirmed: '#3b82f6', CheckedIn: '#8b5cf6', Seated: '#10b981' }

// ── Create form ──
const guestQuery = ref('')
const guestResults = ref<GuestSearchResult[]>([])
const selectedGuest = ref<GuestSearchResult | null>(null)
const showGuestResults = ref(false)
const saving = ref(false)
const selectedTable = ref<TableItem | null>(null)

const defaultForm = () => ({
  customer_id: null as number | null,
  guest_name: '', guest_email: '', guest_phone: '',
  guest_date_of_birth: '', guest_gender: '' as string,
  membership_no: '', room_no: '',
  covers: 2, date: dateFilter.value, time: '', duration_minutes: 90,
  type: 'DineIn' as string, seating_type: 'Seated' as SeatingType,
  method: 'Phone' as string, notes: '',
  deposit_amount: null as number | null,
  discount_percent: null as number | null, discount_reason: '',
  time_slot_id: timeSlotFilter.value as number | null,
  table_id: null as number | null,
  tag_ids: [] as number[]
})
const form = ref(defaultForm())

async function searchGuests() {
  if (guestQuery.value.length < 2) { guestResults.value = []; return }
  try {
    const { data } = await api.get<GuestSearchResult[]>('/reservations/search-guests', { params: { q: guestQuery.value } })
    guestResults.value = data
    showGuestResults.value = data.length > 0
  } catch { /* silent */ }
}

function selectGuest(guest: GuestSearchResult) {
  selectedGuest.value = guest
  form.value.customer_id = guest.id
  form.value.guest_name = guest.fullName
  form.value.guest_phone = guest.phone || ''
  form.value.guest_email = guest.email || ''
  guestQuery.value = guest.fullName
  showGuestResults.value = false
}

function clearGuest() {
  selectedGuest.value = null
  form.value.customer_id = null
  guestQuery.value = ''
}

const statusOptions = [
  { label: 'All', value: '' },
  { label: 'Pending', value: 'Pending' },
  { label: 'Confirmed', value: 'Confirmed' },
  { label: 'Checked In', value: 'CheckedIn' },
  { label: 'Seated', value: 'Seated' },
  { label: 'Checked Out', value: 'CheckedOut' },
  { label: 'Cancelled', value: 'Cancelled' },
  { label: 'No Show', value: 'NoShow' },
]

const statusColors: Record<string, string> = {
  Pending: '#f59e0b', Confirmed: '#3b82f6', CheckedIn: '#8b5cf6',
  Seated: '#10b981', CheckedOut: '#6b7280', Cancelled: '#ef4444', NoShow: '#dc2626', Waitlisted: '#a855f7'
}

// ── Data loaders ──
async function loadReservations() {
  loading.value = true
  try {
    const params: Record<string, any> = { page: page.value, per_page: perPage.value }
    if (search.value) params.search = search.value
    if (statusFilter.value) params.status = statusFilter.value
    if (dateFilter.value) params.date = dateFilter.value
    if (timeSlotFilter.value && slotScope.value === 'current') params.timeSlotId = timeSlotFilter.value

    const { data } = await api.get<PaginatedResponse<Reservation>>('/reservations', { params })
    reservations.value = data.data
    total.value = data.total
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load reservations', life: 3000 })
  } finally { loading.value = false }
}

async function loadAll() {
  loading.value = true
  auth.refreshUser()
  try {
    const [slotRes, tagRes, catRes, outletRes, layoutRes] = await Promise.all([
      api.get<TimeSlot[]>('/time-slots'),
      api.get<Tag[]>('/settings/tags'),
      api.get<Category[]>('/settings/categories'),
      api.get<Outlet[]>('/settings/outlets'),
      api.get<Layout[]>('/floor-plans/layouts')
    ])
    timeSlots.value = slotRes.data.filter((s: TimeSlot) => s.is_active)
    allTags.value = tagRes.data
    allCategories.value = catRes.data.filter(c => c.is_active)
    allOutlets.value = outletRes.data.filter(b => b.is_active)
    layouts.value = layoutRes.data
    activeFloorPlanIdx.value = 0
    if (timeSlots.value.length > 0 && !timeSlotFilter.value) timeSlotFilter.value = timeSlots.value[0].id
  } catch { /* silent */ }
  await Promise.all([loadReservations(), loadCapacity()])
}

async function loadCapacity() {
  try {
    const params: Record<string, number> = {}
    if (timeSlotFilter.value) params.timeSlotId = timeSlotFilter.value
    const { data } = await api.get<CapacityData>('/reservations/capacity', { params })
    capacity.value = data
  } catch { /* silent */ }
}

// ── Map helpers ──
function getTableStyle(table: TableItem) {
  const sc = table.current_status ? mapStatusColors[table.current_status] || 'var(--addrez-border)' : 'var(--addrez-border)'
  const isRound = table.shape === 'Circle' || table.shape === 'Oval' || table.shape === 'Round'
  return {
    position: 'absolute' as const, left: `${table.x}px`, top: `${table.y}px`,
    width: `${table.width}px`, height: `${table.height}px`,
    transform: `rotate(${table.rotation}deg)`,
    borderRadius: isRound ? '50%' : '8px',
    border: `2.5px solid ${sc}`,
    backgroundColor: table.current_status ? sc + '22' : 'var(--addrez-bg-card)',
    boxShadow: `inset 0 0 0 2px ${table.current_status ? sc + '18' : 'var(--addrez-border)'}`,
    cursor: table.current_status ? 'default' : 'pointer',
    display: 'flex', alignItems: 'center', justifyContent: 'center', flexDirection: 'column' as const, transition: 'all 0.2s'
  }
}

function openReserveFromTable(table: TableItem) {
  if (table.current_status) return
  selectedTable.value = table
  form.value = defaultForm()
  form.value.table_id = table.id
  form.value.covers = table.min_covers
  rightPanelMode.value = 'form'
  if (viewMode.value === 'list') viewMode.value = 'map'
}

function openCreateNew() {
  selectedTable.value = null
  form.value = defaultForm()
  rightPanelMode.value = 'form'
  if (viewMode.value === 'list') viewMode.value = 'map'
}

function cancelCreate() {
  rightPanelMode.value = 'list'
  selectedTable.value = null
}

// ── CRUD ──
async function createReservation() {
  saving.value = true
  try {
    await api.post('/reservations', form.value)
    toast.add({ severity: 'success', summary: 'Created', detail: 'Reservation created', life: 3000 })
    rightPanelMode.value = 'list'
    form.value = defaultForm()
    clearGuest()
    await Promise.all([loadReservations(), loadCapacity()])
    if (viewMode.value === 'map') {
      const { data } = await api.get<Layout[]>('/floor-plans/layouts')
      layouts.value = data
    }
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Failed to create', life: 3000 })
  } finally { saving.value = false }
}

async function changeStatus(id: number, status: string) {
  try {
    await api.post(`/reservations/${id}/status`, { status })
    toast.add({ severity: 'success', summary: 'Updated', detail: `Status changed to ${status}`, life: 3000 })
    loadReservations()
    loadCapacity()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Status change failed', life: 3000 })
  }
}

function toggleTag(id: number) {
  const idx = form.value.tag_ids.indexOf(id)
  if (idx >= 0) form.value.tag_ids.splice(idx, 1)
  else form.value.tag_ids.push(id)
}

function getStatusColor(status: string) { return statusColors[status] || '#6b7280' }

function goToToday() {
  dateFilter.value = new Date().toISOString().split('T')[0]
  loadReservations()
  loadCapacity()
}

watch(timeSlotFilter, () => { loadReservations(); loadCapacity() })
watch(dateFilter, () => { loadReservations() })
watch(slotScope, () => { loadReservations() })

onMounted(() => { loadAll(); nextTick(initMapResize) })
onBeforeUnmount(() => { mapResizeObserver?.disconnect() })
</script>

<template>
  <div>
    <!-- ═══ Top bar: Outlet + Date + Slot + Actions + Map/List toggle ═══ -->
    <div class="flex items-center justify-between mb-4">
      <div class="flex items-center gap-3">
        <!-- Outlet Dropdown -->
        <div class="relative">
          <button @click="toggleOutletDropdown" class="flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold border-0 cursor-pointer transition-all"
            style="background: #3b82f6; color: #fff; box-shadow: 0 2px 8px rgba(59,130,246,0.3)">
            <i class="pi pi-building"></i>
            {{ outletName }}
            <i class="pi pi-chevron-down text-xs" :style="{ transform: showOutletDropdown ? 'rotate(180deg)' : 'none', transition: 'transform 0.2s' }"></i>
          </button>
          <div v-if="showOutletDropdown" class="absolute top-full left-0 mt-1 min-w-[220px] rounded-xl border shadow-xl overflow-hidden z-50"
            :style="{ backgroundColor: 'var(--addrez-bg-card)', borderColor: 'var(--addrez-border)' }">
            <div v-for="o in userOutlets" :key="o.id" @click="switchOutlet(o.id)"
              class="flex items-center gap-2 px-4 py-2.5 cursor-pointer transition-colors text-sm"
              :style="o.id === auth.currentOutletId ? { backgroundColor: 'rgba(59,130,246,0.12)', color: '#3b82f6', fontWeight: '600' } : { color: 'var(--addrez-text-primary)' }">
              <i class="pi pi-building text-xs" :style="{ color: o.id === auth.currentOutletId ? '#3b82f6' : 'var(--addrez-text-secondary)' }"></i>
              {{ o.name }}
              <i v-if="o.id === auth.currentOutletId" class="pi pi-check ml-auto text-xs" style="color: #3b82f6"></i>
            </div>
          </div>
          <div v-if="showOutletDropdown" class="fixed inset-0 z-40" @click="closeOutletDropdown"></div>
        </div>

        <input v-model="dateFilter" type="date" class="px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
        <select v-if="timeSlots.length" :value="timeSlotFilter ?? ''" @change="timeSlotFilter = ($event.target as HTMLSelectElement).value ? Number(($event.target as HTMLSelectElement).value) : null" class="px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
          <option value="">All Slots</option>
          <option v-for="ts in timeSlots" :key="ts.id" :value="ts.id">{{ ts.name }} ({{ ts.start_time }} – {{ ts.end_time }})</option>
        </select>
        <button @click="goToToday" class="px-3 py-2 rounded-lg text-xs font-medium border-0 cursor-pointer" style="color: var(--addrez-gold); background: transparent">Go To Today</button>
      </div>

      <div class="flex items-center gap-2">
        <!-- (+) Add Reservation button -->
        <button @click="openCreateNew" class="w-10 h-10 rounded-lg flex items-center justify-center border-0 cursor-pointer text-lg font-bold transition-all"
          style="background: #0ea5e9; color: #fff; box-shadow: 0 2px 8px rgba(14,165,233,0.3)">
          <i class="pi pi-plus"></i>
        </button>
        <!-- Map / List toggle -->
        <div class="flex rounded-lg overflow-hidden border" :style="{ borderColor: 'var(--addrez-border)' }">
          <button @click="viewMode = 'map'; rightPanelMode = 'list'" class="px-4 py-2 text-xs font-medium border-0 cursor-pointer transition-colors" :style="viewMode === 'map' ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' } : { backgroundColor: 'var(--addrez-bg-card)', color: 'var(--addrez-text-secondary)' }"><i class="pi pi-th-large mr-1"></i>Map</button>
          <button @click="viewMode = 'list'" class="px-4 py-2 text-xs font-medium border-0 cursor-pointer transition-colors" :style="viewMode === 'list' ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' } : { backgroundColor: 'var(--addrez-bg-card)', color: 'var(--addrez-text-secondary)' }"><i class="pi pi-list mr-1"></i>List</button>
        </div>
      </div>
    </div>

    <div v-if="loading" class="text-center py-12"><i class="pi pi-spin pi-spinner text-3xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i></div>

    <!-- ═══════════════════ MAP VIEW — side-by-side ═══════════════════ -->
    <template v-else-if="viewMode === 'map'">
      <div class="flex gap-4" style="min-height: 500px">
        <!-- ── Left: Map canvas + metrics ── -->
        <div class="flex-1 min-w-0 flex flex-col">
          <template v-if="currentLayout && activeFloorPlan">
            <div ref="mapContainerRef" class="rounded-xl border overflow-hidden relative" :style="{ borderColor: 'var(--addrez-border)', backgroundColor: canvasBg }">
              <!-- Area selector overlay -->
              <div v-if="floorPlanAreas.length > 1" class="absolute top-3 left-3 z-10">
                <select :value="activeFloorPlanIdx" @change="activeFloorPlanIdx = Number(($event.target as HTMLSelectElement).value)"
                  class="px-3 py-1.5 rounded-lg border text-xs font-medium cursor-pointer"
                  style="background: rgba(59,130,246,0.15); border-color: #3b82f6; color: #3b82f6; backdrop-filter: blur(8px); -webkit-backdrop-filter: blur(8px)">
                  <option v-for="(fp, idx) in floorPlanAreas" :key="fp.id" :value="idx" :style="{ color: 'var(--addrez-text-primary)', backgroundColor: 'var(--addrez-bg-card)' }">{{ fp.name }}</option>
                </select>
              </div>
              <!-- Canvas (auto-fit to container) -->
              <div :style="{ width: (activeFloorPlan.width * mapScale) + 'px', height: (activeFloorPlan.height * mapScale) + 'px', overflow: 'hidden' }">
                <div class="relative" :style="{ width: activeFloorPlan.width + 'px', height: activeFloorPlan.height + 'px', transform: `scale(${mapScale})`, transformOrigin: 'top left' }">
                  <!-- Tables -->
                  <div v-for="table in activeFloorPlan.tables" :key="table.id" :style="getTableStyle(table)"
                    :title="`${table.name} (${table.min_covers}-${table.max_covers})${table.current_reservation_guest ? '\n' + table.current_reservation_guest : ''}${!table.current_status ? '\nClick to reserve' : ''}`"
                    @click="openReserveFromTable(table)">
                    <span class="text-sm font-bold leading-tight" :style="{ color: table.current_status ? mapStatusColors[table.current_status] : 'var(--addrez-text-primary)' }">{{ table.label || table.name }}</span>
                    <span class="text-xs leading-tight" :style="{ color: 'var(--addrez-text-secondary)' }">{{ table.max_covers }}</span>
                    <span v-if="table.current_reservation_guest" class="text-[10px] mt-0.5 leading-tight" :style="{ color: mapStatusColors[table.current_status!] }">{{ table.current_reservation_guest }}</span>
                  </div>
                  <!-- Landmarks -->
                  <div v-for="lm in activeFloorPlan.landmarks" :key="lm.id" class="absolute flex items-center justify-center"
                    :style="{ left: lm.x+'px', top: lm.y+'px', width: lm.width+'px', height: lm.height+'px', transform: `rotate(${lm.rotation}deg)`, border: '2px solid #64748b90', borderRadius: '6px', backgroundColor: '#64748b20' }">
                    <span class="text-xs font-semibold" style="color: #64748b">{{ lm.name }}</span>
                  </div>
                </div>
              </div>
            </div>
          </template>
          <div v-else class="rounded-xl border flex-1 flex items-center justify-center" :style="{ borderColor: 'var(--addrez-border)', backgroundColor: canvasBg }">
            <div class="text-center">
              <i class="pi pi-th-large text-4xl mb-3" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
              <p :style="{ color: 'var(--addrez-text-secondary)' }">No floor plans configured.</p>
            </div>
          </div>

          <!-- ── Metrics bar BELOW the map ── -->
          <div v-if="capacity" class="grid grid-cols-7 gap-2 mt-3">
            <div class="rounded-lg py-2 text-center" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
              <div class="text-base font-bold" style="color: var(--addrez-gold)">{{ capacity.totalCovers }}</div>
              <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Overview</div>
            </div>
            <div class="rounded-lg py-2 text-center" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
              <div class="text-base font-bold" style="color: #3b82f6">{{ capacity.totalSeated }}</div>
              <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Seated</div>
            </div>
            <div class="rounded-lg py-2 text-center" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
              <div class="text-base font-bold" style="color: #8b5cf6">{{ capacity.totalStanding }}</div>
              <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Standing</div>
            </div>
            <div class="rounded-lg py-2 text-center" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
              <div class="text-base font-bold" style="color: #10b981">{{ capacity.tablesReserved }}/{{ capacity.totalTables }}</div>
              <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Tables</div>
            </div>
            <div class="rounded-lg py-2 text-center" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
              <div class="text-base font-bold" style="color: #10b981">{{ capacity.attendedCustomers }}</div>
              <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Attended</div>
            </div>
            <div class="rounded-lg py-2 text-center" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
              <div class="text-base font-bold" style="color: #06b6d4">{{ capacity.walkInCustomers }}</div>
              <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Walk-ins</div>
            </div>
            <div class="rounded-lg py-2 text-center" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
              <div class="text-base font-bold" style="color: #f59e0b">{{ capacity.pendingCount }}</div>
              <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Pending</div>
            </div>
          </div>
        </div>

        <!-- ── Right: Reservations list OR Create form ── -->
        <div class="w-[360px] shrink-0 flex flex-col">
          <!-- === Right panel: Reservation List === -->
          <template v-if="rightPanelMode === 'list'">
            <div class="flex items-center justify-between mb-2">
              <div>
                <h2 class="text-lg font-semibold" style="color: #0ea5e9">Reservations</h2>
                <div class="text-[10px]" style="color: #0ea5e9">(Showing {{ reservations.length }} of total {{ total }})</div>
              </div>
              <div class="flex rounded-lg overflow-hidden border" :style="{ borderColor: 'var(--addrez-border)' }">
                <button @click="viewMode = 'map'" class="px-3 py-1 text-[10px] font-medium border-0 cursor-pointer" :style="{ backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' }">Map</button>
                <button @click="viewMode = 'list'" class="px-3 py-1 text-[10px] font-medium border-0 cursor-pointer" :style="{ backgroundColor: 'var(--addrez-bg-card)', color: 'var(--addrez-text-secondary)' }">List</button>
              </div>
            </div>
            <input v-model="search" placeholder="Name/Mobile" @keyup.enter="loadReservations" class="w-full px-3 py-2 rounded-lg border text-sm mb-3" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            <!-- Reservation cards -->
            <div class="flex-1 overflow-y-auto space-y-2" style="max-height: calc(100vh - 260px)">
              <div v-for="r in reservations" :key="r.id" class="rounded-xl border p-3" :style="{ backgroundColor: 'var(--addrez-bg-card)', borderColor: 'var(--addrez-border)' }">
                <div class="flex items-start justify-between">
                  <div>
                    <div class="flex items-center gap-2">
                      <span class="font-semibold text-sm" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.guest_name || r.customer_name || 'Walk-in' }}</span>
                      <span class="w-2.5 h-2.5 rounded-full" :style="{ backgroundColor: getStatusColor(r.status) }"></span>
                    </div>
                    <div v-if="r.guest_phone" class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.guest_phone }}</div>
                  </div>
                  <div class="flex items-center gap-0.5">
                    <button v-if="r.status === 'Pending'" @click="changeStatus(r.id, 'Confirmed')" class="p-1 rounded bg-transparent border-0 cursor-pointer" style="color: #3b82f6" title="Confirm"><i class="pi pi-check text-xs"></i></button>
                    <button v-if="r.status === 'Confirmed'" @click="changeStatus(r.id, 'CheckedIn')" class="p-1 rounded bg-transparent border-0 cursor-pointer" style="color: #8b5cf6" title="Check In"><i class="pi pi-sign-in text-xs"></i></button>
                    <button v-if="r.status === 'CheckedIn'" @click="changeStatus(r.id, 'Seated')" class="p-1 rounded bg-transparent border-0 cursor-pointer" style="color: #10b981" title="Seat"><i class="pi pi-user-plus text-xs"></i></button>
                    <button v-if="r.status !== 'Cancelled' && r.status !== 'CheckedOut'" @click="changeStatus(r.id, 'Cancelled')" class="p-1 rounded bg-transparent border-0 cursor-pointer" style="color: #ef4444" title="Cancel"><i class="pi pi-times text-xs"></i></button>
                  </div>
                </div>
                <!-- Card details row -->
                <div class="flex items-center gap-4 mt-2 text-[10px] font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">
                  <div class="text-center"><div class="uppercase tracking-wider">Table</div><div :style="{ color: 'var(--addrez-text-primary)' }">{{ r.table_name || '—' }}</div></div>
                  <div class="text-center"><div class="uppercase tracking-wider">Covers</div><div :style="{ color: 'var(--addrez-text-primary)' }">{{ r.covers }}</div></div>
                  <div class="text-center"><div class="uppercase tracking-wider">Arrival</div><div :style="{ color: 'var(--addrez-text-primary)' }">{{ r.time }}</div></div>
                  <div class="text-center"><div class="uppercase tracking-wider">Status</div><div :style="{ color: getStatusColor(r.status) }">{{ r.status }}</div></div>
                </div>
                <!-- Tags -->
                <div v-if="r.tags?.length" class="flex flex-wrap gap-1 mt-2">
                  <span v-for="tag in r.tags" :key="tag.id" class="text-[9px] px-1.5 py-0.5 rounded-full" :style="{ backgroundColor: tag.color + '20', color: tag.color }">{{ tag.name }}</span>
                </div>
              </div>
              <div v-if="reservations.length === 0" class="text-center py-8" :style="{ color: 'var(--addrez-text-secondary)' }">No reservations</div>
            </div>
          </template>

          <!-- === Right panel: Create Reservation Form === -->
          <template v-else>
            <div class="flex items-center justify-between mb-4">
              <h2 class="text-lg font-semibold" style="color: #0ea5e9">
                <i class="pi pi-calendar-plus mr-1"></i>{{ selectedTable ? `Reserve ${selectedTable.name}` : 'New Reservation' }}
              </h2>
              <button @click="cancelCreate" class="p-2 rounded bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times text-lg"></i></button>
            </div>

            <!-- Table info -->
            <div v-if="selectedTable" class="rounded-lg p-3 mb-4 flex items-center gap-2" :style="{ backgroundColor: 'var(--addrez-bg-primary)', border: '1px solid var(--addrez-gold)' }">
              <i class="pi pi-th-large" style="color: var(--addrez-gold)"></i>
              <div class="text-sm"><span class="font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ selectedTable.label || selectedTable.name }}</span> · {{ selectedTable.min_covers }}-{{ selectedTable.max_covers }} covers</div>
            </div>

            <div class="flex-1 overflow-y-auto" style="max-height: calc(100vh - 260px)">
              <form @submit.prevent="createReservation" class="space-y-3">
                <!-- Guest search -->
                <div class="relative">
                  <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Search Guest</label>
                  <div class="flex gap-1">
                    <input v-model="guestQuery" @input="searchGuests" placeholder="Name or phone..." class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: selectedGuest ? 'var(--addrez-gold)' : 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
                    <button v-if="selectedGuest" type="button" @click="clearGuest" class="px-2 rounded border-0 bg-transparent cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times text-sm"></i></button>
                  </div>
                  <div v-if="selectedGuest" class="mt-1 text-xs" style="color: var(--addrez-gold)"><i class="pi pi-user-plus mr-0.5"></i>{{ selectedGuest.fullName }}</div>
                  <div v-if="showGuestResults && guestResults.length" class="absolute z-10 w-full mt-1 rounded-lg border shadow-lg overflow-hidden" :style="{ backgroundColor: 'var(--addrez-bg-card)', borderColor: 'var(--addrez-border)' }">
                    <div v-for="g in guestResults" :key="g.id" @click="selectGuest(g)" class="px-3 py-2.5 cursor-pointer" :style="{ borderBottom: '1px solid var(--addrez-border)' }">
                      <div class="text-sm font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ g.fullName }}</div>
                      <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ g.phone || '' }}</div>
                    </div>
                  </div>
                </div>

                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Name *</label>
                    <input v-model="form.guest_name" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
                  </div>
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Mobile *</label>
                    <input v-model="form.guest_phone" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
                  </div>
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Email</label>
                    <input v-model="form.guest_email" type="email" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
                  </div>
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Gender *</label>
                    <select v-model="form.guest_gender" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
                      <option value="" disabled>Select</option><option value="Male">Male</option><option value="Female">Female</option>
                    </select>
                  </div>
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Date *</label>
                    <input v-model="form.date" type="date" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
                  </div>
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Time *</label>
                    <input v-model="form.time" type="time" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
                  </div>
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Covers *</label>
                    <input v-model.number="form.covers" type="number" min="1" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
                  </div>
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Duration</label>
                    <input v-model.number="form.duration_minutes" type="number" min="15" step="15" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
                  </div>
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Time Slot</label>
                    <select v-model="form.time_slot_id" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
                      <option :value="null">None</option>
                      <option v-for="ts in timeSlots" :key="ts.id" :value="ts.id">{{ ts.name }}</option>
                    </select>
                  </div>
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Seating</label>
                    <div class="flex gap-1">
                      <button type="button" @click="form.seating_type = 'Seated'" class="flex-1 px-3 py-2 rounded-lg text-xs font-medium border cursor-pointer"
                        :style="form.seating_type === 'Seated' ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24', borderColor: 'var(--addrez-gold)' } : { backgroundColor: 'transparent', color: 'var(--addrez-text-secondary)', borderColor: 'var(--addrez-border)' }">Seated</button>
                      <button type="button" @click="form.seating_type = 'Standing'" class="flex-1 px-3 py-2 rounded-lg text-xs font-medium border cursor-pointer"
                        :style="form.seating_type === 'Standing' ? { backgroundColor: '#8b5cf6', color: '#fff', borderColor: '#8b5cf6' } : { backgroundColor: 'transparent', color: 'var(--addrez-text-secondary)', borderColor: 'var(--addrez-border)' }">Standing</button>
                    </div>
                  </div>
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Type</label>
                    <select v-model="form.type" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
                      <option value="DineIn">Dine In</option><option value="Event">Event</option><option value="Birthday">Birthday</option>
                      <option value="Corporate">Corporate</option><option value="Group">Group</option><option value="PrivateDining">Private Dining</option>
                      <option value="Lounge">Lounge</option><option value="Takeaway">Takeaway</option><option value="Inhouse">Inhouse</option>
                    </select>
                  </div>
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Source</label>
                    <select v-model="form.method" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
                      <option value="Phone">Phone</option><option value="WalkIn">Walk-in</option><option value="Online">Online</option>
                      <option value="Email">Email</option><option value="ThirdParty">Third Party</option><option value="SocialMedia">Social Media</option>
                    </select>
                  </div>
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Deposit</label>
                    <input v-model.number="form.deposit_amount" type="number" min="0" step="0.01" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
                  </div>
                  <div>
                    <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">DOB</label>
                    <input v-model="form.guest_date_of_birth" type="date" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
                  </div>
                </div>
                <!-- Tags -->
                <div v-if="allTags.length">
                  <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Tags</label>
                  <div class="flex flex-wrap gap-1.5">
                    <button v-for="tag in allTags" :key="tag.id" type="button" @click="toggleTag(tag.id)"
                      class="px-2.5 py-1 rounded-full text-xs font-medium border cursor-pointer"
                      :style="form.tag_ids.includes(tag.id) ? { backgroundColor: tag.color + '25', borderColor: tag.color, color: tag.color } : { backgroundColor: 'transparent', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-secondary)' }">
                      {{ tag.name }}
                    </button>
                  </div>
                </div>
                <!-- Notes -->
                <div>
                  <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Notes</label>
                  <textarea v-model="form.notes" rows="3" class="w-full px-3 py-2 rounded-lg border text-sm resize-none" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"></textarea>
                </div>
                <div class="flex gap-2 pt-2">
                  <button type="button" @click="cancelCreate" class="flex-1 px-4 py-2.5 rounded-lg text-sm border bg-transparent cursor-pointer font-medium" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
                  <button type="submit" :disabled="saving" class="flex-1 btn-gold text-sm">{{ saving ? 'Creating...' : 'Create Reservation' }}</button>
                </div>
              </form>
            </div>
          </template>
        </div>
      </div>
    </template>

    <!-- ═══════════════════ LIST VIEW — full width ═══════════════════ -->
    <template v-else>
      <!-- List filters -->
      <div class="card mb-4">
        <div class="flex flex-wrap gap-3 items-center">
          <input v-model="search" placeholder="Name/Mobile" class="px-3 py-2 rounded-lg border text-sm min-w-[180px]"
            :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" @keyup.enter="loadReservations" />
          <select v-model="statusFilter" class="px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" @change="loadReservations">
            <option v-for="s in statusOptions" :key="s.value" :value="s.value">{{ s.label }}</option>
          </select>
          <!-- Capacity summary inline -->
          <div v-if="capacity" class="flex items-center gap-3 ml-auto">
            <div class="text-center"><div class="text-sm font-bold" style="color: #0ea5e9">{{ capacity.attendedCustomers }}</div><div class="text-[9px]" :style="{ color: 'var(--addrez-text-secondary)' }">Attended</div></div>
            <div class="text-center"><div class="text-sm font-bold" :style="{ color: 'var(--addrez-text-primary)' }">{{ capacity.totalCovers }}</div><div class="text-[9px]" :style="{ color: 'var(--addrez-text-secondary)' }">Total</div></div>
          </div>
          <div class="flex rounded-lg overflow-hidden border" :style="{ borderColor: 'var(--addrez-border)' }">
            <button @click="slotScope = 'current'" class="px-3 py-1.5 text-xs border-0 cursor-pointer" :style="slotScope === 'current' ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' } : { backgroundColor: 'var(--addrez-bg-card)', color: 'var(--addrez-text-secondary)' }">Current Slot</button>
            <button @click="slotScope = 'all'" class="px-3 py-1.5 text-xs border-0 cursor-pointer" :style="slotScope === 'all' ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' } : { backgroundColor: 'var(--addrez-bg-card)', color: 'var(--addrez-text-secondary)' }">All Slots</button>
          </div>
        </div>
      </div>

      <!-- Reservation table -->
      <div class="card overflow-x-auto">
        <table class="w-full text-sm">
          <thead><tr :style="{ color: 'var(--addrez-text-secondary)' }">
            <th class="text-left py-3 px-3 font-medium">Reservation Name</th>
            <th class="text-left py-3 px-3 font-medium">Arrival Time</th>
            <th class="text-center py-3 px-3 font-medium">Covers</th>
            <th class="text-center py-3 px-3 font-medium">Attended(M/F)</th>
            <th class="text-left py-3 px-3 font-medium">Table</th>
            <th class="text-left py-3 px-3 font-medium">Notes</th>
            <th class="text-left py-3 px-3 font-medium">Tags</th>
            <th class="text-right py-3 px-3 font-medium">Actions</th>
          </tr></thead>
          <tbody>
            <tr v-for="r in reservations" :key="r.id" class="border-t" :style="{ borderColor: 'var(--addrez-border)' }">
              <td class="py-3 px-3">
                <div class="flex items-center gap-2">
                  <span class="w-2 h-2 rounded-full" :style="{ backgroundColor: getStatusColor(r.status) }"></span>
                  <div>
                    <div class="font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.guest_name || r.customer_name || 'Walk-in' }}</div>
                    <div v-if="r.guest_phone" class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.guest_phone }}</div>
                  </div>
                </div>
              </td>
              <td class="py-3 px-3" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.time }}</td>
              <td class="py-3 px-3 text-center" :style="{ color: 'var(--addrez-text-primary)' }">{{ r.covers }}</td>
              <td class="py-3 px-3 text-center" :style="{ color: 'var(--addrez-text-secondary)' }">0 (0/0)</td>
              <td class="py-3 px-3" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.table_name || '—' }}</td>
              <td class="py-3 px-3 text-xs max-w-[150px] truncate" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.notes || '—' }}</td>
              <td class="py-3 px-3">
                <div class="flex flex-wrap gap-1">
                  <span v-for="tag in r.tags" :key="tag.id" class="text-[10px] px-1.5 py-0.5 rounded-full" :style="{ backgroundColor: tag.color + '20', color: tag.color }">{{ tag.name }}</span>
                  <span v-if="!r.tags?.length" class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">—</span>
                </div>
              </td>
              <td class="py-3 px-3 text-right">
                <div class="flex items-center justify-end gap-1">
                  <button v-if="r.status === 'Pending'" @click="changeStatus(r.id, 'Confirmed')" class="px-2 py-1 rounded text-xs bg-transparent border-0 cursor-pointer" style="color: #3b82f6" title="Confirm"><i class="pi pi-check"></i></button>
                  <button v-if="r.status === 'Confirmed'" @click="changeStatus(r.id, 'CheckedIn')" class="px-2 py-1 rounded text-xs bg-transparent border-0 cursor-pointer" style="color: #8b5cf6" title="Check In"><i class="pi pi-sign-in"></i></button>
                  <button v-if="r.status === 'CheckedIn'" @click="changeStatus(r.id, 'Seated')" class="px-2 py-1 rounded text-xs bg-transparent border-0 cursor-pointer" style="color: #10b981" title="Seat"><i class="pi pi-user-plus"></i></button>
                  <button v-if="r.status !== 'Cancelled' && r.status !== 'CheckedOut'" @click="changeStatus(r.id, 'Cancelled')" class="px-2 py-1 rounded text-xs bg-transparent border-0 cursor-pointer" style="color: #ef4444" title="Cancel"><i class="pi pi-times"></i></button>
                </div>
              </td>
            </tr>
            <tr v-if="reservations.length === 0"><td colspan="8" class="text-center py-8" :style="{ color: 'var(--addrez-text-secondary)' }">No reservations found</td></tr>
          </tbody>
        </table>
        <div v-if="total > perPage" class="flex items-center justify-between pt-4 mt-4 border-t" :style="{ borderColor: 'var(--addrez-border)' }">
          <span class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ total }} total</span>
          <div class="flex gap-1">
            <button @click="page--; loadReservations()" :disabled="page <= 1" class="px-3 py-1 rounded text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Prev</button>
            <button @click="page++; loadReservations()" :disabled="page * perPage >= total" class="px-3 py-1 rounded text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Next</button>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
