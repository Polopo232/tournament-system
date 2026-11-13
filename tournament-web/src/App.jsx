import { BrowserRouter, Link, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import { clearAuthSession, getStoredUser } from './api/client';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { TournamentsPage } from './pages/TournamentsPage';

function Navbar() {
    const navigate = useNavigate();
    const user = getStoredUser();

    const handleLogout = () => {
        clearAuthSession();
        navigate('/login');
    };

    return (
        <nav style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 24px', borderBottom: '1px solid #e5e7eb' }}>
            <div style={{ display: 'flex', gap: 16, alignItems: 'center' }}>
                <strong>Tournament System</strong>
                <Link to="/tournaments">Turniirid</Link>
            </div>

            <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
                {user ? (
                    <>
                        <span>Tere, <strong>{user.username}</strong> ({user.role})</span>
                        <button onClick={handleLogout} style={{ padding: '4px 10px', cursor: 'pointer' }}>
                            Logi välja
                        </button>
                    </>
                ) : (
                    <>
                        <Link to="/login">Logi sisse</Link>
                        <Link to="/register">Registreeru</Link>
                    </>
                )}
            </div>
        </nav>
    );
}

export function App() {
    return (
        <BrowserRouter>
            <Navbar />
            <Routes>
                <Route path="/" element={<Navigate to="/tournaments" replace />} />
                <Route path="/tournaments" element={<TournamentsPage />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;