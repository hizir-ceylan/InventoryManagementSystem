import React, { useState } from 'react'
import { Link } from 'react-router-dom'
import { Search, Filter, RotateCcw, Eye, Plus, Building, BarChart3, Sparkles, Zap, TrendingUp } from 'lucide-react'
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

  const summaryStats = [
    {
      title: 'Toplam Cihaz',
      value: filteredDevices.length,
      gradient: 'from-blue-500 to-blue-600',
      bgGradient: 'from-blue-50 to-blue-100',
      textColor: 'text-blue-600',
      icon: Building
    },
    {
      title: 'Aktif',
      value: filteredDevices.filter(d => d.status === 0).length,
      gradient: 'from-green-500 to-green-600',
      bgGradient: 'from-green-50 to-green-100',
      textColor: 'text-green-600',
      icon: Zap
    },
    {
      title: 'Agent Kurulu',
      value: filteredDevices.filter(d => d.managementType === 1).length,
      gradient: 'from-orange-500 to-orange-600',
      bgGradient: 'from-orange-50 to-orange-100',
      textColor: 'text-orange-600',
      icon: TrendingUp
    },
    {
      title: 'Farklı Üretici',
      value: new Set(filteredDevices.map(d => d.manufacturer).filter(Boolean)).size,
      gradient: 'from-purple-500 to-purple-600',
      bgGradient: 'from-purple-50 to-purple-100',
      textColor: 'text-purple-600',
      icon: Sparkles
    }
  ]

  return (
    <div className="space-y-8">
      {/* Enhanced Header */}
      <div className="relative overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-r from-blue-600/10 via-green-600/10 to-purple-600/10 rounded-3xl"></div>
        <div className="absolute inset-0 bg-gradient-to-br from-white to-white/40 rounded-3xl backdrop-blur-sm"></div>
        <div className="relative p-8">
          <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between">
            <div className="space-y-4">
              <div className="flex items-center space-x-4">
                <div className="p-3 bg-gradient-to-r from-green-600 via-blue-600 to-purple-600 rounded-2xl shadow-lg">
                  <Building className="h-8 w-8 text-white" />
                </div>
                <div>
                  <h1 className="text-4xl font-bold bg-gradient-to-r from-gray-900 via-green-800 to-blue-800 bg-clip-text text-transparent">
                    Cihazlar
                  </h1>
                  <p className="text-xl text-gray-600 mt-2 font-medium">Kayıtlı tüm cihazları görüntüle ve yönet</p>
                </div>
              </div>
            </div>
            <div className="mt-6 lg:mt-0 flex space-x-4">
              <button
                onClick={() => refetch()}
                className="inline-flex items-center px-6 py-3 border-2 border-blue-500 text-blue-600 hover:bg-blue-500 hover:text-white font-medium rounded-xl transition-all duration-200 focus:outline-none focus:ring-4 focus:ring-blue-300 transform hover:scale-105"
              >
                <RotateCcw className="h-5 w-5 mr-2" />
                Yenile
              </button>
              <button className="inline-flex items-center px-6 py-3 bg-gradient-to-r from-blue-500 to-blue-600 hover:from-blue-600 hover:to-blue-700 text-white font-medium rounded-xl shadow-lg hover:shadow-xl transform hover:scale-105 transition-all duration-200 focus:outline-none focus:ring-4 focus:ring-blue-300">
                <Plus className="h-5 w-5 mr-2" />
                Cihaz Ekle
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Enhanced Filters */}
      <div className="bg-white backdrop-blur-sm rounded-2xl shadow-lg border border-gray-200 hover:shadow-xl transition-all duration-300">
        <div className="p-8">
          <h2 className="text-xl font-bold text-gray-900 mb-6 flex items-center">
            <div className="p-2 bg-gradient-to-r from-gray-600 to-gray-700 rounded-xl mr-3">
              <Filter className="h-5 w-5 text-white" />
            </div>
            Filtreler
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
            <div className="relative group">
              <Search className="absolute left-4 top-1/2 transform -translate-y-1/2 h-5 w-5 text-gray-400 group-focus-within:text-blue-500 transition-colors duration-200" />
              <input
                type="text"
                placeholder="Cihaz adı, IP, MAC, üretici ile arama..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-12 pr-4 py-4 w-full border-2 border-gray-200 rounded-xl focus:ring-4 focus:ring-blue-500/20 focus:border-blue-500 transition-all duration-200 bg-gray-50 focus:bg-white text-gray-900 placeholder-gray-500"
              />
            </div>
            
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="border-2 border-gray-200 rounded-xl px-4 py-4 focus:ring-4 focus:ring-blue-500/20 focus:border-blue-500 transition-all duration-200 bg-gray-50 focus:bg-white text-gray-900"
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
              className="border-2 border-gray-200 rounded-xl px-4 py-4 focus:ring-4 focus:ring-blue-500/20 focus:border-blue-500 transition-all duration-200 bg-gray-50 focus:bg-white text-gray-900"
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
              className="inline-flex items-center justify-center px-6 py-3 border-2 border-blue-500 text-blue-600 hover:bg-blue-500 hover:text-white font-medium rounded-xl transition-all duration-200 focus:outline-none focus:ring-4 focus:ring-blue-300 transform hover:scale-105"
            >
              <Filter className="h-5 w-5 mr-2" />
              Temizle
            </button>
          </div>
        </div>
      </div>

      {/* Enhanced Devices Table */}
      <div className="bg-white backdrop-blur-sm rounded-2xl shadow-lg border border-gray-200 hover:shadow-xl transition-all duration-300 overflow-hidden">
        {isLoading ? (
          <div className="p-16 text-center">
            <div className="relative">
              <div className="animate-spin rounded-full h-16 w-16 border-4 border-gray-200 border-t-blue-600 mx-auto"></div>
              <div className="absolute inset-0 rounded-full bg-gradient-to-r from-blue-500/20 to-purple-500/20 animate-pulse"></div>
            </div>
            <p className="mt-6 text-xl font-semibold text-gray-700">Veriler yükleniyor...</p>
            <p className="text-sm text-gray-500 mt-2">Lütfen bekleyiniz</p>
          </div>
        ) : filteredDevices.length === 0 ? (
          <div className="p-16 text-center">
            <div className="text-gray-300 text-8xl mb-6">📦</div>
            <p className="text-2xl font-bold text-gray-700 mb-3">
              {devices?.length === 0 ? 'Henüz hiç cihaz bulunmuyor' : 'Arama sonucu bulunamadı'}
            </p>
            <p className="text-gray-500 mb-8 max-w-md mx-auto">
              {devices?.length === 0 
                ? 'Sisteme cihaz eklemek için yukarıdaki "Cihaz Ekle" butonunu kullanabilirsiniz.' 
                : 'Arama kriterlerinizi değiştirerek tekrar deneyin.'}
            </p>
            {devices?.length === 0 && (
              <button className="inline-flex items-center px-6 py-3 bg-gradient-to-r from-blue-500 to-blue-600 hover:from-blue-600 hover:to-blue-700 text-white font-medium rounded-xl shadow-lg hover:shadow-xl transform hover:scale-105 transition-all duration-200 focus:outline-none focus:ring-4 focus:ring-blue-300 mx-auto">
                <Plus className="h-5 w-5 mr-2" />
                İlk Cihazı Ekle
              </button>
            )}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full">
              <thead>
                <tr className="bg-gradient-to-r from-gray-50 via-blue-50 to-purple-50 border-b border-gray-200">
                  <th className="px-8 py-5 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                    Cihaz
                  </th>
                  <th className="px-8 py-5 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                    IP Adresi
                  </th>
                  <th className="px-8 py-5 text-left text-xs font-bold text-gray-700 uppercase tracking-wider hidden md:table-cell">
                    MAC Adresi
                  </th>
                  <th className="px-8 py-5 text-left text-xs font-bold text-gray-700 uppercase tracking-wider hidden lg:table-cell">
                    Üretici
                  </th>
                  <th className="px-8 py-5 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                    Tür
                  </th>
                  <th className="px-8 py-5 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                    Durum
                  </th>
                  <th className="px-8 py-5 text-left text-xs font-bold text-gray-700 uppercase tracking-wider hidden lg:table-cell">
                    Model
                  </th>
                  <th className="px-8 py-5 text-left text-xs font-bold text-gray-700 uppercase tracking-wider hidden lg:table-cell">
                    Son Görülme
                  </th>
                  <th className="px-8 py-5 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                    İşlemler
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {filteredDevices.map((device, index) => {
                  const DeviceIcon = getDeviceTypeIcon(device.deviceType)
                  
                  return (
                    <tr key={device.id} className={`hover:bg-gradient-to-r hover:from-blue-50 hover:to-purple-50 transition-all duration-300 group ${
                      index % 2 === 0 ? 'bg-white' : 'bg-gray-50/50'
                    }`}>
                      <td className="px-8 py-6 whitespace-nowrap">
                        <div className="flex items-center">
                          <div className="p-3 bg-gradient-to-r from-gray-100 to-gray-200 rounded-2xl mr-4 group-hover:from-blue-100 group-hover:to-purple-100 transition-all duration-300">
                            <DeviceIcon className="h-6 w-6 text-gray-600 group-hover:text-blue-600" />
                          </div>
                          <div>
                            <div className="text-sm font-bold text-gray-900 mb-1">
                              {device.name || 'N/A'}
                            </div>
                            <div className="text-xs text-gray-500 bg-gray-100 px-3 py-1 rounded-full inline-block">
                              ID: #{device.id}
                            </div>
                          </div>
                        </div>
                      </td>
                      <td className="px-8 py-6 whitespace-nowrap">
                        <code className="text-sm bg-blue-50 text-blue-700 px-4 py-2 rounded-xl font-mono font-semibold border border-blue-200">
                          {device.ipAddress || 'N/A'}
                        </code>
                      </td>
                      <td className="px-8 py-6 whitespace-nowrap hidden md:table-cell">
                        <code className="text-sm bg-gray-50 text-gray-600 px-4 py-2 rounded-xl font-mono border border-gray-200">
                          {device.macAddress || 'N/A'}
                        </code>
                      </td>
                      <td className="px-8 py-6 whitespace-nowrap text-sm text-gray-900 hidden lg:table-cell">
                        {device.manufacturer ? (
                          <div className="flex items-center bg-gradient-to-r from-orange-50 to-orange-100 px-4 py-2 rounded-xl border border-orange-200">
                            <Building className="h-4 w-4 mr-2 text-orange-600" />
                            <span className="text-orange-700 font-semibold">{device.manufacturer}</span>
                          </div>
                        ) : (
                          <span className="text-gray-400 font-medium">N/A</span>
                        )}
                      </td>
                      <td className="px-8 py-6 whitespace-nowrap">
                        <span className={`inline-flex px-4 py-2 text-xs font-bold rounded-xl shadow-sm ${getDeviceTypeColor(device.deviceType)}`}>
                          {getDeviceTypeText(device.deviceType)}
                        </span>
                      </td>
                      <td className="px-8 py-6 whitespace-nowrap">
                        <span className={`inline-flex px-4 py-2 text-xs font-bold rounded-xl shadow-sm ${getStatusColor(device.status)}`}>
                          {getStatusText(device.status)}
                        </span>
                      </td>
                      <td className="px-8 py-6 whitespace-nowrap text-sm text-gray-600 hidden lg:table-cell">
                        {device.model ? (
                          <span className="bg-gradient-to-r from-purple-50 to-purple-100 text-purple-700 px-4 py-2 rounded-xl font-semibold border border-purple-200">
                            {device.model}
                          </span>
                        ) : (
                          <span className="text-gray-400 font-medium">N/A</span>
                        )}
                      </td>
                      <td className="px-8 py-6 whitespace-nowrap text-sm text-gray-500 hidden lg:table-cell">
                        <span className="bg-gradient-to-r from-green-50 to-green-100 text-green-700 px-4 py-2 rounded-xl font-semibold border border-green-200">
                          {formatRelativeTime(device.lastSeen)}
                        </span>
                      </td>
                      <td className="px-8 py-6 whitespace-nowrap text-sm font-medium">
                        <Link
                          to={`/devices/${device.id}`}
                          className="inline-flex items-center px-5 py-3 bg-gradient-to-r from-blue-500 to-indigo-600 text-white rounded-xl hover:from-blue-600 hover:to-indigo-700 transition-all duration-300 transform hover:scale-105 shadow-lg hover:shadow-xl"
                        >
                          <Eye className="h-4 w-4 mr-2" />
                          <span className="hidden sm:inline font-semibold">Detay</span>
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

      {/* Enhanced Summary Statistics */}
      <div className="bg-white backdrop-blur-sm rounded-2xl shadow-lg border border-gray-200 hover:shadow-xl transition-all duration-300">
        <div className="p-8">
          <h3 className="text-2xl font-bold text-gray-900 mb-8 flex items-center">
            <div className="p-2 bg-gradient-to-r from-indigo-600 to-purple-600 rounded-xl mr-4">
              <BarChart3 className="h-6 w-6 text-white" />
            </div>
            Özet İstatistikler
          </h3>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-6">
            {summaryStats.map((stat, index) => {
              const Icon = stat.icon
              return (
                <div 
                  key={stat.title}
                  className={`text-center p-6 bg-gradient-to-br ${stat.bgGradient} rounded-2xl border border-gray-200/50 hover:shadow-xl transition-all duration-500 group transform hover:scale-105`}
                  style={{ animationDelay: `${index * 100}ms` }}
                >
                  <div className={`p-3 bg-gradient-to-r ${stat.gradient} rounded-xl shadow-lg mx-auto w-fit mb-4 group-hover:scale-110 transition-transform duration-300`}>
                    <Icon className="h-6 w-6 text-white" />
                  </div>
                  <div className={`text-4xl font-bold ${stat.textColor} mb-2 group-hover:scale-110 transition-transform duration-300`}>
                    {stat.value}
                  </div>
                  <div className="text-sm font-semibold text-gray-700">{stat.title}</div>
                </div>
              )
            })}
          </div>
        </div>
      </div>
    </div>
  )
}

export default Devices