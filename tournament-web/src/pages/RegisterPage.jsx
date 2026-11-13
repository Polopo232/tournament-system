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
        <div style={{ maxWidth: 400, margin: '40px auto', padding: 24, border: '1px solid #ddd', borderRadius: 8 }}>
            <h2>Loo konto</h2>

            {error && (
                <div style={{ background: '#fee2e2', color: '#991b1b', padding: 10, borderRadius: 6, marginBottom: 16 }}>
                    {error}
                </div>
            )}

            <form onSubmit={handleSubmit} noValidate>
                <div style={{ marginBottom: 12 }}>
                    <label style={{ display: 'block', marginBottom: 4 }}>Kasutajanimi</label>
                    <input
                        type="text"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                        style={{ width: '100%', padding: 8, boxSizing: 'border-box' }}
                    />
                </div>

                <div style={{ marginBottom: 12 }}>
                    <label style={{ display: 'block', marginBottom: 4 }}>E-post</label>
                    <input
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        placeholder="nimi@naide.ee"
                        style={{ width: '100%', padding: 8, boxSizing: 'border-box' }}
                    />
                </div>

                <div style={{ marginBottom: 16 }}>
                    <label style={{ display: 'block', marginBottom: 4 }}>Parool</label>
                    <input
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        style={{ width: '100%', padding: 8, boxSizing: 'border-box' }}
                    />
                </div>

                <button type="submit" disabled={loading} style={{ width: '100%', padding: 10, cursor: 'pointer' }}>
                    {loading ? 'Registreerimine...' : 'Registreeru'}
                </button>
            </form>

            <p style={{ marginTop: 16, textAlign: 'center' }}>
                Konto on juba olemas? <Link to="/login">Logi sisse</Link>
            </p>
        </div>
    );
}