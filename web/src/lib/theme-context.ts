import { createContext } from 'react'
export type ThemeChoice = 'light' | 'dark' | 'system'
export type ThemeContextValue = { theme: ThemeChoice; setTheme: (choice: ThemeChoice) => void }
export const ThemeContext = createContext<ThemeContextValue | null>(null)
