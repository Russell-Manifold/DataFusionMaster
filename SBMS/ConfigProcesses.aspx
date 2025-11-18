<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ConfigProcesses.aspx.cs" Inherits="SBMS.ConfigProcesses" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Company</title>
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
                        <h2 style="padding-top:0; line-height:1em">Internal Processes Configuration</h2>                       
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
                 <div class="row 150%">
                     <div class="6u 12u$(medium)">
                         <asp:Panel ID="PnlPickProcesses" runat="server">
                              <h4>Picking Processes</h4>
                              <asp:LinkButton ID="lbtnAddPProc" runat="server" style="float:right" CssClass="icon fa-plus-square buttonRed"> Add New</asp:LinkButton>
                            <asp:GridView ID="GridPickProc" runat="server" AutoGenerateColumns="false" CssClass="gridview"  ToolTip="Open User" OnRowDataBound="GridPickProc_RowDataBound"  OnSelectedIndexChanged="GridPickProc_SelectedIndexChanged">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <Columns>
                                        <asp:BoundField DataField="PSPID"  />
                                        <asp:BoundField HeaderText="Process" DataField="PSName" ReadOnly="True"  />
                                       <asp:BoundField HeaderText="Seqence" DataField="Seq" ReadOnly="True"  />
                                         <asp:TemplateField HeaderText ="Active">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkPSPActive" runat="server" Checked='<%# Eval("PSActive") %>' Enabled="false" />
                                           </ItemTemplate>
                                       </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                         </asp:Panel> 
                     </div>   
                     <div class="6u 12u$(medium)" style="text-align:center">  
                           <asp:Panel ID="PnlJCProcesses" runat="server">
                             <h4>Job Card/Production Processes</h4>
                               <asp:LinkButton ID="lbtnAddJCProc" runat="server" style="float:right" CssClass="icon fa-plus-square buttonRed"> Add New</asp:LinkButton>
                               <asp:GridView ID="GridJCProc" runat="server" AutoGenerateColumns="false" CssClass="gridview"  ToolTip="Open User" OnRowDataBound="GridJCProc_RowDataBound"  OnSelectedIndexChanged="GridJCProc_SelectedIndexChanged">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <Columns>
                                        <asp:BoundField DataField="WSID"  />
                                        <asp:BoundField HeaderText="Process" DataField="WSName" ReadOnly="True"  />
                                        <asp:TemplateField HeaderText ="IsWIP">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkWIP" runat="server" Checked='<%# Eval("IsWIP") %>' Enabled="false" />
                                           </ItemTemplate>
                                       </asp:TemplateField>
                                       <asp:BoundField HeaderText="Sequence" DataField="Seq" ReadOnly="True"  />
                                         <asp:TemplateField HeaderText ="Active">
                                           <ItemTemplate>
                                               <asp:CheckBox ID="chkWSActive" runat="server" Checked='<%# Eval("WSActive") %>' Enabled="false" />
                                           </ItemTemplate>
                                       </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                               </asp:Panel> 
                        </div>
                     </div>

                <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="lbtnAddPProc"></cci:ModalPopupExtender>
                        <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                                    <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" OnClick="lbtnCancel_Click" > </asp:LinkButton>
                                    <div class="HellowWorldPopup">
                                        <div id="Div4" class="PopupHeader">
                                            <h4>Add/Edit Picking Process</h4>
                                        </div>
                                        <div class="PopupBody" style="text-align:center">
                                            <table style="width:450px; margin:auto; font-size:.8em; text-align:left">
                                                <tr>
                                                    <td>Process *</td>
                                                    <td style="padding:.5em"><asp:TextBox ID="txtprocess" runat="server" Width="250px" style="padding:.5em" ></asp:TextBox><asp:Label ID="lblProcID" runat="server" Text="" style="display:none"></asp:Label></td>
                                                </tr>
                                               <tr>
                                                    <td>Sequence Number *</td>
                                                    <td style="padding:.5em"> <asp:TextBox ID="txtseq" runat="server" Width="20px" ></asp:TextBox></td>
                                                </tr>
                                                <tr>
                                                    <td>Active *</td>
                                                    <td style="padding:.5em">
                                                        <asp:CheckBox ID="chkActive" runat="server" /></td>
                                                </tr>
                                            </table>
                                           </div>
                                        <div class="Controls">
                                            <input id="btnCancel5" type="button" class="fa fa-times-circle" value="" runat="server" style="display:none"/>
                                            <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                            <asp:LinkButton ID="btnSaveConfirm" runat="server" CssClass="icon fa-save buttonCancel" OnClick="btnSaveConfirm_Click"> Save</asp:LinkButton><br />
                                        </div>
                                    </div>
                                </asp:Panel>

                 <cci:ModalPopupExtender ID="ModalPopupExtender1" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancelJ" Drag="true" OkControlID="btnOkayJ" PopupControlID="PnlConfJ" PopupDragHandleControlID="PopupHeader" TargetControlID="lbtnAddJCProc"></cci:ModalPopupExtender>
                        <asp:Panel ID="PnlConfJ" runat="server" Style="display: none">
                                    <asp:LinkButton ID="lbtnCancelJ" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" OnClick="lbtnCancelJ_Click"> </asp:LinkButton>
                                    <div class="HellowWorldPopup">
                                        <div id="Div5" class="PopupHeader">
                                            <h4>Add/Edit Job Card Process<asp:Label ID="Label1" runat="server" Text=""></asp:Label></h4>
                                        </div>
                                        <div class="PopupBody" style="text-align:center">
                                            <table style="width:450px; margin:auto; font-size:.8em; text-align:left">
                                                <tr>
                                                    <td>Process *</td>
                                                    <td style="padding:.5em"><asp:TextBox ID="txtJCProcess" runat="server" Width="250px" style="padding:.5em" ></asp:TextBox><asp:Label ID="lblJProcID" runat="server" Text="" style="display:none"></asp:Label></td>
                                                </tr>
                                               <tr>
                                                    <td>Sequence Number *</td>
                                                    <td style="padding:.5em"> <asp:TextBox ID="txtJCSeq" runat="server" Width="20px" ></asp:TextBox></td>
                                                </tr>
                                                 <tr>
                                                    <td>Is WIP *</td>
                                                    <td style="padding:.5em">
                                                        <asp:CheckBox ID="chkWIPJC" runat="server" /></td>
                                                </tr>
                                                <tr>
                                                    <td>Active *</td>
                                                    <td style="padding:.5em">
                                                        <asp:CheckBox ID="chkJCActive" runat="server" /></td>
                                                </tr>
                                            </table>
                                           </div>
                                        <div class="Controls">
                                            <input id="btnCancelJ" type="button" class="fa fa-times-circle" value="" runat="server" style="display:none"/>
                                            <input id="btnOkayJ" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                            <asp:LinkButton ID="btnSaveConfirmJ" runat="server" CssClass="icon fa-save buttonCancel" OnClick="btnSaveConfirmJ_Click"> Save</asp:LinkButton><br />
                                        </div>
                                    </div>
                                </asp:Panel>
                </div>
        </div>
    </form>
</body>
</html>
