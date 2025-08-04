import axios from 'axios'
import type { Device, NetworkScanRequest, NetworkScanResult, ChangeLog, Statistics } from '../types'
import { mockDevices, mockStatistics } from '../data/mockData'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || ''

const api = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Helper function to check if backend is available
const isBackendAvailable = async (): Promise<boolean> => {
  try {
    // For development, we'll always use mock data until backend is running
    // await api.get('/health', { timeout: 2000 })
    // return true
    return false
  } catch {
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
    // Return mock data if backend is not available
    return Promise.resolve(mockDevices)
  },

  // Get device by ID
  getDevice: async (id: number): Promise<Device> => {
    if (await isBackendAvailable()) {
      const response = await api.get(`/device/${id}`)
      return response.data
    }
    // Return mock data if backend is not available
    const device = mockDevices.find(d => d.id === id)
    if (!device) {
      throw new Error(`Device with ID ${id} not found`)
    }
    return Promise.resolve(device)
  },

  // Get devices with agent installed
  getAgentDevices: async (): Promise<Device[]> => {
    if (await isBackendAvailable()) {
      const response = await api.get('/device/agent-installed')
      return response.data
    }
    // Return mock data if backend is not available
    return Promise.resolve(mockDevices.filter(d => d.managementType === 1))
  },

  // Get network discovered devices
  getNetworkDevices: async (): Promise<Device[]> => {
    if (await isBackendAvailable()) {
      const response = await api.get('/device/network-discovered')
      return response.data
    }
    // Return mock data if backend is not available
    return Promise.resolve(mockDevices.filter(d => d.managementType === 2))
  },

  // Add new device
  addDevice: async (device: Partial<Device>): Promise<Device> => {
    if (await isBackendAvailable()) {
      const response = await api.post('/device', device)
      return response.data
    }
    // For development, just return a mock response
    return Promise.resolve({ ...device, id: Date.now() } as Device)
  },

  // Update device
  updateDevice: async (id: number, device: Partial<Device>): Promise<Device> => {
    if (await isBackendAvailable()) {
      const response = await api.put(`/device/${id}`, device)
      return response.data
    }
    // For development, just return the updated device
    return Promise.resolve({ ...device, id } as Device)
  },

  // Delete device
  deleteDevice: async (id: number): Promise<void> => {
    if (await isBackendAvailable()) {
      await api.delete(`/device/${id}`)
    }
    // For development, just resolve
    return Promise.resolve()
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
    // Return mock statistics if backend is not available
    return Promise.resolve(mockStatistics)
  },
}

export default api