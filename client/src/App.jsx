import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { MainLayout } from './layouts/MainLayout.jsx'
import { HomePage } from './pages/HomePage/HomePage.jsx'
import { SearchPage } from './pages/SearchPage/SearchPage.jsx'
import { StrategyPage } from './pages/StrategyPage/StrategyPage.jsx'
import { UploadPage } from './pages/UploadPage/UploadPage.jsx'
import './App.css'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<MainLayout />}>
          <Route index element={<HomePage />} />
          <Route path="search" element={<SearchPage />} />
          <Route path="strategies/:id" element={<StrategyPage />} />
          <Route path="upload" element={<UploadPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}

export default App
