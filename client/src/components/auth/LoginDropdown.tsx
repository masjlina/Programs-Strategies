import { useState, useRef, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'

export function LoginDropdown() {
  const { user, signIn, signOut } = useAuth()
  const [isLoginOpen, setIsLoginOpen] = useState(false)
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()

  // Close dropdown on click outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsLoginOpen(false)
      }
    }
    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [])

  const handleLoginSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!email.trim() || !password.trim()) {
      setError("Заповніть всі поля")
      return
    }

    setError(null)
    setSubmitting(true)

    try {
      await signIn(email, password)
      setIsLoginOpen(false)
      setEmail("")
      setPassword("")
      navigate("/admin")
    } catch (err: any) {
      setError(err.message || "Неправильний email або пароль")
    } finally {
      setSubmitting(false)
    }
  }

  if (user) {
    return (
      <div className="user-profile">
        <span className="user-email" title={user.email}>
          {user.email}
        </span>
        <button
          onClick={signOut} 
          className="btn btn--ghost btn--sm btn-logout"
          style={{ padding: '6px 12px', fontSize: '13px', borderRadius: '8px' }}
        >
          Вийти
        </button>
      </div>
    )
  }

  return (
    <div className="login-dropdown-container" ref={dropdownRef} style={{ position: 'relative' }}>
      <button 
        onClick={() => {
          setIsLoginOpen(!isLoginOpen)
          setError(null)
        }}
        className={`btn ${isLoginOpen ? 'btn--primary' : 'btn--outline'} btn--sm btn-login`}
        style={{ padding: '6px 12px', fontSize: '13px', borderRadius: '8px' }}
      >
        Увійти
      </button>

      {isLoginOpen && (
        <div className="login-dropdown">
          <h3 className="login-dropdown__title">Вхід в систему</h3>
          {error && (
            <div className="login-dropdown__error" role="alert">
              {error}
            </div>
          )}
          <form className="login-dropdown__form" onSubmit={handleLoginSubmit}>
            <div className="login-dropdown__field">
              <label htmlFor="dropdown-email" className="login-dropdown__label">
                Електронна пошта
              </label>
              <input
                id="dropdown-email"
                className="login-dropdown__input"
                type="email"
                placeholder="admin@admin.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={submitting}
                required
              />
            </div>
            <div className="login-dropdown__field">
              <label htmlFor="dropdown-password" className="login-dropdown__label">
                Пароль
              </label>
              <input
                id="dropdown-password"
                className="login-dropdown__input"
                type="password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                disabled={submitting}
                required
              />
            </div>
            <button
              type="submit"
              className={`btn btn--primary login-dropdown__submit ${submitting ? "btn-loading" : ""}`}
              disabled={submitting}
            >
              {submitting ? "Вхід…" : "Увійти"}
            </button>
          </form>
        </div>
      )}
    </div>
  )
}
