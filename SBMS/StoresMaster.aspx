<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="StoresMaster.aspx.cs" Inherits="SBMS.StoresMaster" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Location Master</title>
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
                        <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click" >&nbsp;&nbsp;</asp:LinkButton>
                         <asp:LinkButton ID="LbtnConfig" runat="server" class="buttonC icon fa-gears" PostBackUrl="~/ConfigMaster.aspx" > &nbsp;Settings</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <h3 style="padding-top:0; line-height:1em">Location Master</h3>
                        <div style="font-size:small;color:#666; width:600px; text-align:center; margin:auto">A location can be anything from a whole warehouse down to a single bin &ndash; 
                            e.g. a warehouse, an area, an aisle, a rack, a shelf or a bin. Define each level you want to track as its own location. 
                            Please see your Company Config for requirements on setting these up.</div>
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
            
                <div class="row 150%">
                    <div class="4u 12u$(medium)">&nbsp;</div>    
                    <div class="4u 12u$(medium)">
                        <asp:LinkButton ID="lbtnAddStore" runat="server" CssClass="fa fa-plus-circle buttonRed"> Add New Location</asp:LinkButton>
                        <asp:GridView ID="GridStores" runat="server" AutoGenerateColumns="false" CssClass="gridview" OnRowDataBound="GridStores_RowDataBound">
                        <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="5" />
                                    <Columns>
                                        <asp:BoundField HeaderText="Code" DataField="StoreCode" ReadOnly="True" />
                                        <asp:BoundField HeaderText="Description" DataField="StoreDescript" ReadOnly="True" />
                                         <asp:TemplateField HeaderText="Allow Receiving" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                         <ItemTemplate>
                                             <asp:CheckBox ID="chkAllowReceive" runat="server" Text=" " Checked='<%# Eval("AllowReceiving") %>' AutoPostBack="true" OnCheckedChanged="chkAllowReceive_CheckedChanged" />
                                         </ItemTemplate>
                                     </asp:TemplateField >
                                         <asp:TemplateField HeaderText="Allow Picking" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                         <ItemTemplate>
                                             <asp:CheckBox ID="chkAllowPick" runat="server" Text=" " Checked='<%# Eval("AllowPicking") %>' AutoPostBack="true" OnCheckedChanged="chkAllowPick_CheckedChanged" />
                                         </ItemTemplate>
                                     </asp:TemplateField>
                                         <asp:TemplateField HeaderText="WIP" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                         <ItemTemplate>
                                             <asp:CheckBox ID="chkIsWip" runat="server" Text=" " Checked='<%# Eval("IsWip") %>' AutoPostBack="true" OnCheckedChanged="chkIsWip_CheckedChanged" />
                                         </ItemTemplate>
                                     </asp:TemplateField>
                                         <asp:TemplateField HeaderText="Reject" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                         <ItemTemplate>
                                             <asp:CheckBox ID="chkIsReject" runat="server" Text=" " Checked='<%# Eval("IsRejectStore") %>' AutoPostBack="true" OnCheckedChanged="chkIsReject_CheckedChanged" />
                                         </ItemTemplate>
                                     </asp:TemplateField>
                                     <asp:TemplateField HeaderText="Active">
                                         <ItemTemplate>
                                             <asp:CheckBox ID="chkActive" runat="server" Text=" " Checked='<%# Eval("StoreActive") %>' AutoPostBack="true" OnCheckedChanged="chkActive_CheckedChanged" />
                                         </ItemTemplate>
                                     </asp:TemplateField>
                                    </Columns>
                            </asp:GridView>
                    
                    </div>
                    <div class="4u 12u$(medium)">&nbsp;</div>    
                    </div>
                </div>
        </div>
      
        <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="lbtnAddStore"></cci:ModalPopupExtender>
            <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                        <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                        <div class="HellowWorldPopup">
                            <div id="Div4" class="PopupHeader">
                                <h4>Add/Edit Store<asp:Label ID="lblTpe" runat="server" Text="" style="width:10em"></asp:Label></h4>
                            </div>
                            <div class="PopupBody" style="text-align:center">
                                <asp:Label ID="lblBinFormat" runat="server" style="display:block; font-size:small; color:#666; margin-bottom:8px"></asp:Label>
                                <table style="width:400px; margin:auto; text-align:left">
                                    <tr>
                                        <td>Code</td>
                                        <td style="text-align:left"><asp:TextBox ID="txtstorecode" runat="server" MaxLength="15" Width="80px" ></asp:TextBox>
                                            <br /><span style="font-size:small">(Max Length = 15 chars)</span>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>Name</td>
                                        <td style="text-align:left"><asp:TextBox ID="txtStoreDesctript" runat="server" MaxLength="50" Width="250px" ></asp:TextBox>
                                              <br /><span  style="font-size:small">(Max Length = 50 chars)</span>
                                        </td>
                                    </tr>
                                     <tr>
                                        <td>Allow Picking</td>
                                        <td style="text-align:left"> <asp:CheckBox ID="chkAllowP" runat="server" Text=" " Checked="true" /></td>
                                    </tr>
                                    <tr>
                                        <td style="width:30em; text-align:left">Allow Receiving</td>
                                        <td style="text-align:left"> <asp:CheckBox ID="chKAllowRec" runat="server" Text=" " Checked="true" /></td>
                                    </tr>
                                </table>
                               </div>
                            <div class="Controls">
                                <input id="btnCancel5" type="button" class="fa fa-times-circle" value="" runat="server" style="display:none"/>
                                <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                <asp:LinkButton ID="btnSaveConfirm" runat="server" CssClass="icon fa-save buttonCancel" OnClick="btnSaveConfirm_Click"> Save Store</asp:LinkButton><br /><br />
                            </div>
                        </div>
                    </asp:Panel>
    </form>
</body>
</html>
