import React from 'react'
import { Monitor, CheckCircle, Download, Wifi, TrendingUp, Sparkles, Zap, Heart, Star, Target } from 'lucide-react'
import { useStatistics } from '../hooks'

const StatCard: React.FC<{
  title: string
  value: number | string
  icon: React.ComponentType<any>
  gradient: string
  loading?: boolean
  delay?: number
}> = ({ title, value, icon: Icon, gradient, loading, delay = 0 }) => (
  <div 
    className="card group hover:scale-105 transform transition-all duration-500 relative overflow-hidden"
    style={{ animationDelay: `${delay}ms` }}
  >
    <div className={`absolute inset-0 bg-gradient-to-br ${gradient} opacity-5 group-hover:opacity-10 transition-opacity duration-300`}></div>
    <div className="relative p-8">
      <div className="flex items-center justify-between">
        <div className="space-y-2">
          <p className="text-sm font-semibold text-gray-600 uppercase tracking-wide">{title}</p>
          <p className="text-4xl font-bold text-gray-900 transition-all duration-500 group-hover:scale-110">
            {loading ? (
              <div className="flex items-center">
                <div className="animate-spin rounded-full h-8 w-8 border-3 border-gray-300 border-t-blue-600"></div>
              </div>
            ) : (
              <span className="bg-gradient-to-r from-gray-900 to-gray-700 bg-clip-text text-transparent">
                {value}
              </span>
            )}
          </p>
        </div>
        <div className={`p-4 rounded-2xl bg-gradient-to-br ${gradient} shadow-lg group-hover:scale-110 group-hover:rotate-6 transition-all duration-500`}>
          <Icon className="h-8 w-8 text-white" />
        </div>
      </div>
      <div className="mt-4 flex items-center text-sm">
        <TrendingUp className="h-4 w-4 text-green-500 mr-2" />
        <span className="text-green-600 font-medium">+12% bu ay</span>
      </div>
    </div>
  </div>
)

const Dashboard: React.FC = () => {
  const { data: stats, isLoading } = useStatistics()

  const features = [
    {
      title: 'Cihaz Yönetimi',
      description: 'Donanım ve yazılım bilgilerinin otomatik toplama ve takibi',
      icon: Monitor,
      gradient: 'from-blue-500 to-blue-600',
      bgGradient: 'from-blue-50 to-blue-100'
    },
    {
      title: 'Ağ Keşfi',
      description: 'Otomatik cihaz bulma ve kaydetme',
      icon: Wifi,
      gradient: 'from-green-500 to-green-600',
      bgGradient: 'from-green-50 to-green-100'
    },
    {
      title: 'Değişiklik Takibi',
      description: 'Sistem değişikliklerinin otomatik loglanması',
      icon: TrendingUp,
      gradient: 'from-purple-500 to-purple-600',
      bgGradient: 'from-purple-50 to-purple-100'
    },
    {
      title: 'Çoklu Platform',
      description: 'Windows ve Linux ortamlarında çalışma',
      icon: Target,
      gradient: 'from-orange-500 to-orange-600',
      bgGradient: 'from-orange-50 to-orange-100'
    },
    {
      title: 'RESTful API',
      description: 'Swagger/OpenAPI dokümantasyonu ile gelişmiş API',
      icon: Zap,
      gradient: 'from-red-500 to-red-600',
      bgGradient: 'from-red-50 to-red-100'
    },
    {
      title: 'Docker Desteği',
      description: 'Konteyner tabanlı kolay deployment',
      icon: Star,
      gradient: 'from-indigo-500 to-indigo-600',
      bgGradient: 'from-indigo-50 to-indigo-100'
    }
  ]

  const quickActions = [
    {
      title: 'Cihazları Görüntüle',
      description: 'Tüm kayıtlı cihazları listele',
      href: '/devices',
      icon: Monitor,
      gradient: 'from-blue-500 to-blue-600',
      bgGradient: 'from-blue-50 to-blue-100'
    },
    {
      title: 'Ağ Taraması Başlat',
      description: 'Yeni cihazları keşfet',
      href: '/network-scan',
      icon: Wifi,
      gradient: 'from-green-500 to-green-600',
      bgGradient: 'from-green-50 to-green-100'
    },
    {
      title: 'Değişiklik Logları',
      description: 'Sistem değişikliklerini incele',
      href: '/change-logs',
      icon: TrendingUp,
      gradient: 'from-purple-500 to-purple-600',
      bgGradient: 'from-purple-50 to-purple-100'
    }
  ]

  return (
    <div className="space-y-10">
      {/* Header */}
      <div className="relative overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-r from-blue-600/10 via-purple-600/10 to-indigo-600/10 rounded-3xl"></div>
        <div className="absolute inset-0 bg-gradient-to-br from-white to-white/40 rounded-3xl backdrop-blur-sm"></div>
        <div className="relative p-10">
          <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between">
            <div className="space-y-4">
              <div className="flex items-center space-x-4">
                <div className="p-3 bg-gradient-to-r from-blue-600 via-purple-600 to-indigo-600 rounded-2xl shadow-lg">
                  <Sparkles className="h-10 w-10 text-white" />
                </div>
                <div>
                  <h1 className="text-5xl font-bold bg-gradient-to-r from-gray-900 via-blue-800 to-purple-800 bg-clip-text text-transparent">
                    Dashboard
                  </h1>
                  <p className="text-xl text-gray-600 mt-2 font-medium">Envanter yönetim sistemine genel bakış</p>
                </div>
              </div>
            </div>
            <div className="mt-6 lg:mt-0">
              <div className="flex items-center space-x-4">
                <div className="flex items-center space-x-3 bg-gradient-to-r from-green-500/20 to-emerald-500/20 px-6 py-4 rounded-2xl backdrop-blur-sm shadow-lg">
                  <div className="flex items-center space-x-2">
                    <div className="w-3 h-3 bg-green-500 rounded-full animate-ping"></div>
                    <div className="w-3 h-3 bg-green-500 rounded-full"></div>
                  </div>
                  <span className="text-lg font-semibold text-green-700">Sistem Aktif</span>
                </div>
                <Heart className="h-6 w-6 text-red-500 animate-pulse" />
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Statistics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-8">
        <StatCard
          title="Toplam Cihaz"
          value={stats?.totalDevices ?? 0}
          icon={Monitor}
          gradient="from-blue-500 to-blue-600"
          loading={isLoading}
          delay={0}
        />
        <StatCard
          title="Aktif Cihazlar"
          value={stats?.activeDevices ?? 0}
          icon={CheckCircle}
          gradient="from-green-500 to-emerald-600"
          loading={isLoading}
          delay={100}
        />
        <StatCard
          title="Agent Kurulu"
          value={stats?.agentDevices ?? 0}
          icon={Download}
          gradient="from-orange-500 to-orange-600"
          loading={isLoading}
          delay={200}
        />
        <StatCard
          title="Ağ Keşfi"
          value={stats?.networkDevices ?? 0}
          icon={Wifi}
          gradient="from-purple-500 to-violet-600"
          loading={isLoading}
          delay={300}
        />
      </div>

      {/* Overview Section */}
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-10">
        {/* Quick Actions */}
        <div className="bg-white backdrop-blur-sm rounded-2xl shadow-lg border border-gray-200 hover:shadow-xl transition-all duration-300 group">
          <div className="p-8">
            <h2 className="text-2xl font-bold text-gray-900 mb-8 flex items-center">
              <div className="p-2 bg-gradient-to-r from-blue-500 to-purple-600 rounded-xl mr-4">
                <Zap className="h-6 w-6 text-white" />
              </div>
              Hızlı İşlemler
            </h2>
            <div className="space-y-4">
              {quickActions.map((action, index) => {
                const Icon = action.icon
                return (
                  <a
                    key={action.title}
                    href={action.href}
                    className={`flex items-center p-6 rounded-2xl border-2 border-gray-200/50 hover:border-transparent bg-gradient-to-r ${action.bgGradient} hover:shadow-xl transition-all duration-300 group transform hover:scale-105`}
                    style={{ animationDelay: `${index * 100}ms` }}
                  >
                    <div className={`p-3 bg-gradient-to-r ${action.gradient} rounded-xl shadow-lg group-hover:scale-110 transition-transform duration-300`}>
                      <Icon className="h-6 w-6 text-white" />
                    </div>
                    <div className="ml-6">
                      <p className="font-bold text-gray-900 text-lg group-hover:text-gray-800 transition-colors duration-300">
                        {action.title}
                      </p>
                      <p className="text-sm text-gray-600 mt-1">{action.description}</p>
                    </div>
                    <div className="ml-auto opacity-0 group-hover:opacity-100 transition-opacity duration-300">
                      <Sparkles className="h-5 w-5 text-gray-400" />
                    </div>
                  </a>
                )
              })}
            </div>
          </div>
        </div>

        {/* System Information */}
        <div className="bg-white backdrop-blur-sm rounded-2xl shadow-lg border border-gray-200 hover:shadow-xl transition-all duration-300">
          <div className="p-8">
            <h2 className="text-2xl font-bold text-gray-900 mb-8 flex items-center">
              <div className="p-2 bg-gradient-to-r from-gray-600 to-gray-700 rounded-xl mr-4">
                <Monitor className="h-6 w-6 text-white" />
              </div>
              Sistem Bilgileri
            </h2>
            <div className="space-y-6">
              <div className="flex justify-between items-center py-4 border-b border-gray-100">
                <span className="text-gray-600 font-medium">Sistem Adı:</span>
                <span className="font-bold text-gray-900 bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
                  Envanter Yönetim Sistemi
                </span>
              </div>
              <div className="flex justify-between items-center py-4 border-b border-gray-100">
                <span className="text-gray-600 font-medium">Versiyon:</span>
                <span className="font-bold text-blue-600 bg-blue-50 px-4 py-2 rounded-xl text-sm shadow-sm">v2.0.0</span>
              </div>
              <div className="flex justify-between items-center py-4 border-b border-gray-100">
                <span className="text-gray-600 font-medium">Platform:</span>
                <span className="font-bold text-purple-600 bg-purple-50 px-4 py-2 rounded-xl text-sm shadow-sm">.NET 8.0</span>
              </div>
              <div className="flex justify-between items-center py-4">
                <span className="text-gray-600 font-medium">Durum:</span>
                <div className="flex items-center">
                  <div className="w-3 h-3 bg-green-500 rounded-full mr-3 animate-pulse"></div>
                  <span className="font-bold text-green-700 bg-green-50 px-4 py-2 rounded-xl text-sm shadow-sm">Çalışıyor</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Features Section */}
      <div className="bg-white backdrop-blur-sm rounded-2xl shadow-lg border border-gray-200 hover:shadow-xl transition-all duration-300">
        <div className="p-8">
          <h2 className="text-2xl font-bold text-gray-900 mb-8 flex items-center">
            <div className="p-2 bg-gradient-to-r from-indigo-600 to-purple-600 rounded-xl mr-4">
              <Sparkles className="h-6 w-6 text-white" />
            </div>
            Özellikler
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {features.map((feature, index) => {
              const Icon = feature.icon
              return (
                <div 
                  key={feature.title}
                  className={`relative p-6 rounded-2xl bg-gradient-to-br ${feature.bgGradient} border border-gray-200/50 hover:shadow-xl transition-all duration-500 group transform hover:scale-105`}
                  style={{ animationDelay: `${index * 100}ms` }}
                >
                  <div className="flex items-start space-x-4">
                    <div className={`p-3 bg-gradient-to-r ${feature.gradient} rounded-xl shadow-lg group-hover:scale-110 group-hover:rotate-6 transition-all duration-500`}>
                      <Icon className="h-6 w-6 text-white" />
                    </div>
                    <div>
                      <h3 className="font-bold text-gray-900 text-lg group-hover:text-gray-800 transition-colors duration-300">
                        {feature.title}
                      </h3>
                      <p className="text-sm text-gray-600 mt-2 leading-relaxed">{feature.description}</p>
                    </div>
                  </div>
                  <div className="absolute top-4 right-4 opacity-0 group-hover:opacity-100 transition-opacity duration-300">
                    <Star className="h-4 w-4 text-yellow-500" />
                  </div>
                </div>
              )
            })}
          </div>
        </div>
      </div>
    </div>
  )
}

export default Dashboard