import { useCallback, useEffect, useState } from 'react';
import { createPortal } from 'react-dom';

function DuelModal({ duel, onClose }) {
    useEffect(() => {
        const onKey = (e) => {
            if (e.key === 'Escape') onClose();
        };
        document.addEventListener('keydown', onKey);
        document.body.style.overflow = 'hidden';

        return () => {
            document.removeEventListener('keydown', onKey);
            document.body.style.overflow = '';
        };
    }, [onClose]);

    // Portaal: okno asub otse body-s, nii et teised paneelid seda ei kata
    return createPortal(
        <div className="modal-backdrop duel-backdrop" onClick={onClose}>
            <div
                className="duel-modal"
                role="dialog"
                aria-modal="true"
                onClick={(e) => e.stopPropagation()}
            >
                <div className="duel-modal-head">
                    <span>
                        {duel.whiteName} <span className="duel-vs-inline">vs</span> {duel.blackName}
                    </span>
                    <button className="btn btn-outline btn-sm" onClick={onClose}>
                        Sulge
                    </button>
                </div>

                <div className="duel-modal-body">
                    <iframe
                        title={`Mäng ${duel.gameId}`}
                        src={`https://lichess.org/embed/game/${duel.gameId}?theme=auto&bg=auto`}
                        className="duel-modal-frame"
                        allow="fullscreen"
                    />
                </div>

                <a href={duel.url} target="_blank" rel="noreferrer" className="duel-modal-link">
                    Ava Lichessis ↗
                </a>
            </div>
        </div>,
        document.body
    );
}

export function LiveDuels({ duels }) {
    const [activeDuel, setActiveDuel] = useState(null);
    const close = useCallback(() => setActiveDuel(null), []);

    if (!duels || duels.length === 0) return null;

    return (
        <div className="panel">
            <h2 className="section-title">
                Käimasolevad mängud <span className="live-badge">LIVE</span>
            </h2>

            <div className="duel-grid">
                {duels.map((d) => (
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
                            <button className="btn btn-primary btn-sm" onClick={() => setActiveDuel(d)}>
                                Vaata siin
                            </button>
                        </div>
                    </div>
                ))}
            </div>

            {activeDuel && <DuelModal duel={activeDuel} onClose={close} />}
        </div>
    );
}