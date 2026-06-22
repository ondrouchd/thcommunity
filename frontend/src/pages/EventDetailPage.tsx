import { useTranslation } from 'react-i18next'
import { useParams, Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'

export function EventDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()

  return (
    <div className="p-4">
      {/* Header */}
      <div className="flex items-center gap-4 mb-6">
        <Link
          to="/events"
          className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
        >
          <ArrowLeft className="w-6 h-6 text-gray-600" />
        </Link>
        <h1 className="text-xl font-bold text-gray-800">{t('events.eventDetails')}</h1>
      </div>

      {/* Event content placeholder */}
      <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
        <p className="text-gray-500 text-center">
          Event ID: {id}
        </p>
      </div>
    </div>
  )
}
