import { Monitor, Moon, Sun } from 'lucide-react'
import { useTheme } from '@/lib/useTheme'

export function ThemeSwitcher() {
  const { theme, setTheme } = useTheme()
  return <label className="flex min-h-11 items-center gap-2 text-sm font-semibold text-lime-950">
    {theme === 'dark' ? <Moon className="size-4" aria-hidden /> : theme === 'light' ? <Sun className="size-4" aria-hidden /> : <Monitor className="size-4" aria-hidden />}
    <span className="sr-only sm:not-sr-only">Theme</span>
    <select aria-label="Colour theme" value={theme} onChange={e => setTheme(e.target.value as 'light' | 'dark' | 'system')}
      className="min-h-11 max-w-full rounded-lg border border-lime-950/30 bg-lime-100 px-2 text-lime-950 focus-visible:ring-2 focus-visible:ring-lime-950">
      <option value="light">Light</option><option value="dark">Dark</option><option value="system">System</option>
    </select>
  </label>
}
