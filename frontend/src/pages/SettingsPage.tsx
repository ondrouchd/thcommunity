import { useTranslation } from 'react-i18next'
import { ArrowLeft, Bell, Globe } from 'lucide-react'
import { Link } from 'react-router-dom'

export function SettingsPage() {
  const { t, i18n } = useTranslation()

  const toggleLanguage = () => {
    const newLang = i18n.language === 'cs' ? 'en' : 'cs'
    i18n.changeLanguage(newLang)
  }

  return (
    <div className="p-4">
      {/* Header */}
      <div className="flex items-center gap-4 mb-6">
        <Link
          to="/profile"
          className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
        >
          <ArrowLeft className="w-6 h-6 text-gray-600" />
        </Link>
        <h1 className="text-xl font-bold text-gray-800">{t('settings.title')}</h1>
      </div>

      {/* Settings sections */}
      <div className="space-y-4">
        {/* Notifications */}
        <div className="bg-white rounded-xl p-4 shadow-sm border border-gray-100">
          <div className="flex items-center gap-3 mb-4">
            <Bell className="w-5 h-5 text-gray-600" />
            <h2 className="font-semibold text-gray-800">{t('settings.notifications')}</h2>
          </div>

          <div className="space-y-3">
            <label className="flex items-center justify-between">
              <span className="text-gray-700">{t('settings.enableNotifications')}</span>
              <input type="checkbox" defaultChecked className="w-5 h-5 rounded text-primary-600" />
            </label>
            <label className="flex items-center justify-between">
              <span className="text-gray-700">{t('settings.eventCreated')}</span>
              <input type="checkbox" defaultChecked className="w-5 h-5 rounded text-primary-600" />
            </label>
            <label className="flex items-center justify-between">
              <span className="text-gray-700">{t('settings.eventReminder')}</span>
              <input type="checkbox" defaultChecked className="w-5 h-5 rounded text-primary-600" />
            </label>
            <label className="flex items-center justify-between">
              <span className="text-gray-700">{t('settings.spotOpened')}</span>
              <input type="checkbox" defaultChecked className="w-5 h-5 rounded text-primary-600" />
            </label>
          </div>
        </div>

        {/* Language */}
        <div className="bg-white rounded-xl p-4 shadow-sm border border-gray-100">
          <div className="flex items-center gap-3 mb-4">
            <Globe className="w-5 h-5 text-gray-600" />
            <h2 className="font-semibold text-gray-800">{t('settings.language')}</h2>
          </div>

          <div className="flex gap-2">
            <button
              onClick={() => i18n.changeLanguage('cs')}
              className={`flex-1 py-2 rounded-lg font-medium transition-colors ${
                i18n.language === 'cs'
                  ? 'bg-primary-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              🇨🇿 Čeština
            </button>
            <button
              onClick={() => i18n.changeLanguage('en')}
              className={`flex-1 py-2 rounded-lg font-medium transition-colors ${
                i18n.language === 'en'
                  ? 'bg-primary-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              🇬🇧 English
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
