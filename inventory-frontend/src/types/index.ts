export interface Device {
  id: number
  name: string
  ipAddress: string
  macAddress: string
  deviceType: DeviceType
  status: DeviceStatus
  model?: string
  location?: string
  managementType: ManagementType
  discoveryMethod: DiscoveryMethod
  firstSeen?: string
  lastSeen?: string
  createdAt?: string
  updatedAt?: string
  hardwareInfo?: HardwareInfo[]
  softwareInfo?: SoftwareInfo[]
}

export const DeviceType = {
  PC: 0,
  Laptop: 1,
  Server: 2,
  Printer: 3,
  NetworkDevice: 4,
  MobileDevice: 5,
  Unknown: 6
} as const

export type DeviceType = typeof DeviceType[keyof typeof DeviceType]

export const DeviceStatus = {
  Active: 0,
  Inactive: 1,
  Maintenance: 2,
  Broken: 3
} as const

export type DeviceStatus = typeof DeviceStatus[keyof typeof DeviceStatus]

export const ManagementType = {
  Unmanaged: 0,
  AgentInstalled: 1,
  NetworkDiscovered: 2
} as const

export type ManagementType = typeof ManagementType[keyof typeof ManagementType]

export const DiscoveryMethod = {
  Unknown: 0,
  Ping: 1,
  PortScan: 2,
  SNMP: 3,
  AgentRegistration: 4
} as const

export type DiscoveryMethod = typeof DiscoveryMethod[keyof typeof DiscoveryMethod]

export interface HardwareInfo {
  id: number
  componentType: string
  componentName: string
  deviceId: number
}

export interface SoftwareInfo {
  id: number
  name: string
  version: string
  deviceId: number
}

export interface NetworkScanRequest {
  networkRange: string
}

export interface NetworkScanResult {
  id: number
  ipAddress: string
  status: string
  discoveredAt: string
  deviceName?: string
}

export interface ChangeLog {
  id: number
  deviceId: number
  deviceName: string
  changeType: string
  description: string
  timestamp: string
  details?: string
}

export interface Statistics {
  totalDevices: number
  activeDevices: number
  agentDevices: number
  networkDevices: number
}