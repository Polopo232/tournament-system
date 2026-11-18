import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
    CartesianGrid,
    Line,
    LineChart,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis,
} from 'recharts';
import { apiClient } from '../api/client';
import { formatDiscipline } from '../utils/formatters';

export function PlayerProfilePage() {
    const { id } = useParams();
    const [profile, setProfile] = useState(null);
    const [history, setHistory] = useState([]);
    const [selectedGame, setSelectedGame] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        const load = async () => {
            try {
                setLoading(true);
                setError(null);

                const [profileRes, historyRes] = await Promise.all([
                    apiClient.get(`/api/players/${id}`),
                    apiClient.get(`/api/players/${id}/rating-history`),
                ]);

                setProfile(profileRes.data);
                setHistory(historyRes.data);
                setSelectedGame((prev) => prev ?? profileRes.data.ratings[0]?.game ?? null);
            } catch (err) {
                setError(err.response?.data?.detail || 'Mängija profiili ei leitud.');
            } finally {
                setLoading(false);
            }
        };

        load();
    }, [id]);

    const gameHistory = useMemo(() => {
        const items = history.filter((h) => h.game === selectedGame);
        if (items.length === 0) return [];

        const label = (value) => new Date(value).toLocaleDateString('et-EE');
        const first = items[0];

        return [
            { date: label(first.createdAt), rating: first.oldRating },
            ...items.map((h) => ({ date: label(h.createdAt), rating: h.newRating })),
        ];
    }, [history, selectedGame]);

    const selectedRating = profile?.ratings.find((r) => r.game === selectedGame)?.rating;

    if (loading) {
        return (
            <div className="container">
                <p style={{ color: 'var(--text-muted)' }}>Laadin profiili...</p>
            </div>
        );
    }

    if (error || !profile) {
        return (
            <div className="container">
                <Link to="/leaderboard" className="back-link">← Edetabel</Link>
                <div className="alert-error">{error || 'Mängija profiili ei leitud.'}</div>
            </div>
        );
    }

    return (
        <div className="container">
            <Link to="/leaderboard" className="back-link">← Edetabel</Link>

            <div className="panel" style={{ marginBottom: 20 }}>
                <span className="badge badge-discipline" style={{ marginBottom: 8 }}>
                    Mängija profiil
                </span>
                <h1 className="page-title">{profile.displayName}</h1>
            </div>

            {profile.ratings.length === 0 ? (
                <div className="panel">
                    <p style={{ margin: 0, color: 'var(--text-muted)' }}>Mängijal pole veel reitingut.</p>
                </div>
            ) : (
                <>
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, marginBottom: 20 }}>
                        {profile.ratings.map((r) => (
                            <button
                                key={r.game}
                                onClick={() => setSelectedGame(r.game)}
                                className={`btn ${selectedGame === r.game ? 'btn-primary' : 'btn-outline'}`}
                            >
                                {formatDiscipline(r.game)} · {r.rating}
                            </button>
                        ))}
                    </div>

                    <div className="panel" style={{ marginBottom: 20 }}>
                        <div className="info-box-label">{formatDiscipline(selectedGame)}</div>
                        <div style={{ fontSize: 32, fontWeight: 700, color: 'var(--primary)' }}>
                            {selectedRating}
                        </div>
                        <div style={{ color: 'var(--text-muted)' }}>Elo-reiting</div>
                    </div>

                    <div className="panel" style={{ marginBottom: 20 }}>
                        <h2 style={{ margin: '0 0 16px', fontSize: 18 }}>Elo graafik</h2>

                        {gameHistory.length === 0 ? (
                            <p style={{ margin: 0, color: 'var(--text-muted)' }}>
                                Selles mängus pole veel reitingu muutusi.
                            </p>
                        ) : (
                            <div style={{ width: '100%', height: 300 }}>
                                <ResponsiveContainer>
                                    <LineChart data={gameHistory}>
                                        <CartesianGrid strokeDasharray="3 3" />
                                        <XAxis dataKey="date" />
                                        <YAxis domain={['auto', 'auto']} />
                                        <Tooltip />
                                        <Line
                                            type="monotone"
                                            dataKey="rating"
                                            stroke="var(--primary)"
                                            strokeWidth={2}
                                            dot={{ r: 3 }}
                                        />
                                    </LineChart>
                                </ResponsiveContainer>
                            </div>
                        )}
                    </div>

                    <div className="panel">
                        <h2 style={{ margin: '0 0 16px', fontSize: 18 }}>Mängude ajalugu</h2>

                        {gameHistory.length <= 1 ? (
                            <p style={{ margin: 0, color: 'var(--text-muted)' }}>
                                Mänge selles kategoorias pole veel.
                            </p>
                        ) : (
                            <div style={{ overflowX: 'auto' }}>
                                <table className="data-table">
                                    <thead>
                                        <tr>
                                            <th>Kuupäev</th>
                                            <th>Vana Elo</th>
                                            <th>Uus Elo</th>
                                            <th>Muutus</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {history
                                            .filter((h) => h.game === selectedGame)
                                            .map((h) => (
                                                <tr key={h.id}>
                                                    <td>{new Date(h.createdAt).toLocaleString('et-EE')}</td>
                                                    <td>{h.oldRating}</td>
                                                    <td><strong>{h.newRating}</strong></td>
                                                    <td
                                                        style={{
                                                            color: h.ratingChange >= 0 ? 'var(--success)' : 'var(--danger)',
                                                            fontWeight: 700,
                                                        }}
                                                    >
                                                        {h.ratingChange >= 0 ? `+${h.ratingChange}` : h.ratingChange}
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