import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { authApi } from '../lib/api'
import { useAuthStore } from '../stores/authStore'

interface RegisterForm {
  displayName: string
  phone: string
  position: 'Player' | 'Goalie'
}

export function RegisterPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { user, setProfile } = useAuthStore()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const { register, handleSubmit, formState: { errors } } = useForm<RegisterForm>({
    defaultValues: {
      position: 'Player',
    },
  })

  const onSubmit = async (data: RegisterForm) => {
    if (!user) return

    setLoading(true)
    setError('')

    try {
      const profile = await authApi.register({
        email: user.email || '',
        displayName: data.displayName,
        phone: data.phone,
        position: data.position === 'Goalie' ? 1 : 0,
      })

      setProfile(profile as any)
      navigate('/')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex flex-col min-h-screen bg-gradient-to-br from-primary-600 to-primary-800">
      <div className="flex-1 flex flex-col items-center justify-center p-6">
        <div className="w-full max-w-sm bg-white rounded-2xl shadow-xl p-6">
          <h2 className="text-xl font-semibold text-gray-800 mb-2 text-center">
            {t('auth.createAccount')}
          </h2>
          <p className="text-gray-500 text-center mb-6">
            {t('auth.welcomeBack')}
          </p>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div>
              <label htmlFor="displayName" className="block text-sm font-medium text-gray-700 mb-1">
                {t('auth.displayName')} *
              </label>
              <input
                type="text"
                id="displayName"
                {...register('displayName', { required: true })}
                className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none"
              />
              {errors.displayName && (
                <p className="mt-1 text-sm text-red-500">{t('common.error')}</p>
              )}
            </div>

            <div>
              <label htmlFor="phone" className="block text-sm font-medium text-gray-700 mb-1">
                {t('auth.phone')} *
              </label>
              <input
                type="tel"
                id="phone"
                placeholder="+420 123 456 789"
                {...register('phone', { required: true })}
                className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none"
              />
              {errors.phone && (
                <p className="mt-1 text-sm text-red-500">{t('common.error')}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                {t('team.positions.player')} / {t('team.positions.goalie')}
              </label>
              <div className="flex gap-4">
                <label className="flex-1">
                  <input
                    type="radio"
                    value="Player"
                    {...register('position')}
                    className="sr-only peer"
                  />
                  <div className="p-4 border-2 rounded-lg text-center cursor-pointer peer-checked:border-primary-600 peer-checked:bg-primary-50 transition-colors">
                    <span className="text-2xl">🏒</span>
                    <p className="mt-1 font-medium">{t('team.positions.player')}</p>
                  </div>
                </label>
                <label className="flex-1">
                  <input
                    type="radio"
                    value="Goalie"
                    {...register('position')}
                    className="sr-only peer"
                  />
                  <div className="p-4 border-2 rounded-lg text-center cursor-pointer peer-checked:border-primary-600 peer-checked:bg-primary-50 transition-colors">
                    <span className="text-2xl">🥅</span>
                    <p className="mt-1 font-medium">{t('team.positions.goalie')}</p>
                  </div>
                </label>
              </div>
            </div>

            {error && (
              <p className="text-sm text-red-500 text-center">{error}</p>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 bg-primary-600 text-white rounded-lg font-medium hover:bg-primary-700 transition-colors disabled:opacity-50"
            >
              {loading ? t('common.loading') : t('common.submit')}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
