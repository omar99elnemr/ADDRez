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
    if (error.response?.status === 401) {
      localStorage.removeItem('addrez_token')
      localStorage.removeItem('addrez_user')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export default api
