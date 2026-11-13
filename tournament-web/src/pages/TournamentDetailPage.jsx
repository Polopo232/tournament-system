import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { apiClient, getStoredToken, getStoredUser } from '../api/client';

export function TournamentDetailPage() {
    const { id } = useParams();
    const tournamentId = Number(id);
    const user = getStoredUser();

    const [tournament, setTournament] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [actionMsg, setActionMsg] = useState(null);

    const [selectedMatch, setSelectedMatch] = useState(null);
    const [score1, setScore1] = useState(0);
    const [score2, setScore2] = useState(0);
    const [submittingScore, setSubmittingScore] = useState(false);

    const loadTournament = useCallback(async () => {
        try {
            setError(null);
            const response = await apiClient.get(`/api/tournaments/${tournamentId}`);
            setTournament(response.data);
        } catch {
            setError('Turniiri andmete laadimine ebaõnnestus.');
        } finally {
            setLoading(false);
        }
    }, [tournamentId]);

    useEffect(() => {
        loadTournament();
    }, [loadTournament]);

    useEffect(() => {
        const token = getStoredToken();
        if (!token || !tournamentId) return;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl('http://localhost:5000/ws/tournament', {
                accessTokenFactory: () => token,
            })
            .withAutomaticReconnect()
            .build();

        connection.on('MatchResultSaved', (payload) => {
            const updatedMatch = payload?.match || payload?.Match;
            if (!updatedMatch) return;

            setTournament((prev) => {
                if (!prev || !prev.matches) return prev;
                return {
                    ...prev,
                    matches: prev.matches.map((m) =>
                        m.id === updatedMatch.id ? { ...m, ...updatedMatch } : m
                    ),
                };
            });
        });

        connection.on('RoundCreated', () => {
            loadTournament();
        });

        connection
            .start()
            .then(() => connection.invoke('JoinTournament', tournamentId))
            .catch((err) => console.error('SignalR ühenduse viga:', err));

        return () => {
            if (connection.state === signalR.HubConnectionState.Connected) {
                connection.invoke('LeaveTournament', tournamentId).catch(() => {});
            }
            connection.stop();
        };
    }, [tournamentId, loadTournament]);

    const handleRegister = async () => {
        try {
            setActionMsg(null);
            setError(null);
            await apiClient.post(`/api/tournaments/${tournamentId}/registrations`);
            setActionMsg('Oled edukalt turniirile registreeritud!');
            await loadTournament();
        } catch (err) {
            setError(err.response?.data?.detail || 'Registreerimine ebaõnnestus.');
        }
    };

    const handleGenerateBracket = async () => {
        try {
            setActionMsg(null);
            setError(null);
            await apiClient.post(`/api/tournaments/${tournamentId}/bracket`);
            setActionMsg('Turniiritabel on edukalt loodud!');
            await loadTournament();
        } catch (err) {
            setError(err.response?.data?.detail || 'Turniiritabeli loomine ebaõnnestus.');
        }
    };

    const openScoreModal = (match) => {
        setSelectedMatch(match);
        setScore1(match.player1Score ?? 0);
        setScore2(match.player2Score ?? 0);
    };

    const handleSaveResult = async (e) => {
        e.preventDefault();
        if (!selectedMatch) return;

        try {
            setSubmittingScore(true);
            setError(null);
            await apiClient.put(`/api/matches/${selectedMatch.id}/result`, {
                player1Score: Number(score1),
                player2Score: Number(score2),
            });
            setSelectedMatch(null);
            await loadTournament();
        } catch (err) {
            setError(err.response?.data?.detail || 'Tulemuse salvestamine ebaõnnestus.');
        } finally {
            setSubmittingScore(false);
        }
    };

    if (loading) return <p style={{ padding: 24 }}>Laadin turniiri...</p>;
    if (!tournament) return <p style={{ padding: 24 }}>Turniiri ei leitud.</p>;

    const participants = tournament.participants || tournament.registrations || [];
    const matches = tournament.matches || [];
    const isOrganizer = user?.role === 'Organizer' || user?.role === 'Admin';
    const isPlayer = user?.role === 'Player';

    const roundsMap = matches.reduce((acc, match) => {
        const r = match.round || 1;
        if (!acc[r]) acc[r] = [];
        acc[r].push(match);
        return acc;
    }, {});
    const roundNumbers = Object.keys(roundsMap)
        .map(Number)
        .sort((a, b) => a - b);

    return (
        <div style={{ maxWidth: 1050, margin: '24px auto', padding: '0 16px' }}>
            <Link to="/tournaments">← Tagasi turniiride nimekirja</Link>

            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 12 }}>
                <div>
                    <h1 style={{ margin: '4px 0' }}>{tournament.name}</h1>
                    <p style={{ margin: 0, color: '#555' }}>
                        Ala: <strong>{tournament.discipline}</strong> | Formaat: <strong>{tournament.format}</strong> | Staatus: <strong>{tournament.status}</strong>
                    </p>
                </div>

                <div style={{ display: 'flex', gap: 10 }}>
                    {isPlayer && tournament.status === 'Open' && (
                        <button onClick={handleRegister} style={{ padding: '8px 16px', background: '#2563eb', color: '#fff', border: 'none', borderRadius: 6, cursor: 'pointer' }}>
                            Registreeru turniirile
                        </button>
                    )}

                    {isOrganizer && tournament.status === 'Open' && matches.length === 0 && (
                        <button onClick={handleGenerateBracket} style={{ padding: '8px 16px', background: '#059669', color: '#fff', border: 'none', borderRadius: 6, cursor: 'pointer' }}>
                            Genereeri turniiritabel
                        </button>
                    )}
                </div>
            </div>

            {actionMsg && (
                <div style={{ background: '#dcfce7', color: '#166534', padding: 10, borderRadius: 6, marginTop: 16 }}>
                    {actionMsg}
                </div>
            )}

            {error && (
                <div style={{ background: '#fee2e2', color: '#991b1b', padding: 10, borderRadius: 6, marginTop: 16 }}>
                    {error}
                </div>
            )}

            <section style={{ marginTop: 28 }}>
                <h2>Osalejad ({participants.length} / {tournament.maxParticipants})</h2>
                {participants.length === 0 ? (
                    <p>Osalejaid pole veel registreerunud.</p>
                ) : (
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
                        {participants.map((p) => {
                            const playerId = p.playerId || p.userId || p.id;
                            const playerName = p.username || p.playerName || `Mängija #${playerId}`;
                            const elo = p.eloRating ?? p.rating;
                            return (
                                <Link
                                    key={playerId}
                                    to={`/players/${playerId}`}
                                    style={{
                                        padding: '6px 12px',
                                        border: '1px solid #d1d5db',
                                        borderRadius: 6,
                                        textDecoration: 'none',
                                        color: '#111',
                                        background: '#f9fafb',
                                    }}
                                >
                                    <strong>{playerName}</strong> {elo !== undefined ? `(Elo: ${elo})` : ''}
                                </Link>
                            );
                        })}
                    </div>
                )}
            </section>

            <section style={{ marginTop: 32 }}>
                <h2>Turniiritabel</h2>
                {roundNumbers.length === 0 ? (
                    <p>Turniiritabel pole veel genereeritud.</p>
                ) : (
                    <div style={{ display: 'flex', gap: 20, overflowX: 'auto', paddingBottom: 12 }}>
                        {roundNumbers.map((roundNum) => (
                            <div
                                key={roundNum}
                                style={{ minWidth: 280, background: '#f8fafc', border: '1px solid #e2e8f0', borderRadius: 8, padding: 14 }}
                            >
                                <h3 style={{ marginTop: 0 }}>Voor {roundNum}</h3>
                                <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                                    {roundsMap[roundNum].map((m) => (
                                        <div
                                            key={m.id}
                                            style={{ background: '#fff', border: '1px solid #cbd5e1', borderRadius: 6, padding: 12 }}
                                        >
                                            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
                        <span>
                          {m.player1Id ? (
                              <Link to={`/players/${m.player1Id}`}>{m.player1Name || `Mängija #${m.player1Id}`}</Link>
                          ) : (
                              'TBD'
                          )}
                        </span>
                                                <strong>{m.player1Score ?? '-'}</strong>
                                            </div>

                                            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                        <span>
                          {m.player2Id ? (
                              <Link to={`/players/${m.player2Id}`}>{m.player2Name || `Mängija #${m.player2Id}`}</Link>
                          ) : (
                              <em>Vaba voor (Bye)</em>
                          )}
                        </span>
                                                <strong>{m.player2Score ?? '-'}</strong>
                                            </div>

                                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: 12, color: '#64748b' }}>
                                                <span>Staatus: {m.status}</span>
                                                {isOrganizer && m.status !== 'Completed' && m.player2Id && (
                                                    <button
                                                        onClick={() => openScoreModal(m)}
                                                        style={{ padding: '4px 8px', fontSize: 12, cursor: 'pointer' }}
                                                    >
                                                        Sisesta tulemus
                                                    </button>
                                                )}
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </section>

            {selectedMatch && (
                <div
                    style={{
                        position: 'fixed',
                        inset: 0,
                        background: 'rgba(0,0,0,0.45)',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        zIndex: 50,
                    }}
                >
                    <div style={{ background: '#fff', padding: 24, borderRadius: 8, width: 360 }}>
                        <h3 style={{ marginTop: 0 }}>Sisesta mängu tulemus</h3>
                        <form onSubmit={handleSaveResult}>
                            <div style={{ marginBottom: 12 }}>
                                <label style={{ display: 'block', marginBottom: 4 }}>
                                    {selectedMatch.player1Name || `Mängija #${selectedMatch.player1Id}`} skoor:
                                </label>
                                <input
                                    type="number"
                                    min="0"
                                    value={score1}
                                    onChange={(e) => setScore1(e.target.value)}
                                    style={{ width: '100%', padding: 8, boxSizing: 'border-box' }}
                                />
                            </div>

                            <div style={{ marginBottom: 16 }}>
                                <label style={{ display: 'block', marginBottom: 4 }}>
                                    {selectedMatch.player2Name || `Mängija #${selectedMatch.player2Id}`} skoor:
                                </label>
                                <input
                                    type="number"
                                    min="0"
                                    value={score2}
                                    onChange={(e) => setScore2(e.target.value)}
                                    style={{ width: '100%', padding: 8, boxSizing: 'border-box' }}
                                />
                            </div>

                            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
                                <button type="button" onClick={() => setSelectedMatch(null)} style={{ padding: '8px 12px' }}>
                                    Tühista
                                </button>
                                <button
                                    type="submit"
                                    disabled={submittingScore}
                                    style={{ padding: '8px 14px', background: '#2563eb', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer' }}
                                >
                                    {submittingScore ? 'Salvestan...' : 'Salvesta'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}