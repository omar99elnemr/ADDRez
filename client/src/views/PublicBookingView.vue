<script setup lang="ts">
import { ref, onMounted } from 'vue'

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5179/api'
const outletId = ref(1) // default outlet
const slots = ref<any[]>([])
const loading = ref(false)
const submitted = ref(false)
const confirmationCode = ref('')
const error = ref('')

const form = ref({
  guestName: '',
  guestEmail: '',
  guestPhone: '',
  covers: 2,
  date: '',
  time: '',
  notes: '',
  specialRequests: '',
  timeSlotId: null as number | null
})

async function loadSlots() {
  if (!form.value.date) return
  try {
    const res = await fetch(`${API_BASE}/public-booking/outlets/${outletId.value}/slots?date=${form.value.date}`)
    slots.value = await res.json()
  } catch { /* silent */ }
}

async function submitBooking() {
  error.value = ''
  if (!form.value.guestName || !form.value.date || !form.value.time) {
    error.value = 'Name, date, and time are required'
    return
  }
  loading.value = true
  try {
    const res = await fetch(`${API_BASE}/public-booking/outlets/${outletId.value}/reserve`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        guest_name: form.value.guestName,
        guest_email: form.value.guestEmail,
        guest_phone: form.value.guestPhone,
        covers: form.value.covers,
        date: form.value.date,
        time: form.value.time,
        notes: form.value.notes,
        special_requests: form.value.specialRequests,
        time_slot_id: form.value.timeSlotId
      })
    })
    const data = await res.json()
    if (!res.ok) {
      error.value = data.message || 'Booking failed'
      return
    }
    confirmationCode.value = data.confirmationCode
    submitted.value = true
  } catch {
    error.value = 'Network error. Please try again.'
  } finally {
    loading.value = false
  }
}

function getToday(): string {
  return new Date().toISOString().split('T')[0] as string
}

onMounted(() => {
  form.value.date = getToday()
  loadSlots()
})
</script>

<template>
  <div class="min-h-screen flex items-center justify-center p-4" style="background: linear-gradient(135deg, #0f0f1a 0%, #1a1a2e 50%, #16213e 100%)">
    <!-- Success State -->
    <div v-if="submitted" class="w-full max-w-md text-center">
      <div class="rounded-2xl p-8" style="background: rgba(26, 26, 46, 0.95); border: 1px solid rgba(212, 168, 83, 0.3)">
        <div class="w-20 h-20 rounded-full mx-auto flex items-center justify-center text-3xl mb-4" style="background: linear-gradient(135deg, #d4a853, #c49b48); color: #1a1a24">
          ✓
        </div>
        <h2 class="text-2xl font-bold mb-2" style="color: #d4a853">Reservation Confirmed!</h2>
        <p class="text-sm mb-6" style="color: rgba(255,255,255,0.6)">Your table has been reserved. Please save your confirmation code.</p>
        <div class="py-4 px-6 rounded-lg mb-6" style="background: rgba(212, 168, 83, 0.1); border: 2px dashed #d4a853">
          <div class="text-xs mb-1" style="color: rgba(255,255,255,0.5)">Confirmation Code</div>
          <div class="text-3xl font-mono font-bold tracking-wider" style="color: #d4a853">{{ confirmationCode }}</div>
        </div>
        <p class="text-xs" style="color: rgba(255,255,255,0.4)">Show this code at the entrance or mention it when you arrive.</p>
      </div>
    </div>

    <!-- Booking Form -->
    <div v-else class="w-full max-w-md">
      <div class="rounded-2xl p-6" style="background: rgba(26, 26, 46, 0.95); border: 1px solid rgba(212, 168, 83, 0.2)">
        <!-- Header -->
        <div class="text-center mb-6">
          <div class="text-2xl font-bold mb-1" style="color: #d4a853">ADDR<span style="color: rgba(255,255,255,0.9)">ez</span></div>
          <p class="text-sm" style="color: rgba(255,255,255,0.5)">Reserve your table online</p>
        </div>

        <div v-if="error" class="mb-4 p-3 rounded-lg text-sm text-center" style="background: rgba(239,68,68,0.1); color: #ef4444; border: 1px solid rgba(239,68,68,0.3)">
          {{ error }}
        </div>

        <form @submit.prevent="submitBooking" class="space-y-4">
          <div>
            <label class="block text-xs font-medium mb-1" style="color: rgba(255,255,255,0.5)">Your Name *</label>
            <input v-model="form.guestName" required placeholder="Full name" class="w-full px-3 py-2.5 rounded-lg text-sm" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); color: #fff; outline: none" />
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium mb-1" style="color: rgba(255,255,255,0.5)">Phone</label>
              <input v-model="form.guestPhone" placeholder="+20..." class="w-full px-3 py-2.5 rounded-lg text-sm" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); color: #fff; outline: none" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" style="color: rgba(255,255,255,0.5)">Email</label>
              <input v-model="form.guestEmail" type="email" placeholder="email@..." class="w-full px-3 py-2.5 rounded-lg text-sm" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); color: #fff; outline: none" />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium mb-1" style="color: rgba(255,255,255,0.5)">Date *</label>
              <input v-model="form.date" type="date" required :min="getToday()" @change="loadSlots" class="w-full px-3 py-2.5 rounded-lg text-sm" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); color: #fff; outline: none; color-scheme: dark" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" style="color: rgba(255,255,255,0.5)">Time *</label>
              <input v-model="form.time" type="time" required class="w-full px-3 py-2.5 rounded-lg text-sm" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); color: #fff; outline: none; color-scheme: dark" />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium mb-1" style="color: rgba(255,255,255,0.5)">Guests *</label>
              <input v-model.number="form.covers" type="number" min="1" max="20" required class="w-full px-3 py-2.5 rounded-lg text-sm" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); color: #fff; outline: none" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" style="color: rgba(255,255,255,0.5)">Time Slot</label>
              <select v-model="form.timeSlotId" class="w-full px-3 py-2.5 rounded-lg text-sm" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); color: #fff; outline: none">
                <option :value="null" style="background: #1a1a2e">Any</option>
                <option v-for="s in slots" :key="s.id" :value="s.id" :disabled="s.isFull" style="background: #1a1a2e">
                  {{ s.name }} ({{ s.startTime }}-{{ s.endTime }}) {{ s.isFull ? '— Full' : `— ${s.available} left` }}
                </option>
              </select>
            </div>
          </div>

          <div>
            <label class="block text-xs font-medium mb-1" style="color: rgba(255,255,255,0.5)">Special Requests</label>
            <textarea v-model="form.specialRequests" rows="2" placeholder="Allergies, preferences, occasion..." class="w-full px-3 py-2.5 rounded-lg text-sm resize-none" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); color: #fff; outline: none"></textarea>
          </div>

          <button
            type="submit"
            :disabled="loading"
            class="w-full py-3 rounded-lg text-sm font-bold border-0 cursor-pointer transition-all"
            style="background: linear-gradient(135deg, #d4a853, #c49b48); color: #1a1a24"
          >
            <span v-if="loading"><i class="pi pi-spin pi-spinner mr-1"></i>Booking...</span>
            <span v-else>Reserve Now</span>
          </button>
        </form>

        <p class="text-center text-xs mt-4" style="color: rgba(255,255,255,0.3)">Powered by ADDRez</p>
      </div>
    </div>
  </div>
</template>
