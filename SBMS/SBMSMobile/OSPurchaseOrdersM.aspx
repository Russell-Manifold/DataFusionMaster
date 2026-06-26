<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="OSPurchaseOrdersM.aspx.cs" Inherits="SBMS.OSPurchaseOrdersM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Purchase Orders</title>
    <link rel="shortcut icon" href="../images/datafusionicon.ico" type="image/x-icon" />
    <link rel="stylesheet" href="css/main.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/main.css") %>" />
    <link rel="stylesheet" href="css/mobile-ui.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/mobile-ui.css") %>" />
    <!-- PWA -->
    <link rel="manifest" href="manifest.json" />
    <meta name="theme-color" content="#4282C1" />
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
    <meta name="apple-mobile-web-app-title" content="Data Fusion" />
    <link rel="apple-touch-icon" href="icons/icon-192.png" />
    <meta name="format-detection" content="telephone=no" />
</head>
<body>
<form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" />

    <%-- Loading overlay --%>
    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="upMain">
        <ProgressTemplate>
            <div style="position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:999;display:flex;align-items:center;justify-content:center;color:#fff;font-size:1.1em;">
                Loading&hellip;
            </div>
        </ProgressTemplate>
    </asp:UpdateProgress>

    <%-- Top bar --%>
    <div class="mob-topbar">
        <div class="mob-topbar-left">
            <asp:LinkButton ID="lbtnBack" runat="server" OnClick="lbtnBack_Click" CssClass="mob-topbar-back">&#8592; Back</asp:LinkButton>
            <asp:LinkButton ID="lbtnHome" runat="server" OnClick="lbtnHome_Click" CssClass="mob-topbar-icon" title="Home">&#127968;</asp:LinkButton>
        </div>
        <span class="mob-topbar-title">&#128230; Purchase Orders</span>
        <div class="mob-topbar-right">
            <asp:Label ID="lblUsername" runat="server" CssClass="mob-topbar-user" style="display:none;" />
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click" CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>

        <%-- Filter bar --%>
        <asp:Panel ID="pnlFilter" runat="server" DefaultButton="lbtnFind" CssClass="mob-filterbar">
            <asp:TextBox ID="txtfind" runat="server" placeholder="Supplier / PO #" />
            <asp:LinkButton ID="lbtnFind" runat="server" OnClick="lbtnFind_Click" CssClass="mob-find-btn">&#128269;</asp:LinkButton>
            <asp:DropDownList ID="ddStatus" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddStatus_SelectedIndexChanged" />
            <asp:LinkButton ID="lbtnSort" runat="server" OnClick="lbtnSort_Click" CssClass="mob-sort-btn" title="Sort by document number">&#8593;</asp:LinkButton>
        </asp:Panel>

        <%-- Card list --%>
        <div class="mob-cardlist">
            <asp:Repeater ID="rptPOs" runat="server" OnItemDataBound="rptPOs_ItemDataBound">
                <ItemTemplate>
                    <div class="mob-card">
                        <asp:LinkButton ID="lbtnPO" runat="server" CssClass="mob-card-btn"
                                        CommandArgument='<%# Eval("DocGUID") %>'
                                        OnClick="lbtnPO_Click">
                            <div class="mob-card-row">
                                <span class="mob-po-num"><%# Eval("DocumentNumber") %></span>
                                <span class="mob-badge" id="spnBadge" runat="server"></span>
                            </div>
                            <div class="mob-supplier"><%# Eval("CustSupName") %></div>
                            <div class="mob-meta">
                                <span>Due: <%# Eval("DueDelDate", "{0:dd MMM yyyy}") %></span>
                                <span class="mob-started-flag"><%# Convert.ToBoolean(Eval("Started")) ? "&#9679; Started" : "" %></span>
                            </div>
                        </asp:LinkButton>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <asp:Label ID="lblEmpty" runat="server" CssClass="mob-empty"
                Visible="false" Text="No purchase orders found." />
        </div>

    </ContentTemplate>
    </asp:UpdatePanel>
</form>
<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
<script>
    // Hide URL bar on mobile (best-effort; full hiding requires installing as PWA)
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
