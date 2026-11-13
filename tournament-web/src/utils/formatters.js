export function formatStatus(status) {
    const map = {
        Registration: 'Avatud registreerimiseks',
        Open: 'Avatud registreerimiseks',
        InProgress: 'Käimasolev',
        Finished: 'Lõppenud',
        Completed: 'Lõppenud',
        Cancelled: 'Tühistatud',
    };
    return map[status] || status;
}

export function getStatusBadgeClass(status) {
    if (status === 'Registration' || status === 'Open') return 'badge badge-open';
    if (status === 'InProgress') return 'badge badge-progress';
    return 'badge badge-completed';
}

export function formatMatchStatus(status) {
    const map = {
        Pending: 'Ootel',
        Scheduled: 'Ootel',
        InProgress: 'Mängib',
        Completed: 'Lõppenud',
    };
    return map[status] || status;
}

export function formatDiscipline(discipline) {
    const map = {
        CS2: 'Counter-Strike 2',
        Dota2: 'Dota 2',
        Chess: 'Male',
        TableTennis: 'Lauatennis',
    };
    return map[discipline] || discipline;
}

export function formatTournamentFormat(format) {
    const map = {
        SingleElimination: 'Ühe väljalangemisega',
        RoundRobin: 'Kõik kõigiga',
    };
    return map[format] || format;
}