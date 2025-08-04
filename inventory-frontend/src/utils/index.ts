import { DeviceType, DeviceStatus, ManagementType, DiscoveryMethod } from '../types'
import { 
  Monitor, 
  Laptop, 
  Server, 
  Printer, 
  Router, 
  Smartphone, 
  HelpCircle 
} from 'lucide-react'

export const getDeviceTypeIcon = (deviceType: DeviceType) => {
  switch (deviceType) {
    case DeviceType.PC:
      return Monitor
    case DeviceType.Laptop:
      return Laptop
    case DeviceType.Server:
      return Server
    case DeviceType.Printer:
      return Printer
    case DeviceType.NetworkDevice:
      return Router
    case DeviceType.MobileDevice:
      return Smartphone
    default:
      return HelpCircle
  }
}

export const getDeviceTypeText = (deviceType: DeviceType): string => {
  switch (deviceType) {
    case DeviceType.PC:
      return 'PC'
    case DeviceType.Laptop:
      return 'Laptop'
    case DeviceType.Server:
      return 'Server'
    case DeviceType.Printer:
      return 'Printer'
    case DeviceType.NetworkDevice:
      return 'Network Device'
    case DeviceType.MobileDevice:
      return 'Mobile Device'
    default:
      return 'Unknown'
  }
}

export const getDeviceTypeColor = (deviceType: DeviceType): string => {
  switch (deviceType) {
    case DeviceType.PC:
      return 'bg-blue-100 text-blue-800'
    case DeviceType.Laptop:
      return 'bg-purple-100 text-purple-800'
    case DeviceType.Server:
      return 'bg-red-100 text-red-800'
    case DeviceType.Printer:
      return 'bg-orange-100 text-orange-800'
    case DeviceType.NetworkDevice:
      return 'bg-green-100 text-green-800'
    case DeviceType.MobileDevice:
      return 'bg-indigo-100 text-indigo-800'
    default:
      return 'bg-gray-100 text-gray-800'
  }
}

export const getStatusText = (status: DeviceStatus): string => {
  switch (status) {
    case DeviceStatus.Active:
      return 'Aktif'
    case DeviceStatus.Inactive:
      return 'Pasif'
    case DeviceStatus.Maintenance:
      return 'Bakım'
    case DeviceStatus.Broken:
      return 'Arızalı'
    default:
      return 'Bilinmiyor'
  }
}

export const getStatusColor = (status: DeviceStatus): string => {
  switch (status) {
    case DeviceStatus.Active:
      return 'bg-green-100 text-green-800'
    case DeviceStatus.Inactive:
      return 'bg-gray-100 text-gray-800'
    case DeviceStatus.Maintenance:
      return 'bg-yellow-100 text-yellow-800'
    case DeviceStatus.Broken:
      return 'bg-red-100 text-red-800'
    default:
      return 'bg-gray-100 text-gray-800'
  }
}

export const getManagementTypeText = (managementType: ManagementType): string => {
  switch (managementType) {
    case ManagementType.Unmanaged:
      return 'Yönetilmeyen'
    case ManagementType.AgentInstalled:
      return 'Agent Kurulu'
    case ManagementType.NetworkDiscovered:
      return 'Ağ Keşfi'
    default:
      return 'Bilinmiyor'
  }
}

export const getDiscoveryMethodText = (discoveryMethod: DiscoveryMethod): string => {
  switch (discoveryMethod) {
    case DiscoveryMethod.Unknown:
      return 'Bilinmiyor'
    case DiscoveryMethod.Ping:
      return 'Ping'
    case DiscoveryMethod.PortScan:
      return 'Port Tarama'
    case DiscoveryMethod.SNMP:
      return 'SNMP'
    case DiscoveryMethod.AgentRegistration:
      return 'Agent Kaydı'
    default:
      return 'Bilinmiyor'
  }
}

export const formatDate = (dateString?: string): string => {
  if (!dateString) return 'Bilinmiyor'
  
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('tr-TR', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}

export const formatRelativeTime = (dateString?: string): string => {
  if (!dateString) return 'Bilinmiyor'
  
  const date = new Date(dateString)
  const now = new Date()
  const diffInMs = now.getTime() - date.getTime()
  const diffInMinutes = Math.floor(diffInMs / (1000 * 60))
  const diffInHours = Math.floor(diffInMinutes / 60)
  const diffInDays = Math.floor(diffInHours / 24)

  if (diffInMinutes < 1) return 'Şimdi'
  if (diffInMinutes < 60) return `${diffInMinutes} dakika önce`
  if (diffInHours < 24) return `${diffInHours} saat önce`
  if (diffInDays < 7) return `${diffInDays} gün önce`
  
  return formatDate(dateString)
}