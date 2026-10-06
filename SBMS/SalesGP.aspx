<%-- Sales GP Analysis: invoiced Sales Orders by order or by item, with the stock drawn
     underneath. Figures and their definitions are in Classes/SalesGP.cs. --%>
<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SalesGP.aspx.cs" Inherits="SBMS.SalesGPPage" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Sales GP Analysis</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="prologue/assets/css/main.css" />
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    <style>
        .sg-wrap   { max-width:1200px; margin:auto; text-align:left; }
        .sg-sec    { color:#e68a00; font-weight:600; margin:1.4em 0 .4em; border-bottom:1px solid #dfe6eb; padding-bottom:.2em; }
        .sg-form td { padding:.25em .6em .25em 0; vertical-align:middle; }
        .sg-form input[type=text] { height:2.5em; }
        /* Date pickers: same look as the rest of the form (aliceblue, rounded, blue focus), not the browser default. */
        .sg-form input[type=date] { -webkit-appearance:none; appearance:none; background:aliceblue; color:#333; border:1px solid #cfd8de; border-radius:.7em; padding:.3em .8em; height:2.5em; font:inherit; font-size:.95em; }
        .sg-form input[type=date]:focus { outline:none; border-color:#4282C1; box-shadow:0 0 0 2px rgba(66,130,193,.25); }
        .sg-form input[type=date]::-webkit-calendar-picker-indicator { cursor:pointer; opacity:.55; padding:.15em; border-radius:.3em; }
        .sg-form input[type=date]::-webkit-calendar-picker-indicator:hover { opacity:1; background:#dde9f5; }
        .sg-grid   { width:100%; border-collapse:collapse; font-size:.9em; }
        .sg-grid th { background:#f1f5f8; text-align:left; padding:.35em .5em; border-bottom:1px solid #cfd8de; }
        .sg-grid td { padding:.3em .5em; border-bottom:1px solid #e6ecf0; }
        .sg-num    { text-align:right !important; }
        .sg-hint   { font-size:.85em; color:#777; }
        .sg-view label { margin-right:1.2em; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div id="header">
            <div class="top">
                <div id="logo">
                    <asp:Label ID="lblUsername" runat="server" Text="" Font-Size="Small" style="padding-top:0em; float:left"></asp:Label>
                    <asp:LinkButton ID="lbtnLogOut" runat="server" style="font-size:0.9em; float:right" OnClick="lbtnLogOut_Click"> Log Out</asp:LinkButton>
                </div>
                <nav id="nav">
                    <ul>
                        <li><asp:LinkButton ID="lbtnDash" runat="server" CssClass="buttonM" OnClick="lbtnDash_Click" ToolTip="Return to main dashboard">Dashboard</asp:LinkButton></li>
                        <li><asp:LinkButton ID="lbtnStockControl" runat="server" CssClass="buttonM" OnClick="lbtnStockControl_Click" ToolTip="Back to Stock Control">Stock Control</asp:LinkButton></li>
                    </ul>
                </nav>
            </div>
        </div>
        <div id="main">
            <div class="content">
                <div class="container">
                    <div class="row 150%">
                        <div class="col-12 col-12-wide" style="text-align:center">
                            <a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/Logo.png" style="border-radius:0.25em; float:left" class="logoImg" /></a>
                            <asp:Image ID="imgCoImg" runat="server" style="float:right" class="logoImg" />
                            <h3 style="padding-top:2em; line-height:1em">Sales GP Analysis</h3>
                        </div>
                    </div>

                    <div class="sg-wrap">
                        <table class="sg-form">
                            <tr>
                                <td>View</td>
                                <td>
                                    <asp:RadioButtonList ID="rblView" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" CssClass="sg-view" AutoPostBack="true" OnSelectedIndexChanged="rblView_SelectedIndexChanged">
                                        <asp:ListItem Value="orders" Selected="True">By Sales Order</asp:ListItem>
                                        <asp:ListItem Value="items">By Item</asp:ListItem>
                                    </asp:RadioButtonList>
                                </td>
                            </tr>
                            <tr>
                                <td>Invoiced orders completed from</td>
                                <td><asp:TextBox ID="txtFrom" runat="server" TextMode="Date" Width="160px"></asp:TextBox></td>
                                <td>to</td>
                                <td><asp:TextBox ID="txtTo" runat="server" TextMode="Date" Width="160px"></asp:TextBox></td>
                                <td>Search</td>
                                <td><asp:TextBox ID="txtSearch" runat="server" Width="220px" MaxLength="100" placeholder="Order, customer, item" ToolTip="Any part of the sales order number, customer name, item code or description"></asp:TextBox></td>
                                <td><asp:LinkButton ID="lbtnShow" runat="server" CssClass="fa fa-search buttonC" OnClick="lbtnShow_Click"> Show</asp:LinkButton></td>
                            </tr>
                        </table>
                        <span class="sg-hint">Only Sales Orders that are complete and invoiced. Revenue is the order's lines excl VAT after discount. Cost is everything its picking slips or job card drew from stock, at the cost it left the store at. Materials only.</span>

                        <%-- ═════════ By Sales Order ═════════ --%>
                        <asp:Panel ID="pnlOrders" runat="server">
                            <div class="sg-sec">Sales Orders</div>
                            <asp:GridView ID="GridOrders" runat="server" AutoGenerateColumns="false" CssClass="sg-grid" GridLines="None" DataKeyNames="DocID"
                                OnRowCommand="GridOrders_RowCommand" ShowFooter="true" EmptyDataText="No invoiced Sales Orders in this period.">
                                <Columns>
                                    <asp:BoundField DataField="CompleteDate" HeaderText="Completed" DataFormatString="{0:dd MMM yyyy}" />
                                    <asp:BoundField DataField="SONumber" HeaderText="Sales order" />
                                    <asp:BoundField DataField="Customer" HeaderText="Customer" />
                                    <asp:BoundField DataField="Route" HeaderText="Via" />
                                    <asp:BoundField DataField="Revenue" HeaderText="Revenue" DataFormatString="{0:N2}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" FooterStyle-CssClass="sg-num" />
                                    <asp:BoundField DataField="Cost" HeaderText="Cost" DataFormatString="{0:N2}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" FooterStyle-CssClass="sg-num" />
                                    <asp:BoundField DataField="GP" HeaderText="GP" DataFormatString="{0:N2}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" FooterStyle-CssClass="sg-num" />
                                    <asp:BoundField DataField="Margin" HeaderText="Margin" DataFormatString="{0:P1}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" FooterStyle-CssClass="sg-num" />
                                    <asp:TemplateField HeaderText="">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="lbtnDetail" runat="server" CommandName="Detail" CommandArgument='<%# Eval("DocID") %>'>Detail</asp:LinkButton>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>

                            <asp:Panel ID="pnlOrderDetail" runat="server" Visible="false">
                                <div class="sg-sec"><asp:Label ID="lblOrderTitle" runat="server" Text=""></asp:Label></div>
                                <asp:GridView ID="GridOrderLines" runat="server" AutoGenerateColumns="false" CssClass="sg-grid" GridLines="None" EmptyDataText="No lines.">
                                    <Columns>
                                        <asp:BoundField DataField="ItemCode" HeaderText="Code" />
                                        <asp:BoundField DataField="ItemDescription" HeaderText="Item" />
                                        <asp:BoundField DataField="Qty" HeaderText="Qty" DataFormatString="{0:0.####}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" />
                                        <asp:BoundField DataField="Revenue" HeaderText="Revenue" DataFormatString="{0:N2}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" />
                                    </Columns>
                                </asp:GridView>
                                <br />
                                <asp:GridView ID="GridSlips" runat="server" AutoGenerateColumns="false" CssClass="sg-grid" GridLines="None" EmptyDataText="No picking slip or job card drew stock for this order.">
                                    <Columns>
                                        <asp:BoundField DataField="Slip" HeaderText="Picking slip / job card" />
                                        <asp:BoundField DataField="CompleteDate" HeaderText="Completed" DataFormatString="{0:dd MMM yyyy}" />
                                        <asp:BoundField DataField="Cost" HeaderText="Cost drawn" DataFormatString="{0:N2}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" />
                                    </Columns>
                                </asp:GridView>
                            </asp:Panel>
                        </asp:Panel>

                        <%-- ═════════ By Item ═════════ --%>
                        <asp:Panel ID="pnlItems" runat="server" Visible="false">
                            <div class="sg-sec">Items</div>
                            <asp:GridView ID="GridItems" runat="server" AutoGenerateColumns="false" CssClass="sg-grid" GridLines="None" DataKeyNames="ItemID"
                                OnRowCommand="GridItems_RowCommand" ShowFooter="true" EmptyDataText="Nothing sold in this period.">
                                <Columns>
                                    <asp:BoundField DataField="ItemCode" HeaderText="Code" />
                                    <asp:BoundField DataField="ItemDescription" HeaderText="Item" />
                                    <asp:BoundField DataField="Qty" HeaderText="Qty sold" DataFormatString="{0:0.####}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" />
                                    <asp:BoundField DataField="Revenue" HeaderText="Revenue" DataFormatString="{0:N2}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" FooterStyle-CssClass="sg-num" />
                                    <asp:BoundField DataField="Cost" HeaderText="Cost" DataFormatString="{0:N2}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" FooterStyle-CssClass="sg-num" />
                                    <asp:BoundField DataField="GP" HeaderText="GP" DataFormatString="{0:N2}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" FooterStyle-CssClass="sg-num" />
                                    <asp:BoundField DataField="Margin" HeaderText="Margin" DataFormatString="{0:P1}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" FooterStyle-CssClass="sg-num" />
                                    <asp:TemplateField HeaderText="">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="lbtnCust" runat="server" CommandName="Customers" CommandArgument='<%# Eval("ItemID") %>'>Customers</asp:LinkButton>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>

                            <asp:Panel ID="pnlItemDetail" runat="server" Visible="false">
                                <div class="sg-sec"><asp:Label ID="lblItemTitle" runat="server" Text=""></asp:Label></div>
                                <asp:GridView ID="GridItemCustomers" runat="server" AutoGenerateColumns="false" CssClass="sg-grid" GridLines="None" EmptyDataText="No invoiced sales of this item in the period (cost only).">
                                    <Columns>
                                        <asp:BoundField DataField="Customer" HeaderText="Customer" />
                                        <asp:BoundField DataField="Orders" HeaderText="Orders" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" />
                                        <asp:BoundField DataField="Qty" HeaderText="Qty" DataFormatString="{0:0.####}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" />
                                        <asp:BoundField DataField="Revenue" HeaderText="Revenue" DataFormatString="{0:N2}" ItemStyle-CssClass="sg-num" HeaderStyle-CssClass="sg-num" />
                                    </Columns>
                                </asp:GridView>
                            </asp:Panel>
                        </asp:Panel>
                    </div>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
