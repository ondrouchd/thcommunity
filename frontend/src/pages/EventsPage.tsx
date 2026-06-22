import { useState, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { Plus, MapPin, Calendar } from 'lucide-react'
import { eventsApi } from '../lib/api'
import { useAuthStore } from '../stores/authStore'
import { EventFormModal } from '../components/EventFormModal'
import { formatDateTime } from '../lib/format'
import type { Event } from '../lib/database.types'

type Tab = 'upcoming' | 'past'

export function EventsPage() {
  const { t, i18n } = useTranslation()
  const { profile } = useAuthStore()
  const [tab, setTab] = useState<Tab>('upcoming')
  const [events, setEvents] = useState<Event[]>([])
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)

  const canManage = profile?.role === 'Admin' || profile?.role === 'Coach'

  const fetchEvents = useCallback(async () => {
    if (!profile?.team_id) {
      setLoading(false)
      return
    }
    setLoading(true)
    try {
      const data =
        tab === 'upcoming'
          ? await eventsApi.getUpcoming(profile.team_id)
          : await eventsApi.getPast(profile.team_id)
      setEvents(data ?? [])
    } catch (err) {
      console.error('Error fetching events:', err)
      setEvents([])
    } finally {
      setLoading(false)
    }
  }, [profile?.team_id, tab])

  useEffect(() => {
    fetchEvents()
  }, [fetchEvents])

  if (!profile?.team_id) {
    return (
      <div className="p-4">
        <h1 className="text-2xl font-bold text-gray-800 mb-6">{t('events.title')}</h1>
        <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100 text-center text-gray-500">
          {t('team.noTeam')}
        </div>
      </div>
    )
  }

  return (
    <div className="p-4">
      {/* Header */}
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold text-gray-800">{t('events.title')}</h1>
        {canManage && (
          <button
            onClick={() => setShowModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg font-medium hover:bg-primary-700 transition-colors"
          >
            <Plus className="w-5 h-5" />
            <span className="hidden sm:inline">{t('events.createEvent')}</span>
          </button>
        )}
      </div>

      {/* Tabs */}
      <div className="flex gap-2 mb-6">
        <button
          onClick={() => setTab('upcoming')}
          className={`px-4 py-2 rounded-lg font-medium transition-colors ${
            tab === 'upcoming' ? 'bg-primary-600 text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
        >
          {t('events.upcoming')}
        </button>
        <button
          onClick={() => setTab('past')}
          className={`px-4 py-2 rounded-lg font-medium transition-colors ${
            tab === 'past' ? 'bg-primary-600 text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
        >
          {t('events.past')}
        </button>
      </div>

      {/* Events list */}
      <div className="space-y-4">
        {loading ? (
          <div className="text-center text-gray-500 py-8">{t('common.loading')}</div>
        ) : events.length === 0 ? (
          <div className="text-center text-gray-500 py-8">{t('common.noResults')}</div>
        ) : (
          events.map((event) => (
            <Link
              key={event.id}
              to={`/events/${event.id}`}
              className="block bg-white rounded-xl p-4 shadow-sm border border-gray-100 hover:shadow-md transition-shadow"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <span
                      className={`px-2 py-0.5 rounded text-xs font-medium ${
                        event.event_type === 'Match'
                          ? 'bg-red-100 text-red-700'
                          : 'bg-blue-100 text-blue-700'
                      }`}
                    >
                      {event.event_type === 'Match'
                        ? t('events.eventType.match')
                        : t('events.eventType.training')}
                    </span>
                  </div>
                  <h3 className="font-semibold text-gray-800 truncate">{event.title}</h3>
                  <div className="flex items-center gap-1 text-sm text-gray-500 mt-1">
                    <Calendar className="w-4 h-4" />
                    <span>{formatDateTime(event.start_time, i18n.language)}</span>
                  </div>
                  {event.location && (
                    <div className="flex items-center gap-1 text-sm text-gray-500 mt-0.5">
                      <MapPin className="w-4 h-4" />
                      <span className="truncate">{event.location}</span>
                    </div>
                  )}
                </div>
              </div>
            </Link>
          ))
        )}
      </div>

      {showModal && profile.team_id && (
        <EventFormModal
          teamId={profile.team_id}
          onClose={() => setShowModal(false)}
          onCreated={() => {
            setShowModal(false)
            setTab('upcoming')
            fetchEvents()
          }}
        />
      )}
    </div>
  )
}
