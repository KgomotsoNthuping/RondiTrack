import { useEffect, useState, type ReactNode } from 'react'
import { ThemeContext, type ThemeChoice } from '@/lib/theme-context'

const KEY = 'ronditrack-theme'
function storedTheme(): ThemeChoice {
  try {
    const value = window.localStorage.getItem(KEY)
    if (value === 'light' || value === 'dark' || value === 'system') return value
  } catch { /* Storage may be blocked by the browser. */ }
  return 'system'
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<ThemeChoice>(storedTheme)
  useEffect(() => {
    const media = window.matchMedia('(prefers-color-scheme: dark)')
    const update = () => document.documentElement.classList.toggle('dark', theme === 'dark' || (theme === 'system' && media.matches))
    update()
    media.addEventListener('change', update)
    return () => media.removeEventListener('change', update)
  }, [theme])
  const change = (next: ThemeChoice) => {
    setTheme(next)
    try { window.localStorage.setItem(KEY, next) } catch { /* Device storage unavailable. */ }
  }
  return <ThemeContext.Provider value={{ theme, setTheme: change }}>{children}</ThemeContext.Provider>
}
