<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PickingSlipM.aspx.cs" Inherits="SBMS.PickingSlipM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Picking Slip</title>
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
        /* Pick message banner */
        .mob-pick-msg {
            background: var(--mob-brand-dark);
            color: #fff;
            font-size: .8em;
            padding: .38em 1em;
            border-top: 1px solid rgba(255,255,255,.15);
        }
        /* Lot dropdown sits directly under the qty row */
        .mob-lot-select {
            width: 100%;
            height: 2.4em;
            border: 1px solid #ccc;
            border-radius: var(--mob-radius-sm);
            padding: 0 .4em;
            font-size: .9em;
            background: var(--mob-surface);
            margin-top: .45em;
            transition: border-color .15s, box-shadow .15s;
        }
        .mob-lot-select:focus { outline: none; border-color: var(--mob-brand); box-shadow: var(--mob-focus); }
    </style>
</head>
<body>
<form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" />
    <%-- Icon library (outline, 2px, rounded) — referenced via <use href="#i-…"> --%>
    <svg class="mob-ico-defs" aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg">
        <symbol id="i-house" viewBox="0 0 24 24"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><path d="M9 22V12h6v10"/></symbol>
        <symbol id="i-clipboard" viewBox="0 0 24 24"><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/><rect x="8" y="2" width="8" height="4" rx="1"/><path d="M9 12l2 2 4-4"/></symbol>
        <symbol id="i-package" viewBox="0 0 24 24"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><path d="M3.3 7 12 12l8.7-5"/><path d="M12 22V12"/></symbol>
        <symbol id="i-note" viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><path d="M14 2v6h6"/><path d="M16 13H8"/><path d="M16 17H8"/><path d="M10 9H8"/></symbol>
        <symbol id="i-scan" viewBox="0 0 24 24"><path d="M3 7V5a2 2 0 0 1 2-2h2"/><path d="M17 3h2a2 2 0 0 1 2 2v2"/><path d="M21 17v2a2 2 0 0 1-2 2h-2"/><path d="M7 21H5a2 2 0 0 1-2-2v-2"/><path d="M7 12h10"/></symbol>
        <symbol id="i-arrow-up" viewBox="0 0 24 24"><path d="M12 19V5"/><path d="M5 12l7-7 7 7"/></symbol>
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
        <span class="mob-topbar-title"><svg class="mob-ico" aria-hidden="true"><use href="#i-clipboard"/></svg>Picking Slip</span>
        <div class="mob-topbar-right">
            <asp:Label ID="lblUsername" runat="server" style="display:none;" />
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click" CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>

        <%-- Store selection modal --%>
        <asp:Panel ID="pnlStoreModal" runat="server" Visible="false" CssClass="mob-modal-overlay">
            <div class="mob-modal-box">
                <span class="mob-modal-icon"><svg class="mob-ico" aria-hidden="true"><use href="#i-package"/></svg></span>
                <h3>Select Picking Store</h3>
                <p>Choose the store to pick from. All items will be picked from the selected store.</p>
                <asp:DropDownList ID="ddStoreGlobal" runat="server" CssClass="mob-modal-select" />
                <asp:Label ID="lblStoreModalError" runat="server" CssClass="mob-modal-error" Visible="false" />
                <asp:LinkButton ID="lbtnConfirmStore" runat="server" OnClick="lbtnConfirmStore_Click"
                    CssClass="mob-modal-confirm">&#10003; Confirm Store</asp:LinkButton>
                <asp:LinkButton ID="lbtnModalCancel" runat="server" OnClick="lbtnModalCancel_Click"
                    CssClass="mob-modal-cancel">Wrong document << go back</asp:LinkButton>
            </div>
        </asp:Panel>

        <%-- Document header --%>
        <div class="mob-doc-header">
            <div class="mob-doc-header-main">
                <span class="mob-doc-num"><asp:Label ID="lblPSNum" runat="server" /></span>
                <span class="mob-doc-secondary">&mdash; <asp:Label ID="lblCustomer" runat="server" /></span>
            </div>
            <div class="mob-doc-meta">
                <span class="mob-doc-meta-item">SO: <asp:Label ID="lblSONum" runat="server" /></span>
                <span class="mob-doc-meta-item">Due: <asp:Label ID="lblDueDate" runat="server" /></span>
                <span class="mob-doc-badge"><asp:Label ID="lblLineCount" runat="server" /> line(s) remaining</span>
            </div>
            <asp:Panel ID="pnlStoreIndicator" runat="server" CssClass="mob-doc-selection-strip" Visible="false">
                &#10003; Picking from: <strong><asp:Label ID="lblSelectedStoreName" runat="server" /></strong>
                &nbsp;&middot;&nbsp;
                <asp:LinkButton ID="lbtnChangeStore" runat="server" OnClick="lbtnChangeStore_Click"
                    CssClass="mob-selection-change-link">Change</asp:LinkButton>
            </asp:Panel>
        </div>

        <%-- Pick message --%>
        <asp:Panel ID="pnlPickMsg" runat="server" Visible="false" CssClass="mob-pick-msg">
            <svg class="mob-ico" aria-hidden="true"><use href="#i-note"/></svg> <asp:Label ID="lblPickMsg" runat="server" />
        </asp:Panel>

        <%-- Barcode scan bar (hidden when the company is not barcode-based) --%>
        <asp:Panel ID="pnlScanBar" runat="server">
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
        </asp:Panel>

        <%-- Scan feedback --%>
        <asp:Label ID="lblScanFeedback" runat="server" />

        <%-- Finalise panel --%>
        <asp:Panel ID="pnlFinalise" runat="server" Visible="false" CssClass="mob-action-panel">
            <h4><svg class="mob-ico" aria-hidden="true"><use href="#i-clipboard"/></svg> Close Off Picking Slip</h4>
            <p style="font-size:.82em;color:#555;margin:0 0 .7em 0;line-height:1.4;">
                All lines are marked as picked. Confirm to close off this slip and update stock.
            </p>
            <asp:Label ID="lblFinaliseError" runat="server" CssClass="mob-panel-error" Visible="false" />
            <div class="mob-panel-buttons">
                <asp:LinkButton ID="lbtnCancelFinalise" runat="server"
                    OnClick="lbtnCancelFinalise_Click"
                    CssClass="mob-btn-cancel">Cancel</asp:LinkButton>
                <asp:LinkButton ID="lbtnConfirmFinalise" runat="server"
                    OnClick="lbtnConfirmFinalise_Click"
                    CssClass="mob-btn-confirm"
                    OnClientClick="return confirmCloseOff();">&#10003; Confirm Close Off</asp:LinkButton>
            </div>
        </asp:Panel>

        <%-- Line cards --%>
        <div class="mob-linelist">
            <asp:Repeater ID="rptLines" runat="server"
                OnItemDataBound="rptLines_ItemDataBound"
                OnItemCommand="rptLines_ItemCommand">
                <ItemTemplate>
                    <div class='mob-linecard<%# (int)Eval("LineID") == MatchedLineID ? " matched" : (Eval("PickComplete") as bool? == true ? " received" : "") %>'
                         id="card_<%# Eval("LineID") %>">
                        <div class="mob-linecard-body">

                            <div class="mob-card-main">
                                <div class="mob-item-code">
                                    <%# Eval("ItemCode") %>
                                    <asp:Label ID="lblSavedBadge" runat="server"
                                        CssClass="mob-saved-badge" Visible="false">&#10003; Picked</asp:Label>
                                </div>
                                <div class="mob-item-unit"><%# Eval("Unit") %></div>
                                <div class="mob-item-descr"><%# Eval("ItemDescription") %></div>

                                <div class="mob-qty-row">
                                    <span class="mob-qty-badge ordered">To Pick&nbsp;<%# Eval("Quantity", "{0:0.##}") %></span>
                                    <asp:HiddenField ID="hfLineID" runat="server" Value='<%# Eval("LineID") %>' />
                                </div>

                                <div class="mob-qty-input-row">
                                    <span class="mob-qty-label">Qty</span>
                                    <asp:TextBox ID="txtPickQty" runat="server"
                                        TextMode="Number"
                                        Text='<%# Eval("PickQty", "{0:0.##}") %>'
                                        style="width:6em;height:2.7em;border:1px solid #ccc;border-radius:.45em;
                                               text-align:center;font-size:1.05em;font-weight:600;" />
                                </div>

                                <asp:DropDownList ID="ddLotNum" runat="server" CssClass="mob-lot-select" />
                            </div>

                            <asp:LinkButton ID="lbtnPickLine" runat="server"
                                CssClass="mob-action-btn"
                                CommandName="PickLine"
                                CommandArgument='<%# Eval("LineID") %>'
                                OnClick="lbtnPickLine_Click">&#10004;</asp:LinkButton>

                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <asp:Label ID="lblEmpty" runat="server" CssClass="mob-empty"
                Visible="false" Text="&#10003; All lines picked!" />
        </div>

    </ContentTemplate>
    </asp:UpdatePanel>

    <%-- Bottom toolbar --%>
    <div class="mob-toolbar">
        <asp:LinkButton ID="lbtnBack"    runat="server" OnClick="lbtnBack_Click"    style="display:none;" />
        <asp:LinkButton ID="lbtnPickAll" runat="server" OnClick="lbtnPickAll_Click" style="display:none;" />
        <asp:LinkButton ID="lbtnFinalise" runat="server" OnClick="lbtnFinalise_Click"
            CssClass="mob-btn-primary"><svg class="mob-ico" aria-hidden="true"><use href="#i-arrow-up"/></svg> Close Off</asp:LinkButton>
    </div>
</form>

<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
<script>
    function focusScanBox() {
        var b = document.getElementById('<%= txtBarcode.ClientID %>');
        if (b) b.focus();
    }

    function markPickedCards() {
        document.querySelectorAll('.mob-linecard').forEach(function (card) {
            var badge = card.querySelector('.mob-saved-badge');
            if (badge && badge.offsetParent !== null) card.classList.add('received');
        });
    }

    var prm = Sys && Sys.WebForms && Sys.WebForms.PageRequestManager.getInstance();
    if (prm) {
        prm.add_endRequest(function () {
            var modal = document.querySelector('.mob-modal-overlay');
            if (!modal || modal.style.display === 'none') focusScanBox();
            markPickedCards();
            var matched = document.querySelector('.mob-linecard.matched');
            if (matched) matched.scrollIntoView({ behavior: 'smooth', block: 'center' });
        });
    }

    window.onload = function () {
        var modal = document.querySelector('.mob-modal-overlay');
        if (!modal || modal.style.display === 'none') focusScanBox();
        markPickedCards();
        if (!window.matchMedia('(display-mode: standalone)').matches) {
            document.body.style.height = (window.screen.height + 50) + 'px';
            setTimeout(function () { window.scrollTo(0, 1); }, 50);
            setTimeout(function () { document.body.style.height = ''; }, 600);
        }
    };

    function confirmCloseOff() {
        var btn = document.getElementById('<%= lbtnConfirmFinalise.ClientID %>');
        if (btn) { btn.disabled = true; btn.innerText = 'Processing…'; }
        return true;
    }
</script>
</body>
</html>
