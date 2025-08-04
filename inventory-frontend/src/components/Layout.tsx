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
  Database
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
    { name: 'Dashboard', href: '/', icon: BarChart3 },
    { name: 'Cihazlar', href: '/devices', icon: Monitor },
    { name: 'Ağ Taraması', href: '/network-scan', icon: Wifi },
    { name: 'Değişiklik Logları', href: '/change-logs', icon: Clock },
  ]

  const isActive = (href: string) => {
    if (href === '/') {
      return location.pathname === '/'
    }
    return location.pathname.startsWith(href)
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div 
            className="fixed inset-0 bg-gray-600 bg-opacity-75"
            onClick={() => setSidebarOpen(false)}
          />
        </div>
      )}

      {/* Sidebar */}
      <div className={`
        fixed inset-y-0 left-0 z-50 w-64 bg-white shadow-lg transform transition-transform duration-300 ease-in-out lg:translate-x-0 lg:static lg:inset-0
        ${sidebarOpen ? 'translate-x-0' : '-translate-x-full'}
      `}>
        <div className="flex items-center justify-between h-16 px-6 border-b border-gray-200">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-gradient-to-r from-blue-600 to-indigo-600 rounded-xl">
              <Laptop className="h-6 w-6 text-white" />
            </div>
            <div>
              <h1 className="text-lg font-bold text-gray-900">Envanter</h1>
              <p className="text-xs text-gray-500 -mt-1">Yönetim Sistemi</p>
            </div>
          </div>
          <button
            onClick={() => setSidebarOpen(false)}
            className="lg:hidden p-2 rounded-xl text-gray-400 hover:text-gray-500 hover:bg-gray-100 transition-all duration-200"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <nav className="flex-1 px-4 py-6 space-y-2">
          {navigation.map((item) => {
            const Icon = item.icon
            const active = isActive(item.href)
            
            return (
              <Link
                key={item.name}
                to={item.href}
                onClick={() => setSidebarOpen(false)}
                className={`
                  flex items-center px-4 py-3 text-sm font-medium rounded-xl transition-all duration-200 group
                  ${active
                    ? 'bg-gradient-to-r from-blue-500 to-indigo-600 text-white shadow-lg transform scale-[1.02]'
                    : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900 hover:scale-[1.01]'
                  }
                `}
              >
                <Icon className={`mr-3 h-5 w-5 transition-all duration-200 ${
                  active ? 'text-white' : 'text-gray-400 group-hover:text-gray-600'
                }`} />
                {item.name}
                {active && (
                  <div className="ml-auto w-2 h-2 bg-white rounded-full animate-pulse"></div>
                )}
              </Link>
            )
          })}
        </nav>

        <div className="px-4 py-4 border-t border-gray-200">
          <button
            onClick={handleDemoToggle}
            className={`w-full flex items-center px-4 py-3 text-sm font-medium rounded-xl transition-all duration-200 group mb-3 ${
              demoMode 
                ? 'bg-gradient-to-r from-green-500 to-green-600 text-white shadow-lg' 
                : 'text-gray-600 hover:bg-green-50 hover:text-green-700 border border-gray-200'
            }`}
          >
            <div className={`p-1 rounded-lg mr-3 transition-colors duration-200 ${
              demoMode 
                ? 'bg-white bg-opacity-20' 
                : 'bg-green-100 group-hover:bg-green-200'
            }`}>
              {demoMode ? (
                <Database className={`h-4 w-4 ${demoMode ? 'text-white' : 'text-green-600'}`} />
              ) : (
                <Play className="h-4 w-4 text-green-600" />
              )}
            </div>
            <div className="flex-1 text-left">
              <div className="font-medium">
                {demoMode ? 'Demo Modu Aktif' : 'Demo Verilerini Göster'}
              </div>
              <div className={`text-xs ${demoMode ? 'text-white text-opacity-80' : 'text-gray-500'}`}>
                {demoMode ? 'Örnek veriler görüntüleniyor' : 'Örnek verilerle test edin'}
              </div>
            </div>
          </button>
          
          <a
            href="/swagger"
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center px-4 py-3 text-sm font-medium text-gray-600 rounded-xl hover:bg-gray-50 hover:text-gray-900 transition-all duration-200 group"
          >
            <div className="p-1 bg-gray-100 rounded-lg mr-3 group-hover:bg-blue-100 transition-colors duration-200">
              <ExternalLink className="h-4 w-4 text-gray-400 group-hover:text-blue-600 transition-colors duration-200" />
            </div>
            API Dökümanları
          </a>
        </div>
      </div>

      {/* Main content */}
      <div className="lg:pl-64">
        {/* Top bar */}
        <div className="flex items-center justify-between h-16 px-6 bg-white border-b border-gray-200 shadow-sm">
          <button
            onClick={() => setSidebarOpen(true)}
            className="lg:hidden p-2 rounded-xl text-gray-400 hover:text-gray-500 hover:bg-gray-100 transition-all duration-200"
          >
            <Menu className="h-5 w-5" />
          </button>
          
          <div className="flex-1" />
          
          <div className="flex items-center space-x-4">
            <div className="text-sm text-gray-500 bg-gray-50 px-3 py-2 rounded-lg">
              Son güncelleme: {formatDate(new Date().toISOString())}
            </div>
            <div className="flex items-center space-x-2">
              <div className="w-2 h-2 bg-green-500 rounded-full animate-ping"></div>
              <div className="w-2 h-2 bg-green-500 rounded-full"></div>
            </div>
          </div>
        </div>

        {/* Page content */}
        <main className="flex-1 p-6">
          {children}
        </main>
      </div>
    </div>
  )
}

export default Layout