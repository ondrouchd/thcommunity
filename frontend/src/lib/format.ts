const DATE_LOCALE: Record<string, string> = {
  cs: 'cs-CZ',
  en: 'en-US',
}

function resolveLocale(lang?: string): string {
  if (!lang) return 'cs-CZ'
  const base = lang.split('-')[0]
  return DATE_LOCALE[base] ?? 'cs-CZ'
}

export function formatDateTime(value: string, lang?: string): string {
  const locale = resolveLocale(lang)
  return new Date(value).toLocaleString(locale, {
    weekday: 'short',
    day: 'numeric',
    month: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function formatDate(value: string, lang?: string): string {
  const locale = resolveLocale(lang)
  return new Date(value).toLocaleDateString(locale, {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  })
}

export function formatTime(value: string, lang?: string): string {
  const locale = resolveLocale(lang)
  return new Date(value).toLocaleTimeString(locale, {
    hour: '2-digit',
    minute: '2-digit',
  })
}

/** Converts an ISO timestamp into a value usable by <input type="datetime-local">. */
export function toDateTimeLocal(value: string): string {
  const d = new Date(value)
  const offset = d.getTimezoneOffset()
  const local = new Date(d.getTime() - offset * 60000)
  return local.toISOString().slice(0, 16)
}
