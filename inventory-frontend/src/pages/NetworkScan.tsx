import React, { useState } from 'react'
import { Play, RefreshCw, Network, AlertCircle } from 'lucide-react'
import { useStartNetworkScan, useNetworkScanResults } from '../hooks'

const NetworkScan: React.FC = () => {
  const [networkRange, setNetworkRange] = useState('192.168.1.0/24')
  const [isScanning, setIsScanning] = useState(false)
  
  const startScanMutation = useStartNetworkScan()
  const { data: scanResults, isLoading: loadingResults, refetch } = useNetworkScanResults()

  const handleStartScan = async () => {
    if (!networkRange.trim()) return
    
    setIsScanning(true)
    try {
      await startScanMutation.mutateAsync({ networkRange })
      // Refresh results after scan
      setTimeout(() => {
        refetch()
        setIsScanning(false)
      }, 2000)
    } catch (error) {
      console.error('Scan failed:', error)
      setIsScanning(false)
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Ağ Taraması</h1>
        <p className="text-gray-600">Ağınızdaki cihazları otomatik olarak keşfedin</p>
      </div>

      {/* Scan Configuration */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Tarama Ayarları</h2>
        <div className="space-y-4">
          <div>
            <label htmlFor="networkRange" className="block text-sm font-medium text-gray-700 mb-2">
              Ağ Aralığı
            </label>
            <div className="flex space-x-3">
              <input
                type="text"
                id="networkRange"
                value={networkRange}
                onChange={(e) => setNetworkRange(e.target.value)}
                placeholder="192.168.1.0/24"
                className="flex-1 border border-gray-300 rounded-md px-3 py-2 focus:ring-blue-500 focus:border-blue-500"
              />
              <button
                onClick={handleStartScan}
                disabled={isScanning || !networkRange.trim()}
                className="inline-flex items-center px-6 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isScanning ? (
                  <>
                    <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                    Taranıyor...
                  </>
                ) : (
                  <>
                    <Play className="h-4 w-4 mr-2" />
                    Taramayı Başlat
                  </>
                )}
              </button>
            </div>
            <p className="mt-2 text-sm text-gray-500">
              Örnek: 192.168.1.0/24, 10.0.0.0/8, 172.16.0.0/16
            </p>
          </div>
        </div>
      </div>

      {/* Scan Information */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <div className="flex">
          <AlertCircle className="h-5 w-5 text-blue-600 mr-3 mt-0.5" />
          <div className="text-sm text-blue-800">
            <p className="font-semibold mb-1">Ağ Taraması Hakkında</p>
            <ul className="space-y-1 text-blue-700">
              <li>• Ağ taraması, belirtilen IP aralığındaki aktif cihazları keşfeder</li>
              <li>• Ping ve port tarama yöntemleri kullanılır</li>
              <li>• Bulunan cihazlar otomatik olarak sisteme eklenir</li>
              <li>• Tarama süresi ağ boyutuna göre değişiklik gösterebilir</li>
            </ul>
          </div>
        </div>
      </div>

      {/* Scan Results */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-gray-900">Tarama Sonuçları</h2>
          <button
            onClick={() => refetch()}
            className="inline-flex items-center px-3 py-1 text-sm border border-gray-300 rounded-md text-gray-700 bg-white hover:bg-gray-50"
          >
            <RefreshCw className="h-4 w-4 mr-1" />
            Yenile
          </button>
        </div>

        <div className="p-6">
          {loadingResults ? (
            <div className="text-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
              <p className="mt-2 text-gray-600">Sonuçlar yükleniyor...</p>
            </div>
          ) : scanResults && scanResults.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      IP Adresi
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Cihaz Adı
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Durum
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Keşfedilme Zamanı
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {scanResults.map((result, index) => (
                    <tr key={index} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                        {result.ipAddress}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {result.deviceName || 'Bilinmiyor'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                          result.status === 'Active' 
                            ? 'bg-green-100 text-green-800' 
                            : 'bg-red-100 text-red-800'
                        }`}>
                          {result.status === 'Active' ? 'Aktif' : 'Pasif'}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {new Date(result.discoveredAt).toLocaleString('tr-TR')}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="text-center py-8">
              <Network className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <p className="text-gray-600">
                {isScanning ? 'Tarama devam ediyor...' : 'Henüz tarama sonucu bulunmuyor.'}
              </p>
              <p className="text-gray-500 text-sm mt-1">
                Ağ keşfi için yukarıdaki tarama butonunu kullanın.
              </p>
            </div>
          )}
        </div>
      </div>

      {/* Quick Network Ranges */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Sık Kullanılan Ağ Aralıkları</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {[
            { range: '192.168.1.0/24', description: 'Ev/Küçük ofis ağı (192.168.1.1-254)' },
            { range: '192.168.0.0/24', description: 'Genel ev ağı (192.168.0.1-254)' },
            { range: '10.0.0.0/24', description: 'Kurumsal ağ (10.0.0.1-254)' },
          ].map((item) => (
            <button
              key={item.range}
              onClick={() => setNetworkRange(item.range)}
              className="text-left p-4 border border-gray-200 rounded-lg hover:border-blue-300 hover:bg-blue-50 transition-colors"
            >
              <div className="font-medium text-gray-900">{item.range}</div>
              <div className="text-sm text-gray-500 mt-1">{item.description}</div>
            </button>
          ))}
        </div>
      </div>
    </div>
  )
}

export default NetworkScan