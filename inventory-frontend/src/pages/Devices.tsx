import React, { useState } from 'react'
import { Link } from 'react-router-dom'
import { Search, Filter, RotateCcw, Eye, Plus, Building, BarChart3 } from 'lucide-react'
import { useDevices } from '../hooks'
import { 
  getDeviceTypeIcon, 
  getDeviceTypeText, 
  getDeviceTypeColor,
  getStatusText,
  getStatusColor,
  formatRelativeTime 
} from '../utils'

const Devices: React.FC = () => {
  const { data: devices, isLoading, refetch } = useDevices()
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('')
  const [typeFilter, setTypeFilter] = useState<string>('')

  // Filter devices based on search and filters
  const filteredDevices = devices?.filter((device) => {
    const matchesSearch = !searchTerm || 
      device.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      device.ipAddress?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      device.macAddress?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      device.model?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      device.manufacturer?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      device.location?.toLowerCase().includes(searchTerm.toLowerCase())

    const matchesStatus = !statusFilter || device.status.toString() === statusFilter
    const matchesType = !typeFilter || device.deviceType.toString() === typeFilter

    return matchesSearch && matchesStatus && matchesType
  }) || []

  const clearFilters = () => {
    setSearchTerm('')
    setStatusFilter('')
    setTypeFilter('')
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Cihazlar</h1>
          <p className="text-gray-600">Kayıtlı tüm cihazları görüntüle ve yönet</p>
        </div>
        <div className="mt-4 sm:mt-0 flex space-x-3">
          <button
            onClick={() => refetch()}
            className="inline-flex items-center px-6 py-3 border border-gray-300 rounded-xl shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-all duration-200 transform hover:scale-105"
          >
            <RotateCcw className="h-4 w-4 mr-2" />
            Yenile
          </button>
          <button className="inline-flex items-center px-6 py-3 border border-transparent rounded-xl shadow-sm text-sm font-medium text-white bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-all duration-200 transform hover:scale-105">
            <Plus className="h-4 w-4 mr-2" />
            Cihaz Ekle
          </button>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div className="relative">
            <Search className="absolute left-4 top-1/2 transform -translate-y-1/2 h-5 w-5 text-gray-400" />
            <input
              type="text"
              placeholder="Cihaz adı, IP, MAC, üretici ile arama..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-12 pr-4 py-3 w-full border border-gray-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all duration-200 bg-gray-50 focus:bg-white"
            />
          </div>
          
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="border border-gray-300 rounded-xl px-4 py-3 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all duration-200 bg-gray-50 focus:bg-white"
          >
            <option value="">Tüm Durumlar</option>
            <option value="0">Aktif</option>
            <option value="1">Pasif</option>
            <option value="2">Bakım</option>
            <option value="3">Arızalı</option>
          </select>

          <select
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
            className="border border-gray-300 rounded-xl px-4 py-3 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all duration-200 bg-gray-50 focus:bg-white"
          >
            <option value="">Tüm Türler</option>
            <option value="0">PC</option>
            <option value="1">Laptop</option>
            <option value="2">Server</option>
            <option value="3">Printer</option>
            <option value="4">Network Device</option>
            <option value="5">Mobile Device</option>
            <option value="6">Unknown</option>
          </select>

          <button
            onClick={clearFilters}
            className="inline-flex items-center justify-center px-6 py-3 border border-gray-300 rounded-xl shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-all duration-200 transform hover:scale-105"
          >
            <Filter className="h-4 w-4 mr-2" />
            Temizle
          </button>
        </div>
      </div>

      {/* Devices Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        {isLoading ? (
          <div className="p-12 text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-4 border-gray-200 border-t-blue-600 mx-auto"></div>
            <p className="mt-4 text-gray-600 font-medium">Veriler yükleniyor...</p>
            <p className="text-sm text-gray-500">Lütfen bekleyiniz</p>
          </div>
        ) : filteredDevices.length === 0 ? (
          <div className="p-12 text-center">
            <div className="text-gray-300 text-8xl mb-6">📦</div>
            <p className="text-xl font-medium text-gray-700 mb-2">
              {devices?.length === 0 ? 'Henüz hiç cihaz bulunmuyor' : 'Arama sonucu bulunamadı'}
            </p>
            <p className="text-gray-500 mb-6">
              {devices?.length === 0 
                ? 'Sisteme cihaz eklemek için yukarıdaki "Cihaz Ekle" butonunu kullanabilirsiniz.' 
                : 'Arama kriterlerinizi değiştirerek tekrar deneyin.'}
            </p>
            {devices?.length === 0 && (
              <button className="inline-flex items-center px-6 py-3 border border-transparent rounded-xl shadow-sm text-sm font-medium text-white bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-all duration-200">
                <Plus className="h-4 w-4 mr-2" />
                İlk Cihazı Ekle
              </button>
            )}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gradient-to-r from-gray-50 to-gray-100">
                <tr>
                  <th className="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                    Cihaz
                  </th>
                  <th className="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                    IP Adresi
                  </th>
                  <th className="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider hidden md:table-cell">
                    MAC Adresi
                  </th>
                  <th className="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider hidden lg:table-cell">
                    Üretici
                  </th>
                  <th className="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                    Tür
                  </th>
                  <th className="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                    Durum
                  </th>
                  <th className="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider hidden lg:table-cell">
                    Model
                  </th>
                  <th className="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider hidden lg:table-cell">
                    Son Görülme
                  </th>
                  <th className="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                    İşlemler
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-100">
                {filteredDevices.map((device, index) => {
                  const DeviceIcon = getDeviceTypeIcon(device.deviceType)
                  
                  return (
                    <tr key={device.id} className={`hover:bg-blue-50 transition-colors duration-200 ${
                      index % 2 === 0 ? 'bg-white' : 'bg-gray-50'
                    }`}>
                      <td className="px-6 py-5 whitespace-nowrap">
                        <div className="flex items-center">
                          <div className="p-2 bg-gray-100 rounded-xl mr-4">
                            <DeviceIcon className="h-5 w-5 text-gray-600" />
                          </div>
                          <div>
                            <div className="text-sm font-semibold text-gray-900">
                              {device.name || 'N/A'}
                            </div>
                            <div className="text-xs text-gray-500 bg-gray-100 px-2 py-1 rounded-full inline-block">
                              ID: #{device.id}
                            </div>
                          </div>
                        </div>
                      </td>
                      <td className="px-6 py-5 whitespace-nowrap">
                        <code className="text-sm bg-blue-50 text-blue-700 px-3 py-1 rounded-lg font-mono">
                          {device.ipAddress || 'N/A'}
                        </code>
                      </td>
                      <td className="px-6 py-5 whitespace-nowrap hidden md:table-cell">
                        <code className="text-sm bg-gray-50 text-gray-600 px-3 py-1 rounded-lg font-mono">
                          {device.macAddress || 'N/A'}
                        </code>
                      </td>
                      <td className="px-6 py-5 whitespace-nowrap text-sm text-gray-900 hidden lg:table-cell">
                        {device.manufacturer ? (
                          <div className="flex items-center bg-orange-50 px-3 py-1 rounded-lg">
                            <Building className="h-4 w-4 mr-2 text-orange-600" />
                            <span className="text-orange-700 font-medium">{device.manufacturer}</span>
                          </div>
                        ) : (
                          <span className="text-gray-400">N/A</span>
                        )}
                      </td>
                      <td className="px-6 py-5 whitespace-nowrap">
                        <span className={`inline-flex px-3 py-1 text-xs font-bold rounded-full ${getDeviceTypeColor(device.deviceType)}`}>
                          {getDeviceTypeText(device.deviceType)}
                        </span>
                      </td>
                      <td className="px-6 py-5 whitespace-nowrap">
                        <span className={`inline-flex px-3 py-1 text-xs font-bold rounded-full ${getStatusColor(device.status)}`}>
                          {getStatusText(device.status)}
                        </span>
                      </td>
                      <td className="px-6 py-5 whitespace-nowrap text-sm text-gray-600 hidden lg:table-cell">
                        {device.model ? (
                          <span className="bg-purple-50 text-purple-700 px-3 py-1 rounded-lg font-medium">
                            {device.model}
                          </span>
                        ) : (
                          <span className="text-gray-400">N/A</span>
                        )}
                      </td>
                      <td className="px-6 py-5 whitespace-nowrap text-sm text-gray-500 hidden lg:table-cell">
                        <span className="bg-green-50 text-green-700 px-3 py-1 rounded-lg">
                          {formatRelativeTime(device.lastSeen)}
                        </span>
                      </td>
                      <td className="px-6 py-5 whitespace-nowrap text-sm font-medium">
                        <Link
                          to={`/devices/${device.id}`}
                          className="inline-flex items-center px-4 py-2 bg-gradient-to-r from-blue-500 to-indigo-600 text-white rounded-lg hover:from-blue-600 hover:to-indigo-700 transition-all duration-200 transform hover:scale-105"
                        >
                          <Eye className="h-4 w-4 mr-2" />
                          <span className="hidden sm:inline">Detay</span>
                        </Link>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Summary Statistics */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-6 flex items-center">
          <BarChart3 className="h-5 w-5 text-gray-600 mr-2" />
          Özet İstatistikler
        </h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
          <div className="text-center p-4 bg-blue-50 rounded-xl">
            <div className="text-3xl font-bold text-blue-600 mb-1">{filteredDevices.length}</div>
            <div className="text-sm text-blue-700 font-medium">Toplam Cihaz</div>
          </div>
          <div className="text-center p-4 bg-green-50 rounded-xl">
            <div className="text-3xl font-bold text-green-600 mb-1">
              {filteredDevices.filter(d => d.status === 0).length}
            </div>
            <div className="text-sm text-green-700 font-medium">Aktif</div>
          </div>
          <div className="text-center p-4 bg-orange-50 rounded-xl">
            <div className="text-3xl font-bold text-orange-600 mb-1">
              {filteredDevices.filter(d => d.managementType === 1).length}
            </div>
            <div className="text-sm text-orange-700 font-medium">Agent Kurulu</div>
          </div>
          <div className="text-center p-4 bg-purple-50 rounded-xl">
            <div className="text-3xl font-bold text-purple-600 mb-1">
              {new Set(filteredDevices.map(d => d.manufacturer).filter(Boolean)).size}
            </div>
            <div className="text-sm text-purple-700 font-medium">Farklı Üretici</div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default Devices