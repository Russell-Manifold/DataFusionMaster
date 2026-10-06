<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="StockControl.aspx.cs" Inherits="SBMS.StockControl" %>
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
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js"></script>
    <script type="text/javascript" src="js/pickslipkanbanscript.js"></script>
    <style>
        .sc-wrap  { max-width:1180px; margin:0 auto 2em; text-align:left; }
        .sc-h     { text-align:left; font-size:1.05em; color:#4282C1; margin:1.6em 0 .6em; padding-bottom:.25em; border-bottom:1px solid #dfe6eb; }
        .sc-grid  { display:grid; grid-template-columns:repeat(auto-fill, minmax(250px, 1fr)); gap:.9em; }
        .sc-tile  { display:grid; grid-template-columns:auto 1fr; column-gap:.9em; align-items:center; min-height:4.6em; padding:.8em 1em; border-radius:.6em;
                    background:linear-gradient(135deg, #2f4a5c 0%, #1f3443 100%); color:#fff !important; text-decoration:none !important;
                    box-shadow:0 2px 6px rgba(0,0,0,.18); border:0; line-height:1.3; }
        .sc-tile:hover { background:linear-gradient(135deg, #3a5b70 0%, #27404f 100%); transform:translateY(-1px); box-shadow:0 4px 10px rgba(0,0,0,.22); }
        .sc-i     { grid-row:1 / span 2; font-size:1.5em; width:1.4em; text-align:center; color:#9fc3e2; }
        .sc-t     { display:block; font-weight:600; font-size:.98em; align-self:end; }
        .sc-d     { display:block; font-size:.78em; color:#c9d6e0; margin-top:.15em; align-self:start; }
        @media (max-width: 640px) { .sc-grid { grid-template-columns:1fr; } }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true"></asp:ScriptManager>
  <div id="header" >
     <div class="top">
         <!-- Logo -->
	            <div id="logo" >
		            <asp:Label ID="lblUsername" runat="server" Text="" Font-Size="Small" style="padding-top:0em; float:left"></asp:Label>
                    <asp:LinkButton ID="lbtnLogOut" runat="server" style="font-size:0.9em;float:right" OnClick="lbtnLogOut_Click"  > Log Out</asp:LinkButton>
	            </div>

         <!-- Nav -->
	            <nav id="nav" >
		            <ul >
                        <li><asp:LinkButton ID="imgdash" runat="server" CssClass="buttonM" OnClick="imgdash_Click" ToolTip="Return to main dashboard">Dashboard</asp:LinkButton></li>
                        <li><asp:LinkButton ID="imgbRec" runat="server" CssClass="buttonM" OnClick="imgbRec_Click" ToolTip="Receive from Purchase Orders, allocate lot numbers">Purchase Orders</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnPickSlips" runat="server" CssClass="buttonM" OnClick="ibtnPickSlips_Click" ToolTip="View Sales Orders and picking slips, fulfill orders" >Sales Orders</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnPickTrack" runat="server" CssClass="buttonM" OnClick="ibtnPickTrack_Click" ToolTip="Track all picking slips in a simple drag and drop process " >Picking Slip Tracking</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnStckCtl" runat="server" CssClass="buttonM" ToolTip="Inter Store Transfers, Stock adjustments, Stock Takes, Reporting" OnClick="ibtnStckCtl_Click" >Stock Control</asp:LinkButton></li>
                        <li>______________</li>
	       
                        <li><asp:LinkButton ID="ibtnFCasts" runat="server" CssClass="buttonM" OnClick="ibtnFCasts_Click" ToolTip="Ensure greater stock accuracy with sales forecasting"> Sales Forecasts</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnmrp" runat="server" CssClass="buttonM" OnClick="ibtnmrp_Click" ToolTip="View finished good demands based on PO's, Sales Orders, Forecasts etc" > Finished Goods Demands</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnJobTrack" runat="server" CssClass="buttonM" OnClick="ibtnJobTrack_Click" ToolTip="Track all Job cards, simply drag and drop their change in status"> Job Card Tracking</asp:LinkButton></li>
                        <li>______________</li>
                        <li><asp:LinkButton ID="ibtmWorksOrders" runat="server" CssClass="buttonM" OnClick="ibtmWorksOrders_Click" ToolTip="Create and view works orders for manufacturing or production" > Works Orders</asp:LinkButton></li>		
                        <li><asp:LinkButton ID="ibtmWOrdMgment" runat="server" CssClass="buttonM" OnClick="ibtmWOrdMgment_Click" ToolTip="Allocate raw materials to works orders and update stock levels on order completion." > Works Order Fulfillment</asp:LinkButton></li>		
                        <li><asp:LinkButton ID="ibtnRMD" runat="server" CssClass="buttonM" OnClick="ibtnRMD_Click" ToolTip="View RMD based on PO's, Sales orders, Works Orders." > Raw Materials Demands</asp:LinkButton></li>
                        <li>______________</li>
                        <li><a href="https://mydatafusion.online/learningCenter.aspx" class="buttonM icon fa-lightbulb" target="_blank"> Learning Hub >></a></li>
                    <%-- <li>______________</li>
                        <li style="text-align:left">Item Movement Dashboard <br /> (Coming Soon)</li>--%>
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
                            <asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" />
                            <h3 style="padding-top:2em; line-height:1em">Stock Control</h3>
                        </div>
                     </div>
                <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <%-- Stock Control menu: tiles grouped by task (was one tall stack of buttons per column).
                             Every control keeps its ID and handler; only the presentation changed. --%>
                        <div class="sc-wrap">
                            <h4 class="sc-h">Stock Tracking</h4>
                            <div class="sc-grid">
                                <asp:LinkButton ID="LinkButton1" runat="server" PostBackUrl="~/PurchaseOrdersIncomplete.aspx" CssClass="sc-tile" ToolTip="View SBCA Purchase Orders with lines">
                                    <i class="fa fa-list-alt sc-i"></i><span class="sc-t">Purchase Orders By Lines</span><span class="sc-d">Every open purchase order, line by line</span>
                                </asp:LinkButton>
                                <asp:LinkButton ID="LinkButton3" runat="server" PostBackUrl="~/OSPurchaseOrdersPartial.aspx" CssClass="sc-tile" ToolTip="View partially received Purchase Orders">
                                    <i class="fa fa-truck sc-i"></i><span class="sc-t">Partially Received POs</span><span class="sc-d">Deliveries still outstanding</span>
                                </asp:LinkButton>
                                <asp:LinkButton ID="LinkButton2" runat="server" PostBackUrl="~/SalesOrdersIncomplete.aspx" CssClass="sc-tile" ToolTip="Sales orders not yet fully invoiced">
                                    <i class="fa fa-file-alt sc-i"></i><span class="sc-t">Incomplete Sales Orders</span><span class="sc-d">Ordered, invoiced and outstanding per line</span>
                                </asp:LinkButton>
                            </div>
                            <h4 class="sc-h">Movement</h4>
                            <div class="sc-grid">
                                <asp:LinkButton ID="imgbTrf" runat="server" OnClick="imgbTrf_Click" CssClass="sc-tile" ToolTip="Carry out an inter-store transfer of a single item">
                                    <i class="fa fa-exchange-alt sc-i"></i><span class="sc-t">Quick Item Transfer</span><span class="sc-d">One item, store to store</span>
                                </asp:LinkButton>
                                <asp:LinkButton ID="imgbTrfB" runat="server" OnClick="imgbTrfB_Click" CssClass="sc-tile" ToolTip="Carry out an inter-store transfer">
                                    <i class="fa fa-cubes sc-i"></i><span class="sc-t">Bulk Item Transfer</span><span class="sc-d">Several items on one transfer slip</span>
                                </asp:LinkButton>
                                <asp:LinkButton ID="imgItemAdjust" runat="server" OnClick="imgItemAdjust_Click" CssClass="sc-tile" ToolTip="Carry out an item adjustment with the option of updating Sage Accounting">
                                    <i class="fa fa-sliders-h sc-i"></i><span class="sc-t">Item Adjustment</span><span class="sc-d">Add or remove stock, optionally in Sage too</span>
                                </asp:LinkButton>
                                <a href="Returnables.aspx" class="sc-tile" title="No-charge items that are expected back (pallets, crates, cylinders): log returns against a customer and see who is holding what. Not a credit note.">
                                    <i class="fa fa-undo sc-i"></i><span class="sc-t">Returnables</span><span class="sc-d">Pallets and crates: returns and who holds what</span>
                                </a>
                                <asp:LinkButton ID="imgItemConvert" runat="server" OnClick="imgItemConvert_Click" CssClass="sc-tile" ToolTip="Convert an item to a different item code or unit of measure">
                                    <i class="fa fa-random sc-i"></i><span class="sc-t">Convert Item Code</span><span class="sc-d">Re-code stock or change its unit</span>
                                </asp:LinkButton>
                            </div>
                            <h4 class="sc-h">Analysis</h4>
                            <div class="sc-grid">
                                <asp:LinkButton ID="lbtnStockMove" runat="server" OnClick="lbtnStockMove_Click" CssClass="sc-tile" ToolTip="View and logs of items and lot number transactions.">
                                    <i class="fa fa-chart-line sc-i"></i><span class="sc-t">Stock/Lot Movement</span><span class="sc-d">Every movement of an item or lot</span>
                                </asp:LinkButton>
                                <asp:LinkButton ID="lbtnSOH" runat="server" OnClick="lbtnSOH_Click" CssClass="sc-tile" ToolTip="View stock balances by store">
                                    <i class="fa fa-balance-scale sc-i"></i><span class="sc-t">Stock/Lot Balances</span><span class="sc-d">On hand by store and lot</span>
                                </asp:LinkButton>
                                <asp:LinkButton ID="lbtnReOrder" runat="server" OnClick="lbtnReOrder_Click" CssClass="sc-tile" ToolTip="What to buy: on hand, on order, committed and recommended order quantity per item.">
                                    <i class="fa fa-shopping-cart sc-i"></i><span class="sc-t">Re-Order Report</span><span class="sc-d">What to buy, and how much</span>
                                </asp:LinkButton>
                                <asp:LinkButton ID="lbtnTrace" runat="server" OnClick="lbtnTrace_Click" CssClass="sc-tile" ToolTip="Search a serial or batch: where it came from, where it went, and who has it.">
                                    <i class="fa fa-search sc-i"></i><span class="sc-t">Traceability &amp; Recall</span><span class="sc-d">Follow a serial or batch end to end</span>
                                </asp:LinkButton>
                                <asp:LinkButton ID="lbtnExpiry" runat="server" OnClick="lbtnExpiry_Click" CssClass="sc-tile" ToolTip="Expired and short-dated stock still on hand.">
                                    <i class="fa fa-calendar-times sc-i"></i><span class="sc-t">Expiry Control</span><span class="sc-d">Expired and short-dated stock</span>
                                </asp:LinkButton>
                                <asp:LinkButton ID="lbtnPickGP" runat="server" OnClick="lbtnPickGP_Click" CssClass="sc-tile" ToolTip="Revenue, cost and GP per invoiced sales order, or per item.">
                                    <i class="fa fa-chart-pie sc-i"></i><span class="sc-t">Sales GP Analysis</span><span class="sc-d">By sales order or by item</span>
                                </asp:LinkButton>
                                <a href="JobCardGP.aspx" class="sc-tile" title="Revenue, cost and GP per completed job card, including stock drawn that the customer did not see">
                                    <i class="fa fa-wrench sc-i"></i><span class="sc-t">Job Card GP Analysis</span><span class="sc-d">Per job, including internal materials</span>
                                </a>
                            </div>
                            <h4 class="sc-h">Counts &amp; Reports</h4>
                            <div class="sc-grid">
                                <asp:LinkButton ID="lbtnStckCount" runat="server" OnClick="lbtnStckCount_Click" CssClass="sc-tile" ToolTip="Plan and record stock takes.">
                                    <i class="fa fa-clipboard-check sc-i"></i><span class="sc-t">Stock Counts</span><span class="sc-d">Plan and record stock takes</span>
                                </asp:LinkButton>
                                <asp:LinkButton ID="lbtnCustom" runat="server" OnClick="lbtnCustom_Click" CssClass="sc-tile" ToolTip="View custom reports created for you.">
                                    <i class="fa fa-star sc-i"></i><span class="sc-t">My Customised</span><span class="sc-d">Reports built for your company</span>
                                </asp:LinkButton>
                            </div>
                        </div>
                      </ContentTemplate>
                </asp:UpdatePanel>              
            </div>
        </div>
     </div>
  </form>
</body>
</html>
