import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './index.css'
import { AuthRbacProvider } from './contexts/AuthRbacContext'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <AuthRbacProvider>
      <App />
    </AuthRbacProvider>
  </React.StrictMode>,
)
