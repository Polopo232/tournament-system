import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { apiClient } from '../api/client';
import { formatDiscipline } from '../utils/formatters';

const GAMES = ['Chess', 'CS2', 'Dota2', 'TableTennis', 'Chess (rapid)'];

export function LeaderboardPage() {
    const [game, setGame] = useState('Chess');
    const [search, setSearch] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const [page, setPage] = useState(1);
    const [data, setData] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    useEffect(() => {
        const timer = setTimeout(() => {
            setDebouncedSearch(search.trim());
            setPage(1);
        }, 300);
        return () => clearTimeout(timer);
    }, [search]);

    useEffect(() => {
        const load = async () => {
            try {
                setLoading(true);
                setError(null);

                const res = await apiClient.get('/api/players/leaderboard', {
                    params: {
                        game,
                        search: debouncedSearch || undefined,
                        page,
                        pageSize: 20,
                    },
                });

                setData(res.data);
            } catch (err) {
                setError(err.response?.data?.detail || 'Edetabeli laadimine ebaõnnestus.');
            } finally {
                setLoading(false);
            }
        };

        load();
    }, [game, debouncedSearch, page]);

    const changeGame = (value) => {
        setGame(value);
        setPage(1);
    };

    return (
        <div className="container">
            <div className="page-header">
                <div>
                    <h1 className="page-title">Edetabel</h1>
                    <p className="page-subtitle">Parimad mängijad valitud mängus Elo-reitingu järgi</p>
                </div>
            </div>

            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, marginBottom: 16 }}>
                {GAMES.map((g) => (
                    <button
                        key={g}
                        onClick={() => changeGame(g)}
                        className={`btn ${game === g ? 'btn-primary' : 'btn-outline'}`}
                    >
                        {formatDiscipline(g)}
                    </button>
                ))}
            </div>

            <div className="filter-bar">
                <input
                    type="text"
                    className="input"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    placeholder="Otsi nicki järgi..."
                />
            </div>

            {error && <div className="alert-error">{error}</div>}

            <div className="panel">
                {loading && !data ? (
                    <p style={{ color: 'var(--text-muted)' }}>Laadin edetabelit...</p>
                ) : !data || data.items.length === 0 ? (
                    <p style={{ margin: 0, color: 'var(--text-muted)' }}>
                        Selles mängus pole veel mängijaid.
                    </p>
                ) : (
                    <div style={{ overflowX: 'auto', opacity: loading ? 0.6 : 1 }}>
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>#</th>
                                    <th>Mängija</th>
                                    <th>Elo</th>
                                </tr>
                            </thead>
                            <tbody>
                                {data.items.map((row) => (
                                    <tr key={row.playerId}>
                                        <td>{row.rank}</td>
                                        <td>
                                            <Link to={`/players/${row.playerId}`} style={{ fontWeight: 600 }}>
                                                {row.displayName}
                                            </Link>
                                        </td>
                                        <td><strong>{row.rating}</strong></td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            {data && data.totalPages > 1 && (
                <div className="pagination">
                    <button
                        className="page-btn"
                        disabled={page <= 1}
                        onClick={() => setPage((p) => Math.max(1, p - 1))}
                    >
                        Eelmine
                    </button>
                    <span style={{ alignSelf: 'center', color: 'var(--text-muted)' }}>
                        {page} / {data.totalPages}
                    </span>
                    <button
                        className="page-btn"
                        disabled={page >= data.totalPages}
                        onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
                    >
                        Järgmine
                    </button>
                </div>
            )}
        </div>
    );
}