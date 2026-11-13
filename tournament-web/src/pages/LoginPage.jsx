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
        <div style={{ maxWidth: 400, margin: '40px auto', padding: 24, border: '1px solid #ddd', borderRadius: 8 }}>
            <h2>Logi sisse</h2>

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
                        placeholder="nt. player1 või organizer"
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
                    {loading ? 'Sisselogimine...' : 'Logi sisse'}
                </button>
            </form>

            <p style={{ marginTop: 16, textAlign: 'center' }}>
                Pole veel kontot? <Link to="/register">Registreeru</Link>
            </p>
        </div>
    );
}