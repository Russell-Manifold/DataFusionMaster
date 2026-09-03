(function () {
    "use strict";

    var salesChart = null;
    var custChart = null;

    function cssVar(name, fallback) {
        var v = getComputedStyle(document.documentElement).getPropertyValue(name);
        return (v && v.trim()) || fallback;
    }

    function isDark() {
        return document.documentElement.getAttribute("data-theme") === "dark";
    }

    function money(v) {
        try {
            return new Intl.NumberFormat("en-ZA", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(v);
        } catch (e) {
            return v;
        }
    }

    function moneyShort(v) {
        var abs = Math.abs(v);
        if (abs >= 1000000) return (v / 1000000).toFixed(1) + "m";
        if (abs >= 1000) return (v / 1000).toFixed(0) + "k";
        return String(v);
    }

    function baseTheme() {
        return {
            chart: {
                fontFamily: "inherit",
                foreColor: cssVar("--df-text-muted", "#64748B"),
                toolbar: { show: false },
                animations: { enabled: !window.matchMedia("(prefers-reduced-motion: reduce)").matches }
            },
            tooltip: { theme: isDark() ? "dark" : "light" },
            grid: { borderColor: cssVar("--df-border", "#E2E8F0"), strokeDashArray: 4 }
        };
    }

    function buildSales(data) {
        var el = document.getElementById("dfChartSales");
        if (!el || typeof ApexCharts === "undefined") return;
        var t = baseTheme();

        var opts = {
            chart: Object.assign({ type: "area", height: 300 }, t.chart),
            colors: [cssVar("--df-primary", "#1E40AF"), cssVar("--df-accent", "#D97706")],
            series: [
                { name: "Sales", data: data.sales || [] },
                { name: "Gross Profit", data: data.gp || [] }
            ],
            xaxis: { categories: data.months || [], axisBorder: { show: false }, axisTicks: { show: false } },
            yaxis: { labels: { formatter: moneyShort } },
            dataLabels: { enabled: false },
            stroke: { curve: "smooth", width: 2 },
            fill: { type: "gradient", gradient: { shadeIntensity: 1, opacityFrom: 0.35, opacityTo: 0.05 } },
            legend: { position: "top", horizontalAlign: "right" },
            tooltip: Object.assign({ y: { formatter: money } }, t.tooltip),
            grid: t.grid,
            noData: { text: "No sales in the last 12 months" }
        };

        if (salesChart) { salesChart.destroy(); }
        salesChart = new ApexCharts(el, opts);
        salesChart.render();
    }

    function buildCustomers(data) {
        var el = document.getElementById("dfChartCustomers");
        if (!el || typeof ApexCharts === "undefined") return;
        var t = baseTheme();

        var opts = {
            chart: Object.assign({ type: "bar", height: 260 }, t.chart),
            colors: [cssVar("--df-primary", "#1E40AF")],
            series: [{ name: "Sales", data: data.custSales || [] }],
            xaxis: { categories: data.custLabels || [], labels: { formatter: moneyShort } },
            plotOptions: { bar: { horizontal: true, borderRadius: 4, barHeight: "60%" } },
            dataLabels: { enabled: false },
            tooltip: Object.assign({ y: { formatter: money } }, t.tooltip),
            grid: t.grid,
            noData: { text: "No customer sales in the last 12 months" }
        };

        if (custChart) { custChart.destroy(); }
        custChart = new ApexCharts(el, opts);
        custChart.render();
    }

    function renderAll() {
        var data = window.dfChartData;
        if (!data) return;
        buildSales(data);
        buildCustomers(data);
    }

    document.addEventListener("DOMContentLoaded", renderAll);
    // Charts bake in their colours at render time, so rebuild them when the
    // light/dark toggle fires (df-ui.js dispatches this).
    document.addEventListener("df:themechange", renderAll);
})();
