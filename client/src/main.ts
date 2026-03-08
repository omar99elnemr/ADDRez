import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import Aura from '@primevue/themes/aura'
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'

import App from './App.vue'
import router from './router'
import './style.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)
app.use(PrimeVue, {
  theme: {
    preset: Aura,
    options: {
      darkModeSelector: '.theme-dark',
      cssLayer: false
    }
  }
})
app.use(ToastService)
app.use(ConfirmationService)

// Initialize auth store from localStorage
import { useAuthStore } from './stores/auth'
const auth = useAuthStore()
auth.init()

// Initialize theme
import { useThemeStore } from './stores/theme'
useThemeStore()

app.mount('#app')
