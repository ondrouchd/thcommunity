import { useTranslation } from 'react-i18next'
import { useAuthStore } from '../stores/authStore'
import { supabase } from '../lib/supabase'
import { LogOut, Settings, User, TrendingUp } from 'lucide-react'
import { Link } from 'react-router-dom'

export function ProfilePage() {
  const { t } = useTranslation()
  const { profile, logout } = useAuthStore()

  const handleLogout = async () => {
    await supabase.auth.signOut()
    logout()
    window.location.href = '/'
  }

  return (
    <div className="p-4">
      {/* Profile header */}
      <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100 mb-6">
        <div className="flex items-center gap-4">
          <div className="w-20 h-20 bg-primary-100 rounded-full flex items-center justify-center">
            {profile?.avatar_url ? (
              <img
                src={profile.avatar_url}
                alt={profile.display_name}
                className="w-full h-full rounded-full object-cover"
              />
            ) : (
              <User className="w-10 h-10 text-primary-600" />
            )}
          </div>
          <div>
            <h1 className="text-xl font-bold text-gray-800">{profile?.display_name}</h1>
            <p className="text-gray-500">{profile?.email}</p>
            <div className="flex gap-2 mt-2">
              <span className="px-2 py-1 bg-primary-100 text-primary-700 rounded text-xs font-medium">
                {t(`team.roles.${profile?.role?.toLowerCase()}`)}
              </span>
              <span className="px-2 py-1 bg-gray-100 text-gray-700 rounded text-xs font-medium">
                {t(`team.positions.${profile?.position?.toLowerCase()}`)}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Menu items */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 divide-y divide-gray-100">
        <Link
          to="/profile/edit"
          className="flex items-center gap-4 p-4 hover:bg-gray-50 transition-colors"
        >
          <div className="w-10 h-10 bg-gray-100 rounded-lg flex items-center justify-center">
            <User className="w-5 h-5 text-gray-600" />
          </div>
          <span className="font-medium text-gray-800">{t('profile.editProfile')}</span>
        </Link>

        <Link
          to="/profile/statistics"
          className="flex items-center gap-4 p-4 hover:bg-gray-50 transition-colors"
        >
          <div className="w-10 h-10 bg-orange-100 rounded-lg flex items-center justify-center">
            <TrendingUp className="w-5 h-5 text-orange-600" />
          </div>
          <span className="font-medium text-gray-800">{t('profile.myStatistics')}</span>
        </Link>

        <Link
          to="/settings"
          className="flex items-center gap-4 p-4 hover:bg-gray-50 transition-colors"
        >
          <div className="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
            <Settings className="w-5 h-5 text-blue-600" />
          </div>
          <span className="font-medium text-gray-800">{t('settings.title')}</span>
        </Link>

        <button
          onClick={handleLogout}
          className="flex items-center gap-4 p-4 hover:bg-gray-50 transition-colors w-full"
        >
          <div className="w-10 h-10 bg-red-100 rounded-lg flex items-center justify-center">
            <LogOut className="w-5 h-5 text-red-600" />
          </div>
          <span className="font-medium text-red-600">{t('auth.logout')}</span>
        </button>
      </div>
    </div>
  )
}
