<script setup lang="ts">
import { ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'

const toast = useToast()
const code = ref('')
const loading = ref(false)
const reservation = ref<any>(null)
const checkingIn = ref(false)

async function lookup() {
  if (!code.value.trim()) return
  loading.value = true
  reservation.value = null
  try {
    const { data } = await api.get('/gate-checker/lookup', { params: { code: code.value.trim() } })
    reservation.value = data
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Not Found', detail: err.response?.data?.message || 'No reservation found', life: 4000 })
  } finally {
    loading.value = false
  }
}

async function checkIn() {
  if (!reservation.value) return
  checkingIn.value = true
  try {
    const { data } = await api.post(`/gate-checker/${reservation.value.id}/checkin`)
    toast.add({ severity: 'success', summary: 'Checked In', detail: data.message, life: 4000 })
    reservation.value.status = 'CheckedIn'
    reservation.value.canCheckIn = false
    reservation.value.isAlreadyIn = true
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Check-in failed', life: 4000 })
  } finally {
    checkingIn.value = false
  }
}

function reset() {
  code.value = ''
  reservation.value = null
}
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">
        <i class="pi pi-shield mr-2"></i>Gate Checker
      </h1>
    </div>

    <!-- Search Card -->
    <div class="card max-w-xl mx-auto">
      <div class="text-center mb-6">
        <div class="w-16 h-16 rounded-full mx-auto flex items-center justify-center text-2xl mb-3" style="background: linear-gradient(135deg, var(--addrez-gold), #c49b48); color: #1a1a24">
          <i class="pi pi-qrcode"></i>
        </div>
        <h2 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">Scan or Enter Code</h2>
        <p class="text-sm mt-1" :style="{ color: 'var(--addrez-text-secondary)' }">Enter the confirmation code or scan QR to look up a reservation</p>
      </div>

      <div class="flex gap-2 mb-6">
        <input
          v-model="code"
          placeholder="e.g. ADR-12345"
          class="input flex-1 px-4 py-3 text-center font-mono tracking-wider"
          style="font-size: 1.1rem"
          @keyup.enter="lookup"
          autofocus
        />
        <button @click="lookup" :disabled="loading" class="btn-gold px-6 py-3 text-sm">
          <i v-if="loading" class="pi pi-spin pi-spinner mr-1"></i>
          <i v-else class="pi pi-search mr-1"></i>
          Lookup
        </button>
      </div>

      <!-- Result -->
      <div v-if="reservation" class="rounded-lg p-5" :style="{ border: '2px solid var(--addrez-gold)', backgroundColor: 'var(--addrez-bg-primary)' }">
        <div class="flex items-start justify-between mb-4">
          <div>
            <h3 class="text-xl font-bold" :style="{ color: 'var(--addrez-text-primary)' }">{{ reservation.guestName || 'Guest' }}</h3>
            <div class="text-sm mt-1" :style="{ color: 'var(--addrez-text-secondary)' }">{{ reservation.confirmationCode }}</div>
          </div>
          <span
            class="px-3 py-1 rounded-full text-xs font-bold"
            :style="{
              backgroundColor: reservation.isAlreadyIn ? '#10b98120' : reservation.canCheckIn ? '#3b82f620' : '#f59e0b20',
              color: reservation.isAlreadyIn ? '#10b981' : reservation.canCheckIn ? '#3b82f6' : '#f59e0b'
            }"
          >
            {{ reservation.status }}
          </span>
        </div>

        <div class="grid grid-cols-2 gap-3 text-sm mb-4">
          <div>
            <span class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">Time</span>
            <div class="font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ reservation.time }}</div>
          </div>
          <div>
            <span class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">Covers</span>
            <div class="font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ reservation.covers }} <span class="text-xs opacity-60">{{ reservation.seatingType }}</span></div>
          </div>
          <div>
            <span class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">Table</span>
            <div class="font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ reservation.tableName || 'Not assigned' }}</div>
          </div>
          <div>
            <span class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">Slot</span>
            <div class="font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ reservation.timeSlotName || '—' }}</div>
          </div>
        </div>

        <div v-if="reservation.notes" class="text-xs p-2 rounded mb-4" :style="{ backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)' }">
          <i class="pi pi-info-circle mr-1"></i>{{ reservation.notes }}
        </div>
        <div v-if="reservation.specialRequests" class="text-xs p-2 rounded mb-4" style="background: #f59e0b15; color: #f59e0b">
          <i class="pi pi-star mr-1"></i>{{ reservation.specialRequests }}
        </div>

        <!-- Actions -->
        <div class="flex gap-2">
          <button
            v-if="reservation.canCheckIn"
            @click="checkIn"
            :disabled="checkingIn"
            class="flex-1 py-3 rounded-lg text-sm font-bold border-0 cursor-pointer transition-colors"
            style="background: #10b981; color: #fff"
          >
            <i v-if="checkingIn" class="pi pi-spin pi-spinner mr-1"></i>
            <i v-else class="pi pi-check-circle mr-1"></i>
            CHECK IN
          </button>
          <div v-else-if="reservation.isAlreadyIn" class="flex-1 py-3 rounded-lg text-sm font-bold text-center" style="background: #10b98120; color: #10b981">
            <i class="pi pi-check-circle mr-1"></i>Already Checked In
          </div>
          <div v-else class="flex-1 py-3 rounded-lg text-sm font-bold text-center" style="background: #f59e0b20; color: #f59e0b">
            <i class="pi pi-exclamation-circle mr-1"></i>{{ reservation.status }}
          </div>
          <button @click="reset" class="px-4 py-3 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-secondary)' }">
            <i class="pi pi-refresh"></i>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
