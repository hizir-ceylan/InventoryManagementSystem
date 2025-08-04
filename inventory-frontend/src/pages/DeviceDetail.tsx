import React from 'react'
import { useParams, Link } from 'react-router-dom'
import { 
  ArrowLeft, 
  Monitor, 
  Wifi, 
  MapPin, 
  Calendar, 
  CheckCircle2,
  AlertCircle,
  Wrench,
  XCircle,
  Cpu,
  HardDrive,
  MemoryStick,
  Package,
  Globe,
  Shield
} from 'lucide-react'
import { useDevice } from '../hooks'
import { 
  getDeviceTypeIcon, 
  getDeviceTypeText, 
  getDeviceTypeColor,
  getStatusText,
  getStatusColor,
  formatRelativeTime,
  formatDate
} from '../utils'

const DeviceDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>()
  const deviceId = id ? parseInt(id, 10) : 0
  const { data: device, isLoading, error } = useDevice(deviceId)

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-600">Cihaz bilgileri yükleniyor...</p>
        </div>
      </div>
    )
  }

  if (error || !device) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <XCircle className="h-16 w-16 text-red-500 mx-auto mb-4" />
          <h1 className="text-2xl font-bold text-gray-900 mb-2">Cihaz Bulunamadı</h1>
          <p className="text-gray-600 mb-6">
            Aradığınız cihaz bulunamadı veya erişim izniniz yok.
          </p>
          <Link
            to="/devices"
            className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Cihazlar Listesine Dön
          </Link>
        </div>
      </div>
    )
  }

  const DeviceIcon = getDeviceTypeIcon(device.deviceType)
  
  const getStatusIcon = (status: number) => {
    switch (status) {
      case 0: return CheckCircle2
      case 1: return XCircle
      case 2: return Wrench
      case 3: return AlertCircle
      default: return AlertCircle
    }
  }

  const StatusIcon = getStatusIcon(device.status)

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white shadow-sm border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            <div className="flex items-center space-x-4">
              <Link
                to="/devices"
                className="inline-flex items-center text-gray-500 hover:text-gray-700 transition-colors duration-200"
              >
                <ArrowLeft className="h-5 w-5 mr-2" />
                Cihazlar
              </Link>
              <div className="text-gray-400">/</div>
              <div className="flex items-center space-x-2">
                <DeviceIcon className="h-5 w-5 text-gray-400" />
                <span className="text-gray-900 font-medium">{device.name || 'İsimsiz Cihaz'}</span>
              </div>
            </div>
            <div className="flex items-center space-x-2">
              <span className={`inline-flex px-3 py-1 text-sm font-semibold rounded-full ${getStatusColor(device.status)}`}>
                <StatusIcon className="h-4 w-4 mr-1" />
                {getStatusText(device.status)}
              </span>
              <span className={`inline-flex px-3 py-1 text-sm font-semibold rounded-full ${getDeviceTypeColor(device.deviceType)}`}>
                {getDeviceTypeText(device.deviceType)}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Main Info */}
          <div className="lg:col-span-2 space-y-6">
            {/* Basic Information */}
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-6 flex items-center">
                <Monitor className="h-5 w-5 mr-2 text-blue-600" />
                Temel Bilgiler
              </h2>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-500">Cihaz ID</label>
                    <p className="mt-1 text-sm text-gray-900 font-mono bg-gray-50 px-3 py-2 rounded-md">
                      #{device.id}
                    </p>
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-500">Cihaz Adı</label>
                    <p className="mt-1 text-sm text-gray-900">{device.name || 'N/A'}</p>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-500">IP Adresi</label>
                    <p className="mt-1 text-sm text-gray-900 font-mono bg-blue-50 px-3 py-2 rounded-md">
                      <Globe className="inline h-4 w-4 mr-2 text-blue-600" />
                      {device.ipAddress || 'N/A'}
                    </p>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-500">MAC Adresi</label>
                    <p className="mt-1 text-sm text-gray-900 font-mono bg-green-50 px-3 py-2 rounded-md">
                      <Wifi className="inline h-4 w-4 mr-2 text-green-600" />
                      {device.macAddress || 'N/A'}
                    </p>
                  </div>
                </div>

                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-500">Model</label>
                    <p className="mt-1 text-sm text-gray-900">{device.model || 'N/A'}</p>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-500">Üretici</label>
                    <p className="mt-1 text-sm text-gray-900">{device.manufacturer || 'N/A'}</p>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-500">Konum</label>
                    <p className="mt-1 text-sm text-gray-900">
                      {device.location ? (
                        <span className="flex items-center">
                          <MapPin className="h-4 w-4 mr-2 text-gray-400" />
                          {device.location}
                        </span>
                      ) : 'N/A'}
                    </p>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-500">Yönetim Türü</label>
                    <p className="mt-1 text-sm text-gray-900">
                      <span className="flex items-center">
                        <Shield className="h-4 w-4 mr-2 text-gray-400" />
                        {device.managementType === 1 ? 'Agent Kurulu' : 
                         device.managementType === 2 ? 'Ağ Keşfi' : 'Yönetilmiyor'}
                      </span>
                    </p>
                  </div>
                </div>
              </div>
            </div>

            {/* Hardware Information */}
            {device.hardwareInfo && device.hardwareInfo.length > 0 && (
              <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <h2 className="text-lg font-semibold text-gray-900 mb-6 flex items-center">
                  <Cpu className="h-5 w-5 mr-2 text-green-600" />
                  Donanım Bilgileri
                </h2>
                
                <div className="space-y-4">
                  {device.hardwareInfo.map((hardware) => (
                    <div key={hardware.id} className="flex items-center justify-between py-3 border-b border-gray-100 last:border-b-0">
                      <div className="flex items-center space-x-3">
                        <div className="p-2 bg-green-100 rounded-lg">
                          {hardware.componentType === 'CPU' && <Cpu className="h-4 w-4 text-green-600" />}
                          {hardware.componentType === 'RAM' && <MemoryStick className="h-4 w-4 text-green-600" />}
                          {hardware.componentType === 'Storage' && <HardDrive className="h-4 w-4 text-green-600" />}
                          {!['CPU', 'RAM', 'Storage'].includes(hardware.componentType) && 
                           <Package className="h-4 w-4 text-green-600" />}
                        </div>
                        <div>
                          <p className="text-sm font-medium text-gray-900">{hardware.componentType}</p>
                          <p className="text-sm text-gray-500">{hardware.componentName}</p>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Software Information */}
            {device.softwareInfo && device.softwareInfo.length > 0 && (
              <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <h2 className="text-lg font-semibold text-gray-900 mb-6 flex items-center">
                  <Package className="h-5 w-5 mr-2 text-purple-600" />
                  Yazılım Bilgileri
                </h2>
                
                <div className="space-y-4">
                  {device.softwareInfo.map((software) => (
                    <div key={software.id} className="flex items-center justify-between py-3 border-b border-gray-100 last:border-b-0">
                      <div className="flex items-center space-x-3">
                        <div className="p-2 bg-purple-100 rounded-lg">
                          <Package className="h-4 w-4 text-purple-600" />
                        </div>
                        <div>
                          <p className="text-sm font-medium text-gray-900">{software.name}</p>
                          <p className="text-sm text-gray-500">Versiyon: {software.version}</p>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Activity Timeline */}
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-6 flex items-center">
                <Calendar className="h-5 w-5 mr-2 text-orange-600" />
                Zaman Çizelgesi
              </h2>
              
              <div className="space-y-4">
                {device.lastSeen && (
                  <div className="flex items-start space-x-3">
                    <div className="w-2 h-2 bg-green-500 rounded-full mt-2"></div>
                    <div>
                      <p className="text-sm font-medium text-gray-900">Son Görülme</p>
                      <p className="text-sm text-gray-500">{formatRelativeTime(device.lastSeen)}</p>
                      <p className="text-xs text-gray-400">{formatDate(device.lastSeen)}</p>
                    </div>
                  </div>
                )}
                
                {device.firstSeen && (
                  <div className="flex items-start space-x-3">
                    <div className="w-2 h-2 bg-blue-500 rounded-full mt-2"></div>
                    <div>
                      <p className="text-sm font-medium text-gray-900">İlk Keşif</p>
                      <p className="text-sm text-gray-500">{formatRelativeTime(device.firstSeen)}</p>
                      <p className="text-xs text-gray-400">{formatDate(device.firstSeen)}</p>
                    </div>
                  </div>
                )}
                
                {device.createdAt && (
                  <div className="flex items-start space-x-3">
                    <div className="w-2 h-2 bg-gray-400 rounded-full mt-2"></div>
                    <div>
                      <p className="text-sm font-medium text-gray-900">Kaydedilme</p>
                      <p className="text-sm text-gray-500">{formatRelativeTime(device.createdAt)}</p>
                      <p className="text-xs text-gray-400">{formatDate(device.createdAt)}</p>
                    </div>
                  </div>
                )}
              </div>
            </div>

            {/* Quick Actions */}
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-6">Hızlı İşlemler</h2>
              
              <div className="space-y-3">
                <button className="w-full text-left p-3 rounded-lg border border-gray-200 hover:border-blue-300 hover:bg-blue-50 transition-colors duration-200">
                  <div className="flex items-center">
                    <Wrench className="h-4 w-4 text-blue-600 mr-3" />
                    <div>
                      <p className="font-medium text-gray-900">Cihazı Düzenle</p>
                      <p className="text-sm text-gray-500">Bilgileri güncelle</p>
                    </div>
                  </div>
                </button>
                
                <button className="w-full text-left p-3 rounded-lg border border-gray-200 hover:border-green-300 hover:bg-green-50 transition-colors duration-200">
                  <div className="flex items-center">
                    <Monitor className="h-4 w-4 text-green-600 mr-3" />
                    <div>
                      <p className="font-medium text-gray-900">Uzaktan Bağlan</p>
                      <p className="text-sm text-gray-500">RDP/SSH bağlantısı</p>
                    </div>
                  </div>
                </button>
                
                <button className="w-full text-left p-3 rounded-lg border border-gray-200 hover:border-orange-300 hover:bg-orange-50 transition-colors duration-200">
                  <div className="flex items-center">
                    <Calendar className="h-4 w-4 text-orange-600 mr-3" />
                    <div>
                      <p className="font-medium text-gray-900">Değişiklik Geçmişi</p>
                      <p className="text-sm text-gray-500">Logları görüntüle</p>
                    </div>
                  </div>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default DeviceDetail