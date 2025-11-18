<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ProductionRMD.aspx.cs" Inherits="SBMS.ProductionRMD" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Raw Materials Demands</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
        <%--<link href="assets/css/main.css" rel="stylesheet" />--%>
           <link rel="stylesheet" href="prologue/assets/css/main.css" />
        <noscript><link rel="stylesheet" href="assets/css/noscript.css" /></noscript>
        <script  type="text/javascript" src="lib/jquery/dist/jquery.js"></script>
        <script  type="text/javascript" src="lib/jquery/dist/jquery.min.js"></script>
        <script  type="text/javascript" src="lib/jqueryui/jquery-ui.min.js"></script>
        <script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/PapaParse/4.1.2/papaparse.min.js"></script>

         <script  type="text/javascript" src="https://cdn.plot.ly/plotly-basic-latest.min.js"></script>
         <script  type="text/javascript" src="pivot/pivot.js"></script>
        <script  type="text/javascript" src="pivot/plotly_renderers.js"></script>
         <link href="pivot/pivot.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
      <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
              	<div id="header" >
                    <div class="top">
                        <!-- Logo -->
                         <div id="logo" >
                          <asp:Label ID="lblUsername" runat="server" Text="" Font-Size="Small" style="padding-top:0em; float:left"></asp:Label>   
                                <asp:LinkButton ID="lbtnLogOut" runat="server" style="font-size:0.9em; float:right" OnClick="lbtnLogOut_Click" CssClass="icon fa-door-open"  > Log Out</asp:LinkButton>         
                         </div>
                        <!-- Nav -->
                         <nav id="nav" >
                          <ul>
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
                                   <asp:LinkButton ID="lbtnRefresh" runat="server" CssClass="buttonC fa fa-registered" OnClick="lbtnRefresh_Click" ToolTip="Refresh Items opening balances with Sage" style="float:left">&nbsp;Refresh&nbsp;&nbsp;</asp:LinkButton>
                                    <asp:LinkButton ID="lbtnDownload" runat="server" class="buttonC fa fa-download" OnClick="lbtnDownload_Click" ToolTip="Download and analyse data using an excel pivot table." style="float:right; font-size:1em">&nbsp;Pivot In Excel</asp:LinkButton>
                                   <h3 style="padding-top:2em; line-height:1em">Raw Materials Demands</h3>
                               </div>
                               </div>                                
                        <div class="row 150%">
                            <div class="col-12 col-12-wide">    
                                <div id="pivotoutput"></div>
                            </div>
                        </div>
                        <asp:Literal ID="Literal1" runat="server"></asp:Literal>
                    </div>
        </div>
         </div>
    </form>
</body>
</html>
