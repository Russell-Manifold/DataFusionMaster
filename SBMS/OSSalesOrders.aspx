<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="OSSalesOrders.aspx.cs" Inherits="SBMS.OSSalesOrders" %>
<%@ Register Src="~/CommonScripts.ascx" TagPrefix="uc" TagName="CommonScripts" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Sales Orders</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="prologue/assets/css/main.css" />
    <%-- bump ?v= whenever df-theme.css / df-ui.js change, so browsers don't serve a stale copy --%>
    <link rel="stylesheet" href="assets/css/df-theme.css?v=13" />
    <link href="lib/toastr/toastr.min.css" rel="stylesheet" />
   <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script  type="text/javascript" src="lib/toastr/toastr.min.js"></script>
    <script type="text/javascript" src="scripts/notifications.js"></script>
    <script src="assets/js/df-ui.js?v=13"></script>
</head>
<body class="df-page df-mod-sales">
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <uc:CommonScripts ID="CommonScripts" runat="server" />

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
                        <asp:LinkButton ID="ibtnPickSlips" runat="server" CssClass="df-nav-link df-nav-link-active" aria-current="page" OnClick="ibtnPickSlips_Click" ToolTip="View Sales Orders and picking slips, fulfill orders"><i class="icon solid fa-shopping-cart df-nav-icon-font"></i><span class="df-nav-text">Sales Orders</span></asp:LinkButton>
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
                        <span class="df-topbar-co">Sales Orders</span>
                    </div>
                    <div class="df-topbar-spacer"></div>
                    <button type="button" class="df-icon-btn" id="dfThemeToggle" data-icon-light="fa-moon" data-icon-dark="fa-sun" title="Toggle dark mode" aria-label="Toggle dark mode"><i class="icon solid fa-moon" id="dfThemeIcon" data-icon-light="fa-moon" data-icon-dark="fa-sun"></i></button>
                    <asp:Image ID="imgCoImg" runat="server" CssClass="df-co-logo" />
                    <span class="df-user"><asp:Label ID="lblUsername" runat="server" Text=""></asp:Label></span>
                    <asp:LinkButton ID="lbtnLogOut" runat="server" CssClass="df-icon-btn" OnClick="lbtnLogOut_Click" ToolTip="Log Out" aria-label="Log Out"><i class="icon solid fa-sign-out-alt"></i></asp:LinkButton>
                </header>

                <main class="df-content">
                    <div class="df-page-head">
                        <h2 class="df-page-title"><i class="icon solid fa-shopping-cart df-page-icon"></i>Sales Orders <asp:Label ID="lblpoqty" runat="server" Text="" CssClass="df-count"></asp:Label></h2>
                        <asp:LinkButton ID="lbtnSOs" runat="server" CssClass="df-btn" PostBackUrl="~/SalesOrdersIncomplete.aspx"><i class="icon solid fa-chevron-circle-right"></i>&nbsp;Sales Order Tracking</asp:LinkButton>
                    </div>

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

                            <div style="display:none">
                            <asp:Label ID="lblDir" runat="server" Text=""></asp:Label>
                            </div>

                            <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnfind" CssClass="df-card df-filters">
                                <div class="df-filter">
                                    <asp:Label runat="server" CssClass="df-label" AssociatedControlID="txtfind" Text="Find (Cust / SO # / Ref)"></asp:Label>
                                    <div class="df-input-group">
                                        <asp:TextBox ID="txtfind" runat="server" CssClass="df-input"></asp:TextBox>
                                        <asp:LinkButton ID="lbtnfind" runat="server" CssClass="df-btn df-btn-primary" OnClick="lbtnfind_Click" ToolTip="Search Sales Orders"><i class="icon solid fa-search"></i></asp:LinkButton>
                                    </div>
                                </div>
                                <div class="df-filter">
                                    <asp:Label runat="server" CssClass="df-label" AssociatedControlID="DDStatus" Text="Status"></asp:Label>
                                    <asp:DropDownList ID="DDStatus" runat="server" CssClass="df-select df-select-sm" AutoPostBack="true" OnSelectedIndexChanged="DDSOStatus_SelectedIndexChanged"></asp:DropDownList>
                                </div>
                                <div class="df-filter">
                                    <asp:Label runat="server" CssClass="df-label" AssociatedControlID="DDSOStatus" Text="SO Status"></asp:Label>
                                    <asp:DropDownList ID="DDSOStatus" runat="server" CssClass="df-select df-select-sm" AutoPostBack="true" OnSelectedIndexChanged="DDSOStatus_SelectedIndexChanged"></asp:DropDownList>
                                </div>
                                <div class="df-filter">
                                    <asp:Label runat="server" CssClass="df-label" AssociatedControlID="DDueDate" Text="Due Date &lt;="></asp:Label>
                                    <asp:DropDownList ID="DDueDate" runat="server" CssClass="df-select df-select-sm" AutoPostBack="true" OnSelectedIndexChanged="DDSOStatus_SelectedIndexChanged"></asp:DropDownList>
                                </div>
                                <div class="df-filter df-filter-check">
                                    <asp:CheckBox ID="chkCompl" runat="server" Text=" Show Completed" AutoPostBack="true" OnCheckedChanged="chkCompl_CheckedChanged" />
                                </div>
                                <div class="df-filter">
                                    <asp:LinkButton ID="lbtnDownload" runat="server" CssClass="df-btn" ToolTip="Download to excel" OnClick="lbtnDownload_Click"><i class="icon solid fa-download"></i>&nbsp;Export</asp:LinkButton>
                                </div>
                            </asp:Panel>

                            <div class="df-card df-table-card">
                                <div class="df-table-wrap">
                                    <asp:GridView ID="GridPOs" runat="server" AutoGenerateColumns="false" CssClass="gridview" OnRowDataBound="GridPOs_RowDataBound" OnRowCommand="GridPOs_RowCommand" AllowSorting="true" OnSorting="GridPOs_Sorting" ToolTip="Open Sales Order">
                                        <HeaderStyle CssClass="gridViewHeader" />
                                        <FooterStyle CssClass="gridViewHeader" />
                                        <RowStyle CssClass="gridViewRow" />
                                        <AlternatingRowStyle CssClass="gridViewAltRow" />
                                        <PagerStyle CssClass="gridViewPager" />
                                        <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="5" />
                                        <Columns>
                                            <asp:BoundField DataField="DocID" ReadOnly="True" />
                                            <asp:TemplateField HeaderText="SO #" ItemStyle-Width="6em" SortExpression="DocumentNumber" >
                                                <ItemTemplate >
                                                    <asp:LinkButton ID="lbtnSO" CommandArgument='<%# Eval("DocGUID")%>' CommandName="lbtnSO" runat="server" Text='<%# Eval("DocumentNumber")%>' ToolTip="View Sales Order" CssClass="df-link-strong" ></asp:LinkButton>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField HeaderText="Customer" DataField="CustSupName" ReadOnly="True" SortExpression="CustSupName" />
                                            <asp:BoundField HeaderText="Address_3" DataField="DelAddress3" ReadOnly="True" SortExpression="DelAddress3" ItemStyle-CssClass="hidecolumn" HeaderStyle-CssClass="hidecolumn" />
                                            <asp:BoundField HeaderText="Reference" DataField="Reference" ReadOnly="True" SortExpression="Reference" />
                                            <asp:TemplateField HeaderText="Pick-Slip" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" SortExpression="LinkedPSNum">
                                                <ItemTemplate >
                                                    <asp:LinkButton ID="lbtnPS" CommandArgument='<%# String.Format("{0} | {1}", Eval("DocGUID"), Eval("LinkedPSNum")) %>' CommandName="lbtnPS"  runat="server" Text='<%# Eval("LinkedPSNum")%>' ToolTip="View Picking Slip" CssClass="df-link-strong" ></asp:LinkButton>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Job Card" ItemStyle-BackColor="antiquewhite" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" SortExpression="LinkedJCNum">
                                                <ItemTemplate>
                                                    <asp:LinkButton ID="lbtnJC" CommandArgument='<%# String.Format("{0} | {1}", Eval("DocGUID"), Eval("LinkedJCNum")) %>' CommandName="lbtnJC"  runat="server" Text='<%# Eval("LinkedJCNum")%>' ToolTip="View Job Card" CssClass="df-link-strong"></asp:LinkButton>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                             <asp:BoundField HeaderText="Status" DataField="LinkedStatus" ReadOnly="True" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="8em" SortExpression="LinkedStatus" />
                                            <asp:BoundField HeaderText="Due_Date" DataField="DueDelDate" ReadOnly="True" DataFormatString="{0:dd MMM yyyy}" ItemStyle-Width="8em" SortExpression="DueDelDate" />
                                            <asp:BoundField HeaderText="Total" DataField="Total" ReadOnly="True" DataFormatString="{0:#.00}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
                                            <asp:TemplateField HeaderText="Start" ItemStyle-HorizontalAlign="Left" SortExpression="RecStarted" ItemStyle-Width="3em" HeaderStyle-HorizontalAlign="Right">
                                                <ItemTemplate >
                                                    <asp:CheckBox ID="chkstarted" runat="server" Checked='<%# Eval("Started")%>' Enabled="false" Text=" "  />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Compl" ItemStyle-HorizontalAlign="Left" ItemStyle-Width="3em" HeaderStyle-HorizontalAlign="Right" ItemStyle-VerticalAlign="Bottom">
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="chkComplete" runat="server" Checked='<%# Eval("Complete") %>' Enabled="false" Text=" " />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField HeaderText="SO_Status" DataField="Status" ReadOnly="True" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" SortExpression="Status" />
                                            <asp:BoundField HeaderText="Delivery" DataField="DeliveryBy" ReadOnly="True" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" SortExpression="DeliveryBy" ItemStyle-CssClass="hidecolumn" HeaderStyle-CssClass="hidecolumn" />
                                        </Columns>
                                    </asp:GridView>
                                </div>
                            </div>
                        </ContentTemplate>
                        <Triggers>
                            <asp:PostBackTrigger ControlID="lbtnDownload" />
                            <asp:PostBackTrigger ControlID="imgbRec" />
                            <asp:PostBackTrigger ControlID="ibtnPickSlips" />
                        </Triggers>
                    </asp:UpdatePanel>
                </main>
            </div>
        </div>

    </form>
     <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</body>
</html>
