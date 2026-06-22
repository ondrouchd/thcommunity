import { useTranslation } from 'react-i18next'
import { Plus } from 'lucide-react'

export function EventsPage() {
  const { t } = useTranslation()

  return (
    <div className="p-4">
      {/* Header */}
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold text-gray-800">{t('events.title')}</h1>
        <button className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg font-medium hover:bg-primary-700 transition-colors">
          <Plus className="w-5 h-5" />
          <span className="hidden sm:inline">{t('events.createEvent')}</span>
        </button>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 mb-6">
        <button className="px-4 py-2 bg-primary-600 text-white rounded-lg font-medium">
          {t('events.upcoming')}
        </button>
        <button className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg font-medium hover:bg-gray-200 transition-colors">
          {t('events.past')}
        </button>
      </div>

      {/* Events list */}
      <div className="space-y-4">
        <div className="text-center text-gray-500 py-8">
          {t('common.noResults')}
        </div>
      </div>
    </div>
  )
}
