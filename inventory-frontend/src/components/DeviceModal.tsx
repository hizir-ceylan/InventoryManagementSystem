import React from 'react'
import { X, Info, Calendar, Cpu, Package } from 'lucide-react'
import type { Device } from '../types'
import { 
  getDeviceTypeText,
  getDeviceTypeColor,
  getStatusText,
  getStatusColor,
  getManagementTypeText,
  getDiscoveryMethodText,
  formatDate
} from '../utils'

interface DeviceModalProps {
  device: Device
  isOpen: boolean
  onClose: () => void
}

const DeviceModal: React.FC<DeviceModalProps> = ({ device, isOpen, onClose }) => {
  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex min-h-screen items-center justify-center p-4">
        <div className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" onClick={onClose} />
        
        <div className="relative bg-white rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-y-auto">
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b border-gray-200">
            <h2 className="text-xl font-semibold text-gray-900">Cihaz Detayları</h2>
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600 transition-colors"
            >
              <X className="h-6 w-6" />
            </button>
          </div>

          <div className="p-6 space-y-6">
            {/* General Information */}
            <div>
              <div className="flex items-center mb-4">
                <Info className="h-5 w-5 text-blue-600 mr-2" />
                <h3 className="text-lg font-medium text-gray-900">Genel Bilgiler</h3>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 bg-gray-50 p-4 rounded-lg">
                <div className="space-y-3">
                  <div className="flex justify-between">
                    <span className="text-sm font-medium text-gray-500">Cihaz Adı:</span>
                    <span className="text-sm text-gray-900">{device.name || 'N/A'}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm font-medium text-gray-500">IP Adresi:</span>
                    <span className="text-sm text-gray-900">{device.ipAddress || 'N/A'}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm font-medium text-gray-500">MAC Adresi:</span>
                    <span className="text-sm text-gray-900 font-mono">{device.macAddress || 'N/A'}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm font-medium text-gray-500">Cihaz Türü:</span>
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getDeviceTypeColor(device.deviceType)}`}>
                      {getDeviceTypeText(device.deviceType)}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm font-medium text-gray-500">Durum:</span>
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getStatusColor(device.status)}`}>
                      {getStatusText(device.status)}
                    </span>
                  </div>
                </div>
                <div className="space-y-3">
                  <div className="flex justify-between">
                    <span className="text-sm font-medium text-gray-500">Model:</span>
                    <span className="text-sm text-gray-900">{device.model || 'N/A'}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm font-medium text-gray-500">Konum:</span>
                    <span className="text-sm text-gray-900">{device.location || 'N/A'}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm font-medium text-gray-500">Yönetim Türü:</span>
                    <span className="text-sm text-gray-900">{getManagementTypeText(device.managementType)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm font-medium text-gray-500">Keşif Yöntemi:</span>
                    <span className="text-sm text-gray-900">{getDiscoveryMethodText(device.discoveryMethod)}</span>
                  </div>
                </div>
              </div>
            </div>

            {/* Time Information */}
            <div>
              <div className="flex items-center mb-4">
                <Calendar className="h-5 w-5 text-green-600 mr-2" />
                <h3 className="text-lg font-medium text-gray-900">Zaman Bilgileri</h3>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 bg-gray-50 p-4 rounded-lg">
                <div className="space-y-3">
                  <div className="flex justify-between">
                    <span className="text-sm font-medium text-gray-500">İlk Görülme:</span>
                    <span className="text-sm text-gray-900">{formatDate(device.firstSeen)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm font-medium text-gray-500">Son Görülme:</span>
                    <span className="text-sm text-gray-900">{formatDate(device.lastSeen)}</span>
                  </div>
                </div>
                <div className="space-y-3">
                  <div className="flex justify-between">
                    <span className="text-sm font-medium text-gray-500">Oluşturulma:</span>
                    <span className="text-sm text-gray-900">{formatDate(device.createdAt)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm font-medium text-gray-500">Son Güncelleme:</span>
                    <span className="text-sm text-gray-900">{formatDate(device.updatedAt)}</span>
                  </div>
                </div>
              </div>
            </div>

            {/* Hardware Information */}
            {device.hardwareInfo && device.hardwareInfo.length > 0 && (
              <div>
                <div className="flex items-center mb-4">
                  <Cpu className="h-5 w-5 text-purple-600 mr-2" />
                  <h3 className="text-lg font-medium text-gray-900">Donanım Bilgileri</h3>
                </div>
                <div className="bg-gray-50 p-4 rounded-lg">
                  <div className="space-y-2">
                    {device.hardwareInfo.map((hw, index) => (
                      <div key={index} className="flex justify-between py-1">
                        <span className="text-sm font-medium text-gray-500">{hw.componentType || 'Bilinmeyen'}:</span>
                        <span className="text-sm text-gray-900">{hw.componentName || 'N/A'}</span>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            )}

            {/* Software Information */}
            {device.softwareInfo && device.softwareInfo.length > 0 && (
              <div>
                <div className="flex items-center mb-4">
                  <Package className="h-5 w-5 text-orange-600 mr-2" />
                  <h3 className="text-lg font-medium text-gray-900">Yazılım Bilgileri</h3>
                </div>
                <div className="bg-gray-50 p-4 rounded-lg">
                  <div className="space-y-2 max-h-60 overflow-y-auto">
                    {device.softwareInfo.slice(0, 20).map((sw, index) => (
                      <div key={index} className="flex justify-between py-1">
                        <span className="text-sm font-medium text-gray-500">{sw.name || 'Bilinmeyen'}:</span>
                        <span className="text-sm text-gray-900">{sw.version || 'N/A'}</span>
                      </div>
                    ))}
                    {device.softwareInfo.length > 20 && (
                      <p className="text-sm text-gray-500 italic mt-2">
                        ve {device.softwareInfo.length - 20} yazılım daha...
                      </p>
                    )}
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Footer */}
          <div className="flex justify-end p-6 border-t border-gray-200">
            <button
              onClick={onClose}
              className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              Kapat
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

export default DeviceModal