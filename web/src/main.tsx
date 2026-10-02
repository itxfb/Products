import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { AuthProvider } from 'react-oidc-context'
import { ApiError } from './api.ts'
import App from './App.tsx'
import './index.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (failureCount, error) => !(error instanceof ApiError && error.status < 500) && failureCount < 3,
    },
  },
})

const clearSigninParameters = () => window.history.replaceState(null, '', window.location.pathname)

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider
      authority={import.meta.env.VITE_OIDC_AUTHORITY || 'http://localhost:8080/realms/products'}
      client_id={import.meta.env.VITE_OIDC_CLIENT_ID || 'products-web'}
      redirect_uri={window.location.origin}
      post_logout_redirect_uri={window.location.origin}
      onSigninCallback={clearSigninParameters}
    >
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </AuthProvider>
  </StrictMode>,
)
