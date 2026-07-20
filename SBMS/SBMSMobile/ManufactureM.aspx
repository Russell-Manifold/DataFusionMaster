<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ManufactureM.aspx.cs" Inherits="SBMS.ManufactureM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Manufacture</title>
    <link rel="shortcut icon" href="../images/datafusionicon.ico" type="image/x-icon" />
    <link rel="stylesheet" href="../SBMSMobile/css/main.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/main.css") %>" />
    <link rel="stylesheet" href="../SBMSMobile/css/mobile-ui.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/mobile-ui.css") %>" />
    <link rel="manifest" href="../SBMSMobile/manifest.json" />
    <meta name="theme-color" content="#4282C1" />
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
    <meta name="apple-mobile-web-app-title" content="Data Fusion" />
    <link rel="apple-touch-icon" href="../SBMSMobile/icons/icon-192.png" />
    <meta name="format-detection" content="telephone=no" />
    <style>
        .qm-prompt { padding: .4em 1em .2em; margin-top: 3.2em; font-size: .92em; font-weight: 600; color: var(--mob-ink); }
        .qm-card { background: var(--mob-surface); border: 0.5px solid var(--mob-border);
            border-radius: var(--mob-radius-lg); box-shadow: var(--mob-shadow-card); margin: .5em .6em; padding: .9em 1em; }
        .qm-code { font-weight: 700; font-size: 1.05em; color: var(--mob-ink-strong); }
        .qm-unit { font-size: .74em; color: #999; margin-top: .08em; }
        .qm-descr { font-size: .84em; color: var(--mob-text-muted); margin-top: .25em; line-height: 1.35; }
        .qm-wonum { font-size: .8em; color: var(--mob-brand); font-weight: 700; margin-bottom: .3em; }
        .qm-remain { margin-top: .5em; font-size: .9em; }
        .qm-remain strong { color: var(--mob-ink-strong); }

        .conv-bom { margin-top: .6em; border-top: 1px solid var(--mob-divider); padding-top: .4em; }
        .conv-bom-head { font-size: .72em; color: var(--mob-text-faint); font-weight: 700; text-transform: uppercase; margin-bottom: .25em; }
        .conv-bom-row { display: flex; justify-content: space-between; font-size: .85em; padding: .18em 0; border-bottom: 1px solid #f2f2f2; }
        .conv-bom-code { font-weight: 600; color: var(--mob-ink-strong); }
        .conv-bom-qty { color: var(--mob-text-muted); }

        .ho-field { margin-top: .55em; }
        .ho-field label { display: block; font-size: .74em; color: var(--mob-text-faint); font-weight: 700; text-transform: uppercase; margin-bottom: .2em; }
        .ho-field select { width: 100%; }

        .conv-actions { display: flex; gap: .5em; margin-top: .8em; }
        .conv-actions a { flex: 1; text-align: center; height: 3.1em; line-height: 3.1em; border-radius: var(--mob-radius-md); font-weight: 700; }

        .mob-done-list { margin: 1em .6em 6em; border-top: 1px solid var(--mob-divider); padding-top: .7em; }
        .mob-done-head { font-weight: 700; color: var(--mob-dest); font-size: .95em; margin-bottom: .4em; }
        .mob-done-row { display: flex; align-items: baseline; gap: .5em; padding: .35em 0; border-bottom: 1px solid #f0f0f0; font-size: .9em; }
        .mob-done-qty { font-weight: 700; min-width: 2.6em; text-align: right; color: var(--mob-ink-strong); }
        .mob-done-item { font-weight: 600; color: var(--mob-ink-strong); }
        .mob-done-route { color: var(--mob-text-muted); }
        .mob-done-time { margin-left: auto; color: var(--mob-text-faint); font-size: .85em; }

        .mob-toast { position: fixed; left: 50%; bottom: 5.2em; transform: translateX(-50%) translateY(1em);
            background: var(--mob-dest); color: #fff; padding: .8em 1.3em; border-radius: var(--mob-radius-md);
            font-size: 1.05em; font-weight: 600; box-shadow: var(--mob-shadow-modal);
            opacity: 0; transition: opacity .25s, transform .25s; z-index: 1000; pointer-events: none; max-width: 90%; text-align: center; }
        .mob-toast.show { opacity: 1; transform: translateX(-50%) translateY(0); }
    </style>
</head>
<body>
<form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" />
    <svg class="mob-ico-defs" aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg">
        <symbol id="i-house" viewBox="0 0 24 24"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><path d="M9 22V12h6v10"/></symbol>
        <symbol id="i-convert" viewBox="0 0 24 24"><path d="M21 2v6h-6"/><path d="M3 12a9 9 0 0 1 15-6.7L21 8"/><path d="M3 22v-6h6"/><path d="M21 12a9 9 0 0 1-15 6.7L3 16"/></symbol>
    </svg>

    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="upMain">
        <ProgressTemplate>
            <div style="position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:999;display:flex;align-items:center;justify-content:center;color:#fff;font-size:1.1em;">Posting&hellip;</div>
        </ProgressTemplate>
    </asp:UpdateProgress>

    <div class="mob-topbar">
        <div class="mob-topbar-left">
            <asp:LinkButton ID="lbtnTopBack" runat="server" OnClick="lbtnTopBack_Click" CssClass="mob-topbar-back">&#8592; Back</asp:LinkButton>
            <asp:LinkButton ID="lbtnTopHome" runat="server" OnClick="lbtnTopHome_Click" CssClass="mob-topbar-icon" title="Home"><svg class="mob-ico" aria-hidden="true"><use href="#i-house"/></svg></asp:LinkButton>
        </div>
        <span class="mob-topbar-title"><svg class="mob-ico" aria-hidden="true"><use href="#i-convert"/></svg>Manufacture</span>
        <div class="mob-topbar-right">
            <asp:Label ID="lblUsername" runat="server" style="display:none;" />
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click" CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfActionToken" runat="server" />

        <div class="qm-prompt"><asp:Label ID="lblPrompt" runat="server" /></div>

        <asp:Panel ID="pnlScan" runat="server" CssClass="mob-scan-bar">
            <div class="mob-scan-wrap">
                <asp:TextBox ID="txtScan" runat="server" placeholder="Scan WO&hellip;" AutoPostBack="true" OnTextChanged="txtScan_TextChanged"
                    autocomplete="off" autocorrect="off" autocapitalize="off" style="font-size:16px;" />
                <asp:LinkButton ID="lbtnLoadDoc" runat="server" OnClick="lbtnLoadDoc_Click" Visible="false" CssClass="mob-action-btn" style="min-width:4em;">Load</asp:LinkButton>
                <asp:LinkButton ID="lbtnClearScan" runat="server" OnClick="lbtnClearScan_Click" CssClass="mob-scan-clear" title="Clear">&#10005;</asp:LinkButton>
            </div>
        </asp:Panel>

        <asp:Label ID="lblFeedback" runat="server" CssClass="mob-feedback" />

        <asp:Panel ID="pnlWO" runat="server" Visible="false" CssClass="qm-card">
            <div class="qm-wonum">WO <asp:Label ID="lblWONum" runat="server" /></div>
            <div class="qm-code"><asp:Label ID="lblOutItem" runat="server" /></div>
            <div class="qm-unit"><asp:Label ID="lblOutUnit" runat="server" /></div>
            <div class="qm-descr"><asp:Label ID="lblOutDescr" runat="server" /></div>
            <div class="qm-remain">Outstanding: <strong><asp:Label ID="lblRemaining" runat="server" /></strong></div>
            <div class="conv-bom">
                <div class="conv-bom-head">Consumes per unit</div>
                <asp:Repeater ID="rptBom" runat="server">
                    <ItemTemplate>
                        <div class="conv-bom-row">
                            <span class="conv-bom-code"><%# Eval("Code") %></span>
                            <span class="conv-bom-qty"><%# Eval("PerUnit") %></span>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </asp:Panel>

        <asp:Panel ID="pnlMake" runat="server" Visible="false" CssClass="qm-card">
            <div class="ho-field" style="margin-top:0;">
                <label>From store (consume raw materials)</label>
                <asp:DropDownList ID="ddlStore" runat="server" CssClass="mob-lot-select" />
            </div>
            <div class="ho-field">
                <label>Destination store (for "Make &amp; move")</label>
                <asp:DropDownList ID="ddlToStore" runat="server" CssClass="mob-lot-select" />
            </div>
            <div class="mob-qty-input-row" style="margin-top:.6em;">
                <span class="mob-qty-label">Make</span>
                <asp:TextBox ID="txtQty" runat="server" TextMode="Number" />
                <span class="mob-qty-label" style="margin-left:.4em;">of the outstanding qty</span>
            </div>
            <div class="conv-actions">
                <asp:LinkButton ID="lbtnMakeHere" runat="server" OnClick="lbtnMakeHere_Click" CssClass="mob-btn-secondary">Make &amp; keep here</asp:LinkButton>
                <asp:LinkButton ID="lbtnMakeMove" runat="server" OnClick="lbtnMakeMove_Click" CssClass="mob-btn-primary">Make &amp; move &#8594;</asp:LinkButton>
            </div>
        </asp:Panel>

        <div style="text-align:center;padding:.2em;">
            <asp:LinkButton ID="lbtnRestart" runat="server" OnClick="lbtnRestart_Click" Visible="false" CssClass="mob-selection-change-link">Start over</asp:LinkButton>
        </div>

        <asp:Panel ID="pnlDone" runat="server" Visible="false" CssClass="mob-done-list">
            <div class="mob-done-head">&#10003; Made this session (<asp:Label ID="lblDoneCount" runat="server" />)</div>
            <asp:Repeater ID="rptDone" runat="server">
                <ItemTemplate>
                    <div class="mob-done-row">
                        <span class="mob-done-qty"><%# Eval("Qty", "{0:0.##}") %></span>
                        <span class="mob-done-item"><%# Eval("ItemCode") %></span>
                        <span class="mob-done-route">&#8594; <%# Eval("Store") %></span>
                        <span class="mob-done-time"><%# Eval("TimeText") %></span>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </asp:Panel>

    </ContentTemplate>
    </asp:UpdatePanel>

<script>
    function focusScanBox() { var b = document.getElementById('<%= txtScan.ClientID %>'); if (b) { b.focus(); } }
    function showToast(msg) {
        var t = document.getElementById('mobToast');
        if (!t) { t = document.createElement('div'); t.id = 'mobToast'; t.className = 'mob-toast'; document.body.appendChild(t); }
        t.innerHTML = msg; void t.offsetWidth; t.classList.add('show');
        clearTimeout(t._hide); t._hide = setTimeout(function () { t.classList.remove('show'); }, 2500);
    }
    var prm = Sys && Sys.WebForms && Sys.WebForms.PageRequestManager.getInstance();
    if (prm) { prm.add_endRequest(function () { focusScanBox(); }); }
    window.onload = function () { focusScanBox(); };
</script>
    </form>
</body>
</html>
