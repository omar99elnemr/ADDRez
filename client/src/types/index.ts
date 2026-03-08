// ── Auth ──
export interface User {
  id: number
  username: string
  email: string
  first_name: string
  last_name: string
  full_name: string
  phone?: string
  avatar_url?: string
  is_active: boolean
  company_id: number
  company_name: string
  roles: Role[]
  permissions: string[]
  outlets: OutletSummary[]
}

export interface Role {
  id: number
  name: string
  slug: string
}

export interface OutletSummary {
  id: number
  name: string
  is_active: boolean
}

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  user: User
}

// ── Pagination ──
export interface PaginatedResponse<T> {
  data: T[]
  current_page: number
  last_page: number
  total: number
  per_page: number
}

export interface PaginationQuery {
  page?: number
  per_page?: number
  search?: string
  sort_by?: string
  sort_dir?: 'asc' | 'desc'
}

// ── Dashboard ──
export interface DashboardKpis {
  today_reservations: number
  today_reservations_change: number
  today_expected_visitors: number
  today_expected_visitors_change: number
  today_covers: number
  new_customers_this_week: number
  new_customers_change: number
  today_revenue: number
  today_revenue_change: number
  week_revenue: number
  week_revenue_change: number
  pending_confirmation: number
  checked_in: number
  seated: number
  no_shows: number
  cancelled: number
  total_reservations: number
  total_cancelled: number
  monthly_revenue: number
}

export interface WeekChartPoint {
  date: string
  day_label: string
  reservations: number
  walk_ins: number
  covers: number
}

export interface TopCustomer {
  id: number
  name: string
  total_visits: number
  total_spend: number
  category?: string
  category_color?: string
}

export interface ReservationSummary {
  id: number
  guest_name?: string
  covers: number
  date: string
  time: string
  status: string
  table_name?: string
}

export interface OutletStat {
  id: number
  name: string
  total_reservations: number
  today_reservations: number
}

export interface StatusDistribution {
  confirmed: number
  pending: number
  seated: number
  checked_in: number
  checked_out: number
  no_show: number
  cancelled: number
}

export interface TodayReservation {
  id: number
  confirmation_code?: string
  outlet_name: string
  guest_name?: string
  guest_phone?: string
  covers: number
  time: string
  status: string
  status_color: string
  table_name?: string
  time_slot_name?: string
  method: string
}

export interface FutureUnconfirmed {
  id: number
  outlet_name: string
  date: string
  time_slot_name?: string
  guest_name?: string
  guest_phone?: string
  covers: number
  type: string
  notes?: string
  created_by: string
}

export interface DashboardData {
  kpis: DashboardKpis
  week_chart: WeekChartPoint[]
  top_customers: TopCustomer[]
  unconfirmed_queue: ReservationSummary[]
  outlet_stats: OutletStat[]
  status_distribution: StatusDistribution
  today_reservations: TodayReservation[]
  future_unconfirmed: FutureUnconfirmed[]
}

// ── Reservations ──
export type ReservationStatus = 'Pending' | 'Confirmed' | 'CheckedIn' | 'Seated' | 'CheckedOut' | 'Cancelled' | 'NoShow' | 'Waitlisted'
export type ReservationType = 'DineIn' | 'Event' | 'Birthday' | 'Corporate' | 'Group' | 'PrivateDining' | 'Lounge' | 'Takeaway' | 'Inhouse'
export type SeatingType = 'Seated' | 'Standing'
export type ReservationMethod = 'Phone' | 'WalkIn' | 'Online' | 'Email' | 'ThirdParty' | 'SocialMedia'

export interface Tag {
  id: number
  name: string
  color: string
}

export interface Reservation {
  id: number
  guest_name?: string
  guest_email?: string
  guest_phone?: string
  customer_id?: number
  customer_name?: string
  covers: number
  date: string
  time: string
  duration_minutes: number
  status: ReservationStatus
  type: ReservationType
  seating_type: SeatingType
  method: ReservationMethod
  notes?: string
  table_id?: number
  table_name?: string
  time_slot_id?: number
  time_slot_name?: string
  confirmation_code?: string
  tags: Tag[]
  created_at: string
}

export interface CreateReservationRequest {
  customer_id?: number
  guest_name?: string
  guest_email?: string
  guest_phone?: string
  guest_date_of_birth?: string
  guest_gender?: string
  membership_no?: string
  room_no?: string
  covers: number
  date: string
  time: string
  duration_minutes: number
  type: ReservationType
  seating_type: SeatingType
  method: ReservationMethod
  notes?: string
  special_requests?: string
  deposit_amount?: number
  discount_percent?: number
  discount_reason?: string
  time_slot_id?: number
  table_id?: number
  tag_ids?: number[]
}

export interface CapacityData {
  totalSeated: number
  totalStanding: number
  tablesReserved: number
  totalTables: number
  attendedCustomers: number
  walkInCustomers: number
  totalCovers: number
  pendingCount: number
}

export interface GuestSearchResult {
  id: number
  firstName: string
  lastName: string
  fullName: string
  phone?: string
  email?: string
  status: string
}

// ── Customers ──
export type CustomerStatus = 'Active' | 'VIP' | 'Blacklisted' | 'Inactive'

export interface Customer {
  id: number
  first_name: string
  last_name: string
  full_name: string
  email?: string
  phone?: string
  status: CustomerStatus
  category_name?: string
  category_color?: string
  total_visits: number
  total_spend: number
  last_visit_at?: string
  tags: Tag[]
  created_at: string
}

export interface CustomerDetail extends Customer {
  phone_country_code?: string
  date_of_birth?: string
  gender?: string
  nationality?: string
  address?: string
  city?: string
  country?: string
  instagram?: string
  facebook_url?: string
  company_name?: string
  position?: string
  client_category_id?: number
  average_spend: number
  no_show_count: number
  cancellation_count: number
  blacklist_reason?: string
  blacklisted_at?: string
  notes: CustomerNote[]
}

export interface BirthdayEntry {
  id: number
  fullName: string
  day: number
  month: number
  phone?: string
  categoryName?: string
  categoryColor?: string
  totalVisits: number
}

export interface BlacklistedCustomer {
  id: number
  fullName: string
  phone?: string
  email?: string
  blacklistReason?: string
  blacklistedAt?: string
}

export interface CustomerNote {
  id: number
  note: string
  user_name?: string
  created_at: string
}

export interface CustomerReservation {
  id: number
  guest_name?: string
  covers: number
  date: string
  time: string
  duration_minutes: number
  status: string
  type: string
  seating_type: string
  table_name?: string
  time_slot_name?: string
  outlet_name?: string
  notes?: string
  amount_spent?: number
  closing_comments?: string
  created_at: string
}

export interface CustomerActivityLog {
  id: number
  action: string
  description?: string
  done_by?: string
  created_at: string
}

// ── Floor Plans ──
export type TableShape = 'Rectangle' | 'Circle' | 'Square' | 'Oval' | 'LShape'

export interface Layout {
  id: number
  name: string
  description?: string
  is_default: boolean
  is_active: boolean
  floor_plans: FloorPlan[]
}

export interface FloorPlan {
  id: number
  name: string
  sort_order: number
  width: number
  height: number
  background_color?: string
  is_active: boolean
  tables: TableItem[]
  landmarks: Landmark[]
}

export interface TableItem {
  id: number
  name: string
  label?: string
  min_covers: number
  max_covers: number
  shape: TableShape
  x: number
  y: number
  width: number
  height: number
  rotation: number
  is_combinable: boolean
  is_active: boolean
  table_type_id?: number
  table_type_name?: string
  current_status?: string
  current_reservation_guest?: string
}

export interface Landmark {
  id: number
  name: string
  type: string
  icon?: string
  x: number
  y: number
  width: number
  height: number
  rotation: number
}

// ── Time Slots ──
export interface TimeSlot {
  id: number
  name: string
  start_time: string
  end_time: string
  layout_id?: number
  layout_name?: string
  monday: boolean
  tuesday: boolean
  wednesday: boolean
  thursday: boolean
  friday: boolean
  saturday: boolean
  sunday: boolean
  start_date?: string
  end_date?: string
  max_covers: number
  max_reservations: number
  turn_time_minutes: number
  grace_period_minutes: number
  require_deposit: boolean
  deposit_amount_per_person: number
  is_active: boolean
  excluded_category_ids: number[]
}

export interface TimeSlotFull extends TimeSlot {
  outlet_id: number
  outlet_name: string
}

// ── Settings ──
export interface CompanySettings {
  id: number
  name: string
  email?: string
  phone?: string
  website?: string
  logo_url?: string
  timezone: string
  default_currency: string
  default_locale: string
}

export interface OutletArea {
  id: number
  name: string
  sort_order: number
}

export interface Outlet {
  id: number
  name: string
  address?: string
  phone?: string
  email?: string
  default_grace_period_minutes: number
  default_turn_time_minutes: number
  auto_confirm_online: boolean
  is_active: boolean
  areas: OutletArea[]
}

export interface UserListItem {
  id: number
  username: string
  email: string
  first_name: string
  last_name: string
  full_name: string
  phone?: string
  is_active: boolean
  last_login_at?: string
  roles: string[]
  outlets: string[]
}

export interface RoleDetail {
  id: number
  name: string
  slug: string
  description?: string
  is_system: boolean
  permission_ids: number[]
}

export interface Permission {
  id: number
  key: string
  name: string
  group: string
  description?: string
}

export interface Category {
  id: number
  name: string
  description?: string
  color: string
  priority: number
  is_active: boolean
}

export interface TagSettings {
  id: number
  name: string
  color: string
  type: string
  tag_category_id?: number | null
}

export interface TagCategory {
  id: number
  name: string
  icon?: string
  type: string
  sort_order: number
  tags: TagSettings[]
}

export interface NotificationTemplate {
  id: number
  name: string
  type: string
  channel: string
  subject?: string
  body: string
  is_active: boolean
}

export interface Terms {
  id: number
  title: string
  content: string
  sort_order: number
  is_active: boolean
}

// ── Campaigns ──
export type CampaignStatus = 'Draft' | 'Scheduled' | 'Sending' | 'Sent' | 'Cancelled'

export interface Campaign {
  id: number
  name: string
  subject?: string
  channel: string
  status: CampaignStatus
  target_audience: string
  total_recipients: number
  sent_count: number
  open_count: number
  scheduled_at?: string
  sent_at?: string
  created_at: string
}

// ── Guest Lists ──
export type GuestListItemStatus = 'Invited' | 'Confirmed' | 'CheckedIn' | 'Declined' | 'NoShow'

export interface GuestList {
  id: number
  reservation_id: number
  name?: string
  max_capacity: number
  total_guests: number
  checked_in_count: number
  items: GuestListItem[]
}

export interface GuestListItem {
  id: number
  name: string
  email?: string
  phone?: string
  covers: number
  status: GuestListItemStatus
  notes?: string
  checked_in_at?: string
}
