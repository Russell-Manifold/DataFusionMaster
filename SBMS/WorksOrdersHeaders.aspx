<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="WorksOrdersHeaders.aspx.cs" Inherits="SBMS.WorksOrdersHeaders" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Works Orders</title>
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
                            <h3 style="padding-top:2em; line-height:1em">Works Orders</h3>
                        </div>
                     </div>
                <div class="row 150%">
                    <div class="col-12 col-12-wide">
                        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                            <ContentTemplate>
                                <div style="display:none">
                                <asp:Label ID="lblDir" runat="server" Text=""></asp:Label>
                                </div> 
                                <asp:Panel ID="Panel1" runat="server">
                                    <table style="width: 100%">
                                        <tr>
                                           <td><asp:LinkButton ID="lbtnCreateNew" runat="server" CssClass=" fa fa-plus-circle buttonC" ToolTip="Add New Works Order" OnClick="lbtnCreateNew_Click"> Add New Works Order</asp:LinkButton>
                                               <cci:ConfirmButtonExtender ID="lbtnCreateNew_ConfirmButtonExtender1" runat="server" ConfirmText="Create a new Works Order, are you sure?" Enabled="True" TargetControlID="lbtnCreateNew"></cci:ConfirmButtonExtender>
                                           </td>
                                            <td>
                                                <asp:Panel ID="Panel2" runat="server" DefaultButton="lbtnSearch"><asp:TextBox ID="txtSearch" runat="server" placeholder="Find" Width="150px"></asp:TextBox><asp:LinkButton ID="lbtnSearch" runat="server" CssClass="fa fa-search buttonC" ToolTip="Seach" OnClick="lbtnSearch_Click"></asp:LinkButton></asp:Panel></td>
                                            <td style="text-align:right">Status&nbsp;</td> 
                                           <td style="width:100px"><asp:DropDownList ID="DDStatus" runat="server" AutoPostBack="true" OnSelectedIndexChanged="lbtnSearch_Click">
                                               <asp:ListItem Value="0">Active</asp:ListItem>
                                               <asp:ListItem Value="1">All</asp:ListItem>
                                               <asp:ListItem Value="2">Complete</asp:ListItem>
                                               </asp:DropDownList></td> 
                                        </tr>
                                    </table>  
                                </asp:Panel>
                                <asp:GridView ID="GridWOs" runat="server" AutoGenerateColumns="false" CssClass="gridview" AllowSorting="true" ToolTip="Open Works Order" OnSorting="GridWOs_Sorting" OnRowDataBound="GridWOs_RowDataBound">
                                            <HeaderStyle CssClass="gridViewHeader" />
                                            <FooterStyle CssClass="gridViewHeader" />
                                            <RowStyle CssClass="gridViewRow" />
                                            <AlternatingRowStyle CssClass="gridViewAltRow" />
                                            <PagerStyle CssClass="gridViewPager" />
                                            <Columns>
                                                <asp:BoundField DataField="ID" SortExpression="ID" HeaderText="ID" />
                                                <asp:TemplateField HeaderText="Number" SortExpression="ID" ItemStyle-Width="5em">
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="lbtnWO" CommandArgument='<%# Eval("ID")%>' CommandName="lbtnWO"
                                                            runat="server" Text='<%# "WO" + DataBinder.Eval(Container.DataItem, "WONum").ToString() %>' 
                                                            ToolTip="View Works Order Details" style="color:#4A82AB; font-weight:600; padding: 0.5em; border:1px solid #4A82AB; border-radius:0.5em; text-align:center"  Width="55px"
                                                            OnClick="lbtnWO_Click">
                                                        </asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:BoundField HeaderText="Customer" DataField="CustSupName" SortExpression="CustSupName" />
                                                <asp:BoundField HeaderText="Reference" DataField="Reference" SortExpression="Reference" />
                                                 <asp:TemplateField HeaderText="Linked Document" SortExpression="LinkedDocumentNum">
                                                <ItemTemplate>
                                                     <asp:LinkButton ID="lbtnLinkedDoc" CommandArgument='<%# Eval("LinkedDocumentNum")%>' CommandName="lbtnLinkedDoc"
                                                         runat="server" Text='<%# Eval("LinkedDocumentNum") %>' 
                                                         ToolTip="View Linked Document" style="color:#4A82AB; font-weight:600" 
                                                         OnClick="lbtnLinkedDoc_Click">
                                                     </asp:LinkButton>
                                                 </ItemTemplate>
                                                     </asp:TemplateField>
                                                <asp:BoundField HeaderText="By" DataField="WOrderBy" SortExpression="WOrderBy" />
                                                <%--<asp:BoundField HeaderText="Created" DataField="WOrderDate" SortExpression="WOrderDate" DataFormatString="{0:dd MMM yyyy}" ItemStyle-Width="8em"/>--%>
                                                <asp:BoundField HeaderText="Due Date" DataField="DueDate" SortExpression="DueDate"  DataFormatString="{0:dd MMM yyyy}" ItemStyle-Width="8em" />
                                                <asp:TemplateField HeaderText="Status" SortExpression="Status" ItemStyle-Width="8em">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblStatus" runat="server" Text='<%# Eval("Status") %>' ></asp:Label>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="Active" SortExpression="Active" ItemStyle-Width="3em">
                                                    <ItemTemplate>
                                                        <asp:CheckBox ID="chkActive" runat="server" Text=" " Checked='<%# Eval("Active") %>' Enabled="false" />
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField ItemStyle-Width="3em">
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="lbtnDelete" CommandArgument='<%# Eval("ID") %>' runat="server" CssClass="fa fa-ban" style="color:red" OnClick="lbtnDelete_Click" ToolTip="Delete works Order">
                                                        </asp:LinkButton>
                                                        <cci:ConfirmButtonExtender ID="lbtnDelete_ConfirmButtonExtender1" runat="server" ConfirmText="Delete this Works Order? Are you sure" Enabled="True" TargetControlID="lbtnDelete">
                                                        </cci:ConfirmButtonExtender>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                            </Columns>
                                        </asp:GridView>
                                 </ContentTemplate>
                        </asp:UpdatePanel>
                         </ div>  
                    </div>
                </div>
        </div>
        </div>
        <section id="footer" class="wrapper">&nbsp;</section>      
    </form>
</body>
</html>
