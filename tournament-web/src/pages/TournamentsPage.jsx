import { useEffect, useState } from 'react';
import { apiClient } from '../api/client';
import { Link } from 'react-router-dom';

export function TournamentsPage() {
    const [tournaments, setTournaments] = useState([]);
    const [search, setSearch] = useState('');
    const [status, setStatus] = useState('');
    const [discipline, setDiscipline] = useState('');
    const [sortBy, setSortBy] = useState('startDate');
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    const pageSize = 6;

    useEffect(() => {
        const fetchTournaments = async () => {
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
                if (discipline) params.discipline = discipline;

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
        };

        fetchTournaments();
    }, [search, status, discipline, sortBy, page]);

    const handleFilterChange = (setter, value) => {
        setter(value);
        setPage(1);
    };

    return (
        <div style={{ maxWidth: 1000, margin: '24px auto', padding: '0 16px' }}>
            <h1>Turniirid</h1>

            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, marginBottom: 24 }}>
                <input
                    type="text"
                    value={search}
                    onChange={(e) => handleFilterChange(setSearch, e.target.value)}
                    placeholder="Otsi turniiri nime järgi..."
                    style={{ flex: '1 1 220px', padding: 8 }}
                />

                <select
                    value={status}
                    onChange={(e) => handleFilterChange(setStatus, e.target.value)}
                    style={{ padding: 8 }}
                >
                    <option value="">Kõik staatused</option>
                    <option value="Open">Avatud (Open)</option>
                    <option value="InProgress">Käimas (InProgress)</option>
                    <option value="Completed">Lõppenud (Completed)</option>
                </select>

                <select
                    value={discipline}
                    onChange={(e) => handleFilterChange(setDiscipline, e.target.value)}
                    style={{ padding: 8 }}
                >
                    <option value="">Kõik alad / mängud</option>
                    <option value="CS2">CS2</option>
                    <option value="Dota2">Dota 2</option>
                    <option value="Chess">Male (Chess)</option>
                    <option value="TableTennis">Lauatennis (TableTennis)</option>
                </select>

                <select
                    value={sortBy}
                    onChange={(e) => handleFilterChange(setSortBy, e.target.value)}
                    style={{ padding: 8 }}
                >
                    <option value="startDate">Sorteeri: Kuupäev</option>
                    <option value="participants">Sorteeri: Osalejate arv</option>
                    <option value="name">Sorteeri: Nimi</option>
                </select>
            </div>

            {error && (
                <div style={{ background: '#fee2e2', color: '#991b1b', padding: 12, borderRadius: 6, marginBottom: 16 }}>
                    {error}
                </div>
            )}

            {loading ? (
                <p>Laadin turniire...</p>
            ) : tournaments.length === 0 ? (
                <p>Vastavaid turniire ei leitud.</p>
            ) : (
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(290px, 1fr))', gap: 16 }}>
                    {tournaments.map((t) => {
                        const count = t.currentParticipantsCount ?? t.participantsCount ?? 0;
                        return (
                            <div
                                key={t.id}
                                style={{ border: '1px solid #ddd', borderRadius: 8, padding: 16, display: 'flex', flexDirection: 'column', gap: 8 }}
                            >
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span style={{ fontSize: 12, fontWeight: 'bold', padding: '2px 8px', background: '#eef2ff', borderRadius: 4 }}>
                    {t.discipline}
                  </span>
                                    <span style={{ fontSize: 12, color: '#555' }}>{t.status}</span>
                                </div>

                                <h3 style={{ margin: '4px 0' }}>
                                    <Link to={`/tournaments/${t.id}`} style={{ textDecoration: 'none', color: '#2563eb' }}>
                                        {t.name}
                                    </Link>
                                </h3>
                                <p style={{ margin: 0, fontSize: 14, color: '#666' }}>Formaat: {t.format}</p>
                                <p style={{ margin: 0, fontSize: 14 }}>
                                    Osalejad: <strong>{count} / {t.maxParticipants}</strong>
                                </p>
                                <p style={{ margin: 0, fontSize: 13, color: '#666' }}>
                                    Algus: {new Date(t.startDate).toLocaleDateString('et-EE')}
                                </p>
                            </div>
                        );
                    })}
                </div>
            )}

            <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 8, marginTop: 28 }}>
                <button
                    disabled={page <= 1}
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    style={{ padding: '6px 12px', cursor: page <= 1 ? 'not-allowed' : 'pointer' }}
                >
                    Eelmine
                </button>

                {Array.from({ length: totalPages }, (_, idx) => idx + 1).map((pageNum) => (
                    <button
                        key={pageNum}
                        onClick={() => setPage(pageNum)}
                        style={{
                            padding: '6px 12px',
                            fontWeight: pageNum === page ? 'bold' : 'normal',
                            background: pageNum === page ? '#2563eb' : '#f3f4f6',
                            color: pageNum === page ? '#fff' : '#111',
                            border: '1px solid #d1d5db',
                            borderRadius: 4,
                            cursor: 'pointer',
                        }}
                    >
                        {pageNum}
                    </button>
                ))}

                <button
                    disabled={page >= totalPages}
                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                    style={{ padding: '6px 12px', cursor: page >= totalPages ? 'not-allowed' : 'pointer' }}
                >
                    Järgmine
                </button>
            </div>
        </div>
    );
}