import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { ArrowLeft, CheckCircle2, Percent } from 'lucide-react'
import { usersApi } from '../lib/api'

interface UserStats {
  total_events_attended: number
  attendance_rate: number
  total_payments: number
}

export function ProfileStatisticsPage() {
  const { t } = useTranslation()
  const [stats, setStats] = useState<UserStats | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    usersApi
      .getMyStatistics()
      .then((data) => setStats(data as UserStats))
      .catch((err) => console.error('Error loading statistics:', err))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div className="p-4">
      <div className="flex items-center gap-4 mb-6">
        <Link to="/profile" className="p-2 hover:bg-gray-100 rounded-lg transition-colors">
          <ArrowLeft className="w-6 h-6 text-gray-600" />
        </Link>
        <h1 className="text-xl font-bold text-gray-800">{t('profile.myStatistics')}</h1>
      </div>

      {loading ? (
        <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100 text-center text-gray-500">
          {t('common.loading')}
        </div>
      ) : !stats ? (
        <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100 text-center text-gray-500">
          {t('common.noResults')}
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-4">
          <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
            <div className="w-10 h-10 bg-green-100 rounded-lg flex items-center justify-center mb-3">
              <CheckCircle2 className="w-5 h-5 text-green-600" />
            </div>
            <p className="text-2xl font-bold text-gray-800">{stats.total_events_attended}</p>
            <p className="text-sm text-gray-500">{t('statistics.attended')}</p>
          </div>

          <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
            <div className="w-10 h-10 bg-orange-100 rounded-lg flex items-center justify-center mb-3">
              <Percent className="w-5 h-5 text-orange-600" />
            </div>
            <p className="text-2xl font-bold text-gray-800">{Math.round(stats.attendance_rate)}%</p>
            <p className="text-sm text-gray-500">{t('statistics.attendanceRate')}</p>
          </div>
        </div>
      )}
    </div>
  )
}
