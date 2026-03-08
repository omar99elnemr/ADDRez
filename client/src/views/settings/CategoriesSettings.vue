<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import type { Category } from '@/types'
import ConfirmDialog from '@/components/ConfirmDialog.vue'

const toast = useToast()
const categories = ref<Category[]>([])
const loading = ref(true)
const showDialog = ref(false)
const editingId = ref<number | null>(null)
const saving = ref(false)

const form = ref({ name: '', description: '', color: '#d4a853', priority: 1, is_active: true })

const presetColors = ['#d4a853', '#22c55e', '#3b82f6', '#a855f7', '#ec4899', '#ef4444', '#f59e0b', '#06b6d4', '#6b7280']

async function load() {
  loading.value = true
  try {
    const { data } = await api.get<Category[]>('/settings/categories')
    categories.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load categories', life: 3000 })
  } finally { loading.value = false }
}

function openCreate() {
  editingId.value = null
  form.value = { name: '', description: '', color: '#d4a853', priority: categories.value.length + 1, is_active: true }
  showDialog.value = true
}

function openEdit(c: Category) {
  editingId.value = c.id
  form.value = { name: c.name, description: c.description || '', color: c.color, priority: c.priority, is_active: c.is_active }
  showDialog.value = true
}

async function save() {
  if (!form.value.name.trim()) return
  saving.value = true
  try {
    if (editingId.value) {
      await api.put(`/settings/categories/${editingId.value}`, form.value)
      toast.add({ severity: 'success', summary: 'Updated', detail: 'Category updated', life: 2000 })
    } else {
      await api.post('/settings/categories', form.value)
      toast.add({ severity: 'success', summary: 'Created', detail: 'Category created', life: 2000 })
    }
    showDialog.value = false
    await load()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Save failed', life: 3000 })
  } finally { saving.value = false }
}

const confirmVisible = ref(false)
const confirmTarget = ref<Category | null>(null)
function requestDelete(c: Category) { confirmTarget.value = c; confirmVisible.value = true }
async function doDelete() {
  const c = confirmTarget.value; confirmVisible.value = false; if (!c) return
  try {
    await api.delete(`/settings/categories/${c.id}`)
    toast.add({ severity: 'success', summary: 'Deleted', detail: 'Category deleted', life: 2000 })
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
      <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">Customer Categories</h3>
      <button @click="openCreate" class="btn-gold text-sm"><i class="pi pi-plus mr-1"></i>Add Category</button>
    </div>

    <div v-if="loading" class="text-center py-8">
      <i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
    </div>
    <div v-else>
      <div v-if="categories.length === 0" class="text-center py-8" :style="{ color: 'var(--addrez-text-secondary)' }">
        <i class="pi pi-th-large text-3xl mb-2"></i>
        <p>No categories configured. Click "Add Category" to create one.</p>
      </div>
      <div v-else class="space-y-3">
        <div v-for="c in categories" :key="c.id" class="card flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div class="w-4 h-4 rounded-full shrink-0" :style="{ backgroundColor: c.color }"></div>
            <div>
              <div class="font-medium text-sm" :style="{ color: 'var(--addrez-text-primary)' }">{{ c.name }}</div>
              <div v-if="c.description" class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ c.description }}</div>
            </div>
          </div>
          <div class="flex items-center gap-3">
            <span class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">Priority: {{ c.priority }}</span>
            <span class="text-xs px-2 py-1 rounded-full font-medium" :style="c.is_active ? { backgroundColor: '#10b98120', color: '#10b981' } : { backgroundColor: '#6b728020', color: '#6b7280' }">
              {{ c.is_active ? 'Active' : 'Inactive' }}
            </span>
            <button @click="openEdit(c)" class="p-1.5 rounded bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-pencil text-xs"></i></button>
            <button @click="requestDelete(c)" class="p-1.5 rounded bg-transparent border-0 cursor-pointer" style="color: #ef4444"><i class="pi pi-trash text-xs"></i></button>
          </div>
        </div>
      </div>
    </div>

    <!-- Create/Edit Dialog -->
    <div v-if="showDialog" class="fixed inset-0 z-50 flex items-center justify-center" style="background: rgba(0,0,0,0.5)">
      <div class="card w-full max-w-sm mx-4" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ editingId ? 'Edit Category' : 'New Category' }}</h3>
          <button @click="showDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="save" class="space-y-3">
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Name *</label>
            <input v-model="form.name" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" placeholder="e.g. VIP, Corporate" />
          </div>
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Description</label>
            <input v-model="form.description" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Priority</label>
              <input v-model.number="form.priority" type="number" min="1" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div v-if="editingId">
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Status</label>
              <label class="flex items-center gap-2 mt-2 text-sm cursor-pointer" :style="{ color: 'var(--addrez-text-primary)' }">
                <input type="checkbox" v-model="form.is_active" /> Active
              </label>
            </div>
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
    <ConfirmDialog :visible="confirmVisible" title="Delete Category" :message="`Delete category '${confirmTarget?.name}'? Customers in this category will be unlinked.`" @confirm="doDelete" @cancel="confirmVisible = false" />
  </div>
</template>
