import { useTranslation } from 'react-i18next'

export function ChatPage() {
  const { t } = useTranslation()

  return (
    <div className="flex flex-col h-[calc(100vh-4rem)]">
      {/* Header */}
      <div className="bg-white border-b border-gray-200 px-4 py-3">
        <h1 className="text-lg font-semibold text-gray-800">{t('chat.title')}</h1>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 bg-gray-50">
        <div className="text-center text-gray-500 py-8">
          {t('common.noResults')}
        </div>
      </div>

      {/* Message input */}
      <div className="bg-white border-t border-gray-200 p-4">
        <div className="flex gap-2">
          <input
            type="text"
            placeholder={t('chat.sendMessage')}
            className="flex-1 px-4 py-2 border border-gray-300 rounded-full focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none"
          />
          <button className="px-4 py-2 bg-primary-600 text-white rounded-full font-medium hover:bg-primary-700 transition-colors">
            →
          </button>
        </div>
      </div>
    </div>
  )
}
