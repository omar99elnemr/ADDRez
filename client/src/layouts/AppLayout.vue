<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const theme = useThemeStore()

// ── Sidebar state ──
const sidebarPinned = ref(localStorage.getItem('addrez_sidebar_pinned') !== 'false')
const sidebarHovered = ref(false)
const sidebarExpanded = computed(() => sidebarPinned.value || sidebarHovered.value)

function togglePin() {
  sidebarPinned.value = !sidebarPinned.value
  localStorage.setItem('addrez_sidebar_pinned', String(sidebarPinned.value))
}
function onSidebarEnter() { if (!sidebarPinned.value) sidebarHovered.value = true }
function onSidebarLeave() { sidebarHovered.value = false }

const showProfileMenu = ref(false)

const userRole = computed(() => auth.user?.roles?.[0]?.name ?? 'User')

function toggleProfileMenu() {
  showProfileMenu.value = !showProfileMenu.value
}

function closeProfileMenu(e: MouseEvent) {
  const target = e.target as HTMLElement
  if (!target.closest('.profile-dropdown')) {
    showProfileMenu.value = false
  }
}

onMounted(() => document.addEventListener('click', closeProfileMenu))
onUnmounted(() => document.removeEventListener('click', closeProfileMenu))

const navItems = computed(() => [
  { label: 'Dashboard', icon: 'pi pi-home', to: '/dashboard', permission: 'dashboard.view' },
  { label: 'Reservations', icon: 'pi pi-calendar', to: '/reservations', permission: 'reservations.view' },
  { label: 'Customers', icon: 'pi pi-users', to: '/customers', permission: 'customers.view' },
  { label: 'Time Slots', icon: 'pi pi-clock', to: '/time-slots', permission: 'time_slots.view' },
  { label: 'Campaigns', icon: 'pi pi-megaphone', to: '/campaigns', permission: 'campaigns.view' },
  { label: 'Reports', icon: 'pi pi-chart-bar', to: '/reports', permission: 'reports.view' },
  { label: 'Gate Checker', icon: 'pi pi-shield', to: '/gate-checker', permission: 'reservations.view' },
  { label: 'Settings', icon: 'pi pi-cog', to: '/settings', permission: 'settings.company' },
].filter(item => auth.hasPermission(item.permission)))

function isActive(path: string) {
  return route.path.startsWith(path)
}

function handleLogout() {
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <div class="flex h-screen overflow-hidden">
    <!-- ═══ Sidebar ═══ -->
    <aside
      class="sidebar"
      :class="{ 'sidebar--expanded': sidebarExpanded, 'sidebar--overlay': sidebarHovered && !sidebarPinned }"
      :style="{ backgroundColor: 'var(--addrez-sidebar-bg)', borderRight: '1px solid var(--addrez-border)' }"
      @mouseenter="onSidebarEnter"
      @mouseleave="onSidebarLeave"
    >
      <!-- Logo -->
      <div class="sidebar__header" :style="{ borderBottom: '1px solid var(--addrez-border)' }">
        <div class="w-8 h-8 min-w-[32px] rounded-lg flex items-center justify-center font-bold text-sm"
             style="background: linear-gradient(135deg, var(--addrez-gold), var(--addrez-gold-dark)); color: #1a1a24;">
          A
        </div>
        <span v-show="sidebarExpanded" class="font-bold text-lg gold-gradient whitespace-nowrap">ADDRez</span>
        <button
          v-show="sidebarExpanded"
          @click.stop="togglePin"
          class="sidebar__pin-btn"
          :class="{ 'sidebar__pin-btn--active': sidebarPinned }"
          :title="sidebarPinned ? 'Unpin sidebar' : 'Pin sidebar'"
        >
          <i :class="sidebarPinned ? 'pi pi-lock' : 'pi pi-lock-open'" class="text-xs"></i>
        </button>
      </div>

      <!-- Nav -->
      <nav class="flex-1 py-3 px-3 space-y-0.5 overflow-y-auto overflow-x-hidden">
        <router-link
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          class="sidebar__nav-item"
          :class="{ 'sidebar__nav-item--active': isActive(item.to) }"
          :title="!sidebarExpanded ? item.label : undefined"
        >
          <i :class="item.icon" class="sidebar__nav-icon"></i>
          <span v-show="sidebarExpanded" class="sidebar__nav-label">{{ item.label }}</span>
        </router-link>
      </nav>

      <!-- Sidebar footer -->
      <div class="border-t px-3 py-2" :style="{ borderColor: 'var(--addrez-border)' }">
        <button
          @click="theme.toggle()"
          class="sidebar__footer-btn"
          :title="!sidebarExpanded ? (theme.mode === 'dark' ? 'Light Mode' : 'Dark Mode') : undefined"
        >
          <i :class="theme.mode === 'dark' ? 'pi pi-sun' : 'pi pi-moon'" class="sidebar__nav-icon"></i>
          <span v-show="sidebarExpanded" class="sidebar__nav-label">{{ theme.mode === 'dark' ? 'Light' : 'Dark' }} Mode</span>
        </button>
      </div>
    </aside>

    <!-- Main content -->
    <div class="flex-1 flex flex-col overflow-hidden" :style="{ marginLeft: sidebarPinned ? '240px' : '64px', transition: 'margin-left 280ms cubic-bezier(0.4, 0, 0.2, 1)' }">
      <!-- Top bar -->
      <header
        class="flex items-center justify-between h-14 px-6 border-b"
        :style="{ backgroundColor: 'var(--addrez-bg-secondary)', borderColor: 'var(--addrez-border)' }"
      >
        <!-- Left: Page context -->
        <div class="flex items-center gap-2">
          <span class="text-sm font-medium" :style="{ color: 'var(--addrez-text-secondary)' }">{{ route.meta?.title || route.name?.toString().replace(/-/g, ' ').replace(/\b\w/g, (l: string) => l.toUpperCase()) || '' }}</span>
        </div>

        <div class="flex items-center gap-3">
          <!-- Profile dropdown -->
          <div class="relative profile-dropdown">
            <button
              @click.stop="toggleProfileMenu"
              class="flex items-center gap-2 px-2 py-1.5 rounded-lg transition-colors hover:bg-[var(--addrez-bg-hover)] bg-transparent border-0 cursor-pointer"
            >
              <div class="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold shadow-sm"
                   style="background: linear-gradient(135deg, var(--addrez-gold), var(--addrez-gold-dark)); color: #1a1a24;">
                {{ auth.user?.first_name?.charAt(0) }}{{ auth.user?.last_name?.charAt(0) }}
              </div>
              <div v-if="auth.user" class="text-left hidden md:block">
                <div class="text-sm font-medium leading-tight" :style="{ color: 'var(--addrez-text-primary)' }">{{ auth.user.full_name }}</div>
                <div class="text-[10px] leading-tight" :style="{ color: 'var(--addrez-gold)' }">{{ userRole }}</div>
              </div>
              <i class="pi pi-chevron-down text-xs hidden md:inline" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
            </button>

            <!-- Dropdown menu -->
            <div
              v-if="showProfileMenu"
              class="absolute right-0 top-full mt-2 w-64 rounded-xl shadow-xl border z-50 overflow-hidden"
              :style="{ backgroundColor: 'var(--addrez-bg-card)', borderColor: 'var(--addrez-border)' }"
            >
              <!-- User info header -->
              <div class="px-4 py-3 border-b" :style="{ borderColor: 'var(--addrez-border)', backgroundColor: 'var(--addrez-bg-hover)' }">
                <div class="font-medium text-sm" :style="{ color: 'var(--addrez-text-primary)' }">{{ auth.user?.full_name }}</div>
                <div class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ auth.user?.email }}</div>
                <div class="text-xs mt-1 px-1.5 py-0.5 rounded inline-block" style="background: var(--addrez-gold); color: #1a1a24; font-weight: 600">{{ userRole }}</div>
              </div>
              <div class="py-1">
                <router-link to="/profile" @click="showProfileMenu = false" class="w-full flex items-center gap-3 px-4 py-2.5 text-sm no-underline transition-colors hover:bg-[var(--addrez-bg-hover)]" :style="{ color: 'var(--addrez-text-primary)' }">
                  <i class="pi pi-user w-4 text-center" :style="{ color: 'var(--addrez-text-secondary)' }"></i> My Profile
                </router-link>
              </div>
              <div class="border-t py-1" :style="{ borderColor: 'var(--addrez-border)' }">
                <button @click="handleLogout" class="w-full flex items-center gap-3 px-4 py-2.5 text-sm bg-transparent border-0 cursor-pointer transition-colors hover:bg-[var(--addrez-bg-hover)]" style="color: #ef4444">
                  <i class="pi pi-sign-out w-4 text-center"></i> Sign Out
                </button>
              </div>
            </div>
          </div>
        </div>
      </header>

      <!-- Page content -->
      <main class="flex-1 overflow-y-auto p-6" :style="{ backgroundColor: 'var(--addrez-bg-primary)' }">
        <router-view />
      </main>
    </div>

  </div>
</template>
