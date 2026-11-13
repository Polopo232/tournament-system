import axios from 'axios';

export const TOKEN_KEY = 'tournament_jwt_token';
export const USER_KEY = 'tournament_user';

export const apiClient = axios.create({
    baseURL: 'http://localhost:5000',
    headers: {
        'Content-Type': 'application/json',
    },
});

apiClient.interceptors.request.use((config) => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

function parseJwtUser(token) {
    try {
        const payloadBase64 = token.split('.')[1];
        const base64 = payloadBase64.replace(/-/g, '+').replace(/_/g, '/');
        const json = JSON.parse(atob(base64));

        const rawId =
            json['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
            json.sub ||
            json.nameid ||
            json.id;

        const username =
            json['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
            json.unique_name ||
            json.name ||
            json.username ||
            'Kasutaja';

        const role =
            json['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
            json.role ||
            'Player';

        return {
            id: rawId ? Number(rawId) : null,
            username,
            role,
        };
    } catch {
        return { id: null, username: 'Kasutaja', role: 'Player' };
    }
}

export function saveAuthSession(data) {
    const token = data.token || data.accessToken;
    localStorage.setItem(TOKEN_KEY, token);

    const fromJwt = parseJwtUser(token);
    const userObj = {
        id: data.id ?? data.userId ?? data.user?.id ?? fromJwt.id,
        username: data.username ?? data.user?.username ?? fromJwt.username,
        role: data.role ?? data.user?.role ?? fromJwt.role,
    };

    localStorage.setItem(USER_KEY, JSON.stringify(userObj));
}

export function clearAuthSession() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
}

export function getStoredUser() {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return null;

    const fromJwt = parseJwtUser(token);
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return fromJwt;

    try {
        const parsed = JSON.parse(raw);
        return {
            id: parsed.id || fromJwt.id,
            username: parsed.username || fromJwt.username,
            role: parsed.role || fromJwt.role,
        };
    } catch {
        return fromJwt;
    }
}

export function getStoredToken() {
    return localStorage.getItem(TOKEN_KEY);
}