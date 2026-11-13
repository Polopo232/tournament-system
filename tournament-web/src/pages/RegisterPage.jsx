import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { apiClient, saveAuthSession } from '../api/client';

export function RegisterPage() {
    const navigate = useNavigate();
    const [username, setUsername] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(false);

    const isValidEmail = (value) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError(null);

        if (!username.trim() || !email.trim() || !password.trim()) {
            setError('Kõik väljad on kohustuslikud.');
            return;
        }

        if (!isValidEmail(email.trim())) {
            setError('Palun sisesta korrektne e-posti aadress.');
            return;
        }

        if (password.length < 6) {
            setError('Parool peab olema vähemalt 6 tähemärki pikk.');
            return;
        }

        try {
            setLoading(true);
            const response = await apiClient.post('/api/auth/register', {
                username: username.trim(),
                email: email.trim(),
                password,
            });

            saveAuthSession(response.data);
            navigate('/tournaments');
        } catch (err) {
            const detail = err.response?.data?.detail || 'Registreerimine ebaõnnestus.';
            setError(detail);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="auth-wrapper">
            <div className="auth-card">
                <h1 className="auth-title">Loo mängijakonto</h1>
                <p className="auth-subtitle">Alusta oma teekonda ja kogu Elo-reitingut</p>

                {error && <div className="alert-error">{error}</div>}

                <form onSubmit={handleSubmit} noValidate>
                    <div className="form-group">
                        <label className="form-label">Kasutajanimi</label>
                        <input
                            type="text"
                            className="input"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            placeholder="Vali unikaalne nimi"
                        />
                    </div>

                    <div className="form-group">
                        <label className="form-label">E-posti aadress</label>
                        <input
                            type="email"
                            className="input"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            placeholder="nimi@naide.ee"
                        />
                    </div>

                    <div className="form-group" style={{ marginBottom: 22 }}>
                        <label className="form-label">Parool (vähemalt 6 tähemärki)</label>
                        <input
                            type="password"
                            className="input"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            placeholder="••••••••"
                        />
                    </div>

                    <button type="submit" disabled={loading} className="btn btn-primary" style={{ width: '100%', height: 42 }}>
                        {loading ? 'Konto loomine...' : 'Registreeru'}
                    </button>
                </form>

                <div className="auth-footer">
                    Konto on juba olemas? <Link to="/login" style={{ fontWeight: 600 }}>Logi sisse</Link>
                </div>
            </div>
        </div>
    );
}