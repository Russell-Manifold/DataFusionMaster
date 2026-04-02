﻿<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="OSSalesOrders.aspx.cs" Inherits="SBMS.OSSalesOrders" %>
<%@ Register Src="~/CommonScripts.ascx" TagPrefix="uc" TagName="CommonScripts" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Sales Orders</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="prologue/assets/css/main.css" />
    <link href="lib/toastr/toastr.min.css" rel="stylesheet" />
   <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script  type="text/javascript" src="lib/toastr/toastr.min.js"></script>
    <script type="text/javascript" src="scripts/notifications.js"></script>
</head>
<body>
    <form id="form1" runat="server">
         <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
                 <uc:CommonScripts ID="CommonScripts" runat="server" />
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
                        <%--<li><asp:LinkButton ID="lbtnDashSales" runat="server" CssClass="buttonM" OnClick="lbtnDashSales_Click" ToolTip="View Sales Analysis Dashboard" > Sales Dashboard<br /> (Coming Soon)</asp:LinkButton></li>--%>
                    </ul>
	            </nav>
     </div>
 </div>
            <div id="main">
        <div class="content">
            <div class="container">
                        <div class="row 150%">
                            <div class="col-12 col-12-wide" style="text-align: center">
                                <a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/Logo.png" style="border-radius: 0.25em; float: left" class="logoImg" /></a>
                                <asp:Image ID="imgCoImg" runat="server" Style="float: right" class="logoImg" />
                                <asp:LinkButton ID="lbtnSOs" runat="server" class="buttonC fa fa-chevron-circle-right" PostBackUrl="~/SalesOrdersIncomplete.aspx" style="float:left; margin:1em">&nbsp;Sales Order Tracking</asp:LinkButton>
                                <h3 style="padding-top: 2em; line-height: 1em">Sales Orders <asp:Label ID="lblpoqty" runat="server" Text=""></asp:Label></h3>
                            </div>
                        </div>
                        <div class="row 150%">
                            <div class="col-12 col-12-wide">
                               <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                            <ContentTemplate>
                                <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
                                    <ProgressTemplate>
                                        <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
                                            <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." style="padding: 10px;position:fixed;top:30%;left:40%; border-radius:1.5em" />
                                        </div>
                                      </ProgressTemplate>
                                </asp:UpdateProgress> 
                                <div style="display:none">
                                <asp:Label ID="lblDir" runat="server" Text=""></asp:Label>
                                </div> 
                                <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnfind" style="width:100%">
                                    <table style="width: 100%">
                                        <tr>
                                            <td style="padding:3px">Find (Cust/SO#/Ref) &nbsp;<asp:TextBox ID="txtfind" runat="server"></asp:TextBox><asp:LinkButton ID="lbtnfind" runat="server" CssClass="fa fa-search buttonRed" OnClick="lbtnfind_Click" ToolTip="Search Sales Orders"></asp:LinkButton></td>
                                            <td style="text-align: center">Status &nbsp;<asp:DropDownList ID="DDStatus" runat="server" Width="100px" AutoPostBack="true" OnSelectedIndexChanged="DDSOStatus_SelectedIndexChanged"></asp:DropDownList></td>
                                            <td style="text-align: center">SO Status &nbsp;<asp:DropDownList ID="DDSOStatus" runat="server" Width="100px" AutoPostBack="true" OnSelectedIndexChanged="DDSOStatus_SelectedIndexChanged"></asp:DropDownList></td>
                                            <td style="text-align: center">Due Date <= &nbsp;<asp:DropDownList ID="DDueDate" runat="server" Width="100px" AutoPostBack="true" OnSelectedIndexChanged="DDSOStatus_SelectedIndexChanged"></asp:DropDownList></td>
                                            <td style="text-align: center">Delivery &nbsp;<asp:DropDownList ID="dlDelivery" runat="server" Width="100px" AutoPostBack="true" OnSelectedIndexChanged="DDSOStatus_SelectedIndexChanged"></asp:DropDownList></td>
                                            <td style="text-align: left; margin-top:1em"><asp:CheckBox ID="chkCompl" runat="server" Text=" Show Completed" AutoPostBack="true" OnCheckedChanged="chkCompl_CheckedChanged" /></td>
                                            <td><asp:LinkButton ID="lbtnDownload" runat="server" CssClass="fa fa-download buttonRed" ToolTip="Download to excel" OnClick="lbtnDownload_Click"></asp:LinkButton></td>
                                        </tr>
                                    </table>   
                                </asp:Panel>
                                <asp:GridView ID="GridPOs" runat="server" AutoGenerateColumns="false" CssClass="gridview" OnRowDataBound="GridPOs_RowDataBound" OnRowCommand="GridPOs_RowCommand" AllowSorting="true" OnSorting="GridPOs_Sorting" ToolTip="Open Sales Order">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="5" />
                                    <Columns>
                                        <asp:BoundField DataField="DocID" ReadOnly="True" />
                                        <asp:TemplateField HeaderText="SO #" ItemStyle-Width="6em" SortExpression="DocumentNumber" >
                                            <ItemTemplate >
                                                <asp:LinkButton ID="lbtnSO" CommandArgument='<%# Eval("DocGUID")%>' CommandName="lbtnSO" runat="server" Text='<%# Eval("DocumentNumber")%>' ToolTip="View Sales Order" style="color:#4A82AB; font-weight:600; padding: 0.5em; border:1px solid #4A82AB; border-radius:0.5em"  ></asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Customer" DataField="CustSupName" ReadOnly="True" SortExpression="CustSupName" />
                                        <asp:BoundField HeaderText="Address_3" DataField="DelAddress3" ReadOnly="True" SortExpression="DelAddress3" ItemStyle-CssClass="hidecolumn" HeaderStyle-CssClass="hidecolumn" />
                                        <asp:BoundField HeaderText="Reference" DataField="Reference" ReadOnly="True" SortExpression="Reference" />
                                        <asp:TemplateField HeaderText="Pick-Slip" ItemStyle-BackColor="Azure" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" SortExpression="LinkedPSNum">
                                            <ItemTemplate >
                                                <asp:LinkButton ID="lbtnPS" CommandArgument='<%# String.Format("{0} | {1}", Eval("DocGUID"), Eval("LinkedPSNum")) %>' CommandName="lbtnPS"  runat="server" Text='<%# Eval("LinkedPSNum")%>' ToolTip="View Picking Slip" style="color:#4A82AB; font-weight:600;" ></asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Job Card" ItemStyle-BackColor="antiquewhite" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" SortExpression="LinkedJCNum">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="lbtnJC" CommandArgument='<%# String.Format("{0} | {1}", Eval("DocGUID"), Eval("LinkedJCNum")) %>' CommandName="lbtnJC"  runat="server" Text='<%# Eval("LinkedJCNum")%>' ToolTip="View Job Card" style="color:#4A82AB; font-weight:600;"></asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                         <asp:BoundField HeaderText="Status" DataField="LinkedStatus" ReadOnly="True" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="8em" SortExpression="LinkedStatus" />
                                        <asp:BoundField HeaderText="Due_Date" DataField="DueDelDate" ReadOnly="True" DataFormatString="{0:dd MMM yyyy}" ItemStyle-Width="8em" SortExpression="DueDelDate" />
                                        <asp:BoundField HeaderText="Total" DataField="Total" ReadOnly="True" DataFormatString="{0:# ###.00}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />  
                                        <asp:TemplateField HeaderText="Start" ItemStyle-HorizontalAlign="Left" SortExpression="RecStarted" ItemStyle-Width="3em" HeaderStyle-HorizontalAlign="Right">
                                            <ItemTemplate >
                                                <asp:CheckBox ID="chkstarted" runat="server" Checked='<%# Eval("Started")%>' Enabled="false" Text=" "  />
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Compl" ItemStyle-HorizontalAlign="Left" ItemStyle-Width="3em" HeaderStyle-HorizontalAlign="Right" ItemStyle-VerticalAlign="Bottom">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkComplete" runat="server" Checked='<%# Eval("Complete") %>' Enabled="false" Text=" " />
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="SO_Status" DataField="Status" ReadOnly="True" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" SortExpression="Status" />
                                        <asp:BoundField HeaderText="Delivery" DataField="DeliveryBy" ReadOnly="True" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" SortExpression="DeliveryBy" ItemStyle-CssClass="hidecolumn" HeaderStyle-CssClass="hidecolumn" />
                                    </Columns>
                                </asp:GridView>
                            </ContentTemplate>
                            <Triggers>
                                <asp:PostBackTrigger ControlID="lbtnDownload" />
                                <asp:PostBackTrigger ControlID="imgbRec" />
                                <asp:PostBackTrigger ControlID="ibtnPickSlips" />
                            </Triggers>
                        </asp:UpdatePanel>
                            </div>
                        </div>
            </div>
        </div>

                <section id="footer" class="wrapper">
                    &nbsp;
                </section>
        </div>
    </form>
     <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</body>
</html>
