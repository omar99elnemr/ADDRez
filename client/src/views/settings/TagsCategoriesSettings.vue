<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import api from '@/services/api'
import type { TagCategory, TagSettings, Category } from '@/types'
import ConfirmDialog from '@/components/ConfirmDialog.vue'

const toast = useToast()

// ── State ──
const activeTab = ref<'customer_tags' | 'reservation_tags' | 'client_categories'>('customer_tags')
const loading = ref(true)

// Tag Categories & Tags
const tagCategories = ref<TagCategory[]>([])
const uncategorizedTags = ref<TagSettings[]>([])

// Client Categories
const clientCategories = ref<Category[]>([])

// Dialogs
const showCategoryDialog = ref(false)
const showTagDialog = ref(false)
const showClientCatDialog = ref(false)
const saving = ref(false)

// Tag category form
const editingCategoryId = ref<number | null>(null)
const categoryForm = ref({ name: '', icon: '' })

// Tag form
const editingTagId = ref<number | null>(null)
const tagForm = ref({ name: '', color: '#d4a853', tag_category_id: null as number | null })
const tagFormType = ref<'Customer' | 'Reservation'>('Customer')

// Client category form
const editingClientCatId = ref<number | null>(null)
const clientCatForm = ref({ name: '', description: '', color: '#d4a853', priority: 1, is_active: true })

// Confirm dialog
const confirmVisible = ref(false)
const confirmTitle = ref('')
const confirmMessage = ref('')
const confirmCallback = ref<(() => void) | null>(null)

const presetColors = ['#ef4444', '#f59e0b', '#22c55e', '#3b82f6', '#8b5cf6', '#ec4899', '#06b6d4', '#d4a853', '#dc2626', '#16a34a', '#7c3aed', '#0891b2']

// ── Computed ──
const currentTagType = computed(() => activeTab.value === 'customer_tags' ? 'Customer' : 'Reservation')

const filteredCategories = computed(() =>
  tagCategories.value.filter(tc => tc.type === currentTagType.value)
)

const filteredUncategorized = computed(() =>
  uncategorizedTags.value.filter(t => t.type === currentTagType.value)
)

// ── Load Data ──
async function loadTagCategories() {
  try {
    const { data } = await api.get<TagCategory[]>('/settings/tag-categories')
    tagCategories.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load tag categories', life: 3000 })
  }
}

async function loadUncategorizedTags() {
  try {
    const { data } = await api.get<TagSettings[]>('/settings/tags')
    uncategorizedTags.value = data.filter(t => !t.tag_category_id)
  } catch {
    // silently fail
  }
}

async function loadClientCategories() {
  try {
    const { data } = await api.get<Category[]>('/settings/categories')
    clientCategories.value = data
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load categories', life: 3000 })
  }
}

async function loadAll() {
  loading.value = true
  await Promise.all([loadTagCategories(), loadUncategorizedTags(), loadClientCategories()])
  loading.value = false
}

// ── Tag Category CRUD ──
function openCreateCategory() {
  editingCategoryId.value = null
  categoryForm.value = { name: '', icon: '' }
  showCategoryDialog.value = true
}

function openEditCategory(tc: TagCategory) {
  editingCategoryId.value = tc.id
  categoryForm.value = { name: tc.name, icon: tc.icon || '' }
  showCategoryDialog.value = true
}

async function saveCategory() {
  if (!categoryForm.value.name.trim()) return
  saving.value = true
  try {
    if (editingCategoryId.value) {
      const existing = tagCategories.value.find(tc => tc.id === editingCategoryId.value)
      await api.put(`/settings/tag-categories/${editingCategoryId.value}`, {
        name: categoryForm.value.name,
        icon: categoryForm.value.icon || null,
        sort_order: existing?.sort_order ?? 0
      })
      toast.add({ severity: 'success', summary: 'Updated', detail: 'Tag category updated', life: 2000 })
    } else {
      await api.post('/settings/tag-categories', {
        name: categoryForm.value.name,
        icon: categoryForm.value.icon || null,
        type: currentTagType.value
      })
      toast.add({ severity: 'success', summary: 'Created', detail: 'Tag category created', life: 2000 })
    }
    showCategoryDialog.value = false
    await loadTagCategories()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Save failed', life: 3000 })
  } finally { saving.value = false }
}

function requestDeleteCategory(tc: TagCategory) {
  confirmTitle.value = 'Delete Tag Category'
  confirmMessage.value = `Delete category "${tc.name}"? Tags inside will be kept but uncategorized.`
  confirmCallback.value = async () => {
    try {
      await api.delete(`/settings/tag-categories/${tc.id}`)
      toast.add({ severity: 'success', summary: 'Deleted', detail: 'Tag category deleted', life: 2000 })
      await Promise.all([loadTagCategories(), loadUncategorizedTags()])
    } catch {
      toast.add({ severity: 'error', summary: 'Error', detail: 'Delete failed', life: 3000 })
    }
  }
  confirmVisible.value = true
}

// ── Tag CRUD ──
function openAddTag(categoryId: number | null) {
  editingTagId.value = null
  tagForm.value = { name: '', color: '#d4a853', tag_category_id: categoryId }
  tagFormType.value = currentTagType.value as 'Customer' | 'Reservation'
  showTagDialog.value = true
}

function openEditTag(t: TagSettings) {
  editingTagId.value = t.id
  tagForm.value = { name: t.name, color: t.color, tag_category_id: t.tag_category_id ?? null }
  tagFormType.value = t.type as 'Customer' | 'Reservation'
  showTagDialog.value = true
}

async function saveTag() {
  if (!tagForm.value.name.trim()) return
  saving.value = true
  try {
    if (editingTagId.value) {
      await api.put(`/settings/tags/${editingTagId.value}`, {
        name: tagForm.value.name,
        color: tagForm.value.color,
        tag_category_id: tagForm.value.tag_category_id
      })
      toast.add({ severity: 'success', summary: 'Updated', detail: 'Tag updated', life: 2000 })
    } else {
      await api.post('/settings/tags', {
        name: tagForm.value.name,
        color: tagForm.value.color,
        type: tagFormType.value,
        tag_category_id: tagForm.value.tag_category_id
      })
      toast.add({ severity: 'success', summary: 'Created', detail: 'Tag created', life: 2000 })
    }
    showTagDialog.value = false
    await Promise.all([loadTagCategories(), loadUncategorizedTags()])
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Save failed', life: 3000 })
  } finally { saving.value = false }
}

function requestDeleteTag(t: TagSettings) {
  confirmTitle.value = 'Delete Tag'
  confirmMessage.value = `Delete tag "${t.name}"? It will be removed from all customers/reservations using it.`
  confirmCallback.value = async () => {
    try {
      await api.delete(`/settings/tags/${t.id}`)
      toast.add({ severity: 'success', summary: 'Deleted', detail: 'Tag deleted', life: 2000 })
      await Promise.all([loadTagCategories(), loadUncategorizedTags()])
    } catch {
      toast.add({ severity: 'error', summary: 'Error', detail: 'Delete failed', life: 3000 })
    }
  }
  confirmVisible.value = true
}

// ── Client Category CRUD ──
function openCreateClientCat() {
  editingClientCatId.value = null
  clientCatForm.value = { name: '', description: '', color: '#d4a853', priority: clientCategories.value.length + 1, is_active: true }
  showClientCatDialog.value = true
}

function openEditClientCat(c: Category) {
  editingClientCatId.value = c.id
  clientCatForm.value = { name: c.name, description: c.description || '', color: c.color, priority: c.priority, is_active: c.is_active }
  showClientCatDialog.value = true
}

async function saveClientCat() {
  if (!clientCatForm.value.name.trim()) return
  saving.value = true
  try {
    if (editingClientCatId.value) {
      await api.put(`/settings/categories/${editingClientCatId.value}`, clientCatForm.value)
      toast.add({ severity: 'success', summary: 'Updated', detail: 'Category updated', life: 2000 })
    } else {
      await api.post('/settings/categories', clientCatForm.value)
      toast.add({ severity: 'success', summary: 'Created', detail: 'Category created', life: 2000 })
    }
    showClientCatDialog.value = false
    await loadClientCategories()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Save failed', life: 3000 })
  } finally { saving.value = false }
}

function requestDeleteClientCat(c: Category) {
  confirmTitle.value = 'Delete Client Category'
  confirmMessage.value = `Delete category "${c.name}"? Customers in this category will be unlinked.`
  confirmCallback.value = async () => {
    try {
      await api.delete(`/settings/categories/${c.id}`)
      toast.add({ severity: 'success', summary: 'Deleted', detail: 'Category deleted', life: 2000 })
      await loadClientCategories()
    } catch {
      toast.add({ severity: 'error', summary: 'Error', detail: 'Delete failed', life: 3000 })
    }
  }
  confirmVisible.value = true
}

function doConfirm() {
  confirmVisible.value = false
  confirmCallback.value?.()
}

onMounted(loadAll)
</script>

<template>
  <div>
    <!-- ═══ Tabs: Customer Tags / Reservation Tags / Client Categories ═══ -->
    <div class="flex items-center gap-2 mb-6">
      <button
        v-for="tab in [
          { key: 'customer_tags', label: 'Customer Tags', icon: 'pi-user' },
          { key: 'reservation_tags', label: 'Reservation Tags', icon: 'pi-calendar' },
          { key: 'client_categories', label: 'Client Categories', icon: 'pi-th-large' }
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

    <!-- ════════════════════════════════════════════════════════════════ -->
    <!-- ═══ CUSTOMER TAGS / RESERVATION TAGS TAB ═══ -->
    <!-- ════════════════════════════════════════════════════════════════ -->
    <template v-else-if="activeTab === 'customer_tags' || activeTab === 'reservation_tags'">

      <div v-if="filteredCategories.length === 0 && filteredUncategorized.length === 0" class="text-center py-12 card">
        <i class="pi pi-tags text-4xl mb-3" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
        <p :style="{ color: 'var(--addrez-text-secondary)' }">No {{ currentTagType.toLowerCase() }} tags configured yet.</p>
        <p class="text-xs mt-1" :style="{ color: 'var(--addrez-text-secondary)' }">Create a tag category first, then add tags inside it.</p>
      </div>

      <!-- Tag Categories with inline tags -->
      <div class="space-y-5">
        <div v-for="tc in filteredCategories" :key="tc.id" class="card">
          <!-- Category Header -->
          <div class="flex items-center justify-between mb-3">
            <div class="flex items-center gap-2">
              <span v-if="tc.icon" class="text-lg">{{ tc.icon }}</span>
              <h3 class="text-base font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ tc.name }}</h3>
            </div>
            <div class="flex items-center gap-1">
              <button @click="openEditCategory(tc)" class="p-1.5 rounded bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }">
                <i class="pi pi-pencil text-xs"></i>
              </button>
              <button @click="requestDeleteCategory(tc)" class="p-1.5 rounded bg-transparent border-0 cursor-pointer" style="color: #ef4444">
                <i class="pi pi-trash text-xs"></i>
              </button>
            </div>
          </div>

          <!-- Tags as pills + Add More -->
          <div class="flex flex-wrap items-center gap-2">
            <div v-for="t in tc.tags" :key="t.id" class="group flex items-center gap-1.5 px-3 py-1.5 rounded-full text-sm font-medium transition-all"
              :style="{ backgroundColor: t.color + '20', color: t.color, border: '1px solid ' + t.color + '40' }">
              <span>{{ t.name }}</span>
              <button @click.stop="openEditTag(t)" class="opacity-0 group-hover:opacity-100 transition-opacity bg-transparent border-0 cursor-pointer p-0 ml-0.5"
                :style="{ color: t.color }">
                <i class="pi pi-pencil" style="font-size: 10px"></i>
              </button>
              <button @click.stop="requestDeleteTag(t)" class="opacity-0 group-hover:opacity-100 transition-opacity bg-transparent border-0 cursor-pointer p-0"
                style="color: #ef4444">
                <i class="pi pi-times" style="font-size: 10px"></i>
              </button>
            </div>

            <!-- Add More button -->
            <button @click="openAddTag(tc.id)"
              class="flex items-center gap-1 px-3 py-1.5 rounded-full text-xs font-medium border cursor-pointer transition-colors bg-transparent"
              :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-secondary)' }">
              <i class="pi pi-plus" style="font-size: 10px"></i> Add More...
            </button>
          </div>
        </div>

        <!-- Uncategorized Tags -->
        <div v-if="filteredUncategorized.length > 0" class="card">
          <h3 class="text-base font-semibold mb-3" :style="{ color: 'var(--addrez-text-secondary)' }">Uncategorized</h3>
          <div class="flex flex-wrap items-center gap-2">
            <div v-for="t in filteredUncategorized" :key="t.id" class="group flex items-center gap-1.5 px-3 py-1.5 rounded-full text-sm font-medium"
              :style="{ backgroundColor: t.color + '20', color: t.color, border: '1px solid ' + t.color + '40' }">
              <span>{{ t.name }}</span>
              <button @click.stop="openEditTag(t)" class="opacity-0 group-hover:opacity-100 transition-opacity bg-transparent border-0 cursor-pointer p-0 ml-0.5"
                :style="{ color: t.color }">
                <i class="pi pi-pencil" style="font-size: 10px"></i>
              </button>
              <button @click.stop="requestDeleteTag(t)" class="opacity-0 group-hover:opacity-100 transition-opacity bg-transparent border-0 cursor-pointer p-0"
                style="color: #ef4444">
                <i class="pi pi-times" style="font-size: 10px"></i>
              </button>
            </div>
            <button @click="openAddTag(null)"
              class="flex items-center gap-1 px-3 py-1.5 rounded-full text-xs font-medium border cursor-pointer transition-colors bg-transparent"
              :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-secondary)' }">
              <i class="pi pi-plus" style="font-size: 10px"></i> Add More...
            </button>
          </div>
        </div>
      </div>

      <!-- Add New Category button -->
      <button @click="openCreateCategory" class="mt-5 btn-gold text-sm">
        <i class="pi pi-plus mr-1.5"></i>Add New Category
      </button>
    </template>

    <!-- ════════════════════════════════════════════════════════════════ -->
    <!-- ═══ CLIENT CATEGORIES TAB ═══ -->
    <!-- ════════════════════════════════════════════════════════════════ -->
    <template v-else-if="activeTab === 'client_categories'">
      <div class="flex items-center justify-between mb-4">
        <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">Client Categories</h3>
        <button @click="openCreateClientCat" class="btn-gold text-sm"><i class="pi pi-plus mr-1"></i>Add New</button>
      </div>

      <div v-if="clientCategories.length === 0" class="text-center py-12 card">
        <i class="pi pi-th-large text-4xl mb-3" :style="{ color: 'var(--addrez-text-secondary)' }"></i>
        <p :style="{ color: 'var(--addrez-text-secondary)' }">No client categories configured yet.</p>
      </div>

      <div v-else class="card overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr :style="{ color: 'var(--addrez-text-secondary)' }">
              <th class="text-left py-3 px-4 font-medium">Name</th>
              <th class="text-left py-3 px-4 font-medium">Description</th>
              <th class="text-center py-3 px-4 font-medium">Color Code</th>
              <th class="text-center py-3 px-4 font-medium">Priority</th>
              <th class="text-center py-3 px-4 font-medium">Status</th>
              <th class="text-right py-3 px-4 font-medium">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in clientCategories" :key="c.id" class="border-t transition-colors" :style="{ borderColor: 'var(--addrez-border)' }">
              <td class="py-3 px-4 font-medium" :style="{ color: 'var(--addrez-text-primary)' }">{{ c.name }}</td>
              <td class="py-3 px-4 text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">{{ c.description || '—' }}</td>
              <td class="py-3 px-4 text-center">
                <div class="inline-flex items-center gap-2">
                  <div class="w-20 h-5 rounded" :style="{ backgroundColor: c.color }"></div>
                </div>
              </td>
              <td class="py-3 px-4 text-center" :style="{ color: 'var(--addrez-text-secondary)' }">{{ c.priority }}</td>
              <td class="py-3 px-4 text-center">
                <span class="text-xs px-2 py-1 rounded-full font-medium"
                  :style="c.is_active ? { backgroundColor: '#10b98120', color: '#10b981' } : { backgroundColor: '#6b728020', color: '#6b7280' }">
                  {{ c.is_active ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="py-3 px-4 text-right">
                <button @click="openEditClientCat(c)" class="p-1.5 rounded bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }">
                  <i class="pi pi-pencil text-xs"></i>
                </button>
                <button @click="requestDeleteClientCat(c)" class="p-1.5 rounded bg-transparent border-0 cursor-pointer" style="color: #ef4444">
                  <i class="pi pi-trash text-xs"></i>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        <div class="mt-3 px-4 py-2 rounded-lg text-xs font-medium" :style="{ backgroundColor: 'var(--addrez-bg-hover)', color: 'var(--addrez-text-secondary)' }">
          {{ clientCategories.length }} total
        </div>
      </div>
    </template>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- ═══ DIALOGS ═══ -->
    <!-- ═══════════════════════════════════════════════════════════ -->

    <!-- Tag Category Dialog -->
    <div v-if="showCategoryDialog" class="fixed inset-0 z-50 flex items-center justify-center" style="background: rgba(0,0,0,0.5)">
      <div class="card w-full max-w-sm mx-4" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ editingCategoryId ? 'Edit Tag Category' : 'New Tag Category' }}</h3>
          <button @click="showCategoryDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="saveCategory" class="space-y-3">
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Name *</label>
            <input v-model="categoryForm.name" required class="w-full px-3 py-2 rounded-lg border text-sm"
              :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
              placeholder="e.g. Occasion-Based Tags" />
          </div>
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Icon (emoji)</label>
            <input v-model="categoryForm.icon" class="w-full px-3 py-2 rounded-lg border text-sm"
              :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
              placeholder="e.g. 🎉 🍽️ ♿" />
          </div>
          <div class="flex justify-end gap-2 pt-2">
            <button type="button" @click="showCategoryDialog = false" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
            <button type="submit" :disabled="saving" class="btn-gold text-sm">{{ saving ? 'Saving...' : editingCategoryId ? 'Update' : 'Create' }}</button>
          </div>
        </form>
      </div>
    </div>

    <!-- Tag Dialog -->
    <div v-if="showTagDialog" class="fixed inset-0 z-50 flex items-center justify-center" style="background: rgba(0,0,0,0.5)">
      <div class="card w-full max-w-sm mx-4" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ editingTagId ? 'Edit Tag' : 'New Tag' }}</h3>
          <button @click="showTagDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="saveTag" class="space-y-3">
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Name *</label>
            <input v-model="tagForm.name" required class="w-full px-3 py-2 rounded-lg border text-sm"
              :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
              placeholder="e.g. Birthday, VIP, Vegetarian" />
          </div>
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Color</label>
            <div class="flex flex-wrap gap-2 mb-2">
              <button v-for="c in presetColors" :key="c" type="button" @click="tagForm.color = c"
                class="w-7 h-7 rounded-full border-2 cursor-pointer transition-transform"
                :style="{ backgroundColor: c, borderColor: tagForm.color === c ? '#fff' : 'transparent', transform: tagForm.color === c ? 'scale(1.2)' : 'scale(1)' }">
              </button>
            </div>
            <div class="flex items-center gap-2">
              <input v-model="tagForm.color" type="color" class="w-8 h-8 rounded border-0 cursor-pointer" />
              <input v-model="tagForm.color" class="flex-1 px-3 py-1.5 rounded-lg border text-xs font-mono"
                :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
          </div>
          <div v-if="!editingTagId">
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Category</label>
            <select v-model="tagForm.tag_category_id" class="w-full px-3 py-2 rounded-lg border text-sm"
              :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
              <option :value="null">No category</option>
              <option v-for="tc in filteredCategories" :key="tc.id" :value="tc.id">{{ tc.name }}</option>
            </select>
          </div>
          <div class="flex justify-end gap-2 pt-2">
            <button type="button" @click="showTagDialog = false" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
            <button type="submit" :disabled="saving" class="btn-gold text-sm">{{ saving ? 'Saving...' : editingTagId ? 'Update' : 'Create' }}</button>
          </div>
        </form>
      </div>
    </div>

    <!-- Client Category Dialog -->
    <div v-if="showClientCatDialog" class="fixed inset-0 z-50 flex items-center justify-center" style="background: rgba(0,0,0,0.5)">
      <div class="card w-full max-w-sm mx-4" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">{{ editingClientCatId ? 'Edit Category' : 'New Category' }}</h3>
          <button @click="showClientCatDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="saveClientCat" class="space-y-3">
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Name *</label>
            <input v-model="clientCatForm.name" required class="w-full px-3 py-2 rounded-lg border text-sm"
              :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }"
              placeholder="e.g. VIP, Corporate" />
          </div>
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Description</label>
            <input v-model="clientCatForm.description" class="w-full px-3 py-2 rounded-lg border text-sm"
              :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Priority</label>
              <input v-model.number="clientCatForm.priority" type="number" min="1" class="w-full px-3 py-2 rounded-lg border text-sm"
                :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div v-if="editingClientCatId">
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Status</label>
              <label class="flex items-center gap-2 mt-2 text-sm cursor-pointer" :style="{ color: 'var(--addrez-text-primary)' }">
                <input type="checkbox" v-model="clientCatForm.is_active" /> Active
              </label>
            </div>
          </div>
          <div>
            <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Color</label>
            <div class="flex flex-wrap gap-2 mb-2">
              <button v-for="c in presetColors" :key="c" type="button" @click="clientCatForm.color = c"
                class="w-7 h-7 rounded-full border-2 cursor-pointer transition-transform"
                :style="{ backgroundColor: c, borderColor: clientCatForm.color === c ? '#fff' : 'transparent', transform: clientCatForm.color === c ? 'scale(1.2)' : 'scale(1)' }">
              </button>
            </div>
            <div class="flex items-center gap-2">
              <input v-model="clientCatForm.color" type="color" class="w-8 h-8 rounded border-0 cursor-pointer" />
              <input v-model="clientCatForm.color" class="flex-1 px-3 py-1.5 rounded-lg border text-xs font-mono"
                :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
          </div>
          <div class="flex justify-end gap-2 pt-2">
            <button type="button" @click="showClientCatDialog = false" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
            <button type="submit" :disabled="saving" class="btn-gold text-sm">{{ saving ? 'Saving...' : editingClientCatId ? 'Update' : 'Create' }}</button>
          </div>
        </form>
      </div>
    </div>

    <ConfirmDialog :visible="confirmVisible" :title="confirmTitle" :message="confirmMessage" @confirm="doConfirm" @cancel="confirmVisible = false" />
  </div>
</template>
