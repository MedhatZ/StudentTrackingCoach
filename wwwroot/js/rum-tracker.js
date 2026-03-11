(function () {
    const cfg = window.gradPathRum || {};
    if (!cfg.enabled) {
        return;
    }

    const sessionKey = "gradpath_rum_session";
    let sessionId = localStorage.getItem(sessionKey);
    if (!sessionId) {
        sessionId = (Math.random().toString(36).slice(2) + Date.now().toString(36)).slice(0, 24);
        localStorage.setItem(sessionKey, sessionId);
    }

    const postJson = async (url, payload) => {
        try {
            await fetch(url, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload),
                keepalive: true
            });
        } catch {
            // Silent fail: RUM must never break UX.
        }
    };

    const getPerfMetric = (name) => {
        const entries = performance.getEntriesByName(name);
        if (!entries || entries.length === 0) {
            return null;
        }

        const latest = entries[entries.length - 1];
        return typeof latest.startTime === "number" ? Math.round(latest.startTime) : null;
    };

    const inferDeviceType = () => {
        const ua = navigator.userAgent || "";
        if (/Mobi|Android|iPhone|iPad/i.test(ua)) {
            return "Mobile";
        }
        return "Desktop";
    };

    const parseRegion = () => {
        const tz = Intl.DateTimeFormat().resolvedOptions().timeZone || "";
        return tz.split("/")[0] || "Unknown";
    };

    if (cfg.trackPageViews) {
        window.addEventListener("load", () => {
            const navEntries = performance.getEntriesByType("navigation");
            const nav = navEntries && navEntries.length ? navEntries[0] : null;

            const payload = {
                path: window.location.pathname + window.location.search,
                pageTitle: document.title || "",
                ttfbMs: nav ? Math.round(nav.responseStart) : null,
                firstContentfulPaintMs: getPerfMetric("first-contentful-paint"),
                timeToInteractiveMs: nav ? Math.round(nav.domInteractive) : null,
                pageLoadCompleteMs: nav ? Math.round(nav.loadEventEnd) : null,
                browser: navigator.userAgent,
                deviceType: inferDeviceType(),
                screenSize: `${window.screen.width}x${window.screen.height}`,
                region: parseRegion(),
                sessionId: sessionId
            };

            postJson("/rum/page-view", payload);
        });
    }

    if (cfg.trackUserActions) {
        document.addEventListener("click", (event) => {
            const target = event.target;
            if (!(target instanceof HTMLElement)) {
                return;
            }

            const actionName = target.dataset.rumAction || "Click";
            const payload = {
                actionName: actionName,
                path: window.location.pathname + window.location.search,
                elementType: target.tagName,
                elementId: target.id || target.getAttribute("name") || target.className || "",
                sessionId: sessionId,
                region: parseRegion(),
                success: true
            };

            postJson("/rum/action", payload);
        }, { passive: true });
    }
})();
