<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="ProdLineTrackingHistory.aspx.cs" Inherits="SBMS.ProdLineTrackingHistory" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Production History</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
</head>
<body>
    <form id="form1" runat="server">
         <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="content">
            <div class="container">      
                <div class="row 150%">
                    <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>        
                    <div class="8u 12u$(medium)">
                          <asp:LinkButton ID="lbtnDash" runat="server" class="buttonC icon fa-home" OnClick="lbtnDash_Click">&nbsp;&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="lbtnJobTrack" runat="server" class="buttonTransparent icon fa-boxes" OnClick="lbtnJobTrack_Click">&nbsp;Production Tracking</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton>
                        <br /><br />
                        <h2 style="padding-top:0; line-height:1em">Production Line Tracking History</h2>                       
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
                <div class="row 150%">
                    <div class="2u 12u$(medium)">&nbsp;</div>    
                    <div class="8u 12u$(medium)">
                        <table style="width:100%">
                            <tr>
                                <td style="width:8em">Item Code</td>
                                <td><asp:Label ID="lblJobNumber" runat="server" Text=""></asp:Label></td>
                                <td  style="width:6em">&nbsp;</td>
                                <td><asp:Label ID="lblCustomer" runat="server" Text=""></asp:Label></td>
                                <td  style="width:8em">Due Date</td>
                                <td  style="width:8em"><asp:Label ID="lblCreatedDate" runat="server" Text=""></asp:Label></td>
                            </tr>
                            <tr>
                                <td>Item</td>
                                <td colspan="3"><asp:Label ID="lblSummary" runat="server" Text=""></asp:Label></td>
                                <td>Required Qty</td>
                                <td><asp:Label ID="lblJCQty" runat="server" Text=""></asp:Label></td>
                            </tr>
                        </table>
                           <asp:GridView ID="GridHistLines" runat="server" AutoGenerateColumns="false" CssClass="gridview" RowStyle-Wrap="true" Style="font-size: 1em" AllowSorting="true" OnSorting="GridHistLines_Sorting" OnRowDataBound="GridHistLines_RowDataBound" ShowFooter="true">
                            <HeaderStyle CssClass="gridViewHeader" />
                            <RowStyle CssClass="gridViewRow" />
                            <AlternatingRowStyle CssClass="gridViewAltRow" />
                            <FooterStyle CssClass="gridViewHeader" />
                            <PagerStyle CssClass="gridViewPager" />
                            <Columns>
                                <asp:BoundField HeaderText="Date" DataField="MoveDate" ReadOnly="True" ItemStyle-Width="8em" DataFormatString ="{0:dd MMM yyyy}" SortExpression="MoveDate" />
                                <asp:BoundField HeaderText="From" DataField="FromProcess" ReadOnly="True" SortExpression="FromProcess" />
                                <asp:BoundField HeaderText="To" DataField="ToProcess" ReadOnly="True" SortExpression="ToProcess" />
                                <asp:BoundField HeaderText="Approved_Qty" DataField="MoveQty" ReadOnly="True" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="8em" SortExpression="ApprovedQty" DataFormatString="{0:N0}" />
                                <asp:BoundField HeaderText="Reject_Qty" DataField="RejectQty" ReadOnly="True" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="8em" SortExpression="RejectQty" FooterStyle-HorizontalAlign="Center" DataFormatString="{0:N0}" />
                            </Columns>
                        </asp:GridView>
                         </ div> 
                    <div class="2u 12u$(medium)">&nbsp;</div>    
                    </div>
                </div>
        </div>
    </form>
</body>
</html>
