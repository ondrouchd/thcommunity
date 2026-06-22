import { Outlet, NavLink } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Home, MessageCircle, Calendar, Users, User } from 'lucide-react'

export function Layout() {
  const { t } = useTranslation()

  const navItems = [
    { to: '/', icon: Home, label: t('navigation.home') },
    { to: '/chat', icon: MessageCircle, label: t('navigation.chat') },
    { to: '/events', icon: Calendar, label: t('navigation.events') },
    { to: '/team', icon: Users, label: t('navigation.team') },
    { to: '/profile', icon: User, label: t('navigation.profile') },
  ]

  return (
    <div className="flex flex-col min-h-screen bg-gray-50">
      {/* Main content */}
      <main className="flex-1 pb-16 safe-area-top">
        <Outlet />
      </main>

      {/* Bottom navigation */}
      <nav className="fixed bottom-0 left-0 right-0 bg-white border-t border-gray-200 safe-area-bottom">
        <div className="flex justify-around items-center h-16">
          {navItems.map(({ to, icon: Icon, label }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                `flex flex-col items-center justify-center w-full h-full text-xs transition-colors ${
                  isActive
                    ? 'text-primary-600'
                    : 'text-gray-500 hover:text-gray-700'
                }`
              }
            >
              <Icon className="w-6 h-6 mb-1" />
              <span>{label}</span>
            </NavLink>
          ))}
        </div>
      </nav>
    </div>
  )
}
