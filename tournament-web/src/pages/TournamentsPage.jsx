import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { apiClient, getStoredUser } from '../api/client';
import {
    formatDiscipline,
    formatStatus,
    formatTournamentFormat,
    getStatusBadgeClass,
} from '../utils/formatters';

const PAGE_SIZE = 9;

const GAME_TABS = [
    { key: 'all', label: 'Kõik', discipline: null, format: null, color: '#c6ff34' },
    { key: 'Chess', label: 'Male', discipline: 'Chess', format: null, color: '#e5e7eb' },
    { key: 'CS2', label: 'Counter-Strike 2', discipline: 'CS2', format: null, color: '#ff7a1a' },
    { key: 'Dota2', label: 'Dota 2', discipline: 'Dota2', format: null, color: '#ff3b5c' },
    { key: 'TableTennis', label: 'Lauatennis', discipline: 'TableTennis', format: null, color: '#2dd4bf' },
    { key: 'Lichess', label: 'Lichess-arenad', discipline: null, format: 'Arena', color: '#38bdf8' },
];

const STATUS_CHIPS = [
    { value: '', label: 'Kõik' },
    { value: 'Registration', label: 'Registreerimine' },
    { value: 'InProgress', label: 'Käimas' },
    { value: 'Finished', label: 'Lõppenud' },
];

const SORT_OPTIONS = [
    { value: '', label: 'Uuemad enne' },
    { value: 'startdate', label: 'Alguskuupäev' },
    { value: 'participants', label: 'Osalejate arv' },
    { value: 'name', label: 'Nimi (A–Z)' },
];

const GAME_COLORS = {
    CS2: '#ff7a1a',
    Dota2: '#ff3b5c',
    Chess: '#e5e7eb',
    TableTennis: '#2dd4bf',
    Arena: '#38bdf8',
};

function themeColor(t) {
    if (t.format === 'Arena') return GAME_COLORS.Arena;
    const base = (t.game || '').split(' (')[0];
    return GAME_COLORS[base] || '#a78bfa';
}

function gameLabel(t) {
    const game = t.game || t.discipline || '';
    const [base, detail] = game.split(' (');
    const label = formatDiscipline(base);
    return detail ? `${label} · ${detail.replace(')', '')}` : label;
}

function buildPages(current, total) {
    const wanted = [1, total, current - 1, current, current + 1].filter((p) => p >= 1 && p <= total);
    const sorted = [...new Set(wanted)].sort((a, b) => a - b);
    const out = [];
    sorted.forEach((p, i) => {
        if (i > 0 && p - sorted[i - 1] > 1) out.push('…');
        out.push(p);
    });
    return out;
}

function TournamentCard({ tournament: t }) {
    const max = t.maxPlayers ?? t.maxParticipants ?? 0;
    const count = t.registeredPlayersCount ?? t.registrations?.length ?? 0;
    const percent = max > 0 ? Math.min(100, Math.round((count / max) * 100)) : 0;

    return (
        <article className="t-card" style={{ '--game': themeColor(t) }}>
            <div className="t-card-top">
                <span className="chip-game">{gameLabel(t)}</span>
                <span className={getStatusBadgeClass(t.status)}>{formatStatus(t.status)}</span>
            </div>

            <Link to={`/tournaments/${t.id}`} className="t-card-title">
                {t.name}
            </Link>

            {t.description && <p className="t-card-desc">{t.description}</p>}

            <div className="progress">
                <div className="progress-bar" style={{ width: `${percent}%` }} />
            </div>

            <div className="t-card-meta">
                <span>{count} / {max} mängijat</span>
                <span>{formatTournamentFormat(t.format)}</span>
            </div>

            <div className="t-card-date">
                {new Date(t.startDate).toLocaleDateString('et-EE', {
                    day: '2-digit',
                    month: 'short',
                    year: 'numeric',
                })}
            </div>
        </article>
    );
}

function TournamentCardSkeleton() {
    return (
        <div className="t-card t-card-skeleton" aria-hidden="true">
            <div className="skeleton" style={{ height: 22, width: '40%' }} />
            <div className="skeleton" style={{ height: 26, width: '85%', marginTop: 14 }} />
            <div className="skeleton" style={{ height: 14, width: '70%' }} />
            <div className="skeleton" style={{ height: 8, width: '100%', marginTop: 'auto' }} />
        </div>
    );
}

export function TournamentsPage() {
    const user = getStoredUser();
    const role = (user?.role || '').toLowerCase();
    const canCreate = role === 'admin' || role === 'organizer';

    const [searchParams, setSearchParams] = useSearchParams();
    const gameKey = searchParams.get('game') || 'all';
    const status = searchParams.get('status') || '';
    const sort = searchParams.get('sort') || '';
    const q = searchParams.get('q') || '';
    const page = Math.max(1, Number(searchParams.get('page')) || 1);

    const activeTab = GAME_TABS.find((t) => t.key === gameKey) ?? GAME_TABS[0];

    const [qInput, setQInput] = useState(q);
    const [items, setItems] = useState([]);
    const [totalCount, setTotalCount] = useState(0);
    const [totalPages, setTotalPages] = useState(1);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [reloadKey, setReloadKey] = useState(0);

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

    const updateParams = (changes) => {
        const next = new URLSearchParams(searchParams);
        Object.entries(changes).forEach(([key, value]) => {
            if (value === '' || value === null || value === undefined) next.delete(key);
            else next.set(key, String(value));
        });
        if (!('page' in changes)) next.delete('page');
        setSearchParams(next);
    };

    useEffect(() => {
        setQInput(q);
    }, [q]);

    useEffect(() => {
        if (qInput.trim() === q) return undefined;
        const timer = setTimeout(() => updateParams({ q: qInput.trim() }), 300);
        return () => clearTimeout(timer);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [qInput]);

    const queryString = searchParams.toString();

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            setLoading(true);
            setError(null);

            const params = { page, pageSize: PAGE_SIZE };
            if (q) params.search = q;
            if (status) params.status = status;
            if (sort) params.sortBy = sort;
            if (activeTab.discipline) params.discipline = activeTab.discipline;
            if (activeTab.format) params.format = activeTab.format;

            try {
                const res = await apiClient.get('/api/tournaments', { params });
                if (cancelled) return;
                setItems(res.data.items ?? []);
                setTotalCount(res.data.totalCount ?? 0);
                setTotalPages(Math.max(1, res.data.totalPages ?? 1));
            } catch {
                if (!cancelled) setError('Turniiride laadimine ebaõnnestus. Kontrolli, kas API töötab.');
            } finally {
                if (!cancelled) setLoading(false);
            }
        };

        load();
        return () => {
            cancelled = true;
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [queryString, reloadKey]);

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
            setCreateForm({ ...createForm, name: '', description: '' });
            setReloadKey((k) => k + 1);
        } catch (err) {
            setError(err.response?.data?.detail || 'Turniiri loomine ebaõnnestus.');
        } finally {
            setCreating(false);
        }
    };

    const hasFilters = Boolean(q || status || sort || gameKey !== 'all');

    return (
        <div className="container">
            <section className="hero">
                <div>
                    <span className="hero-kicker">ArenaHub</span>
                    <h1 className="hero-title">Turniirid</h1>
                    <p className="hero-sub">
                        Otsi turniire, jälgi käimasolevaid mänge ja vaata tulemusi ühest kohast.
                    </p>
                </div>

                <div className="hero-side">
                    <div className="hero-stat">
                        <strong>{totalCount}</strong>
                        <span>turniiri</span>
                    </div>
                    {canCreate && (
                        <button onClick={() => setIsCreateOpen(true)} className="btn btn-success">
                            + Loo uus turniir
                        </button>
                    )}
                </div>
            </section>

            <div className="tabs" role="tablist">
                {GAME_TABS.map((tab) => (
                    <button
                        key={tab.key}
                        role="tab"
                        aria-selected={tab.key === activeTab.key}
                        className={`tab ${tab.key === activeTab.key ? 'active' : ''}`}
                        style={{ '--game': tab.color }}
                        onClick={() => updateParams({ game: tab.key === 'all' ? '' : tab.key })}
                    >
                        {tab.label}
                    </button>
                ))}
            </div>

            <div className="toolbar">
                <div className="search-box">
                    <span className="search-icon">⌕</span>
                    <input
                        type="text"
                        className="input search-input"
                        value={qInput}
                        onChange={(e) => setQInput(e.target.value)}
                        placeholder="Otsi turniiri nime järgi..."
                    />
                </div>

                <div className="seg" role="group" aria-label="Staatus">
                    {STATUS_CHIPS.map((s) => (
                        <button
                            key={s.value || 'all'}
                            className={`seg-btn ${status === s.value ? 'active' : ''}`}
                            onClick={() => updateParams({ status: s.value })}
                        >
                            {s.label}
                        </button>
                    ))}
                </div>

                <select
                    className="select toolbar-select"
                    value={sort}
                    onChange={(e) => updateParams({ sort: e.target.value })}
                >
                    {SORT_OPTIONS.map((o) => (
                        <option key={o.value || 'default'} value={o.value}>
                            {o.label}
                        </option>
                    ))}
                </select>
            </div>

            {error && <div className="alert-error">{error}</div>}

            {loading ? (
                <div className="t-grid">
                    {Array.from({ length: 6 }, (_, i) => (
                        <TournamentCardSkeleton key={i} />
                    ))}
                </div>
            ) : items.length === 0 ? (
                <div className="empty-state">
                    <div className="empty-icon">◎</div>
                    <h3>Turniire ei leitud</h3>
                    <p>Proovi teist filtrit või otsingusõna.</p>
                    {hasFilters && (
                        <button className="btn btn-outline" onClick={() => setSearchParams({})}>
                            Lähtesta filtrid
                        </button>
                    )}
                </div>
            ) : (
                <div className="t-grid">
                    {items.map((t) => (
                        <TournamentCard key={t.id} tournament={t} />
                    ))}
                </div>
            )}

            {totalPages > 1 && (
                <div className="pager">
                    <button
                        className="page-btn"
                        disabled={page <= 1}
                        onClick={() => updateParams({ page: page - 1 })}
                    >
                        ←
                    </button>

                    {buildPages(page, totalPages).map((p, i) =>
                        p === '…' ? (
                            <span key={`dots-${i}`} className="pager-dots">…</span>
                        ) : (
                            <button
                                key={p}
                                className={`page-btn ${p === page ? 'active' : ''}`}
                                onClick={() => updateParams({ page: p })}
                            >
                                {p}
                            </button>
                        )
                    )}

                    <button
                        className="page-btn"
                        disabled={page >= totalPages}
                        onClick={() => updateParams({ page: page + 1 })}
                    >
                        →
                    </button>
                </div>
            )}

            {isCreateOpen && (
                <div className="modal-backdrop">
                    <div className="modal-box" style={{ maxWidth: 480 }}>
                        <h3 style={{ margin: '0 0 16px' }}>Loo uus turniir</h3>

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