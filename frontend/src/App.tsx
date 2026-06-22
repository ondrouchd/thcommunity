import { Routes, Route, Navigate } from 'react-router-dom'
import { useEffect } from 'react'
import { supabase } from './lib/supabase'
import { setApiToken, authApi } from './lib/api'
import { useAuthStore } from './stores/authStore'

// Pages
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { HomePage } from './pages/HomePage'
import { ChatPage } from './pages/ChatPage'
import { EventsPage } from './pages/EventsPage'
import { EventDetailPage } from './pages/EventDetailPage'
import { TeamPage } from './pages/TeamPage'
import { ProfilePage } from './pages/ProfilePage'
import { SettingsPage } from './pages/SettingsPage'
import { SurveysPage } from './pages/SurveysPage'

// Components
import { Layout } from './components/Layout'
import { LoadingScreen } from './components/LoadingScreen'

function App() {
  // Include `profile` in the destructured state so it's available in the JSX checks
  const { user, profile, isLoading, setUser, setProfile, setLoading } = useAuthStore()

  useEffect(() => {
    // Timeout safety - never stay loading forever
    const timeout = setTimeout(() => {
      console.log('Loading timeout - forcing to false')
      setLoading(false)
    }, 5000)

    // Check initial session
    supabase.auth.getSession().then(({ data: { session }, error }) => {
      clearTimeout(timeout)
      if (error) {
        console.error('Session error:', error)
        setLoading(false)
        return
      }
      setUser(session?.user ?? null)
      setApiToken(session?.access_token ?? null)
      if (session?.user) {
        fetchProfile(session.user.id)
      } else {
        setLoading(false)
      }
    }).catch(err => {
      clearTimeout(timeout)
      console.error('Failed to get session:', err)
      setLoading(false)
    })

    // Listen for auth changes
    const { data: { subscription } } = supabase.auth.onAuthStateChange(
      async (_event, session) => {
        setUser(session?.user ?? null)
        setApiToken(session?.access_token ?? null)
        if (session?.user) {
          await fetchProfile(session.user.id)
        } else {
          setProfile(null)
        }
        setLoading(false)
      }
    )

    return () => {
      clearTimeout(timeout)
      subscription.unsubscribe()
    }
  }, [setUser, setProfile, setLoading])

  const fetchProfile = async (authId: string) => {
    try {
      // Fetch profile from backend API instead of directly from Supabase
      const profile = await authApi.me()
      setProfile(profile as any)
    } catch (err: any) {
      console.error('Failed to fetch profile:', err)
      // Profile doesn't exist yet - user needs to register
      setProfile(null)
    } finally {
      setLoading(false)
    }
  }

  if (isLoading) {
    return <LoadingScreen />
  }

  return (
    <Routes>
      {/* Public routes */}
      <Route
        path="/login"
        element={user ? <Navigate to="/" replace /> : <LoginPage />}
      />
      <Route
        path="/register"
        element={user && !profile ? <RegisterPage /> : <Navigate to="/" replace />}
      />

      {/* Protected routes - require both user AND profile */}
      <Route
        element={
          user ? (
            profile ? (
              <Layout />
            ) : (
              <Navigate to="/register" replace />
            )
          ) : (
            <Navigate to="/login" replace />
          )
        }
      >
        <Route path="/" element={<HomePage />} />
        <Route path="/chat" element={<ChatPage />} />
        <Route path="/events" element={<EventsPage />} />
        <Route path="/events/:id" element={<EventDetailPage />} />
        <Route path="/surveys" element={<SurveysPage />} />
        <Route path="/team" element={<TeamPage />} />
        <Route path="/profile" element={<ProfilePage />} />
        <Route path="/settings" element={<SettingsPage />} />
      </Route>

      {/* Catch all */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
