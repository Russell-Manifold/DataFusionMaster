<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="StockCountsM.aspx.cs" Inherits="SBMS.StockCountsM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Stock Counts</title>
    <link rel="shortcut icon" href="../images/datafusionicon.ico" type="image/x-icon" />
    <link rel="stylesheet" href="css/main.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/main.css") %>" />
    <link rel="stylesheet" href="css/mobile-ui.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/mobile-ui.css") %>" />
    <link rel="manifest" href="../SBMSMobile/manifest.json" />
    <meta name="theme-color" content="#4282C1" />
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
    <meta name="apple-mobile-web-app-title" content="Data Fusion" />
    <link rel="apple-touch-icon" href="../SBMSMobile/icons/icon-192.png" />
    <meta name="format-detection" content="telephone=no" />
    <style>
        .mob-store-picker {
            background: var(--mob-surface);
            border-radius: var(--mob-radius-lg);
            border: 0.5px solid var(--mob-border);
            padding: 1rem;
            margin: .5rem 1rem;
            box-shadow: var(--mob-shadow-card);
        }
        .mob-store-picker label {
            display: block;
            font-size: .82em;
            font-weight: 600;
            color: var(--mob-text);
            margin-bottom: .35em;
        }
        .mob-store-picker select {
            width: 100%;
            height: 2.8em;
            border: 1.5px solid #ccc;
            border-radius: var(--mob-radius-sm);
            padding: 0 .5em;
            font-size: 1em;
            background: var(--mob-surface);
            transition: border-color .15s, box-shadow .15s;
        }
        .mob-store-picker select:focus { outline: none; border-color: var(--mob-brand); box-shadow: var(--mob-focus); }
        .mob-start-btn {
            display: block;
            margin: 1rem;
            padding: .9rem;
            background: var(--mob-success);
            color: #fff;
            font-size: 1.1em;
            font-weight: 700;
            text-align: center;
            border-radius: var(--mob-radius-md);
            text-decoration: none;
            transition: background .15s;
        }
        .mob-start-btn:active {
            background: var(--mob-success-dark);
        }
        .mob-start-btn.disabled {
            background: #bbb;
            cursor: not-allowed;
        }
    </style>
</head>
<body>
<form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" />
    <%-- Icon library (outline, 2px, rounded) — referenced via <use href="#i-…"> --%>
    <svg class="mob-ico-defs" aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg">
        <symbol id="i-house" viewBox="0 0 24 24"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><path d="M9 22V12h6v10"/></symbol>
        <symbol id="i-chart" viewBox="0 0 24 24"><path d="M3 3v18h18"/><path d="M18 17V9"/><path d="M13 17V5"/><path d="M8 17v-3"/></symbol>
        <symbol id="i-search" viewBox="0 0 24 24"><circle cx="11" cy="11" r="8"/><path d="M21 21l-4.3-4.3"/></symbol>
        <symbol id="i-package" viewBox="0 0 24 24"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><path d="M3.3 7 12 12l8.7-5"/><path d="M12 22V12"/></symbol>
    </svg>


    <%-- Loading overlay --%>
    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="upMain">
        <ProgressTemplate>
            <div style="position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:999;
                        display:flex;align-items:center;justify-content:center;color:#fff;font-size:1.1em;">
                Loading&hellip;
            </div>
        </ProgressTemplate>
    </asp:UpdateProgress>

    <%-- Top bar --%>
    <div class="mob-topbar">
        <div class="mob-topbar-left">
            <asp:LinkButton ID="lbtnBack" runat="server" OnClick="lbtnBack_Click" CssClass="mob-topbar-back">&#8592; Back</asp:LinkButton>
            <asp:LinkButton ID="lbtnHome" runat="server" OnClick="lbtnHome_Click" CssClass="mob-topbar-icon" title="Home" aria-label="Home"><svg class="mob-ico" aria-hidden="true"><use href="#i-house"/></svg></asp:LinkButton>
        </div>
        <span class="mob-topbar-title"><svg class="mob-ico" aria-hidden="true"><use href="#i-chart"/></svg>Stock Counts</span>
        <div class="mob-topbar-right">
            <asp:Label ID="lblUsername" runat="server" style="display:none;" />
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click" CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>

        <%-- Count selector --%>
        <asp:Panel ID="pnlCountSelect" runat="server" CssClass="mob-filterbar" DefaultButton="lbtnLoadCount"
            style="justify-content:space-between;">
            <asp:DropDownList ID="ddCounts" runat="server"
                style="flex:1;min-width:0;max-width:none;height:2.7em;border:1px solid #ccc;border-radius:.4em;padding:0 .4em;font-size:.9em;" />
            <asp:LinkButton ID="lbtnLoadCount" runat="server" OnClick="lbtnLoadCount_Click"
                CssClass="mob-find-btn" style="margin-left:auto;" title="Load" aria-label="Load"><svg class="mob-ico" aria-hidden="true"><use href="#i-search"/></svg></asp:LinkButton>
            <asp:CheckBox ID="chkShowClosed" runat="server" AutoPostBack="true"
                OnCheckedChanged="chkShowClosed_CheckedChanged" Text="Closed"
                style="font-size:.75em;color:#888;white-space:nowrap;margin-left:.5em;" />
        </asp:Panel>

        <%-- Count header --%>
        <asp:Panel ID="pnlCountHeader" runat="server" Visible="false">
            <div class="mob-doc-header">
                <div class="mob-doc-header-main">
                    <span class="mob-doc-num"><asp:Label ID="lblCountRef" runat="server" /></span>
                    <span class="mob-doc-secondary" style="margin-left:.5em;">
                        <asp:Label ID="lblCountStatus" runat="server" />
                    </span>
                </div>
                <div class="mob-doc-meta">
                    <span class="mob-doc-meta-item">Created: <asp:Label ID="lblCountDate" runat="server" /></span>
                    <span class="mob-doc-meta-item">By: <asp:Label ID="lblCreatedBy" runat="server" /></span>
                    <span class="mob-doc-badge"><asp:Label ID="lblLineCount" runat="server" /> line(s) total</span>
                </div>
            </div>

            <%-- Store picker --%>
            <div class="mob-store-picker">
                <label><svg class="mob-ico" aria-hidden="true"><use href="#i-package"/></svg> Pick the store you are counting:</label>
                <asp:DropDownList ID="ddStore" runat="server" style="width:100%;height:2.8em;border:1.5px solid #ccc;border-radius:.5em;padding:0 .5em;font-size:1em;background:#fff;" />
            </div>

            <%-- Start Counting button --%>
            <asp:LinkButton ID="lbtnStartCounting" runat="server" OnClick="lbtnStartCounting_Click"
                CssClass="mob-start-btn"><svg class="mob-ico" aria-hidden="true"><use href="#i-chart"/></svg> Start Counting</asp:LinkButton>
        </asp:Panel>

    </ContentTemplate>
    </asp:UpdatePanel>
</form>

<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
<script>
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
