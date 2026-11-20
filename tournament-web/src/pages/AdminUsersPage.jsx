import { useEffect, useState } from 'react';
import { apiClient, getStoredUser } from '../api/client';
import {
    BAN_DURATIONS,
    BAN_REASONS,
    RECOMMENDED_DURATION,
    durationLabel,
    formatUntil,
    shortReason,
} from '../utils/banLabels';

const ROLES = ['Admin', 'Organizer', 'Player', 'Service'];

const SOURCE_TABS = [
    { value: '', label: 'Kõik' },
    { value: 'local', label: 'Kohalikud' },
    { value: 'lichess', label: 'Lichess' },
];

const SORT_OPTIONS = [
    { value: 'name', label: 'Nimi (A–Z)' },
    { value: 'newest', label: 'Uuemad enne' },
    { value: 'role', label: 'Roll' },
];

const PAGE_SIZE = 15;

function isLichessUser(user) {
    return user.username?.startsWith('lichess_');
}

export function AdminUsersPage() {
    const me = getStoredUser();

    const [searchInput, setSearchInput] = useState('');
    const [search, setSearch] = useState('');
    const [role, setRole] = useState('');
    const [source, setSource] = useState('');
    const [sort, setSort] = useState('name');
    const [page, setPage] = useState(1);
    const [reloadKey, setReloadKey] = useState(0);

    const [data, setData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [message, setMessage] = useState(null);

    const [banTarget, setBanTarget] = useState(null);
    const [banReason, setBanReason] = useState('Cheating');
    const [banDuration, setBanDuration] = useState(RECOMMENDED_DURATION.Cheating);

    useEffect(() => {
        const timer = setTimeout(() => {
            setSearch(searchInput.trim());
            setPage(1);
        }, 300);
        return () => clearTimeout(timer);
    }, [searchInput]);

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            setLoading(true);
            setError(null);

            const params = { page, pageSize: PAGE_SIZE, sortBy: sort };
            if (search) params.search = search;
            if (role) params.role = role;
            if (source) params.source = source;

            try {
                const res = await apiClient.get('/api/users', { params });
                if (!cancelled) setData(res.data);
            } catch (err) {
                if (!cancelled) {
                    setError(err.response?.data?.detail || 'Kasutajate laadimine ebaõnnestus.');
                }
            } finally {
                if (!cancelled) setLoading(false);
            }
        };

        load();
        return () => {
            cancelled = true;
        };
    }, [search, role, source, sort, page, reloadKey]);

    const changeRole = async (user, newRole) => {
        if (newRole === user.role) return;
        if (!window.confirm(`Muuda kasutaja "${user.displayName}" roll: ${user.role} → ${newRole}?`)) return;

        try {
            setError(null);
            setMessage(null);
            await apiClient.put(`/api/users/${user.id}/role`, { role: newRole });
            setMessage(`${user.displayName}: roll on nüüd ${newRole}.`);
            setReloadKey((k) => k + 1);
        } catch (err) {
            setError(err.response?.data?.detail || 'Rolli muutmine ebaõnnestus.');
        }
    };

    const openBan = (user) => {
        setBanTarget(user);
        setBanReason('Cheating');
        setBanDuration(RECOMMENDED_DURATION.Cheating);
    };

    const submitBan = async (e) => {
        e.preventDefault();

        try {
            setError(null);
            setMessage(null);
            await apiClient.put(`/api/users/${banTarget.id}/block`, {
                isBlocked: true,
                reason: banReason,
                duration: banDuration,
            });
            setMessage(`${banTarget.displayName} on blokeeritud.`);
            setBanTarget(null);
            setReloadKey((k) => k + 1);
        } catch (err) {
            setError(err.response?.data?.detail || 'Blokeerimine ebaõnnestus.');
        }
    };

    const unblock = async (user) => {
        if (!window.confirm(`Kas avada kasutaja "${user.displayName}"?`)) return;

        try {
            setError(null);
            setMessage(null);
            await apiClient.put(`/api/users/${user.id}/block`, { isBlocked: false });
            setMessage(`${user.displayName} on avatud.`);
            setReloadKey((k) => k + 1);
        } catch (err) {
            setError(err.response?.data?.detail || 'Konto avamine ebaõnnestus.');
        }
    };

    const toggleBlock = (user) => {
        if (user.isBlocked) {
            unblock(user);
        } else {
            openBan(user);
        }
    };

    const resetFilters = () => {
        setSearchInput('');
        setSearch('');
        setRole('');
        setSource('');
        setSort('name');
        setPage(1);
    };

    const hasFilters = Boolean(search || role || source);

    return (
        <div className="container">
            <section className="hero" style={{ marginBottom: 20 }}>
                <div>
                    <span className="hero-kicker">Admin</span>
                    <h1 className="hero-title" style={{ fontSize: 40 }}>Kasutajad</h1>
                    <p className="hero-sub">
                        Otsi kasutajaid, filtreeri rolli ja allika järgi, muuda rolle ja blokeeri kontosid.
                    </p>
                </div>
                <div className="hero-stat">
                    <strong>{data?.totalCount ?? 0}</strong>
                    <span>kasutajat</span>
                </div>
            </section>

            <div className="tabs" role="tablist">
                {SOURCE_TABS.map((tab) => (
                    <button
                        key={tab.value || 'all'}
                        role="tab"
                        aria-selected={source === tab.value}
                        className={`tab ${source === tab.value ? 'active' : ''}`}
                        style={{ '--game': '#c6ff34' }}
                        onClick={() => {
                            setSource(tab.value);
                            setPage(1);
                        }}
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
                        value={searchInput}
                        onChange={(e) => setSearchInput(e.target.value)}
                        placeholder="Otsi nime, logini või e-posti järgi..."
                    />
                </div>

                <select
                    className="select toolbar-select"
                    value={role}
                    onChange={(e) => {
                        setRole(e.target.value);
                        setPage(1);
                    }}
                >
                    <option value="">Kõik rollid</option>
                    {ROLES.map((r) => (
                        <option key={r} value={r}>{r}</option>
                    ))}
                </select>

                <select
                    className="select toolbar-select"
                    value={sort}
                    onChange={(e) => {
                        setSort(e.target.value);
                        setPage(1);
                    }}
                >
                    {SORT_OPTIONS.map((o) => (
                        <option key={o.value} value={o.value}>{o.label}</option>
                    ))}
                </select>

                {hasFilters && (
                    <button className="btn btn-outline btn-sm" onClick={resetFilters}>
                        Lähtesta
                    </button>
                )}
            </div>

            {message && <div className="alert-success">{message}</div>}
            {error && <div className="alert-error">{error}</div>}

            <div className="panel">
                {loading && !data ? (
                    <p style={{ color: 'var(--text-muted)' }}>Laadin kasutajaid...</p>
                ) : !data || data.items.length === 0 ? (
                    <div style={{ textAlign: 'center', padding: 32 }}>
                        <h3 style={{ margin: '0 0 8px' }}>Kasutajaid ei leitud</h3>
                        <p style={{ margin: 0, color: 'var(--text-muted)' }}>
                            Proovi teist otsingut või filtrit.
                        </p>
                    </div>
                ) : (
                    <div style={{ overflowX: 'auto', opacity: loading ? 0.6 : 1 }}>
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Nimi</th>
                                    <th>E-post</th>
                                    <th>Allikas</th>
                                    <th>Roll</th>
                                    <th>Konto</th>
                                </tr>
                            </thead>
                            <tbody>
                                {data.items.map((u) => {
                                    const lichess = isLichessUser(u);
                                    const isMe = me && Number(me.id) === u.id;

                                    return (
                                        <tr key={u.id} style={u.isBlocked ? { opacity: 0.75 } : undefined}>
                                            <td>
                                                <strong>{u.displayName}</strong>{' '}
                                                <span style={{ color: 'var(--text-muted)', fontSize: 12 }}>
                                                    @{u.username}
                                                </span>
                                            </td>
                                            <td style={{ color: 'var(--text-muted)' }}>{u.email}</td>
                                            <td>
                                                <span
                                                    className="badge"
                                                    style={
                                                        lichess
                                                            ? { color: '#38bdf8', borderColor: 'rgba(56, 189, 248, 0.4)' }
                                                            : undefined
                                                    }
                                                >
                                                    {lichess ? 'Lichess' : 'Kohalik'}
                                                </span>
                                            </td>
                                            <td>
                                                <select
                                                    className="select"
                                                    style={{ width: 'auto', height: 36 }}
                                                    value={u.role}
                                                    disabled={isMe || loading}
                                                    title={isMe ? 'Oma rolli ei saa muuta' : undefined}
                                                    onChange={(e) => changeRole(u, e.target.value)}
                                                >
                                                    {ROLES.map((r) => (
                                                        <option key={r} value={r}>{r}</option>
                                                    ))}
                                                </select>
                                            </td>
                                            <td>
                                                {u.isBlocked ? (
                                                    <div style={{ marginBottom: 6 }}>
                                                        <span
                                                            className="badge"
                                                            style={{ color: 'var(--danger)', borderColor: 'rgba(255, 77, 109, 0.4)' }}
                                                        >
                                                            {shortReason(u.banReason)}
                                                        </span>
                                                        <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>
                                                            kuni {formatUntil(u)}
                                                        </div>
                                                    </div>
                                                ) : (
                                                    <div style={{ marginBottom: 6, color: 'var(--text-muted)', fontSize: 13 }}>
                                                        Aktiivne
                                                    </div>
                                                )}
                                                <button
                                                    className="btn btn-sm"
                                                    style={
                                                        u.isBlocked
                                                            ? { border: '1px solid var(--border-strong)' }
                                                            : { background: 'var(--danger-soft)', color: 'var(--danger)' }
                                                    }
                                                    disabled={isMe || loading}
                                                    title={isMe ? 'Oma kontot ei saa blokeerida' : undefined}
                                                    onClick={() => toggleBlock(u)}
                                                >
                                                    {u.isBlocked ? 'Ava' : 'Blokeeri'}
                                                </button>
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            {data && data.totalPages > 1 && (
                <div className="pager">
                    <button className="page-btn" disabled={page <= 1} onClick={() => setPage(page - 1)}>
                        ←
                    </button>
                    <span style={{ alignSelf: 'center', color: 'var(--text-muted)' }}>
                        {page} / {data.totalPages}
                    </span>
                    <button
                        className="page-btn"
                        disabled={page >= data.totalPages}
                        onClick={() => setPage(page + 1)}
                    >
                        →
                    </button>
                </div>
            )}

            {banTarget && (
                <div className="modal-backdrop" onClick={() => setBanTarget(null)}>
                    <div
                        className="modal-box"
                        style={{ maxWidth: 520 }}
                        onClick={(e) => e.stopPropagation()}
                    >
                        <h3 style={{ margin: '0 0 6px' }}>Blokeeri kasutaja</h3>
                        <p style={{ margin: '0 0 18px', color: 'var(--text-muted)' }}>
                            {banTarget.displayName} (@{banTarget.username})
                        </p>

                        <form onSubmit={submitBan}>
                            <div className="form-group">
                                <label className="form-label">Põhjus</label>
                                <select
                                    className="select"
                                    value={banReason}
                                    onChange={(e) => {
                                        setBanReason(e.target.value);
                                        setBanDuration(RECOMMENDED_DURATION[e.target.value]);
                                    }}
                                >
                                    {BAN_REASONS.map((r) => (
                                        <option key={r.value} value={r.value}>{r.label}</option>
                                    ))}
                                </select>
                            </div>

                            <div className="form-group">
                                <label className="form-label">Kestus</label>
                                <select
                                    className="select"
                                    value={banDuration}
                                    onChange={(e) => setBanDuration(e.target.value)}
                                >
                                    {BAN_DURATIONS.map((d) => (
                                        <option key={d.value} value={d.value}>{d.label}</option>
                                    ))}
                                </select>
                                <small style={{ color: 'var(--text-muted)' }}>
                                    Soovitus: {durationLabel(RECOMMENDED_DURATION[banReason])}
                                </small>
                            </div>

                            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
                                <button type="button" className="btn btn-outline" onClick={() => setBanTarget(null)}>
                                    Tühista
                                </button>
                                <button
                                    type="submit"
                                    className="btn"
                                    style={{ background: 'var(--danger-soft)', color: 'var(--danger)' }}
                                >
                                    Blokeeri
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
} 