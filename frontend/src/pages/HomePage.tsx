import { useTranslation } from 'react-i18next'
import { useAuthStore } from '../stores/authStore'
import { Calendar, MessageCircle, Users, TrendingUp } from 'lucide-react'
import { Link } from 'react-router-dom'

export function HomePage() {
  const { t } = useTranslation()
  const { profile } = useAuthStore()

  const quickActions = [
    { to: '/events', icon: Calendar, label: t('navigation.events'), color: 'bg-blue-500' },
    { to: '/chat', icon: MessageCircle, label: t('navigation.chat'), color: 'bg-green-500' },
    { to: '/team', icon: Users, label: t('navigation.team'), color: 'bg-purple-500' },
    { to: '/profile', icon: TrendingUp, label: t('statistics.title'), color: 'bg-orange-500' },
  ]

  return (
    <div className="p-4">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-800">
          Ahoj, {profile?.display_name?.split(' ')[0] || 'hráči'}! 👋
        </h1>
        <p className="text-gray-500 mt-1">
          {profile?.team_id ? 'Co dnes podnikneme?' : t('team.noTeam')}
        </p>
      </div>

      {/* No team state */}
      {!profile?.team_id && (
        <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100 mb-6">
          <div className="text-center">
            <div className="w-16 h-16 bg-primary-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <Users className="w-8 h-8 text-primary-600" />
            </div>
            <h2 className="text-lg font-semibold text-gray-800 mb-2">
              {t('team.noTeam')}
            </h2>
            <p className="text-gray-500 mb-4">
              {t('team.joinOrCreate')}
            </p>
            <div className="flex gap-3 justify-center">
              <Link
                to="/team"
                className="px-4 py-2 bg-primary-600 text-white rounded-lg font-medium hover:bg-primary-700 transition-colors"
              >
                {t('team.joinTeam')}
              </Link>
              <Link
                to="/team"
                className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg font-medium hover:bg-gray-50 transition-colors"
              >
                {t('team.createTeam')}
              </Link>
            </div>
          </div>
        </div>
      )}

      {/* Quick actions */}
      <div className="grid grid-cols-2 gap-4 mb-6">
        {quickActions.map(({ to, icon: Icon, label, color }) => (
          <Link
            key={to}
            to={to}
            className="bg-white rounded-xl p-4 shadow-sm border border-gray-100 hover:shadow-md transition-shadow"
          >
            <div className={`w-12 h-12 ${color} rounded-lg flex items-center justify-center mb-3`}>
              <Icon className="w-6 h-6 text-white" />
            </div>
            <h3 className="font-medium text-gray-800">{label}</h3>
          </Link>
        ))}
      </div>

      {/* Upcoming event preview */}
      {profile?.team_id && (
        <div className="bg-white rounded-xl p-4 shadow-sm border border-gray-100">
          <div className="flex justify-between items-center mb-3">
            <h2 className="font-semibold text-gray-800">{t('events.upcoming')}</h2>
            <Link to="/events" className="text-primary-600 text-sm font-medium">
              {t('common.next')} →
            </Link>
          </div>
          <div className="text-center py-6 text-gray-500">
            {t('common.noResults')}
          </div>
        </div>
      )}
    </div>
  )
}
