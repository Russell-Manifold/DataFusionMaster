<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="WorksOrdersRMEdit.aspx.cs" Inherits="SBMS.WorksOrdersRMEdit" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Edit Components</title>
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
                        <asp:LinkButton ID="lbtnBack" runat="server" class="buttonC icon fa-arrow-left" OnClick="lbtnBack_Click" ToolTip="Return to the works order without saving">&nbsp;Works Order</asp:LinkButton>
                        <h3 id="woheader" runat="server" style="padding-top:1em; line-height:1em"></h3>
                    </div>
                    <div class="2u 12u$(medium)"><img src="images/SageAcctLogo.jpg" style="float:right" class="logoImg" /></div>
                </div>
                <div class="row 150%">
                    <div class="1u 12u$(medium)">&nbsp;</div>
                    <div class="10u 12u$(medium)">
                        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                            <ContentTemplate>
                                <asp:Panel ID="Panel1" runat="server">
                                    <table style="width: 100%">
                                        <tr>
                                            <td style="width:12em">Making:</td>
                                            <td><asp:Label ID="lblFGCode" runat="server" Text=""></asp:Label></td>
                                            <td style="width:8em">Quantity:</td>
                                            <td><asp:Label ID="lblFGQty" runat="server" Text=""></asp:Label></td>
                                            <td style="width:8em">Due Date:</td>
                                            <td><asp:Label ID="lblDueDate" runat="server" Text=""></asp:Label></td>
                                        </tr>
                                        <tr>
                                            <td>Description:</td>
                                            <td colspan="5"><asp:Label ID="lblFGDescription" runat="server" Text=""></asp:Label></td>
                                        </tr>
                                    </table>
                                    <hr />
                                </asp:Panel>

                                <h3>Components <asp:Label ID="lblCustomised" runat="server" Text="" Font-Size="Small" ForeColor="#4282C1"></asp:Label></h3>
                                <span style="font-size:x-small">Quantities here are the totals for the whole works order line, not per unit. Editing them leaves the BOM/Kit master untouched, and stops this line being rebuilt from it.</span>
                                <br /><br />

                                <asp:GridView ID="GridRMLines" runat="server" AutoGenerateColumns="false" CssClass="gridview" OnRowDataBound="GridRMLines_RowDataBound" ShowFooter="false">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <Columns>
                                        <asp:BoundField DataField="LineID" />
                                        <asp:TemplateField HeaderText="Type" ItemStyle-Width="9em">
                                            <ItemTemplate>
                                                <asp:DropDownList ID="DDLineType" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDLineType_SelectedIndexChanged" Width="100px">
                                                    <asp:ListItem Value="1">Item</asp:ListItem>
                                                    <asp:ListItem Value="2">From BOM</asp:ListItem>
                                                    <asp:ListItem Value="3">From KIT</asp:ListItem>
                                                    <asp:ListItem Value="4">Service</asp:ListItem>
                                                    <asp:ListItem Value="5">GL Account</asp:ListItem>
                                                </asp:DropDownList>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Item Code" ItemStyle-Width="10em">
                                            <ItemTemplate>
                                                <asp:DropDownList ID="DDItemCode" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDItemCode_SelectedIndexChanged" Width="100px"></asp:DropDownList>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Description" ItemStyle-Width="100%">
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtDescription" runat="server" Text='<%# Eval("ItemDescription") %>' Width="100%"></asp:TextBox>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Unit" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="5em">
                                            <ItemTemplate>
                                                <asp:Label ID="lblUnit" runat="server" Text='<%# Eval("Unit") %>'></asp:Label>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Qty" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="7em">
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtQty" runat="server" Width="6em" Style="text-align:center" Text='<%# Eval("Quantity") %>'></asp:TextBox>
                                                <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtQty" FilterType="Custom, Numbers" ValidChars="." />
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="On Hand" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="7em">
                                            <ItemTemplate><asp:Label ID="lblOnHand" runat="server"></asp:Label></ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Short" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="7em">
                                            <ItemTemplate><asp:Label ID="lblShort" runat="server"></asp:Label></ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="lbtnLineSave" CommandArgument='<%# Eval("LineID") %>' runat="server" CssClass="fa fa-save buttonRed" ToolTip="Save this component" OnClick="lbtnLineSave_Click"> </asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="2em">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="lbtnDeleteLine" CommandArgument='<%# Eval("LineID") %>' runat="server" CssClass="fa fa-ban" ToolTip="Remove this component" OnClick="lbtnDeleteLine_Click" Style="color:red"> </asp:LinkButton>
                                                <cci:ConfirmButtonExtender ID="cbeDel" runat="server" ConfirmText="Remove this component from the works order?" Enabled="True" TargetControlID="lbtnDeleteLine"></cci:ConfirmButtonExtender>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>

                                <asp:Label ID="lblLocked" runat="server" Text="" ForeColor="Firebrick" Font-Bold="true"></asp:Label>

                                <div style="text-align:center">
                                    <asp:LinkButton ID="lbtnRebuild" runat="server" class="buttonRed icon fa-refresh" OnClick="lbtnRebuild_Click" Style="margin-right:2em; margin-top:2em" ToolTip="Discard these components and rebuild them from the BOM/Kit master">&nbsp;Reset to BOM</asp:LinkButton>
                                    <cci:ConfirmButtonExtender ID="cbeRebuild" runat="server" ConfirmText="Discard the edited components and rebuild this line from the BOM/Kit master?" Enabled="True" TargetControlID="lbtnRebuild"></cci:ConfirmButtonExtender>
                                    <asp:LinkButton ID="lbtnSave" runat="server" Style="font-size:1em; margin-top:2em; margin-right:2em;" CssClass="icon fa-save buttonSage" OnClick="lbtnSave_Click"> Save Components</asp:LinkButton>
                                </div>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </div>
                    <div class="1u 12u$(medium)">&nbsp;</div>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
