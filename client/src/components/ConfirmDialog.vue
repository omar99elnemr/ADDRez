<script setup lang="ts">
defineProps<{
  visible: boolean
  title?: string
  message: string
  confirmLabel?: string
  confirmColor?: string
}>()

defineEmits<{
  confirm: []
  cancel: []
}>()
</script>

<template>
  <Teleport to="body">
    <div v-if="visible" class="fixed inset-0 z-[60] flex items-center justify-center" style="background: rgba(0,0,0,0.5)" @click.self="$emit('cancel')">
      <div class="card w-full max-w-sm mx-4" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center gap-3 mb-4">
          <div class="w-10 h-10 rounded-full flex items-center justify-center shrink-0" :style="{ backgroundColor: (confirmColor || '#ef4444') + '15' }">
            <i class="pi pi-exclamation-triangle text-lg" :style="{ color: confirmColor || '#ef4444' }"></i>
          </div>
          <div>
            <h3 class="text-base font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ title || 'Confirm Action' }}</h3>
            <p class="text-sm mt-0.5" :style="{ color: 'var(--addrez-text-secondary)' }">{{ message }}</p>
          </div>
        </div>
        <div class="flex justify-end gap-2">
          <button @click="$emit('cancel')" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
          <button @click="$emit('confirm')" class="px-4 py-2 rounded-lg text-sm font-medium cursor-pointer border-0" :style="{ backgroundColor: confirmColor || '#ef4444', color: '#fff' }">{{ confirmLabel || 'Delete' }}</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
