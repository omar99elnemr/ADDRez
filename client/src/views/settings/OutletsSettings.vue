<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import type { Outlet } from '@/types'
import ConfirmDialog from '@/components/ConfirmDialog.vue'

const toast = useToast()
const outlets = ref<Outlet[]>([])
const loading = ref(true)
const showDialog = ref(false)
const editingId = ref<number | null>(null)
const saving = ref(false)

const form = ref({
  name: '', address: '', phone: '', email: '',
  default_grace_period_minutes: 15, default_turn_time_minutes: 90,
  auto_confirm_online: false, is_active: true,
  area_names: ['Main'] as string[]
})

async function load() {
  loading.value = true
  try {
    const { data } = await api.get<Outlet[]>('/settings/outlets')
    outlets.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load outlets', life: 3000 })
  } finally { loading.value = false }
}

function openCreate() {
  editingId.value = null
  form.value = { name: '', address: '', phone: '', email: '', default_grace_period_minutes: 15, default_turn_time_minutes: 90, auto_confirm_online: false, is_active: true, area_names: ['Main'] }
  showDialog.value = true
}

function openEdit(o: Outlet) {
  editingId.value = o.id
  form.value = {
    name: o.name, address: o.address || '', phone: o.phone || '', email: o.email || '',
    default_grace_period_minutes: o.default_grace_period_minutes,
    default_turn_time_minutes: o.default_turn_time_minutes,
    auto_confirm_online: o.auto_confirm_online, is_active: o.is_active,
    area_names: o.areas?.length ? o.areas.map(a => a.name) : ['Main']
  }
  showDialog.value = true
}

function addArea() {
  form.value.area_names.push('')
}

function removeArea(idx: number) {
  if (form.value.area_names.length <= 1) return
  form.value.area_names.splice(idx, 1)
}

async function save() {
  if (!form.value.name.trim()) return
  saving.value = true
  try {
    if (editingId.value) {
      await api.put(`/settings/outlets/${editingId.value}`, form.value)
      toast.add({ severity: 'success', summary: 'Updated', detail: 'Outlet updated', life: 2000 })
    } else {
      await api.post('/settings/outlets', form.value)
      toast.add({ severity: 'success', summary: 'Created', detail: 'Outlet created', life: 2000 })
    }
    showDialog.value = false
    await load()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Save failed', life: 3000 })
  } finally { saving.value = false }
}

async function toggleActive(o: Outlet) {
  try {
    await api.put(`/settings/outlets/${o.id}`, {
      name: o.name, address: o.address, phone: o.phone, email: o.email,
      default_grace_period_minutes: o.default_grace_period_minutes,
      default_turn_time_minutes: o.default_turn_time_minutes,
      auto_confirm_online: o.auto_confirm_online, is_active: !o.is_active
    })
    toast.add({ severity: 'success', summary: 'Updated', detail: `Outlet ${o.is_active ? 'deactivated' : 'activated'}`, life: 2000 })
    await load()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Failed to update', life: 4000 })
  }
}

const confirmVisible = ref(false)
const confirmTarget = ref<Outlet | null>(null)

function requestDelete(o: Outlet) {
  confirmTarget.value = o
  confirmVisible.value = true
}

async function doDelete() {
  const o = confirmTarget.value
  confirmVisible.value = false
  if (!o) return
  try {
    await api.delete(`/settings/outlets/${o.id}`)
    toast.add({ severity: 'success', summary: 'Deleted', detail: 'Outlet deleted', life: 2000 })
    await load()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Delete failed', life: 3000 })
  }
}

onMounted(load)
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-4">
      <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">Outlets</h3>
      <button @click="openCreate" class="btn-gold text-sm"><i class="pi pi-plus mr-1"></i>Add Outlet</button>
    </div>

    <div v-if="loading" class="text-center py-8">
      <i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
    </div>

    <div v-else class="space-y-3">
      <div v-for="o in outlets" :key="o.id" class="card">
        <div class="flex items-start justify-between">
          <div class="flex-1">
            <div class="flex items-center gap-2">
              <h3 class="font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ o.name }}</h3>
              <span class="text-xs px-2 py-0.5 rounded-full font-medium"
                :style="o.is_active ? { backgroundColor: '#10b98120', color: '#10b981' } : { backgroundColor: '#6b728020', color: '#6b7280' }">
                {{ o.is_active ? 'Active' : 'Inactive' }}
              </span>
            </div>
            <p v-if="o.address" class="text-sm mt-0.5" :style="{ color: 'var(--addrez-text-secondary)' }">{{ o.address }}</p>
            <div class="flex flex-wrap gap-1 mt-2" v-if="o.areas?.length">
              <span v-for="area in o.areas" :key="area.id" class="text-[10px] px-2 py-0.5 rounded-full font-medium"
                :style="{ backgroundColor: 'rgba(212,168,83,0.12)', color: 'var(--addrez-gold)' }">{{ area.name }}</span>
            </div>
            <div class="flex gap-6 mt-2 text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">
              <span v-if="o.phone"><i class="pi pi-phone mr-1"></i>{{ o.phone }}</span>
              <span v-if="o.email"><i class="pi pi-envelope mr-1"></i>{{ o.email }}</span>
              <span><i class="pi pi-clock mr-1"></i>{{ o.default_turn_time_minutes }}min turn</span>
              <span><i class="pi pi-stopwatch mr-1"></i>{{ o.default_grace_period_minutes }}min grace</span>
              <span v-if="o.auto_confirm_online"><i class="pi pi-check-circle mr-1"></i>Auto-confirm</span>
            </div>
          </div>
          <div class="flex items-center gap-1">
            <button @click="openEdit(o)" class="p-2 rounded-lg bg-transparent border-0 cursor-pointer hover:bg-[var(--addrez-bg-hover)]" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-pencil text-sm"></i></button>
            <button @click="toggleActive(o)" class="p-2 rounded-lg bg-transparent border-0 cursor-pointer hover:bg-[var(--addrez-bg-hover)]"
              :style="{ color: o.is_active ? '#f59e0b' : '#10b981' }" :title="o.is_active ? 'Deactivate' : 'Activate'">
              <i :class="o.is_active ? 'pi pi-ban' : 'pi pi-check'" class="text-sm"></i>
            </button>
            <button @click="requestDelete(o)" class="p-2 rounded-lg bg-transparent border-0 cursor-pointer hover:bg-[var(--addrez-bg-hover)]" style="color: #ef4444"><i class="pi pi-trash text-sm"></i></button>
          </div>
        </div>
      </div>
      <div v-if="outlets.length === 0" class="text-center py-8" :style="{ color: 'var(--addrez-text-secondary)' }">
        <i class="pi pi-building text-3xl mb-2"></i>
        <p>No outlets configured. Click "Add Outlet" to create one.</p>
      </div>
    </div>

    <!-- Create/Edit Dialog -->
    <div v-if="showDialog" class="modal-overlay">
      <div class="card w-full max-w-lg mx-4" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ editingId ? 'Edit Outlet' : 'New Outlet' }}</h3>
          <button @click="showDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="save" class="space-y-3">
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Name *</label>
            <input v-model="form.name" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" placeholder="e.g. Parkfood" />
          </div>
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Address</label>
            <input v-model="form.address" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Phone</label>
              <input v-model="form.phone" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" placeholder="+20-xxx-xxx-xxxx" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Email</label>
              <input v-model="form.email" type="email" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Turn Time (min)</label>
              <input v-model.number="form.default_turn_time_minutes" type="number" min="15" step="15" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Grace Period (min)</label>
              <input v-model.number="form.default_grace_period_minutes" type="number" min="5" step="5" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
          </div>
          <!-- Areas -->
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Areas *</label>
            <div class="space-y-2">
              <div v-for="(area, idx) in form.area_names" :key="idx" class="flex gap-2">
                <input v-model="form.area_names[idx]" class="flex-1 px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" :placeholder="`Area ${idx + 1} (e.g. Indoor, Floor 1)`" />
                <button v-if="form.area_names.length > 1" type="button" @click="removeArea(idx)" class="px-2 rounded-lg bg-transparent border-0 cursor-pointer" style="color: #ef4444" title="Remove area"><i class="pi pi-times text-sm"></i></button>
              </div>
              <button type="button" @click="addArea" class="text-xs px-3 py-1.5 rounded-lg bg-transparent border cursor-pointer" :style="{ borderColor: 'var(--addrez-gold)', color: 'var(--addrez-gold)' }"><i class="pi pi-plus mr-1"></i>Add Area</button>
            </div>
          </div>
          <div class="flex items-center gap-4">
            <label class="flex items-center gap-2 text-sm cursor-pointer" :style="{ color: 'var(--addrez-text-primary)' }">
              <input type="checkbox" v-model="form.auto_confirm_online" /> Auto-confirm online bookings
            </label>
            <label v-if="editingId" class="flex items-center gap-2 text-sm cursor-pointer" :style="{ color: 'var(--addrez-text-primary)' }">
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
    <ConfirmDialog :visible="confirmVisible" title="Delete Outlet" :message="`Delete outlet '${confirmTarget?.name}'? This cannot be undone.`" @confirm="doDelete" @cancel="confirmVisible = false" />
  </div>
</template>
