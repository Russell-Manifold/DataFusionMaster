<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="StockMoveM.aspx.cs" Inherits="SBMS.StockMoveM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Stock Move</title>
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
        .sm-toggle-row { display: flex; gap: .5em; margin: 3.2em .6em .2em; }
        .sm-toggle { flex: 1; text-align: center; height: 2.9em; line-height: 2.9em; border-radius: var(--mob-radius-md);
            font-weight: 700; font-size: .95em; border: 1px solid var(--mob-border); }
        .sm-toggle.on { background: var(--mob-brand); color: #fff; border-color: var(--mob-brand); }
        .sm-toggle.off { background: var(--mob-surface); color: var(--mob-text-muted); }

        .qm-prompt { padding: .4em 1em .2em; font-size: .92em; font-weight: 600; color: var(--mob-ink); }
        .qm-card { background: var(--mob-surface); border: 0.5px solid var(--mob-border);
            border-radius: var(--mob-radius-lg); box-shadow: var(--mob-shadow-card); margin: .5em .6em; padding: .9em 1em; }
        .sm-trfnum { font-size: .8em; color: var(--mob-brand); font-weight: 700; }
        .sm-route { font-size: 1.05em; font-weight: 700; color: var(--mob-ink-strong); margin-top: .2em; }

        .sm-line { display: flex; align-items: baseline; justify-content: space-between; padding: .2em 0; }
        .sm-line-code { font-weight: 700; color: var(--mob-ink-strong); }
        .sm-line-descr { font-size: .82em; color: var(--mob-text-muted); margin-top: .1em; }
        .sm-line-sent { font-size: .8em; color: var(--mob-text-faint); }
        .sm-line-rem { font-size: .85em; margin-top: .25em; }
        .sm-line-rem strong { color: var(--mob-ink-strong); }
        .sm-recv-row { display: flex; gap: .5em; align-items: center; margin-top: .45em; }
        .sm-recv-row input { flex: 1; }
        .sm-recv-row a { min-width: 5.6em; text-align: center; height: 2.7em; line-height: 2.7em; border-radius: var(--mob-radius-md); font-weight: 700; }
        .sm-done { color: var(--mob-dest); font-weight: 700; font-size: .85em; margin-top: .4em; }

        .conv-actions { display: flex; gap: .5em; margin-top: .2em; padding: 0 .6em; }
        .conv-actions a { flex: 1; text-align: center; height: 3.1em; line-height: 3.1em; border-radius: var(--mob-radius-md); font-weight: 700; }

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
        <symbol id="i-move" viewBox="0 0 24 24"><path d="M5 9l-3 3 3 3"/><path d="M2 12h13"/><path d="M19 15l3-3-3-3"/><path d="M22 12H9"/></symbol>
    </svg>

    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="upMain">
        <ProgressTemplate>
            <div style="position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:999;display:flex;align-items:center;justify-content:center;color:#fff;font-size:1.1em;">Moving&hellip;</div>
        </ProgressTemplate>
    </asp:UpdateProgress>

    <div class="mob-topbar">
        <div class="mob-topbar-left">
            <asp:LinkButton ID="lbtnTopBack" runat="server" OnClick="lbtnTopBack_Click" CssClass="mob-topbar-back">&#8592; Back</asp:LinkButton>
            <asp:LinkButton ID="lbtnTopHome" runat="server" OnClick="lbtnTopHome_Click" CssClass="mob-topbar-icon" title="Home"><svg class="mob-ico" aria-hidden="true"><use href="#i-house"/></svg></asp:LinkButton>
        </div>
        <span class="mob-topbar-title"><svg class="mob-ico" aria-hidden="true"><use href="#i-move"/></svg>Stock Move</span>
        <div class="mob-topbar-right">
            <asp:Label ID="lblUsername" runat="server" style="display:none;" />
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click" CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfActionToken" runat="server" />

        <div class="sm-toggle-row">
            <asp:LinkButton ID="lbtnModeOut" runat="server" OnClick="lbtnModeOut_Click" CssClass="sm-toggle on">Send Out &#8594;</asp:LinkButton>
            <asp:LinkButton ID="lbtnModeIn" runat="server" OnClick="lbtnModeIn_Click" CssClass="sm-toggle off">&#8594; Receive In</asp:LinkButton>
        </div>

        <div class="qm-prompt"><asp:Label ID="lblModeTitle" runat="server" /> &mdash; <asp:Label ID="lblPrompt" runat="server" /></div>

        <asp:Panel ID="pnlScan" runat="server" CssClass="mob-scan-bar">
            <div class="mob-scan-wrap">
                <asp:TextBox ID="txtScan" runat="server" placeholder="Scan Transfer&hellip;" AutoPostBack="true" OnTextChanged="txtScan_TextChanged"
                    autocomplete="off" autocorrect="off" autocapitalize="off" style="font-size:16px;" />
                <asp:LinkButton ID="lbtnLoadDoc" runat="server" OnClick="lbtnLoadDoc_Click" Visible="false" CssClass="mob-action-btn" style="min-width:4em;">Load</asp:LinkButton>
                <asp:LinkButton ID="lbtnClearScan" runat="server" OnClick="lbtnClearScan_Click" CssClass="mob-scan-clear" title="Clear">&#10005;</asp:LinkButton>
            </div>
        </asp:Panel>

        <asp:Label ID="lblFeedback" runat="server" CssClass="mob-feedback" />

        <asp:Panel ID="pnlHdr" runat="server" Visible="false" CssClass="qm-card">
            <div class="sm-trfnum">Transfer <asp:Label ID="lblTrfNum" runat="server" /></div>
            <div class="sm-route"><asp:Label ID="lblRoute" runat="server" /></div>
        </asp:Panel>

        <asp:Panel ID="pnlLines" runat="server" Visible="false">
            <asp:Repeater ID="rptLines" runat="server" OnItemCommand="rptLines_ItemCommand">
                <ItemTemplate>
                    <div class="qm-card" style="margin-top:.4em;margin-bottom:.4em;">
                        <div class="sm-line">
                            <span class="sm-line-code"><%# Eval("ItemCode") %></span>
                            <span class="sm-line-sent">sent <%# Eval("Sent") %></span>
                        </div>
                        <div class="sm-line-descr"><%# Eval("ItemDescription") %></div>
                        <div class="sm-line-rem">In transit: <strong><%# Eval("RemainingText") %></strong></div>
                        <asp:Panel runat="server" Visible='<%# InMode && (decimal)Eval("Remaining") > 0 %>' CssClass="sm-recv-row">
                            <asp:TextBox runat="server" ID="txtRecv" TextMode="Number" Text='<%# Eval("RemainingText") %>' style="font-size:16px;" />
                            <asp:LinkButton runat="server" ID="lbtnRecv" CommandName="receive" CommandArgument='<%# Eval("TrfLID") %>' CssClass="mob-btn-primary">Receive</asp:LinkButton>
                        </asp:Panel>
                        <asp:Panel runat="server" Visible='<%# InMode && (decimal)Eval("Remaining") <= 0 %>' CssClass="sm-done">&#10003; Fully received</asp:Panel>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </asp:Panel>

        <asp:Panel ID="pnlSendOut" runat="server" Visible="false" CssClass="conv-actions">
            <asp:LinkButton ID="lbtnSendOut" runat="server" OnClick="lbtnSendOut_Click" CssClass="mob-btn-primary">Send whole transfer to transit &#8594;</asp:LinkButton>
        </asp:Panel>

        <div style="text-align:center;padding:.4em;">
            <asp:LinkButton ID="lbtnRestart" runat="server" OnClick="lbtnRestart_Click" Visible="false" CssClass="mob-selection-change-link">Scan another transfer</asp:LinkButton>
        </div>

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
