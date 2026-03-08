<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import type { UserListItem } from '@/types'

const toast = useToast()
const users = ref<UserListItem[]>([])
const roles = ref<{ id: number; name: string; slug: string }[]>([])
const outlets = ref<{ id: number; name: string }[]>([])
const loading = ref(true)
const showDialog = ref(false)
const showResetDialog = ref(false)
const editingId = ref<number | null>(null)
const resetUserId = ref<number | null>(null)
const resetPassword = ref('')

const form = ref({
  username: '', email: '', password: '', first_name: '', last_name: '',
  phone: '', role_id: null as number | null, branch_ids: [] as number[], is_active: true
})

const isAdminRole = computed(() => {
  if (!form.value.role_id) return false
  const role = roles.value.find(r => r.id === form.value.role_id)
  return role?.slug === 'superadmin'
})

function selectAllOutlets() { form.value.branch_ids = outlets.value.map(b => b.id) }
function deselectAllOutlets() { form.value.branch_ids = [] }

async function load() {
  loading.value = true
  try {
    const [u, r, b] = await Promise.all([
      api.get<UserListItem[]>('/settings/users'),
      api.get<{ id: number; name: string; slug: string }[]>('/settings/roles'),
      api.get<{ id: number; name: string }[]>('/settings/outlets')
    ])
    users.value = u.data
    roles.value = r.data
    outlets.value = b.data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load', life: 3000 })
  } finally { loading.value = false }
}

function openCreate() {
  editingId.value = null
  form.value = { username: '', email: '', password: '', first_name: '', last_name: '', phone: '', role_id: null, branch_ids: [], is_active: true }
  showDialog.value = true
}

function openEdit(u: UserListItem) {
  editingId.value = u.id
  const matchedRole = roles.value.find(r => u.roles.includes(r.name))
  form.value = {
    username: u.username, email: u.email, password: '', first_name: u.full_name.split(' ')[0] || '',
    last_name: u.full_name.split(' ').slice(1).join(' ') || '', phone: u.phone || '',
    role_id: matchedRole?.id ?? null,
    branch_ids: outlets.value.filter(b => u.outlets.includes(b.name)).map(b => b.id),
    is_active: u.is_active
  }
  showDialog.value = true
}

async function save() {
  const branchIds = isAdminRole.value ? outlets.value.map(b => b.id) : form.value.branch_ids
  const roleIds = form.value.role_id ? [form.value.role_id] : []
  try {
    if (editingId.value) {
      await api.put(`/settings/users/${editingId.value}`, {
        email: form.value.email, first_name: form.value.first_name, last_name: form.value.last_name,
        phone: form.value.phone, is_active: form.value.is_active,
        role_ids: roleIds, branch_ids: branchIds
      })
      toast.add({ severity: 'success', summary: 'Updated', detail: 'User updated', life: 2000 })
    } else {
      await api.post('/settings/users', { ...form.value, role_ids: roleIds, branch_ids: branchIds })
      toast.add({ severity: 'success', summary: 'Created', detail: 'User created', life: 2000 })
    }
    showDialog.value = false
    load()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Save failed', life: 3000 })
  }
}

async function toggleActive(u: UserListItem) {
  try {
    const matchedRole = roles.value.find(r => u.roles.includes(r.name))
    await api.put(`/settings/users/${u.id}`, {
      email: u.email, first_name: u.full_name.split(' ')[0], last_name: u.full_name.split(' ').slice(1).join(' '),
      phone: u.phone || '', is_active: !u.is_active,
      role_ids: matchedRole ? [matchedRole.id] : [],
      branch_ids: outlets.value.filter(b => u.outlets.includes(b.name)).map(b => b.id)
    })
    toast.add({ severity: 'success', summary: 'Updated', detail: u.is_active ? 'User deactivated' : 'User activated', life: 2000 })
    load()
  } catch { toast.add({ severity: 'error', summary: 'Error', detail: 'Failed', life: 3000 }) }
}

function openResetPassword(u: UserListItem) {
  resetUserId.value = u.id
  resetPassword.value = ''
  showResetDialog.value = true
}

async function doResetPassword() {
  if (!resetPassword.value || resetPassword.value.length < 6) {
    toast.add({ severity: 'warn', summary: 'Warning', detail: 'Password must be at least 6 characters', life: 3000 })
    return
  }
  try {
    await api.post(`/settings/users/${resetUserId.value}/reset-password`, { new_password: resetPassword.value })
    toast.add({ severity: 'success', summary: 'Done', detail: 'Password reset', life: 2000 })
    showResetDialog.value = false
  } catch { toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to reset', life: 3000 }) }
}

onMounted(load)
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-4">
      <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">Users</h3>
      <button @click="openCreate" class="btn-gold text-sm"><i class="pi pi-plus mr-1"></i>Add User</button>
    </div>

    <div v-if="loading" class="text-center py-8">
      <i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
    </div>
    <div v-else class="card overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr :style="{ color: 'var(--addrez-text-secondary)' }">
            <th class="text-left py-3 px-3 font-medium">User</th>
            <th class="text-left py-3 px-3 font-medium">Email</th>
            <th class="text-left py-3 px-3 font-medium">Roles</th>
            <th class="text-left py-3 px-3 font-medium">Outlets</th>
            <th class="text-left py-3 px-3 font-medium">Status</th>
            <th class="text-right py-3 px-3 font-medium">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="u in users" :key="u.id" class="border-t" :style="{ borderColor: 'var(--addrez-border)' }">
            <td class="py-3 px-3">
              <div class="font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ u.full_name }}</div>
              <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">@{{ u.username }}</div>
            </td>
            <td class="py-3 px-3" :style="{ color: 'var(--addrez-text-secondary)' }">{{ u.email }}</td>
            <td class="py-3 px-3">
              <div class="flex gap-1 flex-wrap">
                <span v-for="r in u.roles" :key="r" class="text-xs px-1.5 py-0.5 rounded" :style="{ backgroundColor: 'var(--addrez-gold)' + '20', color: 'var(--addrez-gold)' }">{{ r }}</span>
              </div>
            </td>
            <td class="py-3 px-3">
              <div class="flex gap-1 flex-wrap">
                <span v-for="b in u.outlets" :key="b" class="text-xs px-1.5 py-0.5 rounded" :style="{ backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)' }">{{ b }}</span>
              </div>
            </td>
            <td class="py-3 px-3">
              <span class="text-xs px-2 py-1 rounded-full font-medium" :style="u.is_active ? { backgroundColor: '#10b98120', color: '#10b981' } : { backgroundColor: '#ef444420', color: '#ef4444' }">
                {{ u.is_active ? 'Active' : 'Disabled' }}
              </span>
            </td>
            <td class="py-3 px-3 text-right">
              <div class="flex items-center justify-end gap-1">
                <button @click="openEdit(u)" class="px-2 py-1 rounded text-xs bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-gold)' }" title="Edit"><i class="pi pi-pencil"></i></button>
                <button @click="openResetPassword(u)" class="px-2 py-1 rounded text-xs bg-transparent border-0 cursor-pointer" style="color: #3b82f6" title="Reset Password"><i class="pi pi-key"></i></button>
                <button @click="toggleActive(u)" class="px-2 py-1 rounded text-xs bg-transparent border-0 cursor-pointer" :style="{ color: u.is_active ? '#ef4444' : '#10b981' }" :title="u.is_active ? 'Deactivate' : 'Activate'">
                  <i :class="u.is_active ? 'pi pi-ban' : 'pi pi-check-circle'"></i>
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Create/Edit Dialog -->
    <div v-if="showDialog" class="modal-overlay">
      <div class="card w-full max-w-lg mx-4 max-h-[80vh] overflow-y-auto" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ editingId ? 'Edit User' : 'New User' }}</h3>
          <button @click="showDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="save" class="space-y-3">
          <div class="grid grid-cols-2 gap-3">
            <div v-if="!editingId">
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Username *</label>
              <input v-model="form.username" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div v-if="!editingId">
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Password *</label>
              <input v-model="form.password" type="password" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">First Name *</label>
              <input v-model="form.first_name" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Last Name *</label>
              <input v-model="form.last_name" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Email *</label>
              <input v-model="form.email" type="email" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Phone</label>
              <input v-model="form.phone" placeholder="+20-100-000-0000" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
          </div>
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Role *</label>
            <select v-model="form.role_id" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
              <option :value="null" disabled>Select a role</option>
              <option v-for="r in roles" :key="r.id" :value="r.id">{{ r.name }}</option>
            </select>
          </div>
          <div>
            <div class="flex items-center justify-between mb-1">
              <label class="text-xs font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">Outlets</label>
              <div v-if="!isAdminRole" class="flex gap-1">
                <button type="button" @click="selectAllOutlets" class="text-[10px] px-2 py-0.5 rounded border-0 cursor-pointer" style="background: var(--addrez-gold); color: #1a1a24">Select All</button>
                <button type="button" @click="deselectAllOutlets" class="text-[10px] px-2 py-0.5 rounded border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-secondary)' }">Deselect All</button>
              </div>
              <span v-else class="text-[10px]" style="color: var(--addrez-gold)">All outlets (admin)</span>
            </div>
            <div class="flex flex-wrap gap-2" :style="isAdminRole ? { opacity: '0.5', pointerEvents: 'none' } : {}">
              <label v-for="b in outlets" :key="b.id" class="flex items-center gap-1.5 px-2 py-1 rounded-lg border text-xs cursor-pointer" :style="(isAdminRole || form.branch_ids.includes(b.id)) ? { borderColor: '#3b82f6', backgroundColor: '#3b82f615', color: '#3b82f6' } : { borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-secondary)' }">
                <input type="checkbox" :value="b.id" v-model="form.branch_ids" class="hidden" :disabled="isAdminRole" />
                <i :class="(isAdminRole || form.branch_ids.includes(b.id)) ? 'pi pi-check-square' : 'pi pi-stop'" class="text-xs"></i>
                {{ b.name }}
              </label>
            </div>
          </div>
          <div v-if="editingId" class="flex items-center gap-2">
            <input type="checkbox" id="user-active" v-model="form.is_active" />
            <label for="user-active" class="text-sm" :style="{ color: 'var(--addrez-text-primary)' }">Active</label>
          </div>
          <div class="flex justify-end gap-2 pt-2">
            <button type="button" @click="showDialog = false" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
            <button type="submit" class="btn-gold text-sm">{{ editingId ? 'Update' : 'Create' }}</button>
          </div>
        </form>
      </div>
    </div>

    <!-- Reset Password Dialog -->
    <div v-if="showResetDialog" class="modal-overlay">
      <div class="card w-full max-w-sm mx-4" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }"><i class="pi pi-key mr-2"></i>Reset Password</h3>
          <button @click="showResetDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="doResetPassword" class="space-y-3">
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">New Password *</label>
            <input v-model="resetPassword" type="password" required minlength="6" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
          </div>
          <div class="flex justify-end gap-2 pt-2">
            <button type="button" @click="showResetDialog = false" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
            <button type="submit" class="btn-gold text-sm">Reset Password</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
