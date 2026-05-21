<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="DashboardM.aspx.cs" Inherits="SBMS.DashboardM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Data Fusion</title>
    <link rel="shortcut icon" href="../images/datafusionicon.ico" type="image/x-icon" />
    <link rel="stylesheet" href="css/main.css" />
    <link rel="stylesheet" href="css/mobile-ui.css" />
    <link rel="manifest" href="manifest.json" />
    <meta name="theme-color" content="#4282C1" />
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
    <meta name="apple-mobile-web-app-title" content="Data Fusion" />
    <link rel="apple-touch-icon" href="icons/icon-192.png" />
    <meta name="format-detection" content="telephone=no" />
    <style>
        .dash-brand {
            background: #fff;
            text-align: center;
            padding: 1.5rem 1rem 1rem;
            border-bottom: 1px solid #e8eaed;
        }
        .dash-logo { max-width: 180px; height: auto; }
        .dash-welcome {
            margin-top: .5rem;
            font-size: .85rem;
            color: #666;
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
            background: #fff;
            border-radius: 14px;
            padding: 1.1rem .75rem .9rem;
            box-shadow: 0 2px 8px rgba(0,0,0,.07);
            border: 1.5px solid #e8eaed;
            cursor: pointer;
            text-decoration: none;
            -webkit-tap-highlight-color: transparent;
            transition: box-shadow .15s, transform .1s;
        }
        .dash-tile:active {
            box-shadow: 0 1px 3px rgba(0,0,0,.1);
            transform: scale(.97);
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
            color: #4282C1;
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

    <%-- Top bar --%>
    <div class="mob-topbar">
        <div class="mob-topbar-left"></div>
        <span class="mob-topbar-title">&#128640; Data Fusion</span>
        <div class="mob-topbar-right">
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click"
                CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <%-- Branding strip --%>
    <div class="dash-brand">
        <a href="https://mydatafusion.online" title="My Data Fusion website">
            <img src="../images/logo.png" class="dash-logo" alt="Data Fusion" style="padding-top:2.5em; width:5em" />
        </a>
        <div class="dash-welcome">Welcome, <asp:Label ID="lblUsername" runat="server" /></div>
    </div>

    <%-- Navigation grid --%>
    <div class="dash-grid">

        <asp:LinkButton ID="imgbRec" runat="server" OnClick="imgbRec_Click"
            CssClass="dash-tile" ToolTip="Outstanding Purchase Orders and Receiving">
            <img src="../images/receivingM.png" alt="Receiving" />
            <span class="dash-tile-label">Receiving</span>
        </asp:LinkButton>

        <asp:LinkButton ID="ibtnPickSlips" runat="server" OnClick="ibtnPickSlips_Click"
            CssClass="dash-tile" ToolTip="Open Picking Slips">
            <img src="../images/PickSlipsM.png" alt="Picking Slips" />
            <span class="dash-tile-label">Picking Slips</span>
        </asp:LinkButton>

        <asp:LinkButton ID="lbtnBinTransfer" runat="server" OnClick="ibtnPickSlips_Click"
            CssClass="dash-tile" ToolTip="Bin Transfer">
            <img src="../images/BinTrfM.png" alt="Bin Transfer" />
            <span class="dash-tile-label">Bin Transfer</span>
        </asp:LinkButton>

        <asp:LinkButton ID="ibtnStockMove" runat="server" OnClick="ibtnPickSlips_Click"
            CssClass="dash-tile" ToolTip="Warehouse Transfer">
            <img src="../images/WHTrf.png" alt="Stock Move" />
            <span class="dash-tile-label">Stock Move</span>
        </asp:LinkButton>

        <asp:LinkButton ID="ibtnStockCount" runat="server" OnClick="ibtnStockCount_Click"
            CssClass="dash-tile dash-tile-wide" ToolTip="Stock Count">
            <img src="../images/StockCountsM.png" alt="Stock Count" />
            <span class="dash-tile-label">Stock Count</span>
        </asp:LinkButton>

    </div>

</form>

<%-- PWA install banner --%>
<div id="pwaPrompt" style="display:none;position:fixed;bottom:1rem;left:1rem;right:1rem;
     background:#4282C1;color:#fff;padding:.9rem 1rem;border-radius:12px;
     z-index:1000;box-shadow:0 4px 16px rgba(0,0,0,.25);">
    <p style="margin:0 0 .6rem;font-size:.9rem;font-weight:600;">
        Install Data Fusion on your home screen for the best experience.
    </p>
    <div style="display:flex;gap:.6rem;">
        <button onclick="installPWA()"
            style="flex:1;background:#fff;color:#4282C1;border:none;padding:.5rem;
                   border-radius:8px;font-weight:700;font-size:.85rem;cursor:pointer;">
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
