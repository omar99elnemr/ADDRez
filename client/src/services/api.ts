import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' }
})

// Attach JWT token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('addrez_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  const outletId = localStorage.getItem('addrez_outlet_id')
  if (outletId) {
    config.headers['X-Outlet-Id'] = outletId
  }

  return config
})

// Handle 401 → redirect to login
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const isOutletDenied =
      error.response?.status === 403
      && typeof error.response?.data?.message === 'string'
      && error.response.data.message.toLowerCase().includes('access denied to this outlet')

    if (isOutletDenied) {
      const originalRequest = error.config as any

      // Try once with a fallback outlet from cached user profile.
      if (originalRequest && !originalRequest._outletRetry) {
        originalRequest._outletRetry = true
        try {
          const rawUser = localStorage.getItem('addrez_user')
          const parsedUser = rawUser ? JSON.parse(rawUser) : null
          const fallbackOutletId = parsedUser?.outlets?.[0]?.id

          if (fallbackOutletId) {
            localStorage.setItem('addrez_outlet_id', String(fallbackOutletId))
            originalRequest.headers = originalRequest.headers ?? {}
            originalRequest.headers['X-Outlet-Id'] = String(fallbackOutletId)
            return api(originalRequest)
          }
        } catch {
          // Ignore parse/runtime issues and continue to hard fallback.
        }
      }

      // Unrecoverable outlet mismatch (token claims are stale) -> force re-auth.
      localStorage.removeItem('addrez_token')
      localStorage.removeItem('addrez_user')
      localStorage.removeItem('addrez_outlet_id')
      window.location.href = '/login'
    }

    if (error.response?.status === 401) {
      localStorage.removeItem('addrez_token')
      localStorage.removeItem('addrez_user')
      localStorage.removeItem('addrez_outlet_id')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export default api
