import type { Device, Statistics } from '../types'

export const mockDevices: Device[] = [
  {
    id: 1,
    name: 'DEV-PC-001',
    ipAddress: '192.168.1.10',
    macAddress: '00:1B:44:11:3A:B7',
    deviceType: 0, // PC
    status: 0, // Active
    model: 'OptiPlex 7090',
    manufacturer: 'Dell',
    location: 'IT Ofis',
    managementType: 1, // AgentInstalled
    discoveryMethod: 4, // AgentRegistration
    firstSeen: '2024-01-15T09:00:00Z',
    lastSeen: '2024-12-20T10:30:00Z',
    createdAt: '2024-01-15T09:00:00Z',
    updatedAt: '2024-12-20T10:30:00Z',
    hardwareInfo: [
      {
        id: 1,
        componentType: 'CPU',
        componentName: 'Intel Core i7-11700',
        deviceId: 1
      },
      {
        id: 2,
        componentType: 'RAM',
        componentName: '32GB DDR4',
        deviceId: 1
      }
    ],
    softwareInfo: [
      {
        id: 1,
        name: 'Windows 11 Pro',
        version: '23H2',
        deviceId: 1
      },
      {
        id: 2,
        name: 'Microsoft Office',
        version: '365',
        deviceId: 1
      }
    ]
  },
  {
    id: 2,
    name: 'LAP-HR-002',
    ipAddress: '192.168.1.25',
    macAddress: '00:1E:58:C2:8B:F1',
    deviceType: 1, // Laptop
    status: 0, // Active
    model: 'ThinkPad X1 Carbon',
    manufacturer: 'Lenovo',
    location: 'İK Departmanı',
    managementType: 1, // AgentInstalled
    discoveryMethod: 4, // AgentRegistration
    firstSeen: '2024-02-01T14:20:00Z',
    lastSeen: '2024-12-20T15:45:00Z',
    createdAt: '2024-02-01T14:20:00Z',
    updatedAt: '2024-12-20T15:45:00Z',
    hardwareInfo: [
      {
        id: 3,
        componentType: 'CPU',
        componentName: 'Intel Core i5-1135G7',
        deviceId: 2
      },
      {
        id: 4,
        componentType: 'RAM',
        componentName: '16GB DDR4',
        deviceId: 2
      }
    ],
    softwareInfo: [
      {
        id: 3,
        name: 'Windows 11 Pro',
        version: '23H2',
        deviceId: 2
      }
    ]
  },
  {
    id: 3,
    name: 'SRV-DB-001',
    ipAddress: '192.168.1.100',
    macAddress: '00:25:90:88:2C:BA',
    deviceType: 2, // Server
    status: 0, // Active
    model: 'PowerEdge R750',
    manufacturer: 'Dell',
    location: 'Sunucu Odası',
    managementType: 2, // NetworkDiscovered
    discoveryMethod: 3, // SNMP
    firstSeen: '2024-01-10T08:00:00Z',
    lastSeen: '2024-12-20T16:00:00Z',
    createdAt: '2024-01-10T08:00:00Z',
    updatedAt: '2024-12-20T16:00:00Z',
    hardwareInfo: [
      {
        id: 5,
        componentType: 'CPU',
        componentName: 'Intel Xeon Silver 4314',
        deviceId: 3
      },
      {
        id: 6,
        componentType: 'RAM',
        componentName: '128GB DDR4 ECC',
        deviceId: 3
      }
    ],
    softwareInfo: [
      {
        id: 4,
        name: 'Windows Server 2022',
        version: 'Standard',
        deviceId: 3
      },
      {
        id: 5,
        name: 'SQL Server',
        version: '2022',
        deviceId: 3
      }
    ]
  },
  {
    id: 4,
    name: 'PRT-OFC-001',
    ipAddress: '192.168.1.50',
    macAddress: '00:11:85:3F:D4:12',
    deviceType: 3, // Printer
    status: 1, // Inactive
    model: 'LaserJet Pro M404dn',
    manufacturer: 'HP',
    location: 'Ana Ofis',
    managementType: 2, // NetworkDiscovered
    discoveryMethod: 2, // PortScan
    firstSeen: '2024-03-05T11:30:00Z',
    lastSeen: '2024-12-19T17:20:00Z',
    createdAt: '2024-03-05T11:30:00Z',
    updatedAt: '2024-12-19T17:20:00Z'
  },
  {
    id: 5,
    name: 'NET-SW-001',
    ipAddress: '192.168.1.1',
    macAddress: '00:1F:26:A8:7C:65',
    deviceType: 4, // NetworkDevice
    status: 0, // Active
    model: 'SG350-28',
    manufacturer: 'Cisco',
    location: 'Ağ Dolabı',
    managementType: 2, // NetworkDiscovered
    discoveryMethod: 3, // SNMP
    firstSeen: '2024-01-08T07:00:00Z',
    lastSeen: '2024-12-20T16:30:00Z',
    createdAt: '2024-01-08T07:00:00Z',
    updatedAt: '2024-12-20T16:30:00Z'
  },
  {
    id: 6,
    name: 'MOB-TAB-001',
    ipAddress: '192.168.1.45',
    macAddress: '00:A0:C9:14:C8:29',
    deviceType: 5, // MobileDevice
    status: 0, // Active
    model: 'iPad Pro',
    manufacturer: 'Apple',
    location: 'Toplantı Odası',
    managementType: 0, // Unmanaged
    discoveryMethod: 1, // Ping
    firstSeen: '2024-04-12T13:15:00Z',
    lastSeen: '2024-12-20T14:10:00Z',
    createdAt: '2024-04-12T13:15:00Z',
    updatedAt: '2024-12-20T14:10:00Z'
  }
]

export const mockStatistics: Statistics = {
  totalDevices: 6,
  activeDevices: 5,
  agentDevices: 2,
  networkDevices: 3
}