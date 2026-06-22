import { Link, NavLink } from 'react-router-dom'
import { Container } from './Container'
import { useAuth } from '../../context/AuthContext'
import { LoginDropdown } from '../auth/LoginDropdown'

export function Header() {
  const { user } = useAuth()

  return (
    <header className="site-header">
      <Container className="site-header__inner">
        <div className="header-left">
          <Link className="brand" to="/">
            Є <span>рішення</span>
          </Link>
          <nav className="site-nav" aria-label="Головна навігація">
            <NavLink className="site-nav__link" to="/" end>
              Головна
            </NavLink>
            <NavLink className="site-nav__link" to="/search">
              Пошук
            </NavLink>
            <NavLink className="site-nav__link" to="/analytics">
              Аналітика
            </NavLink>
            {user && (
              <>
                <NavLink className="site-nav__link" to="/upload">
                  Завантаження
                </NavLink>
                <NavLink className="site-nav__link" to="/admin">
                  Адмін-панель
                </NavLink>
              </>
            )}
          </nav>
        </div>

        <div className="header-actions">
          <LoginDropdown />
        </div>
      </Container>
    </header>
  )
}
