import { useState } from 'react';

export function LiveDuels({ duels }) {
    const [openId, setOpenId] = useState(null);

    if (!duels || duels.length === 0) return null;

    return (
        <div className="panel">
            <h2 className="section-title">
                Käimasolevad mängud <span className="live-badge">LIVE</span>
            </h2>

            <div className="duel-grid">
                {duels.map((d) => {
                    const isOpen = openId === d.gameId;
                    return (
                        <div key={d.gameId} className="duel-card">
                            <div className="duel-side">
                                <span className="duel-name">{d.whiteName}</span>
                                <span className="duel-rating">{d.whiteRating}</span>
                            </div>

                            <div className="duel-vs">vs</div>

                            <div className="duel-side">
                                <span className="duel-name">{d.blackName}</span>
                                <span className="duel-rating">{d.blackRating}</span>
                            </div>

                            <div className="duel-actions">
                                <a href={d.url} target="_blank" rel="noreferrer" className="btn btn-outline btn-sm">
                                    Ava Lichessis
                                </a>
                                <button
                                    className="btn btn-primary btn-sm"
                                    onClick={() => setOpenId(isOpen ? null : d.gameId)}
                                >
                                    {isOpen ? 'Sulge' : 'Vaata siin'}
                                </button>
                            </div>

                            {isOpen && (
                                <iframe
                                    title={`Mäng ${d.gameId}`}
                                    src={`https://lichess.org/embed/game/${d.gameId}?theme=auto&bg=auto`}
                                    className="duel-embed"
                                    allow="fullscreen"
                                />
                            )}
                        </div>
                    );
                })}
            </div>
        </div>
    );
}