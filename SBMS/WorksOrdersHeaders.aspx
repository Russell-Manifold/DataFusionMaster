<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="WorksOrdersHeaders.aspx.cs" Inherits="SBMS.WorksOrdersHeaders" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Works Orders</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <%--<link rel="stylesheet" href="assets/css/main.css" />--%>
    <link rel="stylesheet" href="prologue/assets/css/main.css" />
    <%-- bump ?v= whenever df-theme.css / df-ui.js change, so browsers don't serve a stale copy --%>
    <link rel="stylesheet" href="assets/css/df-theme.css?v=13" />
    <script src="assets/js/df-ui.js?v=13"></script>
</head>
<body class="df-page df-mod-works">
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
                        <asp:LinkButton ID="imgbRec" runat="server" CssClass="df-nav-link" OnClick="imgbRec_Click" ToolTip="Receive from Purchase Orders, allocate lot numbers"><i class="icon solid fa-file-invoice df-nav-icon-font"></i><span class="df-nav-text">Purchase Orders</span></asp:LinkButton>
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
                        <asp:LinkButton ID="ibtmWorksOrders" runat="server" CssClass="df-nav-link df-nav-link-active" aria-current="page" OnClick="ibtmWorksOrders_Click" ToolTip="Create and view works orders for manufacturing or production"><i class="icon solid fa-industry df-nav-icon-font"></i><span class="df-nav-text">Works Orders</span></asp:LinkButton>
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
                        <span class="df-topbar-co">Works Orders</span>
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

                            <div style="display:none">
                            <asp:Label ID="lblDir" runat="server" Text=""></asp:Label>
                            </div>

                            <%-- kept inside the UpdatePanel so lbtnCreateNew keeps its async postback --%>
                            <div class="df-page-head">
                                <h2 class="df-page-title"><i class="icon solid fa-industry df-page-icon"></i>Works Orders</h2>
                                <asp:LinkButton ID="lbtnCreateNew" runat="server" CssClass="df-btn df-btn-primary" ToolTip="Add New Works Order" OnClick="lbtnCreateNew_Click"><i class="icon solid fa-plus-circle"></i>&nbsp;Add New Works Order</asp:LinkButton>
                                <cci:ConfirmButtonExtender ID="lbtnCreateNew_ConfirmButtonExtender1" runat="server" ConfirmText="Create a new Works Order, are you sure?" Enabled="True" TargetControlID="lbtnCreateNew"></cci:ConfirmButtonExtender>
                            </div>

                            <asp:Panel ID="Panel1" runat="server" CssClass="df-card df-filters">
                                <div class="df-filter">
                                    <asp:Label runat="server" CssClass="df-label" AssociatedControlID="txtSearch" Text="Find"></asp:Label>
                                    <asp:Panel ID="Panel2" runat="server" DefaultButton="lbtnSearch" CssClass="df-input-group">
                                        <asp:TextBox ID="txtSearch" runat="server" placeholder="Find" CssClass="df-input"></asp:TextBox>
                                        <asp:LinkButton ID="lbtnSearch" runat="server" CssClass="df-btn df-btn-primary" ToolTip="Seach" OnClick="lbtnSearch_Click"><i class="icon solid fa-search"></i></asp:LinkButton>
                                    </asp:Panel>
                                </div>
                                <div class="df-filter">
                                    <asp:Label runat="server" CssClass="df-label" AssociatedControlID="DDStatus" Text="Status"></asp:Label>
                                    <asp:DropDownList ID="DDStatus" runat="server" CssClass="df-select df-select-sm" AutoPostBack="true" OnSelectedIndexChanged="lbtnSearch_Click">
                                        <asp:ListItem Value="0">Active</asp:ListItem>
                                        <asp:ListItem Value="1">All</asp:ListItem>
                                        <asp:ListItem Value="2">Complete</asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                            </asp:Panel>

                            <div class="df-card df-table-card">
                                <div class="df-table-wrap">
                                    <asp:GridView ID="GridWOs" runat="server" AutoGenerateColumns="false" CssClass="gridview" AllowSorting="true" ToolTip="Open Works Order" OnSorting="GridWOs_Sorting" OnRowDataBound="GridWOs_RowDataBound">
                                        <HeaderStyle CssClass="gridViewHeader" />
                                        <FooterStyle CssClass="gridViewHeader" />
                                        <RowStyle CssClass="gridViewRow" />
                                        <AlternatingRowStyle CssClass="gridViewAltRow" />
                                        <PagerStyle CssClass="gridViewPager" />
                                        <Columns>
                                            <asp:BoundField DataField="ID" SortExpression="ID" HeaderText="ID" />
                                            <asp:TemplateField HeaderText="Number" SortExpression="ID" ItemStyle-Width="5em">
                                                <ItemTemplate>
                                                    <asp:LinkButton ID="lbtnWO" CommandArgument='<%# Eval("ID")%>' CommandName="lbtnWO"
                                                        runat="server" Text='<%# "WO" + DataBinder.Eval(Container.DataItem, "WONum").ToString() %>'
                                                        ToolTip="View Works Order Details" CssClass="df-link-strong"
                                                        OnClick="lbtnWO_Click">
                                                    </asp:LinkButton>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField HeaderText="Customer" DataField="CustSupName" SortExpression="CustSupName" />
                                            <asp:BoundField HeaderText="Reference" DataField="Reference" SortExpression="Reference" />
                                             <asp:TemplateField HeaderText="Linked Document" SortExpression="LinkedDocumentNum">
                                            <ItemTemplate>
                                                 <asp:LinkButton ID="lbtnLinkedDoc" CommandArgument='<%# Eval("LinkedDocumentNum")%>' CommandName="lbtnLinkedDoc"
                                                     runat="server" Text='<%# Eval("LinkedDocumentNum") %>'
                                                     ToolTip="View Linked Document" CssClass="df-link-strong"
                                                     OnClick="lbtnLinkedDoc_Click">
                                                 </asp:LinkButton>
                                             </ItemTemplate>
                                                 </asp:TemplateField>
                                            <asp:BoundField HeaderText="By" DataField="WOrderBy" SortExpression="WOrderBy" />
                                            <%--<asp:BoundField HeaderText="Created" DataField="WOrderDate" SortExpression="WOrderDate" DataFormatString="{0:dd MMM yyyy}" ItemStyle-Width="8em"/>--%>
                                            <asp:BoundField HeaderText="Due Date" DataField="DueDate" SortExpression="DueDate"  DataFormatString="{0:dd MMM yyyy}" ItemStyle-Width="8em" />
                                            <asp:TemplateField HeaderText="Status" SortExpression="Status" ItemStyle-Width="8em">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblStatus" runat="server" Text='<%# Eval("Status") %>' ></asp:Label>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Active" SortExpression="Active" ItemStyle-Width="3em">
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="chkActive" runat="server" Text=" " Checked='<%# Eval("Active") %>' Enabled="false" />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField ItemStyle-Width="3em">
                                                <ItemTemplate>
                                                    <asp:LinkButton ID="lbtnDelete" CommandArgument='<%# Eval("ID") %>' runat="server" CssClass="icon solid fa-ban df-link-danger" OnClick="lbtnDelete_Click" ToolTip="Delete works Order">
                                                    </asp:LinkButton>
                                                    <cci:ConfirmButtonExtender ID="lbtnDelete_ConfirmButtonExtender1" runat="server" ConfirmText="Delete this Works Order? Are you sure" Enabled="True" TargetControlID="lbtnDelete">
                                                    </cci:ConfirmButtonExtender>
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
</body>
</html>
