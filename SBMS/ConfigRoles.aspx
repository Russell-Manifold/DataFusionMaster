<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="ConfigRoles.aspx.cs" Inherits="SBMS.ConfigRoles" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Roles Settings</title>
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
                          <asp:LinkButton ID="LbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click" >&nbsp;&nbsp;</asp:LinkButton>
                          <asp:LinkButton ID="LbtnConfig" runat="server" class="buttonC icon fa-gears" PostBackUrl="~/ConfigMaster.aspx" > &nbsp;Settings</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton>
                        <br />
                        <h2 style="padding-top:0; line-height:1em">Company Roles Setup and Authorisations</h2>                       
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
                 <div class="row 150%">
                        <div class="12u 12u$(medium)" style="text-align:center">
                             <asp:LinkButton ID="lbtnAddRole" runat="server" style="float:right" CssClass="icon fa-user-plus buttonRed"> Add New Role</asp:LinkButton>
                            <asp:GridView ID="GridRoles" runat="server" AutoGenerateColumns="false" CssClass="gridview"  ToolTip="Open Role" OnRowDataBound="GridRoles_RowDataBound"  OnSelectedIndexChanged="GridRoles_SelectedIndexChanged" >
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <Columns>
                                        <asp:BoundField DataField="RoleID"  />
                                        <asp:BoundField HeaderText="Role" DataField="RoleName" ReadOnly="True"  />
                                       <asp:TemplateField HeaderText ="Can Receive"  ItemStyle-Width="5em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkCanReceive" runat="server" Checked='<%# Eval("CanReceive") %>' Enabled="false" />
                                           </ItemTemplate>
                                       </asp:TemplateField>
                                        <asp:TemplateField HeaderText ="Can Transfer"  ItemStyle-Width="5em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkCanTransfer" runat="server" Checked='<%# Eval("CanTransfer") %>' Enabled="false" />
                                           </ItemTemplate>
                                       </asp:TemplateField>
                                         <asp:TemplateField HeaderText ="Can Create Tax Invoice" ItemStyle-Width="8em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkCanInvoice" runat="server" Checked='<%# Eval("CanInvoice") %>' Enabled="false" />
                                           </ItemTemplate>
                                       </asp:TemplateField>    
                                        <asp:TemplateField HeaderText ="Notify GRN" ItemStyle-Width="3em"  ItemStyle-BackColor="#dfffdf" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkNotifyGRN" runat="server" Checked='<%# Eval("NotifyGRN") %>' Enabled="false" />
                                           </ItemTemplate>
                                       </asp:TemplateField>
                                        <asp:TemplateField HeaderText ="Notify Item Transfer" ItemStyle-Width="8em" ItemStyle-BackColor="#dfffdf" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkNotifyTransfer" runat="server" Checked='<%# Eval("NotifyTransfer") %>' Enabled="false" />
                                           </ItemTemplate>
                                       </asp:TemplateField>
                                    <asp:TemplateField HeaderText ="Notify Pick Slip Move" ItemStyle-Width="8em" ItemStyle-BackColor="#dfffdf" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkNotifyPSMove" runat="server" Checked='<%# Eval("NotifyPSMove") %>' Enabled="false" />
                                           </ItemTemplate>
                                       </asp:TemplateField>
                                        <asp:TemplateField HeaderText ="Notify Job Card Move" ItemStyle-Width="8em" ItemStyle-BackColor="#dfffdf" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkNotifyJCMove" runat="server" Checked='<%# Eval("NotifyJCMove") %>' Enabled="false" />
                                           </ItemTemplate>
                                       </asp:TemplateField>
                                         <asp:TemplateField HeaderText ="Notify Of New Sales Order" ItemStyle-Width="8em" ItemStyle-BackColor="#dfffdf" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkNotifyNewSO" runat="server" Checked='<%# Eval("NotifyNewSO") %>' Enabled="false" />
                                           </ItemTemplate>
                                       </asp:TemplateField>
                                         <asp:TemplateField HeaderText ="Notify Of Sales Order Complete" ItemStyle-Width="8em" ItemStyle-BackColor="#dfffdf" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkNotifyPSComplete" runat="server" Checked='<%# Eval("NotifySOComplete") %>' Enabled="false" />
                                           </ItemTemplate>
                                       </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Picker" ItemStyle-Width="8em" ItemStyle-BackColor="#dfffdf" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkPicker" runat="server" Checked='<%# Eval("IsPicker") %>' Enabled="false" />
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Job Cards" ItemStyle-Width="8em" ItemStyle-BackColor="#dfffdf" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkJobCards" runat="server" Checked='<%# Eval("IsJobCards") %>' Enabled="false" />
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                    <%--<asp:TemplateField HeaderText ="Use Generic Login" ItemStyle-Width="8em" ItemStyle-BackColor="Wheat" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkUseGenericLogin" runat="server" Checked='<%# Eval("UseGenericLogin") %>' Enabled="false"  />
                                           </ItemTemplate>
                                       </asp:TemplateField>--%>
                                    </Columns>
                                </asp:GridView>
                            </div>

         <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="lbtnAddRole"></cci:ModalPopupExtender>
            <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                        <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" OnClick="lbtnCancel_Click" > </asp:LinkButton>
                        <div class="HellowWorldPopup">
                            <div id="Div4" class="PopupHeader">
                                <h4>Add/Edit Roles<asp:Label ID="lblTpe" runat="server" Text=""></asp:Label></h4>
                            </div>
                            <div class="PopupBody" style="text-align:center">
                                <table style="width:450px; margin:auto; font-size:.8em; text-align:left">
                                    <tr>
                                        <td>Role Name *</td>
                                        <td style="padding:.5em"><asp:TextBox ID="txtRoleName" runat="server" Width="250px" style="padding:.5em" ></asp:TextBox><asp:Label ID="lblRoleID" runat="server" Text="" style="display:none"></asp:Label></td>
                                    </tr>
                                   <tr>
                                        <td>Can Receive Stock *</td>
                                        <td style="padding:.5em">    <asp:CheckBox ID="chkCanReceiveS" runat="server" Checked="false" /></td>
                                    </tr>
                                    <tr>
                                        <td>Can Transfer Items *</td>
                                        <td style="padding:.5em">    <asp:CheckBox ID="chkCanTransferS" runat="server" Checked="false" /></td>
                                    </tr>
                                    <tr>
                                        <td>Can Create Tax Invoice *</td>
                                        <td style="padding:.5em">    <asp:CheckBox ID="chkCanInvoiceS" runat="server" Checked="false" /></td>
                                    </tr>
                                    <tr>
                                        <td style="background-color:#dfffdf">Notify On New GRN Created *</td>
                                        <td style="padding:.5em"><asp:CheckBox ID="chkNotifyGRNS" runat="server" Checked="false" /></td>
                                    </tr>
                                    <tr>
                                        <td style="background-color:#dfffdf">Notify On Item Transfer *</td>
                                        <td style="padding:.5em"><asp:CheckBox ID="chkNotifyTransferS" runat="server" Checked="false" /></td>
                                    </tr>
                                     <tr>
                                        <td style="background-color:#dfffdf">Notify On Picking Slip Status Change*</td>
                                        <td style="padding:.5em"><asp:CheckBox ID="chkNotifyPSMoveS" runat="server" Checked="false" /></td>
                                    </tr>
                                     <tr>
                                        <td style="background-color:#dfffdf">Notify On Job Card Status Change*</td>
                                        <td style="padding:.5em"><asp:CheckBox ID="chkNotifyJCMoveS" runat="server" Checked="false" /></td>
                                    </tr>
                                    <tr>
                                        <td style="background-color:#dfffdf">Notify On New SO Detected *</td>
                                        <td style="padding:.5em"><asp:CheckBox ID="chkNotifyNewSOS" runat="server" Checked="false" /></td>
                                    </tr>
                                     <tr>
                                        <td style="background-color:#dfffdf">Notify SO (Picking) Complete *</td>
                                        <td style="padding:.5em"><asp:CheckBox ID="chkNotifyPSCompleteS" runat="server" Checked="false" /></td>
                                    </tr>
                                </table>
                               </div>
                            <div class="Controls">
                                <input id="btnCancel5" type="button" class="fa fa-times-circle" value="" runat="server" style="display:none"/>
                                <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                <asp:LinkButton ID="btnSaveConfirm" runat="server" CssClass="icon fa-save buttonCancel" OnClick="btnSaveConfirm_Click" > Save User</asp:LinkButton><br />
                            </div>
                        </div>
                    </asp:Panel>
                     </div>
                </div>
        </div>
    </form>
</body>
</html>
