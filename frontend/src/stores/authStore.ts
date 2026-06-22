import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { User as SupabaseUser } from '@supabase/supabase-js'
import type { User } from '../lib/database.types'

interface AuthState {
  user: SupabaseUser | null
  profile: User | null
  isLoading: boolean
  setUser: (user: SupabaseUser | null) => void
  setProfile: (profile: User | null) => void
  setLoading: (loading: boolean) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      profile: null,
      isLoading: true,
      setUser: (user) => set({ user }),
      setProfile: (profile) => set({ profile }),
      setLoading: (isLoading) => set({ isLoading }),
      logout: () => set({ user: null, profile: null }),
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({ profile: state.profile }),
    }
  )
)

interface TeamState {
  currentTeam: User['team_id']
  setCurrentTeam: (teamId: User['team_id']) => void
}

export const useTeamStore = create<TeamState>()(
  persist(
    (set) => ({
      currentTeam: null,
      setCurrentTeam: (teamId) => set({ currentTeam: teamId }),
    }),
    {
      name: 'team-storage',
    }
  )
)
