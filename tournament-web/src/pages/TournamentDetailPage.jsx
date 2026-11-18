import { useCallback, useEffect, useMemo, useState } from 'react';
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
import { WinnerBanner } from '../components/WinnerBanner';
import { LiveDuels } from '../components/LiveDuels';

export function TournamentDetailPage() {

    function describeError(err, fallback = 'Tulemuse salvestamine ebaõnnestus.') {
        if (!err.response) {
            return 'Serveriga ei saadud ühendust. Kontrolli, kas API töötab pordil 5000.';
        }

        const data = err.response.data;
        const status = err.response.status;
        const fieldErrors = data?.errors ? Object.values(data.errors).flat().join(' ') : '';

        return [data?.detail, fieldErrors, `(HTTP ${status})`].filter(Boolean).join(' ') || fallback;
    }

    const { id } = useParams();
    const navigate = useNavigate();
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

    const [isEditOpen, setIsEditOpen] = useState(false);
    const [savingEdit, setSavingEdit] = useState(false);
    const [editForm, setEditForm] = useState({
        name: '',
        game: 'CS2',
        description: '',
        format: 'SingleElimination',
        maxPlayers: 8,
        startDate: '',
    });

    // Laadime turniiri andmed. Backend tagastab osalejad ja mängud samas vastuses.
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

    // Reaalajas uuendused: tulemused ja uued voorud
    useEffect(() => {
        const token = getStoredToken();
        if (!token || !tournamentId) return undefined;

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
                if (!prev?.matches) return prev;
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
                connection.invoke('LeaveTournament', tournamentId).catch(() => { });
            }
            connection.stop();
        };
    }, [tournamentId, loadTournament]);

    // Lichessi arena: käimasolevad mängud ja tabel uuenevad iga 30 sekundi järel
    useEffect(() => {
        if (tournament?.format !== 'Arena' || tournament?.status !== 'InProgress') return undefined;
        const timer = setInterval(loadTournament, 30000);
        return () => clearInterval(timer);
    }, [tournament?.format, tournament?.status, loadTournament]);

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
            setActionMsg('Turniiritabel on edukalt genereeritud!');
            await loadTournament();
        } catch (err) {
            setError(err.response?.data?.detail || 'Turniiritabeli loomine ebaõnnestus.');
        }
    };

    const handleNextRound = async () => {
        try {
            setError(null);
            await apiClient.post(`/api/tournaments/${tournamentId}/ratings/recalculate`);
            await apiClient.post(`/api/tournaments/${tournamentId}/rounds/next`);
            setActionMsg('Järgmine voor on loodud!');
            await loadTournament();
        } catch (err) {
            setError(err.response?.data?.detail || 'Järgmise vooru loomine ebaõnnestus.');
        }
    };

    const handleFinishTournament = async () => {
        if (!window.confirm('Kas oled kindel, et soovid turniiri lõpetada? Elo arvutatakse ümber.')) return;
        try {
            setError(null);
            await apiClient.post(`/api/tournaments/${tournamentId}/finish`);
            await loadTournament();
            setActionMsg('Turniir on lõpetatud!');
        } catch (err) {
            setError(err.response?.data?.detail || 'Turniiri lõpetamine ebaõnnestus.');
        }
    };

    const openEditModal = () => {
        if (!tournament) return;
        setEditForm({
            name: tournament.name || '',
            game: tournament.game || 'CS2',
            description: tournament.description || '',
            format: tournament.format || 'SingleElimination',
            maxPlayers: tournament.maxPlayers ?? 8,
            startDate: tournament.startDate
                ? new Date(tournament.startDate).toISOString().slice(0, 16)
                : '',
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

        const s1 = Number(score1);
        const s2 = Number(score2);

        // Ühe väljalangemisega turniiril viik ei ole lubatud. Vaikimisi on mõlemad skoorid 0,
        // seega ilma muutmiseta salvestamine annab viigi.
        if (tournament.format === 'SingleElimination' && s1 === s2) {
            setError('Ühe väljalangemisega turniiril ei saa olla viiki. Sisesta erinev tulemus.');
            return;
        }

        try {
            setSubmittingScore(true);
            setError(null);
            await apiClient.put(`/api/matches/${selectedMatch.id}/result`, {
                player1Score: s1,
                player2Score: s2,
            });
            setSelectedMatch(null);
            await loadTournament();
        } catch (err) {
            setError(describeError(err));
        } finally {
            setSubmittingScore(false);
        }
    };

    // Kõik arvutatud väärtused on komponendi tasemel, nii et neid näeb igal pool
    const participants = useMemo(() => tournament?.registrations ?? [], [tournament]);
    const matches = useMemo(() => tournament?.matches ?? [], [tournament]);

    const role = (user?.role || '').toLowerCase();
    const isAdminOrOrganizer = role === 'organizer' || role === 'admin';
    const isPlayer = role === 'player';

    const alreadyRegistered = useMemo(() => {
        if (!user) return false;
        return participants.some(
            (p) => Number(p.playerId) === Number(user.id)
                || p.username?.toLowerCase() === user.username?.toLowerCase()
        );
    }, [participants, user]);

    const roundsMap = useMemo(() => {
        return matches.reduce((acc, match) => {
            const r = match.round || 1;
            if (!acc[r]) acc[r] = [];
            acc[r].push(match);
            return acc;
        }, {});
    }, [matches]);

    const roundNumbers = Object.keys(roundsMap).map(Number).sort((a, b) => a - b);

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
                <Link to="/tournaments" className="btn btn-outline">Tagasi turniiride nimekirja</Link>
            </div>
        );
    }

    const isArena = tournament.format === 'Arena';
    const isOpenForRegistration = tournament.status === 'Registration';
    const isInProgress = tournament.status === 'InProgress';
    const maxP = tournament.maxPlayers ?? 0;
    const displayCount = participants.length;

    // Vooru ja lõpetamise kontroll
    const currentRoundMatches = matches.filter((m) => m.round === tournament.currentRound);
    const roundDone =
        currentRoundMatches.length > 0 && currentRoundMatches.every((m) => m.status === 'Completed');

    const totalRrRounds = (() => {
        const n = displayCount;
        const even = n % 2 === 0 ? n : n + 1;
        return even - 1;
    })();

    const isFinalReady =
        tournament.format === 'SingleElimination'
            ? currentRoundMatches.length === 1
            : tournament.format === 'RoundRobin' && tournament.currentRound >= totalRrRounds;

    const canFinish = isAdminOrOrganizer && isInProgress && roundDone && isFinalReady;
    const canNextRound = isAdminOrOrganizer && isInProgress && roundDone && !isFinalReady;

    // Arena puhul näitame tabelit koha ja punktide järgi
    const sortedParticipants = isArena
        ? [...participants].sort((a, b) => (a.finalRank ?? 9999) - (b.finalRank ?? 9999))
        : participants;

    return (
        <div className="container">
            <Link to="/tournaments" className="back-link">← Tagasi turniiride nimekirja</Link>

            <div className="panel">
                <div className="page-header" style={{ marginBottom: 0 }}>
                    <div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 10 }}>
                            <span className="badge badge-discipline">{formatDiscipline(tournament.game)}</span>
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

                        {canNextRound && (
                            <button onClick={handleNextRound} className="btn btn-success">
                                Genereeri järgmine voor
                            </button>
                        )}

                        {canFinish && (
                            <button onClick={handleFinishTournament} className="btn btn-primary">
                                Lõpeta turniir
                            </button>
                        )}

                        {isAdminOrOrganizer && isOpenForRegistration && matches.length === 0 && !isArena && (
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
                        <div className="info-box-value">{formatDiscipline(tournament.game)}</div>
                    </div>
                    <div className="info-box">
                        <div className="info-box-label">Turniiri formaat</div>
                        <div className="info-box-value">{formatTournamentFormat(tournament.format)}</div>
                    </div>
                    <div className="info-box">
                        <div className="info-box-label">Osalejate arv</div>
                        <div className="info-box-value">{displayCount} / {maxP} mängijat</div>
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

            <WinnerBanner tournament={tournament} />

            {isArena && <LiveDuels duels={tournament.liveDuels} />}

            <div className="panel">
                <h2 className="section-title">
                    {isArena ? 'Tulemused' : 'Registreerunud osalejad'} ({displayCount} / {maxP})
                </h2>

                {sortedParticipants.length === 0 ? (
                    <p style={{ margin: 0, color: 'var(--text-muted)' }}>Osalejaid pole veel registreerunud.</p>
                ) : isArena ? (
                    <div style={{ overflowX: 'auto' }}>
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Koht</th>
                                    <th>Mängija</th>
                                    <th>Punktid</th>
                                    <th>Elo (rapid)</th>
                                </tr>
                            </thead>
                            <tbody>
                                {sortedParticipants.map((p) => (
                                    <tr key={p.playerId}>
                                        <td>{p.finalRank ?? '—'}</td>
                                        <td>
                                            <Link to={`/players/${p.playerId}`}>
                                                {p.displayName || p.username}
                                            </Link>
                                        </td>
                                        <td><strong>{p.score ?? '—'}</strong></td>
                                        <td>{p.rating}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                ) : (
                    <div className="participants-list">
                        {sortedParticipants.map((p) => (
                            <Link key={p.playerId} to={`/players/${p.playerId}`} className="participant-chip">
                                <span>{p.displayName || p.username || `Mängija #${p.playerId}`}</span>
                                <span className="elo-tag">Elo {p.rating}</span>
                            </Link>
                        ))}
                    </div>
                )}
            </div>

            {!isArena && (
                <div className="panel">
                    <h2 className="section-title">Turniiritabel</h2>

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
                                                    {m.player1Id ? (
                                                        <Link to={`/players/${m.player1Id}`} style={{ fontWeight: p1Won ? 700 : 500 }}>
                                                            {m.player1Name || `Mängija #${m.player1Id}`}
                                                        </Link>
                                                    ) : (
                                                        <span style={{ color: 'var(--text-muted)' }}>Selgumisel</span>
                                                    )}
                                                    <span className={`match-score ${p1Won ? 'winner' : ''}`}>
                                                        {m.player1Score ?? '-'}
                                                    </span>
                                                </div>

                                                <div className="match-row">
                                                    {m.player2Id ? (
                                                        <Link to={`/players/${m.player2Id}`} style={{ fontWeight: p2Won ? 700 : 500 }}>
                                                            {m.player2Name || `Mängija #${m.player2Id}`}
                                                        </Link>
                                                    ) : (
                                                        <em style={{ color: 'var(--text-muted)' }}>Vaba voor (Bye)</em>
                                                    )}
                                                    <span className={`match-score ${p2Won ? 'winner' : ''}`}>
                                                        {m.player2Score ?? '-'}
                                                    </span>
                                                </div>

                                                <div className="match-footer">
                                                    <span>Staatus: {formatMatchStatus(m.status)}</span>
                                                    {isAdminOrOrganizer && m.status !== 'Completed' && m.player2Id && (
                                                        <button onClick={() => openScoreModal(m)} className="btn btn-primary btn-sm">
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
            )}

            {isEditOpen && (
                <div className="modal-backdrop">
                    <div className="modal-box" style={{ maxWidth: 480 }}>
                        <h3 style={{ margin: '0 0 16px' }}>Muuda turniiri</h3>

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
                        <h3 style={{ margin: '0 0 16px' }}>Sisesta mängu tulemus</h3>

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
                                <button type="button" onClick={() => setSelectedMatch(null)} className="btn btn-outline">
                                    Tühista
                                </button>
                                <button type="submit" disabled={submittingScore} className="btn btn-primary">
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