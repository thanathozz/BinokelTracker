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

window.BinokelLang = {
    save: (lang) => localStorage.setItem('binokel-lang', lang),
    load: () => localStorage.getItem('binokel-lang') ?? 'de'
};

window.PullToRefresh = {
    init: function (dotNetRef) {
        const THRESHOLD = 70;
        let startY = 0, pulling = false, refreshing = false;

        const el = document.createElement('div');
        el.id = 'ptr-spinner';
        el.style.cssText = [
            'position:fixed', 'top:-50px', 'left:50%', 'transform:translateX(-50%)',
            'z-index:400', 'pointer-events:none',
            'width:36px', 'height:36px', 'border-radius:50%',
            'background:var(--card)', 'box-shadow:0 2px 10px rgba(0,0,0,0.25)',
            'display:flex', 'align-items:center', 'justify-content:center',
            'font-size:20px', 'color:var(--accent)', 'opacity:0',
            'transition:top 0.18s ease, opacity 0.18s'
        ].join(';');
        el.textContent = '↻';
        document.body.appendChild(el);

        const scrollTop = () => {
            const c = document.querySelector('.app-content');
            return c ? c.scrollTop : 0;
        };

        document.addEventListener('touchstart', e => {
            if (scrollTop() <= 2 && !refreshing) {
                startY = e.touches[0].clientY;
                pulling = true;
            }
        }, { passive: true });

        document.addEventListener('touchmove', e => {
            if (!pulling) return;
            const dy = e.touches[0].clientY - startY;
            if (dy > 8) {
                el.style.transition = 'none';
                el.style.top  = Math.min((dy - 8) * 0.42 - 46, 16) + 'px';
                el.style.opacity = Math.min((dy - 8) / THRESHOLD, 1);
            }
        }, { passive: true });

        document.addEventListener('touchend', e => {
            if (!pulling) return;
            pulling = false;
            el.style.transition = 'top 0.18s ease, opacity 0.18s';
            const dy = e.changedTouches[0].clientY - startY;
            if (dy >= THRESHOLD && !refreshing) {
                refreshing = true;
                el.style.top = '16px';
                el.style.opacity = '1';
                el.classList.add('ptr-spinning');
                dotNetRef.invokeMethodAsync('Reload').then(() => {
                    refreshing = false;
                    el.classList.remove('ptr-spinning');
                    el.style.opacity = '0';
                    setTimeout(() => { el.style.top = '-50px'; }, 200);
                });
            } else {
                el.style.top = '-50px';
                el.style.opacity = '0';
            }
        }, { passive: true });
    }
};
