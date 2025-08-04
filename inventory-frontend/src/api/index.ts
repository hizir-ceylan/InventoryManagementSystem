import axios from 'axios'
import type { Device, NetworkScanRequest, NetworkScanResult, ChangeLog, Statistics } from '../types'
import { mockDevices, mockStatistics } from '../data/mockData'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || ''
const DISABLE_MOCK_DATA = import.meta.env.VITE_DISABLE_MOCK_DATA === 'true'

const api = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Helper function to check if backend is available
const isBackendAvailable = async (): Promise<boolean> => {
  try {
    // Check if API base URL is configured
    if (!API_BASE_URL) {
      console.warn('API_BASE_URL not configured, using mock data')
      return false
    }
    
    // Try to reach the backend API
    await api.get('/device', { timeout: 5000 })
    return true
  } catch (error) {
    console.warn('Backend not available, using mock data:', error)
    return false
  }
}

export const deviceApi = {
  // Get all devices
  getDevices: async (): Promise<Device[]> => {
    if (await isBackendAvailable()) {
      const response = await api.get('/device')
      return response.data
    }
    // Return mock data if backend is not available and mock data is enabled
    if (!DISABLE_MOCK_DATA) {
      return Promise.resolve(mockDevices)
    }
    // If mock data is disabled and backend is not available, throw error
    throw new Error('Backend not available and mock data is disabled')
  },

  // Get device by ID
  getDevice: async (id: number): Promise<Device> => {
    if (await isBackendAvailable()) {
      const response = await api.get(`/device/${id}`)
      return response.data
    }
    // Return mock data if backend is not available and mock data is enabled
    if (!DISABLE_MOCK_DATA) {
      const device = mockDevices.find(d => d.id === id)
      if (!device) {
        throw new Error(`Device with ID ${id} not found`)
      }
      return Promise.resolve(device)
    }
    // If mock data is disabled and backend is not available, throw error
    throw new Error('Backend not available and mock data is disabled')
  },

  // Get devices with agent installed
  getAgentDevices: async (): Promise<Device[]> => {
    if (await isBackendAvailable()) {
      const response = await api.get('/device/agent-installed')
      return response.data
    }
    // Return mock data if backend is not available and mock data is enabled
    if (!DISABLE_MOCK_DATA) {
      return Promise.resolve(mockDevices.filter(d => d.managementType === 1))
    }
    // If mock data is disabled and backend is not available, throw error
    throw new Error('Backend not available and mock data is disabled')
  },

  // Get network discovered devices
  getNetworkDevices: async (): Promise<Device[]> => {
    if (await isBackendAvailable()) {
      const response = await api.get('/device/network-discovered')
      return response.data
    }
    // Return mock data if backend is not available and mock data is enabled
    if (!DISABLE_MOCK_DATA) {
      return Promise.resolve(mockDevices.filter(d => d.managementType === 2))
    }
    // If mock data is disabled and backend is not available, throw error
    throw new Error('Backend not available and mock data is disabled')
  },

  // Add new device
  addDevice: async (device: Partial<Device>): Promise<Device> => {
    if (await isBackendAvailable()) {
      const response = await api.post('/device', device)
      return response.data
    }
    // For development, return a mock response if mock data is enabled
    if (!DISABLE_MOCK_DATA) {
      return Promise.resolve({ ...device, id: Date.now() } as Device)
    }
    // If mock data is disabled and backend is not available, throw error
    throw new Error('Backend not available and mock data is disabled')
  },

  // Update device
  updateDevice: async (id: number, device: Partial<Device>): Promise<Device> => {
    if (await isBackendAvailable()) {
      const response = await api.put(`/device/${id}`, device)
      return response.data
    }
    // For development, return the updated device if mock data is enabled
    if (!DISABLE_MOCK_DATA) {
      return Promise.resolve({ ...device, id } as Device)
    }
    // If mock data is disabled and backend is not available, throw error
    throw new Error('Backend not available and mock data is disabled')
  },

  // Delete device
  deleteDevice: async (id: number): Promise<void> => {
    if (await isBackendAvailable()) {
      await api.delete(`/device/${id}`)
      return
    }
    // For development, just resolve if mock data is enabled
    if (!DISABLE_MOCK_DATA) {
      return Promise.resolve()
    }
    // If mock data is disabled and backend is not available, throw error
    throw new Error('Backend not available and mock data is disabled')
  },
}

export const networkApi = {
  // Start network scan
  startScan: async (request: NetworkScanRequest): Promise<NetworkScanResult[]> => {
    const response = await api.post('/networkscan/start', request)
    return response.data
  },

  // Get scan results
  getScanResults: async (): Promise<NetworkScanResult[]> => {
    const response = await api.get('/networkscan/results')
    return response.data
  },
}

export const changeLogApi = {
  // Get all change logs
  getChangeLogs: async (): Promise<ChangeLog[]> => {
    const response = await api.get('/changelog')
    return response.data
  },

  // Get change logs for a specific device
  getDeviceChangeLogs: async (deviceId: number): Promise<ChangeLog[]> => {
    const response = await api.get(`/changelog/device/${deviceId}`)
    return response.data
  },
}

export const statisticsApi = {
  // Get dashboard statistics
  getStatistics: async (): Promise<Statistics> => {
    if (await isBackendAvailable()) {
      const [allDevices, agentDevices, networkDevices] = await Promise.all([
        deviceApi.getDevices(),
        deviceApi.getAgentDevices(),
        deviceApi.getNetworkDevices(),
      ])

      return {
        totalDevices: allDevices.length,
        activeDevices: allDevices.filter(d => d.status === 0).length,
        agentDevices: agentDevices.length,
        networkDevices: networkDevices.length,
      }
    }
    // Return mock statistics if backend is not available and mock data is enabled
    if (!DISABLE_MOCK_DATA) {
      return Promise.resolve(mockStatistics)
    }
    // If mock data is disabled and backend is not available, throw error
    throw new Error('Backend not available and mock data is disabled')
  },
}

export default api