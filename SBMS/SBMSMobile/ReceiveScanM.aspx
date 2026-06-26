<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ReceiveScanM.aspx.cs"
         Inherits="SBMS.ReceiveScanM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Receive</title>
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
    <%-- matched-line treatment now lives in mobile-ui.css --%>
</head>
<body>
<form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" />

    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="upMain">
        <ProgressTemplate>
            <div style="position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:999;
                        display:flex;align-items:center;justify-content:center;color:#fff;font-size:1.1em;">
                Loading&hellip;
            </div>
        </ProgressTemplate>
    </asp:UpdateProgress>

    <div class="mob-topbar">
        <div class="mob-topbar-left">
            <asp:LinkButton ID="lbtnTopBack" runat="server" OnClick="lbtnTopBack_Click" CssClass="mob-topbar-back">&#8592; Back</asp:LinkButton>
            <asp:LinkButton ID="lbtnTopHome" runat="server" OnClick="lbtnTopHome_Click" CssClass="mob-topbar-icon" title="Home">&#127968;</asp:LinkButton>
        </div>
        <span class="mob-topbar-title">&#128229; Receive</span>
        <div class="mob-topbar-right">
            <asp:Label ID="lblUsername" runat="server" style="display:none;" />
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click"
                CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>

        <%-- PO summary header --%>
        <div class="mob-doc-header">
            <div class="mob-doc-header-main">
                <span class="mob-doc-num"><asp:Label ID="lblPONum" runat="server" /></span>
                <span class="mob-doc-secondary">&mdash; <asp:Label ID="lblSupplier" runat="server" /></span>
            </div>
            <div class="mob-doc-meta">
                <span class="mob-doc-meta-item">Due: <asp:Label ID="lblDueDate" runat="server" /></span>
                <span class="mob-doc-badge"><asp:Label ID="lblLineCount" runat="server" /> line(s) outstanding</span>
            </div>
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
                <asp:LinkButton ID="lbtnCancelFinalize" runat="server" OnClick="lbtnCancelFinalize_Click"
                    CssClass="mob-btn-cancel">Cancel</asp:LinkButton>
                <asp:LinkButton ID="lbtnConfirmGRN" runat="server" OnClick="lbtnConfirmGRN_Click"
                    CssClass="mob-btn-confirm" OnClientClick="return disableGRNButton();">&#10003; Confirm &amp; Generate GRN</asp:LinkButton>
            </div>
        </asp:Panel>

        <%-- PO line cards --%>
        <div class="mob-linelist">
            <asp:Repeater ID="rptLines" runat="server"
                OnItemDataBound="rptLines_ItemDataBound"
                OnItemCommand="rptLines_ItemCommand">
                <ItemTemplate>
                    <asp:Panel ID="pnlCard" runat="server" CssClass="mob-linecard">
                        <div class="mob-linecard-body">
                            <div class="mob-card-main">
                                <div class="mob-item-code">
                                    <%# Eval("ItemCode") %>
                                    <asp:Label ID="lblSavedBadge" runat="server" CssClass="mob-saved-badge" Visible="false">&#10003; Captured</asp:Label>
                                </div>
                                <div class="mob-item-unit"><%# Eval("Unit") %></div>
                                <div class="mob-item-descr"><%# Eval("ItemDescription") %></div>

                                <div class="mob-qty-row">
                                    <span class="mob-qty-badge ordered">Ordered&nbsp;<%# Eval("Quantity", "{0:0.##}") %></span>
                                    <span class="mob-qty-badge remaining">Remaining&nbsp;<asp:Label ID="lblRemaining" runat="server" /></span>
                                    <asp:HiddenField ID="hfLineID" runat="server" Value='<%# Eval("LineID") %>' />
                                </div>

                                <div class="mob-qty-input-row">
                                    <span class="mob-qty-label">Qty</span>
                                    <asp:TextBox ID="txtRecQty" runat="server" TextMode="Number"
                                        style="width:4.5em;height:2.7em;border:1px solid #ccc;border-radius:.45em;
                                               text-align:center;font-size:1.05em;font-weight:600;" />
                                    <span class="mob-qty-label" style="margin-left:.6em;">Loc</span>
                                    <asp:TextBox ID="txtLoc" runat="server" placeholder="Scan location"
                                        autocomplete="off" autocorrect="off" autocapitalize="off"
                                        style="flex:1;min-width:0;height:2.7em;border:1px solid #ccc;border-radius:.45em;
                                               text-align:left;padding:0 .55em;font-size:1.05em;" />
                                </div>

                                <asp:Label ID="lblLotNum" runat="server" CssClass="mob-lot-label" />
                            </div>

                            <asp:LinkButton ID="lbtnSaveLine" runat="server" CssClass="mob-action-btn"
                                OnClick="lbtnSaveLine_Click">&#10004;</asp:LinkButton>
                        </div>
                    </asp:Panel>
                </ItemTemplate>
            </asp:Repeater>

            <asp:Label ID="lblEmpty" runat="server" CssClass="mob-empty"
                Visible="false" Text="&#10003; All lines received!" />
        </div>

    </ContentTemplate>
    </asp:UpdatePanel>

    <div class="mob-toolbar">
        <asp:LinkButton ID="lbtnFinalize" runat="server" OnClick="lbtnFinalize_Click"
            CssClass="mob-btn-primary">&#x2B06; Finalise GRN</asp:LinkButton>
    </div>
</form>

<script>
    function focusScanBox() {
        var b = document.getElementById('<%= txtBarcode.ClientID %>');
        if (b) b.focus();
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
    function disableGRNButton() {
        var btn = document.getElementById('<%= lbtnConfirmGRN.ClientID %>');
        if (btn) { btn.disabled = true; btn.innerText = 'Processing…'; }
        return true;
    }
</script>
</body>
</html>
