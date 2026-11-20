export const BAN_REASONS = [
    { value: 'Cheating', label: 'Pettus: keelatud tarkvara, cheat või exploit', short: 'Pettus' },
    { value: 'Smurfing', label: 'Smurfimine: teine konto allpool oma oskustaset', short: 'Smurfimine' },
    { value: 'BanEvasion', label: 'Blokeeringust hoidumine: teine konto blokeeringu ajal', short: 'Blokeeringust hoidumine' },
    { value: 'StreamSniping', label: 'Stream sniping: vastase otseülekande vaatamine mängu ajal', short: 'Stream sniping' },
    { value: 'MatchIntegrity', label: 'Mängu terviklikkus: bugide kuritarvitamine, manipuleerimine', short: 'Mängu terviklikkus' },
    { value: 'SevereAbuse', label: 'Raske väärkasutus ja sisurikkumised', short: 'Väärkasutus' },
];

export const BAN_DURATIONS = [
    { value: 'Minute1', label: 'Test: 1 minut' },
    { value: 'Day1', label: '1 päev' },
    { value: 'Days3', label: '3 päeva' },
    { value: 'Week1', label: '1 nädal' },
    { value: 'Month1', label: '1 kuu' },
    { value: 'Year1', label: '1 aasta' },
    { value: 'Years5', label: '5 aastat' },
    { value: 'Permanent', label: 'Igavesti' },
];

/** Soovituslik kestus põhjuse järgi (FACEIT-i loogika lihtsustatult). */
export const RECOMMENDED_DURATION = {
    Cheating: 'Years5',
    Smurfing: 'Month1',
    BanEvasion: 'Permanent',
    StreamSniping: 'Month1',
    MatchIntegrity: 'Month1',
    SevereAbuse: 'Permanent',
};

export function shortReason(value) {
    return BAN_REASONS.find((r) => r.value === value)?.short ?? value;
}

export function durationLabel(value) {
    return BAN_DURATIONS.find((d) => d.value === value)?.label ?? value;
}

/** Lõppaeg kuvamiseks. Kui lõppaeg puudub, on blokeering püsiv. */
export function formatUntil(ban) {
    if (!ban?.blockedUntil) return 'püsivalt';
    return new Date(ban.blockedUntil).toLocaleString('et-EE');
} 