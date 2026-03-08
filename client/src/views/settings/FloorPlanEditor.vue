<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, onBeforeUnmount, nextTick } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useThemeStore } from '@/stores/theme'
import api from '@/services/api'

const toast = useToast()
const theme = useThemeStore()
const canvasBg = computed(() => theme.mode === 'dark' ? '#1a1f2e' : '#e9edf2')

interface EditorObject {
  id?: number
  type: 'table' | 'landmark'
  name: string
  label?: string
  x: number
  y: number
  width: number
  height: number
  rotation: number
  shape?: string
  minCovers?: number
  maxCovers?: number
  tableTypeId?: number
  isCombinable?: boolean
  landmarkType?: string
  icon?: string
  selected?: boolean
}

interface OutletOption { id: number; name: string; areas: { id: number; name: string; sort_order: number }[] }

const outlets = ref<OutletOption[]>([])
const layouts = ref<any[]>([])
const tableTypes = ref<any[]>([])
const selectedOutletId = ref<number | null>(null)
const selectedAreaId = ref<number | null>(null)
const loading = ref(true)
const saving = ref(false)
const objects = ref<EditorObject[]>([])
const selectedIds = ref<Set<number | string>>(new Set())
const dragState = ref<{ objKey: string; offsetX: number; offsetY: number } | null>(null)
const resizeState = ref<{ objKey: string; startW: number; startH: number; startX: number; startY: number } | null>(null)
const selectionBox = ref<{ x1: number; y1: number; x2: number; y2: number } | null>(null)
const canvasRef = ref<HTMLElement | null>(null)
const editorContainerRef = ref<HTMLElement | null>(null)
const editorContainerWidth = ref(800)
let editorResizeObserver: ResizeObserver | null = null

const canvasScale = computed(() => {
  const cw = activeFloor.value?.width || 1200
  const ch = activeFloor.value?.height || 800
  const availW = editorContainerWidth.value
  const maxH = Math.max(400, window.innerHeight - 260)
  return Math.min(availW / cw, maxH / ch, 1)
})

function initEditorResize() {
  if (editorContainerRef.value) {
    editorContainerWidth.value = editorContainerRef.value.clientWidth
    editorResizeObserver = new ResizeObserver(entries => {
      for (const entry of entries) editorContainerWidth.value = entry.contentRect.width
    })
    editorResizeObserver.observe(editorContainerRef.value)
  }
}

function screenToCanvas(clientX: number, clientY: number) {
  const rect = canvasRef.value!.getBoundingClientRect()
  const s = canvasScale.value
  return { x: (clientX - rect.left) / s, y: (clientY - rect.top) / s }
}

const showObjDialog = ref(false)
const editingObj = ref<EditorObject | null>(null)
let justBoxSelected = false

const selectedOutlet = computed(() => outlets.value.find(o => o.id === selectedOutletId.value))
const outletAreas = computed(() => selectedOutlet.value?.areas ?? [])
// Find the floor plan object from layouts data that matches the selected area
const activeFloor = computed(() => {
  if (!selectedAreaId.value) return null
  for (const l of layouts.value) {
    const fp = l.floor_plans?.find((f: any) => f.id === selectedAreaId.value)
    if (fp) return fp
  }
  return null
})

const objectPalette = [
  { type: 'table', shape: 'Round', label: 'Round Table', icon: 'pi-circle', w: 80, h: 80 },
  { type: 'table', shape: 'Rectangular', label: 'Rect Table', icon: 'pi-stop', w: 120, h: 80 },
  { type: 'table', shape: 'Booth', label: 'Booth', icon: 'pi-bookmark', w: 140, h: 80 },
  { type: 'table', shape: 'Bar', label: 'Bar Seat', icon: 'pi-minus', w: 60, h: 60 },
  { type: 'landmark', landmarkType: 'Entrance', label: 'Entrance', icon: 'pi-sign-in', w: 80, h: 40 },
  { type: 'landmark', landmarkType: 'Kitchen', label: 'Kitchen', icon: 'pi-box', w: 100, h: 80 },
  { type: 'landmark', landmarkType: 'Bar', label: 'Bar Area', icon: 'pi-minus', w: 200, h: 40 },
  { type: 'landmark', landmarkType: 'Wall', label: 'Wall', icon: 'pi-minus', w: 200, h: 10 },
  { type: 'landmark', landmarkType: 'Door', label: 'Door', icon: 'pi-external-link', w: 60, h: 20 },
  { type: 'landmark', landmarkType: 'Path', label: 'Path', icon: 'pi-arrows-h', w: 150, h: 30 },
]

let nextTempId = -1
const snapToGrid = ref(true)
const gridSize = 20
const undoStack = ref<EditorObject[][]>([])
const maxUndo = 30

function snapValue(v: number) {
  return snapToGrid.value ? Math.round(v / gridSize) * gridSize : v
}

function pushUndo() {
  undoStack.value.push(JSON.parse(JSON.stringify(objects.value)))
  if (undoStack.value.length > maxUndo) undoStack.value.shift()
}

function undo() {
  if (undoStack.value.length === 0) return
  objects.value = undoStack.value.pop()!
  selectedIds.value.clear()
}

function duplicateSelected() {
  if (selectedIds.value.size === 0) return
  pushUndo()
  const newIds = new Set<string>()
  const toAdd: EditorObject[] = []
  objects.value.forEach((o, i) => {
    if (selectedIds.value.has(objKey(o, i))) {
      const clone: EditorObject = { ...o, id: undefined, x: o.x + 20, y: o.y + 20 }
      toAdd.push(clone)
    }
  })
  toAdd.forEach(c => {
    objects.value.push(c)
    newIds.add(objKey(c, objects.value.length - 1))
  })
  selectedIds.value = newIds
}

function rotateSelected(deg: number) {
  if (selectedIds.value.size === 0) return
  pushUndo()
  objects.value.forEach((o, i) => {
    if (selectedIds.value.has(objKey(o, i))) {
      o.rotation = ((o.rotation || 0) + deg) % 360
    }
  })
}

function handleKeydown(e: KeyboardEvent) {
  // Ignore if user is typing in an input/textarea/select
  const tag = (e.target as HTMLElement)?.tagName
  if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return

  if (e.key === 'Delete' || e.key === 'Backspace') {
    e.preventDefault()
    if (selectedIds.value.size > 0) { pushUndo(); deleteSelected() }
  }
  if (e.key === 'r' || e.key === 'R') {
    if (!e.ctrlKey && !e.metaKey) {
      e.preventDefault()
      rotateSelected(e.shiftKey ? -15 : 15)
    }
  }
  if ((e.ctrlKey || e.metaKey) && e.key === 'd') {
    e.preventDefault()
    duplicateSelected()
  }
  if ((e.ctrlKey || e.metaKey) && e.key === 'z') {
    e.preventDefault()
    undo()
  }
  if ((e.ctrlKey || e.metaKey) && e.key === 'a') {
    e.preventDefault()
    selectedIds.value.clear()
    objects.value.forEach((o, i) => selectedIds.value.add(objKey(o, i)))
  }
}

async function load() {
  loading.value = true
  try {
    const [tr, or] = await Promise.all([
      api.get<any[]>('/settings/table-types'),
      api.get<OutletOption[]>('/settings/outlets')
    ])
    tableTypes.value = tr.data
    outlets.value = or.data.filter((o: any) => o.is_active !== false)
    if (!selectedOutletId.value && outlets.value.length > 0) {
      selectedOutletId.value = outlets.value[0].id
    }
    await loadLayoutsForOutlet()
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load layouts', life: 3000 })
  } finally { loading.value = false }
}

async function loadLayoutsForOutlet() {
  if (!selectedOutletId.value) { layouts.value = []; syncObjects(); return }
  try {
    const lr = await api.get<any[]>('/floor-plans/layouts', { params: { outletId: selectedOutletId.value } })
    layouts.value = lr.data
  } catch { layouts.value = [] }
  autoSelectFirstArea()
  syncObjects()
}

function autoSelectFirstArea() {
  const areas = selectedOutlet.value?.areas ?? []
  if (areas.length > 0 && !areas.find(a => a.id === selectedAreaId.value)) {
    selectedAreaId.value = areas[0].id
  } else if (areas.length === 0) {
    selectedAreaId.value = null
  }
}

async function onOutletChange(id: number) {
  selectedOutletId.value = id
  await loadLayoutsForOutlet()
}

function onAreaChange(id: number) {
  selectedAreaId.value = id
  syncObjects()
}

function syncObjects() {
  const fp = activeFloor.value
  if (!fp) { objects.value = []; selectedIds.value.clear(); return }
  const objs: EditorObject[] = []
  for (const t of fp.tables || []) {
    objs.push({
      id: t.id, type: 'table', name: t.name, label: t.label,
      x: t.x, y: t.y, width: t.width, height: t.height, rotation: t.rotation || 0,
      shape: t.shape, minCovers: t.min_covers, maxCovers: t.max_covers,
      tableTypeId: t.table_type_id, isCombinable: t.is_combinable
    })
  }
  for (const lm of fp.landmarks || []) {
    objs.push({
      id: lm.id, type: 'landmark', name: lm.name, landmarkType: lm.type, icon: lm.icon,
      x: lm.x, y: lm.y, width: lm.width, height: lm.height, rotation: lm.rotation || 0
    })
  }
  objects.value = objs
  selectedIds.value.clear()
}

function objKey(o: EditorObject, i: number): string {
  return o.id ? `${o.type}-${o.id}` : `${o.type}-new-${i}`
}

function isSelected(o: EditorObject, i: number) {
  return selectedIds.value.has(objKey(o, i))
}

function selectObj(o: EditorObject, i: number, e: MouseEvent) {
  e.stopPropagation()
  const key = objKey(o, i)
  if (e.ctrlKey || e.metaKey) {
    if (selectedIds.value.has(key)) selectedIds.value.delete(key)
    else selectedIds.value.add(key)
  } else {
    selectedIds.value.clear()
    selectedIds.value.add(key)
  }
}

function clearSelection() {
  if (justBoxSelected) { justBoxSelected = false; return }
  selectedIds.value.clear()
}

function addFromPalette(p: any) {
  const fp = activeFloor.value
  if (!fp) return
  pushUndo()
  const obj: EditorObject = {
    id: undefined, type: p.type,
    name: p.type === 'table' ? `T${objects.value.filter(o => o.type === 'table').length + 1}` : p.landmarkType,
    label: p.type === 'table' ? `${objects.value.filter(o => o.type === 'table').length + 1}` : undefined,
    x: snapValue(50 + Math.random() * 200), y: snapValue(50 + Math.random() * 200),
    width: p.w, height: p.h, rotation: 0,
    shape: p.shape, minCovers: p.type === 'table' ? 2 : undefined,
    maxCovers: p.type === 'table' ? 4 : undefined, tableTypeId: tableTypes.value[0]?.id,
    isCombinable: false, landmarkType: p.landmarkType, icon: p.icon
  }
  objects.value.push(obj)
  selectedIds.value.clear()
  selectedIds.value.add(objKey(obj, objects.value.length - 1))
}

function startDrag(o: EditorObject, i: number, e: MouseEvent) {
  e.preventDefault()
  pushUndo()
  const key = objKey(o, i)
  if (!selectedIds.value.has(key)) {
    selectedIds.value.clear()
    selectedIds.value.add(key)
  }
  const start = screenToCanvas(e.clientX, e.clientY)
  dragState.value = { objKey: key, offsetX: start.x - o.x, offsetY: start.y - o.y }

  const onMove = (ev: MouseEvent) => {
    if (!dragState.value) return
    const pos = screenToCanvas(ev.clientX, ev.clientY)
    let nx = Math.max(0, pos.x - dragState.value.offsetX)
    let ny = Math.max(0, pos.y - dragState.value.offsetY)
    nx = snapValue(nx)
    ny = snapValue(ny)
    const dx = nx - o.x
    const dy = ny - o.y
    if (dx === 0 && dy === 0) return
    for (let j = 0; j < objects.value.length; j++) {
      if (selectedIds.value.has(objKey(objects.value[j], j))) {
        objects.value[j].x = Math.max(0, snapValue(objects.value[j].x + dx))
        objects.value[j].y = Math.max(0, snapValue(objects.value[j].y + dy))
      }
    }
  }
  const onUp = () => { dragState.value = null; document.removeEventListener('mousemove', onMove); document.removeEventListener('mouseup', onUp) }
  document.addEventListener('mousemove', onMove)
  document.addEventListener('mouseup', onUp)
}

function startResize(o: EditorObject, i: number, e: MouseEvent) {
  e.preventDefault()
  e.stopPropagation()
  const s = canvasScale.value
  resizeState.value = { objKey: objKey(o, i), startW: o.width, startH: o.height, startX: e.clientX, startY: e.clientY }
  const onMove = (ev: MouseEvent) => {
    if (!resizeState.value) return
    o.width = Math.max(30, resizeState.value.startW + (ev.clientX - resizeState.value.startX) / s)
    o.height = Math.max(20, resizeState.value.startH + (ev.clientY - resizeState.value.startY) / s)
  }
  const onUp = () => { resizeState.value = null; document.removeEventListener('mousemove', onMove); document.removeEventListener('mouseup', onUp) }
  document.addEventListener('mousemove', onMove)
  document.addEventListener('mouseup', onUp)
}

function startSelectionBox(e: MouseEvent) {
  // Only start box select from the canvas background, not from objects
  const target = e.target as HTMLElement
  if (target !== canvasRef.value && target.parentElement !== canvasRef.value) return
  e.preventDefault()
  const start = screenToCanvas(e.clientX, e.clientY)
  selectionBox.value = { x1: start.x, y1: start.y, x2: start.x, y2: start.y }
  selectedIds.value.clear()
  let moved = false

  const onMove = (ev: MouseEvent) => {
    if (!selectionBox.value) return
    moved = true
    const pos = screenToCanvas(ev.clientX, ev.clientY)
    selectionBox.value.x2 = pos.x
    selectionBox.value.y2 = pos.y
    // Select objects that overlap with the box (partial overlap)
    const bx1 = Math.min(selectionBox.value.x1, selectionBox.value.x2)
    const by1 = Math.min(selectionBox.value.y1, selectionBox.value.y2)
    const bx2 = Math.max(selectionBox.value.x1, selectionBox.value.x2)
    const by2 = Math.max(selectionBox.value.y1, selectionBox.value.y2)
    selectedIds.value.clear()
    objects.value.forEach((o, i) => {
      // Overlap check: object overlaps box if no axis is fully separated
      if (o.x + o.width > bx1 && o.x < bx2 && o.y + o.height > by1 && o.y < by2) {
        selectedIds.value.add(objKey(o, i))
      }
    })
  }
  const onUp = () => {
    if (moved && selectedIds.value.size > 0) justBoxSelected = true
    selectionBox.value = null
    document.removeEventListener('mousemove', onMove)
    document.removeEventListener('mouseup', onUp)
  }
  document.addEventListener('mousemove', onMove)
  document.addEventListener('mouseup', onUp)
}

function deleteSelected() {
  pushUndo()
  objects.value = objects.value.filter((o, i) => !selectedIds.value.has(objKey(o, i)))
  selectedIds.value.clear()
}

function editSelected() {
  const idx = objects.value.findIndex((o, i) => selectedIds.value.has(objKey(o, i)))
  if (idx < 0) return
  editingObj.value = { ...objects.value[idx] }
  showObjDialog.value = true
}

function saveObjEdit() {
  if (!editingObj.value) return
  const idx = objects.value.findIndex((o, i) => selectedIds.value.has(objKey(o, i)))
  if (idx >= 0) {
    objects.value[idx] = { ...editingObj.value }
  }
  showObjDialog.value = false
}

async function saveLayout() {
  const fp = activeFloor.value
  if (!fp) return
  saving.value = true
  try {
    const tables = objects.value.filter(o => o.type === 'table').map(o => ({
      id: o.id || null, name: o.name, label: o.label || o.name,
      min_covers: o.minCovers || 2, max_covers: o.maxCovers || 4,
      shape: o.shape || 'Round', x: Math.round(o.x), y: Math.round(o.y),
      width: Math.round(o.width), height: Math.round(o.height),
      rotation: o.rotation, is_combinable: o.isCombinable || false,
      table_type_id: o.tableTypeId || tableTypes.value[0]?.id || 1
    }))
    const landmarks = objects.value.filter(o => o.type === 'landmark').map(o => ({
      id: o.id || null, name: o.name, type: o.landmarkType || 'Other',
      icon: o.icon || null, x: Math.round(o.x), y: Math.round(o.y),
      width: Math.round(o.width), height: Math.round(o.height), rotation: o.rotation
    }))
    await api.put(`/floor-plans/zones/${fp.id}/save`, { tables, landmarks })
    toast.add({ severity: 'success', summary: 'Saved', detail: 'Floor plan saved', life: 2000 })
    await load()
  } catch (err: any) {
    toast.add({ severity: 'error', summary: 'Error', detail: err.response?.data?.message || 'Save failed', life: 3000 })
  } finally { saving.value = false }
}

function getObjStyle(o: EditorObject, i: number) {
  const sel = isSelected(o, i)
  const isTable = o.type === 'table'
  const borderColor = sel ? 'var(--addrez-gold)' : isTable ? '#3b82f6' : '#64748b'
  return {
    position: 'absolute' as const,
    left: `${o.x}px`, top: `${o.y}px`,
    width: `${o.width}px`, height: `${o.height}px`,
    borderRadius: o.shape === 'Round' ? '50%' : o.shape === 'Booth' ? '12px 12px 4px 4px' : '6px',
    border: `2.5px solid ${borderColor}`,
    backgroundColor: isTable ? '#3b82f622' : '#64748b25',
    boxShadow: sel
      ? `0 0 0 3px rgba(212,168,83,0.3), inset 0 0 0 2px ${borderColor}30`
      : `inset 0 0 0 2px ${isTable ? '#3b82f618' : '#64748b18'}`,
    cursor: 'move', transform: o.rotation ? `rotate(${o.rotation}deg)` : undefined,
    zIndex: sel ? 10 : 1
  }
}

function selBoxStyle() {
  if (!selectionBox.value) return {}
  const x = Math.min(selectionBox.value.x1, selectionBox.value.x2)
  const y = Math.min(selectionBox.value.y1, selectionBox.value.y2)
  const w = Math.abs(selectionBox.value.x2 - selectionBox.value.x1)
  const h = Math.abs(selectionBox.value.y2 - selectionBox.value.y1)
  return { position: 'absolute' as const, left: `${x}px`, top: `${y}px`, width: `${w}px`, height: `${h}px`, border: '1px dashed var(--addrez-gold)', backgroundColor: 'rgba(212,168,83,0.08)', pointerEvents: 'none' as const, zIndex: 100 }
}

function startRotate(o: EditorObject, i: number, e: MouseEvent) {
  e.preventDefault()
  e.stopPropagation()
  pushUndo()
  const rect = canvasRef.value!.getBoundingClientRect()
  const cx = o.x + o.width / 2
  const cy = o.y + o.height / 2
  const startAngle = Math.atan2(e.clientY - rect.top - cy, e.clientX - rect.left - cx)
  const startRotation = o.rotation || 0

  const onMove = (ev: MouseEvent) => {
    const r = canvasRef.value!.getBoundingClientRect()
    const angle = Math.atan2(ev.clientY - r.top - cy, ev.clientX - r.left - cx)
    let deg = startRotation + ((angle - startAngle) * 180) / Math.PI
    if (ev.shiftKey) deg = Math.round(deg / 15) * 15
    o.rotation = ((deg % 360) + 360) % 360
  }
  const onUp = () => { document.removeEventListener('mousemove', onMove); document.removeEventListener('mouseup', onUp) }
  document.addEventListener('mousemove', onMove)
  document.addEventListener('mouseup', onUp)
}

onMounted(() => {
  load()
  document.addEventListener('keydown', handleKeydown)
  nextTick(initEditorResize)
})
onBeforeUnmount(() => { editorResizeObserver?.disconnect() })

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeydown)
})
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-4">
      <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">Floor Plan Editor</h3>
      <div class="flex gap-2">
        <button v-if="selectedIds.size > 0" @click="editSelected" class="px-3 py-1.5 rounded-lg text-xs border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-gold)', color: 'var(--addrez-gold)' }"><i class="pi pi-pencil mr-1"></i>Edit</button>
        <button v-if="selectedIds.size > 0" @click="rotateSelected(15)" class="px-3 py-1.5 rounded-lg text-xs border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-gold)', color: 'var(--addrez-gold)' }" title="Rotate 15°"><i class="pi pi-replay mr-1"></i>Rotate</button>
        <button v-if="selectedIds.size > 0" @click="duplicateSelected" class="px-3 py-1.5 rounded-lg text-xs border bg-transparent cursor-pointer" :style="{ borderColor: '#3b82f6', color: '#3b82f6' }"><i class="pi pi-copy mr-1"></i>Duplicate</button>
        <button v-if="selectedIds.size > 0" @click="deleteSelected" class="px-3 py-1.5 rounded-lg text-xs border bg-transparent cursor-pointer" style="border-color: #ef4444; color: #ef4444"><i class="pi pi-trash mr-1"></i>Delete</button>
        <button v-if="undoStack.length > 0" @click="undo" class="px-3 py-1.5 rounded-lg text-xs border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-secondary)' }" title="Undo (Ctrl+Z)"><i class="pi pi-undo mr-1"></i>Undo</button>
        <button @click="saveLayout" :disabled="saving" class="btn-gold text-sm">
          <i :class="saving ? 'pi pi-spin pi-spinner' : 'pi pi-save'" class="mr-1"></i>{{ saving ? 'Saving...' : 'Save Layout' }}
        </button>
      </div>
    </div>

    <div v-if="loading" class="text-center py-12"><i class="pi pi-spin pi-spinner text-2xl" :style="{ color: 'var(--addrez-text-secondary)' }"></i></div>

    <template v-else>
      <!-- Outlet + Area selection -->
      <div class="flex items-center gap-3 mb-4">
        <div>
          <label class="block text-[10px] font-medium mb-0.5 uppercase tracking-wider" :style="{ color: 'var(--addrez-text-secondary)' }">Outlet</label>
          <select
            :value="selectedOutletId ?? ''"
            @change="onOutletChange(Number(($event.target as HTMLSelectElement).value))"
            class="px-3 py-2 rounded-lg border text-sm min-w-[180px]"
            :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
            <option v-for="o in outlets" :key="o.id" :value="o.id">{{ o.name }}</option>
          </select>
        </div>
        <div v-if="outletAreas.length > 0">
          <label class="block text-[10px] font-medium mb-0.5 uppercase tracking-wider" :style="{ color: 'var(--addrez-text-secondary)' }">Area</label>
          <select
            :value="selectedAreaId ?? ''"
            @change="onAreaChange(Number(($event.target as HTMLSelectElement).value))"
            class="px-3 py-2 rounded-lg border text-sm min-w-[160px]"
            :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-gold)', color: 'var(--addrez-gold)' }">
            <option v-for="a in outletAreas" :key="a.id" :value="a.id" :style="{ color: 'var(--addrez-text-primary)', backgroundColor: 'var(--addrez-bg-card)' }">{{ a.name }}</option>
          </select>
        </div>
        <div v-if="outletAreas.length === 0 && selectedOutletId" class="self-end pb-1">
          <span class="text-xs" :style="{ color: 'var(--addrez-text-secondary)' }">No areas defined. Add areas in Outlets settings.</span>
        </div>
      </div>

      <div class="flex gap-4">
        <!-- Object Palette -->
        <div class="w-48 shrink-0">
          <div class="card">
            <h4 class="text-xs font-bold uppercase mb-3 tracking-wider" :style="{ color: 'var(--addrez-gold)' }">Objects</h4>
            <div class="space-y-1">
              <button v-for="p in objectPalette" :key="p.label" @click="addFromPalette(p)"
                class="w-full flex items-center gap-2 px-2 py-2 rounded-lg text-xs bg-transparent border cursor-pointer transition-colors hover:opacity-80"
                :style="{ borderColor: 'var(--addrez-border)', color: p.type === 'table' ? '#3b82f6' : 'var(--addrez-text-secondary)' }">
                <i :class="'pi ' + p.icon" class="w-4 text-center"></i>
                {{ p.label }}
              </button>
            </div>
            <div class="mt-4 pt-3 border-t" :style="{ borderColor: 'var(--addrez-border)' }">
              <label class="flex items-center gap-2 text-xs cursor-pointer mb-2" :style="{ color: 'var(--addrez-text-secondary)' }">
                <input type="checkbox" v-model="snapToGrid" class="accent-[var(--addrez-gold)]" /> Snap to Grid
              </label>
            </div>
            <div class="mt-2 pt-2 border-t text-[10px]" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-secondary)' }">
              <div><b>Click</b> select &middot; <b>Ctrl+Click</b> multi</div>
              <div><b>Drag</b> canvas = box-select</div>
              <div><b>Drag</b> object = move</div>
              <div><b>Del</b> delete &middot; <b>R</b> rotate 15&deg;</div>
              <div><b>Ctrl+D</b> duplicate</div>
              <div><b>Ctrl+Z</b> undo &middot; <b>Ctrl+A</b> all</div>
              <div>Corner = resize &middot; <span style="color:var(--addrez-gold)">&#9702;</span> = rotate</div>
            </div>
          </div>
        </div>

        <!-- Canvas -->
        <div ref="editorContainerRef" class="flex-1 overflow-hidden rounded-xl border" :style="{ borderColor: 'var(--addrez-border)' }">
          <div :style="{ width: ((activeFloor?.width || 1200) * canvasScale) + 'px', height: ((activeFloor?.height || 800) * canvasScale) + 'px', overflow: 'hidden' }">
          <div
            ref="canvasRef"
            class="relative"
            :style="{
              width: (activeFloor?.width || 1200) + 'px',
              height: (activeFloor?.height || 800) + 'px',
              backgroundColor: canvasBg,
              backgroundImage: 'radial-gradient(circle, var(--addrez-border) 1px, transparent 1px)',
              backgroundSize: '20px 20px',
              transform: `scale(${canvasScale})`,
              transformOrigin: 'top left'
            }"
            @mousedown="startSelectionBox"
            @click="clearSelection"
          >
            <!-- Objects -->
            <div
              v-for="(o, i) in objects" :key="objKey(o, i)"
              :style="getObjStyle(o, i)"
              class="flex items-center justify-center select-none"
              @mousedown.stop="(e) => { selectObj(o, i, e); startDrag(o, i, e) }"
              @click.stop
              @dblclick.stop="() => { selectedIds.clear(); selectedIds.add(objKey(o, i)); editSelected() }"
            >
              <div class="text-center pointer-events-none">
                <div class="text-xs font-bold" :style="{ color: o.type === 'table' ? '#3b82f6' : '#64748b' }">
                  {{ o.label || o.name }}
                </div>
                <div v-if="o.type === 'table'" class="text-[10px]" :style="{ color: 'var(--addrez-text-secondary)' }">
                  {{ o.minCovers }}-{{ o.maxCovers }}
                </div>
              </div>
              <!-- Resize handle -->
              <div
                v-if="isSelected(o, i)"
                class="absolute -bottom-1 -right-1 w-3 h-3 rounded-sm cursor-se-resize"
                style="background: var(--addrez-gold)"
                @mousedown.stop="(e) => startResize(o, i, e)"
              ></div>
              <!-- Rotation handle -->
              <div
                v-if="isSelected(o, i)"
                class="absolute -top-5 left-1/2 -translate-x-1/2 w-3.5 h-3.5 rounded-full cursor-grab flex items-center justify-center"
                style="background: var(--addrez-gold); box-shadow: 0 1px 4px rgba(0,0,0,0.3)"
                title="Drag to rotate (hold Shift for 15° snap)"
                @mousedown.stop="(e) => startRotate(o, i, e)"
              >
                <svg width="8" height="8" viewBox="0 0 16 16" fill="#1a1a24"><path d="M2 8a6 6 0 0 1 10.5-4H10V2h5v5h-2V4.5A7 7 0 1 0 15 8h-2A5 5 0 1 1 2 8z"/></svg>
              </div>
              <!-- Rotation connector line -->
              <div
                v-if="isSelected(o, i)"
                class="absolute left-1/2 -translate-x-1/2" style="top: -12px; width: 1px; height: 12px; background: var(--addrez-gold); opacity: 0.5"
              ></div>
            </div>

            <!-- Selection box -->
            <div v-if="selectionBox" :style="selBoxStyle()"></div>
          </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Edit Object Dialog -->
    <div v-if="showObjDialog && editingObj" class="modal-overlay">
      <div class="card w-full max-w-md mx-4" :style="{ backgroundColor: 'var(--addrez-bg-card)' }">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold" :style="{ color: 'var(--addrez-text-primary)' }">Edit {{ editingObj.type === 'table' ? 'Table' : 'Object' }}</h3>
          <button @click="showObjDialog = false" class="bg-transparent border-0 cursor-pointer" :style="{ color: 'var(--addrez-text-secondary)' }"><i class="pi pi-times"></i></button>
        </div>
        <form @submit.prevent="saveObjEdit" class="space-y-3">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Name</label>
              <input v-model="editingObj.name" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div v-if="editingObj.type === 'table'">
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Label</label>
              <input v-model="editingObj.label" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div v-if="editingObj.type === 'table'">
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Min Covers</label>
              <input v-model.number="editingObj.minCovers" type="number" min="1" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div v-if="editingObj.type === 'table'">
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Max Covers</label>
              <input v-model.number="editingObj.maxCovers" type="number" min="1" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Width</label>
              <input v-model.number="editingObj.width" type="number" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Height</label>
              <input v-model.number="editingObj.height" type="number" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div>
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Rotation</label>
              <input v-model.number="editingObj.rotation" type="number" min="0" max="360" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }" />
            </div>
            <div v-if="editingObj.type === 'table'">
              <label class="block text-xs font-medium mb-1" :style="{ color: 'var(--addrez-text-secondary)' }">Shape</label>
              <select v-model="editingObj.shape" class="w-full px-3 py-2 rounded-lg border text-sm" :style="{ backgroundColor: 'var(--addrez-bg-primary)', borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">
                <option>Round</option><option>Rectangular</option><option>Booth</option><option>Bar</option>
              </select>
            </div>
          </div>
          <div v-if="editingObj.type === 'table'" class="flex items-center gap-2">
            <input type="checkbox" id="combinable" v-model="editingObj.isCombinable" />
            <label for="combinable" class="text-sm" :style="{ color: 'var(--addrez-text-primary)' }">Combinable</label>
          </div>
          <div class="flex justify-end gap-2 pt-2">
            <button type="button" @click="showObjDialog = false" class="px-4 py-2 rounded-lg text-sm border bg-transparent cursor-pointer" :style="{ borderColor: 'var(--addrez-border)', color: 'var(--addrez-text-primary)' }">Cancel</button>
            <button type="submit" class="btn-gold text-sm">Apply</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
