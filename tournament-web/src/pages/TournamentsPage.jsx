import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { apiClient, getStoredUser } from '../api/client';
import {
    formatDiscipline,
    formatStatus,
    formatTournamentFormat,
    getStatusBadgeClass,
} from '../utils/formatters';

export function TournamentsPage() {
    const user = getStoredUser();
    const role = (user?.role || '').toLowerCase();
    const canCreate = role === 'admin' || role === 'organizer';

    const [tournaments, setTournaments] = useState([]);
    const [search, setSearch] = useState('');
    const [status, setStatus] = useState('');
    const [game, setGame] = useState('');
    const [sortBy, setSortBy] = useState('startDate');
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    // Модальное окно создания турнира
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [creating, setCreating] = useState(false);
    const [createForm, setCreateForm] = useState({
        name: '',
        game: 'CS2',
        description: '',
        format: 'SingleElimination',
        maxPlayers: 8,
        startDate: new Date(Date.now() + 86400000).toISOString().slice(0, 16),
    });

    const pageSize = 6;

    const fetchTournaments = useCallback(async () => {
        try {
            setLoading(true);
            setError(null);

            const params = {
                page,
                pageSize,
                sortBy,
            };

            if (search.trim()) params.search = search.trim();
            if (status) params.status = status;
            if (game) {
                params.game = game;
                params.discipline = game;
            }

            const response = await apiClient.get('/api/tournaments', { params });
            const data = response.data;

            if (Array.isArray(data)) {
                setTournaments(data);
                setTotalPages(1);
            } else {
                setTournaments(data.items ?? []);
                setTotalPages(Math.max(1, data.totalPages || Math.ceil((data.totalCount || 0) / pageSize)));
            }
        } catch {
            setError('Turniiride laadimine ebaõnnestus. Kontrolli, kas API töötab.');
        } finally {
            setLoading(false);
        }
    }, [search, status, game, sortBy, page]);

    useEffect(() => {
        fetchTournaments();
    }, [fetchTournaments]);

    const handleFilterChange = (setter, value) => {
        setter(value);
        setPage(1);
    };

    const handleCreateTournament = async (e) => {
        e.preventDefault();
        if (!createForm.name.trim()) return;

        try {
            setCreating(true);
            setError(null);
            await apiClient.post('/api/tournaments', {
                name: createForm.name.trim(),
                game: createForm.game,
                discipline: createForm.game,
                description: createForm.description.trim(),
                format: createForm.format,
                maxPlayers: Number(createForm.maxPlayers),
                maxParticipants: Number(createForm.maxPlayers),
                startDate: new Date(createForm.startDate).toISOString(),
            });
            setIsCreateOpen(false);
            setCreateForm({
                ...createForm,
                name: '',
                description: '',
            });
            await fetchTournaments();
        } catch (err) {
            setError(err.response?.data?.detail || 'Turniiri loomine ebaõnnestus.');
        } finally {
            setCreating(false);
        }
    };

    return (
        <div className="container">
            <div className="page-header">
                <div>
                    <h1 className="page-title">Turniiride kataloog</h1>
                    <p className="page-subtitle">
                        Sirvi aktiivseid ja tulevasi turniire, jälgi tulemusi ning liitu võistlusega
                    </p>
                </div>

                {canCreate && (
                    <button onClick={() => setIsCreateOpen(true)} className="btn btn-primary">
                        + Loo uus turniir
                    </button>
                )}
            </div>

            <div className="filter-bar">
                <input
                    type="text"
                    className="input"
                    value={search}
                    onChange={(e) => handleFilterChange(setSearch, e.target.value)}
                    placeholder="Otsi turniiri nime järgi..."
                />

                <select
                    className="select"
                    value={status}
                    onChange={(e) => handleFilterChange(setStatus, e.target.value)}
                >
                    <option value="">Kõik staatused</option>
                    <option value="Registration">Avatud registreerimiseks</option>
                    <option value="InProgress">Käimasolev</option>
                    <option value="Finished">Lõppenud</option>
                </select>

                <select
                    className="select"
                    value={game}
                    onChange={(e) => handleFilterChange(setGame, e.target.value)}
                >
                    <option value="">Kõik alad ja mängud</option>
                    <option value="CS2">Counter-Strike 2</option>
                    <option value="Dota2">Dota 2</option>
                    <option value="Chess">Male</option>
                    <option value="TableTennis">Lauatennis</option>
                </select>

                <select
                    className="select"
                    value={sortBy}
                    onChange={(e) => handleFilterChange(setSortBy, e.target.value)}
                >
                    <option value="startDate">Sorteeri: Alguskuupäev</option>
                    <option value="participants">Sorteeri: Osalejate arv</option>
                    <option value="name">Sorteeri: Nimi (A–Z)</option>
                </select>
            </div>

            {error && <div className="alert-error">{error}</div>}

            {loading ? (
                <p style={{ color: 'var(--text-muted)' }}>Laadin turniire...</p>
            ) : tournaments.length === 0 ? (
                <div className="panel" style={{ textAlign: 'center', padding: 48 }}>
                    <h3 style={{ margin: '0 0 8px' }}>Turniire ei leitud</h3>
                    <p style={{ margin: 0, color: 'var(--text-muted)' }}>
                        Proovi muuta otsingusõna või vali teine filter.
                    </p>
                </div>
            ) : (
                <div className="cards-grid">
                    {tournaments.map((t) => {
                        const rawGame = t.game || t.discipline;
                        const maxP = t.maxPlayers ?? t.maxParticipants ?? 0;
                        const count =
                            t.currentParticipantsCount ??
                            t.participantsCount ??
                            t.registeredCount ??
                            (Array.isArray(t.registrations) ? t.registrations.length : 0);

                        return (
                            <article key={t.id} className="card">
                                <div>
                                    <div className="card-badges">
                                        <span className="badge badge-discipline">{formatDiscipline(rawGame)}</span>
                                        <span className={getStatusBadgeClass(t.status)}>{formatStatus(t.status)}</span>
                                    </div>

                                    <h3 className="card-title">
                                        <Link to={`/tournaments/${t.id}`}>{t.name}</Link>
                                    </h3>

                                    {t.description && <p className="card-desc">{t.description}</p>}
                                </div>

                                <div className="card-meta">
                                    <div className="meta-row">
                                        <span className="meta-label">Formaat</span>
                                        <span className="meta-value">{formatTournamentFormat(t.format)}</span>
                                    </div>

                                    <div className="meta-row">
                                        <span className="meta-label">Osalejad</span>
                                        <span className="meta-value">
                      {count} / {maxP} mängijat
                    </span>
                                    </div>

                                    <div className="meta-row">
                                        <span className="meta-label">Alguskuupäev</span>
                                        <span className="meta-value">
                      {new Date(t.startDate).toLocaleDateString('et-EE', {
                          day: '2-digit',
                          month: 'long',
                          year: 'numeric',
                      })}
                    </span>
                                    </div>

                                    <div style={{ marginTop: 10 }}>
                                        <Link to={`/tournaments/${t.id}`} className="btn btn-outline" style={{ width: '100%' }}>
                                            Ava turniiritabel
                                        </Link>
                                    </div>
                                </div>
                            </article>
                        );
                    })}
                </div>
            )}

            <div className="pagination">
                <button
                    className="page-btn"
                    disabled={page <= 1}
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                    Eelmine
                </button>

                {Array.from({ length: totalPages }, (_, idx) => idx + 1).map((pageNum) => (
                    <button
                        key={pageNum}
                        onClick={() => setPage(pageNum)}
                        className={`page-btn ${pageNum === page ? 'active' : ''}`}
                    >
                        {pageNum}
                    </button>
                ))}

                <button
                    className="page-btn"
                    disabled={page >= totalPages}
                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                >
                    Järgmine
                </button>
            </div>

            {/* Модальное окно создания турнира */}
            {isCreateOpen && (
                <div className="modal-backdrop">
                    <div className="modal-box" style={{ maxWidth: 480 }}>
                        <h3 style={{ margin: '0 0 16px', fontSize: 19 }}>Loo uus turniir</h3>

                        <form onSubmit={handleCreateTournament}>
                            <div className="form-group">
                                <label className="form-label">Turniiri nimi</label>
                                <input
                                    type="text"
                                    required
                                    className="input"
                                    placeholder="nt. Tallinn CS2 Weekend Cup"
                                    value={createForm.name}
                                    onChange={(e) => setCreateForm({ ...createForm, name: e.target.value })}
                                />
                            </div>

                            <div className="form-group">
                                <label className="form-label">Mäng / Spordiala</label>
                                <select
                                    className="select"
                                    value={createForm.game}
                                    onChange={(e) => setCreateForm({ ...createForm, game: e.target.value })}
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
                                    value={createForm.format}
                                    onChange={(e) => setCreateForm({ ...createForm, format: e.target.value })}
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
                                    value={createForm.maxPlayers}
                                    onChange={(e) => setCreateForm({ ...createForm, maxPlayers: e.target.value })}
                                />
                            </div>

                            <div className="form-group">
                                <label className="form-label">Algusaeg</label>
                                <input
                                    type="datetime-local"
                                    required
                                    className="input"
                                    value={createForm.startDate}
                                    onChange={(e) => setCreateForm({ ...createForm, startDate: e.target.value })}
                                />
                            </div>

                            <div className="form-group">
                                <label className="form-label">Kirjeldus</label>
                                <input
                                    type="text"
                                    className="input"
                                    placeholder="Lühike info reeglite kohta..."
                                    value={createForm.description}
                                    onChange={(e) => setCreateForm({ ...createForm, description: e.target.value })}
                                />
                            </div>

                            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
                                <button type="button" onClick={() => setIsCreateOpen(false)} className="btn btn-outline">
                                    Tühista
                                </button>
                                <button type="submit" disabled={creating} className="btn btn-primary">
                                    {creating ? 'Loon...' : 'Loo turniir'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}