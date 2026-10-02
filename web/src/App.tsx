import { useAuth } from 'react-oidc-context'
import ProductForm from './ProductForm.tsx'
import ProductList from './ProductList.tsx'
import ThemeToggle from './ThemeToggle.tsx'

export default function App() {
  const auth = useAuth()

  return (
    <>
      <header className="topbar">
        <span className="wordmark">Products</span>
        <div className="actions">
          {auth.isAuthenticated && <span className="user">{auth.user?.profile.preferred_username}</span>}
          <ThemeToggle />
          {auth.isAuthenticated && (
            <button type="button" className="button" onClick={() => void auth.signoutRedirect()}>
              Sign out
            </button>
          )}
        </div>
      </header>
      {auth.isLoading ? (
        <main className="landing">
          <p className="status" role="status">
            Loading…
          </p>
        </main>
      ) : auth.isAuthenticated ? (
        <main className="layout">
          <ProductForm />
          <ProductList />
        </main>
      ) : (
        <main className="landing">
          <h1>
            Every product.
            <br />
            Every colour.
          </h1>
          <p>Browse the catalogue, filter it by colour and add new products.</p>
          {auth.error && (
            <p className="alert" role="alert">
              {auth.error.message}
            </p>
          )}
          <button type="button" className="button primary large" onClick={() => void auth.signinRedirect()}>
            Sign in
          </button>
        </main>
      )}
    </>
  )
}
