import type { Metadata } from 'next'
import type { ReactNode } from 'react'
import './globals.css'

export const metadata: Metadata = {
  title: 'RondiTrack',
  description:
    'Stokvel membership, contributions and financial tracking.',
}

export default function RootLayout({
  children,
}: {
  children: ReactNode
}) {
  return (
    <html lang="en-ZA">
      <body className="min-h-screen bg-slate-50 text-slate-950 antialiased">
        {children}
      </body>
    </html>
  )
}
