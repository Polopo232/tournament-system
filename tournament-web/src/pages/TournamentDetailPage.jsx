import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { apiClient, getStoredToken, getStoredUser } from '../api/client';
import {
    formatDiscipline,
    formatMatchStatus,
    formatStatus,
    formatTournamentFormat,
    getStatusBadgeClass,
} from '../utils/formatters';

export function TournamentDetailPage() {
    const { id } = useParams();
    const navigate = useNavigate();
    const tournamentId = Number(id);
    const user = getStoredUser();

    const [tournament, setTournament] = useState(null);
    const [participantsList, setParticipantsList] = useState([]);
    const [registeredOverride, setRegisteredOverride] = useState(false);

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [actionMsg, setActionMsg] = useState(null);

    const [selectedMatch, setSelectedMatch] = useState(null);
    const [score1, setScore1] = useState(0);
    const [score2, setScore2] = useState(0);
    const [submittingScore, setSubmittingScore] = useState(false);

    const [isEditOpen, setIsEditOpen] = useState(false);
    const [editForm, setEditForm] = useState({
        name: '',
        game: 'CS2',
        description: '',
        format: 'SingleElimination',
        maxPlayers: 8,
        startDate: '',
    });
    const [savingEdit, setSavingEdit] = useState(false);

    const loadTournament = useCallback(async () => {
        try {
            setError(null);
            const response = await apiClient.get(`/api/tournaments/${tournamentId}`);
            const data = response.data;
            setTournament(data);

            let foundParticipants =
                data.participants ||
                data.registrations ||
                data.players ||
                data.registeredPlayers ||
                [];

            if (foundParticipants.length === 0) {
                try {
                    const regRes = await apiClient.get(`/api/tournaments/${tournamentId}/registrations`);
                    if (Array.isArray(regRes.data)) foundParticipants = regRes.data;
                } catch {
                    try {
                        const partRes = await apiClient.get(`/api/tournaments/${tournamentId}/participants`);
                        if (Array.isArray(partRes.data)) foundParticipants = partRes.data;
                    } catch {
                    }
                }
            }

            setParticipantsList(foundParticipants);
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
            try {
                await apiClient.post(`/api/tournaments/${tournamentId}/register`);
            } catch (firstErr) {
                if (firstErr.response?.status === 404 || firstErr.response?.status === 405) {
                    await apiClient.post(`/api/tournaments/${tournamentId}/registrations`);
                } else {
                    throw firstErr;
                }
            }
            setRegisteredOverride(true);
            setActionMsg('Oled edukalt turniirile registreeritud!');
            await loadTournament();
        } catch (err) {
            const msg =
                err.response?.data?.detail ||
                err.response?.data?.message ||
                err.response?.data?.title ||
                'Registreerimine ebaõnnestus.';

            if (msg.toLowerCase().includes('already registered')) {
                setRegisteredOverride(true);
                setActionMsg('Oled juba sellele turniirile registreeritud!');
                await loadTournament();
            } else {
                setError(msg);
            }
        }
    };

    const handleGenerateBracket = async () => {
        try {
            setActionMsg(null);
            setError(null);
            try {
                await apiClient.post(`/api/tournaments/${tournamentId}/bracket`);
            } catch (firstErr) {
                if (firstErr.response?.status === 404 || firstErr.response?.status === 405) {
                    await apiClient.post(`/api/tournaments/${tournamentId}/start`);
                } else {
                    throw firstErr;
                }
            }
            setActionMsg('Turniiritabel on edukalt genereeritud!');
            await loadTournament();
        } catch (err) {
            const msg =
                err.response?.data?.detail ||
                err.response?.data?.message ||
                'Turniiritabeli loomine ebaõnnestus.';
            setError(msg);
        }
    };

    const openEditModal = () => {
        if (!tournament) return;
        const rawGame = tournament.game || tournament.discipline || 'CS2';
        const rawMax = tournament.maxPlayers ?? tournament.maxParticipants ?? 8;
        const isoDate = tournament.startDate
            ? new Date(tournament.startDate).toISOString().slice(0, 16)
            : '';

        setEditForm({
            name: tournament.name || '',
            game: rawGame,
            description: tournament.description || '',
            format: tournament.format || 'SingleElimination',
            maxPlayers: rawMax,
            startDate: isoDate,
        });
        setIsEditOpen(true);
    };

    const handleUpdateTournament = async (e) => {
        e.preventDefault();
        try {
            setSavingEdit(true);
            setError(null);
            await apiClient.put(`/api/tournaments/${tournamentId}`, {
                name: editForm.name.trim(),
                game: editForm.game,
                discipline: editForm.game,
                description: editForm.description.trim(),
                format: editForm.format,
                maxPlayers: Number(editForm.maxPlayers),
                maxParticipants: Number(editForm.maxPlayers),
                startDate: new Date(editForm.startDate).toISOString(),
            });
            setIsEditOpen(false);
            setActionMsg('Turniiri andmed on uuendatud!');
            await loadTournament();
        } catch (err) {
            setError(err.response?.data?.detail || 'Turniiri muutmine ebaõnnestus.');
        } finally {
            setSavingEdit(false);
        }
    };

    const handleDeleteTournament = async () => {
        if (!window.confirm('Kas oled kindel, et soovid selle turniiri kustutada?')) return;
        try {
            setError(null);
            await apiClient.delete(`/api/tournaments/${tournamentId}`);
            navigate('/tournaments');
        } catch (err) {
            setError(err.response?.data?.detail || 'Turniiri kustutamine ebaõnnestus.');
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

    if (loading) {
        return (
            <div className="container">
                <p style={{ color: 'var(--text-muted)' }}>Laadin turniiri andmeid...</p>
            </div>
        );
    }

    if (!tournament) {
        return (
            <div className="container">
                <div className="alert-error">Turniiri ei leitud.</div>
                <Link to="/tournaments" className="btn btn-outline">
                    Tagasi turniiride nimekirja
                </Link>
            </div>
        );
    }

    const rawGame = tournament.game || tournament.discipline;
    const maxP = tournament.maxPlayers ?? tournament.maxParticipants ?? 0;
    const participants = participantsList;
    const displayCount = Math.max(
        participants.length,
        tournament.currentParticipantsCount ??
        tournament.participantsCount ??
        tournament.registeredCount ??
        0
    );
    const matches = tournament.matches || [];

    const role = (user?.role || '').toLowerCase();
    const isAdminOrOrganizer = role === 'organizer' || role === 'admin';
    const isPlayer = role === 'player';

    const isOpenForRegistration =
        tournament.status === 'Registration' || tournament.status === 'Open';

    const alreadyRegistered =
        registeredOverride ||
        (user &&
            participants.some(
                (p) =>
                    Number(p.playerId || p.userId || p.id) === Number(user.id) ||
                    (p.username && p.username.toLowerCase() === user.username?.toLowerCase()) ||
                    (p.playerName && p.playerName.toLowerCase() === user.username?.toLowerCase())
            ));

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
        <div className="container">
            <Link to="/tournaments" className="back-link">
                ← Tagasi turniiride nimekirja
            </Link>

            <div className="panel">
                <div className="page-header" style={{ marginBottom: 0 }}>
                    <div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 10 }}>
                            <span className="badge badge-discipline">{formatDiscipline(rawGame)}</span>
                            <span className={getStatusBadgeClass(tournament.status)}>
                {formatStatus(tournament.status)}
              </span>
                        </div>
                        <h1 className="page-title">{tournament.name}</h1>
                        {tournament.description && (
                            <p className="page-subtitle" style={{ maxWidth: 680 }}>
                                {tournament.description}
                            </p>
                        )}
                    </div>

                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
                        {isPlayer && isOpenForRegistration && !alreadyRegistered && (
                            <button onClick={handleRegister} className="btn btn-primary">
                                Registreeru turniirile
                            </button>
                        )}

                        {isPlayer && alreadyRegistered && (
                            <span className="badge badge-open" style={{ padding: '8px 14px', fontSize: 13 }}>
                ✓ Oled registreeritud
              </span>
                        )}

                        {isAdminOrOrganizer && isOpenForRegistration && matches.length === 0 && (
                            <button onClick={handleGenerateBracket} className="btn btn-success">
                                Genereeri turniiritabel
                            </button>
                        )}

                        {isAdminOrOrganizer && (
                            <>
                                <button onClick={openEditModal} className="btn btn-outline">
                                    Muuda turniiri
                                </button>
                                <button
                                    onClick={handleDeleteTournament}
                                    className="btn"
                                    style={{ background: 'var(--danger-soft)', color: 'var(--danger)' }}
                                >
                                    Kustuta
                                </button>
                            </>
                        )}
                    </div>
                </div>

                <div className="info-grid">
                    <div className="info-box">
                        <div className="info-box-label">Mäng / Spordiala</div>
                        <div className="info-box-value">{formatDiscipline(rawGame)}</div>
                    </div>

                    <div className="info-box">
                        <div className="info-box-label">Turniiri formaat</div>
                        <div className="info-box-value">{formatTournamentFormat(tournament.format)}</div>
                    </div>

                    <div className="info-box">
                        <div className="info-box-label">Osalejate arv</div>
                        <div className="info-box-value">
                            {displayCount} / {maxP} mängijat
                        </div>
                    </div>

                    <div className="info-box">
                        <div className="info-box-label">Alguskuupäev</div>
                        <div className="info-box-value">
                            {new Date(tournament.startDate).toLocaleDateString('et-EE', {
                                day: '2-digit',
                                month: 'long',
                                year: 'numeric',
                            })}
                        </div>
                    </div>
                </div>
            </div>

            {actionMsg && <div className="alert-success">{actionMsg}</div>}
            {error && <div className="alert-error">{error}</div>}

            <div className="panel">
                <h2 style={{ margin: '0 0 16px', fontSize: 18 }}>
                    Registreerunud osalejad ({displayCount} / {maxP})
                </h2>

                {participants.length === 0 ? (
                    <p style={{ margin: 0, color: 'var(--text-muted)' }}>
                        Osalejaid pole veel registreerunud.
                    </p>
                ) : (
                    <div className="participants-list">
                        {participants.map((p) => {
                            const playerId = p.playerId || p.userId || p.id;
                            const playerName = p.username || p.playerName || `Mängija #${playerId}`;
                            const elo = p.rating ?? p.eloRating;

                            return (
                                <Link key={playerId} to={`/players/${playerId}`} className="participant-chip">
                                    <span>{playerName}</span>
                                    {elo !== undefined && <span className="elo-tag">Elo {elo}</span>}
                                </Link>
                            );
                        })}
                    </div>
                )}
            </div>

            <div className="panel">
                <h2 style={{ margin: '0 0 16px', fontSize: 18 }}>Turniiritabel</h2>

                {roundNumbers.length === 0 ? (
                    <p style={{ margin: 0, color: 'var(--text-muted)' }}>
                        Turniiritabel pole veel genereeritud.
                    </p>
                ) : (
                    <div className="bracket-scroll">
                        {roundNumbers.map((roundNum) => (
                            <div key={roundNum} className="round-column">
                                <h3 className="round-title">Voor {roundNum}</h3>

                                {roundsMap[roundNum].map((m) => {
                                    const p1Won = m.status === 'Completed' && m.player1Score > m.player2Score;
                                    const p2Won = m.status === 'Completed' && m.player2Score > m.player1Score;

                                    return (
                                        <div key={m.id} className="match-card">
                                            <div className="match-row">
                        <span>
                          {m.player1Id ? (
                              <Link to={`/players/${m.player1Id}`} style={{ fontWeight: p1Won ? 700 : 500 }}>
                                  {m.player1Name || `Mängija #${m.player1Id}`}
                              </Link>
                          ) : (
                              <span style={{ color: 'var(--text-muted)' }}>Selgumisel</span>
                          )}
                        </span>
                                                <span className={`match-score ${p1Won ? 'winner' : ''}`}>
                          {m.player1Score ?? '-'}
                        </span>
                                            </div>

                                            <div className="match-row">
                        <span>
                          {m.player2Id ? (
                              <Link to={`/players/${m.player2Id}`} style={{ fontWeight: p2Won ? 700 : 500 }}>
                                  {m.player2Name || `Mängija #${m.player2Id}`}
                              </Link>
                          ) : (
                              <em style={{ color: 'var(--text-muted)' }}>Vaba voor (Bye)</em>
                          )}
                        </span>
                                                <span className={`match-score ${p2Won ? 'winner' : ''}`}>
                          {m.player2Score ?? '-'}
                        </span>
                                            </div>

                                            <div className="match-footer">
                                                <span>Staatus: {formatMatchStatus(m.status)}</span>
                                                {isAdminOrOrganizer && m.status !== 'Completed' && m.player2Id && (
                                                    <button
                                                        onClick={() => openScoreModal(m)}
                                                        className="btn btn-primary btn-sm"
                                                    >
                                                        Sisesta tulemus
                                                    </button>
                                                )}
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {isEditOpen && (
                <div className="modal-backdrop">
                    <div className="modal-box" style={{ maxWidth: 480 }}>
                        <h3 style={{ margin: '0 0 16px', fontSize: 19 }}>Muuda turniiri</h3>

                        <form onSubmit={handleUpdateTournament}>
                            <div className="form-group">
                                <label className="form-label">Turniiri nimi</label>
                                <input
                                    type="text"
                                    required
                                    className="input"
                                    value={editForm.name}
                                    onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
                                />
                            </div>

                            <div className="form-group">
                                <label className="form-label">Mäng / Spordiala</label>
                                <select
                                    className="select"
                                    value={editForm.game}
                                    onChange={(e) => setEditForm({ ...editForm, game: e.target.value })}
                                >
                                    <option value="CS2">Counter-Strike 2</option>
                                    <option value="Dota2">Dota 2</option>
                                    <option value="Chess">Male</option>
                                    <option value="TableTennis">Lauatennis</option>
                                </select>
                            </div>

                            <div className="form-group">
                                <label className="form-label">Formaat</label>
                                <select
                                    className="select"
                                    value={editForm.format}
                                    onChange={(e) => setEditForm({ ...editForm, format: e.target.value })}
                                >
                                    <option value="SingleElimination">Ühe väljalangemisega</option>
                                    <option value="RoundRobin">Kõik kõigiga</option>
                                </select>
                            </div>

                            <div className="form-group">
                                <label className="form-label">Maksimaalne osalejate arv</label>
                                <input
                                    type="number"
                                    min="2"
                                    max="128"
                                    className="input"
                                    value={editForm.maxPlayers}
                                    onChange={(e) => setEditForm({ ...editForm, maxPlayers: e.target.value })}
                                />
                            </div>

                            <div className="form-group">
                                <label className="form-label">Algusaeg</label>
                                <input
                                    type="datetime-local"
                                    className="input"
                                    value={editForm.startDate}
                                    onChange={(e) => setEditForm({ ...editForm, startDate: e.target.value })}
                                />
                            </div>

                            <div className="form-group">
                                <label className="form-label">Kirjeldus</label>
                                <input
                                    type="text"
                                    className="input"
                                    value={editForm.description}
                                    onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                                />
                            </div>

                            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
                                <button type="button" onClick={() => setIsEditOpen(false)} className="btn btn-outline">
                                    Tühista
                                </button>
                                <button type="submit" disabled={savingEdit} className="btn btn-primary">
                                    {savingEdit ? 'Salvestan...' : 'Salvesta muudatused'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {selectedMatch && (
                <div className="modal-backdrop">
                    <div className="modal-box">
                        <h3 style={{ margin: '0 0 16px', fontSize: 19 }}>Sisesta mängu tulemus</h3>

                        <form onSubmit={handleSaveResult}>
                            <div className="form-group">
                                <label className="form-label">
                                    {selectedMatch.player1Name || `Mängija #${selectedMatch.player1Id}`} skoor
                                </label>
                                <input
                                    type="number"
                                    min="0"
                                    className="input"
                                    value={score1}
                                    onChange={(e) => setScore1(e.target.value)}
                                />
                            </div>

                            <div className="form-group" style={{ marginBottom: 22 }}>
                                <label className="form-label">
                                    {selectedMatch.player2Name || `Mängija #${selectedMatch.player2Id}`} skoor
                                </label>
                                <input
                                    type="number"
                                    min="0"
                                    className="input"
                                    value={score2}
                                    onChange={(e) => setScore2(e.target.value)}
                                />
                            </div>

                            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
                                <button
                                    type="button"
                                    onClick={() => setSelectedMatch(null)}
                                    className="btn btn-outline"
                                >
                                    Tühista
                                </button>
                                <button
                                    type="submit"
                                    disabled={submittingScore}
                                    className="btn btn-primary"
                                >
                                    {submittingScore ? 'Salvestan...' : 'Salvesta tulemus'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}