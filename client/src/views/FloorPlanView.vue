<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, computed, watch, nextTick } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import type { Layout, TableItem, TimeSlot, CapacityData, GuestSearchResult, SeatingType, Tag } from '@/types'

const toast = useToast()

// ── Auto-fit canvas scaling ──
const fpContainerRef = ref<HTMLElement | null>(null)
const fpContainerWidth = ref(800)
let fpResizeObserver: ResizeObserver | null = null

const fpScale = computed(() => {
  if (!activeFloorPlan.value) return 1
  const cw = activeFloorPlan.value.width
  const ch = activeFloorPlan.value.height
  const availW = fpContainerWidth.value
  const maxH = Math.max(300, window.innerHeight - 300)
  return Math.min(availW / cw, maxH / ch, 1)
})

function initFpResize() {
  if (fpContainerRef.value) {
    fpContainerWidth.value = fpContainerRef.value.clientWidth
    fpResizeObserver = new ResizeObserver(entries => {
      for (const entry of entries) fpContainerWidth.value = entry.contentRect.width
    })
    fpResizeObserver.observe(fpContainerRef.value)
  }
}

const layouts = ref<Layout[]>([])
const timeSlots = ref<TimeSlot[]>([])
const activeLayoutIdx = ref(0)
const activeFloorPlanIdx = ref(0)
const activeSlotId = ref<number | null>(null)
const loading = ref(true)
const capacity = ref<CapacityData | null>(null)

const activeLayout = computed(() => layouts.value[activeLayoutIdx.value])
const activeFloorPlan = computed(() => activeLayout.value?.floor_plans[activeFloorPlanIdx.value])

const statusColors: Record<string, string> = {
  Confirmed: '#3b82f6', CheckedIn: '#8b5cf6', Seated: '#10b981'
}

// ── Reservation modal state ──
const showReserveDialog = ref(false)
const reserveSaving = ref(false)
const selectedTable = ref<TableItem | null>(null)
const guestQuery = ref('')
const guestResults = ref<GuestSearchResult[]>([])
const selectedGuest = ref<GuestSearchResult | null>(null)
const showGuestResults = ref(false)
const allTags = ref<Tag[]>([])

const reserveForm = ref({
  customer_id: null as number | null,
  guest_name: '', guest_email: '', guest_phone: '',
  covers: 2, date: '', time: '', duration_minutes: 90,
  type: 'DineIn' as const, seating_type: 'Seated' as SeatingType,
  method: 'Phone' as const, notes: '',
  time_slot_id: null as number | null, table_id: null as number | null,
  tag_ids: [] as number[]
})

function openReserveDialog(table: TableItem) {
  if (table.current_status) return // already occupied
  selectedTable.value = table
  const today = new Date().toISOString().split('T')[0]
  reserveForm.value = {
    customer_id: null, guest_name: '', guest_email: '', guest_phone: '',
    covers: table.min_covers, date: today, time: '', duration_minutes: 90,
    type: 'DineIn', seating_type: 'Seated', method: 'Phone', notes: '',
    time_slot_id: activeSlotId.value, table_id: table.id, tag_ids: []
  }
  guestQuery.value = ''
  selectedGuest.value = null
  showReserveDialog.value = true
}

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
  reserveForm.value.customer_id = guest.id
  reserveForm.value.guest_name = guest.fullName
  reserveForm.value.guest_phone = guest.phone || ''
  reserveForm.value.guest_email = guest.email || ''
  guestQuery.value = guest.fullName
  showGuestResults.value = false
}

function clearGuest() {
  selectedGuest.value = null
  reserveForm.value.customer_id = null
  guestQuery.value = ''
}

async function createReservation() {
  reserveSaving.value = true
  try {
    await api.post('/reservations', reserveForm.value)
    toast.add({ severity: 'success', summary: 'Created', detail: `Reservation on ${selectedTable.value?.name} created`, life: 3000 })
    showReserveDialog.value = false
    await loadLayouts()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Failed to create reservation', life: 3000 })
  } finally { reserveSaving.value = false }
}

function toggleReserveTag(id: number) {
  const idx = reserveForm.value.tag_ids.indexOf(id)
  if (idx >= 0) reserveForm.value.tag_ids.splice(idx, 1)
  else reserveForm.value.tag_ids.push(id)
}

async function loadLayouts() {
  loading.value = true
  try {
    const [layoutRes, slotRes, tagRes] = await Promise.all([
      api.get<Layout[]>('/floor-plans/layouts'),
      api.get<TimeSlot[]>('/time-slots'),
      api.get<Tag[]>('/settings/tags')
    ])
    allTags.value = tagRes.data
    layouts.value = layoutRes.data
    timeSlots.value = slotRes.data.filter((s: TimeSlot) => s.is_active)
    const defIdx = layoutRes.data.findIndex((l: Layout) => l.is_default)
    if (defIdx >= 0) activeLayoutIdx.value = defIdx
    await loadCapacity()
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load floor plans', life: 3000 })
  } finally {
    loading.value = false
  }
}

async function loadCapacity() {
  try {
    const params: Record<string, number> = {}
    if (activeSlotId.value) params.timeSlotId = activeSlotId.value
    const { data } = await api.get<CapacityData>('/reservations/capacity', { params })
    capacity.value = data
  } catch { /* silent */ }
}

watch(activeSlotId, () => loadCapacity())

function getTableStyle(table: TableItem) {
  const sc = table.current_status ? statusColors[table.current_status] || 'var(--addrez-border)' : 'var(--addrez-border)'
  const isRound = table.shape === 'Circle' || table.shape === 'Oval' || table.shape === 'Round'
  return {
    position: 'absolute' as const,
    left: `${table.x}px`,
    top: `${table.y}px`,
    width: `${table.width}px`,
    height: `${table.height}px`,
    transform: `rotate(${table.rotation}deg)`,
    borderRadius: isRound ? '50%' : '8px',
    border: `2.5px solid ${sc}`,
    backgroundColor: table.current_status ? sc + '22' : 'var(--addrez-bg-card)',
    boxShadow: `inset 0 0 0 2px ${table.current_status ? sc + '18' : 'var(--addrez-border)'}`,
    cursor: table.current_status ? 'default' : 'pointer',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    flexDirection: 'column' as const,
    transition: 'all 0.2s'
  }
}

onMounted(() => { loadLayouts(); nextTick(initFpResize) })
onBeforeUnmount(() => { fpResizeObserver?.disconnect() })
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">Floor Plan</h1>
      <!-- Slot filter -->
      <div v-if="timeSlots.length > 0" class="flex items-center gap-2">
        <span class="text-xs font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Slot:</span>
        <select
          :value="activeSlotId ?? ''"
          @change="activeSlotId = ($event.target as HTMLSelectElement).value ? Number(($event.target as HTMLSelectElement).value) : null"
          class="px-3 py-1.5 rounded-lg text-sm border bg-transparent"
          :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
        >
          <option value="">All Slots</option>
          <option v-for="ts in timeSlots" :key="ts.id" :value="ts.id">{{ ts.name }} ({{ ts.start_time }} - {{ ts.end_time }})</option>
        </select>
      </div>
    </div>

    <!-- Live Capacity Bar -->
    <div v-if="capacity" class="grid grid-cols-4 md:grid-cols-8 gap-2 mb-4">
      <div class="card !p-3 text-center">
        <div class="text-lg font-bold" style="color: var(--addrez-gold)">{{ capacity.totalCovers }}</div>
        <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Total Covers</div>
      </div>
      <div class="card !p-3 text-center">
        <div class="text-lg font-bold" style="color: #3b82f6">{{ capacity.totalSeated }}</div>
        <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Seated</div>
      </div>
      <div class="card !p-3 text-center">
        <div class="text-lg font-bold" style="color: #8b5cf6">{{ capacity.totalStanding }}</div>
        <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Standing</div>
      </div>
      <div class="card !p-3 text-center">
        <div class="text-lg font-bold" style="color: #10b981">{{ capacity.tablesReserved }}/{{ capacity.totalTables }}</div>
        <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Tables</div>
      </div>
      <div class="card !p-3 text-center">
        <div class="text-lg font-bold" style="color: #10b981">{{ capacity.attendedCustomers }}</div>
        <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Attended</div>
      </div>
      <div class="card !p-3 text-center">
        <div class="text-lg font-bold" style="color: #06b6d4">{{ capacity.walkInCustomers }}</div>
        <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Walk-ins</div>
      </div>
      <div class="card !p-3 text-center">
        <div class="text-lg font-bold" style="color: #f59e0b">{{ capacity.pendingCount }}</div>
        <div class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">Pending</div>
      </div>
    </div>

    <div v-if="loading" class="text-center py-12">
      <i class="pi pi-spin pi-spinner text-3xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
    </div>

    <template v-else-if="layouts.length > 0">
      <!-- Layout tabs -->
      <div class="flex gap-2 mb-4">
        <button
          v-for="(layout, i) in layouts" :key="layout.id"
          @click="activeLayoutIdx = i; activeFloorPlanIdx = 0"
          class="px-4 py-2 rounded-lg text-sm font-medium border-0 cursor-pointer transition-colors"
          :style="activeLayoutIdx === i
            ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' }
            : { backgroundColor: 'var(--addrez-bg-card)', color: 'var(--addrez-text-secondary)' }"
        >
          {{ layout.name }}
        </button>
      </div>

      <!-- Floor plan tabs -->
      <div v-if="activeLayout && activeLayout.floor_plans.length > 1" class="flex gap-2 mb-4">
        <button
          v-for="(fp, i) in activeLayout.floor_plans" :key="fp.id"
          @click="activeFloorPlanIdx = i"
          class="px-3 py-1.5 rounded-lg text-xs font-medium border cursor-pointer transition-colors bg-transparent"
          :style="activeFloorPlanIdx === i
            ? { borderColor: 'var(--addrez-gold)', color: 'var(--addrez-gold)' }
            : { borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-secondary)' }"
        >
          {{ fp.name }}
        </button>
      </div>

      <!-- Floor plan canvas -->
      <div v-if="activeFloorPlan" ref="fpContainerRef" class="card overflow-hidden">
        <div :style="{ width: (activeFloorPlan.width * fpScale) + 'px', height: (activeFloorPlan.height * fpScale) + 'px', overflow: 'hidden' }">
        <div
          class="relative"
          :style="{
            width: activeFloorPlan.width + 'px',
            height: activeFloorPlan.height + 'px',
            backgroundColor: activeFloorPlan.background_color || 'var(--addrez-bg-primary)',
            transform: `scale(${fpScale})`,
            transformOrigin: 'top left'
          }"
        >
          <!-- Tables -->
          <div
            v-for="table in activeFloorPlan.tables" :key="table.id"
            :style="getTableStyle(table)"
            :title="`${table.name} (${table.min_covers}-${table.max_covers} covers)${table.current_reservation_guest ? '\n' + table.current_reservation_guest : ''}${!table.current_status ? '\nClick to reserve' : ''}`"
            @click="openReserveDialog(table)"
          >
            <span class="text-sm font-bold leading-tight" :style="{ color: table.current_status ? statusColors[table.current_status] : 'var(--addrez-text-primary)' }">
              {{ table.label || table.name }}
            </span>
            <span class="text-xs leading-tight" :style="{ color: 'var(--addrez-text-secondary)' }">{{ table.max_covers }}</span>
            <span v-if="table.current_reservation_guest" class="text-[10px] mt-0.5 leading-tight" :style="{ color: statusColors[table.current_status!] }">
              {{ table.current_reservation_guest }}
            </span>
          </div>

          <!-- Landmarks -->
          <div
            v-for="lm in activeFloorPlan.landmarks" :key="lm.id"
            class="absolute flex items-center justify-center"
            :style="{
              left: lm.x + 'px', top: lm.y + 'px',
              width: lm.width + 'px', height: lm.height + 'px',
              transform: `rotate(${lm.rotation}deg)`,
              border: '2px solid #64748b90',
              borderRadius: '6px',
              backgroundColor: '#64748b20'
            }"
          >
            <span class="text-xs font-semibold" style="color: #64748b">{{ lm.name }}</span>
          </div>
        </div>
        </div>

        <!-- Legend -->
        <div class="flex gap-4 mt-4 pt-4 border-t" :style="{ borderColor: 'var(--addrez-border)' }">
          <div class="flex items-center gap-1 text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">
            <div class="w-3 h-3 rounded border-2" :style="{ borderColor: 'var(--addrez-border)' }"></div> Available
          </div>
          <div class="flex items-center gap-1 text-xs" style="color: #3b82f6">
            <div class="w-3 h-3 rounded border-2" style="border-color: #3b82f6; background: #3b82f620"></div> Confirmed
          </div>
          <div class="flex items-center gap-1 text-xs" style="color: #8b5cf6">
            <div class="w-3 h-3 rounded border-2" style="border-color: #8b5cf6; background: #8b5cf620"></div> Checked In
          </div>
          <div class="flex items-center gap-1 text-xs" style="color: #10b981">
            <div class="w-3 h-3 rounded border-2" style="border-color: #10b981; background: #10b98120"></div> Seated
          </div>
        </div>
      </div>
    </template>

    <div v-else class="card text-center py-12">
      <i class="pi pi-th-large text-4xl mb-3" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
      <p :style="{ color: 'var(--addrez-text-secondary)' }">No floor plans configured. Set up layouts in Settings.</p>
    </div>

    <!-- Reserve from Table Dialog -->
    <div v-if="showReserveDialog && selectedTable" class="modal-overlay">
      <div class="card w-full max-w-lg mx-4 max-h-[85vh] overflow-y-auto" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">
            <i class="pi pi-calendar-plus mr-2"></i>Reserve {{ selectedTable.name }}
          </h3>
          <button @click="showReserveDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>

        <!-- Table info banner -->
        <div class="rounded-lg p-3 mb-4 flex items-center gap-3" :style="{ backgroundColor: 'var(--addrez-bg-primary)', border: '1px solid var(--addrez-gold)' }">
          <i class="pi pi-th-large text-lg" style="color: var(--addrez-gold)"></i>
          <div>
            <div class="text-sm font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ selectedTable.label || selectedTable.name }}</div>
            <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ selectedTable.min_covers }}-{{ selectedTable.max_covers }} covers · {{ selectedTable.table_type_name || selectedTable.shape }}</div>
          </div>
        </div>

        <form @submit.prevent="createReservation" class="space-y-3">
          <!-- Guest Search -->
          <div class="relative">
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Search Guest</label>
            <div class="flex gap-2">
              <input v-model="guestQuery" @input="searchGuests" placeholder="Type name or phone..."
                class="w-full px-3 py-2 rounded-lg border text-sm"
                :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: selectedGuest ? 'var(--addrez-gold)' : 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
              <button v-if="selectedGuest" type="button" @click="clearGuest" class="px-2 rounded-lg border-0 bg-transparent cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
            </div>
            <div v-if="selectedGuest" class="mt-1 text-xs" style="color: var(--addrez-gold)"><i class="pi pi-user-plus mr-1"></i>Linked: {{ selectedGuest.fullName }}</div>
            <div v-if="showGuestResults && guestResults.length" class="absolute z-10 w-full mt-1 rounded-lg border shadow-lg overflow-hidden" :style="{ backgroundColor: 'var(--addrez-bg-card)', borderColor: 'var(--addrez-border)' }">
              <div v-for="g in guestResults" :key="g.id" @click="selectGuest(g)" class="px-3 py-2 cursor-pointer hover:opacity-80" :style="{ borderBottom: '1px solid var(--addrez-border)' }">
                <div class="text-sm font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ g.fullName }}</div>
                <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ g.phone || '' }} {{ g.email ? '· ' + g.email : '' }}</div>
              </div>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div class="col-span-2">
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Guest Name</label>
              <input v-model="reserveForm.guest_name" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Phone</label>
              <input v-model="reserveForm.guest_phone" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Email</label>
              <input v-model="reserveForm.guest_email" type="email" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Date *</label>
              <input v-model="reserveForm.date" type="date" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Time *</label>
              <input v-model="reserveForm.time" type="time" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Covers *</label>
              <input v-model.number="reserveForm.covers" type="number" :min="selectedTable.min_covers" :max="selectedTable.max_covers" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Duration (min)</label>
              <input v-model.number="reserveForm.duration_minutes" type="number" min="15" step="15" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Time Slot</label>
              <select v-model="reserveForm.time_slot_id" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
                <option :value="null">None</option>
                <option v-for="ts in timeSlots" :key="ts.id" :value="ts.id">{{ ts.name }}</option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Method</label>
              <select v-model="reserveForm.method" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
                <option value="Phone">Phone</option>
                <option value="WalkIn">Walk-in</option>
                <option value="Online">Online</option>
                <option value="Email">Email</option>
              </select>
            </div>
            <!-- Tags -->
            <div v-if="allTags.length" class="col-span-2">
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Tags</label>
              <div class="flex flex-wrap gap-1.5">
                <button v-for="tag in allTags" :key="tag.id" type="button" @click="toggleReserveTag(tag.id)"
                  class="px-2 py-1 rounded-full text-xs font-medium border cursor-pointer transition-all"
                  :style="reserveForm.tag_ids.includes(tag.id)
                    ? { backgroundColor: tag.color + '25', borderColor: tag.color, color: tag.color }
                    : { backgroundColor: 'transparent', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-secondary)' }">
                  <i :class="reserveForm.tag_ids.includes(tag.id) ? 'pi pi-check mr-1' : ''" class="text-[10px]"></i>{{ tag.name }}
                </button>
              </div>
            </div>
            <div class="col-span-2">
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Notes</label>
              <textarea v-model="reserveForm.notes" rows="2" class="w-full px-3 py-2 rounded-lg border text-sm resize-none" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"></textarea>
            </div>
          </div>
          <div class="flex justify-end gap-2 pt-2">
            <button type="button" @click="showReserveDialog = false" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
            <button type="submit" :disabled="reserveSaving" class="btn-gold text-sm">{{ reserveSaving ? 'Creating...' : 'Create Reservation' }}</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
