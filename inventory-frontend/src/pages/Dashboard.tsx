import React from 'react'
import { Monitor, CheckCircle, Download, Wifi, TrendingUp } from 'lucide-react'
import { useStatistics } from '../hooks'

const StatCard: React.FC<{
  title: string
  value: number | string
  icon: React.ComponentType<any>
  color: string
  loading?: boolean
}> = ({ title, value, icon: Icon, color, loading }) => (
  <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
    <div className="flex items-center justify-between">
      <div>
        <p className="text-sm font-medium text-gray-600">{title}</p>
        <p className="text-3xl font-bold text-gray-900">
          {loading ? '...' : value}
        </p>
      </div>
      <div className={`p-3 rounded-lg ${color}`}>
        <Icon className="h-6 w-6 text-white" />
      </div>
    </div>
  </div>
)

const Dashboard: React.FC = () => {
  const { data: stats, isLoading } = useStatistics()

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-gray-600">Envanter yönetim sistemine genel bakış</p>
      </div>

      {/* Statistics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          title="Toplam Cihaz"
          value={stats?.totalDevices ?? 0}
          icon={Monitor}
          color="bg-blue-500"
          loading={isLoading}
        />
        <StatCard
          title="Aktif Cihazlar"
          value={stats?.activeDevices ?? 0}
          icon={CheckCircle}
          color="bg-green-500"
          loading={isLoading}
        />
        <StatCard
          title="Agent Kurulu"
          value={stats?.agentDevices ?? 0}
          icon={Download}
          color="bg-orange-500"
          loading={isLoading}
        />
        <StatCard
          title="Ağ Keşfi"
          value={stats?.networkDevices ?? 0}
          icon={Wifi}
          color="bg-purple-500"
          loading={isLoading}
        />
      </div>

      {/* Overview Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Quick Actions */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Hızlı İşlemler</h2>
          <div className="space-y-3">
            <a
              href="/devices"
              className="flex items-center p-3 rounded-lg border border-gray-200 hover:border-blue-300 hover:bg-blue-50 transition-colors duration-200"
            >
              <Monitor className="h-5 w-5 text-blue-600 mr-3" />
              <div>
                <p className="font-medium text-gray-900">Cihazları Görüntüle</p>
                <p className="text-sm text-gray-500">Tüm kayıtlı cihazları listele</p>
              </div>
            </a>
            <a
              href="/network-scan"
              className="flex items-center p-3 rounded-lg border border-gray-200 hover:border-green-300 hover:bg-green-50 transition-colors duration-200"
            >
              <Wifi className="h-5 w-5 text-green-600 mr-3" />
              <div>
                <p className="font-medium text-gray-900">Ağ Taraması Başlat</p>
                <p className="text-sm text-gray-500">Yeni cihazları keşfet</p>
              </div>
            </a>
            <a
              href="/change-logs"
              className="flex items-center p-3 rounded-lg border border-gray-200 hover:border-purple-300 hover:bg-purple-50 transition-colors duration-200"
            >
              <TrendingUp className="h-5 w-5 text-purple-600 mr-3" />
              <div>
                <p className="font-medium text-gray-900">Değişiklik Logları</p>
                <p className="text-sm text-gray-500">Sistem değişikliklerini incele</p>
              </div>
            </a>
          </div>
        </div>

        {/* System Information */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Sistem Bilgileri</h2>
          <div className="space-y-4">
            <div className="flex justify-between items-center py-2 border-b border-gray-100">
              <span className="text-gray-600">Sistem Adı:</span>
              <span className="font-medium text-gray-900">Envanter Yönetim Sistemi</span>
            </div>
            <div className="flex justify-between items-center py-2 border-b border-gray-100">
              <span className="text-gray-600">Versiyon:</span>
              <span className="font-medium text-gray-900">v2.0.0</span>
            </div>
            <div className="flex justify-between items-center py-2 border-b border-gray-100">
              <span className="text-gray-600">Platform:</span>
              <span className="font-medium text-gray-900">.NET 8.0</span>
            </div>
            <div className="flex justify-between items-center py-2">
              <span className="text-gray-600">Durum:</span>
              <div className="flex items-center">
                <div className="w-2 h-2 bg-green-500 rounded-full mr-2"></div>
                <span className="font-medium text-green-700">Çalışıyor</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Features Section */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Özellikler</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <div className="flex items-start space-x-3">
            <div className="w-2 h-2 bg-blue-500 rounded-full mt-2"></div>
            <div>
              <h3 className="font-medium text-gray-900">Cihaz Yönetimi</h3>
              <p className="text-sm text-gray-500">Donanım ve yazılım bilgilerinin otomatik toplama ve takibi</p>
            </div>
          </div>
          <div className="flex items-start space-x-3">
            <div className="w-2 h-2 bg-green-500 rounded-full mt-2"></div>
            <div>
              <h3 className="font-medium text-gray-900">Ağ Keşfi</h3>
              <p className="text-sm text-gray-500">Otomatik cihaz bulma ve kaydetme</p>
            </div>
          </div>
          <div className="flex items-start space-x-3">
            <div className="w-2 h-2 bg-purple-500 rounded-full mt-2"></div>
            <div>
              <h3 className="font-medium text-gray-900">Değişiklik Takibi</h3>
              <p className="text-sm text-gray-500">Sistem değişikliklerinin otomatik loglanması</p>
            </div>
          </div>
          <div className="flex items-start space-x-3">
            <div className="w-2 h-2 bg-orange-500 rounded-full mt-2"></div>
            <div>
              <h3 className="font-medium text-gray-900">Çoklu Platform</h3>
              <p className="text-sm text-gray-500">Windows ve Linux ortamlarında çalışma</p>
            </div>
          </div>
          <div className="flex items-start space-x-3">
            <div className="w-2 h-2 bg-red-500 rounded-full mt-2"></div>
            <div>
              <h3 className="font-medium text-gray-900">RESTful API</h3>
              <p className="text-sm text-gray-500">Swagger/OpenAPI dokümantasyonu ile gelişmiş API</p>
            </div>
          </div>
          <div className="flex items-start space-x-3">
            <div className="w-2 h-2 bg-indigo-500 rounded-full mt-2"></div>
            <div>
              <h3 className="font-medium text-gray-900">Docker Desteği</h3>
              <p className="text-sm text-gray-500">Konteyner tabanlı kolay deployment</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default Dashboard