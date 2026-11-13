import { BrowserRouter, Link, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import { clearAuthSession, getStoredUser } from './api/client';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { TournamentsPage } from './pages/TournamentsPage';
import { TournamentDetailPage } from './pages/TournamentDetailPage';
import { PlayerProfilePage } from './pages/PlayerProfilePage';
import { AdminUsersPage } from './pages/AdminUsersPage';

function Navbar() {
    const navigate = useNavigate();
    const user = getStoredUser();
    const isAdmin = (user?.role || '').toLowerCase() === 'admin';
    const isPlayer = (user?.role || '').toLowerCase() === 'player';

    const handleLogout = () => {
        clearAuthSession();
        navigate('/login');
    };

    return (
        <header className="navbar">
            <div className="navbar-inner">
                <div className="navbar-left">
                    <Link to="/tournaments" className="brand-logo">
                        <span>ArenaHub</span>
                        <span className="brand-tag">PRO</span>
                    </Link>
                    <Link to="/tournaments" className="nav-item">
                        Turniirid
                    </Link>
                    {isAdmin && (
                        <Link to="/admin/users" className="nav-item">
                            Kasutajad
                        </Link>
                    )}
                </div>

                <div className="navbar-right">
                    {user ? (
                        <>
                            {isPlayer && user.id ? (
                                <Link
                                    to={`/players/${user.id}`}
                                    className="user-chip"
                                    style={{ textDecoration: 'none' }}
                                    title="Ava minu profiil"
                                >
                                    <strong>{user.username}</strong>
                                    <span className="role-badge">{user.role}</span>
                                </Link>
                            ) : (
                                <div className="user-chip">
                                    <strong>{user.username}</strong>
                                    <span className="role-badge">{user.role}</span>
                                </div>
                            )}
                            <button onClick={handleLogout} className="btn btn-dark-ghost btn-sm">
                                Logi välja
                            </button>
                        </>
                    ) : (
                        <>
                            <Link to="/login" className="nav-item">
                                Logi sisse
                            </Link>
                            <Link to="/register" className="btn btn-primary btn-sm">
                                Registreeru
                            </Link>
                        </>
                    )}
                </div>
            </div>
        </header>
    );
}

export function App() {
    return (
        <BrowserRouter>
            <Navbar />
            <Routes>
                <Route path="/" element={<Navigate to="/tournaments" replace />} />
                <Route path="/tournaments" element={<TournamentsPage />} />
                <Route path="/tournaments/:id" element={<TournamentDetailPage />} />
                <Route path="/players/:id" element={<PlayerProfilePage />} />
                <Route path="/admin/users" element={<AdminUsersPage />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;