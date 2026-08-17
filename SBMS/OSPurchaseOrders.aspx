<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="OSPurchaseOrders.aspx.cs" Inherits="SBMS.OSPurchaseOrders" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>OS Purchase Orders</title>
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
				               <%-- <li>______________</li>
                                <li style="text-align:left">Purchases Dashboard <br /> (Coming Soon)</li>--%>
                            </ul>
			            </nav>
	            </div>
            </div>
           <div id="main">
        <div class="content">
            <div class="container">
                 <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                        <ContentTemplate>
                            <div class="row 150%"> 
                                <div class="col-12 col-12-wide" style="text-align: center">
                                    <a href="https://mydatafusion.online" title="My Data Fusion website">
                                        <img src="images/Logo.png" style="border-radius: 0.25em; float: left" class="logoImg" /></a>
                                    <asp:Image ID="imgCoImg" runat="server" Style="float: right" class="logoImg" />
                                    <h3 style="padding-top: 2em; line-height: 1em">Purchase Orders<asp:Label ID="lblpoqty" runat="server" Text=""></asp:Label></h3>
                                </div>
                                </div>
                          <div class="row 150%">
                              <div class="col-12-narrow col-12-wide">
                                  <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
                                    <ProgressTemplate>
                                        <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
                                       <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." style="padding: 10px; padding-top:15%; border-radius:1.5em" />
                                        </div>
                                    </ProgressTemplate>
                                </asp:UpdateProgress>
                                  <div style="display: none"><asp:Label ID="lblDir" runat="server" Text=""></asp:Label></div>
                                  <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnfind" Style="font-size: 1em; width: 100%">
                                  <table style="width: 100%">
                                      <tr>
                                          <td>Find (Supplier / PO Num) &nbsp;<asp:TextBox ID="txtfind" runat="server"></asp:TextBox><asp:LinkButton ID="lbtnfind" runat="server" CssClass="fa fa-search buttonC" OnClick="lbtnfind_Click"></asp:LinkButton></td>
                                          <td>Filter By Status &nbsp;<asp:DropDownList ID="DDPOStatus" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDPOStatus_SelectedIndexChanged" Width="100px"></asp:DropDownList></td>
                                          <td style="text-align:right"><asp:CheckBox ID="chkCompl" runat="server" Text="Show Completed" AutoPostBack="true" OnCheckedChanged="chkCompl_CheckedChanged" /></td>
                                      </tr>
                                  </table>
                                  </asp:Panel>
                              </div>
                          </div>
                            <div class="row 150%">
                                <div class="col-12 col-12-wide">
                                    <asp:GridView ID="GridPOs" runat="server" AutoGenerateColumns="false" CssClass="gridview" OnRowDataBound="GridPOs_RowDataBound" OnSelectedIndexChanged="GridPOs_SelectedIndexChanged" AllowSorting="true" OnSorting="GridPOs_Sorting">
                                        <HeaderStyle CssClass="gridViewHeader" />
                                        <FooterStyle CssClass="gridViewHeader" />
                                        <RowStyle CssClass="gridViewRow" />
                                        <AlternatingRowStyle CssClass="gridViewAltRow" />
                                        <PagerStyle CssClass="gridViewPager" />
                                        <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="5" />
                                        <Columns>
                                            <asp:BoundField DataField="DocID" ReadOnly="True" />
                                            <asp:TemplateField HeaderText="PO #" ItemStyle-Width="6em" SortExpression="DocumentNumber">
                                                <ItemTemplate>
                                                    <asp:LinkButton ID="lbtnPO" CommandArgument='<%# Eval("DocGUID")%>' CommandName="lbtnPO" runat="server" Text='<%# Eval("DocumentNumber")%>' ToolTip="View Purchase Order" Style="color: #4A82AB; font-weight: 600; padding: 0.5em; border:1px solid #4A82AB; border-radius:0.5em" OnClick="lbtnPO_Click"></asp:LinkButton>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField HeaderText="Supplier Name" DataField="CustSupName" ReadOnly="True" SortExpression="CustSupName" />
                                            <asp:BoundField HeaderText="Reference" DataField="Reference" ReadOnly="True" SortExpression="Reference" />
                                            <asp:BoundField HeaderText="Inv #" DataField="SupplierInvNum" ReadOnly="True" SortExpression="SupplierInvNum" />
                                            <asp:BoundField HeaderText="Due_Date" DataField="DueDelDate" ReadOnly="True" DataFormatString="{0:dd MMM yyyy}" ItemStyle-Width="8em" SortExpression="DueDelDate" />
                                            <asp:BoundField HeaderText="Total" DataField="Total" ReadOnly="True" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" ItemStyle-Width="10em" />
                                            <asp:BoundField HeaderText="Status" DataField="Status" ReadOnly="True" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="8em" SortExpression="Status" />
                                            <asp:TemplateField HeaderText="Started" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="5em" SortExpression="RecStarted">
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="chkstarted" runat="server" Checked='<%# Eval("Started")%>' Enabled="false" Text=" " />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Complete" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="5em">
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="chkComplete" runat="server" Checked='<%# Eval("Complete") %>' Enabled="false" Text=" " />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="2em">
                                                        <ItemTemplate>
                                                            <asp:LinkButton ID="lbtnDeletePO" CommandArgument='<%# Eval("DocGUID") %>' CommandName="lbtnDeleteLine" runat="server" CssClass="fa fa-ban" ToolTip="Delete PO" OnClick="lbtnDeletePO_Click" Style="color: red; font-size:.85em"> </asp:LinkButton>
                                                            <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Delete Purchase Order?" Enabled="True" TargetControlID="lbtnDeletePO"></cci:ConfirmButtonExtender>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>
                                </div>
                            </div>
                            </div>
                         </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
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
