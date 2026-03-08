<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'

const toast = useToast()

const activeTab = ref<'global' | 'frequent' | 'spender' | 'noshow'>('global')
const loading = ref(true)
const saving = ref(false)

// ── Global Config ──
const globalConfig = ref<Record<string, string>>({
  'global.gender_mandatory_new_profile': 'false',
  'global.category_required_new_profile': 'false',
  'global.category_required_checkin': 'false',
  'global.category_required_close_cheque': 'false',
  'global.event_type_required_checkin': 'false',
  'global.default_reservation_type': 'Seated',
  'global.allow_on_behalf_reservations': 'true',
  'global.stats_bar_location': 'Top',
  'global.new_reservation_default_status': 'Pending',
  'global.send_sms_on_confirming': 'true',
  'global.send_sms_on_cancelling': 'true',
  'global.convert_to_guest_list_on_confirm': 'false',
  'global.auto_noshow_unattended': 'false',
})

const globalFields = [
  { section: 'Customer Profile', items: [
    { key: 'global.gender_mandatory_new_profile', label: 'Gender is mandatory when creating new profile', type: 'toggle' },
    { key: 'global.category_required_new_profile', label: 'Client Category is required when creating new profile', type: 'toggle' },
    { key: 'global.category_required_checkin', label: 'Client Category is required when checking in a guest', type: 'toggle' },
    { key: 'global.category_required_close_cheque', label: 'Client Category is required when closing a cheque', type: 'toggle' },
    { key: 'global.event_type_required_checkin', label: 'Event type is required when checking in a guest', type: 'toggle' },
  ]},
  { section: 'Reservations', items: [
    { key: 'global.default_reservation_type', label: 'Default Reservation Type', type: 'choice', options: ['Seated', 'Standing'] },
    { key: 'global.allow_on_behalf_reservations', label: 'Allow On Behalf Reservations', type: 'toggle' },
    { key: 'global.stats_bar_location', label: 'Stats Bar Location', type: 'choice', options: ['Top', 'Bottom'] },
    { key: 'global.new_reservation_default_status', label: 'New Reservations Default Status', type: 'choice', options: ['Pending', 'Confirmed'] },
    { key: 'global.send_sms_on_confirming', label: 'Default Send SMS On Confirming Reservation', type: 'toggle' },
    { key: 'global.send_sms_on_cancelling', label: 'Default Send SMS On Canceling Reservation', type: 'toggle' },
    { key: 'global.convert_to_guest_list_on_confirm', label: 'Convert to Generic Guest list when confirmation a reservation', type: 'toggle' },
    { key: 'global.auto_noshow_unattended', label: 'Auto mark all unattended reservations as "No Show" that is older than one day', type: 'toggle' },
  ]},
]

// ── Per-outlet configs ──
interface OutletConfig {
  outlet_id: number
  outlet_name: string
  configs: Record<string, string>
}

const frequentOutlets = ref<OutletConfig[]>([])
const spenderOutlets = ref<OutletConfig[]>([])
const noshowOutlets = ref<OutletConfig[]>([])

// ── Load ──
async function loadGlobalConfig() {
  try {
    const { data } = await api.get<Record<string, string>>('/settings/configurations/global')
    for (const [key, val] of Object.entries(data)) {
      globalConfig.value[key] = val
    }
  } catch { /* use defaults */ }
}

async function loadOutletConfigs(prefix: string): Promise<OutletConfig[]> {
  try {
    const { data } = await api.get<OutletConfig[]>('/settings/configurations/outlets', { params: { prefix } })
    return data
  } catch { return [] }
}

async function loadAll() {
  loading.value = true
  const [, freq, spend, noshow] = await Promise.all([
    loadGlobalConfig(),
    loadOutletConfigs('frequent'),
    loadOutletConfigs('spender'),
    loadOutletConfigs('noshow'),
  ])
  frequentOutlets.value = freq
  spenderOutlets.value = spend
  noshowOutlets.value = noshow
  loading.value = false
}

// ── Save ──
async function saveGlobal() {
  saving.value = true
  try {
    await api.put('/settings/configurations/global', { configs: globalConfig.value })
    toast.add({ severity: 'success', summary: 'Saved', detail: 'Global configuration saved', life: 2000 })
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to save', life: 3000 })
  } finally { saving.value = false }
}

async function saveOutletConfig(outletId: number, configs: Record<string, string>) {
  saving.value = true
  try {
    await api.put(`/settings/configurations/outlets/${outletId}`, { configs })
    toast.add({ severity: 'success', summary: 'Saved', detail: 'Configuration saved', life: 2000 })
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to save', life: 3000 })
  } finally { saving.value = false }
}

function getOrDefault(configs: Record<string, string>, key: string, defaultVal: string): string {
  return configs[key] ?? defaultVal
}

function setConfig(configs: Record<string, string>, key: string, val: string) {
  configs[key] = val
}

onMounted(loadAll)
</script>

<template>
  <div>
    <!-- Tabs -->
    <div class="flex items-center gap-2 mb-6">
      <button
        v-for="tab in [
          { key: 'global', label: 'Global Config', icon: 'pi-cog' },
          { key: 'frequent', label: 'Frequent Config', icon: 'pi-chart-bar' },
          { key: 'spender', label: 'Spender Config', icon: 'pi-wallet' },
          { key: 'noshow', label: 'No Show Config', icon: 'pi-times-circle' },
        ]"
        :key="tab.key"
        @click="activeTab = tab.key as any"
        class="px-4 py-2 rounded-lg text-sm font-medium border-0 cursor-pointer transition-colors"
        :style="activeTab === tab.key
          ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' }
          : { backgroundColor: 'var(--addrez-bg-card)', color: 'var(--addrez-text-secondary)' }"
      >
        <i :class="'pi ' + tab.icon" class="mr-1.5"></i>{{ tab.label }}
      </button>
    </div>

    <div v-if="loading" class="text-center py-12">
      <i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
    </div>

    <!-- ════════════════════════════════════════════ -->
    <!-- ═══ GLOBAL CONFIG TAB ═══ -->
    <!-- ════════════════════════════════════════════ -->
    <template v-else-if="activeTab === 'global'">
      <div class="card mb-4">
        <p class="text-xs mb-4" :style="{ color: 'var(--addrez-text-secondary)' }">(These settings are shared among all users under the same management company)</p>

        <div v-for="section in globalFields" :key="section.section" class="mb-6">
          <h3 class="text-base font-semibold mb-3 pb-2 border-b" :style="{ color: 'var(--addrez-text-primary)', borderColor: 'var(--addrez-border)' }">{{ section.section }}</h3>
          <div class="space-y-4">
            <div v-for="field in section.items" :key="field.key" class="flex items-center justify-between py-2">
              <span class="text-sm" :style="{ color: 'var(--addrez-text-primary)' }">{{ field.label }}</span>

              <!-- Toggle -->
              <template v-if="field.type === 'toggle'">
                <button
                  @click="globalConfig[field.key] = globalConfig[field.key] === 'true' ? 'false' : 'true'"
                  class="relative w-11 h-6 rounded-full border-0 cursor-pointer transition-colors"
                  :style="{ backgroundColor: globalConfig[field.key] === 'true' ? 'var(--addrez-gold)' : '#4b5563' }"
                >
                  <span class="absolute top-0.5 w-5 h-5 rounded-full bg-white transition-all shadow-sm"
                    :style="{ left: globalConfig[field.key] === 'true' ? '22px' : '2px' }"></span>
                </button>
              </template>

              <!-- Choice (radio-like) -->
              <template v-else-if="field.type === 'choice'">
                <div class="flex items-center gap-1 rounded-lg overflow-hidden border" :style="{ borderColor: 'var(--addrez-border)' }">
                  <button
                    v-for="opt in field.options" :key="opt"
                    @click="globalConfig[field.key] = opt"
                    class="px-3 py-1.5 text-xs font-medium border-0 cursor-pointer transition-colors"
                    :style="globalConfig[field.key] === opt
                      ? { backgroundColor: 'var(--addrez-gold)', color: '#1a1a24' }
                      : { backgroundColor: 'transparent', color: 'var(--addrez-text-secondary)' }"
                  >{{ opt }}</button>
                </div>
              </template>
            </div>
          </div>
        </div>

        <p class="text-[10px] mt-2" :style="{ color: 'var(--addrez-text-secondary)' }">* System checks unattended reservations at the end of the day.</p>
      </div>

      <div class="flex justify-end">
        <button @click="saveGlobal" :disabled="saving" class="btn-gold text-sm">
          <i class="pi pi-save mr-1"></i>{{ saving ? 'Saving...' : 'Save' }}
        </button>
      </div>
    </template>

    <!-- ════════════════════════════════════════════ -->
    <!-- ═══ FREQUENT CONFIG TAB ═══ -->
    <!-- ════════════════════════════════════════════ -->
    <template v-else-if="activeTab === 'frequent'">
      <div v-if="frequentOutlets.length === 0" class="card text-center py-8">
        <p :style="{ color: 'var(--addrez-text-secondary)' }">No active outlets found.</p>
      </div>
      <div v-else class="space-y-6">
        <div v-for="outlet in frequentOutlets" :key="outlet.outlet_id" class="card">
          <h3 class="text-base font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">
            <span class="text-xs font-normal mr-2" :style="{ color: 'var(--addrez-text-secondary)' }">Venue Name</span>
            {{ outlet.outlet_name }}
          </h3>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Number Of Visits</label>
              <input
                type="number" min="1"
                :value="getOrDefault(outlet.configs, 'frequent.visits', '15')"
                @input="setConfig(outlet.configs, 'frequent.visits', ($event.target as HTMLInputElement).value)"
                class="w-full px-3 py-2 rounded-lg border text-sm"
                :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
              />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Duration(Days)</label>
              <input
                type="number" min="1"
                :value="getOrDefault(outlet.configs, 'frequent.duration_days', '90')"
                @input="setConfig(outlet.configs, 'frequent.duration_days', ($event.target as HTMLInputElement).value)"
                class="w-full px-3 py-2 rounded-lg border text-sm"
                :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
              />
            </div>
          </div>
        </div>
        <div class="flex justify-end">
          <button @click="frequentOutlets.forEach(o => saveOutletConfig(o.outlet_id, o.configs))" :disabled="saving" class="btn-gold text-sm">
            <i class="pi pi-save mr-1"></i>{{ saving ? 'Saving...' : 'Save' }}
          </button>
        </div>
      </div>
    </template>

    <!-- ════════════════════════════════════════════ -->
    <!-- ═══ SPENDER CONFIG TAB ═══ -->
    <!-- ════════════════════════════════════════════ -->
    <template v-else-if="activeTab === 'spender'">
      <div v-if="spenderOutlets.length === 0" class="card text-center py-8">
        <p :style="{ color: 'var(--addrez-text-secondary)' }">No active outlets found.</p>
      </div>
      <div v-else class="space-y-6">
        <div v-for="outlet in spenderOutlets" :key="outlet.outlet_id" class="card">
          <h3 class="text-base font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">
            <span class="text-xs font-normal mr-2" :style="{ color: 'var(--addrez-text-secondary)' }">Venue Name</span>
            {{ outlet.outlet_name }}
          </h3>
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Duration(Days)</label>
              <input
                type="number" min="1"
                :value="getOrDefault(outlet.configs, 'spender.duration_days', '90')"
                @input="setConfig(outlet.configs, 'spender.duration_days', ($event.target as HTMLInputElement).value)"
                class="w-full px-3 py-2 rounded-lg border text-sm"
                :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
              />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">High Spender</label>
              <input
                type="number" min="0"
                :value="getOrDefault(outlet.configs, 'spender.high', '1000')"
                @input="setConfig(outlet.configs, 'spender.high', ($event.target as HTMLInputElement).value)"
                class="w-full px-3 py-2 rounded-lg border text-sm"
                :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
              />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Medium Spender</label>
              <input
                type="number" min="0"
                :value="getOrDefault(outlet.configs, 'spender.medium', '500')"
                @input="setConfig(outlet.configs, 'spender.medium', ($event.target as HTMLInputElement).value)"
                class="w-full px-3 py-2 rounded-lg border text-sm"
                :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
              />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Low Spender</label>
              <input
                type="number" min="0"
                :value="getOrDefault(outlet.configs, 'spender.low', '100')"
                @input="setConfig(outlet.configs, 'spender.low', ($event.target as HTMLInputElement).value)"
                class="w-full px-3 py-2 rounded-lg border text-sm"
                :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
              />
            </div>
          </div>
        </div>
        <div class="flex justify-end">
          <button @click="spenderOutlets.forEach(o => saveOutletConfig(o.outlet_id, o.configs))" :disabled="saving" class="btn-gold text-sm">
            <i class="pi pi-save mr-1"></i>{{ saving ? 'Saving...' : 'Save' }}
          </button>
        </div>
      </div>
    </template>

    <!-- ════════════════════════════════════════════ -->
    <!-- ═══ NO SHOW CONFIG TAB ═══ -->
    <!-- ════════════════════════════════════════════ -->
    <template v-else-if="activeTab === 'noshow'">
      <div v-if="noshowOutlets.length === 0" class="card text-center py-8">
        <p :style="{ color: 'var(--addrez-text-secondary)' }">No active outlets found.</p>
      </div>
      <div v-else class="space-y-6">
        <div v-for="outlet in noshowOutlets" :key="outlet.outlet_id" class="card">
          <h3 class="text-base font-semibold mb-4" :style="{ color: 'var(--addrez-text-primary)' }">
            <span class="text-xs font-normal mr-2" :style="{ color: 'var(--addrez-text-secondary)' }">Venue Name</span>
            {{ outlet.outlet_name }}
          </h3>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Number Of No Shows</label>
              <input
                type="number" min="1"
                :value="getOrDefault(outlet.configs, 'noshow.count', '5')"
                @input="setConfig(outlet.configs, 'noshow.count', ($event.target as HTMLInputElement).value)"
                class="w-full px-3 py-2 rounded-lg border text-sm"
                :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
              />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Duration(Days)</label>
              <input
                type="number" min="1"
                :value="getOrDefault(outlet.configs, 'noshow.duration_days', '90')"
                @input="setConfig(outlet.configs, 'noshow.duration_days', ($event.target as HTMLInputElement).value)"
                class="w-full px-3 py-2 rounded-lg border text-sm"
                :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
              />
            </div>
          </div>
        </div>
        <div class="flex justify-end">
          <button @click="noshowOutlets.forEach(o => saveOutletConfig(o.outlet_id, o.configs))" :disabled="saving" class="btn-gold text-sm">
            <i class="pi pi-save mr-1"></i>{{ saving ? 'Saving...' : 'Save' }}
          </button>
        </div>
      </div>
    </template>
  </div>
</template>
