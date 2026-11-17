import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { apiClient, getStoredUser } from '../api/client';
import { formatDiscipline } from '../utils/formatters';

export function PlayerProfilePage() {
    const { id } = useParams();
    const [ratings, setRatings] = useState([]);
    const [history, setHistory] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        const load = async () => {
            try {
                setLoading(true);
                setError(null);

                const [ratingsRes, historyRes] = await Promise.all([
                    apiClient.get(`/api/players/${id}/ratings`),
                    apiClient.get(`/api/players/${id}/rating-history`),
                ]);

                setRatings(ratingsRes.data);
                setHistory(historyRes.data);
            } catch (err) {
                setError(err.response?.data?.detail || 'Mängija profiili ei leitud.');
            } finally {
                setLoading(false);
            }
        };

        load();
    }, [id]);

    const currentUser = getStoredUser();
    const isMe = currentUser && Number(currentUser.id) === Number(id);
    const playerName = isMe ? currentUser.displayName : `Mängija #${id}`;

    return (
        <div className="container">
            <Link to="/tournaments" className="back-link">
                ← Tagasi turniiride juurde
            </Link>

            <div className="panel" style={{ marginBottom: 20 }}>
                <span className="badge badge-discipline" style={{ marginBottom: 8 }}>
                    Mängija profiil
                </span>
                <h1 className="page-title">{playerName}</h1>
            </div>

            {error && <div className="alert-error">{error}</div>}

            {loading ? (
                <p style={{ color: 'var(--text-muted)' }}>Laadin profiili...</p>
            ) : (
                <>
                    <div className="cards-grid" style={{ marginBottom: 20 }}>
                        {ratings.length === 0 ? (
                            <div className="panel">
                                <p style={{ margin: 0, color: 'var(--text-muted)' }}>
                                    Mängijal pole veel reitingut.
                                </p>
                            </div>
                        ) : (
                            ratings.map((r) => (
                                <div key={r.game} className="panel">
                                    <span className="badge badge-discipline">{formatDiscipline(r.game)}</span>
                                    <div style={{ fontSize: 28, fontWeight: 700, color: 'var(--primary)', marginTop: 8 }}>
                                        {r.rating}
                                    </div>
                                    <div style={{ color: 'var(--text-muted)' }}>Elo-reiting</div>
                                </div>
                            ))
                        )}
                    </div>

                    <div className="panel">
                        <h2 style={{ margin: '0 0 16px', fontSize: 18 }}>Elo-reitingu ajalugu</h2>

                        {history.length === 0 ? (
                            <p style={{ margin: 0, color: 'var(--text-muted)' }}>
                                Sellel mängijal pole veel Elo-reitingu muudatusi.
                            </p>
                        ) : (
                            <div style={{ overflowX: 'auto' }}>
                                <table className="data-table">
                                    <thead>
                                        <tr>
                                            <th>Mäng</th>
                                            <th>Kuupäev</th>
                                            <th>Vana Elo</th>
                                            <th>Uus Elo</th>
                                            <th>Muutus</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {history.map((item) => (
                                            <tr key={item.id}>
                                                <td>{formatDiscipline(item.game)}</td>
                                                <td>{new Date(item.createdAt).toLocaleString('et-EE')}</td>
                                                <td>{item.oldRating}</td>
                                                <td>
                                                    <strong>{item.newRating}</strong>
                                                </td>
                                                <td
                                                    style={{
                                                        color: item.ratingChange >= 0 ? 'var(--success)' : 'var(--danger)',
                                                        fontWeight: 700,
                                                    }}
                                                >
                                                    {item.ratingChange >= 0 ? `+${item.ratingChange}` : item.ratingChange}
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </div>
                </>
            )}
        </div>
    );
}