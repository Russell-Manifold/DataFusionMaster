<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="StockCountVariances.aspx.cs" Inherits="SBMS.StockCountVariances" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Variances</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="content">
            <div class="container">
                <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
                    <ProgressTemplate>
                        <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
                            <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." style="padding: 10px; padding-top:15%; border-radius:1.5em" />
                        </div>
                    </ProgressTemplate>
                </asp:UpdateProgress>

                <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                    <ContentTemplate>
                        <div class="row 150%">
                            <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>
                            <div class="8u 12u$(medium)">
                                <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" style="float:left" onclick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                                <asp:LinkButton ID="lbtnBack" runat="server" class="buttonC icon fa-arrow-left" style="float:left" onclick="lbtnBack_Click">&nbsp;Stock Counts</asp:LinkButton>
                                <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" Style="float: right;" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton>
                                <asp:LinkButton ID="lbtnDownload" runat="server" class="buttonC icon fa-arrow-down" style="float:right" onclick="lbtnDownload_Click">&nbsp;Download</asp:LinkButton>
                                <br />
                                <h3 style="padding-top: 0; line-height: 1em">Stock Count Variances</h3>
                            </div>
                            <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server" style="float:right" class="logoImg" /></div>
                        </div>

                        <div class="row 150%">
                            <div class="1u 12u$(medium)" style="text-align:center">&nbsp</div>
                            <div class="10u 12u$(medium)" style="text-align:center">
                                <table style="width:100%">
                                    <tr>
                                        <td>Ref *</td>
                                        <td style="text-align: left"><asp:TextBox ID="txtRef" runat="server" placeholder="Reference"></asp:TextBox></td>
                                        <td>Created Date</td>
                                        <td><asp:Label ID="lblDate" runat="server" Text=""></asp:Label>&nbsp;</td>
                                        <td>Created By<asp:Label ID="CntID" runat="server" Text="" style="display:none"></asp:Label></td>
                                        <td><asp:Label ID="lblCreatedBy" runat="server" Text="">&nbsp;</asp:Label></td>
                                        <td>&nbsp<asp:Label ID="lblCountID" runat="server" Text="" style="display:none"></asp:Label></td>
                                    </tr>
                                    <tr>
                                        <td colspan="7"><hr /></td>
                                    </tr>
                                    <tr>
                                        <td>Category</td>
                                        <td style="text-align: left"><asp:DropDownList ID="DDCateg" runat="server" Style="width: 10em" AutoPostBack="true" OnSelectedIndexChanged="DDCateg_SelectedIndexChanged"></asp:DropDownList></td>
                                        <td style="padding-left: 2em">Filter</td>
                                        <td>
                                            <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnSearch">
                                                <asp:TextBox ID="txtFilter" runat="server" Style="width: 10em" placeholder="Code/Description"></asp:TextBox>
                                                <asp:LinkButton ID="lbtnSearch" runat="server" CssClass="icon fa-search buttonC" ToolTip="Search" OnClick="lbtnSearch_Click"></asp:LinkButton>
                                            </asp:Panel>
                                        </td>
                                        <td colspan="3"></td>
                                    </tr>
                                </table>
                                <hr />
                            </div>
                            <div class="1u 12u$(medium)" style="text-align:center">&nbsp</div>
                        </div>

                        <div class="row 150%">
                            <div class="1u 12u$(medium)" style="text-align:center">&nbsp</div>
                            <div class="10u 12u$(medium)" style="text-align:center">   
                                <asp:GridView ID="GridItems" runat="server" AutoGenerateColumns="false" CssClass="gridview" AllowSorting="true" OnSorting="GridCntLines_Sorting" OnRowDataBound="GridCntLines_RowDataBound">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="5" />
                                    <Columns>
                                        <asp:BoundField DataField="ItemCode" HeaderText="Code" SortExpression="ItemCode" />
                                        <asp:BoundField DataField="ItemDescription" HeaderText="Item" SortExpression="ItemDescription" />
                                        <asp:BoundField DataField="CategoryDescript" HeaderText="Category" SortExpression="CategoryDescript" />
                                        <asp:BoundField DataField="SystemQOH" HeaderText="System" SortExpression="SystemQOH" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
                                        <asp:BoundField DataField="TotalCountedQty" HeaderText="Counted" SortExpression="TotalCountedQty"  DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right"  HeaderStyle-HorizontalAlign="Right" />
                                        <asp:BoundField DataField="Variance" HeaderText="Variance" SortExpression="Variance"  DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right"  HeaderStyle-HorizontalAlign="Right" />
                                        <asp:TemplateField HeaderText="Status" ItemStyle-HorizontalAlign="Center">
                                            <ItemTemplate>
                                                <asp:Label ID="lblStatus" runat="server"></asp:Label>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                            </div>
                            <div class="1u 12u$(medium)" style="text-align:center">&nbsp</div>
                        </div>
                    </ContentTemplate>
                    <Triggers>
                        <asp:PostBackTrigger ControlID="lbtnDownload" />
                    </Triggers>

                </asp:UpdatePanel>
            </div>
        </div>
    </form>
</body>
</html>