<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="StockCounts.aspx.cs" Inherits="SBMS.StockCounts" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Stock Counts</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <%--<link rel="stylesheet" href="assets/css/main.css" />--%>
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
                                    <h3 style="padding-top:2em; line-height:1em">Stock Counts</h3>
                                </div>
                             </div>
                <div class="row 150%">
                    <div class="col-2 col-12-wide" style="margin-top:2em">
                        <asp:LinkButton ID="lbtnOpenNew" runat="server" style="float:right; font-size:1em;" CssClass="icon fa-edit buttonRed" ToolTip="Open New Stock Take" PostBackUrl="~/StockCountCreate.aspx">&nbsp;</asp:LinkButton><h3>Active Counts</h3>
                        <cci:ConfirmButtonExtender ID="lbtnOpenNew_ConfirmButtonExtender1" runat="server" ConfirmText="Open New Stock Take? Are You Sure?" Enabled="True" TargetControlID="lbtnOpenNew"></cci:ConfirmButtonExtender>
                        <asp:DropDownList ID="DDStckCount" runat="server" style="width:100%" AutoPostBack="true" OnSelectedIndexChanged="DDStckCount_SelectedIndexChanged">
                        </asp:DropDownList>
                    </div>
                        <div class="col-10 col-12-wide">
                          <h3 style="margin-top:1.2em">Details</h3>
                                <table>
                                    <tr>
                                        <td>Created Date</td>
                                        <td><asp:Label ID="lblDate" runat="server" Text=""></asp:Label>&nbsp;</td>
                                        <td>Created By<asp:Label ID="CntID" runat="server" Text="" style="display:none"></asp:Label></td>
                                        <td><asp:Label ID="lblCreatedBy" runat="server" Text=""></asp:Label></td>
                                        <td></td>
                                        <td></td>
                                    </tr>
                                    <tr>
                                        <td colspan="6"><hr /></td>
                                    </tr>
                                <tr>
                                        <td>Category</td>
                                        <td style="text-align:left"> <asp:DropDownList ID="DDCateg" runat="server" style="width:10em" AutoPostBack="true" OnSelectedIndexChanged="DDCateg_SelectedIndexChanged"></asp:DropDownList></td>
                                        <td style="padding-left:2em">Filter</td>
                                        <td>
                                            <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnSearch">
                                                 <asp:TextBox ID="txtFilter" runat="server" style="width:10em" placeholder="Code/Description"></asp:TextBox><asp:LinkButton ID="lbtnSearch" runat="server" CssClass="icon fa-search buttonC" ToolTip="Search"></asp:LinkButton>
                                            </asp:Panel>
                                           </td>
                                        <td style="padding-left:2em">Store</td>
                                        <td><asp:DropDownList ID="DDStore" runat="server" style="width:10em; float:left" AutoPostBack="true" OnSelectedIndexChanged="DDStore_SelectedIndexChanged"></asp:DropDownList></td>
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
                                        <asp:BoundField DataField="LotNumber" ReadOnly="True" HeaderText="Lot Number"  SortExpression="ItemCode"/>
                                        <asp:BoundField DataField="QtyOnHand" ReadOnly="True" HeaderText="On Hand"  SortExpression="ItemCode" />
                                        <asp:TemplateField HeaderText="Count 1 Qty" ItemStyle-Width="3em" >
                                            <ItemTemplate >
                                                <asp:TextBox ID="txtC1Qty" runat="server" Text='<%# Eval("Count1Qty" , "{0}") ?? "" %>' style="width:7em; text-align:center"></asp:TextBox>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Count 2 Qty" ItemStyle-Width="3em" >
                                            <ItemTemplate >
                                                <asp:TextBox ID="txtC2Qty" runat="server" Text='<%# Eval("Count2Qty", "{0}") ?? "" %>' style="width:7em; text-align:center"></asp:TextBox>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Final Qty" ItemStyle-Width="3em" >
                                            <ItemTemplate >
                                                <asp:TextBox ID="txtFinalQty" runat="server" Text='<%# Eval("FinalQty", "{0}") ?? "" %>' style="width:7em; text-align:center"></asp:TextBox>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Complete" ItemStyle-Width="3em" >
                                            <ItemTemplate >
                                                <asp:CheckBox ID="chkCompl" runat="server" />
                                            </ItemTemplate>
                                        </asp:TemplateField>
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
