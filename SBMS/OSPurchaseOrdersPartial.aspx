<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="OSPurchaseOrdersPartial.aspx.cs" Inherits="SBMS.OSPurchaseOrdersPartial" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Partially Received</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="prologue/assets/css/main.css" />
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js"></script>
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
				               <%-- <li>______________</li>
                                <li style="text-align:left">Purchases Dashboard <br /> (Coming Soon)</li>--%>
                            </ul>
			            </nav>
	            </div>
            </div>
           <div id="main">
        <div class="content">
            <div class="container">
                            <div class="row 150%"> 
                                <div class="col-12 col-12-wide" style="text-align: center">
                                    <a href="https://mydatafusion.online" title="My Data Fusion website">
                                        <img src="images/Logo.png" style="border-radius: 0.25em; float: left" class="logoImg" /></a> 
                                    <asp:Image ID="imgCoImg" runat="server" Style="float: right" class="logoImg" />
                                     <asp:LinkButton ID="lbtnDownload" runat="server" class="buttonC fa fa-download" OnClick="lbtnDownload_Click" ToolTip="Download to excel." style="float:right; font-size:1em">&nbsp;Excel</asp:LinkButton>
                                    <h3 style="padding-top: 2em; line-height: 1em">Partially Received Purchase Orders<asp:Label ID="lblpoqty" runat="server" Text=""></asp:Label></h3>
                                </div>
                                </div>
                          <div class="row 150%">
                              <div class="col-12-narrow col-12-wide">
                                  <div style="display: none"><asp:Label ID="lblDir" runat="server" Text=""></asp:Label></div>
                                  <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnfind" Style="font-size: 1em; width: 100%">
                                  <table style="width: 100%">
                                      <tr>
                                          <td>Find (Supplier / PO Num) &nbsp;<asp:TextBox ID="txtfind" runat="server"></asp:TextBox></td>
                                          <td>Find (Code / Description) &nbsp;<asp:TextBox ID="txtfindItem" runat="server"></asp:TextBox><asp:LinkButton ID="lbtnfind" runat="server" CssClass="fa fa-search buttonC" OnClick="lbtnfind_Click"></asp:LinkButton></td>
                                           <td></td>
                                      </tr>
                                  </table>
                                  </asp:Panel>
                              </div>
                          </div>
                            <div class="row 150%">
                                <div class="col-12 col-12-wide">
                                    <asp:GridView ID="GridPOs" runat="server" AutoGenerateColumns="false" CssClass="gridview" OnRowDataBound="GridPOs_RowDataBound" OnSelectedIndexChanged="GridPOs_SelectedIndexChanged" AllowSorting="true" OnSorting="GridPOs_Sorting" AllowPaging="true" PageSize="50" OnPageIndexChanging="GridPOs_PageIndexChanging">
                                        <HeaderStyle CssClass="gridViewHeader" />
                                        <FooterStyle CssClass="gridViewHeader" />
                                        <RowStyle CssClass="gridViewRow" />
                                        <AlternatingRowStyle CssClass="gridViewAltRow" />
                                        <PagerStyle CssClass="gridViewPager" />
                                        <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="5" />
                                        <Columns>
                                            <asp:BoundField DataField="PODocID" ReadOnly="True" />
                                            <asp:BoundField HeaderText="PO Number" DataField="PONumber" ReadOnly="True" ItemStyle-Width="8em" SortExpression="PONumber" />
                                            <asp:BoundField HeaderText="Date" DataField="CreatedDate" ReadOnly="True" DataFormatString="{0:dd MMM yyyy}" ItemStyle-Width="8em" SortExpression="CreatedDate" />
                                            <asp:BoundField HeaderText="Supplier Name" DataField="Supplier" ReadOnly="True" SortExpression="Supplier" />
                                            <asp:BoundField HeaderText="Code" DataField="ItemCode" ReadOnly="True" SortExpression="ItemCode" />
                                            <asp:BoundField HeaderText="Description" DataField="ItemDescription" ReadOnly="True" SortExpression="ItemDescription" />
                                            <asp:BoundField HeaderText="Order Qty" DataField="OrigQty" ReadOnly="True" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="5em" SortExpression="OrigQty"  />
                                            <asp:BoundField HeaderText="Received" DataField="RecQty" ReadOnly="True" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="5em" SortExpression="RecQty"  />
                                            <asp:BoundField HeaderText="Balance" DataField="QtyLeft" ReadOnly="True" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="5em"  SortExpression="QtyLeft" />
                                        </Columns>
                                    </asp:GridView>
                                </div>
                            </div>
                            </div>
                         </div>
           </div>
    </form>
</body>
</html>
