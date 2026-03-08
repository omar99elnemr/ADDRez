<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'

const router = useRouter()
const auth = useAuthStore()
const theme = useThemeStore()

const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)
const showPassword = ref(false)

const demoUsers = [
  { username: 'admin', role: 'Super Admin', icon: 'pi-crown', color: '#d4a853' },
  { username: 'sarah.mgr', role: 'Manager', icon: 'pi-briefcase', color: '#3b82f6' },
  { username: 'ahmed.host', role: 'Host', icon: 'pi-user', color: '#10b981' },
  { username: 'fatima.gate', role: 'Gate Checker', icon: 'pi-shield', color: '#a855f7' },
]

async function handleLogin() {
  error.value = ''
  loading.value = true
  const result = await auth.login({ username: username.value.trim(), password: password.value })
  loading.value = false

  if (result.success) {
    router.push('/dashboard')
  } else {
    error.value = result.error ?? 'Login failed'
  }
}

function quickLogin(u: string) {
  username.value = u
  password.value = 'password'
  handleLogin()
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center px-4" :style="{ backgroundColor: 'var(--addrez-bg-primary)' }">
    <div class="w-full max-w-md">
      <!-- Logo -->
      <div class="text-center mb-8">
        <div class="inline-flex items-center justify-center w-16 h-16 rounded-2xl mb-4"
             style="background: linear-gradient(135deg, var(--addrez-gold), var(--addrez-gold-dark));">
          <span class="text-2xl font-bold" style="color: #1a1a24;">A</span>
        </div>
        <h1 class="text-3xl font-bold gold-gradient">ADDRez</h1>
        <p class="mt-2 text-sm" :style="{ color: 'var(--addrez-text-secondary)' }">Reservation & Guest Management</p>
      </div>

      <!-- Login Card -->
      <div class="card">
        <h2 class="text-xl font-semibold mb-6" :style="{ color: 'var(--addrez-text-primary)' }">Sign In</h2>

        <div v-if="error" class="mb-4 p-3 rounded-lg text-sm font-medium" style="background: rgba(239, 68, 68, 0.1); color: #ef4444;">
          {{ error }}
        </div>

        <form @submit.prevent="handleLogin" class="space-y-4">
          <div>
            <label class="block text-sm font-medium mb-1.5" :style="{ color: 'var(--addrez-text-secondary)' }">Username</label>
            <input
              v-model="username"
              type="text"
              required
              autofocus
              placeholder="Enter your username"
              class="input px-4 py-2.5"
            />
          </div>

          <div>
            <label class="block text-sm font-medium mb-1.5" :style="{ color: 'var(--addrez-text-secondary)' }">Password</label>
            <div class="relative">
              <input
                v-model="password"
                :type="showPassword ? 'text' : 'password'"
                required
                placeholder="Enter your password"
                class="input px-4 py-2.5 !pr-10"
              />
              <button
                type="button"
                @click="showPassword = !showPassword"
                class="absolute right-3 top-1/2 -translate-y-1/2 bg-transparent border-0 cursor-pointer p-0"
                :style="{ color: 'var(--addrez-text-secondary)' }"
                tabindex="-1"
              >
                <i :class="showPassword ? 'pi pi-eye-slash' : 'pi pi-eye'" class="text-sm"></i>
              </button>
            </div>
          </div>

          <button
            type="submit"
            :disabled="loading"
            class="btn-gold w-full py-3 text-sm rounded-lg"
          >
            {{ loading ? 'Signing in...' : 'Sign In' }}
          </button>
        </form>

        <div class="mt-6 pt-4 border-t" :style="{ borderColor: 'var(--addrez-border)' }">
          <p class="text-xs mb-3 text-center" :style="{ color: 'var(--addrez-text-secondary)' }">Quick Demo Login (password: <code>password</code>)</p>
          <div class="grid grid-cols-2 gap-2">
            <button
              v-for="d in demoUsers" :key="d.username"
              @click="quickLogin(d.username)"
              :disabled="loading"
              class="flex items-center gap-2 px-3 py-2 rounded-lg border text-xs cursor-pointer transition-colors bg-transparent hover:opacity-80"
              :style="{ borderColor: d.color + '40', color: d.color }"
            >
              <i :class="'pi ' + d.icon"></i>
              <div class="text-left">
                <div class="font-medium">{{ d.username }}</div>
                <div class="opacity-60 text-[10px]">{{ d.role }}</div>
              </div>
            </button>
          </div>
        </div>
      </div>

      <!-- Theme toggle -->
      <div class="text-center mt-4">
        <button
          @click="theme.toggle()"
          class="text-xs px-3 py-1.5 rounded-lg bg-transparent border-0 cursor-pointer transition-colors hover:bg-[var(--addrez-bg-hover)]"
          :style="{ color: 'var(--addrez-text-secondary)' }"
        >
          <i :class="theme.mode === 'dark' ? 'pi pi-sun' : 'pi pi-moon'" class="mr-1"></i>
          {{ theme.mode === 'dark' ? 'Light Mode' : 'Dark Mode' }}
        </button>
      </div>
    </div>
  </div>
</template>
