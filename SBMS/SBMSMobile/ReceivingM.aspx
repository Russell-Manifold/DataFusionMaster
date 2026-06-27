<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ReceivingM.aspx.cs"
         Inherits="SBMS.ReceivingM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Put-away</title>
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
        /* selection-strip + matched-line treatment now live in mobile-ui.css */

        /* Put away this session – running confirmation list */
        .mob-done-list { margin: 1em .6em 2em; border-top: 1px solid var(--mob-divider); padding-top: .7em; }
        .mob-done-head { font-weight: 700; color: var(--mob-dest); font-size: .95em; margin-bottom: .4em; }
        .mob-done-row  { display: flex; align-items: baseline; gap: .5em; padding: .35em 0;
                         border-bottom: 1px solid #f0f0f0; font-size: .9em; }
        .mob-done-qty   { font-weight: 700; min-width: 2.6em; text-align: right; color: var(--mob-ink-strong); }
        .mob-done-item  { font-weight: 600; color: var(--mob-ink-strong); }
        .mob-done-arrow { color: #999; }
        .mob-done-dest  { color: var(--mob-dest); font-weight: 600; }
        .mob-done-time  { margin-left: auto; color: var(--mob-text-faint); font-size: .85em; }

        /* Success toast */
        .mob-toast { position: fixed; left: 50%; bottom: 2.2em;
                     transform: translateX(-50%) translateY(1em);
                     background: var(--mob-dest); color: #fff; padding: .8em 1.3em;
                     border-radius: var(--mob-radius-md);
                     font-size: 1.05em; font-weight: 600; box-shadow: var(--mob-shadow-modal);
                     opacity: 0; transition: opacity .25s, transform .25s; z-index: 1000;
                     pointer-events: none; max-width: 90%; text-align: center; }
        .mob-toast.show { opacity: 1; transform: translateX(-50%) translateY(0); }
    </style>
</head>
<body>
<form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" />
    <%-- Icon library (outline, 2px, rounded) — referenced via <use href="#i-…"> --%>
    <svg class="mob-ico-defs" aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg">
        <symbol id="i-house" viewBox="0 0 24 24"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><path d="M9 22V12h6v10"/></symbol>
        <symbol id="i-package" viewBox="0 0 24 24"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><path d="M3.3 7 12 12l8.7-5"/><path d="M12 22V12"/></symbol>
        <symbol id="i-scan" viewBox="0 0 24 24"><path d="M3 7V5a2 2 0 0 1 2-2h2"/><path d="M17 3h2a2 2 0 0 1 2 2v2"/><path d="M21 17v2a2 2 0 0 1-2 2h-2"/><path d="M7 21H5a2 2 0 0 1-2-2v-2"/><path d="M7 12h10"/></symbol>
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
            <asp:LinkButton ID="lbtnTopBack" runat="server" OnClick="lbtnTopBack_Click" CssClass="mob-topbar-back">&#8592; Back</asp:LinkButton>
            <asp:LinkButton ID="lbtnTopHome" runat="server" OnClick="lbtnTopHome_Click" CssClass="mob-topbar-icon" title="Home" aria-label="Home"><svg class="mob-ico" aria-hidden="true"><use href="#i-house"/></svg></asp:LinkButton>
        </div>
        <span class="mob-topbar-title"><svg class="mob-ico" aria-hidden="true"><use href="#i-package"/></svg>Put-away</span>
        <div class="mob-topbar-right">
            <asp:Label ID="lblUsername" runat="server" style="display:none;" />
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click"
                CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>

        <%-- Source (holding) store header --%>
        <div class="mob-doc-header">
            <div class="mob-doc-header-main">
                <span class="mob-doc-num"><svg class="mob-ico" aria-hidden="true"><use href="#i-package"/></svg> From: <asp:Label ID="lblPONum" runat="server" /></span>
            </div>
            <div class="mob-doc-meta">
                <span class="mob-doc-badge"><asp:Label ID="lblLineCount" runat="server" /> line(s) in holding</span>
            </div>
        </div>

        <%-- Barcode scan bar --%>
        <div class="mob-scan-bar">
            <div class="mob-scan-wrap">
                <span class="mob-scan-ico"><svg class="mob-ico" aria-hidden="true"><use href="#i-scan"/></svg></span>
                <asp:TextBox ID="txtBarcode" runat="server" placeholder="Scan or type barcode&hellip;"
                    AutoPostBack="true" OnTextChanged="txtBarcode_TextChanged"
                    autocomplete="off" autocorrect="off" autocapitalize="off"
                    style="font-size:16px;" />
                <asp:LinkButton ID="lbtnClearScan" runat="server" OnClick="lbtnClearScan_Click"
                    CssClass="mob-scan-clear" title="Clear">&#10005;</asp:LinkButton>
            </div>
        </div>

        <%-- Scan feedback --%>
        <asp:Label ID="lblScanFeedback" runat="server" />

        <%-- Holding-stock cards --%>
        <div class="mob-linelist">
            <asp:Repeater ID="rptLines" runat="server">
                <ItemTemplate>
                    <asp:Panel ID="pnlCard" runat="server"
                        CssClass='<%# IsMatched(Eval("ItemID"), Eval("LotNumber")) ? "mob-linecard matched" : "mob-linecard" %>'>
                        <div class="mob-linecard-body">

                            <div class="mob-card-main">
                                <div class="mob-item-code"><%# Eval("ItemCode") %></div>
                                <div class="mob-item-unit"><%# Eval("Unit") %></div>
                                <div class="mob-item-descr"><%# Eval("ItemDescription") %></div>
                                <div class="mob-lot-label">
                                    <%# string.IsNullOrEmpty(Convert.ToString(Eval("LotNumber"))) ? "" : "Lot #: " + Eval("LotNumber") %>
                                </div>

                                <div class="mob-qty-row">
                                    <span class="mob-qty-badge ordered">In holding&nbsp;<%# Eval("QOH", "{0:0.##}") %></span>
                                    <asp:HiddenField ID="hfItemId" runat="server" Value='<%# Eval("ItemID") %>' />
                                    <asp:HiddenField ID="hfLot" runat="server" Value='<%# Eval("LotNumber") %>' />
                                </div>

                                <div class="mob-qty-input-row">
                                    <span class="mob-qty-label">Qty</span>
                                    <asp:TextBox ID="txtQty" runat="server" TextMode="Number"
                                        Text='<%# Eval("QOH", "{0:0.##}") %>'
                                        style="width:4.5em;height:2.7em;border:1px solid #ccc;border-radius:.45em;
                                               text-align:center;font-size:1.05em;font-weight:600;" />
                                    <span class="mob-qty-label" style="margin-left:.6em;">Loc</span>
                                    <asp:TextBox ID="txtLoc" runat="server"
                                        placeholder="Scan location"
                                        autocomplete="off" autocorrect="off" autocapitalize="off"
                                        style="flex:1;min-width:0;height:2.7em;border:1px solid #ccc;border-radius:.45em;
                                               text-align:left;padding:0 .55em;font-size:1.05em;" />
                                </div>
                            </div>

                            <asp:LinkButton ID="lbtnPutAway" runat="server"
                                CssClass="mob-action-btn"
                                OnClick="lbtnPutAway_Click">&#10004;</asp:LinkButton>

                        </div>
                    </asp:Panel>
                </ItemTemplate>
            </asp:Repeater>

            <asp:Label ID="lblEmpty" runat="server" CssClass="mob-empty"
                Visible="false" Text="&#10003; Holding store is clear." />
        </div>

        <%-- Put away this session (running confirmation; clears when you leave the page) --%>
        <asp:Panel ID="pnlDone" runat="server" Visible="false" CssClass="mob-done-list">
            <div class="mob-done-head">&#10003; Put away this session (<asp:Label ID="lblDoneCount" runat="server" />)</div>
            <asp:Repeater ID="rptDone" runat="server">
                <ItemTemplate>
                    <div class="mob-done-row">
                        <span class="mob-done-qty"><%# Eval("Qty", "{0:0.##}") %></span>
                        <span class="mob-done-item"><%# Eval("ItemCode") %></span>
                        <span class="mob-done-arrow">&#8594;</span>
                        <span class="mob-done-dest"><%# Eval("Dest") %></span>
                        <span class="mob-done-time"><%# Eval("TimeText") %></span>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </asp:Panel>

    </ContentTemplate>
    </asp:UpdatePanel>

<script>
    function focusScanBox() {
        var b = document.getElementById('<%= txtBarcode.ClientID %>');
        if (b) b.focus();
    }

    function showToast(msg) {
        var t = document.getElementById('mobToast');
        if (!t) {
            t = document.createElement('div');
            t.id = 'mobToast';
            t.className = 'mob-toast';
            document.body.appendChild(t);
        }
        t.innerHTML = msg;
        void t.offsetWidth;            // reflow so the transition replays
        t.classList.add('show');
        clearTimeout(t._hide);
        t._hide = setTimeout(function () { t.classList.remove('show'); }, 2500);
    }

    var prm = Sys && Sys.WebForms && Sys.WebForms.PageRequestManager.getInstance();
    if (prm) {
        prm.add_endRequest(function () {
            focusScanBox();
            var matched = document.querySelector('.mob-linecard.matched');
            if (matched) matched.scrollIntoView({ behavior: 'smooth', block: 'center' });
        });
    }

    window.onload = function () {
        focusScanBox();
        if (!window.matchMedia('(display-mode: standalone)').matches) {
            document.body.style.height = (window.screen.height + 50) + 'px';
            setTimeout(function () { window.scrollTo(0, 1); }, 50);
            setTimeout(function () { document.body.style.height = ''; }, 600);
        }
    };
</script>
    </form>
</body>
</html>
