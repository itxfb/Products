import { useLayoutEffect, useState, useSyncExternalStore } from 'react'

type Theme = 'light' | 'dark'

const storageKey = 'theme'
const darkScheme = window.matchMedia('(prefers-color-scheme: dark)')

function subscribeToScheme(onChange: () => void) {
  darkScheme.addEventListener('change', onChange)
  return () => darkScheme.removeEventListener('change', onChange)
}

function storedTheme(): Theme | null {
  const stored = localStorage.getItem(storageKey)
  return stored === 'light' || stored === 'dark' ? stored : null
}

export default function ThemeToggle() {
  const [chosen, setChosen] = useState(storedTheme)
  const systemDark = useSyncExternalStore(subscribeToScheme, () => darkScheme.matches)
  const theme: Theme = chosen ?? (systemDark ? 'dark' : 'light')
  const next: Theme = theme === 'dark' ? 'light' : 'dark'

  useLayoutEffect(() => {
    if (chosen) document.documentElement.dataset.theme = chosen
  }, [chosen])

  function toggle() {
    localStorage.setItem(storageKey, next)
    setChosen(next)
  }

  return (
    <button type="button" className="button icon" aria-label={`Switch to ${next} theme`} onClick={toggle}>
      <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true" focusable="false">
        {theme === 'dark' ? (
          <path
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            d="M12 4V2m0 20v-2m8-8h2M2 12h2m13.66-5.66 1.41-1.41M4.93 19.07l1.41-1.41m0-11.32L4.93 4.93m14.14 14.14-1.41-1.41M16 12a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"
          />
        ) : (
          <path fill="currentColor" d="M21 14.5A8.5 8.5 0 0 1 9.5 3a8.5 8.5 0 1 0 11.5 11.5Z" />
        )}
      </svg>
    </button>
  )
}
