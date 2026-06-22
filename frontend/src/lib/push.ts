import { pushApi } from './api'

export function isPushSupported(): boolean {
  return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window
}

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
  const raw = window.atob(base64)
  const output = new Uint8Array(raw.length)
  for (let i = 0; i < raw.length; i++) {
    output[i] = raw.charCodeAt(i)
  }
  return output
}

function arrayBufferToBase64(buffer: ArrayBuffer | null): string {
  if (!buffer) return ''
  const bytes = new Uint8Array(buffer)
  let binary = ''
  bytes.forEach((b) => (binary += String.fromCharCode(b)))
  return window.btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

export async function getPushSubscription(): Promise<PushSubscription | null> {
  if (!isPushSupported()) return null
  const reg = await navigator.serviceWorker.getRegistration()
  if (!reg) return null
  return reg.pushManager.getSubscription()
}

export async function enablePush(): Promise<void> {
  if (!isPushSupported()) {
    throw new Error('unsupported')
  }

  const permission = await Notification.requestPermission()
  if (permission !== 'granted') {
    throw new Error('denied')
  }

  const { public_key } = await pushApi.getVapidKey()
  if (!public_key) {
    throw new Error('not-configured')
  }

  const reg = await navigator.serviceWorker.ready
  const subscription = await reg.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: urlBase64ToUint8Array(public_key),
  })

  const json = subscription.toJSON()
  await pushApi.subscribe({
    endpoint: subscription.endpoint,
    p256dh: json.keys?.p256dh ?? arrayBufferToBase64(subscription.getKey('p256dh')),
    auth: json.keys?.auth ?? arrayBufferToBase64(subscription.getKey('auth')),
  })
}

export async function disablePush(): Promise<void> {
  const subscription = await getPushSubscription()
  if (!subscription) return
  const endpoint = subscription.endpoint
  await subscription.unsubscribe()
  try {
    await pushApi.unsubscribe(endpoint)
  } catch {
    // best effort
  }
}
