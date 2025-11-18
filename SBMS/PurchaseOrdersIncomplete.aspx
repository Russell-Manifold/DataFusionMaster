<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PurchaseOrdersIncomplete.aspx.cs" Inherits="SBMS.PurchaseOrdersIncomplete" Async="true"  %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Purchase Order Tracking</title>
    <meta name="description" content="Analyze your Sales Orders vs. invoicing and assess outstanding stock balances with My Data Insights. Gain actionable insights into order fulfillment and supply management." />
    <meta name="keywords" content="sales orders analysis, invoicing, stock balances, supply management, outstanding stock, Sage Accounting integration, My Data Insights, order fulfillment" />
    <meta name="robots" content="index, follow" />
    <meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1, user-scalable=no" />
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
                     <asp:LinkButton ID="LinkButton1" runat="server" style="font-size:0.9em; float:right" OnClick="lbtnLogOut_Click"  > Log Out</asp:LinkButton>
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
        <asp:UpdateProgress ID="UpdateProgress2" runat="server" AssociatedUpdatePanelID="UpdatePanel2">
             <ProgressTemplate>
                        <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
                            <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." style="padding: 10px;position:fixed;top:30%;left:40%; border-radius:1.5em" />
                        </div>
                      </ProgressTemplate>
            </asp:UpdateProgress>
                <asp:UpdatePanel ID="UpdatePanel2" runat="server" UpdateMode="Conditional">
                <ContentTemplate>
                <div class="row 150%">
                         <div class="col-2 col-12-narrow"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/Logo.png" style="border-radius: 0.25em; float: left" class="logoImg" /></a></div>
                                 <div class="col-8 col-12-narrow">
                                    <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC fa fa-home" onclick="lbtnHome_Click" style="float:left;"></asp:LinkButton>
                                     <asp:LinkButton ID="lbtnPOs" runat="server" class="buttonC fa fa-chevron-circle-left" PostBackUrl="~/OSPurchaseOrders.aspx" style="float:left; margin-left:1em">&nbsp;Purchase Orders</asp:LinkButton>
                                         <asp:LinkButton ID="lbtnDownload" runat="server" class="buttonC fa fa-download" OnClick="btnAppSubmit2_Click" ToolTip="Download source data." style="float:right">&nbsp;Excel</asp:LinkButton>
                                     <div style="margin:auto"><br />
				                            <h1 style="text-align:center">Purchase Order Tracking<br /> <asp:Label ID="lblReccount" runat="server" Text=""></asp:Label></h1>
			                            </div>
                                  </div>
                                 <div class="col-2 col-12-narrow">
                                     <asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" />
                                 </div>
                        </div>
                        <asp:Label ID="lblDir" runat="server" Text="" style="display:none"></asp:Label>
                        <div class="row 150%">
                        <div class="col-12 col-12-narrow">
                            <hr />
                        </div>
                    </div>
                        <div class="row 150%">
                            <div class="col-2 col-12-narrow">
                              <h4>Filter<asp:LinkButton ID="lbtnClear" runat="server" CssClass="buttonC fa fa-search" ToolTip="Clear Filters and Refresh" OnClick="lbtnClear_Click" style="margin:auto; font-size:0.8em; margin-top:0.8em"></asp:LinkButton></h4>
                                     </div>
                            <div class="col-2 col-12-narrow">  
                                <h4>Number</h4>
                                <asp:TextBox ID="txtNumber" runat="server" AutoPostBack="true" OnTextChanged="txtNumber_TextChanged" Style="width: 100%; height:2.5em"></asp:TextBox>
                                 </div>
                            <div class="col-2 col-12-narrow">  
                                <h4>Supplier</h4>
                                <asp:TextBox ID="txtCustomer" runat="server" AutoPostBack="true" OnTextChanged="txtNumber_TextChanged" Style="width: 100%; height:2.5em "></asp:TextBox>
                                </div>
                            <div class="col-2 col-12-narrow">  
                                <h4>Reference</h4>
                                <asp:TextBox ID="txtReference" runat="server" AutoPostBack="true" OnTextChanged="txtNumber_TextChanged" Style="width: 100%; height:2.5em "></asp:TextBox>
                                </div>
                            <div class="col-2 col-12-narrow">  
                                <h4>Item</h4>
                                <asp:TextBox ID="txtItem" runat="server" AutoPostBack="true" OnTextChanged="txtNumber_TextChanged" Style="width: 100%; height:2.5em "></asp:TextBox>
                                </div>
                            <div class="col-2 col-12-narrow" >    
                                <h4 style="text-align:right">Status</h4>  
                                        <asp:DropDownList ID="DDStatus" runat="server" Style="width: 100%; margin-bottom: 1em; height:2em" AutoPostBack="true" OnSelectedIndexChanged="DDStatus_SelectedIndexChanged" CssClass="custom-dropdown">
                                        <asp:ListItem>- ALL -</asp:ListItem>
                                        <asp:ListItem>Overdue</asp:ListItem>
                                        <asp:ListItem>Pending</asp:ListItem>
                                        <asp:ListItem>Confirmed</asp:ListItem>
                                        </asp:DropDownList>
                                </div>
                            </div>
                         <div class="row 150%">
                            <div class="col-12 col-12-narrow">
                                <hr />
                            </div>
                        </div>
                        <div class="row 150%">
                            <div class="col-12 col-12-narrow">
                                <div id="gridgldiv" runat="server">
                                    <asp:GridView ID="GridViewGL" runat="server" AutoGenerateColumns="false" EnableViewState="true" CssClass="gridview" AllowSorting="true" OnSorting="GridViewGL_Sorting" PageSize="50" AllowPaging="true" OnPageIndexChanging="GridViewGL_PageIndexChanging" RowStyle-Wrap="true" PagerStyle-Width="50" PagerStyle-Wrap="False" OnRowCommand="GridViewGL_RowCommand" ShowFooter="true" >
                                        <HeaderStyle CssClass="gridViewHeader" />
                                        <RowStyle CssClass="gridViewRow" />
                                        <AlternatingRowStyle CssClass="gridViewAltRow" />
                                        <FooterStyle CssClass="gridviewfooter" />
                                        <PagerStyle CssClass="gridViewPager" />
                                        <Columns>
                                             <asp:BoundField DataField="Number" HeaderText="Number" ReadOnly="True" ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left" SortExpression="Number" /> 
                                            <asp:BoundField DataField="PO_Date" HeaderText="PO_Date" ReadOnly="True" SortExpression="PO_Date" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" DataFormatString="{0:dd-MMM-yy}" ItemStyle-Width="7em" />
                                            <asp:BoundField DataField="Due_Date" HeaderText="Due_Date" ReadOnly="True" SortExpression="Due_Date" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" DataFormatString="{0:dd-MMM-yy}" ItemStyle-Width="7em" />
                                             <asp:BoundField DataField="Customer_Name" HeaderText="Supplier_Name" ReadOnly="True" ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left" SortExpression="Customer_Name" />
                                            <asp:BoundField DataField="Reference" HeaderText="Reference" ReadOnly="True" SortExpression="Reference" HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Left" />
                                              <asp:BoundField DataField="Item" HeaderText="Code" ReadOnly="True" SortExpression="Item" HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Left" />
                                            <asp:BoundField DataField="Description" HeaderText="Item Description" ReadOnly="True" SortExpression="Description" HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Left" />
                                            <asp:BoundField DataField="PO_Qty" HeaderText="PO_Qty" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" ReadOnly="True" DataFormatString="{0:N0}" SortExpression="PO_Qty"/>
                                            <asp:BoundField DataField="Nett_Line_Value" HeaderText="Nett_Line_Value" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" FooterStyle-HorizontalAlign="Right" ReadOnly="True" DataFormatString="{0:N2}" SortExpression="Nett_Line_Value" />
                                            <asp:BoundField DataField="Status" HeaderText="Status" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" ReadOnly="True" SortExpression="Status" />
                                            <asp:BoundField DataField="LineMessage" HeaderText="Line_Message" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" FooterStyle-HorizontalAlign="Right" ReadOnly="True"  />
                                        </Columns>
                                    </asp:GridView>
                                </div>
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

