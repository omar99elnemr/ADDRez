<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import type { NotificationTemplate } from '@/types'
import ConfirmDialog from '@/components/ConfirmDialog.vue'

const toast = useToast()
const templates = ref<NotificationTemplate[]>([])
const loading = ref(true)
const showDialog = ref(false)
const editingId = ref<number | null>(null)
const saving = ref(false)

const form = ref({ name: '', type: 'Confirmation', channel: 'Email', subject: '', body: '', is_active: true })

const channelIcons: Record<string, string> = {
  Email: 'pi pi-envelope', Sms: 'pi pi-mobile', WhatsApp: 'pi pi-comments', Push: 'pi pi-bell'
}
const typeOptions = ['Confirmation', 'Cancellation', 'Reminder', 'Modification', 'Feedback', 'Marketing']
const channelOptions = ['Email', 'Sms', 'WhatsApp', 'Push']

async function load() {
  loading.value = true
  try {
    const { data } = await api.get<NotificationTemplate[]>('/settings/templates')
    templates.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load templates', life: 3000 })
  } finally { loading.value = false }
}

function openCreate() {
  editingId.value = null
  form.value = { name: '', type: 'Confirmation', channel: 'Email', subject: '', body: '', is_active: true }
  showDialog.value = true
}

function openEdit(t: NotificationTemplate) {
  editingId.value = t.id
  form.value = { name: t.name, type: t.type, channel: t.channel, subject: t.subject || '', body: t.body, is_active: t.is_active }
  showDialog.value = true
}

async function save() {
  if (!form.value.name.trim()) return
  saving.value = true
  try {
    if (editingId.value) {
      await api.put(`/settings/templates/${editingId.value}`, { name: form.value.name, subject: form.value.subject, body: form.value.body, is_active: form.value.is_active })
      toast.add({ severity: 'success', summary: 'Updated', detail: 'Template updated', life: 2000 })
    } else {
      await api.post('/settings/templates', form.value)
      toast.add({ severity: 'success', summary: 'Created', detail: 'Template created', life: 2000 })
    }
    showDialog.value = false
    await load()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Save failed', life: 3000 })
  } finally { saving.value = false }
}

const confirmVisible = ref(false)
const confirmTarget = ref<NotificationTemplate | null>(null)
function requestDelete(t: NotificationTemplate) { confirmTarget.value = t; confirmVisible.value = true }
async function doDelete() {
  const t = confirmTarget.value; confirmVisible.value = false; if (!t) return
  try {
    await api.delete(`/settings/templates/${t.id}`)
    toast.add({ severity: 'success', summary: 'Deleted', detail: 'Template deleted', life: 2000 })
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
      <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">Notification Templates</h3>
      <button @click="openCreate" class="btn-gold text-sm"><i class="pi pi-plus mr-1"></i>Add Template</button>
    </div>

    <div v-if="loading" class="text-center py-8">
      <i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
    </div>
    <div v-else>
      <div v-if="templates.length === 0" class="text-center py-8" :style="{ color: 'var(--addrez-text-secondary)' }">
        <i class="pi pi-envelope text-3xl mb-2"></i>
        <p>No notification templates configured.</p>
      </div>
      <div v-else class="space-y-3">
        <div v-for="t in templates" :key="t.id" class="card">
          <div class="flex items-start justify-between">
            <div class="flex-1">
              <h3 class="font-semibold text-sm" :style="{ color: 'var(--addrez-text-primary)' }">{{ t.name }}</h3>
              <p v-if="t.subject" class="text-xs mt-0.5" :style="{ color: 'var(--addrez-text-secondary)' }">{{ t.subject }}</p>
            </div>
            <div class="flex items-center gap-2">
              <span class="text-xs px-2 py-1 rounded" :style="{ backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)' }">
                <i :class="channelIcons[t.channel] || 'pi pi-send'" class="mr-1"></i>{{ t.channel }}
              </span>
              <span class="text-xs px-2 py-1 rounded-full font-medium" :style="t.is_active ? { backgroundColor: '#10b98120', color: '#10b981' } : { backgroundColor: '#6b728020', color: '#6b7280' }">
                {{ t.is_active ? 'Active' : 'Inactive' }}
              </span>
              <button @click="openEdit(t)" class="p-1.5 rounded bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-pencil text-xs"></i></button>
              <button @click="requestDelete(t)" class="p-1.5 rounded bg-transparent border-0 cursor-pointer" style="color: #ef4444"><i class="pi pi-trash text-xs"></i></button>
            </div>
          </div>
          <div class="text-xs mt-2" :style="{ color: 'var(--addrez-text-secondary)' }">Type: {{ t.type }}</div>
        </div>
      </div>
    </div>

    <!-- Create/Edit Dialog -->
    <div v-if="showDialog" class="modal-overlay">
      <div class="card w-full max-w-lg mx-4 max-h-[85vh] overflow-y-auto" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ editingId ? 'Edit Template' : 'New Template' }}</h3>
          <button @click="showDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="save" class="space-y-3">
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Name *</label>
            <input v-model="form.name" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
          </div>
          <div v-if="!editingId" class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Type</label>
              <select v-model="form.type" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
                <option v-for="tp in typeOptions" :key="tp" :value="tp">{{ tp }}</option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Channel</label>
              <select v-model="form.channel" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
                <option v-for="ch in channelOptions" :key="ch" :value="ch">{{ ch }}</option>
              </select>
            </div>
          </div>
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Subject</label>
            <input v-model="form.subject" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" placeholder="e.g. Reservation Confirmed - {{venue_name}}" />
          </div>
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Body *</label>
            <textarea v-model="form.body" required rows="6" class="w-full px-3 py-2 rounded-lg border text-sm resize-none font-mono" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" placeholder="Use {{guest_name}}, {{date}}, {{time}}, {{covers}}, {{confirmation_code}}, {{venue_name}}"></textarea>
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
    <ConfirmDialog :visible="confirmVisible" title="Delete Template" :message="`Delete template '${confirmTarget?.name}'?`" @confirm="doDelete" @cancel="confirmVisible = false" />
  </div>
</template>
