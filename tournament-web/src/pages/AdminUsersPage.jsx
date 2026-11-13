import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { apiClient, getStoredUser } from '../api/client';

export function AdminUsersPage() {
    const currentUser = getStoredUser();
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [successMsg, setSuccessMsg] = useState(null);

    const loadUsers = async () => {
        try {
            setLoading(true);
            setError(null);
            const response = await apiClient.get('/api/users');
            const data = response.data;
            setUsers(Array.isArray(data) ? data : data.items || []);
        } catch {
            setError('Kasutajate nimekirja laadimine ebaõnnestus. Kontrolli administraatori õigusi.');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadUsers();
    }, []);

    const handleRoleChange = async (userId, newRole) => {
        try {
            setError(null);
            setSuccessMsg(null);
            await apiClient.put(`/api/users/${userId}/role`, { role: newRole });
            setSuccessMsg('Kasutaja roll on edukalt uuendatud!');
            await loadUsers();
        } catch (err) {
            setError(err.response?.data?.detail || 'Rolli muutmine ebaõnnestus.');
        }
    };

    const handleDeleteUser = async (userId, username) => {
        if (!window.confirm(`Kas oled kindel, et soovid kustutada kasutaja "${username}"?`)) return;
        try {
            setError(null);
            setSuccessMsg(null);
            await apiClient.delete(`/api/users/${userId}`);
            setSuccessMsg(`Kasutaja "${username}" on kustutatud.`);
            await loadUsers();
        } catch (err) {
            setError(err.response?.data?.detail || 'Kasutaja kustutamine ebaõnnestus.');
        }
    };

    return (
        <div className="container">
            <div className="page-header">
                <div>
                    <h1 className="page-title">Kasutajate haldus</h1>
                    <p className="page-subtitle">
                        Administraatori paneel: halda kasutajate rolle ja õigusi
                    </p>
                </div>
            </div>

            {successMsg && <div className="alert-success">{successMsg}</div>}
            {error && <div className="alert-error">{error}</div>}

            <div className="panel">
                {loading ? (
                    <p style={{ color: 'var(--text-muted)' }}>Laadin kasutajaid...</p>
                ) : users.length === 0 ? (
                    <p style={{ color: 'var(--text-muted)' }}>Kasutajaid ei leitud.</p>
                ) : (
                    <div style={{ overflowX: 'auto' }}>
                        <table className="data-table">
                            <thead>
                            <tr>
                                <th>ID</th>
                                <th>Kasutajanimi</th>
                                <th>E-post</th>
                                <th>Elo-reiting</th>
                                <th>Roll</th>
                                <th>Tegevused</th>
                            </tr>
                            </thead>
                            <tbody>
                            {users.map((u) => {
                                const isSelf = Number(u.id) === Number(currentUser?.id);
                                return (
                                    <tr key={u.id}>
                                        <td>#{u.id}</td>
                                        <td>
                                            <Link to={`/players/${u.id}`} style={{ fontWeight: 600 }}>
                                                {u.username}
                                            </Link>
                                        </td>
                                        <td>{u.email}</td>
                                        <td>
                                            <strong>{u.rating ?? u.eloRating ?? 1200}</strong>
                                        </td>
                                        <td>
                                            <select
                                                className="select"
                                                style={{ height: 34, width: 150 }}
                                                value={u.role}
                                                disabled={isSelf}
                                                onChange={(e) => handleRoleChange(u.id, e.target.value)}
                                            >
                                                <option value="Player">Player</option>
                                                <option value="Organizer">Organizer</option>
                                                <option value="Admin">Admin</option>
                                                <option value="Service">Service</option>
                                            </select>
                                        </td>
                                        <td>
                                            <button
                                                disabled={isSelf}
                                                onClick={() => handleDeleteUser(u.id, u.username)}
                                                className="btn btn-sm"
                                                style={{
                                                    background: 'var(--danger-soft)',
                                                    color: 'var(--danger)',
                                                }}
                                            >
                                                Kustuta
                                            </button>
                                        </td>
                                    </tr>
                                );
                            })}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>
        </div>
    );
}