<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import type { Terms } from '@/types'
import ConfirmDialog from '@/components/ConfirmDialog.vue'

const toast = useToast()
const terms = ref<Terms[]>([])
const loading = ref(true)
const showDialog = ref(false)
const editingId = ref<number | null>(null)
const saving = ref(false)

const form = ref({ title: '', content: '', sort_order: 1, is_active: true })

async function load() {
  loading.value = true
  try {
    const { data } = await api.get<Terms[]>('/settings/terms')
    terms.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load terms', life: 3000 })
  } finally { loading.value = false }
}

function openCreate() {
  editingId.value = null
  form.value = { title: '', content: '', sort_order: terms.value.length + 1, is_active: true }
  showDialog.value = true
}

function openEdit(t: Terms) {
  editingId.value = t.id
  form.value = { title: t.title, content: t.content, sort_order: t.sort_order, is_active: t.is_active }
  showDialog.value = true
}

async function save() {
  if (!form.value.title.trim()) return
  saving.value = true
  try {
    if (editingId.value) {
      await api.put(`/settings/terms/${editingId.value}`, form.value)
      toast.add({ severity: 'success', summary: 'Updated', detail: 'Term updated', life: 2000 })
    } else {
      await api.post('/settings/terms', form.value)
      toast.add({ severity: 'success', summary: 'Created', detail: 'Term created', life: 2000 })
    }
    showDialog.value = false
    await load()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Save failed', life: 3000 })
  } finally { saving.value = false }
}

const confirmVisible = ref(false)
const confirmTarget = ref<Terms | null>(null)
function requestDelete(t: Terms) { confirmTarget.value = t; confirmVisible.value = true }
async function doDelete() {
  const t = confirmTarget.value; confirmVisible.value = false; if (!t) return
  try {
    await api.delete(`/settings/terms/${t.id}`)
    toast.add({ severity: 'success', summary: 'Deleted', detail: 'Term deleted', life: 2000 })
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
      <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">Terms & Conditions</h3>
      <button @click="openCreate" class="btn-gold text-sm"><i class="pi pi-plus mr-1"></i>Add Term</button>
    </div>

    <div v-if="loading" class="text-center py-8">
      <i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
    </div>
    <div v-else>
      <div v-if="terms.length === 0" class="text-center py-8" :style="{ color: 'var(--addrez-text-secondary)' }">
        <i class="pi pi-file text-3xl mb-2"></i>
        <p>No terms & conditions configured.</p>
      </div>
      <div v-else class="space-y-3">
        <div v-for="t in terms" :key="t.id" class="card">
          <div class="flex items-start justify-between mb-2">
            <div class="flex items-center gap-2">
              <span class="text-xs font-mono px-1.5 py-0.5 rounded" :style="{ backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)' }">#{{ t.sort_order }}</span>
              <h3 class="font-semibold text-sm" :style="{ color: 'var(--addrez-text-primary)' }">{{ t.title }}</h3>
            </div>
            <div class="flex items-center gap-2">
              <span class="text-xs px-2 py-1 rounded-full font-medium" :style="t.is_active ? { backgroundColor: '#10b98120', color: '#10b981' } : { backgroundColor: '#6b728020', color: '#6b7280' }">
                {{ t.is_active ? 'Active' : 'Inactive' }}
              </span>
              <button @click="openEdit(t)" class="p-1.5 rounded bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-pencil text-xs"></i></button>
              <button @click="requestDelete(t)" class="p-1.5 rounded bg-transparent border-0 cursor-pointer" style="color: #ef4444"><i class="pi pi-trash text-xs"></i></button>
            </div>
          </div>
          <p class="text-sm leading-relaxed" :style="{ color: 'var(--addrez-text-secondary)' }">{{ t.content.substring(0, 200) }}{{ t.content.length > 200 ? '...' : '' }}</p>
        </div>
      </div>
    </div>

    <!-- Create/Edit Dialog -->
    <div v-if="showDialog" class="fixed inset-0 z-50 flex items-center justify-center" style="background: rgba(0,0,0,0.5)">
      <div class="card w-full max-w-lg mx-4" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ editingId ? 'Edit Term' : 'New Term' }}</h3>
          <button @click="showDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="save" class="space-y-3">
          <div class="grid grid-cols-4 gap-3">
            <div class="col-span-3">
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Title *</label>
              <input v-model="form.title" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Order</label>
              <input v-model.number="form.sort_order" type="number" min="1" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
          </div>
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Content *</label>
            <textarea v-model="form.content" required rows="5" class="w-full px-3 py-2 rounded-lg border text-sm resize-none" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"></textarea>
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
    <ConfirmDialog :visible="confirmVisible" title="Delete Term" :message="`Delete '${confirmTarget?.title}'?`" @confirm="doDelete" @cancel="confirmVisible = false" />
  </div>
</template>
