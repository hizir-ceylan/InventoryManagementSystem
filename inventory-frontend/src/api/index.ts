import axios from 'axios'
import type { Device, NetworkScanRequest, NetworkScanResult, ChangeLog, Statistics } from '../types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || ''

const api = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
})

export const deviceApi = {
  // Get all devices
  getDevices: async (): Promise<Device[]> => {
    const response = await api.get('/device')
    return response.data
  },

  // Get device by ID
  getDevice: async (id: number): Promise<Device> => {
    const response = await api.get(`/device/${id}`)
    return response.data
  },

  // Get devices with agent installed
  getAgentDevices: async (): Promise<Device[]> => {
    const response = await api.get('/device/agent-installed')
    return response.data
  },

  // Get network discovered devices
  getNetworkDevices: async (): Promise<Device[]> => {
    const response = await api.get('/device/network-discovered')
    return response.data
  },

  // Add new device
  addDevice: async (device: Partial<Device>): Promise<Device> => {
    const response = await api.post('/device', device)
    return response.data
  },

  // Update device
  updateDevice: async (id: number, device: Partial<Device>): Promise<Device> => {
    const response = await api.put(`/device/${id}`, device)
    return response.data
  },

  // Delete device
  deleteDevice: async (id: number): Promise<void> => {
    await api.delete(`/device/${id}`)
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
  },
}

export default api