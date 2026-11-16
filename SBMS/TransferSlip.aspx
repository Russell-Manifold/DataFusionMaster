<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="TransferSlip.aspx.cs" Inherits="SBMS.TransferSlip" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Stock Transfer</title>
<link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
<link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
<link rel="stylesheet" href="assets/css/main.css" />
<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
<style>
    /* Add bar - boxed and labelled so users of the old per-line dropdowns spot where items go now */
    .addbar { max-width: 64em; margin: 1.5em auto 1em; padding: .9em 1.2em 1.1em; border: 2px solid #4A82AB; border-radius: .6em; background: #f3f8fc; }
    .addbar-title { font-weight: 600; color: #4A82AB; }
    .addbar-hint { font-size: .8em; color: #555; margin-bottom: .7em; }
    .addbar table { width: calc(100% + 2em); margin: 0 -1em; border-collapse: separate; border-spacing: 1em 0; }   /* -1em: edges line up with the box text */
    .addbar td { vertical-align: bottom; text-align: center; font-size: .9em; white-space: nowrap; }
    .addbar .qoh { display: inline-block; min-width: 5em; padding: .35em 0; font-weight: 600; }
    /* Less empty space above the title: the 6em logos set the height of the top row */
    .logoImg { max-height: 4.5em; }
    h2 { line-height: 1.2em; margin-bottom: .6em; }
</style>
</head>
<body>
    <form id="form1" runat="server">
          <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
 <div class="content">
     <div class="container">
         <div class="row 150%">
             <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website">
                 <img src="images/logo.png" style="float: left" class="logoImg" /></a></div>
             <div class="8u 12u$(medium)">
                 <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" OnClick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                 <asp:LinkButton ID="lbtnTrfsSOs" runat="server" class="buttonC icon fa-arrow-left" PostBackUrl="~/TransferHeaders.aspx">&nbsp;Open Transfers</asp:LinkButton>
             </div>
             <div class="2u 12u$(medium)">
                 <asp:Image ID="imgCoImg" runat="server" Style="float: right" class="logoImg" /></div>
         </div>
         <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
             <ProgressTemplate>
                 <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
                     <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." Style="padding: 10px; padding-top: 15%; border-radius: 1.5em" />
                 </div>
             </ProgressTemplate>
         </asp:UpdateProgress>
         <asp:UpdatePanel ID="UpdatePanel1" runat="server">
             <ContentTemplate>
                 <div class="row 150%">
                     <div class="12u 12u$(medium)">
                         <h2>Stock Transfer Between Warehouses: ID=<asp:Label ID="lblDocNum" runat="server" Text=""></asp:Label><asp:Label ID="lblDocID" runat="server" Text=""></asp:Label></h2>
                         <asp:Label ID="lblError" runat="server" ForeColor="Red" />
                         <table style="margin:auto">
                             <tr>
                                <td colspan="2"></td>
                                <td style="text-align:left">From Warehouse: &nbsp;</td>
                                <td><asp:DropDownList ID="ddlFromWarehouse" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlFromWarehouse_SelectedIndexChanged" style="width:10em; margin-top:0.25em"/></td>
                                <td>To Warehouse:&nbsp;</td>
                                <td><asp:DropDownList ID="ddlToWarehouse" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlToWarehouse_SelectedIndexChanged" style="width:10em; margin-top:0.25em" /></td>
                                <td>Status:&nbsp;</td>
                                 <td><asp:DropDownList ID="DDStatus" runat="server" style="width:10em" Enabled="false">
                                     <asp:ListItem>New</asp:ListItem>
                                     <asp:ListItem>Started</asp:ListItem>
                                     <asp:ListItem>Complete</asp:ListItem>
                                     <asp:ListItem>Deleted</asp:ListItem>
                                     </asp:DropDownList></td>
                            </tr>
                             <tr>
                                 <td></td>
                                 <td></td>
                                 <td>Date</td>
                                 <td><asp:TextBox ID="txtDate" runat="server" style="width:10em;margin-top:0.25em"></asp:TextBox>
                                     <cci:CalendarExtender ID="CalendarExtender1" runat="server" Enabled="True" TargetControlID="txtDate" Format="dd MMM yyyy"></cci:CalendarExtender>
                                 </td>
                                 <td>Complete_Date:&nbsp;</td>
                                 <td><asp:TextBox ID="txtCompleteDate" runat="server" style="width:10em; margin-top:0.25em" Enabled="false"></asp:TextBox></td>
                                 <td colspan="2"></td>
                             </tr>
                             <tr>
                                <td colspan="2"></td>
                                <td>Reference:&nbsp;</td>
                                <td colspan="5" ><asp:TextBox ID="txtxTrfRef" runat="server" style="width:100%; margin-top:0.25em"></asp:TextBox></td>
                            </tr>
                             <tr>
                                <td colspan="2"></td>
                                <td>Notes:&nbsp;</td>
                                <td colspan="5"><asp:TextBox ID="txtNotes" runat="server" style="width:100%; margin-top:0.25em; height:2.2em; padding:0.25em 0.5em; resize:vertical" TextMode="MultiLine" Rows="1"></asp:TextBox></td>
                            </tr>
                         </table>
                     </div>
                 </div>

                 <%-- One add bar instead of a dropdown on every line: pick item (same searchable picker
                      as Item Adjustment), lot, qty, Add. Enter in the qty box adds the line. --%>
                 <asp:Panel ID="PnlAddLine" runat="server" DefaultButton="lbtnAddLine" CssClass="addbar">
                     <div class="addbar-title"><i class="fa fa-plus-circle"></i>&nbsp;Add items to this transfer</div>
                     <div class="addbar-hint">Pick an item, enter the quantity, then click <b>Add</b> (or press Enter). Each item appears as a line below.</div>
                     <table>
                         <tr>
                             <td style="text-align:left">Item<br />
                                 <asp:DropDownList ID="DDAddItem" runat="server" CssClass="item-search" AutoPostBack="true" OnSelectedIndexChanged="DDAddItem_SelectedIndexChanged" style="width:28em; max-width:90vw"></asp:DropDownList></td>
                             <asp:PlaceHolder ID="phAddLot" runat="server">
                             <td>Lot Number<br />
                                 <asp:DropDownList ID="DDAddLot" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDAddLot_SelectedIndexChanged" style="width:10em"></asp:DropDownList></td>
                             </asp:PlaceHolder>
                             <td>On Hand<br />(From)<br /><asp:Label ID="lblAddFrom" runat="server" CssClass="qoh" /></td>
                             <td>On Hand<br />(To)<br /><asp:Label ID="lblAddTo" runat="server" CssClass="qoh" /></td>
                             <td>Qty To Trf<br />
                                 <asp:TextBox ID="txtAddQty" runat="server" style="text-align:center; width:6em"></asp:TextBox>
                                 <cci:FilteredTextBoxExtender ID="ftbeAddQty" runat="server" TargetControlID="txtAddQty" FilterType="Custom, Numbers" ValidChars="." /></td>
                             <td><asp:LinkButton ID="lbtnAddLine" runat="server" CssClass="buttonC icon fa-plus" OnClick="lbtnAddLine_Click">&nbsp;Add</asp:LinkButton></td>
                         </tr>
                     </table>
                 </asp:Panel>

                 <div class="row 150%">
                     <div class="12u 12u$(medium)">
                         <asp:GridView ID="GridTrfLines" runat="server" AutoGenerateColumns="false" CssClass="gridviewS" OnRowDataBound="GridTrfLines_RowDataBound" EmptyDataText="No lines yet - pick an item above and click Add.">
                             <HeaderStyle CssClass="gridViewHeaderS" />
                             <RowStyle CssClass="gridViewRowS" />
                             <AlternatingRowStyle CssClass="gridViewAltRowS" />
                             <Columns>
                                 <asp:BoundField HeaderText="Code" DataField="ItemCode" ItemStyle-Width="10em" />
                                 <asp:BoundField HeaderText="Description" DataField="ItemDescription" ItemStyle-HorizontalAlign="Left" />
                                 <asp:BoundField HeaderText="Lot Number" DataField="LotNumber" ItemStyle-Width="8em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                 <asp:BoundField HeaderText="Qty To Trf" DataField="TrfOutQty" ItemStyle-Width="6em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                  <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="2em" >
                                     <ItemTemplate>
                                          <asp:LinkButton ID="lbtnDeleteLine" CommandArgument='<%# Eval("TrfLID") %>' CommandName="lbtnDeleteLine" runat="server" CssClass="fa fa-ban" ToolTip="Delete Line" style="color:red" OnClick="lbtnDeleteLine_Click"> </asp:LinkButton>
                                         <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Delete Line?" Enabled="True" TargetControlID="lbtnDeleteLine"></cci:ConfirmButtonExtender>
                                     </ItemTemplate>
                                 </asp:TemplateField>
                             </Columns>
                         </asp:GridView>
                     </div>
                     <div class="12u 12u$(medium)" style="text-align: center">
                         <asp:Panel ID="PnlButtons" runat="server">
                             <asp:Label ID="lblErr" runat="server" Text="" Style="margin: auto; color: red"></asp:Label><br />
                             <asp:LinkButton ID="lbtnDelTrf" CssClass="icon fa-ban buttonTransparent" runat="server" ForeColor="Red" ToolTip="Delete Transfer Slip" OnClick="lbtnDelTrf_Click" Style="margin-right: 2em"> Delete</asp:LinkButton>
                             <cci:ConfirmButtonExtender ID="ConfirmButtonExtender2" runat="server" ConfirmText="WARNING - Delete this Transfer? Are you sure?" Enabled="True" TargetControlID="lbtnDelTrf"></cci:ConfirmButtonExtender>
                             <asp:LinkButton ID="lbtnPrintDN" runat="server" class="buttonRed icon fa-print" Width="200px" OnClick="lbtnPrintDN_Click" >&nbsp;Print Preview</asp:LinkButton>
                             <asp:LinkButton ID="LbtnSaveEdits" runat="server" OnClick="LbtnSaveEdits_Click" CssClass=" icon fa-edit buttonSage" ToolTip="Save Edits"> Save Edits</asp:LinkButton>      
                             <asp:LinkButton ID="lbtnStart" runat="server" class="buttonIndex icon fa-upload" Width="220px" OnClick="lbtnStart_Click" ToolTip="Start Transfer">&nbsp;Start Transfer</asp:LinkButton>
                             <cci:ConfirmButtonExtender ID="ConfirmButtonExtender3" runat="server" ConfirmText="Confirm, Start Item Transfer?" Enabled="True" TargetControlID="lbtnStart"></cci:ConfirmButtonExtender>  
                             <asp:LinkButton ID="lbtnCompleteTrf" runat="server" class="buttonSage icon fa-download" Width="220px" OnClick="lbtnCompleteTrf_Click" ToolTip="Complete Transfer" OnClientClick="if (!confirm('Confirm, Complete Item Transfer?')) return false; this.style.pointerEvents='none'; this.style.opacity='0.5'; return true;">&nbsp;Complete Transfer</asp:LinkButton>
                             <asp:LinkButton ID="lbtnAddShip" CssClass="icon fa-truck buttonTransparent" runat="server"  ToolTip="Added shipping costs will be split accross each line item and the stock on hand value in the receiving warehouse will be adjusted accordingly." OnClick="lbtnAddShip_Click" Style="margin-right: 2em"> Add Shipping Costs</asp:LinkButton>
                         </asp:Panel>
                    </div>
                 </div>
                  <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="lbtnAddShip"></cci:ModalPopupExtender>
                     <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                         <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" Style="float: right" ToolTip="Cancel" OnClick="lbtnCancel_Click"> </asp:LinkButton>
                         <div class="HellowWorldPopup">
                             <div id="Div4" class="PopupHeader">
                                 <h4>Add Shipping/Additional Costs<asp:Label ID="lblTpe" runat="server" Text=""></asp:Label></h4>
                             </div>
                             <div class="PopupBody" style="text-align: center">
                                 <span style="font-size:0.8em">Costs captured will be allocated across all items on this Transfer Slip.<br /> The stock value in the receiving warehouse will be adjusted accordingly. <br /><br /> Tick below to also lift the item average cost in Sage. </span><br /><br />
                                 <table style="width: 300px; margin: auto; font-size: .8em; text-align: left">
                                     <tr>
                                         <td>Additional Cost *</td>
                                         <td style="padding: .5em">
                                             <asp:TextBox ID="txtAddCost" runat="server" Width="100px" Style="padding: .5em; text-align:center"></asp:TextBox></td>
                                     </tr>
                                     <tr>
                                         <td>Description</td>
                                         <td style="padding: .5em">
                                             <asp:TextBox ID="txtAddCostReason" runat="server" TextMode="MultiLine" Rows="2" Width="160px" MaxLength="100" Style="padding: .5em"></asp:TextBox></td>
                                     </tr>
                                     <tr>
                                         <td colspan="2" style="padding: .5em">
                                             <asp:CheckBox ID="chkSageUpdate" runat="server" Text="Update item average cost in Sage?" /></td>
                                     </tr>
                                 </table>
                                 <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtAddCost" FilterType="Custom, Numbers" ValidChars="." />
                             </div>
                             <div class="Controls">
                                 <input id="btnCancel5" type="button" class="fa fa-times-circle" value="" runat="server" style="display: none" />
                                 <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display: none" />
                                 <asp:LinkButton ID="btnSaveConfirm" runat="server" CssClass="icon fa-save buttonCancel" OnClick="btnSaveConfirm_Click"> Save</asp:LinkButton><br />
                             </div>
                         </div>
                     </asp:Panel>

             </ContentTemplate>
         </asp:UpdatePanel>
     </div>
 </div>
    </form>
    <script src="<%= ResolveUrl("~/scripts/itemSearch.js") %>"></script>
</body>
</html>
