<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="PickingSlip.aspx.cs" Inherits="SBMS.PickingSlip" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Picking Slip</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <%-- bump ?v= whenever df-theme.css / df-ui.js change, so browsers don't serve a stale copy --%>
    <link rel="stylesheet" href="assets/css/df-theme.css?v=13" />
    <script src="assets/js/df-ui.js?v=13"></script>
</head>
<body class="df-page df-mod-sales">
    <form id="form1" runat="server">
         <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>

        <div class="df-shell" id="dfShell">
            <div class="df-main">
                <header class="df-topbar">
                    <a href="https://mydatafusion.online" title="My Data Fusion website" target="_blank"><img src="images/logo.png" class="df-brand-logo" alt="Data Fusion" /></a>
                    <asp:LinkButton ID="lbtnHome" runat="server" CssClass="df-icon-btn" onclick="lbtnHome_Click1" ToolTip="Dashboard" aria-label="Dashboard"><i class="icon fa-home"></i></asp:LinkButton>
                    <asp:LinkButton ID="lbtnSOs" runat="server" CssClass="df-btn" PostBackUrl="~/OSSalesOrders.aspx"><i class="icon fa-arrow-left"></i>&nbsp;Open Sales Orders</asp:LinkButton>
                    <div class="df-topbar-spacer"></div>
                    <button type="button" class="df-icon-btn" id="dfThemeToggle" title="Toggle dark mode" aria-label="Toggle dark mode"><i class="icon fa-moon-o" id="dfThemeIcon"></i></button>
                    <asp:Image ID="imgCoImg" runat="server" CssClass="df-co-logo" />
                </header>

                <main class="df-content">
                    <div class="df-page-head">
                        <h2 class="df-page-title"><i class="icon fa-tasks df-page-icon"></i>Picking Slip
                            <asp:Label ID="lblDocNum" runat="server" Text="" CssClass="df-count"></asp:Label></h2>
                        <asp:Label ID="lblPSid" runat="server" Text="" style="display:none" ></asp:Label>
                        <asp:Label ID="lblDocID" runat="server" Text="" Style="display: none"></asp:Label>
                        <asp:LinkButton ID="lbtnViewSO" runat="server" CssClass="df-btn" ToolTip="View Sales Order" OnClick="lbtnViewSO_Click"><i class="icon fa-arrow-circle-o-left"></i>&nbsp;Sales Order</asp:LinkButton>
                        <asp:DropDownList ID="DDOptions" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDOptions_SelectedIndexChanged" CssClass="df-select df-select-sm">
                            <asp:ListItem>- Create -</asp:ListItem>
                             <asp:ListItem value = "1">Works Order</asp:ListItem>
                        </asp:DropDownList>
                        <asp:LinkButton ID="lbtnWOrd" runat="server" CssClass="df-btn" OnClick="lbtnWOrd_Click"><i class="icon fa-align-justify"></i>&nbsp;Works Order</asp:LinkButton>
                        <asp:Label ID="lblWoID" runat="server" Text="" style="display:none"></asp:Label>
                    </div>

                    <div class="df-card">
                        <table class="df-detail-table">
                            <tr>
                                <td style="width: 10em">Customer </td>
                                <td><asp:TextBox ID="txtCustName" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td style="width:10em">Address</td>
                                <td><asp:TextBox ID="txtAddress1" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td style="width:9em">Sales Order&nbsp;<asp:Label ID="lblSOStatus" runat="server" Text=""></asp:Label></td>
                                <td><asp:TextBox ID="txtSONum" runat="server" ReadOnly="true"></asp:TextBox></td>
                            </tr>
                            <tr>
                                <td>Due Date</td>
                                <td><asp:TextBox ID="txtSODate" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td>Address</td>
                                <td><asp:TextBox ID="txtAddress2" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td>Issued To</td>
                                <td><asp:TextBox ID="txtIssuedTo" runat="server" ReadOnly="true"></asp:TextBox><asp:LinkButton ID="lbtnIssue" runat="server" CssClass="icon fa-plus-circle df-btn" ToolTip="Issue To Picker" OnClick="lbtnIssue_Click">&nbsp;</asp:LinkButton>
                                    <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Issue this Picking Slip to a picker? Are you sure?" Enabled="True" TargetControlID="lbtnIssue"></cci:ConfirmButtonExtender>
                                </td>
                           </tr>
                            <tr>
                                <td>Reference</td>
                                <td><asp:TextBox ID="txtRef" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td>From Store</td>
                                <td><asp:Label ID="txtFromStore" runat="server" Font-Bold="true"></asp:Label></td>
                                <td>Issue Date</td>
                                <td><asp:TextBox ID="txtIssueDate" runat="server" ReadOnly="true" ></asp:TextBox></td>
                           </tr>
                            <tr>
                                <td>Sales Rep</td>
                                <td><asp:TextBox ID="txtRep" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td>Delivery By</td>
                                <td><asp:DropDownList ID="DDeliveryBy" runat="server" CssClass="df-select" Width="100%"></asp:DropDownList></td>
                                <td>Picking Status</td>
                                <td><asp:Label ID="lblstatus" runat="server" Text=""></asp:Label></td>
                           </tr>
                            </table>
                    </div>

                   <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
                            <ProgressTemplate>
                                <div class="df-loading-overlay">
                                    <div class="df-loading-box">
                                        <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." CssClass="df-loading-spinner" />
                                    </div>
                                </div>
                              </ProgressTemplate>
                        </asp:UpdateProgress>

               <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                         <ContentTemplate>

                            <div class="df-card df-table-card">
                                <div class="df-table-wrap">
                                  <asp:GridView ID="GridPSLines" runat="server" AutoGenerateColumns="false" CssClass="gridviewS" OnRowDataBound="GridPSLines_RowDataBound" >
                                            <HeaderStyle CssClass="gridViewHeaderS" />
                                                    <RowStyle CssClass="gridViewRowS" VerticalAlign="Top" />
                                                    <AlternatingRowStyle CssClass="gridViewAltRowS" />
                                                    <Columns>
                                                        <asp:BoundField DataField="SelectionID" />
                                                        <asp:BoundField HeaderText="ItemCode" DataField="ItemCode" ReadOnly="True" ItemStyle-Width="8em"  />
                                                         <asp:BoundField HeaderText="Description" DataField="ItemDescription" ReadOnly="True"  />
                                                         <asp:TemplateField HeaderText="Barcode (Scan)" ItemStyle-Width="6em" HeaderStyle-HorizontalAlign="Center" >
                                                             <ItemTemplate>
                                                                 <asp:TextBox ID="txtBarcode" runat="server" AutoPostBack="true" OnTextChanged="txtBarcode_TextChanged" Text="" style="width:9em; text-align:center"></asp:TextBox><br />
                                                                 <asp:Label ID="lblBCError" runat="server" Text="" ForeColor="Red" Font-Size="Small"></asp:Label>
                                                             </ItemTemplate>
                                                         </asp:TemplateField>
                                                         <asp:BoundField HeaderText="Unit" DataField="Unit" ReadOnly="True" ItemStyle-Width="4em" />
                                                        <asp:BoundField HeaderText="Order_Qty" DataField="Quantity" ReadOnly="True" ItemStyle-Width="4em" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center"  />
                                                       <asp:TemplateField HeaderText="Pick_Qty" HeaderStyle-HorizontalAlign="Center" HeaderStyle-Width="4em" >
                                                            <ItemTemplate>
                                                                <asp:TextBox ID="txtPickQty" runat="server" Text='<%# Eval("PickQty") %>' style="width:4em; text-align:center" ></asp:TextBox>
                                                                 <cci:FilteredTextBoxExtender ID="ftbeP" runat="server" TargetControlID="txtPickQty" FilterType="Numbers,Custom" ValidChars="." />
                                                         </ItemTemplate>
                                                        </asp:TemplateField>
                                                       <asp:TemplateField HeaderText="Store (QOH)" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="4em">
                                                            <ItemTemplate>
                                                                <asp:DropDownList ID="DDStore" runat="server" style="width:4em; text-align:center" AutoPostBack="true" OnSelectedIndexChanged="DDStore_SelectedIndexChanged">
                                                                    <asp:ListItem Value="0">- ?-</asp:ListItem>
                                                                </asp:DropDownList>
                                                            </ItemTemplate>
                                                        </asp:TemplateField>
                                                        <asp:TemplateField HeaderText="Lot Number" ItemStyle-Width="8em">
                                                            <ItemTemplate>
                                                                <asp:Label ID="lblLotNum" runat="server" style="display:none" Text='<%# Eval("LotNumber") %>' ></asp:Label>
                                                                    <asp:DropDownList ID="DDlotNum" runat="server" style="width:7.6em; text-align:center" AutoPostBack="true" OnSelectedIndexChanged="DDlotNum_SelectedIndexChanged" >
                                                                  <asp:ListItem Value="0">- Lot Number-</asp:ListItem>
                                                                  </asp:DropDownList>
                                                            </ItemTemplate>
                                                        </asp:TemplateField>
                                                        <asp:TemplateField ItemStyle-Width="0em">
                                                            <ItemTemplate>
                                                                <asp:LinkButton ID="lbtnLotNumAdd" runat="server" CssClass="buttonTransparent icon fa-plus" style="font-size:.8em; margin:-1em" CommandArgument='<%# Eval("LineID") %>' ToolTip="Fulfull this line item with more than one Lot Number" OnClick="lbtnLotNumAdd_Click"></asp:LinkButton>
                                                            </ItemTemplate>
                                                        </asp:TemplateField>
                                                   <asp:TemplateField ItemStyle-HorizontalAlign="Center" ItemStyle-Width="1.5em">
                                                    <HeaderTemplate ><asp:Label ID="Label1" runat="server" Text="Done"></asp:Label><br />
                                                            <asp:CheckBox ID="chkSelectAll" runat="server" AutoPostBack="true" OnCheckedChanged="chkSelectAll_CheckedChanged" />
                                                        </HeaderTemplate>
                                                       <ItemTemplate>
                                                        <asp:CheckBox ID="chkComplete" runat="server" Checked='<%# Eval("PickComplete")%>'  Text=" " AutoPostBack="true" OnCheckedChanged="chkComplete_CheckedChanged"   />
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="2em" >
                                                            <ItemTemplate>
                                                                 <asp:LinkButton ID="lbtnLineSave" CommandArgument='<%# Eval("LineID") %>' CommandName="lbtnLineSave" runat="server" CssClass="icon fa-save df-link-strong" ToolTip="Save Picking Slip Line" OnClick="lbtnLineSave_Click"> </asp:LinkButton>
                                                            </ItemTemplate>
                                                        </asp:TemplateField>
                                                     </Columns>
                                   </asp:GridView>
                                </div>
                            </div>

                            <asp:Panel ID="PnlMsg" runat="server" CssClass="df-split">
                                <div class="df-card">
                                    <h3 class="df-card-title">Picking Message</h3>
                                    <asp:TextBox ID="txtWMsg" runat="server" Rows="5" TextMode="MultiLine" CssClass="df-input-full" Style="height:auto"></asp:TextBox>
                                </div>
                                <div class="df-card">
                                    <h3 class="df-card-title">Packing Message</h3>
                                    <asp:TextBox ID="txtPMsg" runat="server" Rows="5" TextMode="MultiLine" CssClass="df-input-full" Style="height:auto"></asp:TextBox>
                                </div>
                                <div class="df-card">
                                    <h3 class="df-card-title">Delivery Message</h3>
                                    <asp:TextBox ID="txtDMsg" runat="server" Rows="5" TextMode="MultiLine" CssClass="df-input-full" Style="height:auto"></asp:TextBox>
                                </div>
                                <div class="df-card">
                                    <h3 class="df-card-title">Progress Trail</h3>
                                    <div style="max-height:8.5em; overflow:auto">
                                        <asp:GridView ID="GridHistLines" runat="server" AutoGenerateColumns="false" CssClass="gridview" RowStyle-Wrap="true" Style="font-size: .85em">
                                               <HeaderStyle CssClass="gridViewHeader" />
                                               <RowStyle CssClass="gridViewRow" />
                                               <AlternatingRowStyle CssClass="gridViewAltRow" />
                                               <FooterStyle CssClass="gridViewHeader" />
                                               <PagerStyle CssClass="gridViewPager" />
                                               <Columns>
                                                   <asp:BoundField HeaderText="Date &amp; Time" DataField="MoveDate" ReadOnly="True" ItemStyle-Width="12em" />
                                                    <asp:BoundField HeaderText="Process" DataField="ToProcess" ReadOnly="True" />
                                               </Columns>
                                           </asp:GridView>
                                    </div>
                                </div>
                             </asp:Panel>

                            <div class="df-card">
                                <div class="df-toolbar">
                                    <asp:LinkButton ID="lbtnDelPS" CssClass="df-btn df-btn-danger" runat="server" ToolTip="Delete Picking Slip" OnClick="lbtnDelPS_Click"><i class="icon fa-ban"></i>&nbsp;Delete</asp:LinkButton>
                                    <cci:ConfirmButtonExtender ID="ConfirmButtonExtender2" runat="server" ConfirmText="WARNING - Delete this Picking Slip? Are you sure?" Enabled="True" TargetControlID="lbtnDelPS"></cci:ConfirmButtonExtender>
                                    <asp:LinkButton ID="lbtnJCPrint" runat="server" CssClass="df-btn" OnClick="lbtnPrintPS_Click"><i class="icon fa-print"></i>&nbsp;Print Preview</asp:LinkButton>
                                    <asp:LinkButton ID="LbtnSaveEdits" runat="server" OnClick="LbtnSaveEdits_Click" CssClass="df-btn" ToolTip="Save Edits"><i class="icon fa-edit"></i>&nbsp;Save Edits</asp:LinkButton>
                                    <asp:LinkButton ID="LbtnPickSave" runat="server" OnClick="LbtnPickSave_Click" CssClass="df-btn df-btn-primary"><i class="icon fa-save"></i>&nbsp;Close off Picking Slip As Picked</asp:LinkButton>
                                    <cci:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" ConfirmText="Confirm: Mark as picking complete?" Enabled="True" TargetControlID="LbtnPickSave"></cci:ConfirmButtonExtender>
                                </div>
                            </div>

        <asp:LinkButton ID="LinkButton2" runat="server" style="display:none">LinkButton</asp:LinkButton>
        <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton2"></cci:ModalPopupExtender>
            <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                        <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                        <div class="HellowWorldPopup">
                            <div id="Div4" class="PopupHeader">
                                <h2>Add message</h2><asp:Label ID="lblSender" runat="server" Text="Label" style="display:none"></asp:Label><asp:Label ID="lblLineid" runat="server" Text="Label" style="display:none"></asp:Label>
                            </div>
                            <div class="PopupBody">
                              <asp:TextBox ID="txtMsgBody" runat="server" Rows="5" Columns="40" TextMode="MultiLine"></asp:TextBox>
                            </div>
                            <div class="Controls">
                                <input id="btnCancel5" type="button" class="fa fa-times-circle" value="" runat="server" style="display:none"/>
                                <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                <asp:LinkButton ID="btnSaveComment" runat="server" CssClass="buttonCancel" OnClick="btnSaveComment_Click" >OK</asp:LinkButton>
                            </div>
                        </div>
                    </asp:Panel>

                <asp:LinkButton ID="LinkButton1" runat="server" style="display:none">LinkButton</asp:LinkButton>
                    <cci:ModalPopupExtender ID="ModalPopupExtender1" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="LbtnLotAddCancel" Drag="true" OkControlID="Button2" PopupControlID="PnlLotNumAdd" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton1"></cci:ModalPopupExtender>
                        <asp:Panel ID="PnlLotNumAdd" runat="server" Style="display: none">
                                    <asp:LinkButton ID="LbtnLotAddCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                                    <div class="HellowWorldPopup">
                                        <div id="Div44" class="PopupHeader">
                                            <br />
                                            <h4>Fulfill Line Item With Multiple Lot Numbers</h4><asp:Label ID="lblSlipLine" runat="server" Text="Label" style="display:none"></asp:Label><asp:Label ID="itemid" runat="server" Text="Label" style="display:none"></asp:Label><br />
                                           <h5>Qty Required = <asp:Label ID="lblLineQty" runat="server" Text=""></asp:Label></h5>
                                        </div>
                                        <div class="PopupBody" style="text-align:center">
                                           <asp:Panel ID="Panel1" runat="server" style="margin:auto; width:400px; height:200px; overflow:auto">
                                               <asp:GridView ID="GridLotNums" runat="server" AutoGenerateColumns="false" CssClass="gridview" Width="100%" >
                                             <HeaderStyle CssClass="gridViewHeader" />
                                             <RowStyle CssClass="gridViewRow" />
                                             <AlternatingRowStyle CssClass="gridViewAltRow" />
                                             <FooterStyle CssClass="gridViewHeader" />
                                             <PagerStyle CssClass="gridViewPager" />
                                             <Columns>
                                                 <asp:BoundField HeaderText="Store" DataField="StoreCode" ReadOnly="True" />
                                                 <asp:BoundField HeaderText="Lot Number" DataField="LotNumber" ReadOnly="True" />
                                                  <asp:BoundField HeaderText="Q O H" DataField="QtyHandToStore" ReadOnly="True" />
                                                 <asp:TemplateField HeaderText="Use Qty">
                                                     <ItemTemplate>
                                                         <asp:TextBox ID="txtUseQty" runat="server" Width="50px" style="text-align:center"></asp:TextBox>
                                                         <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtUseQty" FilterType="Custom, Numbers" ValidChars="." />
                                                     </ItemTemplate>
                                                 </asp:TemplateField>
                                             </Columns>
                                         </asp:GridView>
                                                <asp:Label ID="lblError" runat="server" Text="" ForeColor="Red"></asp:Label>
                                               </asp:Panel>
                                        </div>
                                        <div class="Controls">
                                            <input id="Button1" type="button" class="fa fa-times-circle" value="" runat="server" style="display:none"/>
                                            <input id="Button2" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                            <asp:LinkButton ID="LbtnLotAddOK" runat="server" CssClass="buttonCancel" OnClick="LbtnLotAddOK_Click">Update</asp:LinkButton><br />
                                            <cci:ConfirmButtonExtender ID="ConfirmButtonExtender3" runat="server" ConfirmText="Update picking slip with selected lot numbers, are you sure?" Enabled="True" TargetControlID="LbtnLotAddOK"></cci:ConfirmButtonExtender>
                                        </div>
                                    </div>
            </asp:Panel>

                        <asp:LinkButton ID="LinkButton3" runat="server" style="display:none">LinkButton</asp:LinkButton>
                            <cci:ModalPopupExtender ID="ModalPopupExtender2" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="lbtnNewWOCancel" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlNewWO" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton3"></cci:ModalPopupExtender>
                                <asp:Panel ID="PnlNewWO" runat="server" Style="display: none">
                                            <div class="HellowWorldPopup">
                                                <div id="Div47" class="PopupHeader">
                                                    <h2>Create New Works Order</h2>
                                                </div>
                                                <div class="PopupBody">
                                                  Are you sure?
                                                </div>
                                                <div class="Controls">
                                                    <input id="lbtnNewWO" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                                    <asp:LinkButton ID="lbtnNewWOCancel" runat="server" CssClass="buttonTransparent icon fa-ban" ToolTip="Cancel" OnClick="lbtnNewWOCancel_Click" >No, Cancel </asp:LinkButton>
                                                    <asp:LinkButton ID="lbtnNewWOYes" runat="server" CssClass="buttonSage icon fa-thumbs-up" OnClick="lbtnNewWOYes_Click" >Yes, Create it</asp:LinkButton>
                                                </div>
                                            </div>
                                        </asp:Panel>

                                  </ContentTemplate>
                            </asp:UpdatePanel>
                </main>
            </div>
        </div>
    </form>
    <script type="text/javascript">
        function triggerSaveOnEnter(event, saveButtonId) {
            if (event.key === "Enter") {
                event.preventDefault(); // Prevent the default form submission
                document.getElementById(saveButtonId).click(); // Trigger the click event on the save button
            }
        }
</script>
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</body>
</html>
