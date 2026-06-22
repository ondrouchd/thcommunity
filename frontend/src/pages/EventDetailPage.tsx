import { useState, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { ArrowLeft, MapPin, Calendar, Trash2, Check, X, Clock } from 'lucide-react'
import { eventsApi, teamsApi } from '../lib/api'
import { useAuthStore } from '../stores/authStore'
import { formatDate, formatTime } from '../lib/format'
import type { Event, EventResponse, User } from '../lib/database.types'

type ResponseValue = 'Player' | 'Goalie' | 'Cannot'

export function EventDetailPage() {
  const { t, i18n } = useTranslation()
  const { id } = useParams()
  const navigate = useNavigate()
  const { profile } = useAuthStore()

  const [event, setEvent] = useState<Event | null>(null)
  const [responses, setResponses] = useState<EventResponse[]>([])
  const [waitlist, setWaitlist] = useState<EventResponse[]>([])
  const [myResponse, setMyResponse] = useState<EventResponse | null>(null)
  const [price, setPrice] = useState<number | null>(null)
  const [members, setMembers] = useState<Record<string, User>>({})
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)

  const canManage = profile?.role === 'Admin' || profile?.role === 'Coach'

  const loadData = useCallback(async () => {
    if (!id) return
    setLoading(true)
    try {
      const [evt, resp, wait, my, priceRes] = await Promise.all([
        eventsApi.get(id),
        eventsApi.getResponses(id).catch(() => []),
        eventsApi.getWaitlist(id).catch(() => []),
        eventsApi.getMyResponse(id).catch(() => null),
        eventsApi.getPrice(id).catch(() => null),
      ])
      setEvent(evt)
      setResponses(resp ?? [])
      setWaitlist(wait ?? [])
      setMyResponse(my)
      setPrice(priceRes?.price ?? null)

      if (evt?.team_id) {
        const m = await teamsApi.getMembers(evt.team_id).catch(() => [])
        const map: Record<string, User> = {}
        ;(m ?? []).forEach((member) => {
          map[member.id] = member
        })
        setMembers(map)
      }
    } catch (err) {
      console.error('Error loading event:', err)
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    loadData()
  }, [loadData])

  const respond = async (value: ResponseValue) => {
    if (!id) return
    setSubmitting(true)
    try {
      await eventsApi.respond(id, { response: value })
      await loadData()
    } catch (err) {
      console.error('Error responding:', err)
    } finally {
      setSubmitting(false)
    }
  }

  const handleDelete = async () => {
    if (!id || !window.confirm(t('common.confirm') + '?')) return
    try {
      await eventsApi.delete(id)
      navigate('/events')
    } catch (err) {
      console.error('Error deleting event:', err)
    }
  }

  const nameOf = (userId: string) => members[userId]?.display_name ?? '—'

  const players = responses.filter((r) => r.response === 'Player')
  const goalies = responses.filter((r) => r.response === 'Goalie')
  const declined = responses.filter((r) => r.response === 'Cannot')

  if (loading) {
    return (
      <div className="p-4">
        <Header />
        <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100 text-center text-gray-500">
          {t('common.loading')}
        </div>
      </div>
    )
  }

  if (!event) {
    return (
      <div className="p-4">
        <Header />
        <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100 text-center text-gray-500">
          {t('errors.notFound')}
        </div>
      </div>
    )
  }

  return (
    <div className="p-4 space-y-4">
      <div className="flex items-center justify-between">
        <Header />
        {canManage && (
          <button
            onClick={handleDelete}
            className="p-2 hover:bg-red-50 rounded-lg transition-colors"
            aria-label={t('common.delete')}
          >
            <Trash2 className="w-5 h-5 text-red-500" />
          </button>
        )}
      </div>

      {/* Event info */}
      <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
        <span
          className={`inline-block px-2 py-0.5 rounded text-xs font-medium mb-2 ${
            event.event_type === 'Match' ? 'bg-red-100 text-red-700' : 'bg-blue-100 text-blue-700'
          }`}
        >
          {event.event_type === 'Match' ? t('events.eventType.match') : t('events.eventType.training')}
        </span>
        <h2 className="text-xl font-bold text-gray-800 mb-3">{event.title}</h2>

        <div className="space-y-2 text-sm text-gray-600">
          <div className="flex items-center gap-2">
            <Calendar className="w-4 h-4 text-gray-400" />
            <span>{formatDate(event.start_time, i18n.language)}</span>
          </div>
          <div className="flex items-center gap-2">
            <Clock className="w-4 h-4 text-gray-400" />
            <span>
              {formatTime(event.start_time, i18n.language)} – {formatTime(event.end_time, i18n.language)}
            </span>
          </div>
          {event.location && (
            <div className="flex items-center gap-2">
              <MapPin className="w-4 h-4 text-gray-400" />
              <span>{event.location}</span>
            </div>
          )}
        </div>

        {event.description && <p className="mt-3 text-gray-600">{event.description}</p>}

        {price != null && (
          <div className="mt-4 bg-gray-50 rounded-lg px-4 py-2 flex justify-between items-center">
            <span className="text-sm text-gray-600">{t('events.price')}</span>
            <span className="font-semibold text-gray-800">{price} Kč</span>
          </div>
        )}
      </div>

      {/* RSVP */}
      <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
        <h3 className="font-semibold text-gray-800 mb-3">{t('events.response.title')}</h3>
        <div className="grid grid-cols-3 gap-2">
          <RsvpButton
            active={myResponse?.response === 'Player'}
            disabled={submitting}
            onClick={() => respond('Player')}
            color="green"
            label={t('events.response.going')}
          />
          <RsvpButton
            active={myResponse?.response === 'Goalie'}
            disabled={submitting}
            onClick={() => respond('Goalie')}
            color="blue"
            label={t('events.response.goingAsGoalie')}
          />
          <RsvpButton
            active={myResponse?.response === 'Cannot'}
            disabled={submitting}
            onClick={() => respond('Cannot')}
            color="red"
            label={t('events.response.notGoing')}
          />
        </div>
      </div>

      {/* Attendance */}
      <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
        <h3 className="font-semibold text-gray-800 mb-3">{t('events.attendance.title')}</h3>

        <AttendanceGroup
          icon={<Check className="w-4 h-4 text-green-600" />}
          title={`${t('events.players')} (${players.length}/${event.capacity_players})`}
          names={players.map((r) => nameOf(r.user_id))}
          emptyLabel={t('events.attendance.noResponse')}
        />
        <AttendanceGroup
          icon={<Check className="w-4 h-4 text-blue-600" />}
          title={`${t('events.goalies')} (${goalies.length}/${event.capacity_goalies})`}
          names={goalies.map((r) => nameOf(r.user_id))}
          emptyLabel={t('events.attendance.noResponse')}
        />
        <AttendanceGroup
          icon={<X className="w-4 h-4 text-red-500" />}
          title={`${t('events.attendance.declined')} (${declined.length})`}
          names={declined.map((r) => nameOf(r.user_id))}
          emptyLabel={t('events.attendance.noResponse')}
        />
      </div>

      {/* Waitlist */}
      {waitlist.length > 0 && (
        <div className="bg-white rounded-xl p-5 shadow-sm border border-gray-100">
          <h3 className="font-semibold text-gray-800 mb-3">
            {t('events.waitlist.title')} ({waitlist.length})
          </h3>
          <div className="space-y-1">
            {waitlist.map((w, idx) => (
              <div key={w.id ?? idx} className="flex items-center gap-2 text-sm text-gray-700">
                <span className="text-gray-400 w-5">{idx + 1}.</span>
                <span>{nameOf(w.user_id)}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )

  function Header() {
    return (
      <div className="flex items-center gap-4">
        <Link to="/events" className="p-2 hover:bg-gray-100 rounded-lg transition-colors">
          <ArrowLeft className="w-6 h-6 text-gray-600" />
        </Link>
        <h1 className="text-xl font-bold text-gray-800">{t('events.eventDetails')}</h1>
      </div>
    )
  }
}

function RsvpButton({
  active,
  disabled,
  onClick,
  color,
  label,
}: {
  active: boolean
  disabled: boolean
  onClick: () => void
  color: 'green' | 'blue' | 'red'
  label: string
}) {
  const activeClasses: Record<string, string> = {
    green: 'bg-green-600 text-white border-green-600',
    blue: 'bg-blue-600 text-white border-blue-600',
    red: 'bg-red-600 text-white border-red-600',
  }
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={`py-2 px-2 rounded-lg text-sm font-medium border transition-colors disabled:opacity-50 ${
        active ? activeClasses[color] : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'
      }`}
    >
      {label}
    </button>
  )
}

function AttendanceGroup({
  icon,
  title,
  names,
  emptyLabel,
}: {
  icon: React.ReactNode
  title: string
  names: string[]
  emptyLabel: string
}) {
  return (
    <div className="mb-4 last:mb-0">
      <div className="flex items-center gap-2 mb-1">
        {icon}
        <span className="text-sm font-medium text-gray-700">{title}</span>
      </div>
      {names.length === 0 ? (
        <p className="text-sm text-gray-400 pl-6">{emptyLabel}</p>
      ) : (
        <div className="flex flex-wrap gap-1.5 pl-6">
          {names.map((name, idx) => (
            <span key={idx} className="px-2 py-0.5 bg-gray-100 rounded text-xs text-gray-700">
              {name}
            </span>
          ))}
        </div>
      )}
    </div>
  )
}
