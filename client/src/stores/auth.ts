import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '@/services/api'
import type { User, Role, LoginRequest, LoginResponse } from '@/types'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const token = ref<string | null>(null)

  const isAuthenticated = computed(() => !!token.value)
  const permissions = computed(() => user.value?.permissions ?? [])
  const currentOutletId = ref<number | null>(null)

  function init() {
    const savedToken = localStorage.getItem('addrez_token')
    const savedUser = localStorage.getItem('addrez_user')
    const savedOutlet = localStorage.getItem('addrez_outlet_id')

    if (savedToken && savedUser) {
      token.value = savedToken
      try {
        user.value = JSON.parse(savedUser)
      } catch {
        logout()
      }
    }

    if (savedOutlet) {
      currentOutletId.value = parseInt(savedOutlet)
    }
  }

  async function login(credentials: LoginRequest): Promise<{ success: boolean; error?: string }> {
    try {
      const { data } = await api.post<LoginResponse>('/auth/login', credentials)
      token.value = data.token
      user.value = data.user

      localStorage.setItem('addrez_token', data.token)
      localStorage.setItem('addrez_user', JSON.stringify(data.user))

      // Auto-select first outlet
      if (data.user.outlets?.length > 0) {
        setOutlet(data.user.outlets[0]!.id)
      }

      return { success: true }
    } catch (err: any) {
      return { success: false, error: err.response?.data?.message ?? 'Login failed' }
    }
  }

  function setOutlet(outletId: number) {
    currentOutletId.value = outletId
    localStorage.setItem('addrez_outlet_id', String(outletId))
  }

  async function refreshUser() {
    try {
      const { data } = await api.get<User>('/auth/me')
      user.value = data
      localStorage.setItem('addrez_user', JSON.stringify(data))
    } catch { /* silent — will use cached data */ }
  }

  function logout() {
    token.value = null
    user.value = null
    currentOutletId.value = null
    localStorage.removeItem('addrez_token')
    localStorage.removeItem('addrez_user')
    localStorage.removeItem('addrez_outlet_id')
  }

  function hasPermission(permission: string): boolean {
    if (!user.value) return false
    if (user.value.roles.some((r: Role) => r.slug === 'superadmin')) return true
    return permissions.value.includes(permission)
  }

  return {
    user, token, isAuthenticated, permissions, currentOutletId,
    init, login, logout, setOutlet, hasPermission, refreshUser
  }
})
