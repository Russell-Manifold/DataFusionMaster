<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="StockControl.aspx.cs" Inherits="SBMS.StockControl" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Stock Control</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <!-- CSS styles -->
    <%--<link rel="stylesheet" href="assets/css/main.css" />--%>
    <link rel="stylesheet" href="prologue/assets/css/main.css" />
    <%-- bump ?v= whenever df-theme.css / df-ui.js change, so browsers don't serve a stale copy --%>
    <link rel="stylesheet" href="assets/css/df-theme.css?v=13" />
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js"></script>
    <script type="text/javascript" src="js/pickslipkanbanscript.js"></script>
    <script src="assets/js/df-ui.js?v=13"></script>
</head>
<body class="df-page df-mod-stock">
    <form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true"></asp:ScriptManager>

        <div class="df-shell" id="dfShell">
            <aside class="df-sidebar" id="dfSidebar">
                <div class="df-sidebar-brand">
                    <a href="https://mydatafusion.online" title="My Data Fusion website" target="_blank">
                        <img src="images/Logo.png" class="df-brand-logo" alt="Data Fusion" />
                    </a>
                </div>

                <nav class="df-nav">
                    <div class="df-nav-group">
                        <h5 class="df-nav-heading">Stores, Receiving &amp; Picking</h5>
                        <asp:LinkButton ID="imgdash" runat="server" CssClass="df-nav-link" OnClick="imgdash_Click" ToolTip="Return to main dashboard"><i class="icon solid fa-home df-nav-icon-font"></i><span class="df-nav-text">Dashboard</span></asp:LinkButton>
                        <asp:LinkButton ID="imgbRec" runat="server" CssClass="df-nav-link" OnClick="imgbRec_Click" ToolTip="Receive from Purchase Orders, allocate lot numbers"><i class="icon solid fa-file-invoice df-nav-icon-font"></i><span class="df-nav-text">Purchase Orders</span></asp:LinkButton>
                        <asp:LinkButton ID="ibtnPickSlips" runat="server" CssClass="df-nav-link" OnClick="ibtnPickSlips_Click" ToolTip="View Sales Orders and picking slips, fulfill orders"><i class="icon solid fa-shopping-cart df-nav-icon-font"></i><span class="df-nav-text">Sales Orders</span></asp:LinkButton>
                        <asp:LinkButton ID="ibtnPickTrack" runat="server" CssClass="df-nav-link" OnClick="ibtnPickTrack_Click" ToolTip="Track all picking slips in a simple drag and drop process "><i class="icon solid fa-tasks df-nav-icon-font"></i><span class="df-nav-text">Picking Slip Tracking</span></asp:LinkButton>
                        <asp:LinkButton ID="ibtnStckCtl" runat="server" CssClass="df-nav-link df-nav-link-active" aria-current="page" ToolTip="Inter Store Transfers, Stock adjustments, Stock Takes, Reporting" OnClick="ibtnStckCtl_Click"><i class="icon solid fa-cubes df-nav-icon-font"></i><span class="df-nav-text">Stock Control</span></asp:LinkButton>
                    </div>

                    <div class="df-nav-group">
                        <h5 class="df-nav-heading">Planning, Job Cards &amp; Kits</h5>
                        <asp:LinkButton ID="ibtnFCasts" runat="server" CssClass="df-nav-link" OnClick="ibtnFCasts_Click" ToolTip="Ensure greater stock accuracy with sales forecasting"><i class="icon solid fa-chart-line df-nav-icon-font"></i><span class="df-nav-text">Sales Forecasts</span></asp:LinkButton>
                        <asp:LinkButton ID="ibtnmrp" runat="server" CssClass="df-nav-link" OnClick="ibtnmrp_Click" ToolTip="View finished good demands based on PO's, Sales Orders, Forecasts etc"><i class="icon solid fa-clipboard-list df-nav-icon-font"></i><span class="df-nav-text">Finished Goods Demands</span></asp:LinkButton>
                        <asp:LinkButton ID="ibtnJobTrack" runat="server" CssClass="df-nav-link" OnClick="ibtnJobTrack_Click" ToolTip="Track all Job cards, simply drag and drop their change in status"><i class="icon solid fa-clipboard-check df-nav-icon-font"></i><span class="df-nav-text">Job Card Tracking</span></asp:LinkButton>
                    </div>

                    <div class="df-nav-group">
                        <h5 class="df-nav-heading">Manufacturing, BOMs &amp; MRP</h5>
                        <asp:LinkButton ID="ibtmWorksOrders" runat="server" CssClass="df-nav-link" OnClick="ibtmWorksOrders_Click" ToolTip="Create and view works orders for manufacturing or production"><i class="icon solid fa-industry df-nav-icon-font"></i><span class="df-nav-text">Works Orders</span></asp:LinkButton>
                        <asp:LinkButton ID="ibtmWOrdMgment" runat="server" CssClass="df-nav-link" OnClick="ibtmWOrdMgment_Click" ToolTip="Allocate raw materials to works orders and update stock levels on order completion."><i class="icon solid fa-cogs df-nav-icon-font"></i><span class="df-nav-text">Works Order Fulfillment</span></asp:LinkButton>
                        <asp:LinkButton ID="ibtnRMD" runat="server" CssClass="df-nav-link" OnClick="ibtnRMD_Click" ToolTip="View RMD based on PO's, Sales orders, Works Orders."><i class="icon solid fa-warehouse df-nav-icon-font"></i><span class="df-nav-text">Raw Materials Demands</span></asp:LinkButton>
                    </div>
                </nav>

                <div class="df-sidebar-footer">
                    <a href="https://mydatafusion.online/learningCenter.aspx" class="df-nav-link" target="_blank"><i class="icon solid fa-lightbulb df-nav-icon-font"></i><span class="df-nav-text">Learning Hub</span></a>
                </div>
            </aside>

            <div class="df-main">
                <header class="df-topbar">
                    <button type="button" class="df-icon-btn" id="dfSidebarToggle" title="Toggle menu" aria-label="Toggle menu"><i class="icon solid fa-bars"></i></button>
                    <div class="df-topbar-title">
                        <h1 class="df-h1">Data Fusion <span class="df-topbar-sub">by Syncflo</span></h1>
                        <span class="df-topbar-co">Stock Control</span>
                    </div>
                    <div class="df-topbar-spacer"></div>
                    <button type="button" class="df-icon-btn" id="dfThemeToggle" data-icon-light="fa-moon" data-icon-dark="fa-sun" title="Toggle dark mode" aria-label="Toggle dark mode"><i class="icon solid fa-moon" id="dfThemeIcon" data-icon-light="fa-moon" data-icon-dark="fa-sun"></i></button>
                    <asp:Image ID="imgCoImg" runat="server" CssClass="df-co-logo" />
                    <span class="df-user"><asp:Label ID="lblUsername" runat="server" Text=""></asp:Label></span>
                    <asp:LinkButton ID="lbtnLogOut" runat="server" CssClass="df-icon-btn" OnClick="lbtnLogOut_Click" ToolTip="Log Out" aria-label="Log Out"><i class="icon solid fa-sign-out-alt"></i></asp:LinkButton>
                </header>

                <main class="df-content">
                    <div class="df-page-head">
                        <h2 class="df-page-title"><i class="icon solid fa-cubes df-page-icon"></i>Stock Control</h2>
                    </div>

                    <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <div class="df-card-grid">

                                <div class="df-card">
                                    <h3 class="df-card-title">Stock Tracking</h3>
                                    <div class="df-action-list">
                                        <asp:LinkButton ID="LinkButton1" runat="server" PostBackUrl="~/PurchaseOrdersIncomplete.aspx" CssClass="df-action" ToolTip="View SBCA Purchase Orders with lines "><i class="icon solid fa-file-invoice"></i>Purchase Orders By Lines</asp:LinkButton>
                                        <asp:LinkButton ID="LinkButton3" runat="server" PostBackUrl="~/OSPurchaseOrdersPartial.aspx" CssClass="df-action" ToolTip="View partially received Purchase Orders"><i class="icon solid fa-box-open"></i>Partially Received Purchase Orders</asp:LinkButton>
                                        <asp:LinkButton ID="LinkButton2" runat="server" PostBackUrl="~/SalesOrdersIncomplete.aspx" CssClass="df-action" ToolTip="View and logs of items and lot number transactions."><i class="icon solid fa-shopping-cart"></i>Incomplete Sales Orders</asp:LinkButton>
                                    </div>
                                </div>

                                <div class="df-card">
                                    <h3 class="df-card-title">Movement</h3>
                                    <div class="df-action-list">
                                        <asp:LinkButton ID="imgbTrf" runat="server" OnClick="imgbTrf_Click" CssClass="df-action" ToolTip="Carry out an inter-store transfer"><i class="icon solid fa-exchange-alt"></i>Quick Inter-Store Transfer</asp:LinkButton>
                                        <asp:LinkButton ID="imgbTrfB" runat="server" OnClick="imgbTrfB_Click" CssClass="df-action" ToolTip="Carry out an inter-store transfer"><i class="icon solid fa-dolly"></i>Bulk Item Transfer</asp:LinkButton>
                                        <asp:LinkButton ID="imgItemAdjust" runat="server" OnClick="imgItemAdjust_Click" CssClass="df-action" ToolTip="Carry out an item adjustment with the option of updating Sage Accounting"><i class="icon solid fa-sliders-h"></i>Item Adjustment</asp:LinkButton>
                                    </div>
                                </div>

                                <div class="df-card">
                                    <h3 class="df-card-title">Counts</h3>
                                    <div class="df-action-list">
                                        <asp:LinkButton ID="lbtnStckCount" runat="server" CssClass="df-action" OnClick="lbtnStckCount_Click" ToolTip="Plan and record stock takes."><i class="icon solid fa-clipboard-list"></i>Stock Counts</asp:LinkButton>
                                    </div>
                                </div>

                                <div class="df-card">
                                    <h3 class="df-card-title">Custom Reports</h3>
                                    <div class="df-action-list">
                                        <asp:LinkButton ID="lbtnCustom" runat="server" CssClass="df-action" OnClick="lbtnCustom_Click" ToolTip="View custom reports created for you."><i class="icon solid fa-file-alt"></i>My Customised</asp:LinkButton>
                                    </div>
                                </div>

                                <div class="df-card">
                                    <h3 class="df-card-title">Analysis</h3>
                                    <div class="df-action-list">
                                        <asp:LinkButton ID="lbtnStockMove" runat="server" OnClick="lbtnStockMove_Click" CssClass="df-action" ToolTip="View and logs of items and lot number transactions."><i class="icon solid fa-random"></i>Stock/Lot Movement</asp:LinkButton>
                                        <asp:LinkButton ID="lbtnSOH" runat="server" OnClick="lbtnSOH_Click" CssClass="df-action" ToolTip="View stock balances by store"><i class="icon solid fa-balance-scale"></i>Stock/Lot Balances</asp:LinkButton>
                                        <asp:LinkButton ID="lbtnPickGP" runat="server" OnClick="lbtnPickGP_Click" CssClass="df-action" ToolTip="Analyse picking slips and view GP per each one"><i class="icon solid fa-chart-pie"></i>Picking Slip GP Analysis</asp:LinkButton>
                                        <asp:LinkButton ID="lbtnItemGP" runat="server" OnClick="lbtnItemGP_Click" CssClass="df-action" ToolTip="Analyse item sale and view GP per item"><i class="icon solid fa-chart-bar"></i>Item Sales GP Analysis</asp:LinkButton>
                                    </div>
                                </div>

                            </div>
                          </ContentTemplate>
                    </asp:UpdatePanel>
                </main>
            </div>
        </div>
  </form>
</body>
</html>
