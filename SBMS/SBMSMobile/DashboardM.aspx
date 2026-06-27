<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="DashboardM.aspx.cs" Inherits="SBMS.DashboardM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Data Fusion</title>
    <link rel="shortcut icon" href="../images/datafusionicon.ico" type="image/x-icon" />
    <link rel="stylesheet" href="css/main.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/main.css") %>" />
    <link rel="stylesheet" href="css/mobile-ui.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/mobile-ui.css") %>" />
    <link rel="manifest" href="manifest.json" />
    <meta name="theme-color" content="#4282C1" />
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
    <meta name="apple-mobile-web-app-title" content="Data Fusion" />
    <link rel="apple-touch-icon" href="icons/icon-192.png" />
    <meta name="format-detection" content="telephone=no" />
    <style>
        .dash-brand {
            background: var(--mob-surface);
            text-align: center;
            padding: 1.5rem 1rem 1rem;
            border-bottom: 0.5px solid var(--mob-border);
        }
        .dash-logo { max-width: 180px; height: auto; }
        .dash-welcome {
            margin-top: .5rem;
            font-size: .85rem;
            color: var(--mob-text-muted);
        }
        .dash-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: .85rem;
            padding: 1rem;
        }
        .dash-tile {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            background: var(--mob-surface);
            border-radius: var(--mob-radius-lg);
            padding: 1.1rem .75rem .9rem;
            box-shadow: var(--mob-shadow-card);
            border: 0.5px solid var(--mob-border);
            cursor: pointer;
            text-decoration: none;
            -webkit-tap-highlight-color: transparent;
            transition: box-shadow .15s, transform .1s, border-color .15s;
        }
        .dash-tile:active {
            box-shadow: var(--mob-shadow-card-active);
            border-color: var(--mob-brand);
            transform: scale(.97);
        }
        .dash-tile:focus-visible {
            outline: none;
            box-shadow: var(--mob-focus);
        }
        .dash-tile img {
            width: 100%;
            max-width: 90px;
            height: 70px;
            object-fit: contain;
        }
        .dash-tile-label {
            margin-top: .6rem;
            font-size: .82rem;
            font-weight: 600;
            color: var(--mob-brand);
            text-align: center;
            line-height: 1.25;
        }
        .dash-tile-wide {
            grid-column: 1 / -1;
            max-width: calc(50% - .425rem);
            margin: 0 auto;
            width: 100%;
        }
    </style>
</head>
<body>
<form id="form1" runat="server">
    <%-- Icon library (outline, 2px, rounded) — referenced via <use href="#i-…"> --%>
    <svg class="mob-ico-defs" aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg">
        <symbol id="i-warehouse" viewBox="0 0 24 24"><path d="M22 8.35V20a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V8.35a2 2 0 0 1 1.26-1.86l8-3.2a2 2 0 0 1 1.48 0l8 3.2A2 2 0 0 1 22 8.35Z"/><path d="M6 18h12"/><path d="M6 14h12"/><path d="M6 10h12"/></symbol>
        <symbol id="i-hash" viewBox="0 0 24 24"><path d="M4 9h16"/><path d="M4 15h16"/><path d="M10 3 8 21"/><path d="M16 3l-2 18"/></symbol>
        <symbol id="i-inbox" viewBox="0 0 24 24"><path d="M22 12h-6l-2 3h-4l-2-3H2"/><path d="M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"/></symbol>
        <symbol id="i-package" viewBox="0 0 24 24"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><path d="M3.3 7 12 12l8.7-5"/><path d="M12 22V12"/></symbol>
        <symbol id="i-clipboard" viewBox="0 0 24 24"><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/><rect x="8" y="2" width="8" height="4" rx="1"/><path d="M9 12l2 2 4-4"/></symbol>
        <symbol id="i-transfer" viewBox="0 0 24 24"><path d="m17 2 4 4-4 4"/><path d="M3 11v-1a4 4 0 0 1 4-4h14"/><path d="m7 22-4-4 4-4"/><path d="M21 13v1a4 4 0 0 1-4 4H3"/></symbol>
        <symbol id="i-chart" viewBox="0 0 24 24"><path d="M3 3v18h18"/><path d="M18 17V9"/><path d="M13 17V5"/><path d="M8 17v-3"/></symbol>
    </svg>


    <%-- Top bar --%>
    <div class="mob-topbar">
        <div class="mob-topbar-left"></div>
        <span class="mob-topbar-title"><svg class="mob-ico" aria-hidden="true"><use href="#i-warehouse"/></svg>Data Fusion</span>
        <div class="mob-topbar-right">
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click"
                CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <%-- Branding strip --%>
    <div class="dash-brand">
        <a href="https://mydatafusion.online" title="My Data Fusion website">
            <img src="../images/Logo.png" class="dash-logo" alt="Data Fusion" style="padding-top:2.5em; width:5em" />
        </a>
        <div class="dash-welcome">Welcome, <asp:Label ID="lblUsername" runat="server" /></div>
    </div>

    <%-- Navigation grid --%>
    <div class="dash-grid">

        <asp:LinkButton ID="imgbCount" runat="server" OnClick="imgbCount_Click"
            CssClass="dash-tile" ToolTip="Count goods against a PO; the web does the receiving">
            <span class="dash-tile-ico"><svg class="mob-ico" aria-hidden="true"><use href="#i-hash"/></svg></span>
            <span class="dash-tile-label">Count and Check</span>
        </asp:LinkButton>

        <asp:LinkButton ID="imgbReceive" runat="server" OnClick="imgbReceive_Click"
            CssClass="dash-tile" ToolTip="Receive goods against a Purchase Order, straight into locations">
            <span class="dash-tile-ico"><svg class="mob-ico" aria-hidden="true"><use href="#i-inbox"/></svg></span>
            <span class="dash-tile-label">Receive</span>
        </asp:LinkButton>

        <asp:LinkButton ID="imgbRec" runat="server" OnClick="imgbRec_Click"
            CssClass="dash-tile" ToolTip="Put received stock away into bins/locations">
            <span class="dash-tile-ico"><svg class="mob-ico" aria-hidden="true"><use href="#i-package"/></svg></span>
            <span class="dash-tile-label">Put-away</span>
        </asp:LinkButton>

        <asp:LinkButton ID="ibtnPickSlips" runat="server" OnClick="ibtnPickSlips_Click"
            CssClass="dash-tile" ToolTip="Open Picking Slips">
            <span class="dash-tile-ico"><svg class="mob-ico" aria-hidden="true"><use href="#i-clipboard"/></svg></span>
            <span class="dash-tile-label">Picking Slips</span>
        </asp:LinkButton>

        <asp:LinkButton ID="lbtnBinTransfer" runat="server" OnClick="imgbQuickMove_Click"
            CssClass="dash-tile" ToolTip="Quick Move — scan one item between two bins/locations">
            <span class="dash-tile-ico"><svg class="mob-ico" aria-hidden="true"><use href="#i-transfer"/></svg></span>
            <span class="dash-tile-label">Quick Move</span>
        </asp:LinkButton>

        <asp:LinkButton ID="ibtnStockMove" runat="server" OnClick="ibtnPickSlips_Click"
            CssClass="dash-tile" ToolTip="Warehouse Transfer">
            <span class="dash-tile-ico"><svg class="mob-ico" aria-hidden="true"><use href="#i-warehouse"/></svg></span>
            <span class="dash-tile-label">Stock Move</span>
        </asp:LinkButton>

        <asp:LinkButton ID="ibtnStockCount" runat="server" OnClick="ibtnStockCount_Click"
            CssClass="dash-tile dash-tile-wide" ToolTip="Stock Count">
            <span class="dash-tile-ico"><svg class="mob-ico" aria-hidden="true"><use href="#i-chart"/></svg></span>
            <span class="dash-tile-label">Stock Count</span>
        </asp:LinkButton>

    </div>

</form>

<%-- PWA install banner --%>
<div id="pwaPrompt" style="display:none;position:fixed;bottom:1rem;left:1rem;right:1rem;
     background:var(--mob-brand);color:#fff;padding:.9rem 1rem;border-radius:var(--mob-radius-lg);
     z-index:1000;box-shadow:var(--mob-shadow-modal);">
    <p style="margin:0 0 .6rem;font-size:.9rem;font-weight:600;">
        Install Data Fusion on your home screen for the best experience.
    </p>
    <div style="display:flex;gap:.6rem;">
        <button onclick="installPWA()"
            style="flex:1;background:#fff;color:var(--mob-brand);border:none;padding:.5rem;
                   border-radius:var(--mob-radius-md);font-weight:700;font-size:.85rem;cursor:pointer;">
            Install
        </button>
        <button onclick="document.getElementById('pwaPrompt').style.display='none'"
            style="flex:1;background:transparent;color:#fff;border:1.5px solid rgba(255,255,255,.6);
                   padding:.5rem;border-radius:8px;font-size:.85rem;cursor:pointer;">
            Later
        </button>
    </div>
</div>

<script>
    let deferredPrompt;
    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferredPrompt = e;
        document.getElementById('pwaPrompt').style.display = 'block';
    });
    function installPWA() {
        if (deferredPrompt) {
            deferredPrompt.prompt();
            deferredPrompt.userChoice.then(function () { deferredPrompt = null; });
        }
        document.getElementById('pwaPrompt').style.display = 'none';
    }
    if (!window.matchMedia('(display-mode: standalone)').matches) {
        window.addEventListener('load', function () {
            document.body.style.height = (window.screen.height + 50) + 'px';
            setTimeout(function () { window.scrollTo(0, 1); }, 50);
            setTimeout(function () { document.body.style.height = ''; }, 600);
        });
    }
</script>
</body>
</html>
