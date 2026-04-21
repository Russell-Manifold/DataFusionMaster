<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ItemConvertUOM.aspx.cs" Inherits="SBMS.ItemConvertUOM" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Convert UOM</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <%--<link rel="stylesheet" href="assets/css/main.css" />--%>
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
                    <div class="col-12 col-12-wide" style="text-align: center">
                        <a href="https://mydatafusion.online" title="My Data Fusion website">
                            <img src="images/Logo.png" style="border-radius: 0.25em; float: left" class="logoImg" /></a>
                        <asp:Image ID="imgCoImg" runat="server" Style="float: right" class="logoImg" />
                        <h3 style="padding-top: 1.5em; line-height: 1em">Convert item to alternate item code</h3>
                        <h4>
                            <asp:Label ID="lblerr" runat="server" Text=" " ForeColor="Red">&nbsp;</asp:Label></h4>
                                        <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
                                       <ProgressTemplate>
                                           <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
                                              <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." style="padding: 10px; padding-top:15%; border-radius:1.5em" />
                                           </div>
                                         </ProgressTemplate>
                                   </asp:UpdateProgress>
                    </div>
                </div>
  
             <asp:UpdatePanel ID="UpdatePanel1" runat="server">
             <ContentTemplate>
                <div class="row 150%">
                             <div class="col-12 col-12-m" style="text-align: center">
                                <table style="width:600px; margin:auto">
                                    <tr>
                                        <td>Select Store</td>
                                        <td><asp:DropDownList ID="DDStore" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDStore_SelectedIndexChanged" style="width:20em">
                                               <asp:ListItem>----------------------------</asp:ListItem>
                                            </asp:DropDownList></td>
                                    </tr>
                                    <tr>
                                            <td colspan="2"><hr /></td>
                                        </tr>
                                    <tr>
                                        <td colspan="2">
                                            <h3 style="text-align:center">Convert From Item</h3>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>Item</td>
                                        <td><asp:DropDownList ID="ddConvertFrom" runat="server" style="width:20em" AutoPostBack="true" OnSelectedIndexChanged="ddConvertFrom_SelectedIndexChanged"></asp:DropDownList></td>
                                    </tr>
                                    <tr >
                                        <td><br />Unit Of Measure</td>
                                        <td><br /><asp:Label ID="lblFromUOM" runat="server" Text="" >ea</asp:Label></td>
                                    </tr>
                                    <tr style="padding-bottom:0.5em">
                                        <td><br />Qty On Hand</td>
                                        <td><br /><asp:Label ID="lblQOH" runat="server" Text="">5</asp:Label><br /><br /></td>
                                    </tr>
                                    <tr>
                                        <td>Qty To Convert</td>
                                        <td>
                                            <asp:TextBox ID="txtFromQty" runat="server" Width="60px"></asp:TextBox>
                                            <cci:FilteredTextBoxExtender ID="ftbeP" runat="server" TargetControlID="txtFromQty" FilterType="Numbers,Custom" ValidChars="." />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td colspan="2"><hr /></td>
                                    </tr>
                                    <tr>
                                        <td colspan="2">
                                            <h3 style="text-align:center">Convert To Item</h3>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>Item</td>
                                        <td><asp:DropDownList ID="DDConvertTo" runat="server" style="width:20em"></asp:DropDownList></td>
                                    </tr>
                                    <tr>
                                        <td>Unit Of Measure</td>
                                        <td><asp:Label ID="lblToUOM" runat="server" Text=""></asp:Label><br /><br /></td>
                                    </tr>
                                    <tr>
                                        <td>Convert To Quantity</td>
                                        <td>
                                            <asp:TextBox ID="txtToQty" runat="server" Width="60px"></asp:TextBox>
                                            <cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender2" runat="server" TargetControlID="txtToQty" FilterType="Numbers,Custom" ValidChars="." />
                                        </td>
                                    </tr>
                                    <tr>
                                    <td colspan="2" style="padding-top:2em">
                                        <asp:LinkButton ID="lbtnConvert" runat="server" CssClass="buttonJC icon fa-refresh" style="width:100%" OnClick="lbtnConvert_Click">Convert Items</asp:LinkButton>
                                        <cci:ConfirmButtonExtender ID="lbtnConvert_ConfirmButtonExtender1" runat="server" ConfirmText="You are about to convert items to different item code, are you sure?" Enabled="True" TargetControlID="lbtnConvert"></cci:ConfirmButtonExtender>
                                    </td>
                                </tr>
                                    <tr>
                                        <td colspan="2" style="padding-top:2em">Note - If you purchase items in a Unit of Measure (UOM) of a Case which contains 24 items, you will convert 1 x Case --> to 24 x Units.<br /> (** Case and Units must have different item codes) <br /><br />
                                                    Your item quantity on hand, across all platforms will be updated accordingly.
                                        </td>
                                    </tr>
                                </table>
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
