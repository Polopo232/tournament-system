import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { apiClient, getStoredUser } from '../api/client';

export function PlayerProfilePage() {
    const { id } = useParams();
    const [history, setHistory] = useState([]);
    const [playerInfo, setPlayerInfo] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        const fetchHistory = async () => {
            try {
                setLoading(true);
                setError(null);
                const response = await apiClient.get(`/api/players/${id}/rating-history`);
                const data = response.data;

                if (Array.isArray(data)) {
                    setHistory(data);
                } else {
                    setPlayerInfo(data);
                    setHistory(data.history || data.items || []);
                }
            } catch (err) {
                const currentUser = getStoredUser();
                if (currentUser && Number(currentUser.id) === Number(id)) {
                    setPlayerInfo({
                        username: currentUser.username,
                        rating: 1200,
                    });
                    setHistory([]);
                } else {
                    setError(err.response?.data?.detail || 'Mängija profiili ei leitud.');
                }
            } finally {
                setLoading(false);
            }
        };

        fetchHistory();
    }, [id]);

    const currentElo =
        playerInfo?.rating ??
        playerInfo?.eloRating ??
        (history.length > 0
            ? history[history.length - 1].newRating ?? history[history.length - 1].ratingAfter
            : 1200);

    const playerName = playerInfo?.username || history[0]?.playerName || `Mängija #${id}`;

    return (
        <div className="container">
            <Link to="/tournaments" className="back-link">
                ← Tagasi turniiride juurde
            </Link>

            <div
                className="panel"
                style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    flexWrap: 'wrap',
                    gap: 16,
                }}
            >
                <div>
          <span className="badge badge-discipline" style={{ marginBottom: 8 }}>
            Mängija profiil
          </span>
                    <h1 className="page-title">{playerName}</h1>
                </div>

                <div className="info-box" style={{ minWidth: 180 }}>
                    <div className="info-box-label">Praegune Elo-reiting</div>
                    <div className="info-box-value" style={{ fontSize: 24, color: 'var(--primary)' }}>
                        {currentElo}
                    </div>
                </div>
            </div>

            <div className="panel">
                <h2 style={{ margin: '0 0 16px', fontSize: 18 }}>Elo-reitingu ajalugu</h2>

                {error && <div className="alert-error">{error}</div>}

                {loading ? (
                    <p style={{ color: 'var(--text-muted)' }}>Laadin ajalugu...</p>
                ) : history.length === 0 ? (
                    <p style={{ margin: 0, color: 'var(--text-muted)' }}>
                        Sellel mängijal pole veel Elo-reitingu muudatusi.
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
                            {history.map((item, idx) => {
                                const oldR = item.oldRating ?? item.ratingBefore ?? '-';
                                const newR = item.newRating ?? item.ratingAfter ?? '-';
                                const diff =
                                    typeof oldR === 'number' && typeof newR === 'number'
                                        ? newR - oldR
                                        : item.change ?? 0;
                                const dateStr = item.recordedAt || item.createdAt || item.date;

                                return (
                                    <tr key={item.id || idx}>
                                        <td>{dateStr ? new Date(dateStr).toLocaleString('et-EE') : '-'}</td>
                                        <td>{oldR}</td>
                                        <td>
                                            <strong>{newR}</strong>
                                        </td>
                                        <td
                                            style={{
                                                color: diff >= 0 ? 'var(--success)' : 'var(--danger)',
                                                fontWeight: 700,
                                            }}
                                        >
                                            {diff >= 0 ? `+${diff}` : diff}
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