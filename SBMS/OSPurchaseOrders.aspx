<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="OSPurchaseOrders.aspx.cs" Inherits="SBMS.OSPurchaseOrders" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>OS Purchase Orders</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <%--<link rel="stylesheet" href="assets/css/main.css" />--%>
    <link rel="stylesheet" href="prologue/assets/css/main.css" />
    <%-- bump ?v= whenever df-theme.css / df-ui.js change, so browsers don't serve a stale copy --%>
    <link rel="stylesheet" href="assets/css/df-theme.css?v=13" />
    <script src="assets/js/df-ui.js?v=13"></script>
</head>
<body class="df-page df-mod-purchases">
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>

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
                        <asp:LinkButton ID="imgbRec" runat="server" CssClass="df-nav-link df-nav-link-active" aria-current="page" OnClick="imgbRec_Click" ToolTip="Receive from Purchase Orders, allocate lot numbers"><i class="icon solid fa-file-invoice df-nav-icon-font"></i><span class="df-nav-text">Purchase Orders</span></asp:LinkButton>
                        <asp:LinkButton ID="ibtnPickSlips" runat="server" CssClass="df-nav-link" OnClick="ibtnPickSlips_Click" ToolTip="View Sales Orders and picking slips, fulfill orders"><i class="icon solid fa-shopping-cart df-nav-icon-font"></i><span class="df-nav-text">Sales Orders</span></asp:LinkButton>
                        <asp:LinkButton ID="ibtnPickTrack" runat="server" CssClass="df-nav-link" OnClick="ibtnPickTrack_Click" ToolTip="Track all picking slips in a simple drag and drop process "><i class="icon solid fa-tasks df-nav-icon-font"></i><span class="df-nav-text">Picking Slip Tracking</span></asp:LinkButton>
                        <asp:LinkButton ID="ibtnStckCtl" runat="server" CssClass="df-nav-link" ToolTip="Inter Store Transfers, Stock adjustments, Stock Takes, Reporting" OnClick="ibtnStckCtl_Click"><i class="icon solid fa-cubes df-nav-icon-font"></i><span class="df-nav-text">Stock Control</span></asp:LinkButton>
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
                        <span class="df-topbar-co">Purchase Orders</span>
                    </div>
                    <div class="df-topbar-spacer"></div>
                    <button type="button" class="df-icon-btn" id="dfThemeToggle" data-icon-light="fa-moon" data-icon-dark="fa-sun" title="Toggle dark mode" aria-label="Toggle dark mode"><i class="icon solid fa-moon" id="dfThemeIcon" data-icon-light="fa-moon" data-icon-dark="fa-sun"></i></button>
                    <asp:Image ID="imgCoImg" runat="server" CssClass="df-co-logo" />
                    <span class="df-user"><asp:Label ID="lblUsername" runat="server" Text=""></asp:Label></span>
                    <asp:LinkButton ID="lbtnLogOut" runat="server" CssClass="df-icon-btn" OnClick="lbtnLogOut_Click" ToolTip="Log Out" aria-label="Log Out"><i class="icon solid fa-sign-out-alt"></i></asp:LinkButton>
                </header>

                <main class="df-content">
                    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                        <ContentTemplate>

                            <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
                                <ProgressTemplate>
                                    <div class="df-loading-overlay">
                                        <div class="df-loading-box">
                                            <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." CssClass="df-loading-spinner" />
                                        </div>
                                    </div>
                                </ProgressTemplate>
                            </asp:UpdateProgress>

                            <div style="display: none"><asp:Label ID="lblDir" runat="server" Text=""></asp:Label></div>

                            <div class="df-page-head">
                                <h2 class="df-page-title"><i class="icon solid fa-file-invoice df-page-icon"></i>Purchase Orders<asp:Label ID="lblpoqty" runat="server" Text="" CssClass="df-count"></asp:Label></h2>
                                <asp:LinkButton ID="lbtnSOs" runat="server" CssClass="df-btn" PostBackUrl="~/PurchaseOrdersIncomplete.aspx"><i class="icon solid fa-chevron-circle-right"></i>&nbsp;Purchase Order Tracking</asp:LinkButton>
                            </div>

                            <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnfind" CssClass="df-card df-filters">
                                <div class="df-filter">
                                    <asp:Label runat="server" CssClass="df-label" AssociatedControlID="txtfind" Text="Find (Supplier / PO Num)"></asp:Label>
                                    <div class="df-input-group">
                                        <asp:TextBox ID="txtfind" runat="server" CssClass="df-input"></asp:TextBox>
                                        <asp:LinkButton ID="lbtnfind" runat="server" CssClass="df-btn df-btn-primary" OnClick="lbtnfind_Click"><i class="icon solid fa-search"></i></asp:LinkButton>
                                    </div>
                                </div>
                                <div class="df-filter">
                                    <asp:Label runat="server" CssClass="df-label" AssociatedControlID="DDPOStatus" Text="Filter By Status"></asp:Label>
                                    <asp:DropDownList ID="DDPOStatus" runat="server" CssClass="df-select" AutoPostBack="true" OnSelectedIndexChanged="DDPOStatus_SelectedIndexChanged"></asp:DropDownList>
                                </div>
                                <div class="df-filter df-filter-check">
                                    <asp:CheckBox ID="chkCompl" runat="server" Text="Show Completed" AutoPostBack="true" OnCheckedChanged="chkCompl_CheckedChanged" />
                                </div>
                            </asp:Panel>

                            <div class="df-card df-table-card">
                                <div class="df-table-wrap">
                                    <asp:GridView ID="GridPOs" runat="server" AutoGenerateColumns="false" CssClass="gridview" OnRowDataBound="GridPOs_RowDataBound" OnSelectedIndexChanged="GridPOs_SelectedIndexChanged" AllowSorting="true" OnSorting="GridPOs_Sorting">
                                        <HeaderStyle CssClass="gridViewHeader" />
                                        <FooterStyle CssClass="gridViewHeader" />
                                        <RowStyle CssClass="gridViewRow" />
                                        <AlternatingRowStyle CssClass="gridViewAltRow" />
                                        <PagerStyle CssClass="gridViewPager" />
                                        <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="5" />
                                        <Columns>
                                            <asp:BoundField DataField="DocID" ReadOnly="True" />
                                            <asp:TemplateField HeaderText="PO #" ItemStyle-Width="6em" SortExpression="DocumentNumber">
                                                <ItemTemplate>
                                                    <asp:LinkButton ID="lbtnPO" CommandArgument='<%# Eval("DocGUID")%>' CommandName="lbtnPO" runat="server" Text='<%# Eval("DocumentNumber")%>' ToolTip="View Sales Order" CssClass="df-link-strong" OnClick="lbtnPO_Click"></asp:LinkButton>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField HeaderText="Supplier Name" DataField="CustSupName" ReadOnly="True" SortExpression="CustSupName" />
                                            <asp:BoundField HeaderText="Reference" DataField="Reference" ReadOnly="True" SortExpression="Reference" />
                                            <asp:BoundField HeaderText="Inv #" DataField="SupplierInvNum" ReadOnly="True" SortExpression="SupplierInvNum" />
                                            <asp:BoundField HeaderText="Due_Date" DataField="DueDelDate" ReadOnly="True" DataFormatString="{0:dd MMM yyyy}" ItemStyle-Width="8em" SortExpression="DueDelDate" />
                                            <asp:BoundField HeaderText="Total" DataField="Total" ReadOnly="True" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" ItemStyle-Width="10em" />
                                            <asp:BoundField HeaderText="Status" DataField="Status" ReadOnly="True" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="8em" SortExpression="Status" />
                                            <asp:TemplateField HeaderText="Started" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="5em" SortExpression="RecStarted">
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="chkstarted" runat="server" Checked='<%# Eval("Started")%>' Enabled="false" Text=" " />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Complete" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="5em">
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="chkComplete" runat="server" Checked='<%# Eval("Complete") %>' Enabled="false" Text=" " />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="2em">
                                                <ItemTemplate>
                                                    <asp:LinkButton ID="lbtnDeletePO" CommandArgument='<%# Eval("DocGUID") %>' CommandName="lbtnDeleteLine" runat="server" CssClass="icon solid fa-ban df-link-danger" ToolTip="Delete PO" OnClick="lbtnDeletePO_Click"> </asp:LinkButton>
                                                    <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Delete Purchase Order?" Enabled="True" TargetControlID="lbtnDeletePO"></cci:ConfirmButtonExtender>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>
                                </div>
                            </div>

                        </ContentTemplate>
                    </asp:UpdatePanel>
                </main>
            </div>
        </div>

    </form>
     <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</body>
</html>
