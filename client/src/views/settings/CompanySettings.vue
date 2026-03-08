<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import type { CompanySettings } from '@/types'

const toast = useToast()
const company = ref<CompanySettings | null>(null)
const loading = ref(true)
const saving = ref(false)

async function load() {
  loading.value = true
  try {
    const { data } = await api.get<CompanySettings>('/settings/company')
    company.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load company settings', life: 3000 })
  } finally {
    loading.value = false
  }
}

async function save() {
  if (!company.value) return
  saving.value = true
  try {
    await api.put('/settings/company', {
      name: company.value.name, email: company.value.email, phone: company.value.phone,
      website: company.value.website, timezone: company.value.timezone,
      default_currency: company.value.default_currency, default_locale: company.value.default_locale
    })
    toast.add({ severity: 'success', summary: 'Saved', detail: 'Company settings updated', life: 3000 })
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to save', life: 3000 })
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <div v-if="loading" class="text-center py-8">
    <i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
  </div>
  <div v-else-if="company" class="card max-w-2xl">
    <h3 class="text-lg font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">Company Information</h3>
    <form @submit.prevent="save" class="space-y-4">
      <div>
        <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Company Name *</label>
        <input v-model="company.name" required class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
      </div>
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Email</label>
          <input v-model="company.email" type="email" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
        </div>
        <div>
          <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Phone</label>
          <input v-model="company.phone" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
        </div>
      </div>
      <div>
        <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Website</label>
        <input v-model="company.website" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
      </div>
      <div class="grid grid-cols-3 gap-4">
        <div>
          <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Timezone</label>
          <input v-model="company.timezone" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
        </div>
        <div>
          <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Currency</label>
          <input v-model="company.default_currency" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
        </div>
        <div>
          <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Locale</label>
          <input v-model="company.default_locale" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
        </div>
      </div>
      <div class="pt-2">
        <button type="submit" :disabled="saving" class="btn-gold text-sm">
          {{ saving ? 'Saving...' : 'Save Changes' }}
        </button>
      </div>
    </form>
  </div>
</template>
