import React, { useState } from 'react'
import { Clock, Filter, RefreshCw, Search, FileText } from 'lucide-react'
import { useChangeLogs } from '../hooks'
import { formatDate, formatRelativeTime } from '../utils'

const ChangeLogs: React.FC = () => {
  const { data: changeLogs, isLoading, refetch } = useChangeLogs()
  const [searchTerm, setSearchTerm] = useState('')
  const [changeTypeFilter, setChangeTypeFilter] = useState('')

  // Filter change logs based on search and filters
  const filteredLogs = changeLogs?.filter((log) => {
    const matchesSearch = !searchTerm || 
      log.deviceName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      log.description?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      log.changeType?.toLowerCase().includes(searchTerm.toLowerCase())

    const matchesType = !changeTypeFilter || log.changeType === changeTypeFilter

    return matchesSearch && matchesType
  }) || []

  const clearFilters = () => {
    setSearchTerm('')
    setChangeTypeFilter('')
  }

  const getChangeTypeColor = (changeType: string) => {
    switch (changeType.toLowerCase()) {
      case 'hardware_added':
        return 'bg-green-100 text-green-800'
      case 'hardware_removed':
        return 'bg-red-100 text-red-800'
      case 'hardware_changed':
        return 'bg-yellow-100 text-yellow-800'
      case 'software_installed':
        return 'bg-blue-100 text-blue-800'
      case 'software_uninstalled':
        return 'bg-orange-100 text-orange-800'
      case 'status_changed':
        return 'bg-purple-100 text-purple-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const getChangeTypeText = (changeType: string) => {
    switch (changeType.toLowerCase()) {
      case 'hardware_added':
        return 'Donanım Eklendi'
      case 'hardware_removed':
        return 'Donanım Çıkarıldı'
      case 'hardware_changed':
        return 'Donanım Değişti'
      case 'software_installed':
        return 'Yazılım Kuruldu'
      case 'software_uninstalled':
        return 'Yazılım Kaldırıldı'
      case 'status_changed':
        return 'Durum Değişti'
      default:
        return changeType
    }
  }

  // Get unique change types for filter
  const changeTypes = Array.from(new Set(changeLogs?.map(log => log.changeType) || []))

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Değişiklik Logları</h1>
          <p className="text-gray-600">Sistem değişikliklerini görüntüle ve takip et</p>
        </div>
        <button
          onClick={() => refetch()}
          className="mt-4 sm:mt-0 inline-flex items-center px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
        >
          <RefreshCw className="h-4 w-4 mr-2" />
          Yenile
        </button>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
            <input
              type="text"
              placeholder="Cihaz adı, açıklama ile arama..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10 pr-4 py-2 w-full border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
          
          <select
            value={changeTypeFilter}
            onChange={(e) => setChangeTypeFilter(e.target.value)}
            className="border border-gray-300 rounded-md px-3 py-2 focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="">Tüm Değişiklik Türleri</option>
            {changeTypes.map((type) => (
              <option key={type} value={type}>
                {getChangeTypeText(type)}
              </option>
            ))}
          </select>

          <button
            onClick={clearFilters}
            className="inline-flex items-center justify-center px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            <Filter className="h-4 w-4 mr-2" />
            Temizle
          </button>
        </div>
      </div>

      {/* Change Logs */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200">
        {isLoading ? (
          <div className="p-8 text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
            <p className="mt-2 text-gray-600">Loglar yükleniyor...</p>
          </div>
        ) : filteredLogs.length === 0 ? (
          <div className="p-8 text-center">
            <FileText className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <p className="text-gray-600">
              {changeLogs?.length === 0 ? 'Henüz değişiklik logu bulunmuyor.' : 'Arama kriterlerinize uygun log bulunamadı.'}
            </p>
          </div>
        ) : (
          <div className="divide-y divide-gray-200">
            {filteredLogs.map((log) => (
              <div key={log.id} className="p-6 hover:bg-gray-50">
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center space-x-3 mb-2">
                      <h3 className="text-sm font-medium text-gray-900">
                        {log.deviceName}
                      </h3>
                      <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getChangeTypeColor(log.changeType)}`}>
                        {getChangeTypeText(log.changeType)}
                      </span>
                    </div>
                    <p className="text-sm text-gray-700 mb-2">
                      {log.description}
                    </p>
                    {log.details && (
                      <details className="text-sm text-gray-600">
                        <summary className="cursor-pointer hover:text-gray-800">
                          Detayları görüntüle
                        </summary>
                        <pre className="mt-2 whitespace-pre-wrap bg-gray-50 p-2 rounded text-xs">
                          {log.details}
                        </pre>
                      </details>
                    )}
                  </div>
                  <div className="flex flex-col items-end text-sm text-gray-500 ml-4">
                    <div className="flex items-center">
                      <Clock className="h-4 w-4 mr-1" />
                      <span>{formatRelativeTime(log.timestamp)}</span>
                    </div>
                    <span className="text-xs mt-1">
                      {formatDate(log.timestamp)}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Information Panel */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <div className="flex">
          <Clock className="h-5 w-5 text-blue-600 mr-3 mt-0.5" />
          <div className="text-sm text-blue-800">
            <p className="font-semibold mb-1">Değişiklik Takibi Hakkında</p>
            <ul className="space-y-1 text-blue-700">
              <li>• Sistem, cihazlarda meydana gelen tüm önemli değişiklikleri otomatik olarak kaydeder</li>
              <li>• Donanım ekleme/çıkarma, yazılım kurulum/kaldırma işlemleri takip edilir</li>
              <li>• Değişiklikler hem API'ye gönderilir hem de yerel olarak saklanır</li>
              <li>• Real-time tespit süresi ortalama 5 dakikadır</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  )
}

export default ChangeLogs