window.BinokelAuth = {
    saveSession: (accessToken, refreshToken, expiresAt) => {
        localStorage.setItem('binokel-access-token',  accessToken);
        localStorage.setItem('binokel-refresh-token', refreshToken);
        localStorage.setItem('binokel-expires-at',    String(expiresAt));
    },
    loadSession: () => ({
        accessToken:  localStorage.getItem('binokel-access-token')  ?? '',
        refreshToken: localStorage.getItem('binokel-refresh-token') ?? '',
        expiresAt:    parseInt(localStorage.getItem('binokel-expires-at') ?? '0', 10)
    }),
    clearSession: () => {
        localStorage.removeItem('binokel-access-token');
        localStorage.removeItem('binokel-refresh-token');
        localStorage.removeItem('binokel-expires-at');
    }
};

window.BinokelTheme = {
    apply: (name) => {
        document.documentElement.setAttribute('data-theme', name);
        localStorage.setItem('binokel-theme', name);
    },
    load: () => {
        const saved = localStorage.getItem('binokel-theme') ?? 'braun';
        document.documentElement.setAttribute('data-theme', saved);
        return saved;
    }
};
