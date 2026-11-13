import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { apiClient } from '../api/client';

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
            } catch {
                setError('Mängija Elo ajaloo laadimine ebaõnnestus.');
            } finally {
                setLoading(false);
            }
        };

        fetchHistory();
    }, [id]);

    const currentElo =
        playerInfo?.eloRating ??
        (history.length > 0 ? history[history.length - 1].newRating ?? history[history.length - 1].ratingAfter : 1200);

    const playerName = playerInfo?.username || history[0]?.playerName || `Mängija #${id}`;

    return (
        <div style={{ maxWidth: 800, margin: '24px auto', padding: '0 16px' }}>
            <Link to="/tournaments">← Tagasi turniiride juurde</Link>

            <div style={{ marginTop: 16, padding: 20, border: '1px solid #ddd', borderRadius: 8, background: '#f8fafc' }}>
                <h1 style={{ margin: '0 0 8px 0' }}>{playerName}</h1>
                <p style={{ margin: 0, fontSize: 18 }}>
                    Praegune Elo-reiting: <strong>{currentElo}</strong>
                </p>
            </div>

            <h2 style={{ marginTop: 28 }}>Elo-reitingu ajalugu</h2>

            {error && (
                <div style={{ background: '#fee2e2', color: '#991b1b', padding: 10, borderRadius: 6, marginBottom: 16 }}>
                    {error}
                </div>
            )}

            {loading ? (
                <p>Laadin ajalugu...</p>
            ) : history.length === 0 ? (
                <p>Sellel mängijal pole veel Elo-reitingu muudatusi.</p>
            ) : (
                <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: 12 }}>
                    <thead>
                    <tr style={{ background: '#f1f5f9', textAlign: 'left' }}>
                        <th style={{ padding: 10, border: '1px solid #cbd5e1' }}>Kuupäev</th>
                        <th style={{ padding: 10, border: '1px solid #cbd5e1' }}>Vana Elo</th>
                        <th style={{ padding: 10, border: '1px solid #cbd5e1' }}>Uus Elo</th>
                        <th style={{ padding: 10, border: '1px solid #cbd5e1' }}>Muutus</th>
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
                                <td style={{ padding: 10, border: '1px solid #cbd5e1' }}>
                                    {dateStr ? new Date(dateStr).toLocaleString('et-EE') : '-'}
                                </td>
                                <td style={{ padding: 10, border: '1px solid #cbd5e1' }}>{oldR}</td>
                                <td style={{ padding: 10, border: '1px solid #cbd5e1' }}>
                                    <strong>{newR}</strong>
                                </td>
                                <td
                                    style={{
                                        padding: 10,
                                        border: '1px solid #cbd5e1',
                                        color: diff >= 0 ? '#15803d' : '#b91c1c',
                                        fontWeight: 'bold',
                                    }}
                                >
                                    {diff >= 0 ? `+${diff}` : diff}
                                </td>
                            </tr>
                        );
                    })}
                    </tbody>
                </table>
            )}
        </div>
    );
}