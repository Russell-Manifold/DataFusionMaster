<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="StockCounts.aspx.cs" Inherits="SBMS.StockCounts" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Stock Counts</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="prologue/assets/css/main.css" />
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
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
		            </ul>
	            </nav>
     </div>
 </div>
<div id="main"> 
        <div class="content">
            <div class="container">             
                 <div class="row 150%">
                     <div class="col-12 col-12-wide" style="text-align: center">
                                <a href="https://mydatafusion.online" title="My Data Fusion website">   <img src="images/Logo.png" style="border-radius: 0.25em; float: left" class="logoImg" /></a>
                                <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC fa fa-home" onclick="lbtnHome_Click" style="float:left">&nbsp;</asp:LinkButton>
                                    <asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" />
                         <div style="float:left; margin-left:2em">
                                Options: <asp:DropDownList ID="DDSelect" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDSelect_SelectedIndexChanged" >
                                    <asp:ListItem>- Select - </asp:ListItem>
                                    <asp:ListItem Value="0">Create New</asp:ListItem>
                                    <asp:ListItem Value="1">Edit</asp:ListItem>
                                    <asp:ListItem Value="2">Download Count worksheets</asp:ListItem>
                                    <asp:ListItem Value="3">Upload Counted quantities</asp:ListItem>
                                    <asp:ListItem Value="4">Combined Stores Variances</asp:ListItem>
                                    <asp:ListItem Value="5">Close Off</asp:ListItem>
                                    </asp:DropDownList>
                             
                            </div>
                         <h3>Stock Counts</h3>
                         </div>
                 </div>  
                <div class="row 150%">
                       <div class="col-12 col-12-wide">                         
                            <table>
                                    <tr>
                                         <td>Count: </td>
                                        <td><asp:DropDownList ID="DDStckCount" runat="server" style="width:10em" AutoPostBack="true" OnSelectedIndexChanged="DDStckCount_SelectedIndexChanged"></asp:DropDownList></td>
                                        <td>Ref:</td>
                                        <td><asp:Label ID="lblRef" runat="server" Text=""></asp:Label>&nbsp;</td>
                                        <td>&nbsp;Created Date</td>
                                        <td><asp:Label ID="lblDate" runat="server" Text=""></asp:Label>&nbsp;</td>
                                        <td>Created By<asp:Label ID="CntID" runat="server" Text="" style="display:none"></asp:Label></td>
                                        <td><asp:Label ID="lblCreatedBy" runat="server" Text=""></asp:Label></td>  
                                    </tr>
                                    <tr>
                                        <td colspan="8"><hr /></td>
                                    </tr>
                                <tr>
                                     <td>&nbsp;Category</td>
                                        <td style="text-align:left"> <asp:DropDownList ID="DDCateg" runat="server" style="width:10em" AutoPostBack="true" OnSelectedIndexChanged="DDCateg_SelectedIndexChanged"></asp:DropDownList></td>
                                        <td>Filter</td>
                                        <td>
                                            <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnSearch">
                                                 <asp:TextBox ID="txtFilter" runat="server" style="width:15em" placeholder="Code/Description"></asp:TextBox><asp:LinkButton ID="lbtnSearch" runat="server" CssClass="fa fa-search-plus buttonRed" ToolTip="Search">&nbsp;</asp:LinkButton>
                                            </asp:Panel>
                                           </td>
                                        <td>Store</td>
                                        <td><asp:DropDownList ID="DDStore" runat="server" style="width:10em;" AutoPostBack="true" OnSelectedIndexChanged="DDStore_SelectedIndexChanged"></asp:DropDownList></td>
                                    <td></td>
                                    </tr>    
                                </table>
                            <hr />
                                 
                            <asp:GridView ID="GridCntLines" runat="server" AutoGenerateColumns="false" CssClass="gridview" AllowSorting="true" OnSorting="GridCntLines_Sorting" OnRowDataBound="GridCntLines_RowDataBound" >
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="5" />
                                    <Columns>
                                        <asp:BoundField DataField="CtLineID" ReadOnly="True" />      
                                        <asp:BoundField DataField="CategoryDescript" ReadOnly="True" HeaderText="Category" SortExpression="CategoryDescript" />
                                        <asp:BoundField DataField="ItemCode" ReadOnly="True" HeaderText="Code" SortExpression="ItemCode" />
                                        <asp:BoundField DataField="ItemDescription" ReadOnly="True" HeaderText="Item"  SortExpression="ItemCode"/>
                                        <asp:BoundField DataField="StoreCode" ReadOnly="True" HeaderText="Store"  SortExpression="StoreCode"/>
                                        <asp:BoundField DataField="QtyOnHand" ReadOnly="True" HeaderText="On Hand"  SortExpression="ItemCode" />
                                        <asp:BoundField DataField="Count1Qty" ReadOnly="True" HeaderText="Count 1"  SortExpression="Count1Qty" />
                                        <asp:BoundField DataField="Count2Qty" ReadOnly="True" HeaderText="Count 2"  SortExpression="Count2Qty" />
                                        <asp:BoundField DataField="FinalQty" ReadOnly="True" HeaderText="Final Qty"  SortExpression="FinalQty" />                                      
                                    </Columns>
                                </asp:GridView>
                         </ div> 
                    </div>
                </div>
        </div>
    </div>
</form>
</body>
</html>
