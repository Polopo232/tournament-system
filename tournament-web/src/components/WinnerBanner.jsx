export function WinnerBanner({ tournament }) {
    if (!tournament || tournament.status !== 'Finished' || !tournament.winnerName) {
        return null;
    }

    return (
        <div
            className="panel"
            style={{
                display: 'flex',
                alignItems: 'center',
                gap: 14,
                marginBottom: 20,
                borderLeft: '4px solid var(--primary)',
            }}
        >
            <span style={{ fontSize: 28 }}>🏆</span>
            <div>
                <div
                    style={{
                        fontSize: 12,
                        color: 'var(--text-muted)',
                        textTransform: 'uppercase',
                        letterSpacing: 0.5,
                    }}
                >
                    Turniiri võitja
                </div>
                <div style={{ fontSize: 20, fontWeight: 700 }}>{tournament.winnerName}</div>
            </div>
        </div>
    );
}