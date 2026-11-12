<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Deliveries.aspx.cs" Inherits="SBMS.Deliveries" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Deliveries</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <style>
        /* main.css styles text boxes but not date boxes - make them match the search box */
        .dlvFilter input { height: 2.2em; border: solid 1px rgba(0,0,0,0.25); border-radius: 4px; padding: 0 0.5em;
                           font-family: inherit; font-size: inherit; color: inherit; background: transparent; outline: 0; vertical-align: middle; }
        .dlvFilter input:focus { border-color: #900000; }
        .dlvFilter .buttonC { vertical-align: middle; }
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
                        <asp:LinkButton ID="lbtnSOs" runat="server" class="buttonC icon fa-list" OnClick="lbtnSOs_Click">&nbsp;Sales Orders</asp:LinkButton>
                        <asp:LinkButton ID="lbtnExcel" runat="server" class="buttonC icon fa-download" OnClick="lbtnExcel_Click">&nbsp;Excel</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right;" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <h3 style="padding-top:0; line-height:1em">Deliveries</h3>
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server" style="float:right" class="logoImg" /></div>
                </div>
                <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                    <ContentTemplate>
                        <div class="row 150%">
                            <div class="12u 12u$(medium) dlvFilter" style="text-align:center">
                                Delivered from <asp:TextBox ID="txtFrom" runat="server" TextMode="Date" style="display:inline-block; width:11em"></asp:TextBox>
                                to <asp:TextBox ID="txtTo" runat="server" TextMode="Date" style="display:inline-block; width:11em"></asp:TextBox>
                                &nbsp; Sales Order or customer <asp:TextBox ID="txtSearch" runat="server" style="display:inline-block; width:14em"></asp:TextBox>
                                <asp:LinkButton ID="lbtnFind" runat="server" class="buttonC icon fa-search" OnClick="lbtnFind_Click">&nbsp;Find</asp:LinkButton>
                                <h4><asp:Label ID="lblInfo" runat="server" Text=""></asp:Label></h4>
                            </div>
                            <asp:Panel ID="pnlPOD" runat="server" Visible="false" CssClass="12u 12u$(medium) dlvFilter" style="text-align:center">
                                <div style="display:inline-block; text-align:left; border:solid 1px rgba(0,0,0,0.25); border-radius:4px; padding:0.75em 1.25em; margin-bottom:1em">
                                    <asp:HiddenField ID="hfPSID" runat="server" />
                                    <strong><asp:Label ID="lblPODSlip" runat="server"></asp:Label></strong><br />
                                    POD number <asp:TextBox ID="txtPOD" runat="server" MaxLength="30" style="display:inline-block; width:12em"></asp:TextBox>
                                    &nbsp; Comment <asp:TextBox ID="txtPODComment" runat="server" MaxLength="500" style="display:inline-block; width:28em"></asp:TextBox>
                                    <asp:LinkButton ID="lbtnPODSave" runat="server" class="buttonC icon fa-save" OnClick="lbtnPODSave_Click">&nbsp;Save</asp:LinkButton>
                                    <asp:LinkButton ID="lbtnPODCancel" runat="server" class="buttonC" OnClick="lbtnPODCancel_Click">Cancel</asp:LinkButton>
                                    <br /><asp:Label ID="lblPODMsg" runat="server" ForeColor="#900000"></asp:Label>
                                </div>
                            </asp:Panel>
                            <div class="12u 12u$(medium)" style="text-align:center">
                                <asp:GridView ID="GridDeliveries" runat="server" CssClass="gridview" AutoGenerateColumns="false" Width="100%" AllowPaging="true" PageSize="50" OnPageIndexChanging="GridDeliveries_PageIndexChanging" OnRowCommand="GridDeliveries_RowCommand" EmptyDataText="No deliveries found for this selection.">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="5" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <Columns>
                                        <asp:TemplateField ItemStyle-Width="1%" ItemStyle-Wrap="false">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="lbtnOpen" runat="server" CommandName="OpenPOD" CommandArgument='<%# Eval("PSID") %>' ToolTip="Add or change the POD number and comment"
                                                    style="color:#4A82AB; font-weight:600; padding: 0.5em; border:1px solid #4A82AB; border-radius:0.5em">Open</asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Delivered" DataField="Delivered" DataFormatString="{0:dd MMM yyyy}" HtmlEncode="false" ItemStyle-Width="7em" />
                                        <asp:BoundField HeaderText="POD No" DataField="POD" ItemStyle-Width="7em" />
                                        <asp:BoundField HeaderText="Received By" DataField="ReceivedBy" ItemStyle-HorizontalAlign="Left" />
                                        <asp:TemplateField HeaderText="Sales Order" ItemStyle-Width="8em">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="lbtnSO" runat="server" CommandName="OpenSO" CommandArgument='<%# Eval("SalesOrderGuid") %>' Text='<%#: Eval("SalesOrder") %>' ToolTip="View Sales Order"
                                                    style="color:#4A82AB; font-weight:600; padding: 0.5em; border:1px solid #4A82AB; border-radius:0.5em"></asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Customer" DataField="Customer" ItemStyle-HorizontalAlign="Left" />
                                        <asp:BoundField HeaderText="Picking Slip" DataField="PickingSlip" ItemStyle-Width="8em" />
                                        <asp:BoundField HeaderText="Description" DataField="Description" ItemStyle-HorizontalAlign="Left" />
                                        <asp:BoundField HeaderText="On Slip" DataField="OnSlip" DataFormatString="{0:N2}" ItemStyle-Width="6em" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
                                        <asp:BoundField HeaderText="Delivered Qty" DataField="DeliveredQty" DataFormatString="{0:N2}" ItemStyle-Width="7em" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
                                    </Columns>
                                </asp:GridView>
                            </div>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
        </div>
    </form>
</body>
</html>
