import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useAuthStore } from '../stores/authStore'
import { teamsApi } from '../lib/api'
import { Users, Settings, Copy, Check } from 'lucide-react'

export function TeamPage() {
  const { t } = useTranslation()
  const { profile } = useAuthStore()
  const [team, setTeam] = useState<any>(null)
  const [members, setMembers] = useState<any[]>([])
  const [loading, setLoading] = useState(true)
  const [copiedCode, setCopiedCode] = useState(false)

  useEffect(() => {
    if (profile?.team_id) {
      fetchTeamData()
    }
  }, [profile?.team_id])

  const fetchTeamData = async () => {
    if (!profile?.team_id) return
    
    try {
      setLoading(true)
      const [teamData, membersData] = await Promise.all([
        teamsApi.get(profile.team_id),
        teamsApi.getMembers(profile.team_id)
      ])
      setTeam(teamData)
      setMembers(membersData as any[])
    } catch (err) {
      console.error('Error fetching team data:', err)
    } finally {
      setLoading(false)
    }
  }

  const copyInviteCode = async () => {
    if (team?.invite_code) {
      await navigator.clipboard.writeText(team.invite_code)
      setCopiedCode(true)
      setTimeout(() => setCopiedCode(false), 2000)
    }
  }

  if (!profile?.team_id) {
    return (
      <div className="p-4">
        <h1 className="text-2xl font-bold text-gray-800 mb-6">{t('team.title')}</h1>
        <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
          <p className="text-gray-600">Dokončete registraci pro připojení k týmu.</p>
        </div>
      </div>
    )
  }

  if (loading) {
    return (
      <div className="p-4">
        <h1 className="text-2xl font-bold text-gray-800 mb-6">{t('team.title')}</h1>
        <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
          <div className="text-center text-gray-500 py-4">
            {t('common.loading')}
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="p-4">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold text-gray-800">{team?.name || t('team.title')}</h1>
        {profile.role === 'Admin' && (
          <button className="p-2 hover:bg-gray-100 rounded-lg transition-colors">
            <Settings className="w-6 h-6 text-gray-600" />
          </button>
        )}
      </div>

      {/* Team info */}
      {team && (
        <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100 mb-4">
          {team.description && (
            <p className="text-gray-600 mb-4">{team.description}</p>
          )}
          
          {/* Invite code */}
          <div className="bg-gray-50 rounded-lg p-4">
            <label className="text-sm text-gray-600 block mb-2">Kód pozvánky</label>
            <div className="flex items-center gap-2">
              <code className="flex-1 text-xl font-mono font-bold text-primary-600 bg-white px-4 py-2 rounded border border-gray-200">
                {team.invite_code}
              </code>
              <button
                onClick={copyInviteCode}
                className="p-2 hover:bg-gray-200 rounded-lg transition-colors"
                title="Kopírovat kód"
              >
                {copiedCode ? (
                  <Check className="w-5 h-5 text-green-600" />
                ) : (
                  <Copy className="w-5 h-5 text-gray-600" />
                )}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Team members */}
      <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
        <div className="flex items-center gap-3 mb-4">
          <Users className="w-5 h-5 text-primary-600" />
          <h2 className="font-semibold text-gray-800">{t('team.members')} ({members.length})</h2>
        </div>
        
        <div className="space-y-2">
          {members.map((member) => (
            <div key={member.id} className="flex items-center gap-3 p-3 hover:bg-gray-50 rounded-lg">
              <div className="w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center">
                <span className="text-primary-600 font-semibold">
                  {member.display_name?.charAt(0).toUpperCase()}
                </span>
              </div>
              <div className="flex-1">
                <p className="font-medium text-gray-800">{member.display_name}</p>
                <p className="text-sm text-gray-500">
                  {member.position} • {member.role}
                </p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
