<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import type { TimeSlotFull, Layout } from '@/types'
import ConfirmDialog from '@/components/ConfirmDialog.vue'

const toast = useToast()
const timeSlots = ref<TimeSlotFull[]>([])
const layouts = ref<Layout[]>([])
const allOutlets = ref<{ id: number; name: string }[]>([])
const loading = ref(true)
const showDialog = ref(false)
const editingId = ref<number | null>(null)
const saving = ref(false)

const filterOutletId = ref<number | null>(null)
const sortBy = ref<string>('time')

const dayLabels = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
const dayKeys = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday'] as const

const defaultForm = () => ({
  name: '', start_time: '12:00', end_time: '15:00', layout_id: null as number | null,
  monday: true, tuesday: true, wednesday: true, thursday: true, friday: true, saturday: true, sunday: true,
  start_date: '', end_date: '',
  max_covers: 50, max_reservations: 25, turn_time_minutes: 90, grace_period_minutes: 15,
  require_deposit: false, deposit_amount_per_person: 0, excluded_category_ids: [] as number[],
  is_active: true,
  apply_all: true,
  outlet_ids: [] as number[]
})
const form = ref(defaultForm())

const filteredSlots = computed(() => {
  let result = timeSlots.value
  if (filterOutletId.value) result = result.filter(ts => ts.outlet_id === filterOutletId.value)
  return result
})

const outletOptions = computed(() => {
  const ids = new Set(timeSlots.value.map(ts => ts.outlet_id))
  return allOutlets.value.filter(o => ids.has(o.id))
})

function getDays(ts: TimeSlotFull): string[] {
  const days: string[] = []
  if (ts.monday) days.push('Mon')
  if (ts.tuesday) days.push('Tue')
  if (ts.wednesday) days.push('Wed')
  if (ts.thursday) days.push('Thu')
  if (ts.friday) days.push('Fri')
  if (ts.saturday) days.push('Sat')
  if (ts.sunday) days.push('Sun')
  return days
}

async function loadTimeSlots() {
  loading.value = true
  try {
    const [slotRes, layoutRes, outletRes] = await Promise.all([
      api.get<TimeSlotFull[]>('/time-slots/all', { params: { sort: sortBy.value } }),
      api.get<Layout[]>('/floor-plans/layouts'),
      api.get<{ id: number; name: string }[]>('/settings/outlets')
    ])
    timeSlots.value = slotRes.data
    layouts.value = layoutRes.data
    allOutlets.value = outletRes.data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load time slots', life: 3000 })
  } finally { loading.value = false }
}

function changeSort(s: string) {
  sortBy.value = s
  loadTimeSlots()
}

function openCreate() {
  editingId.value = null
  form.value = defaultForm()
  showDialog.value = true
}

function openEdit(ts: TimeSlotFull) {
  editingId.value = ts.id
  form.value = {
    name: ts.name, start_time: ts.start_time, end_time: ts.end_time, layout_id: ts.layout_id || null,
    monday: ts.monday, tuesday: ts.tuesday, wednesday: ts.wednesday, thursday: ts.thursday,
    friday: ts.friday, saturday: ts.saturday, sunday: ts.sunday,
    start_date: ts.start_date || '', end_date: ts.end_date || '',
    max_covers: ts.max_covers, max_reservations: ts.max_reservations,
    turn_time_minutes: ts.turn_time_minutes, grace_period_minutes: ts.grace_period_minutes,
    require_deposit: ts.require_deposit, deposit_amount_per_person: ts.deposit_amount_per_person,
    excluded_category_ids: ts.excluded_category_ids || [], is_active: ts.is_active,
    apply_all: false, outlet_ids: [ts.outlet_id]
  }
  showDialog.value = true
}

async function save() {
  if (!form.value.name.trim()) return
  saving.value = true
  try {
    const payload: any = { ...form.value }
    if (!editingId.value) {
      payload.outlet_ids = form.value.apply_all ? allOutlets.value.map(o => o.id) : form.value.outlet_ids
    }
    delete payload.apply_all
    if (editingId.value) {
      delete payload.outlet_ids
      await api.put(`/time-slots/${editingId.value}`, payload)
      toast.add({ severity: 'success', summary: 'Updated', detail: 'Time slot updated', life: 2000 })
    } else {
      await api.post('/time-slots', payload)
      toast.add({ severity: 'success', summary: 'Created', detail: 'Time slot created', life: 2000 })
    }
    showDialog.value = false
    await loadTimeSlots()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Save failed', life: 3000 })
  } finally { saving.value = false }
}

const confirmVisible = ref(false)
const confirmTarget = ref<TimeSlotFull | null>(null)
function requestDelete(ts: TimeSlotFull) { confirmTarget.value = ts; confirmVisible.value = true }
async function doDelete() {
  const ts = confirmTarget.value; confirmVisible.value = false; if (!ts) return
  try {
    await api.delete(`/time-slots/${ts.id}`)
    toast.add({ severity: 'success', summary: 'Deleted', detail: 'Time slot deleted', life: 2000 })
    await loadTimeSlots()
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Delete failed', life: 3000 })
  }
}

function toggleDay(key: typeof dayKeys[number]) {
  (form.value as any)[key] = !(form.value as any)[key]
}

function toggleOutlet(id: number) {
  const idx = form.value.outlet_ids.indexOf(id)
  if (idx >= 0) form.value.outlet_ids.splice(idx, 1)
  else form.value.outlet_ids.push(id)
}

onMounted(loadTimeSlots)
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-4">
      <h1 class="text-2xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">Time Slots</h1>
      <button @click="openCreate" class="btn-gold text-sm"><i class="pi pi-plus mr-1"></i>Add Time Slot</button>
    </div>

    <!-- Filter & Sort Bar -->
    <div class="card mb-4">
      <div class="flex items-center gap-3 flex-wrap">
        <div class="flex items-center gap-1.5">
          <label class="text-xs font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Outlet:</label>
          <select v-model="filterOutletId" class="px-2 py-1 rounded-lg border text-xs" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
            <option :value="null">All Outlets</option>
            <option v-for="o in outletOptions" :key="o.id" :value="o.id">{{ o.name }}</option>
          </select>
        </div>
        <div class="flex items-center gap-1.5">
          <label class="text-xs font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Sort:</label>
          <div class="flex gap-0.5">
            <button v-for="s in [{key:'time',label:'Time'},{key:'name',label:'Name'},{key:'outlet',label:'Outlet'}]" :key="s.key" @click="changeSort(s.key)"
              class="px-2 py-1 rounded text-[10px] font-medium border-0 cursor-pointer transition-colors"
              :style="sortBy === s.key ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' } : { backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)' }"
            >{{ s.label }}</button>
          </div>
        </div>
        <span class="text-xs ml-auto" :style="{ color: 'var(--addrez-text-secondary)' }">{{ filteredSlots.length }} slot{{ filteredSlots.length !== 1 ? 's' : '' }}</span>
      </div>
    </div>

    <div v-if="loading" class="text-center py-12">
      <i class="pi pi-spin pi-spinner text-3xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
    </div>

    <div v-else-if="filteredSlots.length === 0" class="card text-center py-12">
      <i class="pi pi-clock text-4xl mb-3" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
      <p :style="{ color: 'var(--addrez-text-secondary)' }">No time slots found. Click "Add Time Slot" to create one.</p>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div v-for="ts in filteredSlots" :key="ts.id" class="card">
        <div class="flex items-start justify-between mb-3">
          <div>
            <h3 class="text-base font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ ts.name }}</h3>
            <p class="text-sm mt-0.5" style="color: var(--addrez-gold)">{{ ts.start_time }} – {{ ts.end_time }}</p>
          </div>
          <div class="flex items-center gap-1">
            <span class="text-[10px] px-1.5 py-0.5 rounded font-medium" :style="{ backgroundColor: '#3b82f615', color: '#3b82f6' }">{{ ts.outlet_name }}</span>
            <span class="text-xs px-2 py-1 rounded-full font-medium"
              :style="ts.is_active ? { backgroundColor: '#10b98120', color: '#10b981' } : { backgroundColor: '#6b728020', color: '#6b7280' }">
              {{ ts.is_active ? 'Active' : 'Inactive' }}
            </span>
            <button @click="openEdit(ts)" class="p-1.5 rounded bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-pencil text-xs"></i></button>
            <button @click="requestDelete(ts)" class="p-1.5 rounded bg-transparent border-0 cursor-pointer" style="color: #ef4444"><i class="pi pi-trash text-xs"></i></button>
          </div>
        </div>

        <!-- Days -->
        <div class="flex gap-1 mb-3">
          <span v-for="day in dayLabels" :key="day"
            class="text-[10px] px-1.5 py-0.5 rounded font-medium"
            :style="getDays(ts).includes(day)
              ? { backgroundColor: 'var(--addrez-gold)' + '20', color: 'var(--addrez-gold)' }
              : { backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)', opacity: '0.5' }">
            {{ day }}
          </span>
        </div>

        <!-- Details -->
        <div class="space-y-1.5 text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">
          <div class="flex justify-between"><span>Layout</span><span :style="{ color: 'var(--addrez-text-primary)' }">{{ ts.layout_name || 'Default' }}</span></div>
          <div class="flex justify-between"><span>Max Covers</span><span :style="{ color: 'var(--addrez-text-primary)' }">{{ ts.max_covers }}</span></div>
          <div class="flex justify-between"><span>Max Reservations</span><span :style="{ color: 'var(--addrez-text-primary)' }">{{ ts.max_reservations }}</span></div>
          <div class="flex justify-between"><span>Turn Time</span><span :style="{ color: 'var(--addrez-text-primary)' }">{{ ts.turn_time_minutes }} min</span></div>
          <div class="flex justify-between"><span>Grace Period</span><span :style="{ color: 'var(--addrez-text-primary)' }">{{ ts.grace_period_minutes }} min</span></div>
          <div v-if="ts.require_deposit" class="flex justify-between"><span>Deposit</span><span style="color: var(--addrez-gold)">EGP {{ ts.deposit_amount_per_person }}/person</span></div>
        </div>
      </div>
    </div>

    <!-- Create/Edit Dialog -->
    <div v-if="showDialog" class="fixed inset-0 z-50 flex items-center justify-center" style="background: rgba(0,0,0,0.5)">
      <div class="card w-full max-w-lg mx-4 max-h-[85vh] overflow-y-auto" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ editingId ? 'Edit Time Slot' : 'New Time Slot' }}</h3>
          <button @click="showDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="save" class="space-y-3">
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Name *</label>
            <input v-model="form.name" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" placeholder="e.g. Lunch, Dinner" />
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Start Time *</label>
              <input v-model="form.start_time" type="time" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">End Time *</label>
              <input v-model="form.end_time" type="time" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
          </div>
          <!-- Days -->
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Active Days</label>
            <div class="flex gap-1">
              <button v-for="(day, i) in dayLabels" :key="day" type="button" @click="toggleDay(dayKeys[i])"
                class="flex-1 py-1.5 rounded text-xs font-medium border-0 cursor-pointer transition-colors"
                :style="(form as any)[dayKeys[i]]
                  ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' }
                  : { backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)' }">
                {{ day }}
              </button>
            </div>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Max Covers</label>
              <input v-model.number="form.max_covers" type="number" min="1" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Max Reservations</label>
              <input v-model.number="form.max_reservations" type="number" min="1" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Turn Time (min)</label>
              <input v-model.number="form.turn_time_minutes" type="number" min="15" step="15" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Grace Period (min)</label>
              <input v-model.number="form.grace_period_minutes" type="number" min="5" step="5" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
          </div>
          <!-- Layout -->
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Layout</label>
            <select v-model="form.layout_id" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
              <option :value="null">Default</option>
              <option v-for="l in layouts" :key="l.id" :value="l.id">{{ l.name }}</option>
            </select>
          </div>
          <!-- Deposit -->
          <div class="flex items-center gap-3">
            <label class="flex items-center gap-2 text-sm cursor-pointer" :style="{ color: 'var(--addrez-text-primary)' }">
              <input type="checkbox" v-model="form.require_deposit" /> Require Deposit
            </label>
            <input v-if="form.require_deposit" v-model.number="form.deposit_amount_per_person" type="number" min="0" step="10" placeholder="EGP per person"
              class="w-36 px-3 py-1.5 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
          </div>
          <!-- Outlet Assignment (Create only) -->
          <div v-if="!editingId">
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Apply To</label>
            <div class="flex items-center gap-3 mb-2">
              <label class="flex items-center gap-1.5 text-sm cursor-pointer" :style="{ color: 'var(--addrez-text-primary)' }">
                <input type="radio" :value="true" v-model="form.apply_all" /> All Outlets
              </label>
              <label class="flex items-center gap-1.5 text-sm cursor-pointer" :style="{ color: 'var(--addrez-text-primary)' }">
                <input type="radio" :value="false" v-model="form.apply_all" /> Select Outlets
              </label>
            </div>
            <div v-if="!form.apply_all" class="flex flex-wrap gap-2">
              <button v-for="o in allOutlets" :key="o.id" type="button" @click="toggleOutlet(o.id)"
                class="px-2.5 py-1 rounded-lg text-xs font-medium border-0 cursor-pointer transition-colors"
                :style="form.outlet_ids.includes(o.id)
                  ? { backgroundColor: '#3b82f6', color: '#fff' }
                  : { backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)' }">
                {{ o.name }}
              </button>
            </div>
          </div>
          <div v-if="editingId" class="flex items-center gap-2">
            <label class="flex items-center gap-2 text-sm cursor-pointer" :style="{ color: 'var(--addrez-text-primary)' }">
              <input type="checkbox" v-model="form.is_active" /> Active
            </label>
          </div>
          <div class="flex justify-end gap-2 pt-2">
            <button type="button" @click="showDialog = false" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
            <button type="submit" :disabled="saving" class="btn-gold text-sm">{{ saving ? 'Saving...' : editingId ? 'Update' : 'Create' }}</button>
          </div>
        </form>
      </div>
    </div>
    <ConfirmDialog :visible="confirmVisible" title="Delete Time Slot" :message="`Delete time slot '${confirmTarget?.name}'?`" @confirm="doDelete" @cancel="confirmVisible = false" />
  </div>
</template>
