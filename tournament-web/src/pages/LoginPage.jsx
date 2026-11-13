import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { apiClient, saveAuthSession } from '../api/client';

export function LoginPage() {
    const navigate = useNavigate();
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError(null);

        if (!username.trim() || !password.trim()) {
            setError('Palun täida nii kasutajanimi kui ka parool.');
            return;
        }

        try {
            setLoading(true);
            const response = await apiClient.post('/api/auth/login', {
                username: username.trim(),
                password,
            });

            saveAuthSession(response.data);
            navigate('/tournaments');
        } catch (err) {
            const detail = err.response?.data?.detail || 'Vale kasutajanimi või parool.';
            setError(detail);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="auth-wrapper">
            <div className="auth-card">
                <h1 className="auth-title">Tere tulemast tagasi</h1>
                <p className="auth-subtitle">Logi sisse, et liituda turniiridega või hallata mänge</p>

                {error && <div className="alert-error">{error}</div>}

                <form onSubmit={handleSubmit} noValidate>
                    <div className="form-group">
                        <label className="form-label">Kasutajanimi</label>
                        <input
                            type="text"
                            className="input"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            placeholder="nt. player1 või organizer"
                        />
                    </div>

                    <div className="form-group" style={{ marginBottom: 22 }}>
                        <label className="form-label">Parool</label>
                        <input
                            type="password"
                            className="input"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            placeholder="••••••••"
                        />
                    </div>

                    <button type="submit" disabled={loading} className="btn btn-primary" style={{ width: '100%', height: 42 }}>
                        {loading ? 'Sisselogimine...' : 'Logi sisse'}
                    </button>
                </form>

                <div className="auth-footer">
                    Pole veel kontot? <Link to="/register" style={{ fontWeight: 600 }}>Loo uus konto</Link>
                </div>
            </div>
        </div>
    );
}