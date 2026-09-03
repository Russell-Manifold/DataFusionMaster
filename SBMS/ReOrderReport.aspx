<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="ReOrderReport.aspx.cs" Inherits="SBMS.ReOrderReport" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Re-Order Report</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <style type="text/css">
        /* An item with many open works orders overflows the popup past the bottom of the
           screen, taking the Close button with it. Scroll the rows, keep the heading,
           summary and Close button in view. */
        #reorderDetailScroll {
            max-height: 55vh;
            overflow: auto;
            text-align: left;
        }
        /* Column headings stay put while the rows scroll under them. */
        #reorderDetailScroll th {
            position: -webkit-sticky;
            position: sticky;
            top: 0;
            z-index: 2;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="content">
            <div class="container">
                <div class="row 150%">
                    <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg" /></a></div>
                    <div class="8u 12u$(medium)">
                        <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" OnClick="lbtnHome_Click" ToolTip="Return to the dashboard">&nbsp;&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="lbtnBack" runat="server" class="buttonC icon fa-arrow-left" OnClick="lbtnBack_Click" ToolTip="Back to Stock Control">&nbsp;Stock Control</asp:LinkButton>
                        <asp:LinkButton ID="lbtnDownload" runat="server" class="buttonC icon fa-download" Style="float:right" OnClick="lbtnDownload_Click" ToolTip="Download to Excel">&nbsp;Excel</asp:LinkButton>
                        <h3 style="padding-top:0; line-height:1em">Re-Order Report</h3>
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server" Style="float:right" class="logoImg" /></div>
                </div>
                <div class="row 150%">
                    <div class="1u 12u$(medium)">&nbsp;</div>
                    <div class="10u 12u$(medium)">
                        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                            <ContentTemplate>
                                <table style="width:100%">
                                    <tr>
                                        <td style="width:22em">
                                            <asp:TextBox ID="txtfind" runat="server" Width="18em" placeholder="Search code, description or category"></asp:TextBox>
                                            <asp:LinkButton ID="lbtnFind" runat="server" CssClass="fa fa-search buttonRed" OnClick="lbtnFind_Click" ToolTip="Search"> </asp:LinkButton>
                                        </td>
                                        <td>
                                            <asp:CheckBox ID="chkOnlyBelow" runat="server" Text=" Only items to re-order" Checked="true" AutoPostBack="true" OnCheckedChanged="Filter_Changed" Font-Size="Small" />
                                            &nbsp;&nbsp;
                                            <asp:CheckBox ID="chkHideDormant" runat="server" Text=" Hide items with no stock, orders or demand" Checked="true" AutoPostBack="true" OnCheckedChanged="Filter_Changed" Font-Size="Small" />
                                            &nbsp;&nbsp;
                                            <asp:CheckBox ID="chkForecasts" runat="server" Text=" Include sales forecasts as demand" Checked="false" AutoPostBack="true" OnCheckedChanged="Filter_Changed" Font-Size="Small" />
                                        </td>
                                        <td style="text-align:right; width:12em">
                                            <asp:Label ID="lblReccount" runat="server" Text="" Font-Size="Small"></asp:Label>
                                        </td>
                                    </tr>
                                </table>
                                <span style="font-size:x-small">Hover any column heading to see exactly what it counts.</span>
                                <br /><br />

                                <asp:GridView ID="GridReOrder" runat="server" AutoGenerateColumns="false" CssClass="gridview"
                                    AllowSorting="true" OnSorting="GridReOrder_Sorting" OnRowDataBound="GridReOrder_RowDataBound">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <Columns>
                                        <asp:TemplateField HeaderText="Item Code" SortExpression="ItemCode" ItemStyle-Width="10em">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="lbtnDrill" runat="server" CommandArgument='<%# Eval("ItemCode") %>' OnClick="lbtnDrill_Click"
                                                    Text='<%# Eval("ItemCode") %>' ToolTip="Show the documents behind these figures"
                                                    Style="color:#4A82AB; font-weight:600; text-decoration:underline"></asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Description" DataField="Description" ReadOnly="True" SortExpression="Description" />
                                        <asp:BoundField HeaderText="Category" DataField="CategoryDescript" ReadOnly="True" SortExpression="CategoryDescript" ItemStyle-Width="10em" />
                                        <asp:BoundField HeaderText="Unit" DataField="Unit" ReadOnly="True" ItemStyle-Width="4em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                        <asp:BoundField HeaderText="On Hand" DataField="QtyOnHand" ReadOnly="True" SortExpression="QtyOnHand" DataFormatString="{0:N2}" ItemStyle-Width="7em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                        <asp:BoundField HeaderText="On PO" DataField="QtyOnOrder" ReadOnly="True" SortExpression="QtyOnOrder" DataFormatString="{0:N2}" ItemStyle-Width="7em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                        <asp:BoundField HeaderText="Committed" DataField="QtyCommitted" ReadOnly="True" SortExpression="QtyCommitted" DataFormatString="{0:N2}" ItemStyle-Width="7em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                        <asp:BoundField HeaderText="Available" DataField="QtyAvailable" ReadOnly="True" SortExpression="QtyAvailable" DataFormatString="{0:N2}" ItemStyle-Width="7em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                        <asp:BoundField HeaderText="Re-Order Level" DataField="ReOrderLevel" ReadOnly="True" SortExpression="ReOrderLevel" DataFormatString="{0:N2}" ItemStyle-Width="8em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                        <asp:BoundField HeaderText="Order Qty" DataField="RecommendedQty" ReadOnly="True" SortExpression="RecommendedQty" DataFormatString="{0:N2}" ItemStyle-Width="8em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                    </Columns>
                                </asp:GridView>

                                <asp:LinkButton ID="lbtnDetailTarget" runat="server" Style="display:none">t</asp:LinkButton>
                                <cci:ModalPopupExtender ID="mpeDetail" runat="server" BackgroundCssClass="ModalPopupBG" Drag="true"
                                    CancelControlID="btnDetailClose" PopupControlID="PnlDetail" PopupDragHandleControlID="DetailHeader" TargetControlID="lbtnDetailTarget"></cci:ModalPopupExtender>
                                <asp:Panel ID="PnlDetail" runat="server" Style="display:none">
                                    <div class="HellowWorldPopup" style="max-width:92vw; max-height:90vh; overflow:hidden">
                                        <div id="DetailHeader" runat="server" class="PopupHeader">
                                            <h2>Documents Behind These Figures</h2>
                                        </div>
                                        <div class="PopupBody" style="margin:2em">
                                            <h4><asp:Label ID="lblDetailItem" runat="server" Text=""></asp:Label></h4>
                                            <asp:Label ID="lblDetailSummary" runat="server" Text="" Font-Size="Small"></asp:Label>
                                            <br /><br />
                                            <div id="reorderDetailScroll">
                                            <asp:GridView ID="GridDetail" runat="server" AutoGenerateColumns="false" CssClass="gridview" OnRowDataBound="GridDetail_RowDataBound">
                                                <HeaderStyle CssClass="gridViewHeader" />
                                                <RowStyle CssClass="gridViewRow" />
                                                <AlternatingRowStyle CssClass="gridViewAltRow" />
                                                <Columns>
                                                    <asp:BoundField HeaderText="Source" DataField="Source" ItemStyle-Width="9em" />
                                                    <asp:BoundField HeaderText="Document" DataField="DocNumber" ItemStyle-Width="10em" />
                                                    <asp:BoundField HeaderText="Reference" DataField="Reference" />
                                                    <asp:BoundField HeaderText="Status" DataField="Status" ItemStyle-Width="8em" />
                                                    <asp:BoundField HeaderText="Due Date" DataField="DueDate" DataFormatString="{0:dd MMM yyyy}" ItemStyle-Width="8em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                                    <asp:BoundField HeaderText="Qty" DataField="Qty" DataFormatString="{0:N2}" ItemStyle-Width="7em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                                </Columns>
                                            </asp:GridView>
                                            </div>
                                            <asp:Label ID="lblDetailNone" runat="server" Text="" Font-Size="Small" ForeColor="#666666"></asp:Label>
                                        </div>
                                        <div class="Controls" style="text-align:center">
                                            <asp:LinkButton ID="btnDetailClose" runat="server" CssClass="buttonCancel">Close</asp:LinkButton><br />
                                        </div>
                                    </div>
                                </asp:Panel>
                            </ContentTemplate>
                            <Triggers>
                                <asp:PostBackTrigger ControlID="lbtnDownload" />
                            </Triggers>
                        </asp:UpdatePanel>
                    </div>
                    <div class="1u 12u$(medium)">&nbsp;</div>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
