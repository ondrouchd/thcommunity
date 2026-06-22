import { supabase } from './supabase'
import type { Event, EventResponse, Team, User, Message, Survey, SurveyOption } from './database.types'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

// Cache the token to avoid repeated getSession calls
let cachedToken: string | null = null

export function setApiToken(token: string | null) {
  cachedToken = token
  console.log('API token cached:', token ? 'yes' : 'no')
}

class ApiClient {
  private async getToken(): Promise<string | null> {
    // First try cached token
    if (cachedToken) {
      return cachedToken
    }
    
    // Fall back to getting fresh token
    try {
      console.log('Getting fresh token from Supabase...')
      const { data: { session }, error } = await supabase.auth.getSession()
      if (error) {
        console.error('Error getting session:', error)
        return null
      }
      if (session?.access_token) {
        cachedToken = session.access_token
      }
      return session?.access_token ?? null
    } catch (err) {
      console.error('Failed to get token:', err)
      return null
    }
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const token = await this.getToken()
    
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    }

    if (token) {
      headers['Authorization'] = `Bearer ${token}`
      console.log('Request with token:', endpoint, 'token length:', token.length)
    } else {
      console.warn('API request without token:', endpoint)
    }

    const response = await fetch(`${API_URL}${endpoint}`, {
      ...options,
      headers: {
        ...headers,
        ...(options.headers as Record<string, string>),
      },
    })

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: response.statusText }))
      throw new Error(error.message || 'API request failed')
    }

    // Handle 204 No Content
    if (response.status === 204) {
      return {} as T
    }

    return response.json()
  }

  async get<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'GET' })
  }

  async post<T>(endpoint: string, data?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    })
  }

  async put<T>(endpoint: string, data: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
  }

  async delete(endpoint: string): Promise<void> {
    await this.request(endpoint, { method: 'DELETE' })
  }
}

export const api = new ApiClient()

// Auth API
export const authApi = {
  register: (data: { email: string; displayName: string; phone: string; position: number }) =>
    api.post<User>('/api/auth/register', {
      email: data.email,
      phone: data.phone,
      display_name: data.displayName,
      position: data.position,
    }),

  login: (data: { email: string; password: string }) =>
    api.post<{ token: string; user: unknown }>('/api/auth/login', data),

  me: () => api.get<User>('/api/auth/me'),
}

// Teams API
export const teamsApi = {
  get: (id: string) => api.get<Team>(`/api/teams/${id}`),
  create: (data: { name: string; description?: string }) => api.post<Team>('/api/teams', data),
  update: (id: string, data: { name?: string; description?: string }) => api.put<Team>(`/api/teams/${id}`, data),
  getMembers: (id: string) => api.get<User[]>(`/api/teams/${id}/members`),
  getSettings: (id: string) => api.get(`/api/teams/${id}/settings`),
  updateSettings: (id: string, settings: unknown) => api.put(`/api/teams/${id}/settings`, settings),
  join: (inviteCode: string) => api.post(`/api/teams/join/${inviteCode}`),
  leave: (id: string) => api.post(`/api/teams/${id}/leave`),
}

// Events API
export const eventsApi = {
  getUpcoming: (teamId: string, limit = 20) =>
    api.get<Event[]>(`/api/events/team/${teamId}/upcoming?limit=${limit}`),

  getPast: (teamId: string, limit = 20, offset = 0) =>
    api.get<Event[]>(`/api/events/team/${teamId}/past?limit=${limit}&offset=${offset}`),

  get: (id: string) => api.get<Event>(`/api/events/${id}`),

  create: (data: {
    teamId: string
    title: string
    description?: string
    eventType: 'Training' | 'Match'
    startTime: string
    endTime: string
    location?: string
    capacityPlayers?: number
    capacityGoalies?: number
    responseDeadline?: string
    priceOverride?: number
  }) => api.post<Event>('/api/events', {
    team_id: data.teamId,
    title: data.title,
    description: data.description,
    event_type: data.eventType,
    start_time: data.startTime,
    end_time: data.endTime,
    location: data.location,
    capacity_players: data.capacityPlayers,
    capacity_goalies: data.capacityGoalies,
    response_deadline: data.responseDeadline,
    price_override: data.priceOverride,
  }),

  update: (id: string, data: Record<string, unknown>) => api.put<Event>(`/api/events/${id}`, data),
  delete: (id: string) => api.delete(`/api/events/${id}`),

  getResponses: (id: string) => api.get<EventResponse[]>(`/api/events/${id}/responses`),
  respond: (id: string, data: { response: 'Player' | 'Goalie' | 'Cannot' | 'Maybe'; note?: string }) =>
    api.post<EventResponse>(`/api/events/${id}/respond`, data),

  getMyResponse: (id: string) => api.get<EventResponse>(`/api/events/${id}/my-response`),
  getWaitlist: (id: string) => api.get<EventResponse[]>(`/api/events/${id}/waitlist`),
  getPrice: (id: string) => api.get<{ price: number }>(`/api/events/${id}/price`),
  getStatistics: (id: string) => api.get(`/api/events/${id}/statistics`),
}

// Messages API
export const messagesApi = {
  getTeamMessages: (teamId: string, limit = 50, before?: string) => {
    const params = new URLSearchParams({ limit: String(limit) })
    if (before) params.append('before', before)
    return api.get<Message[]>(`/api/messages/team/${teamId}?${params}`)
  },

  send: (data: {
    teamId: string
    content: string
    type?: 'Text' | 'Image' | 'Video'
    mediaUrl?: string
    mediaType?: string
    replyToId?: string
  }) => api.post<Message>('/api/messages', {
    team_id: data.teamId,
    content: data.content,
    type: data.type,
    media_url: data.mediaUrl,
    media_type: data.mediaType,
    reply_to_id: data.replyToId,
  }),

  update: (id: string, content: string) => api.put<Message>(`/api/messages/${id}`, { content }),
  delete: (id: string) => api.delete(`/api/messages/${id}`),

  getReactions: (id: string) => api.get(`/api/messages/${id}/reactions`),
  addReaction: (id: string, emoji: string) => api.post(`/api/messages/${id}/reactions`, { emoji }),
  removeReaction: (id: string, emoji: string) => api.delete(`/api/messages/${id}/reactions/${emoji}`),
}

// Surveys API
export const surveysApi = {
  getTeamSurveys: (teamId: string) => api.get<Survey[]>(`/api/surveys/team/${teamId}`),
  get: (id: string) => api.get<Survey>(`/api/surveys/${id}`),
  getOptions: (id: string) => api.get<SurveyOption[]>(`/api/surveys/${id}/options`),
  getResults: (id: string) => api.get(`/api/surveys/${id}/results`),

  create: (data: {
    teamId: string
    question: string
    options: string[]
    allowMultipleAnswers?: boolean
    isAnonymous?: boolean
    expiresAt?: string
  }) => api.post<Survey>('/api/surveys', {
    team_id: data.teamId,
    question: data.question,
    options: data.options,
    allow_multiple_answers: data.allowMultipleAnswers,
    is_anonymous: data.isAnonymous,
    expires_at: data.expiresAt,
  }),

  vote: (id: string, optionId: string) => api.post(`/api/surveys/${id}/vote`, { option_id: optionId }),
  removeVote: (surveyId: string, optionId: string) => api.delete(`/api/surveys/${surveyId}/vote/${optionId}`),
  close: (id: string) => api.post(`/api/surveys/${id}/close`),
  delete: (id: string) => api.delete(`/api/surveys/${id}`),
}

// Users API
export const usersApi = {
  get: (id: string) => api.get<User>(`/api/users/${id}`),
  updateMe: (data: { displayName?: string; phone?: string; avatarUrl?: string; position?: 'Player' | 'Goalie' }) =>
    api.put<User>('/api/users/me', {
      display_name: data.displayName,
      phone: data.phone,
      avatar_url: data.avatarUrl,
      position: data.position,
    }),
  getMyStatistics: () => api.get('/api/users/me/statistics'),
  getTeamStatistics: (teamId: string, from?: string, to?: string) => {
    const params = new URLSearchParams()
    if (from) params.append('from', from)
    if (to) params.append('to', to)
    return api.get(`/api/users/team/${teamId}/statistics?${params}`)
  },
}

// Push Notifications API
export const pushApi = {
  getVapidKey: () => api.get<{ public_key: string | null }>('/api/push/vapid-public-key'),
  subscribe: (data: { endpoint: string; p256dh: string; auth: string }) =>
    api.post('/api/push/subscribe', data),
  unsubscribe: (endpoint: string) =>
    api.post('/api/push/unsubscribe', { endpoint }),
}
