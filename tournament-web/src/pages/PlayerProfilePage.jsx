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
import { PlayerWins } from '../components/PlayerWins';

function EloTooltip({ active, payload }) {
    if (!active || !payload || payload.length === 0) return null;
    const point = payload[0].payload;

    return (
        <div className="panel" style={{ padding: 12, minWidth: 200 }}>
            <div style={{ fontSize: 20, fontWeight: 700 }}>{point.rating}</div>
            <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>
                {new Date(point.time).toLocaleString('et-EE')}
            </div>
            {point.tournamentName && (
                <div style={{ marginTop: 6, fontSize: 13 }}>{point.tournamentName}</div>
            )}
            {point.change !== undefined && (
                <div
                    style={{
                        color: point.change >= 0 ? 'var(--success)' : 'var(--danger)',
                        fontWeight: 700,
                    }}
                >
                    {point.change >= 0 ? `+${point.change}` : point.change}
                </div>
            )}
        </div>
    );
}

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

    const gameItems = useMemo(
        () => history.filter((h) => h.game === selectedGame),
        [history, selectedGame]
    );

    const chartData = useMemo(() => {
        if (gameItems.length === 0) return [];

        const first = gameItems[0];
        const start = {
            time: new Date(first.createdAt).getTime() - 1,
            rating: first.oldRating,
            tournamentName: 'Algseis',
        };

        const points = gameItems.map((h) => ({
            time: new Date(h.createdAt).getTime(),
            rating: h.newRating,
            change: h.ratingChange,
            tournamentName: h.tournamentName || (h.tournamentId ? `Turniir #${h.tournamentId}` : null),
        }));

        return [start, ...points];
    }, [gameItems]);

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
            <PlayerWins wins={profile.wins} />
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

                        {chartData.length === 0 ? (
                            <p style={{ margin: 0, color: 'var(--text-muted)' }}>
                                Selles mängus pole veel reitingu muutusi.
                            </p>
                        ) : (
                            <div style={{ width: '100%', height: 300 }}>
                                <ResponsiveContainer>
                                    <LineChart data={chartData}>
                                        <CartesianGrid strokeDasharray="3 3" />
                                        <XAxis
                                            dataKey="time"
                                            type="number"
                                            domain={['dataMin', 'dataMax']}
                                            tickFormatter={(v) => new Date(v).toLocaleDateString('et-EE')}
                                        />
                                        <YAxis domain={['auto', 'auto']} />
                                        <Tooltip content={<EloTooltip />} />
                                        <Line
                                            type="monotone"
                                            dataKey="rating"
                                            stroke="#c6ff34"
                                            strokeWidth={2}
                                            dot={{ r: 3 }}
                                            activeDot={{ r: 6 }}
                                        />
                                    </LineChart>
                                </ResponsiveContainer>
                            </div>
                        )}
                    </div>

                    <div className="panel">
                        <h2 style={{ margin: '0 0 16px', fontSize: 18 }}>Mängude ajalugu</h2>

                        {gameItems.length === 0 ? (
                            <p style={{ margin: 0, color: 'var(--text-muted)' }}>
                                Selles kategoorias pole veel mänge.
                            </p>
                        ) : (
                            <div style={{ overflowX: 'auto' }}>
                                <table className="data-table">
                                    <thead>
                                        <tr>
                                            <th>Kuupäev</th>
                                            <th>Turniir</th>
                                            <th>Vana Elo</th>
                                            <th>Uus Elo</th>
                                            <th>Muutus</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {gameItems.map((h) => (
                                            <tr key={h.id}>
                                                <td>{new Date(h.createdAt).toLocaleString('et-EE')}</td>
                                                <td>
                                                    {h.tournamentId ? (
                                                        <Link to={`/tournaments/${h.tournamentId}`}>
                                                            {h.tournamentName || `Turniir #${h.tournamentId}`}
                                                        </Link>
                                                    ) : (
                                                        '—'
                                                    )}
                                                </td>
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