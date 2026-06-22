import { useTranslation } from 'react-i18next'

export function LoadingScreen() {
  const { t } = useTranslation()

  return (
    <div className="flex items-center justify-center min-h-screen bg-primary-600">
      <div className="text-center">
        <div className="w-16 h-16 mx-auto mb-4 border-4 border-white border-t-transparent rounded-full animate-spin" />
        <h1 className="text-2xl font-bold text-white">THcommunity</h1>
        <p className="mt-2 text-primary-200">{t('common.loading')}</p>
      </div>
    </div>
  )
}
