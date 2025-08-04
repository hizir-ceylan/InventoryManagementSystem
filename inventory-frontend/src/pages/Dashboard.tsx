import React from 'react'
import { Monitor, CheckCircle, Download, Wifi, TrendingUp, Sparkles } from 'lucide-react'
import { useStatistics } from '../hooks'

const StatCard: React.FC<{
  title: string
  value: number | string
  icon: React.ComponentType<any>
  color: string
  loading?: boolean
}> = ({ title, value, icon: Icon, color, loading }) => (
  <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-all duration-300 group">
    <div className="flex items-center justify-between">
      <div>
        <p className="text-sm font-medium text-gray-600">{title}</p>
        <p className="text-3xl font-bold text-gray-900 transition-all duration-300 group-hover:scale-105">
          {loading ? (
            <div className="flex items-center">
              <div className="animate-spin rounded-full h-6 w-6 border-2 border-gray-300 border-t-blue-600"></div>
            </div>
          ) : value}
        </p>
      </div>
      <div className={`p-3 rounded-xl ${color} group-hover:scale-110 transition-transform duration-300`}>
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
      <div className="relative">
        <div className="absolute inset-0 bg-gradient-to-r from-blue-50 to-indigo-50 rounded-2xl"></div>
        <div className="relative p-8">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-4xl font-bold text-gray-900 flex items-center">
                <Sparkles className="h-8 w-8 text-blue-600 mr-3" />
                Dashboard
              </h1>
              <p className="text-gray-600 mt-2">Envanter yönetim sistemine genel bakış</p>
            </div>
            <div className="hidden lg:block">
              <div className="flex items-center space-x-2 bg-white px-4 py-2 rounded-full shadow-sm">
                <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
                <span className="text-sm text-gray-600">Sistem Aktif</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Statistics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          title="Toplam Cihaz"
          value={stats?.totalDevices ?? 0}
          icon={Monitor}
          color="bg-gradient-to-r from-blue-500 to-blue-600"
          loading={isLoading}
        />
        <StatCard
          title="Aktif Cihazlar"
          value={stats?.activeDevices ?? 0}
          icon={CheckCircle}
          color="bg-gradient-to-r from-green-500 to-green-600"
          loading={isLoading}
        />
        <StatCard
          title="Agent Kurulu"
          value={stats?.agentDevices ?? 0}
          icon={Download}
          color="bg-gradient-to-r from-orange-500 to-orange-600"
          loading={isLoading}
        />
        <StatCard
          title="Ağ Keşfi"
          value={stats?.networkDevices ?? 0}
          icon={Wifi}
          color="bg-gradient-to-r from-purple-500 to-purple-600"
          loading={isLoading}
        />
      </div>

      {/* Overview Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Quick Actions */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow duration-300">
          <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
            <TrendingUp className="h-5 w-5 text-blue-600 mr-2" />
            Hızlı İşlemler
          </h2>
          <div className="space-y-3">
            <a
              href="/devices"
              className="flex items-center p-4 rounded-xl border border-gray-200 hover:border-blue-300 hover:bg-blue-50 transition-all duration-300 group"
            >
              <div className="p-2 bg-blue-100 rounded-lg group-hover:bg-blue-200 transition-colors duration-300">
                <Monitor className="h-5 w-5 text-blue-600" />
              </div>
              <div className="ml-4">
                <p className="font-medium text-gray-900 group-hover:text-blue-700 transition-colors duration-300">
                  Cihazları Görüntüle
                </p>
                <p className="text-sm text-gray-500">Tüm kayıtlı cihazları listele</p>
              </div>
            </a>
            <a
              href="/network-scan"
              className="flex items-center p-4 rounded-xl border border-gray-200 hover:border-green-300 hover:bg-green-50 transition-all duration-300 group"
            >
              <div className="p-2 bg-green-100 rounded-lg group-hover:bg-green-200 transition-colors duration-300">
                <Wifi className="h-5 w-5 text-green-600" />
              </div>
              <div className="ml-4">
                <p className="font-medium text-gray-900 group-hover:text-green-700 transition-colors duration-300">
                  Ağ Taraması Başlat
                </p>
                <p className="text-sm text-gray-500">Yeni cihazları keşfet</p>
              </div>
            </a>
            <a
              href="/change-logs"
              className="flex items-center p-4 rounded-xl border border-gray-200 hover:border-purple-300 hover:bg-purple-50 transition-all duration-300 group"
            >
              <div className="p-2 bg-purple-100 rounded-lg group-hover:bg-purple-200 transition-colors duration-300">
                <TrendingUp className="h-5 w-5 text-purple-600" />
              </div>
              <div className="ml-4">
                <p className="font-medium text-gray-900 group-hover:text-purple-700 transition-colors duration-300">
                  Değişiklik Logları
                </p>
                <p className="text-sm text-gray-500">Sistem değişikliklerini incele</p>
              </div>
            </a>
          </div>
        </div>

        {/* System Information */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow duration-300">
          <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
            <Monitor className="h-5 w-5 text-gray-600 mr-2" />
            Sistem Bilgileri
          </h2>
          <div className="space-y-4">
            <div className="flex justify-between items-center py-3 border-b border-gray-100">
              <span className="text-gray-600">Sistem Adı:</span>
              <span className="font-medium text-gray-900">Envanter Yönetim Sistemi</span>
            </div>
            <div className="flex justify-between items-center py-3 border-b border-gray-100">
              <span className="text-gray-600">Versiyon:</span>
              <span className="font-medium text-gray-900 bg-blue-50 px-2 py-1 rounded text-sm">v2.0.0</span>
            </div>
            <div className="flex justify-between items-center py-3 border-b border-gray-100">
              <span className="text-gray-600">Platform:</span>
              <span className="font-medium text-gray-900 bg-purple-50 px-2 py-1 rounded text-sm">.NET 8.0</span>
            </div>
            <div className="flex justify-between items-center py-3">
              <span className="text-gray-600">Durum:</span>
              <div className="flex items-center">
                <div className="w-2 h-2 bg-green-500 rounded-full mr-2 animate-pulse"></div>
                <span className="font-medium text-green-700 bg-green-50 px-2 py-1 rounded text-sm">Çalışıyor</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Features Section */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow duration-300">
        <h2 className="text-lg font-semibold text-gray-900 mb-6 flex items-center">
          <Sparkles className="h-5 w-5 text-indigo-600 mr-2" />
          Özellikler
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <div className="flex items-start space-x-3 p-4 rounded-xl bg-blue-50 hover:bg-blue-100 transition-colors duration-300">
            <div className="w-3 h-3 bg-blue-500 rounded-full mt-2 animate-pulse"></div>
            <div>
              <h3 className="font-medium text-gray-900">Cihaz Yönetimi</h3>
              <p className="text-sm text-gray-600 mt-1">Donanım ve yazılım bilgilerinin otomatik toplama ve takibi</p>
            </div>
          </div>
          <div className="flex items-start space-x-3 p-4 rounded-xl bg-green-50 hover:bg-green-100 transition-colors duration-300">
            <div className="w-3 h-3 bg-green-500 rounded-full mt-2 animate-pulse"></div>
            <div>
              <h3 className="font-medium text-gray-900">Ağ Keşfi</h3>
              <p className="text-sm text-gray-600 mt-1">Otomatik cihaz bulma ve kaydetme</p>
            </div>
          </div>
          <div className="flex items-start space-x-3 p-4 rounded-xl bg-purple-50 hover:bg-purple-100 transition-colors duration-300">
            <div className="w-3 h-3 bg-purple-500 rounded-full mt-2 animate-pulse"></div>
            <div>
              <h3 className="font-medium text-gray-900">Değişiklik Takibi</h3>
              <p className="text-sm text-gray-600 mt-1">Sistem değişikliklerinin otomatik loglanması</p>
            </div>
          </div>
          <div className="flex items-start space-x-3 p-4 rounded-xl bg-orange-50 hover:bg-orange-100 transition-colors duration-300">
            <div className="w-3 h-3 bg-orange-500 rounded-full mt-2 animate-pulse"></div>
            <div>
              <h3 className="font-medium text-gray-900">Çoklu Platform</h3>
              <p className="text-sm text-gray-600 mt-1">Windows ve Linux ortamlarında çalışma</p>
            </div>
          </div>
          <div className="flex items-start space-x-3 p-4 rounded-xl bg-red-50 hover:bg-red-100 transition-colors duration-300">
            <div className="w-3 h-3 bg-red-500 rounded-full mt-2 animate-pulse"></div>
            <div>
              <h3 className="font-medium text-gray-900">RESTful API</h3>
              <p className="text-sm text-gray-600 mt-1">Swagger/OpenAPI dokümantasyonu ile gelişmiş API</p>
            </div>
          </div>
          <div className="flex items-start space-x-3 p-4 rounded-xl bg-indigo-50 hover:bg-indigo-100 transition-colors duration-300">
            <div className="w-3 h-3 bg-indigo-500 rounded-full mt-2 animate-pulse"></div>
            <div>
              <h3 className="font-medium text-gray-900">Docker Desteği</h3>
              <p className="text-sm text-gray-600 mt-1">Konteyner tabanlı kolay deployment</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default Dashboard