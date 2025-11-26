<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="StockMovement.aspx.cs" Inherits="SBMS.StockMovement" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Stock Movement</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
   <%-- <link rel="stylesheet" href="assets/css/main.css" />--%>
    <link rel="stylesheet" href="prologue/assets/css/main.css" />
</head>
<body>
    <form id="form1" runat="server">
         <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
          <div id="header" >
             <div class="top">
                 <!-- Logo -->
	                    <div id="logo" >
		                    <asp:Label ID="lblUsername" runat="server" Text="" Font-Size="Small" style="padding-top:0em; float:left"></asp:Label>
                            <asp:LinkButton ID="lbtnLogOut" runat="server" style="font-size:0.9em; float:right" OnClick="lbtnLogOut_Click"  > Log Out</asp:LinkButton>
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
                              <asp:LinkButton ID="lbtnDownload" runat="server" class="buttonC fa fa-download" OnClick="lbtnDownload_Click" ToolTip="Download and analyse data using excel." style="float:right; font-size:1em">&nbsp;Export to Excel</asp:LinkButton>
                              <h3 style="padding-top:2em; line-height:1em">Stock/Lot Movement</h3>
                          </div>
                  </div>  
                <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                    <ContentTemplate>
                        <div class="row 150%">
                            <div class="col-12 col-12-wide">
                                <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnsearch" style="text-align:center">
                                  <table>
                                      <tr>
                                          <td>Select Item  <asp:DropDownList ID="dditem" runat="server" AutoPostBack="true" OnSelectedIndexChanged="dditem_SelectedIndexChanged" CssClass="optiondd" style="width:20em"></asp:DropDownList></td>
                                          <td> <asp:Panel ID="PnlLotNum" runat="server">
                                             Select LotNumber  <asp:DropDownList ID="ddLotNum" runat="server" style="min-width:10em" CssClass="optiondd"></asp:DropDownList>
                                         </asp:Panel></td>
                                          <td>Store <asp:DropDownList ID="ddStore" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddStore_SelectedIndexChanged" CssClass="optiondd"></asp:DropDownList></td>
                                          <td> Top # of Records</span>
                                                <asp:DropDownList ID="ddRecCount" runat="server" style="width:4em" CssClass="optiondd">
                                               <asp:ListItem Value="50">50</asp:ListItem>
                                                <asp:ListItem Value="100">100</asp:ListItem>
                                                <asp:ListItem Value="1000">1000</asp:ListItem>        
                                            </asp:DropDownList></td>
                                          <td><br /><asp:LinkButton ID="lbtnsearch" runat="server" CssClass="buttonC fa fa-search" OnClick="lbtnsearch_Click" style="margin:auto; font-size:1em">&nbsp;Search</asp:LinkButton></td>
                                      </tr>
                                  </table>
                                     </asp:Panel>
                                </div>
                            </div>
                        <div class="row 150%">
                            <div class="col-12 col-12-wide">
                                <asp:GridView ID="GridItemTrans" runat="server" AutoGenerateColumns="false" CssClass="gridview"  ShowHeaderWhenEmpty ="true" ShowFooter="true" OnRowDataBound="GridItemTrans_RowDataBound" >
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                      <Columns>
                                        <asp:BoundField DataField="TransactionType" HeaderText="Type" ReadOnly="True" />
                                        <asp:BoundField DataField="Document" ReadOnly="True" HeaderText="Document" />
                                        <asp:BoundField DataField="ItemDescription" ReadOnly="True" HeaderText="Item" />
                                        <asp:BoundField DataField="LotNumber" ReadOnly="True" HeaderText="Lot Number"/>
                                        <asp:BoundField DataField="Qty" ReadOnly="True" HeaderText="Qty" />
                                        <asp:BoundField DataField="PriceExclusive" ReadOnly="True" HeaderText="Unit Excl" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="5em" DataFormatString="{0:N2}" />
                                        <asp:BoundField DataField="AdditionalCosts" ReadOnly="True" HeaderText="Add Costs" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" FooterStyle-HorizontalAlign="Right" ItemStyle-Width="7em" DataFormatString="{0:N2}" />
                                        <asp:BoundField DataField="TotalLineValExcl" ReadOnly="True" HeaderText="Line Value" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" FooterStyle-HorizontalAlign="Right" ItemStyle-Width="7em" DataFormatString="{0:N2}" />
                                        <asp:BoundField DataField="ExchRate" ReadOnly="True" HeaderText="Exch_Rate" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" FooterStyle-HorizontalAlign="Right" ItemStyle-Width="4em" DataFormatString="{0:N2}" />
                                          <asp:BoundField DataField="Store" ReadOnly="True" HeaderText="Store" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center"  />
                                         <asp:BoundField DataField="TransactionDate" ReadOnly="True" HeaderText="Date" DataFormatString="{0:dd MMM}" />
                                         <asp:BoundField DataField="ByRole" ReadOnly="True" HeaderText="By"/>
                                         <asp:BoundField DataField="TransactionReference" ReadOnly="True" HeaderText="Reference" />       
                                    </Columns>
                                </asp:GridView>

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
