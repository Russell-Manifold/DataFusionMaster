<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="SBMS.Dashboard" %>
<%@ Register Src="~/CommonScripts.ascx" TagPrefix="uc" TagName="CommonScripts" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Data Fusion</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <%-- bump ?v= whenever the df-* assets change, so browsers don't serve a stale copy --%>
    <link rel="stylesheet" href="assets/css/df-theme.css?v=13" />
    <link href="lib/toastr/toastr.min.css" rel="stylesheet" />
   <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script type="text/javascript" src="lib/toastr/toastr.min.js"></script>
    <script type="text/javascript" src="scripts/notifications.js"></script>
     <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    <script src="assets/js/df-ui.js?v=13"></script>
    <script src="niceadmin/assets/vendor/apexcharts/apexcharts.min.js"></script>
    <script src="assets/js/df-dashboard-charts.js?v=13"></script>
</head>
<body class="df-page">
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true"></asp:ScriptManager>
        <uc:CommonScripts ID="CommonScripts" runat="server" />

        <div id='myHiddenDiv' runat="server" style='display: none' class="df-loading-overlay">
            <div class="df-loading-box">
                <img src="images/tenorwait.gif" id='myAnimatedImage' align='absmiddle' class="df-loading-spinner" />
            </div>
        </div>

        <div class="df-shell" id="dfShell">
            <aside class="df-sidebar" id="dfSidebar">
                <div class="df-sidebar-brand">
                    <a href="https://mydatafusion.online" title="My Data Fusion website" target="_blank">
                        <img src="images/logo.png" class="df-brand-logo" alt="Data Fusion" />
                    </a>
                </div>
                <nav class="df-nav">
                    <asp:Panel ID="pnlButtons" runat="server">
                        <div class="df-nav-group">
                            <h5 class="df-nav-heading">Stores, Receiving &amp; Picking</h5>
                            <asp:LinkButton ID="imgbRec" runat="server" CssClass="df-nav-link" OnClick="imgbRec_Click" OnClientClick="showDiv()" ToolTip="See outstanding Purchase Orders and carry out receiving process.">
                                <i class="icon fa-truck df-nav-icon-font"></i><span class="df-nav-text">Receiving</span>
                            </asp:LinkButton>
                            <asp:LinkButton ID="ibtnPickSlips" runat="server" CssClass="df-nav-link" OnClick="ibtnPickSlips_Click" OnClientClick="showDiv()" ToolTip="See all open sales orders, and their associated Picking slips or Job cards. Process your Sales Orders from here.">
                                <i class="icon fa-shopping-cart df-nav-icon-font"></i><span class="df-nav-text">Sales Orders &amp; Picking Slips</span>
                            </asp:LinkButton>
                            <asp:LinkButton ID="ibtnPickTrack" runat="server" CssClass="df-nav-link" OnClick="ibtnPickTrack_Click" ToolTip="Track and move all open Picking Slips.">
                                <i class="icon fa-tasks df-nav-icon-font"></i><span class="df-nav-text">Picking Slip Tracking</span>
                            </asp:LinkButton>
                            <asp:LinkButton ID="ibtnStckCtl" runat="server" CssClass="df-nav-link" OnClick="ibtnStckCtl_Click" ToolTip="Stock Transfers, Adjustments, Reports &amp; Stock counts.">
                                <i class="icon fa-cubes df-nav-icon-font"></i><span class="df-nav-text">Stock Control</span>
                            </asp:LinkButton>
                            <asp:LinkButton ID="ibtnmrp2" runat="server" CssClass="df-nav-link" OnClick="ibtnmrp2_Click" ToolTip="View finished goods materials requirements based on purchase orders, sales orders, stock balances, minimum stock etc.">
                                <i class="icon fa-list-alt df-nav-icon-font"></i><span class="df-nav-text">FG Requirements (MRP)</span>
                            </asp:LinkButton>
                        </div>

                        <asp:Panel ID="PnlForecast" runat="server" CssClass="df-nav-group">
                            <h5 id="headmod2" runat="server" class="df-nav-heading">Planning, Job Cards &amp; Kits</h5>
                            <asp:LinkButton ID="ibtnFCasts" runat="server" CssClass="df-nav-link" OnClick="ibtnFCasts_Click" ToolTip="Add/Edit Sales Forecasts">
                                <i class="icon fa-line-chart df-nav-icon-font"></i><span class="df-nav-text">Sales Forecasts</span>
                            </asp:LinkButton>
                            <asp:LinkButton ID="ibtnmrp" runat="server" CssClass="df-nav-link" OnClick="ibtnmrp2_Click" ToolTip="View finished goods materials requirements based on purchase orders, sales orders, stock balances, minimum stock etc.">
                                <i class="icon fa-list-alt df-nav-icon-font"></i><span class="df-nav-text">FG Requirements (MRP)</span>
                            </asp:LinkButton>
                            <asp:LinkButton ID="ibtnJobTrack" runat="server" CssClass="df-nav-link" OnClick="ibtnJobTrack_Click" ToolTip="View all open job cards and complete job card workflows.">
                                <i class="icon fa-check-square-o df-nav-icon-font"></i><span class="df-nav-text">Job Card Tracking</span>
                            </asp:LinkButton>
                        </asp:Panel>

                        <asp:Panel ID="PnlProduction" runat="server" CssClass="df-nav-group">
                            <h5 class="df-nav-heading">Manufacturing, BOMs &amp; MRP</h5>
                            <asp:LinkButton ID="ibtmWorksOrders" runat="server" CssClass="df-nav-link" OnClick="ibtmWorksOrders_Click" ToolTip="View all current manufacturing or production works orders.">
                                <i class="icon fa-industry df-nav-icon-font"></i><span class="df-nav-text">Works Orders</span>
                            </asp:LinkButton>
                            <asp:LinkButton ID="ibtmWOrdMgment" runat="server" CssClass="df-nav-link" OnClick="ibtmWOrdMgment_Click" ToolTip="Record manufacturing/production and allocate raw materials.">
                                <i class="icon fa-cogs df-nav-icon-font"></i><span class="df-nav-text">Works Order Filling</span>
                            </asp:LinkButton>
                            <asp:LinkButton ID="ibtnRMD" runat="server" CssClass="df-nav-link" OnClick="ibtnRMD_Click" ToolTip="View and analyse raw materials demands based on current works orders.">
                                <i class="icon fa-cube df-nav-icon-font"></i><span class="df-nav-text">Raw Materials Demand</span>
                            </asp:LinkButton>
                        </asp:Panel>
                    </asp:Panel>
                </nav>

                <div class="df-sidebar-footer">
                    <a href="SBMSMobile/DashboardM.aspx" class="df-nav-link" target="_blank"><i class="icon fa-mobile df-nav-icon-font"></i><span class="df-nav-text">Mobile</span></a>
                    <a href="https://mydatafusion.online/learningCenter.aspx" class="df-nav-link" target="_blank"><i class="icon fa-lightbulb-o df-nav-icon-font"></i><span class="df-nav-text">Learning Hub</span></a>
                </div>
            </aside>

            <div class="df-main">
                <header class="df-topbar">
                    <button type="button" class="df-icon-btn" id="dfSidebarToggle" aria-label="Toggle menu" title="Toggle menu">
                        <i class="icon fa-bars"></i>
                    </button>
                    <div class="df-topbar-title">
                        <h1 class="df-h1">Data Fusion <span class="df-topbar-sub">by Syncflo</span></h1>
                        <span class="df-topbar-co">for <asp:Label ID="lblCoName" runat="server" Text=""></asp:Label></span>
                    </div>
                    <div class="df-topbar-spacer"></div>
                    <button type="button" class="df-icon-btn" id="dfThemeToggle" aria-label="Toggle dark mode" title="Toggle dark mode">
                        <i class="icon fa-moon-o" id="dfThemeIcon"></i>
                    </button>
                    <asp:Image ID="imgCoImg" runat="server" CssClass="df-co-logo" />
                    <span class="df-user"><asp:Label ID="lblUserName" runat="server" Text=""></asp:Label></span>
                    <asp:LinkButton ID="lbtnAdmin" runat="server" CssClass="df-icon-btn" ToolTip="Config, Settings and Master file management" PostBackUrl="~/ConfigMaster.aspx" aria-label="Admin"><i class="icon fa-gears"></i></asp:LinkButton>
                    <asp:LinkButton ID="lbtnLogOut" runat="server" CssClass="df-icon-btn" OnClick="lbtnLogOut_Click" ToolTip="Log Out" aria-label="Log Out"><i class="icon fa-eject"></i></asp:LinkButton>
                </header>

                <main class="df-content">
                    <div id="lbluat" runat="server" class="df-banner df-banner-info">
                        WARNING - Your profile is in <strong>User Acceptance Testing (UAT)</strong> status.
                        This means data will come FROM SBCA, but NO data will be sent to or updated in SBCA.
                        When you are ready, please email us with a request to activate full 2 way integration.
                    </div>

                    <div class="df-banner df-banner-warning">
                        <asp:Label ID="lblWarn" runat="server" Text=""></asp:Label>
                    </div>

                    <div class="df-kpi-row">
                        <asp:LinkButton ID="lnkKpiPO" runat="server" CssClass="df-kpi" OnClick="imgbRec_Click" ToolTip="View outstanding Purchase Orders">
                            <span class="df-kpi-icon df-kpi-icon-po"><i class="icon fa-file-text-o"></i></span>
                            <span class="df-kpi-body">
                                <span class="df-kpi-value"><asp:Label ID="lblKpiPO" runat="server" Text="0"></asp:Label></span>
                                <span class="df-kpi-label">Open Purchase Orders</span>
                            </span>
                        </asp:LinkButton>
                        <asp:LinkButton ID="lnkKpiSO" runat="server" CssClass="df-kpi" OnClick="ibtnPickSlips_Click" ToolTip="View open Sales Orders">
                            <span class="df-kpi-icon df-kpi-icon-so"><i class="icon fa-shopping-cart"></i></span>
                            <span class="df-kpi-body">
                                <span class="df-kpi-value"><asp:Label ID="lblKpiSO" runat="server" Text="0"></asp:Label></span>
                                <span class="df-kpi-label">Open Sales Orders</span>
                            </span>
                        </asp:LinkButton>
                        <asp:LinkButton ID="lnkKpiWO" runat="server" CssClass="df-kpi" OnClick="ibtmWorksOrders_Click" ToolTip="View current Works Orders">
                            <span class="df-kpi-icon df-kpi-icon-wo"><i class="icon fa-industry"></i></span>
                            <span class="df-kpi-body">
                                <span class="df-kpi-value"><asp:Label ID="lblKpiWO" runat="server" Text="0"></asp:Label></span>
                                <span class="df-kpi-label">Open Works Orders</span>
                            </span>
                        </asp:LinkButton>
                    </div>

                    <div id="divCharts" runat="server">
                        <div class="df-card">
                            <h3 class="df-card-title">Sales &amp; Gross Profit <span class="df-card-sub">last 12 months</span></h3>
                            <div id="dfChartSales" class="df-chart"></div>
                        </div>
                        <div class="df-card">
                            <h3 class="df-card-title">Top Customers <span class="df-card-sub">by sales value, last 12 months</span></h3>
                            <div id="dfChartCustomers" class="df-chart df-chart-sm"></div>
                        </div>
                    </div>

                    <div class="df-card df-quickjump">
                        <label class="df-label" for="<%= DDProgBoard.ClientID %>">Tracking Board</label>
                        <asp:DropDownList ID="DDProgBoard" runat="server" CssClass="df-select" AutoPostBack="true" OnSelectedIndexChanged="DDProgBoard_SelectedIndexChanged">
                            <asp:ListItem> - Tracking Board - </asp:ListItem>
                            <asp:ListItem>Picking Slips</asp:ListItem>
                            <asp:ListItem>Job Cards</asp:ListItem>
                            <asp:ListItem>Production</asp:ListItem>
                        </asp:DropDownList>
                    </div>

                    <div class="df-card df-welcome">
                        <h2>Welcome back</h2>
                        <p class="df-muted">Use the menu on the left to jump into Receiving, Sales Orders, Stock Control, Planning and Production.</p>
                    </div>
                </main>
            </div>
        </div>

        <script>
            function showDiv() {
                document.getElementById('myHiddenDiv').style.display = "";
                document.getElementById('pnlButtons').style.display = "none";
                setTimeout('document.images["myAnimatedImage"].src="images/tenorwait.gif"', 200);
            }
        </script>
    </form>
</body>
</html>
