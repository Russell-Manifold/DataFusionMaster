<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="BOMComponentLinks.aspx.cs" Inherits="SBMS.BOMComponentLinks" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>BOMs Using Item</title>
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
                    <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg" /></a></div>
                    <div class="8u 12u$(medium)">
                        <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" OnClick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="lbtnBack" runat="server" class="buttonC icon fa-arrow-left" OnClick="lbtnBack_Click">&nbsp;Back to Item</asp:LinkButton>
                        <asp:LinkButton ID="lbtnDownload" runat="server" class="buttonC icon fa-download" style="float:right" OnClick="lbtnDownload_Click" ToolTip="Download to Excel">&nbsp;Excel</asp:LinkButton>
                        <h3 style="padding-top:0; line-height:1em">BOMs Using: <asp:Label ID="lblItemCode" runat="server" Text=""></asp:Label></h3>
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server" style="float:right" class="logoImg" /></div>
                </div>
                <div class="row 150%">
                    <div class="1u 12u$(medium)">&nbsp;</div>
                    <div class="10u 12u$(medium)">
                        <asp:GridView ID="GridBOMs" runat="server" AutoGenerateColumns="false" CssClass="gridview" AllowSorting="false"
                            OnRowCommand="GridBOMs_RowCommand" ToolTip="Open BOM">
                            <HeaderStyle CssClass="gridViewHeader" />
                            <RowStyle CssClass="gridViewRow" />
                            <AlternatingRowStyle CssClass="gridViewAltRow" />
                            <Columns>
                                <asp:TemplateField HeaderText="BOM Code" SortExpression="BOMCode">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lbtnBOM" CommandArgument='<%# Eval("BomHID")%>' CommandName="lbtnBOM" runat="server" Text='<%# Eval("BOMCode")%>' ToolTip="View BOM" style="color:#4A82AB; font-weight:600; padding: 0.5em; border:1px solid #4A82AB; border-radius:0.5em"></asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField HeaderText="BOM Description" DataField="BomDescript" ReadOnly="True" />
                                <asp:BoundField HeaderText="Finished Good Code" DataField="FGCode" ReadOnly="True" />
                                <asp:BoundField HeaderText="Finished Good Description" DataField="FGDescript" ReadOnly="True" />
                                <asp:BoundField HeaderText="Qty Used" DataField="RMQty" ReadOnly="True" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                <asp:BoundField HeaderText="Active" DataField="BomActive" ReadOnly="True" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                            </Columns>
                        </asp:GridView>
                    </div>
                    <div class="1u 12u$(medium)">&nbsp;</div>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
