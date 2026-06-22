import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface NotificationPrefs {
  eventCreated: boolean
  eventReminder: boolean
  spotOpened: boolean
}

interface SettingsState {
  notifications: NotificationPrefs
  setNotification: (key: keyof NotificationPrefs, value: boolean) => void
}

export const useSettingsStore = create<SettingsState>()(
  persist(
    (set) => ({
      notifications: {
        eventCreated: true,
        eventReminder: true,
        spotOpened: true,
      },
      setNotification: (key, value) =>
        set((state) => ({ notifications: { ...state.notifications, [key]: value } })),
    }),
    {
      name: 'settings-storage',
    }
  )
)
