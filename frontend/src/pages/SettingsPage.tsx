import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { ArrowLeft, Bell, Globe } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useSettingsStore } from '../stores/settingsStore'
import { isPushSupported, getPushSubscription, enablePush, disablePush } from '../lib/push'

export function SettingsPage() {
  const { t, i18n } = useTranslation()
  const { notifications, setNotification } = useSettingsStore()

  const [pushSupported] = useState(isPushSupported())
  const [pushEnabled, setPushEnabled] = useState(false)
  const [pushBusy, setPushBusy] = useState(false)
  const [pushError, setPushError] = useState('')

  useEffect(() => {
    if (!pushSupported) return
    getPushSubscription()
      .then((sub) => setPushEnabled(!!sub))
      .catch(() => setPushEnabled(false))
  }, [pushSupported])

  const togglePush = async () => {
    setPushBusy(true)
    setPushError('')
    try {
      if (pushEnabled) {
        await disablePush()
        setPushEnabled(false)
      } else {
        await enablePush()
        setPushEnabled(true)
      }
    } catch (err) {
      const code = err instanceof Error ? err.message : 'generic'
      const map: Record<string, string> = {
        unsupported: t('settings.pushUnsupported'),
        denied: t('settings.pushDenied'),
        'not-configured': t('settings.pushNotConfigured'),
      }
      setPushError(map[code] ?? t('errors.generic'))
    } finally {
      setPushBusy(false)
    }
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
              <input
                type="checkbox"
                checked={pushEnabled}
                disabled={!pushSupported || pushBusy}
                onChange={togglePush}
                className="w-5 h-5 rounded text-primary-600 disabled:opacity-50"
              />
            </label>
            {!pushSupported && (
              <p className="text-xs text-gray-400">{t('settings.pushUnsupported')}</p>
            )}
            {pushError && <p className="text-xs text-red-500">{pushError}</p>}

            <label className="flex items-center justify-between">
              <span className="text-gray-700">{t('settings.eventCreated')}</span>
              <input
                type="checkbox"
                checked={notifications.eventCreated}
                onChange={(e) => setNotification('eventCreated', e.target.checked)}
                className="w-5 h-5 rounded text-primary-600"
              />
            </label>
            <label className="flex items-center justify-between">
              <span className="text-gray-700">{t('settings.eventReminder')}</span>
              <input
                type="checkbox"
                checked={notifications.eventReminder}
                onChange={(e) => setNotification('eventReminder', e.target.checked)}
                className="w-5 h-5 rounded text-primary-600"
              />
            </label>
            <label className="flex items-center justify-between">
              <span className="text-gray-700">{t('settings.spotOpened')}</span>
              <input
                type="checkbox"
                checked={notifications.spotOpened}
                onChange={(e) => setNotification('spotOpened', e.target.checked)}
                className="w-5 h-5 rounded text-primary-600"
              />
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
