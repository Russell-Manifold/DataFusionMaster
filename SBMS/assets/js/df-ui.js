(function () {
    "use strict";

    var THEME_KEY = "df-theme";
    var COLLAPSED_KEY = "df-nav-collapsed";

    function getStoredTheme() {
        var stored = localStorage.getItem(THEME_KEY);
        if (stored === "light" || stored === "dark") return stored;
        return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute("data-theme", theme);
        // Icon can live on the toggle button itself, or on a child element with id="dfThemeIcon".
        var icon = document.getElementById("dfThemeIcon") || document.getElementById("dfThemeToggle");
        if (!icon) return;
        // Pages are split across two Font Awesome versions (FA4 "fa-moon-o" on the
        // root main.css pages, FA5 "fa-moon" on the prologue pages), so the glyph
        // names are declared on the element rather than hard-coded here.
        var lightIcon = icon.getAttribute("data-icon-light") || "fa-moon-o";
        var darkIcon = icon.getAttribute("data-icon-dark") || "fa-sun-o";
        icon.classList.remove(lightIcon, darkIcon);
        icon.classList.add(theme === "dark" ? darkIcon : lightIcon);
    }

    // Anything that bakes theme colours in at render time (e.g. the dashboard
    // charts) listens for this so it can rebuild on toggle.
    function announceTheme(theme) {
        try {
            document.dispatchEvent(new CustomEvent("df:themechange", { detail: { theme: theme } }));
        } catch (e) { /* older browsers: charts simply keep their initial colours */ }
    }

    function applyCollapsed(collapsed) {
        var shell = document.getElementById("dfShell");
        if (shell) shell.classList.toggle("df-collapsed", collapsed);
        document.body.classList.toggle("df-nav-collapsed", collapsed);
    }

    // Apply immediately (before DOMContentLoaded where possible) to avoid a flash of the wrong theme.
    applyTheme(getStoredTheme());

    document.addEventListener("DOMContentLoaded", function () {
        applyTheme(getStoredTheme());
        applyCollapsed(localStorage.getItem(COLLAPSED_KEY) === "true");

        var themeBtn = document.getElementById("dfThemeToggle");
        if (themeBtn) {
            themeBtn.addEventListener("click", function () {
                var next = document.documentElement.getAttribute("data-theme") === "dark" ? "light" : "dark";
                localStorage.setItem(THEME_KEY, next);
                applyTheme(next);
                announceTheme(next);
            });
        }

        var navToggleBtn = document.getElementById("dfSidebarToggle");
        if (navToggleBtn) {
            navToggleBtn.addEventListener("click", function () {
                var collapsed = !document.body.classList.contains("df-nav-collapsed");
                localStorage.setItem(COLLAPSED_KEY, collapsed ? "true" : "false");
                applyCollapsed(collapsed);
            });
        }

        // Server labels used as banner content (e.g. lblWarn) still render an empty
        // <span> with no text; hide the wrapping banner in that case so an empty
        // colored strip isn't shown when there's nothing to say.
        document.querySelectorAll(".df-banner").forEach(function (banner) {
            var text = banner.textContent.replace(/\s+/g, "");
            if (text === "") banner.style.display = "none";
        });
    });
})();
