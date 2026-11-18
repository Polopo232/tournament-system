import { Link } from 'react-router-dom';
import { formatDiscipline } from '../utils/formatters';

export function PlayerWins({ wins }) {
    const list = wins ?? [];

    return (
        <div className="panel" style={{ marginBottom: 20 }}>
            <h2 className="section-title">
                Võidud <span className="elo-tag">{list.length}</span>
            </h2>

            {list.length === 0 ? (
                <p style={{ margin: 0, color: 'var(--text-muted)' }}>Mängijal pole veel turniirivõite.</p>
            ) : (
                <div className="wins-grid">
                    {list.map((w) => (
                        <Link key={w.tournamentId} to={`/tournaments/${w.tournamentId}`} className="win-card">
                            <span className="win-trophy">🏆</span>
                            <div>
                                <div className="win-name">{w.tournamentName}</div>
                                <div className="win-meta">
                                    {formatDiscipline(w.game)} · {new Date(w.date).toLocaleDateString('et-EE')}
                                </div>
                            </div>
                        </Link>
                    ))}
                </div>
            )}
        </div>
    );
}