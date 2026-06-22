import { useState, useEffect, useRef, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { Send, Trash2, SmilePlus } from 'lucide-react'
import { messagesApi, teamsApi } from '../lib/api'
import { useAuthStore } from '../stores/authStore'
import { supabase } from '../lib/supabase'
import { formatTime } from '../lib/format'
import type { Message, User } from '../lib/database.types'

interface MessageReaction {
  id: string
  message_id: string
  user_id: string
  emoji: string
}

const QUICK_EMOJIS = ['👍', '❤️', '😂', '🔥', '👏']

export function ChatPage() {
  const { t, i18n } = useTranslation()
  const { profile } = useAuthStore()
  const [messages, setMessages] = useState<Message[]>([])
  const [members, setMembers] = useState<Record<string, User>>({})
  const [reactions, setReactions] = useState<Record<string, MessageReaction[]>>({})
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(true)
  const [sending, setSending] = useState(false)
  const [pickerFor, setPickerFor] = useState<string | null>(null)
  const bottomRef = useRef<HTMLDivElement>(null)

  const teamId = profile?.team_id ?? null

  const scrollToBottom = () => {
    requestAnimationFrame(() => bottomRef.current?.scrollIntoView({ behavior: 'smooth' }))
  }

  const loadReactions = useCallback(async (msgs: Message[]) => {
    const entries = await Promise.all(
      msgs.map(async (m) => {
        try {
          const r = (await messagesApi.getReactions(m.id)) as MessageReaction[]
          return [m.id, r ?? []] as const
        } catch {
          return [m.id, []] as const
        }
      })
    )
    setReactions(Object.fromEntries(entries))
  }, [])

  const loadMessages = useCallback(async () => {
    if (!teamId) {
      setLoading(false)
      return
    }
    setLoading(true)
    try {
      const data = (await messagesApi.getTeamMessages(teamId)) as Message[]
      const ordered = (data ?? []).slice().reverse()
      setMessages(ordered)
      await loadReactions(ordered)
      const m = await teamsApi.getMembers(teamId).catch(() => [])
      const map: Record<string, User> = {}
      ;(m ?? []).forEach((member) => {
        map[member.id] = member
      })
      setMembers(map)
      scrollToBottom()
    } catch (err) {
      console.error('Error loading messages:', err)
    } finally {
      setLoading(false)
    }
  }, [teamId, loadReactions])

  useEffect(() => {
    loadMessages()
  }, [loadMessages])

  // Realtime: append new messages for this team
  useEffect(() => {
    if (!teamId) return
    const channel = supabase
      .channel(`messages:${teamId}`)
      .on(
        'postgres_changes',
        { event: 'INSERT', schema: 'public', table: 'messages', filter: `team_id=eq.${teamId}` },
        (payload) => {
          const newMsg = payload.new as Message
          if (newMsg.is_deleted) return
          setMessages((prev) => (prev.some((m) => m.id === newMsg.id) ? prev : [...prev, newMsg]))
          scrollToBottom()
        }
      )
      .subscribe()

    return () => {
      supabase.removeChannel(channel)
    }
  }, [teamId])

  const handleSend = async () => {
    const content = input.trim()
    if (!content || !teamId || sending) return
    setSending(true)
    setInput('')
    try {
      const sent = (await messagesApi.send({ teamId, content })) as Message
      setMessages((prev) => (prev.some((m) => m.id === sent.id) ? prev : [...prev, sent]))
      scrollToBottom()
    } catch (err) {
      console.error('Error sending message:', err)
      setInput(content)
    } finally {
      setSending(false)
    }
  }

  const handleDelete = async (messageId: string) => {
    try {
      await messagesApi.delete(messageId)
      setMessages((prev) => prev.filter((m) => m.id !== messageId))
    } catch (err) {
      console.error('Error deleting message:', err)
    }
  }

  const toggleReaction = async (messageId: string, emoji: string) => {
    if (!profile) return
    setPickerFor(null)
    const current = reactions[messageId] ?? []
    const mine = current.find((r) => r.user_id === profile.id && r.emoji === emoji)
    try {
      if (mine) {
        await messagesApi.removeReaction(messageId, emoji)
        setReactions((prev) => ({
          ...prev,
          [messageId]: (prev[messageId] ?? []).filter((r) => r.id !== mine.id),
        }))
      } else {
        await messagesApi.addReaction(messageId, emoji)
        setReactions((prev) => ({
          ...prev,
          [messageId]: [
            ...(prev[messageId] ?? []),
            { id: `${messageId}-${emoji}-${profile.id}`, message_id: messageId, user_id: profile.id, emoji },
          ],
        }))
      }
    } catch (err) {
      console.error('Error toggling reaction:', err)
    }
  }

  const groupedReactions = (messageId: string) => {
    const list = reactions[messageId] ?? []
    const groups: Record<string, { count: number; mine: boolean }> = {}
    list.forEach((r) => {
      if (!groups[r.emoji]) groups[r.emoji] = { count: 0, mine: false }
      groups[r.emoji].count++
      if (r.user_id === profile?.id) groups[r.emoji].mine = true
    })
    return Object.entries(groups)
  }

  if (!teamId) {
    return (
      <div className="flex flex-col h-[calc(100vh-4rem)]">
        <div className="bg-white border-b border-gray-200 px-4 py-3">
          <h1 className="text-lg font-semibold text-gray-800">{t('chat.title')}</h1>
        </div>
        <div className="flex-1 flex items-center justify-center text-gray-500">{t('team.noTeam')}</div>
      </div>
    )
  }

  return (
    <div className="flex flex-col h-[calc(100vh-4rem)]">
      {/* Header */}
      <div className="bg-white border-b border-gray-200 px-4 py-3">
        <h1 className="text-lg font-semibold text-gray-800">{t('chat.title')}</h1>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 bg-gray-50 space-y-3" onClick={() => setPickerFor(null)}>
        {loading ? (
          <div className="text-center text-gray-500 py-8">{t('common.loading')}</div>
        ) : messages.length === 0 ? (
          <div className="text-center text-gray-500 py-8">{t('common.noResults')}</div>
        ) : (
          messages.map((msg) => {
            const isMine = msg.user_id === profile?.id
            const author = members[msg.user_id]?.display_name ?? 'Někdo'
            const groups = groupedReactions(msg.id)
            return (
              <div key={msg.id} className={`flex ${isMine ? 'justify-end' : 'justify-start'}`}>
                <div className={`max-w-[75%] ${isMine ? 'items-end' : 'items-start'} flex flex-col`}>
                  {!isMine && <span className="text-xs text-gray-500 mb-0.5 px-1">{author}</span>}
                  <div className="group relative">
                    <div
                      className={`px-3 py-2 rounded-2xl ${
                        isMine ? 'bg-primary-600 text-white rounded-br-sm' : 'bg-white text-gray-800 rounded-bl-sm border border-gray-100'
                      }`}
                    >
                      <p className="whitespace-pre-wrap break-words">{msg.content}</p>
                      <div className={`flex items-center gap-2 mt-1 ${isMine ? 'text-primary-100' : 'text-gray-400'}`}>
                        <span className="text-[10px]">{formatTime(msg.created_at, i18n.language)}</span>
                        {msg.edited_at && <span className="text-[10px]">({t('chat.edited')})</span>}
                      </div>
                    </div>

                    {/* actions */}
                    <div
                      className={`absolute top-0 ${isMine ? 'left-0 -translate-x-full pr-1' : 'right-0 translate-x-full pl-1'} hidden group-hover:flex items-center gap-1`}
                    >
                      <button
                        onClick={(e) => {
                          e.stopPropagation()
                          setPickerFor(pickerFor === msg.id ? null : msg.id)
                        }}
                        className="p-1 bg-white border border-gray-200 rounded-full shadow-sm hover:bg-gray-50"
                        aria-label={t('chat.reactions')}
                      >
                        <SmilePlus className="w-4 h-4 text-gray-500" />
                      </button>
                      {isMine && (
                        <button
                          onClick={(e) => {
                            e.stopPropagation()
                            handleDelete(msg.id)
                          }}
                          className="p-1 bg-white border border-gray-200 rounded-full shadow-sm hover:bg-gray-50"
                          aria-label={t('chat.delete')}
                        >
                          <Trash2 className="w-4 h-4 text-red-500" />
                        </button>
                      )}
                    </div>

                    {/* emoji picker */}
                    {pickerFor === msg.id && (
                      <div
                        className={`absolute z-10 -bottom-1 translate-y-full ${isMine ? 'right-0' : 'left-0'} flex gap-1 bg-white border border-gray-200 rounded-full shadow-md px-2 py-1`}
                        onClick={(e) => e.stopPropagation()}
                      >
                        {QUICK_EMOJIS.map((emoji) => (
                          <button
                            key={emoji}
                            onClick={() => toggleReaction(msg.id, emoji)}
                            className="text-lg hover:scale-125 transition-transform"
                          >
                            {emoji}
                          </button>
                        ))}
                      </div>
                    )}
                  </div>

                  {/* reaction chips */}
                  {groups.length > 0 && (
                    <div className={`flex flex-wrap gap-1 mt-1 ${isMine ? 'justify-end' : 'justify-start'}`}>
                      {groups.map(([emoji, info]) => (
                        <button
                          key={emoji}
                          onClick={() => toggleReaction(msg.id, emoji)}
                          className={`px-1.5 py-0.5 rounded-full text-xs border ${
                            info.mine ? 'bg-primary-50 border-primary-300' : 'bg-white border-gray-200'
                          }`}
                        >
                          {emoji} {info.count}
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            )
          })
        )}
        <div ref={bottomRef} />
      </div>

      {/* Message input */}
      <div className="bg-white border-t border-gray-200 p-4">
        <div className="flex gap-2">
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault()
                handleSend()
              }
            }}
            placeholder={t('chat.sendMessage')}
            className="flex-1 px-4 py-2 border border-gray-300 rounded-full focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none"
          />
          <button
            onClick={handleSend}
            disabled={sending || !input.trim()}
            className="px-4 py-2 bg-primary-600 text-white rounded-full font-medium hover:bg-primary-700 transition-colors disabled:opacity-50"
            aria-label={t('common.submit')}
          >
            <Send className="w-5 h-5" />
          </button>
        </div>
      </div>
    </div>
  )
}
