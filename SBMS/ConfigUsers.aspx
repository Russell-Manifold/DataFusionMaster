<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="ConfigUsers.aspx.cs" Inherits="SBMS.ConfigUsers" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>User Settings</title>
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
                        <h2 style="padding-top:0; line-height:1em">User Settings & Authorisations</h2>                       
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
                 <div class="row 150%">
                        <div class="12u 12u$(medium)" style="text-align:center">    
                            <asp:LinkButton ID="lbtnAddUser" runat="server" style="float:right" CssClass="icon fa-user-plus buttonRed"> Add New User</asp:LinkButton>
                            <asp:GridView ID="GridUsers" runat="server" AutoGenerateColumns="false" CssClass="gridview"  ToolTip="Open User" OnRowDataBound="GridUsers_RowDataBound"  OnSelectedIndexChanged="GridUsers_SelectedIndexChanged">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <Columns>
                                        <asp:BoundField DataField="ID"  />
                                        <asp:BoundField HeaderText="FirstName" DataField="FirstName" ReadOnly="True"  />
                                       <asp:BoundField HeaderText="email" DataField="Useremail" ReadOnly="True"  />
                                        <asp:BoundField HeaderText="Role" DataField="RoleDescript" ReadOnly="True"  />
                                       <asp:TemplateField HeaderText ="Active">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkActive" runat="server" Checked='<%# Eval("Active") %>' Enabled="false" />
                                           </ItemTemplate>
                                       </asp:TemplateField>
                                        <asp:BoundField HeaderText="Last Activity" DataField="LastActivity" ReadOnly="True" />
                                    </Columns>
                                </asp:GridView>
                         </div>

          <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="lbtnAddUser"></cci:ModalPopupExtender>
            <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                        <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" OnClick="lbtnCancel_Click" > </asp:LinkButton>
                        <div class="HellowWorldPopup">
                            <div id="Div4" class="PopupHeader">
                                <h4>Add/Edit New User<asp:Label ID="lblTpe" runat="server" Text=""></asp:Label></h4>
                            </div>
                            <div class="PopupBody" style="text-align:center">
                                <table style="width:450px; margin:auto; font-size:.8em; text-align:left">
                                    <tr>
                                        <td>Users Name *</td>
                                        <td style="padding:.5em"><asp:TextBox ID="txtUsername" runat="server" Width="250px" style="padding:.5em" ></asp:TextBox><asp:Label ID="lbluserID" runat="server" Text="" style="display:none"></asp:Label></td>
                                    </tr>
                                    <tr>
                                        <td>Sage User Name (email address) *</td>
                                        <td style="padding:.5em"><asp:TextBox ID="txtemail" runat="server" Width="250px" style="padding:.5em" ></asp:TextBox></td>
                                    </tr>
                                    <tr>
                                        <td >Sage Password *</td>
                                        <td style="padding:.5em"><asp:TextBox ID="txtPwd1" runat="server" Width="250px" ></asp:TextBox></td>
                                    </tr>
                                     <tr>
                                        <td>Confirm Password *</td>
                                        <td style="padding:.5em"><asp:TextBox ID="txtPwd2" runat="server" Width="250px" ></asp:TextBox></td>
                                    </tr> 
                                    <tr>
                                        <td>Role *</td>
                                        <td style="padding:.5em"> <asp:DropDownList ID="DDRole" runat="server" Width="250px"></asp:DropDownList></td>
                                    </tr>
                                    <tr>
                                        <td>Is a Super User</td>
                                        <td> <asp:CheckBox ID="chkSuperUser" runat="server" Text=" " Checked="false" /></td>
                                    </tr>
                                     <tr>
                                        <td>Active</td>
                                        <td> <asp:CheckBox ID="chkIsActive" runat="server" Text=" " Checked="true" /></td>
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
