<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="ConfigAccounts.aspx.cs" Inherits="SBMS.ConfigAccounts" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>GL Account Settings</title>
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
                        <asp:LinkButton ID="LbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="LbtnConfig" runat="server" class="buttonC icon fa-gears" PostBackUrl="~/ConfigMaster.aspx"> &nbsp;Settings</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" Style="float: right" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton>
                        <br />
                        <h2 style="padding-top: 0; line-height: 1em">GL Account Access</h2>
                        <p style="margin-top:.3em; color:#666">Choose which GL accounts may be used, and where.
                            Only <b>one</b> account can be the Stock Adjustment Account.</p>
                    </div>
                    <div class="2u 12u$(medium)">
                        <asp:Image ID="imgCoImg" runat="server" Style="float: right" class="logoImg" /></div>
                </div>
                <div class="row 150%">
                    <div class="3u 12u$(medium)" style="text-align: center">&nbsp;</div>
                    <div class="6u 12u$(medium)" style="text-align: center">
                        <asp:GridView ID="GridAccounts" runat="server" AutoGenerateColumns="false" CssClass="gridview" >
                            <HeaderStyle CssClass="gridViewHeader" />
                            <FooterStyle CssClass="gridViewHeader" />
                            <RowStyle CssClass="gridViewRow" />
                            <AlternatingRowStyle CssClass="gridViewAltRow" />
                            <PagerStyle CssClass="gridViewPager" />
                            <Columns>
                                <asp:BoundField HeaderText="Acct ID" DataField="AccountID"
                                                ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left" />
                                <asp:BoundField HeaderText="Name" DataField="AccountName" ReadOnly="True"
                                                ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left" />
                                <asp:BoundField HeaderText="Category" DataField="AcctCategDescr" ReadOnly="True"
                                                ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left" />
                                <asp:TemplateField HeaderText="Job Cards"
                                                   ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center"
                                                   ItemStyle-Width="110px" HeaderStyle-Width="110px">
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkJCUse" runat="server" Checked='<%# Eval("JCUse") %>' AutoPostBack="true" OnCheckedChanged="chkJCUse_CheckedChanged" ToolTip="Allow this account to be selected on Job Cards" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                 <asp:TemplateField HeaderText="Additional Costs"
                                                    ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center"
                                                    ItemStyle-Width="140px" HeaderStyle-Width="140px">
                                     <ItemTemplate>
                                         <asp:CheckBox ID="chkADCUse" runat="server" Checked='<%# Eval("AccountAddCosts") %>' AutoPostBack="true" OnCheckedChanged="chkADCUse_CheckedChanged" ToolTip="Allow this account to be chosen for additional costs on receiving, and on a BOM" />
                                     </ItemTemplate>
                                 </asp:TemplateField>
                                 <%-- Debit side of the BOM additional-costs journal: the account Sage
                                      posts stock adjustments against. ONE per company. --%>
                                 <asp:TemplateField HeaderText="Stock Adjustment Account"
                                                    ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center"
                                                    ItemStyle-Width="170px" HeaderStyle-Width="170px">
                                     <ItemTemplate>
                                         <asp:CheckBox ID="chkADCContra" runat="server" Checked='<%# Eval("AccountAddCostsContra") %>' AutoPostBack="true" OnCheckedChanged="chkADCContra_CheckedChanged" ToolTip="The account Sage posts stock adjustments to. Only one per company - ticking another clears this one." />
                                     </ItemTemplate>
                                 </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                    <div class="3u 12u$(medium)" style="text-align: center">&nbsp;</div>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
