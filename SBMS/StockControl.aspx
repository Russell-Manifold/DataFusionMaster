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
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js"></script>
    <script type="text/javascript" src="js/pickslipkanbanscript.js"></script>
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
                        <div class="row 150%">
                            <div class="col-6 col-12-wide" style="text-align: center;">
                                <div>
                                     <h4 style="text-align:center">Stock Tracking</h4>
                                        <asp:LinkButton ID="LinkButton1" runat="server" PostBackUrl="~/PurchaseOrdersIncomplete.aspx"   CssClass="button buttonLarge" ToolTip="View SBCA Purchase Orders with lines " >Purchase Orders By Lines</asp:LinkButton><br /><br />
                                        <asp:LinkButton ID="LinkButton3" runat="server" PostBackUrl="~/OSPurchaseOrdersPartial.aspx"   CssClass="button buttonLarge" ToolTip="View partially received Purchase Orders" >Partially Received Purchase Orders</asp:LinkButton><br /><br />   
                                        <asp:LinkButton ID="LinkButton2" runat="server" PostBackUrl="~/SalesOrdersIncomplete.aspx" CssClass="button buttonLarge" ToolTip="View and logs of items and lot number transactions." >Incomplete Sales Orders</asp:LinkButton><br /><br />
                                    <hr />
                                         <h4 style="text-align:center">Movement</h4>
                                        <asp:LinkButton ID="imgbTrf" runat="server" OnClick="imgbTrf_Click" CssClass="button buttonLarge" ToolTip="Carry out an inter-store transfer" >Quick Inter-Store Transfer</asp:LinkButton><br /><br />
                                        <asp:LinkButton ID="imgbTrfB" runat="server" OnClick="imgbTrfB_Click" CssClass="button buttonLarge" ToolTip="Carry out an inter-store transfer" >Bulk Item Transfer</asp:LinkButton><br /><br />
                                        <asp:LinkButton ID="imgItemAdjust" runat="server" OnClick="imgItemAdjust_Click" CssClass="button buttonLarge" ToolTip="Carry out an item adjustment with the option of updating Sage Accounting" >Item Adjustment</asp:LinkButton><br /><br />
                                        <asp:LinkButton ID="imgItemConvert" runat="server" OnClick="imgItemConvert_Click" CssClass="button buttonLarge" ToolTip="Convert an items to a different unit of measure" >Convert Item to Different Item Code</asp:LinkButton><br /><br />
                                    <hr />
                                     <h4 style="text-align:center">Counts</h4>
                                    <asp:LinkButton ID="lbtnStckCount" runat="server" CssClass="button buttonLarge" OnClick="lbtnStckCount_Click" ToolTip="Plan and record stock takes." >Stock Counts</asp:LinkButton><br /><br />   
                                    
                                    <hr />
                                         <h4 style="text-align:center">Custom Reports</h4>
                                        <asp:LinkButton ID="lbtnCustom" runat="server" CssClass="button buttonLarge" OnClick="lbtnCustom_Click"  ToolTip="View custom reports created for you." >My Customised</asp:LinkButton><br /><br />           
                                </div>     
                            </div>
                                <div class="col-6 col-12-wide" style="text-align: center;">
                                    <h4 style="text-align:center">Analysis</h4>
                                    <asp:LinkButton ID="lbtnStockMove" runat="server" OnClick="lbtnStockMove_Click" CssClass="button buttonLarge" ToolTip="View and logs of items and lot number transactions." >Stock/Lot Movement</asp:LinkButton><br /><br />
                                    <asp:LinkButton ID="lbtnSOH" runat="server" OnClick="lbtnSOH_Click" CssClass="button buttonLarge" ToolTip="View stock balances by store"  >Stock/Lot Balances</asp:LinkButton><br /><br />
                                     <asp:LinkButton ID="lbtnPickGP" runat="server" OnClick="lbtnPickGP_Click" CssClass="button buttonLarge" ToolTip="Analyse picking slips and view GP per each one"  >Picking Slip GP Analysis</asp:LinkButton><br /><br />
                                     <asp:LinkButton ID="lbtnItemGP" runat="server" OnClick="lbtnItemGP_Click" CssClass="button buttonLarge" ToolTip="Analyse item sale and view GP per item"  >Item Sales GP Analysis</asp:LinkButton><br /><br />
                                </div>
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
