(function () {
    const API_URL = '/GDMSStatusBridge/api/site-status';
    const REFRESH_MS = 120000; // 2 minutes

    async function refreshAlarmColours() {
        try {
            const response = await fetch(API_URL, { cache: 'no-store' });
            if (!response.ok) {
                console.warn('[GDMS Status Bridge] API returned:', response.status);
                return;
            }

            const sites = await response.json();

            sites.forEach(site => {
                const dot = document.getElementById(site.tag);
                if (!dot) return;

                const colour = site.statusName === 'Alarmed' ? '#ff0000' : '#ffff00';

                dot.setAttribute('fill', colour);
                dot.style.fill = colour;
                dot.setAttribute('data-status', site.statusName || '');
                dot.setAttribute('data-last-status-refresh', new Date().toISOString());
            });
        } catch (error) {
            console.error('[GDMS Status Bridge] Failed to refresh alarm colours:', error);
        }
    }

    refreshAlarmColours();
    setInterval(refreshAlarmColours, REFRESH_MS);
})();
