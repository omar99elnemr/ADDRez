<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import ConfirmDialog from '@/components/ConfirmDialog.vue'

interface TagItem { id: number; name: string; color: string; type: string }

const toast = useToast()
const tags = ref<TagItem[]>([])
const loading = ref(true)
const showDialog = ref(false)
const editingId = ref<number | null>(null)
const saving = ref(false)
const typeFilter = ref('')

const form = ref({ name: '', color: '#d4a853', type: 'Customer' as string })

const presetColors = ['#ef4444', '#f59e0b', '#22c55e', '#3b82f6', '#8b5cf6', '#ec4899', '#06b6d4', '#d4a853', '#dc2626', '#16a34a', '#7c3aed', '#0891b2']

const filteredTags = computed(() => {
  if (!typeFilter.value) return tags.value
  return tags.value.filter(t => t.type === typeFilter.value)
})

const tagTypes = computed(() => [...new Set(tags.value.map(t => t.type))])

async function load() {
  loading.value = true
  try {
    const { data } = await api.get<TagItem[]>('/settings/tags')
    tags.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load tags', life: 3000 })
  } finally { loading.value = false }
}

function openCreate() {
  editingId.value = null
  form.value = { name: '', color: '#d4a853', type: 'Customer' }
  showDialog.value = true
}

function openEdit(t: TagItem) {
  editingId.value = t.id
  form.value = { name: t.name, color: t.color, type: t.type }
  showDialog.value = true
}

async function save() {
  if (!form.value.name.trim()) return
  saving.value = true
  try {
    if (editingId.value) {
      await api.put(`/settings/tags/${editingId.value}`, { name: form.value.name, color: form.value.color })
      toast.add({ severity: 'success', summary: 'Updated', detail: 'Tag updated', life: 2000 })
    } else {
      await api.post('/settings/tags', form.value)
      toast.add({ severity: 'success', summary: 'Created', detail: 'Tag created', life: 2000 })
    }
    showDialog.value = false
    await load()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Save failed', life: 3000 })
  } finally { saving.value = false }
}

const confirmVisible = ref(false)
const confirmTarget = ref<TagItem | null>(null)
function requestDelete(t: TagItem) { confirmTarget.value = t; confirmVisible.value = true }
async function doDelete() {
  const t = confirmTarget.value; confirmVisible.value = false; if (!t) return
  try {
    await api.delete(`/settings/tags/${t.id}`)
    toast.add({ severity: 'success', summary: 'Deleted', detail: 'Tag deleted', life: 2000 })
    await load()
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Delete failed', life: 3000 })
  }
}

onMounted(load)
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-4">
      <div class="flex items-center gap-3">
        <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">Tags</h3>
        <div v-if="tagTypes.length" class="flex gap-1">
          <button @click="typeFilter = ''" class="px-2 py-1 rounded text-xs border-0 cursor-pointer"
            :style="!typeFilter ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' } : { backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)' }">
            All
          </button>
          <button v-for="tp in tagTypes" :key="tp" @click="typeFilter = tp" class="px-2 py-1 rounded text-xs border-0 cursor-pointer"
            :style="typeFilter === tp ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' } : { backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)' }">
            {{ tp }}
          </button>
        </div>
      </div>
      <button @click="openCreate" class="btn-gold text-sm"><i class="pi pi-plus mr-1"></i>Add Tag</button>
    </div>

    <div v-if="loading" class="text-center py-8">
      <i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
    </div>
    <div v-else>
      <div v-if="filteredTags.length === 0" class="text-center py-8" :style="{ color: 'var(--addrez-text-secondary)' }">
        <i class="pi pi-tag text-3xl mb-2"></i>
        <p>No tags found. Click "Add Tag" to create one.</p>
      </div>
      <div v-else class="flex flex-wrap gap-3">
        <div v-for="t in filteredTags" :key="t.id" class="card flex items-center gap-3 !py-3 !px-4 group">
          <div class="w-4 h-4 rounded-full shrink-0" :style="{ backgroundColor: t.color }"></div>
          <div class="flex-1">
            <div class="font-medium text-sm" :style="{ color: 'var(--addrez-text-primary)' }">{{ t.name }}</div>
            <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ t.type }}</div>
          </div>
          <div class="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <button @click="openEdit(t)" class="p-1 rounded bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-pencil text-xs"></i></button>
            <button @click="requestDelete(t)" class="p-1 rounded bg-transparent border-0 cursor-pointer" style="color: #ef4444"><i class="pi pi-trash text-xs"></i></button>
          </div>
        </div>
      </div>
    </div>

    <!-- Create/Edit Dialog -->
    <div v-if="showDialog" class="fixed inset-0 z-50 flex items-center justify-center" style="background: rgba(0,0,0,0.5)">
      <div class="card w-full max-w-sm mx-4" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ editingId ? 'Edit Tag' : 'New Tag' }}</h3>
          <button @click="showDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="save" class="space-y-3">
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Name *</label>
            <input v-model="form.name" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" placeholder="e.g. Vegetarian, Allergy" />
          </div>
          <div v-if="!editingId">
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Type *</label>
            <select v-model="form.type" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
              <option value="Customer">Customer</option>
              <option value="Reservation">Reservation</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Color</label>
            <div class="flex flex-wrap gap-2 mb-2">
              <button v-for="c in presetColors" :key="c" type="button" @click="form.color = c"
                class="w-7 h-7 rounded-full border-2 cursor-pointer transition-transform"
                :style="{ backgroundColor: c, borderColor: form.color === c ? '#fff' : 'transparent', transform: form.color === c ? 'scale(1.2)' : 'scale(1)' }">
              </button>
            </div>
            <div class="flex items-center gap-2">
              <input v-model="form.color" type="color" class="w-8 h-8 rounded border-0 cursor-pointer" />
              <input v-model="form.color" class="flex-1 px-3 py-1.5 rounded-lg border text-xs font-mono" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
          </div>
          <div class="flex justify-end gap-2 pt-2">
            <button type="button" @click="showDialog = false" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
            <button type="submit" :disabled="saving" class="btn-gold text-sm">{{ saving ? 'Saving...' : editingId ? 'Update' : 'Create' }}</button>
          </div>
        </form>
      </div>
    </div>
    <ConfirmDialog :visible="confirmVisible" title="Delete Tag" :message="`Delete tag '${confirmTarget?.name}'?`" @confirm="doDelete" @cancel="confirmVisible = false" />
  </div>
</template>
