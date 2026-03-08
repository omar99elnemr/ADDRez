import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

export type ThemeMode = 'dark' | 'light'

export const useThemeStore = defineStore('theme', () => {
  const mode = ref<ThemeMode>(
    (localStorage.getItem('addrez_theme') as ThemeMode) || 'dark'
  )

  function apply() {
    const html = document.documentElement
    html.classList.remove('theme-dark', 'theme-light')
    html.classList.add(`theme-${mode.value}`)
  }

  function toggle() {
    mode.value = mode.value === 'dark' ? 'light' : 'dark'
  }

  watch(mode, (val) => {
    localStorage.setItem('addrez_theme', val)
    apply()
  })

  // Apply immediately
  apply()

  return { mode, toggle, apply }
})
