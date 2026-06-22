import { useState, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { Plus, BarChart3, X, Lock, Trash2, Users } from 'lucide-react'
import { surveysApi } from '../lib/api'
import { useAuthStore } from '../stores/authStore'
import { SurveyFormModal } from '../components/SurveyFormModal'
import type { Survey, SurveyOption, SurveyVote } from '../lib/database.types'

interface SurveyResults {
  survey: Survey
  options: SurveyOption[]
  vote_counts: Record<string, number>
  votes: SurveyVote[] | null
}

export function SurveysPage() {
  const { t } = useTranslation()
  const { profile } = useAuthStore()
  const [results, setResults] = useState<SurveyResults[]>([])
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [busy, setBusy] = useState<string | null>(null)

  const teamId = profile?.team_id ?? null
  const canManage = profile?.role === 'Admin' || profile?.role === 'Coach'

  const load = useCallback(async () => {
    if (!teamId) {
      setLoading(false)
      return
    }
    setLoading(true)
    try {
      const surveys = (await surveysApi.getTeamSurveys(teamId)) as Survey[]
      const detailed = await Promise.all(
        (surveys ?? []).map((s) => surveysApi.getResults(s.id) as Promise<SurveyResults>)
      )
      setResults(detailed)
    } catch (err) {
      console.error('Error loading surveys:', err)
      setResults([])
    } finally {
      setLoading(false)
    }
  }, [teamId])

  useEffect(() => {
    load()
  }, [load])

  const myVotedOptions = (r: SurveyResults): Set<string> => {
    if (!r.votes || !profile) return new Set()
    return new Set(r.votes.filter((v) => v.user_id === profile.id).map((v) => v.option_id))
  }

  const vote = async (r: SurveyResults, optionId: string) => {
    if (r.survey.is_closed || busy) return
    setBusy(r.survey.id)
    try {
      const mine = myVotedOptions(r).has(optionId)
      if (mine) {
        await surveysApi.removeVote(r.survey.id, optionId)
      } else {
        await surveysApi.vote(r.survey.id, optionId)
      }
      await load()
    } catch (err) {
      console.error('Error voting:', err)
    } finally {
      setBusy(null)
    }
  }

  const closeSurvey = async (surveyId: string) => {
    try {
      await surveysApi.close(surveyId)
      await load()
    } catch (err) {
      console.error('Error closing survey:', err)
    }
  }

  const deleteSurvey = async (surveyId: string) => {
    if (!window.confirm(t('common.confirm') + '?')) return
    try {
      await surveysApi.delete(surveyId)
      setResults((prev) => prev.filter((r) => r.survey.id !== surveyId))
    } catch (err) {
      console.error('Error deleting survey:', err)
    }
  }

  if (!teamId) {
    return (
      <div className="p-4">
        <h1 className="text-2xl font-bold text-gray-800 mb-6">{t('surveys.title')}</h1>
        <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100 text-center text-gray-500">
          {t('team.noTeam')}
        </div>
      </div>
    )
  }

  return (
    <div className="p-4">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold text-gray-800">{t('surveys.title')}</h1>
        {canManage && (
          <button
            onClick={() => setShowModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg font-medium hover:bg-primary-700 transition-colors"
          >
            <Plus className="w-5 h-5" />
            <span className="hidden sm:inline">{t('surveys.createSurvey')}</span>
          </button>
        )}
      </div>

      {loading ? (
        <div className="text-center text-gray-500 py-8">{t('common.loading')}</div>
      ) : results.length === 0 ? (
        <div className="text-center text-gray-500 py-8">{t('common.noResults')}</div>
      ) : (
        <div className="space-y-4">
          {results.map((r) => {
            const total = Object.values(r.vote_counts).reduce((a, b) => a + b, 0)
            const mine = myVotedOptions(r)
            return (
              <div key={r.survey.id} className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
                <div className="flex items-start justify-between gap-2 mb-3">
                  <div className="flex-1">
                    <h3 className="font-semibold text-gray-800">{r.survey.question}</h3>
                    <div className="flex items-center gap-3 mt-1 text-xs text-gray-500">
                      <span className="flex items-center gap-1">
                        <Users className="w-3.5 h-3.5" /> {total} {t('surveys.votes')}
                      </span>
                      {r.survey.is_anonymous && <span>{t('surveys.anonymous')}</span>}
                      {r.survey.is_closed && (
                        <span className="flex items-center gap-1 text-red-500">
                          <Lock className="w-3.5 h-3.5" /> {t('surveys.closed')}
                        </span>
                      )}
                    </div>
                  </div>
                  {canManage && (
                    <div className="flex items-center gap-1">
                      {!r.survey.is_closed && (
                        <button
                          onClick={() => closeSurvey(r.survey.id)}
                          className="p-1.5 hover:bg-gray-100 rounded-lg"
                          title={t('surveys.closeSurvey')}
                        >
                          <X className="w-4 h-4 text-gray-500" />
                        </button>
                      )}
                      <button
                        onClick={() => deleteSurvey(r.survey.id)}
                        className="p-1.5 hover:bg-red-50 rounded-lg"
                        title={t('common.delete')}
                      >
                        <Trash2 className="w-4 h-4 text-red-500" />
                      </button>
                    </div>
                  )}
                </div>

                <div className="space-y-2">
                  {r.options.map((opt) => {
                    const count = r.vote_counts[opt.id] ?? 0
                    const pct = total > 0 ? Math.round((count / total) * 100) : 0
                    const voted = mine.has(opt.id)
                    return (
                      <button
                        key={opt.id}
                        onClick={() => vote(r, opt.id)}
                        disabled={r.survey.is_closed || busy === r.survey.id}
                        className={`relative w-full text-left px-3 py-2 rounded-lg border overflow-hidden transition-colors disabled:cursor-default ${
                          voted ? 'border-primary-500' : 'border-gray-200 hover:border-gray-300'
                        }`}
                      >
                        <div
                          className={`absolute inset-y-0 left-0 ${voted ? 'bg-primary-100' : 'bg-gray-100'}`}
                          style={{ width: `${pct}%` }}
                        />
                        <div className="relative flex justify-between items-center">
                          <span className={`font-medium ${voted ? 'text-primary-700' : 'text-gray-700'}`}>
                            {opt.text}
                          </span>
                          <span className="text-sm text-gray-500">
                            {count} · {pct}%
                          </span>
                        </div>
                      </button>
                    )
                  })}
                </div>

                {!r.survey.is_anonymous && (
                  <p className="mt-2 text-xs text-gray-400 flex items-center gap-1">
                    <BarChart3 className="w-3.5 h-3.5" /> {t('surveys.results')}
                  </p>
                )}
              </div>
            )
          })}
        </div>
      )}

      {showModal && teamId && (
        <SurveyFormModal
          teamId={teamId}
          onClose={() => setShowModal(false)}
          onCreated={() => {
            setShowModal(false)
            load()
          }}
        />
      )}
    </div>
  )
}
