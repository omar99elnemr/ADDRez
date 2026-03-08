<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'

const auth = useAuthStore()
const toast = useToast()

// ── Profile form ──
const profileForm = ref({
  email: '',
  first_name: '',
  last_name: '',
  phone: ''
})
const profileLoading = ref(false)

// ── Password form ──
const passwordForm = ref({ currentPassword: '', newPassword: '', confirmPassword: '' })
const passwordLoading = ref(false)
const showPwCurrent = ref(false)
const showPwNew = ref(false)
const showPwConfirm = ref(false)

// ── Active tab ──
const activeTab = ref<'profile' | 'security' | 'access'>('profile')

// ── Computed ──
const userRole = computed(() => auth.user?.roles?.[0]?.name ?? 'User')
const userRoleSlug = computed(() => auth.user?.roles?.[0]?.slug ?? '')
const userPermissions = computed(() => auth.user?.permissions ?? [])
const userOutlets = computed(() => auth.user?.outlets ?? [])
const initials = computed(() => {
  const f = auth.user?.first_name?.charAt(0) || ''
  const l = auth.user?.last_name?.charAt(0) || ''
  return (f + l).toUpperCase()
})

const permissionGroups = computed(() => {
  const groups: Record<string, string[]> = {}
  for (const p of userPermissions.value) {
    const parts = p.split('.')
    const group = parts[0] ?? 'other'
    const action = parts[1] ?? p
    if (!groups[group]) groups[group] = []
    groups[group].push(action)
  }
  return groups
})

// ── Init ──
onMounted(() => {
  if (auth.user) {
    profileForm.value = {
      email: auth.user.email || '',
      first_name: auth.user.first_name || '',
      last_name: auth.user.last_name || '',
      phone: auth.user.phone || ''
    }
  }
})

// ── Save profile ──
async function saveProfile() {
  profileLoading.value = true
  try {
    const { data } = await api.put('/auth/profile', profileForm.value)
    auth.user = data
    localStorage.setItem('addrez_user', JSON.stringify(data))
    toast.add({ severity: 'success', summary: 'Profile Updated', detail: 'Your information has been saved', life: 3000 })
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Failed to update profile', life: 4000 })
  } finally {
    profileLoading.value = false
  }
}

// ── Change password ──
async function changePassword() {
  if (passwordForm.value.newPassword !== passwordForm.value.confirmPassword) {
    toast.add({ severity: 'warn', summary: 'Mismatch', detail: 'Passwords do not match', life: 3000 })
    return
  }
  if (passwordForm.value.newPassword.length < 6) {
    toast.add({ severity: 'warn', summary: 'Too Short', detail: 'Password must be at least 6 characters', life: 3000 })
    return
  }
  passwordLoading.value = true
  try {
    await api.post('/auth/update-password', {
      current_password: passwordForm.value.currentPassword,
      new_password: passwordForm.value.newPassword
    })
    passwordForm.value = { currentPassword: '', newPassword: '', confirmPassword: '' }
    toast.add({ severity: 'success', summary: 'Password Changed', detail: 'Your password has been updated', life: 3000 })
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Failed to change password', life: 4000 })
  } finally {
    passwordLoading.value = false
  }
}

function formatGroup(key: string): string {
  return key.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase())
}

function formatAction(action: string): string {
  return action.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase())
}
</script>

<template>
  <div class="max-w-4xl mx-auto animate-fade-in">
    <!-- Page header -->
    <div class="flex items-center gap-4 mb-8">
      <div class="w-16 h-16 rounded-2xl flex items-center justify-center text-xl font-bold shadow-md"
           style="background: linear-gradient(135deg, var(--addrez-gold), var(--addrez-gold-dark)); color: #1a1a24;">
        {{ initials }}
      </div>
      <div>
        <h1 class="page-heading">{{ auth.user?.full_name }}</h1>
        <div class="flex items-center gap-2 mt-1">
          <span class="text-xs px-2 py-0.5 rounded-full font-semibold" style="background: var(--addrez-gold); color: #1a1a24;">{{ userRole }}</span>
          <span class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">@{{ auth.user?.username }}</span>
        </div>
      </div>
    </div>

    <!-- Tabs -->
    <div class="flex gap-0 mb-6 border-b" :style="{ borderColor: 'var(--addrez-border)' }">
      <button
        v-for="tab in [
          { key: 'profile', label: 'Personal Info', icon: 'pi pi-user' },
          { key: 'security', label: 'Security', icon: 'pi pi-lock' },
          { key: 'access', label: 'Role & Permissions', icon: 'pi pi-shield' }
        ]" :key="tab.key"
        @click="activeTab = tab.key as any"
        class="flex items-center gap-2 px-4 py-2.5 text-sm font-medium bg-transparent border-0 cursor-pointer transition-all relative whitespace-nowrap"
        :style="activeTab === tab.key
          ? { color: 'var(--addrez-gold)', borderBottom: '2px solid var(--addrez-gold)' }
          : { color: 'var(--addrez-text-secondary)', borderBottom: '2px solid transparent' }"
      >
        <i :class="tab.icon" class="text-xs"></i>
        {{ tab.label }}
      </button>
    </div>

    <!-- ═══ Personal Info Tab ═══ -->
    <div v-if="activeTab === 'profile'" class="card">
      <h3 class="text-lg font-semibold mb-1" :style="{ color: 'var(--addrez-text-primary)' }">Personal Information</h3>
      <p class="text-sm mb-6" :style="{ color: 'var(--addrez-text-secondary)' }">Update your personal details and contact information.</p>

      <form @submit.prevent="saveProfile" class="space-y-4">
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-medium mb-1.5" :style="{ color: 'var(--addrez-text-secondary)' }">First Name</label>
            <input v-model="profileForm.first_name" required class="input px-3 py-2.5" placeholder="Your first name" />
          </div>
          <div>
            <label class="block text-xs font-medium mb-1.5" :style="{ color: 'var(--addrez-text-secondary)' }">Last Name</label>
            <input v-model="profileForm.last_name" required class="input px-3 py-2.5" placeholder="Your last name" />
          </div>
        </div>
        <div>
          <label class="block text-xs font-medium mb-1.5" :style="{ color: 'var(--addrez-text-secondary)' }">Email Address</label>
          <input v-model="profileForm.email" type="email" required class="input px-3 py-2.5" placeholder="your@email.com" />
        </div>
        <div>
          <label class="block text-xs font-medium mb-1.5" :style="{ color: 'var(--addrez-text-secondary)' }">Phone Number</label>
          <input v-model="profileForm.phone" class="input px-3 py-2.5" placeholder="+20-100-000-0000" />
        </div>

        <!-- Read-only info -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 pt-2">
          <div>
            <label class="block text-xs font-medium mb-1.5" :style="{ color: 'var(--addrez-text-secondary)' }">Username</label>
            <div class="px-3 py-2.5 rounded-lg text-sm" :style="{ backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)', border: '1px solid var(--addrez-border)' }">
              @{{ auth.user?.username }}
            </div>
          </div>
          <div>
            <label class="block text-xs font-medium mb-1.5" :style="{ color: 'var(--addrez-text-secondary)' }">Company</label>
            <div class="px-3 py-2.5 rounded-lg text-sm" :style="{ backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)', border: '1px solid var(--addrez-border)' }">
              {{ auth.user?.company_name || '—' }}
            </div>
          </div>
        </div>

        <div class="flex justify-end pt-2">
          <button type="submit" :disabled="profileLoading" class="btn-gold">
            <i class="pi pi-check mr-1.5"></i>{{ profileLoading ? 'Saving...' : 'Save Changes' }}
          </button>
        </div>
      </form>
    </div>

    <!-- ═══ Security Tab ═══ -->
    <div v-if="activeTab === 'security'" class="card">
      <h3 class="text-lg font-semibold mb-1" :style="{ color: 'var(--addrez-text-primary)' }">Change Password</h3>
      <p class="text-sm mb-6" :style="{ color: 'var(--addrez-text-secondary)' }">Update your password to keep your account secure.</p>

      <form @submit.prevent="changePassword" class="space-y-4 max-w-md">
        <div>
          <label class="block text-xs font-medium mb-1.5" :style="{ color: 'var(--addrez-text-secondary)' }">Current Password</label>
          <div class="relative">
            <input v-model="passwordForm.currentPassword" :type="showPwCurrent ? 'text' : 'password'" required class="input px-3 py-2.5 !pr-10" placeholder="Enter current password" />
            <button type="button" @click="showPwCurrent = !showPwCurrent" tabindex="-1" class="absolute right-3 top-1/2 -translate-y-1/2 bg-transparent border-0 cursor-pointer p-0" :style="{ color: 'var(--addrez-text-secondary)' }">
              <i :class="showPwCurrent ? 'pi pi-eye-slash' : 'pi pi-eye'" class="text-sm"></i>
            </button>
          </div>
        </div>
        <div>
          <label class="block text-xs font-medium mb-1.5" :style="{ color: 'var(--addrez-text-secondary)' }">New Password</label>
          <div class="relative">
            <input v-model="passwordForm.newPassword" :type="showPwNew ? 'text' : 'password'" required minlength="6" class="input px-3 py-2.5 !pr-10" placeholder="At least 6 characters" />
            <button type="button" @click="showPwNew = !showPwNew" tabindex="-1" class="absolute right-3 top-1/2 -translate-y-1/2 bg-transparent border-0 cursor-pointer p-0" :style="{ color: 'var(--addrez-text-secondary)' }">
              <i :class="showPwNew ? 'pi pi-eye-slash' : 'pi pi-eye'" class="text-sm"></i>
            </button>
          </div>
        </div>
        <div>
          <label class="block text-xs font-medium mb-1.5" :style="{ color: 'var(--addrez-text-secondary)' }">Confirm New Password</label>
          <div class="relative">
            <input v-model="passwordForm.confirmPassword" :type="showPwConfirm ? 'text' : 'password'" required minlength="6" class="input px-3 py-2.5 !pr-10" placeholder="Re-enter new password" />
            <button type="button" @click="showPwConfirm = !showPwConfirm" tabindex="-1" class="absolute right-3 top-1/2 -translate-y-1/2 bg-transparent border-0 cursor-pointer p-0" :style="{ color: 'var(--addrez-text-secondary)' }">
              <i :class="showPwConfirm ? 'pi pi-eye-slash' : 'pi pi-eye'" class="text-sm"></i>
            </button>
          </div>
        </div>
        <div class="flex justify-end pt-2">
          <button type="submit" :disabled="passwordLoading" class="btn-gold">
            <i class="pi pi-lock mr-1.5"></i>{{ passwordLoading ? 'Updating...' : 'Update Password' }}
          </button>
        </div>
      </form>
    </div>

    <!-- ═══ Role & Permissions Tab ═══ -->
    <div v-if="activeTab === 'access'" class="space-y-4">
      <!-- Role card -->
      <div class="card">
        <h3 class="text-lg font-semibold mb-1" :style="{ color: 'var(--addrez-text-primary)' }">Your Role</h3>
        <p class="text-sm mb-4" :style="{ color: 'var(--addrez-text-secondary)' }">Your role determines what you can access and manage in the system.</p>
        <div class="flex items-center gap-3 p-3 rounded-lg" :style="{ backgroundColor: 'var(--addrez-bg-hover)', border: '1px solid var(--addrez-border)' }">
          <div class="w-10 h-10 rounded-xl flex items-center justify-center" style="background: rgba(212,168,83,0.15)">
            <i class="pi pi-shield text-lg" style="color: var(--addrez-gold)"></i>
          </div>
          <div>
            <div class="font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ userRole }}</div>
            <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ userRoleSlug }}</div>
          </div>
        </div>
      </div>

      <!-- Outlets card -->
      <div class="card">
        <h3 class="text-lg font-semibold mb-1" :style="{ color: 'var(--addrez-text-primary)' }">Assigned Outlets</h3>
        <p class="text-sm mb-4" :style="{ color: 'var(--addrez-text-secondary)' }">Outlets you have access to.</p>
        <div v-if="userOutlets.length" class="flex flex-wrap gap-2">
          <div v-for="outlet in userOutlets" :key="outlet.id"
               class="flex items-center gap-2 px-3 py-2 rounded-lg text-sm"
               :style="{ backgroundColor: 'var(--addrez-bg-hover)', border: '1px solid var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
            <i class="pi pi-building text-xs" style="color: var(--addrez-gold)"></i>
            {{ outlet.name }}
            <span class="text-[10px] px-1.5 py-0.5 rounded-full font-medium"
                  :style="outlet.is_active ? { backgroundColor: '#10b98120', color: '#10b981' } : { backgroundColor: '#ef444420', color: '#ef4444' }">
              {{ outlet.is_active ? 'Active' : 'Inactive' }}
            </span>
          </div>
        </div>
        <p v-else class="text-sm" :style="{ color: 'var(--addrez-text-secondary)' }">No outlets assigned.</p>
      </div>

      <!-- Permissions card -->
      <div class="card">
        <h3 class="text-lg font-semibold mb-1" :style="{ color: 'var(--addrez-text-primary)' }">Permissions</h3>
        <p class="text-sm mb-4" :style="{ color: 'var(--addrez-text-secondary)' }">A detailed view of what actions you can perform.</p>

        <div v-if="Object.keys(permissionGroups).length" class="space-y-3">
          <div v-for="(actions, group) in permissionGroups" :key="group">
            <div class="text-xs font-semibold uppercase tracking-wider mb-1.5" style="color: var(--addrez-gold)">
              {{ formatGroup(group) }}
            </div>
            <div class="flex flex-wrap gap-1.5">
              <span v-for="action in actions" :key="action"
                    class="text-xs px-2 py-1 rounded-md font-medium"
                    :style="{ backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)', border: '1px solid var(--addrez-border)' }">
                {{ formatAction(action) }}
              </span>
            </div>
          </div>
        </div>
        <p v-else class="text-sm" :style="{ color: 'var(--addrez-text-secondary)' }">No permissions assigned.</p>
      </div>
    </div>
  </div>
</template>
