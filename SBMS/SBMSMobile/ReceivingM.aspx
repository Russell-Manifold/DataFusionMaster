<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ReceivingM.aspx.cs"
         Inherits="SBMS.ReceivingM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Receiving</title>
    <link rel="shortcut icon" href="../images/datafusionicon.ico" type="image/x-icon" />
    <link rel="stylesheet" href="../SBMSMobile/css/main.css" />
    <link rel="stylesheet" href="../SBMSMobile/css/mobile-ui.css" />
    <link rel="manifest" href="../SBMSMobile/manifest.json" />
    <meta name="theme-color" content="#4282C1" />
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
    <meta name="apple-mobile-web-app-title" content="Data Fusion" />
    <link rel="apple-touch-icon" href="../SBMSMobile/icons/icon-192.png" />
    <meta name="format-detection" content="telephone=no" />
    <style>
        /* ── Page-specific overrides (not shared) ── */

        /* Store indicator uses green to signal "receiving destination confirmed" */
        .mob-doc-selection-strip { color: #2d6a2d; }
        .mob-doc-selection-strip strong { color: #1e4d1e; }
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
        <span class="mob-topbar-title">&#128230; Receiving</span>
        <div class="mob-topbar-right">
            <asp:Label ID="lblUsername" runat="server" style="display:none;" />
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click"
                CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>

        <%-- Store selection modal --%>
        <asp:Panel ID="pnlStoreModal" runat="server" Visible="false" CssClass="mob-modal-overlay">
            <div class="mob-modal-box">
                <span class="mob-modal-icon">&#127968;</span>
                <h3>Select Receiving Store</h3>
                <p>Choose the store for this delivery. All items will be received into the selected store.</p>
                <asp:DropDownList ID="ddStoreGlobal" runat="server" CssClass="mob-modal-select" Font-Size="Smaller" />
                <asp:Label ID="lblStoreModalError" runat="server" CssClass="mob-modal-error" Visible="false" />
                <asp:LinkButton ID="lbtnConfirmStore" runat="server" OnClick="lbtnConfirmStore_Click"
                    CssClass="mob-modal-confirm">&#10003; Confirm Store</asp:LinkButton>
                <asp:LinkButton ID="lbtnModalCancel" runat="server" OnClick="lbtnModalCancel_Click"
                    CssClass="mob-modal-cancel">Wrong document << go back</asp:LinkButton>
            </div>
        </asp:Panel>

        <%-- Document summary header --%>
        <div class="mob-doc-header">
            <div class="mob-doc-header-main">
                <span class="mob-doc-num"><asp:Label ID="lblPONum" runat="server" /></span>
                <span class="mob-doc-secondary">&mdash; <asp:Label ID="lblSupplier" runat="server" /></span>
            </div>
            <div class="mob-doc-meta">
                <span class="mob-doc-meta-item">Due: <asp:Label ID="lblDueDate" runat="server" /></span>
                <span class="mob-doc-badge"><asp:Label ID="lblLineCount" runat="server" /> line(s) outstanding</span>
            </div>
            <asp:Panel ID="pnlStoreIndicator" runat="server" CssClass="mob-doc-selection-strip" Visible="false">
                &#10003; Receiving into: <strong><asp:Label ID="lblSelectedStoreName" runat="server" /></strong>
                &nbsp;&middot;&nbsp;
                <asp:LinkButton ID="lbtnChangeStore" runat="server" OnClick="lbtnChangeStore_Click"
                    CssClass="mob-selection-change-link">Change</asp:LinkButton>
            </asp:Panel>
        </div>

        <%-- Barcode scan bar --%>
        <div class="mob-scan-bar">
            <div class="mob-scan-wrap">
                <asp:TextBox ID="txtBarcode" runat="server" placeholder="&#128247; Scan or type barcode&hellip;"
                    AutoPostBack="true" OnTextChanged="txtBarcode_TextChanged"
                    autocomplete="off" autocorrect="off" autocapitalize="off"
                    style="font-size:16px;" />
                <asp:LinkButton ID="lbtnClearScan" runat="server" OnClick="lbtnClearScan_Click"
                    CssClass="mob-scan-clear" title="Clear">&#10005;</asp:LinkButton>
            </div>
        </div>

        <%-- Scan feedback --%>
        <asp:Label ID="lblScanFeedback" runat="server" />

        <%-- Finalise panel --%>
        <asp:Panel ID="pnlFinalize" runat="server" Visible="false" CssClass="mob-action-panel">
            <h4>&#x1F4CB; Finalise &amp; Generate GRN</h4>
            <div class="mob-panel-row">
                <label>D/N Number **</label>
                <asp:TextBox ID="txtDNNum" runat="server" placeholder="Delivery Note #"
                    autocomplete="off" autocorrect="off" autocapitalize="off" />
            </div>
            <div class="mob-panel-row">
                <label>Invoice Number **</label>
                <asp:TextBox ID="txtInvNum" runat="server" placeholder="Supplier Invoice #"
                    autocomplete="off" autocorrect="off" autocapitalize="off" />
            </div>
            <div class="mob-panel-row">
                <label>Receive Date</label>
                <asp:TextBox ID="txtRecDate" runat="server" TextMode="Date" />
            </div>
            <div class="mob-panel-check">
                <asp:CheckBox ID="chkReceivingComplete" runat="server" Checked="true" Text=" " />
                <span>Mark receiving as complete</span>
            </div>
            <asp:Label ID="lblFinalizeError" runat="server" CssClass="mob-panel-error" Visible="false" />
            <div class="mob-panel-buttons">
                <asp:LinkButton ID="lbtnCancelFinalize" runat="server"
                    OnClick="lbtnCancelFinalize_Click"
                    CssClass="mob-btn-cancel">Cancel</asp:LinkButton>
                <asp:LinkButton ID="lbtnConfirmGRN" runat="server"
                    OnClick="lbtnConfirmGRN_Click"
                    CssClass="mob-btn-confirm"
                    OnClientClick="return disableGRNButton();">&#10003; Confirm &amp; Generate GRN</asp:LinkButton>
            </div>
        </asp:Panel>

        <%-- PO line cards --%>
        <div class="mob-linelist">
            <asp:Repeater ID="rptLines" runat="server"
                OnItemDataBound="rptLines_ItemDataBound"
                OnItemCommand="rptLines_ItemCommand">
                <ItemTemplate>
                    <div class='mob-linecard' id="card_<%# Eval("LineID") %>">
                        <div class="mob-linecard-body">

                            <div class="mob-card-main">
                                <div class="mob-item-code">
                                    <%# Eval("ItemCode") %>
                                    <asp:Label ID="lblSavedBadge" runat="server"
                                        CssClass="mob-saved-badge" Visible="false">&#10003; Received</asp:Label>
                                </div>
                                <div class="mob-item-unit"><%# Eval("Unit") %></div>
                                <div class="mob-item-descr"><%# Eval("ItemDescription") %></div>

                                <div class="mob-qty-row">
                                    <span class="mob-qty-badge ordered">Ordered&nbsp;<%# Eval("Quantity", "{0:0.##}") %></span>
                                    <span class="mob-qty-badge remaining">Remaining&nbsp;<%# Eval("QtyLeft", "{0:0.##}") %></span>
                                    <asp:HiddenField ID="hfLineID" runat="server" Value='<%# Eval("LineID") %>' />
                                </div>

                                <div class="mob-qty-input-row">
                                    <span class="mob-qty-label">Qty</span>
                                    <asp:TextBox ID="txtRecQty" runat="server"
                                        TextMode="Number"
                                        Text='<%# Eval("QtyLeft", "{0:0.##}") %>'
                                        style="width:6em;height:2.7em;border:1px solid #ccc;border-radius:.45em;
                                               text-align:center;font-size:1.05em;font-weight:600;" />
                                </div>

                                <asp:Label ID="lblLotNum" runat="server" CssClass="mob-lot-label" />
                                <asp:HiddenField ID="hfLotNum" runat="server" />
                            </div>

                            <asp:LinkButton ID="lbtnSaveLine" runat="server"
                                CssClass="mob-action-btn"
                                CommandName="ReceiveLine"
                                CommandArgument='<%# Eval("LineID") %>'
                                OnClick="lbtnSaveLine_Click">&#10004;</asp:LinkButton>

                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <asp:Label ID="lblEmpty" runat="server" CssClass="mob-empty"
                Visible="false" Text="&#10003; All lines received!" />
        </div>

    </ContentTemplate>
    </asp:UpdatePanel>

    <%-- Bottom toolbar --%>
    <div class="mob-toolbar">
        <asp:LinkButton ID="lbtnBack"   runat="server" OnClick="lbtnBack_Click"   style="display:none;" />
        <asp:LinkButton ID="lbtnRecAll" runat="server" OnClick="lbtnRecAll_Click" style="display:none;" />
        <asp:LinkButton ID="lbtnFinalize" runat="server" OnClick="lbtnFinalize_Click"
            CssClass="mob-btn-primary">&#x2B06; Finalise GRN</asp:LinkButton>
    </div>
</form>

<script>
    function focusScanBox() {
        var b = document.getElementById('<%= txtBarcode.ClientID %>');
        if (b) b.focus();
    }

    function markReceivedCards() {
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
            markReceivedCards();
            var matched = document.querySelector('.mob-linecard.matched');
            if (matched) matched.scrollIntoView({ behavior: 'smooth', block: 'center' });
        });
    }

    window.onload = function () {
        var modal = document.querySelector('.mob-modal-overlay');
        if (!modal || modal.style.display === 'none') focusScanBox();
        markReceivedCards();
        if (!window.matchMedia('(display-mode: standalone)').matches) {
            document.body.style.height = (window.screen.height + 50) + 'px';
            setTimeout(function () { window.scrollTo(0, 1); }, 50);
            setTimeout(function () { document.body.style.height = ''; }, 600);
        }
    };

    function disableGRNButton() {
        var btn = document.getElementById('<%= lbtnConfirmGRN.ClientID %>');
        if (btn) { btn.disabled = true; btn.innerText = 'Processing…'; }
        return true;
    }
</script>
</body>
</html>
