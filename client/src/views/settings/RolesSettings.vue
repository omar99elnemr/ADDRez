<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import type { RoleDetail, Permission } from '@/types'

const toast = useToast()
const roles = ref<RoleDetail[]>([])
const permissions = ref<Permission[]>([])
const loading = ref(true)
const showDialog = ref(false)
const editingId = ref<number | null>(null)
const expandedRole = ref<number | null>(null)
const form = ref({ name: '', description: '', slug: '', permission_ids: [] as number[] })

const permissionGroups = computed(() => {
  const groups = new Map<string, Permission[]>()
  permissions.value.forEach(p => {
    if (!groups.has(p.group)) groups.set(p.group, [])
    groups.get(p.group)!.push(p)
  })
  return groups
})

async function load() {
  loading.value = true
  try {
    const [rolesRes, permsRes] = await Promise.all([
      api.get<RoleDetail[]>('/settings/roles'),
      api.get<Permission[]>('/settings/permissions')
    ])
    roles.value = rolesRes.data
    permissions.value = permsRes.data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load roles', life: 3000 })
  } finally { loading.value = false }
}

function openCreate() {
  editingId.value = null
  form.value = { name: '', description: '', slug: '', permission_ids: [] }
  showDialog.value = true
}

function openEdit(r: RoleDetail) {
  editingId.value = r.id
  form.value = { name: r.name, description: r.description || '', slug: r.slug, permission_ids: [...r.permission_ids] }
  showDialog.value = true
}

async function save() {
  try {
    if (editingId.value) {
      await api.put(`/settings/roles/${editingId.value}`, form.value)
      toast.add({ severity: 'success', summary: 'Updated', detail: 'Role updated', life: 2000 })
    } else {
      await api.post('/settings/roles', form.value)
      toast.add({ severity: 'success', summary: 'Created', detail: 'Role created', life: 2000 })
    }
    showDialog.value = false
    load()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Save failed', life: 3000 })
  }
}

const confirmVisible = ref(false)
const confirmTargetId = ref<number | null>(null)
const confirmTargetName = ref('')
function requestDelete(r: any) { confirmTargetId.value = r.id; confirmTargetName.value = r.name; confirmVisible.value = true }
async function doDelete() {
  const id = confirmTargetId.value; confirmVisible.value = false; if (!id) return
  try {
    await api.delete(`/settings/roles/${id}`)
    toast.add({ severity: 'success', summary: 'Deleted', detail: 'Role deleted', life: 2000 })
    load()
  } catch { toast.add({ severity: 'error', summary: 'Error', detail: 'Cannot delete system roles', life: 3000 }) }
}

function toggleGroup(group: string) {
  const groupPerms = permissionGroups.value.get(group) || []
  const allSelected = groupPerms.every(p => form.value.permission_ids.includes(p.id))
  if (allSelected) {
    form.value.permission_ids = form.value.permission_ids.filter(id => !groupPerms.some(p => p.id === id))
  } else {
    groupPerms.forEach(p => { if (!form.value.permission_ids.includes(p.id)) form.value.permission_ids.push(p.id) })
  }
}

function selectAll() { form.value.permission_ids = permissions.value.map(p => p.id) }
function selectNone() { form.value.permission_ids = [] }

onMounted(load)
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-4">
      <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">Roles & Permissions</h3>
      <button @click="openCreate" class="btn-gold text-sm"><i class="pi pi-plus mr-1"></i>Add Role</button>
    </div>

    <div v-if="loading" class="text-center py-8">
      <i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
    </div>
    <div v-else class="space-y-3">
      <div v-for="r in roles" :key="r.id" class="card">
        <div class="flex items-start justify-between">
          <div class="cursor-pointer flex-1" @click="expandedRole = expandedRole === r.id ? null : r.id">
            <h3 class="font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">
              <i :class="expandedRole === r.id ? 'pi pi-chevron-down' : 'pi pi-chevron-right'" class="text-xs mr-2"></i>
              {{ r.name }}
            </h3>
            <p v-if="r.description" class="text-sm mt-0.5 ml-5" :style="{ color: 'var(--addrez-text-secondary)' }">{{ r.description }}</p>
          </div>
          <div class="flex items-center gap-2">
            <span v-if="r.is_system" class="text-xs px-2 py-1 rounded-full font-medium" style="background: #3b82f620; color: #3b82f6">System</span>
            <span class="text-xs px-2 py-1 rounded" :style="{ backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)' }">{{ r.permission_ids.length }} permissions</span>
            <button @click="openEdit(r)" class="px-2 py-1 rounded text-xs bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-gold)' }" title="Edit"><i class="pi pi-pencil"></i></button>
            <button v-if="!r.is_system" @click="requestDelete(r)" class="px-2 py-1 rounded text-xs bg-transparent border-0 cursor-pointer" style="color: #ef4444" title="Delete"><i class="pi pi-trash"></i></button>
          </div>
        </div>
        <!-- Expanded permissions list -->
        <div v-if="expandedRole === r.id" class="mt-3 pt-3 border-t" :style="{ borderColor: 'var(--addrez-border)' }">
          <div class="flex flex-wrap gap-1">
            <span v-for="pid in r.permission_ids" :key="pid" class="text-[10px] px-1.5 py-0.5 rounded" :style="{ backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)' }">
              {{ permissions.find(p => p.id === pid)?.key || pid }}
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- Create/Edit Role Dialog -->
    <div v-if="showDialog" class="fixed inset-0 z-50 flex items-center justify-center" style="background: rgba(0,0,0,0.5)">
      <div class="card w-full max-w-2xl mx-4 max-h-[85vh] overflow-y-auto" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ editingId ? 'Edit Role' : 'New Role' }}</h3>
          <button @click="showDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="save" class="space-y-4">
          <div class="grid grid-cols-3 gap-3">
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Name *</label>
              <input v-model="form.name" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Slug *</label>
              <input v-model="form.slug" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Description</label>
              <input v-model="form.description" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
          </div>

          <div>
            <div class="flex items-center justify-between mb-2">
              <label class="text-xs font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Permissions ({{ form.permission_ids.length }}/{{ permissions.length }})</label>
              <div class="flex gap-2">
                <button type="button" @click="selectAll" class="text-xs bg-transparent border-0 cursor-pointer" style="color: #3b82f6">Select All</button>
                <button type="button" @click="selectNone" class="text-xs bg-transparent border-0 cursor-pointer" style="color: #ef4444">Clear</button>
              </div>
            </div>
            <div class="space-y-3">
              <div v-for="[group, perms] in permissionGroups" :key="group" class="rounded-lg p-3" :style="{ backgroundColor: 'var(--addrez-bg-primary)', border: '1px solid var(--addrez-border)' }">
                <div class="flex items-center justify-between mb-2">
                  <span class="text-xs font-bold uppercase tracking-wider" :style="{ color: 'var(--addrez-gold)' }">{{ group }}</span>
                  <button type="button" @click="toggleGroup(group)" class="text-[10px] bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }">Toggle All</button>
                </div>
                <div class="flex flex-wrap gap-1">
                  <label v-for="p in perms" :key="p.id" class="flex items-center gap-1 px-2 py-1 rounded text-xs cursor-pointer transition-colors" :style="form.permission_ids.includes(p.id) ? { backgroundColor: '#10b98115', color: '#10b981' } : { color: 'var(--addrez-text-secondary)' }">
                    <input type="checkbox" :value="p.id" v-model="form.permission_ids" class="hidden" />
                    <i :class="form.permission_ids.includes(p.id) ? 'pi pi-check-circle' : 'pi pi-circle'" class="text-[10px]"></i>
                    {{ p.key.split('.')[1] }}
                  </label>
                </div>
              </div>
            </div>
          </div>

          <div class="flex justify-end gap-2 pt-2">
            <button type="button" @click="showDialog = false" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
            <button type="submit" class="btn-gold text-sm">{{ editingId ? 'Update' : 'Create' }}</button>
          </div>
        </form>
      </div>
    </div>
    <ConfirmDialog :visible="confirmVisible" title="Delete Role" :message="`Delete role '${confirmTargetName}'?`" @confirm="doDelete" @cancel="confirmVisible = false" />
  </div>
</template>
