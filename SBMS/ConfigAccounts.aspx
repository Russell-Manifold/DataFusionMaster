<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ConfigAccounts.aspx.cs" Inherits="SBMS.ConfigAccounts" %>
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
                        <h2 style="padding-top: 0; line-height: 1em">Select which GL Accouts can be used for Sales Orders.</h2>
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
                                <asp:BoundField HeaderText="Acct ID " DataField="AccountID" />
                                <asp:BoundField HeaderText="Name" DataField="AccountName" ReadOnly="True" />
                                <asp:BoundField HeaderText="Category" DataField="AcctCategDescr" ReadOnly="True" />
                                <asp:TemplateField HeaderText ="Use On Job Cards" ItemStyle-HorizontalAlign="Center">
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkJCUse" runat="server" Checked='<%# Eval("JCUse") %>' AutoPostBack="true" OnCheckedChanged="chkJCUse_CheckedChanged" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                 <asp:TemplateField HeaderText ="Receiving Additional Costs" ItemStyle-HorizontalAlign="Center">
                                     <ItemTemplate>
                                         <asp:CheckBox ID="chkADCUse" runat="server" Checked='<%# Eval("AccountAddCosts") %>' AutoPostBack="true" OnCheckedChanged="chkADCUse_CheckedChanged" />
                                     </ItemTemplate>
                                 </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                    <div class="3u 12u$(medium)" style="text-align: center">&nbsp;</div>
                   <%-- <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="lbtnDelMethod"></cci:ModalPopupExtender>
                    <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                        <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" Style="float: right" ToolTip="Cancel" OnClick="lbtnCancel_Click"> </asp:LinkButton>
                        <div class="HellowWorldPopup">
                            <div id="Div4" class="PopupHeader">
                                <h4>Add Delivery Method<asp:Label ID="lblTpe" runat="server" Text=""></asp:Label></h4>
                            </div>
                            <div class="PopupBody" style="text-align: center">
                                <table style="width: 450px; margin: auto; font-size: .8em; text-align: left">
                                    <tr>
                                        <td>Delivery Method *</td>
                                        <td style="padding: .5em">
                                            <asp:TextBox ID="txtDelMName" runat="server" Width="250px" Style="padding: .5em"></asp:TextBox><asp:Label ID="lblDelMID" runat="server" Text="" Style="display: none"></asp:Label></td>
                                    </tr>
                                </table>
                            </div>
                            <div class="Controls">
                                <input id="btnCancel5" type="button" class="fa fa-times-circle" value="" runat="server" style="display: none" />
                                <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display: none" />
                                <asp:LinkButton ID="btnSaveConfirm" runat="server" CssClass="icon fa-save buttonCancel" OnClick="btnSaveConfirm_Click"> Save</asp:LinkButton><br />
                            </div>
                        </div>
                    </asp:Panel>--%>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
