import React, { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { 
  Monitor, 
  Wifi, 
  Clock, 
  BarChart3, 
  Menu, 
  X,
  Laptop,
  ExternalLink,
  Play,
  Database,
  Sparkles,
  Zap
} from 'lucide-react'
import { formatDate } from '../utils'
import { toggleDemoMode, isDemoMode } from '../api'

interface LayoutProps {
  children: React.ReactNode
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [demoMode, setDemoMode] = useState(isDemoMode())
  const location = useLocation()

  const handleDemoToggle = () => {
    const newDemoMode = toggleDemoMode()
    setDemoMode(newDemoMode)
    // Reload the page to refresh data
    window.location.reload()
  }

  const navigation = [
    { name: 'Dashboard', href: '/', icon: BarChart3, color: 'from-blue-500 to-indigo-600' },
    { name: 'Cihazlar', href: '/devices', icon: Monitor, color: 'from-green-500 to-emerald-600' },
    { name: 'Ağ Taraması', href: '/network-scan', icon: Wifi, color: 'from-purple-500 to-violet-600' },
    { name: 'Değişiklik Logları', href: '/change-logs', icon: Clock, color: 'from-orange-500 to-amber-600' },
  ]

  const isActive = (href: string) => {
    if (href === '/') {
      return location.pathname === '/'
    }
    return location.pathname.startsWith(href)
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50 to-indigo-50">
      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div 
            className="fixed inset-0 bg-black/50 backdrop-blur-sm"
            onClick={() => setSidebarOpen(false)}
          />
        </div>
      )}

      {/* Sidebar */}
      <div className={`
        fixed inset-y-0 left-0 z-50 w-72 glass rounded-r-3xl shadow-2xl transform transition-transform duration-300 ease-in-out lg:translate-x-0 lg:static lg:inset-0
        ${sidebarOpen ? 'translate-x-0' : '-translate-x-full'}
      `}>
        <div className="flex items-center justify-between h-20 px-6 border-b border-white/20">
          <div className="flex items-center space-x-4">
            <div className="relative">
              <div className="p-3 bg-gradient-to-r from-blue-600 via-purple-600 to-indigo-600 rounded-2xl shadow-lg">
                <Laptop className="h-7 w-7 text-white" />
              </div>
              <div className="absolute -top-1 -right-1 w-4 h-4 bg-gradient-to-r from-green-400 to-green-500 rounded-full animate-pulse shadow-glow"></div>
            </div>
            <div>
              <h1 className="text-xl font-bold bg-gradient-to-r from-gray-800 to-gray-600 bg-clip-text text-transparent">Envanter</h1>
              <p className="text-sm text-gray-500 -mt-1 font-medium">Yönetim Sistemi</p>
            </div>
          </div>
          <button
            onClick={() => setSidebarOpen(false)}
            className="lg:hidden p-2 rounded-xl text-gray-400 hover:text-gray-600 hover:bg-white/20 transition-all duration-200"
          >
            <X className="h-6 w-6" />
          </button>
        </div>

        <nav className="flex-1 px-4 py-8 space-y-3">
          {navigation.map((item, index) => {
            const Icon = item.icon
            const active = isActive(item.href)
            
            return (
              <Link
                key={item.name}
                to={item.href}
                onClick={() => setSidebarOpen(false)}
                className={`
                  flex items-center px-5 py-4 text-sm font-semibold rounded-2xl transition-all duration-300 group relative overflow-hidden
                  ${active
                    ? `bg-gradient-to-r ${item.color} text-white shadow-xl transform scale-105 shadow-glow`
                    : 'text-gray-700 hover:bg-white/30 hover:text-gray-900 hover:scale-105 hover:shadow-lg'
                  }
                `}
                style={{ animationDelay: `${index * 100}ms` }}
              >
                {active && (
                  <div className="absolute inset-0 bg-gradient-to-r from-white/20 to-white/0 rounded-2xl"></div>
                )}
                <div className={`p-2 rounded-xl mr-4 transition-all duration-300 ${
                  active ? 'bg-white/20' : 'bg-gray-100 group-hover:bg-white/50'
                }`}>
                  <Icon className={`h-5 w-5 transition-all duration-300 ${
                    active ? 'text-white' : 'text-gray-500 group-hover:text-gray-700'
                  }`} />
                </div>
                <span className="relative z-10">{item.name}</span>
                {active && (
                  <div className="ml-auto flex items-center space-x-2">
                    <Sparkles className="h-4 w-4 text-white/80 animate-spin-slow" />
                    <div className="w-2 h-2 bg-white rounded-full animate-pulse"></div>
                  </div>
                )}
              </Link>
            )
          })}
        </nav>

        <div className="px-4 py-6 border-t border-white/20 space-y-3">
          <button
            onClick={handleDemoToggle}
            className={`w-full flex items-center px-5 py-4 text-sm font-semibold rounded-2xl transition-all duration-300 group relative overflow-hidden ${
              demoMode 
                ? 'bg-gradient-to-r from-green-500 to-emerald-600 text-white shadow-xl shadow-green-500/30' 
                : 'text-gray-700 hover:bg-white/30 border-2 border-gray-200/50 hover:border-green-300'
            }`}
          >
            {demoMode && (
              <div className="absolute inset-0 bg-gradient-to-r from-white/20 to-white/0 rounded-2xl"></div>
            )}
            <div className={`p-2 rounded-xl mr-4 transition-all duration-300 ${
              demoMode 
                ? 'bg-white/20' 
                : 'bg-green-100 group-hover:bg-green-200'
            }`}>
              {demoMode ? (
                <Database className={`h-5 w-5 ${demoMode ? 'text-white' : 'text-green-600'}`} />
              ) : (
                <Play className="h-5 w-5 text-green-600" />
              )}
            </div>
            <div className="flex-1 text-left relative z-10">
              <div className="font-semibold">
                {demoMode ? 'Demo Modu Aktif' : 'Demo Verilerini Göster'}
              </div>
              <div className={`text-xs ${demoMode ? 'text-white/80' : 'text-gray-500'}`}>
                {demoMode ? 'Örnek veriler görüntüleniyor' : 'Örnek verilerle test edin'}
              </div>
            </div>
            {demoMode && <Zap className="h-4 w-4 text-white/80 animate-bounce" />}
          </button>
          
          <a
            href="/swagger"
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center px-5 py-4 text-sm font-semibold text-gray-700 rounded-2xl hover:bg-white/30 hover:text-gray-900 transition-all duration-300 group border-2 border-gray-200/50 hover:border-blue-300"
          >
            <div className="p-2 bg-blue-100 rounded-xl mr-4 group-hover:bg-blue-200 transition-all duration-300">
              <ExternalLink className="h-5 w-5 text-blue-600 transition-all duration-300" />
            </div>
            <span>API Dökümanları</span>
          </a>
        </div>
      </div>

      {/* Main content */}
      <div className="lg:pl-72">
        {/* Top bar */}
        <div className="flex items-center justify-between h-20 px-6 glass border-b border-white/20 shadow-lg">
          <button
            onClick={() => setSidebarOpen(true)}
            className="lg:hidden p-3 rounded-2xl text-gray-500 hover:text-gray-700 hover:bg-white/30 transition-all duration-200 shadow-lg"
          >
            <Menu className="h-6 w-6" />
          </button>
          
          <div className="flex-1" />
          
          <div className="flex items-center space-x-6">
            <div className="text-sm text-gray-600 bg-white/50 px-4 py-2 rounded-2xl font-medium shadow-lg backdrop-blur-sm">
              Son güncelleme: {formatDate(new Date().toISOString())}
            </div>
            <div className="flex items-center space-x-3 bg-gradient-to-r from-green-500/20 to-emerald-500/20 px-4 py-2 rounded-2xl backdrop-blur-sm">
              <div className="flex items-center space-x-2">
                <div className="w-2 h-2 bg-green-500 rounded-full animate-ping"></div>
                <div className="w-2 h-2 bg-green-500 rounded-full"></div>
              </div>
              <span className="text-sm font-medium text-green-700">Sistem Aktif</span>
            </div>
          </div>
        </div>

        {/* Page content */}
        <main className="flex-1 p-8">
          <div className="max-w-7xl mx-auto">
            {children}
          </div>
        </main>
      </div>
    </div>
  )
}

export default Layout