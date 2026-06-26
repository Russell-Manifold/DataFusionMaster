<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="QuickMoveM.aspx.cs"
         Inherits="SBMS.QuickMoveM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Quick Move</title>
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
        /* Step indicator */
        .qm-steps { display: flex; gap: .4em; padding: .7em .6em .2em; margin-top: 3.2em; }
        .qm-step {
            flex: 1; display: flex; flex-direction: column; align-items: center; gap: .15em;
            font-size: .68em; font-weight: 600; color: var(--mob-text-faint);
            text-align: center; line-height: 1.1;
        }
        .qm-step b {
            display: flex; align-items: center; justify-content: center;
            width: 1.9em; height: 1.9em; border-radius: 50%;
            background: #e2e6eb; color: var(--mob-text-muted); font-size: 1.05em;
        }
        .qm-step.active { color: var(--mob-brand); }
        .qm-step.active b { background: var(--mob-brand); color: #fff; }
        .qm-step.done   { color: var(--mob-dest); }
        .qm-step.done b { background: var(--mob-dest); color: #fff; }

        /* Prompt */
        .qm-prompt { padding: .4em 1em .2em; font-size: .92em; font-weight: 600; color: var(--mob-ink); }

        /* Out-detail / summary cards */
        .qm-card {
            background: var(--mob-surface); border: 0.5px solid var(--mob-border);
            border-radius: var(--mob-radius-lg); box-shadow: var(--mob-shadow-card);
            margin: .5em .6em; padding: .9em 1em;
        }
        .qm-card .qm-code  { font-weight: 700; font-size: 1.05em; color: var(--mob-ink-strong); }
        .qm-card .qm-unit  { font-size: .74em; color: #999; margin-top: .08em; }
        .qm-card .qm-descr { font-size: .84em; color: var(--mob-text-muted); margin-top: .25em; line-height: 1.35; }
        .qm-avail { margin-top: .55em; }

        /* From → To summary */
        .qm-route { display: flex; align-items: center; gap: .6em; font-size: 1.05em; font-weight: 700; color: var(--mob-ink); }
        .qm-route .qm-arrow { color: var(--mob-brand); }
        .qm-route .qm-to-pending { color: var(--mob-text-faint); font-weight: 600; }
        .qm-route .qm-to-set { color: var(--mob-dest); }
        .qm-sub { font-size: .85em; color: var(--mob-text-muted); margin-top: .45em; }
        .qm-sub strong { color: var(--mob-ink-strong); }

        /* Moved this session */
        .mob-done-list { margin: 1em .6em 6em; border-top: 1px solid var(--mob-divider); padding-top: .7em; }
        .mob-done-head { font-weight: 700; color: var(--mob-dest); font-size: .95em; margin-bottom: .4em; }
        .mob-done-row  { display: flex; align-items: baseline; gap: .5em; padding: .35em 0;
                         border-bottom: 1px solid #f0f0f0; font-size: .9em; }
        .mob-done-qty   { font-weight: 700; min-width: 2.6em; text-align: right; color: var(--mob-ink-strong); }
        .mob-done-item  { font-weight: 600; color: var(--mob-ink-strong); }
        .mob-done-route { color: var(--mob-text-muted); }
        .mob-done-route strong { color: var(--mob-dest); }
        .mob-done-time  { margin-left: auto; color: var(--mob-text-faint); font-size: .85em; }

        /* Success toast */
        .mob-toast { position: fixed; left: 50%; bottom: 5.2em;
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
            <asp:LinkButton ID="lbtnTopHome" runat="server" OnClick="lbtnTopHome_Click" CssClass="mob-topbar-icon" title="Home">&#127968;</asp:LinkButton>
        </div>
        <span class="mob-topbar-title">&#128257; Quick Move</span>
        <div class="mob-topbar-right">
            <asp:Label ID="lblUsername" runat="server" style="display:none;" />
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click"
                CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>

        <%-- Step indicator (rendered from code) --%>
        <asp:Literal ID="lblSteps" runat="server" />

        <%-- Step prompt --%>
        <div class="qm-prompt"><asp:Label ID="lblPrompt" runat="server" /></div>

        <%-- Barcode / location scan bar --%>
        <div class="mob-scan-bar">
            <div class="mob-scan-wrap">
                <asp:TextBox ID="txtScan" runat="server" placeholder="Scan&hellip;"
                    AutoPostBack="true" OnTextChanged="txtScan_TextChanged"
                    autocomplete="off" autocorrect="off" autocapitalize="off"
                    style="font-size:16px;" />
                <asp:LinkButton ID="lbtnClearScan" runat="server" OnClick="lbtnClearScan_Click"
                    CssClass="mob-scan-clear" title="Clear">&#10005;</asp:LinkButton>
            </div>
        </div>

        <%-- Scan feedback --%>
        <asp:Label ID="lblFeedback" runat="server" CssClass="mob-feedback" />

        <%-- Step 2 detail: item + lot + qty to move out --%>
        <asp:Panel ID="pnlOutDetail" runat="server" Visible="false" CssClass="qm-card">
            <div class="qm-code"><asp:Label ID="lblOutItem" runat="server" /></div>
            <div class="qm-unit"><asp:Label ID="lblOutUnit" runat="server" /></div>
            <div class="qm-descr"><asp:Label ID="lblOutDescr" runat="server" /></div>

            <asp:DropDownList ID="ddLot" runat="server" CssClass="mob-lot-select"
                AutoPostBack="true" OnSelectedIndexChanged="ddLot_SelectedIndexChanged" Visible="false" />

            <div class="mob-qty-input-row qm-avail">
                <span class="mob-qty-label">Qty</span>
                <asp:TextBox ID="txtQty" runat="server" TextMode="Number" />
                <span class="mob-qty-label" style="margin-left:.4em;">of <asp:Label ID="lblAvail" runat="server" /> on hand</span>
            </div>

            <asp:LinkButton ID="lbtnConfirmOut" runat="server" OnClick="lbtnConfirmOut_Click"
                CssClass="mob-btn-secondary" style="display:block;height:3.1em;line-height:3.1em;
                text-align:center;border-radius:var(--mob-radius-md);font-weight:700;margin-top:.8em;">
                Confirm out &#8594;</asp:LinkButton>
        </asp:Panel>

        <%-- Step 3/4 summary: from → to + item/qty --%>
        <asp:Panel ID="pnlSummary" runat="server" Visible="false" CssClass="qm-card">
            <div class="qm-route">
                <span><asp:Label ID="lblSumFrom" runat="server" /></span>
                <span class="qm-arrow">&#8594;</span>
                <span class="qm-to-set"><asp:Label ID="lblSumDest" runat="server" /></span>
            </div>
            <div class="qm-sub">
                Moving <strong><asp:Label ID="lblSumQty" runat="server" /></strong>
                of <strong><asp:Label ID="lblSumItem" runat="server" /></strong>
            </div>
        </asp:Panel>

        <%-- Finish (step 4, enabled once the item-in scan matches) --%>
        <div style="padding:0 .6em;">
            <asp:LinkButton ID="lbtnFinish" runat="server" OnClick="lbtnFinish_Click" Visible="false"
                CssClass="mob-btn-primary" style="display:block;height:3.3em;line-height:3.3em;
                text-align:center;border-radius:var(--mob-radius-md);font-weight:700;margin:.2em 0;">
                &#10004; Finish &amp; post move</asp:LinkButton>
        </div>

        <%-- Start over --%>
        <div style="text-align:center;padding:.2em;">
            <asp:LinkButton ID="lbtnRestart" runat="server" OnClick="lbtnRestart_Click" Visible="false"
                CssClass="mob-selection-change-link">Start over</asp:LinkButton>
        </div>

        <%-- Moved this session --%>
        <asp:Panel ID="pnlDone" runat="server" Visible="false" CssClass="mob-done-list">
            <div class="mob-done-head">&#10003; Moved this session (<asp:Label ID="lblDoneCount" runat="server" />)</div>
            <asp:Repeater ID="rptDone" runat="server">
                <ItemTemplate>
                    <div class="mob-done-row">
                        <span class="mob-done-qty"><%# Eval("Qty", "{0:0.##}") %></span>
                        <span class="mob-done-item"><%# Eval("ItemCode") %></span>
                        <span class="mob-done-route"><%# Eval("From") %> &#8594; <strong><%# Eval("Dest") %></strong></span>
                        <span class="mob-done-time"><%# Eval("TimeText") %></span>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </asp:Panel>

    </ContentTemplate>
    </asp:UpdatePanel>

<script>
    function focusScanBox() {
        var b = document.getElementById('<%= txtScan.ClientID %>');
        if (b) { b.focus(); }
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
        prm.add_endRequest(function () { focusScanBox(); });
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
