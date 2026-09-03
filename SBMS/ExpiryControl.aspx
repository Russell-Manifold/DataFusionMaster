<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="ExpiryControl.aspx.cs" Inherits="SBMS.ExpiryControl" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Expiry Control</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <style>
        .exp-tiles  { display:flex; gap:1em; justify-content:center; flex-wrap:wrap; margin-bottom:1em; }
        .exp-tile   { border:1px solid #cfd8df; border-radius:.4em; padding:.6em 1.2em; min-width:9em; background:#fff; }
        .exp-tile b { display:block; font-size:1.6em; line-height:1.2em; }
        .exp-red    { border-color:#e6b0aa; background:#fdedec; color:#c0392b; }
        .exp-amber  { border-color:#e6d0a0; background:#fdf6e3; color:#b9770e; }
        .exp-green  { border-color:#a9dfbf; background:#eafaf1; color:#1e8449; }
        .exp-filter { margin-bottom:1em; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="content">
            <div class="container">
                <div class="row 150%">
                    <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>
                    <div class="8u 12u$(medium)">
                        <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" OnClick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="lbtnTrace" runat="server" class="buttonC icon fa-search" PostBackUrl="~/Traceability.aspx">&nbsp;Traceability</asp:LinkButton>
                        <asp:LinkButton ID="lbtnExcel" runat="server" class="buttonC icon fa-download" OnClick="lbtnExcel_Click">&nbsp;Excel</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right;" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <h3 style="padding-top:0; line-height:1em">Expiry Control</h3>
                        <p style="margin-top:.2em; color:#666; font-size:.9em">
                            Stock still on hand, soonest expiry first. Anything already expired is at the top.</p>
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server" style="float:right" class="logoImg" /></div>
                </div>

                <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                    <ContentTemplate>
                        <div class="row 150%">
                            <div class="12u 12u$(medium)" style="text-align:center">
                                <div class="exp-tiles">
                                    <div class="exp-tile exp-red"><b><asp:Label ID="lblExpired" runat="server" Text="0"></asp:Label></b>Expired</div>
                                    <div class="exp-tile exp-amber"><b><asp:Label ID="lblShort" runat="server" Text="0"></asp:Label></b>Short dated</div>
                                    <div class="exp-tile exp-green"><b><asp:Label ID="lblOk" runat="server" Text="0"></asp:Label></b>In date</div>
                                    <div class="exp-tile"><b><asp:Label ID="lblNoDate" runat="server" Text="0"></asp:Label></b>No expiry set</div>
                                </div>

                                <div class="exp-filter">
                                    Show
                                    <asp:DropDownList ID="ddlView" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlView_SelectedIndexChanged">
                                        <asp:ListItem Value="all" Text="everything on hand" />
                                        <asp:ListItem Value="expired" Text="expired only" />
                                        <asp:ListItem Value="short" Text="expired and short dated" Selected="True" />
                                        <asp:ListItem Value="nodate" Text="no expiry set" />
                                    </asp:DropDownList>
                                    &nbsp; Short dated =
                                    <asp:TextBox ID="txtDays" runat="server" Text="30" Width="4em" style="text-align:center"
                                                 AutoPostBack="true" OnTextChanged="txtDays_TextChanged"></asp:TextBox> days
                                    <cci:FilteredTextBoxExtender ID="ftbeDays" runat="server" TargetControlID="txtDays" FilterType="Numbers" />
                                    &nbsp; Store
                                    <asp:DropDownList ID="ddlStore" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlStore_SelectedIndexChanged"></asp:DropDownList>
                                </div>
                                <h4><asp:Label ID="lblErr" runat="server" ForeColor="Red"></asp:Label></h4>
                            </div>
                        </div>

                        <div class="row 150%">
                            <div class="1u 12u$(medium)">&nbsp;</div>
                            <div class="10u 12u$(medium)" style="text-align:center">
                                <asp:GridView ID="GridExpiry" runat="server" CssClass="gridview" AutoGenerateColumns="false" Width="100%"
                                              AllowPaging="true" PageSize="50" OnPageIndexChanging="GridExpiry_PageIndexChanging"
                                              OnRowDataBound="GridExpiry_RowDataBound">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="10" />
                                    <Columns>
                                        <asp:TemplateField HeaderText="Serial / Lot">
                                            <ItemTemplate>
                                                <asp:HyperLink ID="lnkTrace" runat="server"
                                                     NavigateUrl='<%# "~/Traceability.aspx?s=" + Server.UrlEncode(Convert.ToString(Eval("LotNumber"))) %>'
                                                     Text='<%# Eval("LotNumber") %>' ToolTip="Open in Traceability"></asp:HyperLink>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Batch" DataField="Batch" />
                                        <asp:BoundField HeaderText="Item" DataField="ItemCode" />
                                        <asp:BoundField HeaderText="Description" DataField="ItemDescription" />
                                        <asp:BoundField HeaderText="Store" DataField="StoreCode" ItemStyle-HorizontalAlign="Center" />
                                        <asp:BoundField HeaderText="On Hand" DataField="OnHand" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Center" />
                                        <asp:BoundField HeaderText="Expires" DataField="UseByDate" DataFormatString="{0:dd MMM yyyy}" ItemStyle-HorizontalAlign="Center" />
                                        <asp:BoundField HeaderText="Days" DataField="DaysLeft" ItemStyle-HorizontalAlign="Center" />
                                    </Columns>
                                    <EmptyDataTemplate>Nothing to show for this filter.</EmptyDataTemplate>
                                </asp:GridView>
                            </div>
                            <div class="1u 12u$(medium)">&nbsp;</div>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
        </div>
    </form>
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</body>
</html>
