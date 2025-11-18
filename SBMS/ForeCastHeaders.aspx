<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ForeCastHeaders.aspx.cs" Inherits="SBMS.ForeCastHeaders" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Forecasting</title>
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
                           <h3 style="padding-top:2em; line-height:1em">Sales Forecasting</h3>
                       </div>
                       </div>
                <div class="row 150%">
                    <div class="col-3 col-12-wide">&nbsp;</div>      
                    <div class="col-6 col-12-wide">
                        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                            <ContentTemplate>
                                <div style="display:none">
                                <asp:Label ID="lblDir" runat="server" Text=""></asp:Label>
                                </div> 
                                <asp:Panel ID="Panel1" runat="server">
                                    <table style="width: 100%">
                                        <tr>
                                           <td><asp:LinkButton ID="lbtnCreateNew" runat="server" CssClass="fa fa-plus-circle buttonC" ToolTip="Add New Forecast" OnClick="lbtnCreateNew_Click"> Add New Forecast</asp:LinkButton>
                                               <cci:ConfirmButtonExtender ID="lbtnCreateNew_ConfirmButtonExtender1" runat="server" ConfirmText="Create a new Forecast, are you sure?" Enabled="True" TargetControlID="lbtnCreateNew"></cci:ConfirmButtonExtender>
                                           </td>
                                            <td style="text-align:right"></td>            
                                        </tr>
                                    </table>  
                                    <br />
                                </asp:Panel>
                                <asp:GridView ID="GridFCs" runat="server" AutoGenerateColumns="false" CssClass="gridview"  ToolTip="Open Forecast" OnRowDataBound="GridFC_RowDataBound">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <Columns>
                                        <asp:BoundField DataField="ID"  />
                                        <asp:TemplateField HeaderText="Customer" >
                                            <ItemTemplate >
                                                <asp:LinkButton ID="lbtnFC" CommandArgument='<%# Eval("ID")%>' CommandName="lbtnFC" runat="server" Text='<%# Eval("CustSupName")%>' ToolTip="View Forecasting Details" style="color:#4A82AB; font-weight:600" OnClick="lbtnFC_Click" ></asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Reference" DataField="Reference" ReadOnly="True"  />
                                        <asp:BoundField HeaderText="By" DataField="FCastBy" ReadOnly="True"  />
                                        <asp:BoundField HeaderText="Created Date" DataField="DocDate" ReadOnly="True" DataFormatString="{0:dd MMM yyyy}"  />
                                       <asp:TemplateField HeaderText="Active">
                                         <ItemTemplate>
                                             <asp:CheckBox ID="chkActive" runat="server" Text=" " Checked='<%# Eval("Active") %>' Enabled="false" />
                                         </ItemTemplate>
                                     </asp:TemplateField>
                                         <asp:TemplateField>
                                         <ItemTemplate>
                                             <asp:LinkButton ID="lbtnDelete" CommandArgument ='<%# Eval("ID") %>' runat="server" CssClass =" fa fa-ban" style="color:red" OnClick="lbtnDelete_Click" ToolTip="Delete Forecast"></asp:LinkButton>
                                             <cci:ConfirmButtonExtender ID="lbtnDelete_ConfirmButtonExtender1" runat="server" ConfirmText="Delete this forecast? Are you sure" Enabled="True" TargetControlID="lbtnDelete"></cci:ConfirmButtonExtender>
                                         </ItemTemplate>
                                     </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                         </ div> 
                    <div class="col-3 col-12-wide">&nbsp;</div>    
                    </div>
                </div>
        </div>
    </div>
    </form>
</body>
</html>
