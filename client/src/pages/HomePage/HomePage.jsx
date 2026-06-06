import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Container } from '../../components/layout/Container.jsx'
import './HomePage.css'

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ?? 'http://localhost:5257'

export function HomePage() {
  const [selectedFile, setSelectedFile] = useState(null)
  const [status, setStatus] = useState('idle')
  const [message, setMessage] = useState('')

  const handleFileChange = (event) => {
    setSelectedFile(event.target.files?.[0] ?? null)
    setStatus('idle')
    setMessage('')
  }

  const handleUpload = async (event) => {
    event.preventDefault()
    const form = event.currentTarget

    if (!selectedFile) {
      setStatus('error')
      setMessage('Оберіть JSON-файл для завантаження.')
      return
    }

    const formData = new FormData()
    formData.append('File', selectedFile)

    setStatus('loading')
    setMessage('')

    try {
      const response = await fetch(`${API_BASE_URL}/api/UploadFile`, {
        method: 'POST',
        body: formData,
      })

      if (!response.ok) {
        const payload = await response.json().catch(() => null)
        throw new Error(payload?.message ?? 'Не вдалося завантажити файл.')
      }

      setStatus('success')
      setMessage('Файл успішно завантажено та оброблено.')
      setSelectedFile(null)
      form.reset()
    } catch (error) {
      setStatus('error')
      setMessage(error.message)
    }
  }

  return (
    <main className="home">
      <section className="home-hero">
        <Container className="home-hero__inner">
          <p className="home-hero__kicker">Прозорість рішень місцевої влади</p>
          <h1 className="home-hero__title">Вітаємо</h1>
          <p className="home-hero__text">
            «Є рішення» допомагає знаходити та переглядати стратегії розвитку міст у
            зручному вигляді — з посиланням на документ і офіційне джерело.
          </p>
          <Link className="btn btn--primary" to="/search">
            Перейти до пошуку
          </Link>
        </Container>
      </section>

      <section id="about" className="home-section">
        <Container>
          <h2>Про сайт</h2>
          <p className="muted">
            Сервіс збирає стратегії та плани розвитку по містах. Зараз у базі — демо-записи;
            повний каталог міст додаватиметься поступово.
          </p>
        </Container>
      </section>
    </main>
  )
}
