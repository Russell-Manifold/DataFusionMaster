<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="JobCard.aspx.cs" Inherits="SBMS.JobCard" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Job Card</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="content">
            <div class="container">
                <div class="row 150%">
                    <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>
                    <div class="8u 12u$(medium)">
                        <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="lbtnSalesOrd" runat="server" class="buttonC icon fa-book" PostBackUrl="~/OSSalesOrders.aspx">&nbsp;Open Sales Orders</asp:LinkButton>
                        <asp:LinkButton ID="lbtnViewSO" runat="server" CssClass="buttonC icon fa-arrow-circle-o-left" ToolTip="View Sales Order" OnClick="lbtnViewSO_Click">&nbsp;Sales Order</asp:LinkButton>
                        <asp:LinkButton ID="lbtnJobTrack" runat="server" class="buttonC icon fa-sitemap" PostBackUrl="~/JobTracking.aspx">&nbsp;Job Card Tracking</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" Style="float: right" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton>
                        <br />
                        <br />
                        <h2 style="padding-top: 0; line-height: 1em">Job Card</h2>
                    </div>
                    <div class="2u 12u$(medium)">
                        <asp:Image ID="imgCoImg" runat="server" Style="float: right" class="logoImg" />
                    </div>
                </div>

                <div class="row 150%">
                    <div class="1u 12u$(medium)">&nbsp;</div>
                    <div class="10u 12u$(medium)">
                        <table style="width: 100%">
                            <tr>
                                <td colspan="6">
                                    <h6>Job Card Details:
                                        <asp:Label ID="lblDocNum" runat="server" Text=""></asp:Label><asp:Label ID="lblJCid" runat="server" Text="" Style="display: none"></asp:Label><asp:Label ID="lblDocID" runat="server" Text="" Style="display: none"></asp:Label>
                                    </h6>
                                </td>
                            </tr>
                            <tr>
                                <td style="width: 10em">Customer </td>
                                <td>
                                    <asp:TextBox ID="txtCustName" runat="server" Style="width: 95%" ReadOnly="true"></asp:TextBox></td>
                                <td style="width: 10em">Address</td>
                                <td>
                                    <asp:TextBox ID="txtAddress1" runat="server" Style="width: 95%" ReadOnly="true"></asp:TextBox></td>
                                <td>Sales Order</td>
                                <td style="width: 14em">
                                    <asp:TextBox ID="txtPSNum" runat="server" ReadOnly="true"></asp:TextBox></td>
                            </tr>
                            <tr>
                                <td>Due Date</td>
                                <td>
                                    <asp:TextBox ID="txtPODate" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td></td>
                                <td>
                                    <asp:TextBox ID="txtAddress2" runat="server" Style="width: 95%" ReadOnly="true"></asp:TextBox></td>
                                <td>Issued To</td>
                                <td>
                                    <asp:TextBox ID="txtIssuedTo" runat="server" ReadOnly="true"></asp:TextBox>&nbsp;<asp:LinkButton ID="lbtnIssue" runat="server" CssClass="fa fa-plus-circle buttonRed" ToolTip="Issue Job Card" OnClick="lbtnIssue_Click">&nbsp;</asp:LinkButton>
                                </td>
                            </tr>
                            <tr>
                                <td>Reference</td>
                                <td>
                                    <asp:TextBox ID="txtRef" runat="server" Style="width: 95%" ReadOnly="true"></asp:TextBox></td>
                                <td></td>
                                <td>
                                    <asp:TextBox ID="txtAddress3" runat="server" Style="width: 95%" ReadOnly="true"></asp:TextBox></td>
                                <td>Issue Date</td>
                                <td>
                                    <asp:TextBox ID="txtIssueDate" runat="server" ReadOnly="true"></asp:TextBox></td>
                            </tr>
                            <tr>
                                <td>Sales Rep</td>
                                <td style="padding-top: 2px">
                                    <asp:TextBox ID="txtRep" runat="server" ReadOnly="true" Style="width: 95%;" ></asp:TextBox></td>
                                <td>Delivery By</td>
                                <td style="padding-top: 2px">
                                    <asp:DropDownList ID="DDeliveryBy" runat="server" Style="width: 95%; border:1px darkgreen solid"></asp:DropDownList></td>
                                <td>Status</td>
                                <td>
                                    <asp:Label ID="lblstatus" runat="server" Text=""></asp:Label></td>
                            </tr>
                            <tr>
                                <td colspan="6">
                                    <br />
                                </td>
                            </tr>
                            <tr style="background-color: #fff; border: 2px #4282C1 solid;">
                                <td style="padding: .5em 0em .5em .25em">
                                    <asp:Label ID="lblJCSummary" runat="server" Text="Job Card Summary*" Style="color: red;"></asp:Label></td>
                                <td colspan="4">
                                    <asp:TextBox ID="txtJobCardSummary" runat="server" Style="width: 100%;"></asp:TextBox></td>
                                <td style="text-align: right">
                                    <asp:Label ID="lblJCQuantity" runat="server" Text="Qty Of Items*" Style="color: red;"></asp:Label><asp:TextBox ID="txtJCQuantity" runat="server" Width="50px" Style="margin-right: 3em; text-align: center">1</asp:TextBox></td>
                            </tr>
                        </table>
                        <br />
                        <%--<hr />--%>
                    </div>
                    <div class="1u 12u$(medium)">&nbsp;</div>
                </div>
                <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                    <ContentTemplate>
                        <div class="row 150%">
                            <%--<div class="1u 12u$(medium)">&nbsp;</div>--%>
                            <div class="12u 12u$(medium)">
                                <asp:GridView ID="GridJCLines" runat="server" AutoGenerateColumns="false" CssClass="gridviewS" RowStyle-Wrap="true" OnRowDataBound="GridJCLines_RowDataBound" ShowFooter="true" Style="font-size: 1em">
                                    <HeaderStyle CssClass="gridViewHeaderS" />
                                    <RowStyle CssClass="gridViewRowS" />
                                    <AlternatingRowStyle CssClass="gridViewAltRowS" />
                                    <FooterStyle CssClass="gridViewHeaderS" />
                                    <PagerStyle CssClass="gridViewPagerS" />
                                    <Columns>
                                        <asp:TemplateField HeaderText="Type">
                                            <ItemTemplate>
                                                <asp:DropDownList ID="DDItemType" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDItemType_SelectedIndexChanged">
                                                    <asp:ListItem Value="99">-Select-</asp:ListItem>
                                                    <asp:ListItem Value="0">Item</asp:ListItem>
                                                    <asp:ListItem Value="1">Service</asp:ListItem>
                                                    <asp:ListItem Value="2">GL Account</asp:ListItem>
                                                    <asp:ListItem Value="3">Flexi-Kit</asp:ListItem>
                                                    <asp:ListItem Value="6">Bundle</asp:ListItem>
                                                </asp:DropDownList>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Selection">
                                            <ItemTemplate>
                                                <asp:DropDownList ID="DDItemCode" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDItemCode_SelectedIndexChanged" Width="70px">
                                                    <asp:ListItem Value="0">- ? -</asp:ListItem>
                                                </asp:DropDownList>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Description" ItemStyle-Width="100%">
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtDescription" runat="server" Style="width: 100%" Text='<%# Eval("ItemDescription") %>'></asp:TextBox><br />
                                                <asp:Label ID="lblLineComment" runat="server" Text='<%# Eval("Comments") %>' Style="font-size: .8em"></asp:Label>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Store" ItemStyle-HorizontalAlign="Center">
                                            <ItemTemplate>
                                                <asp:DropDownList ID="DDStore" runat="server" Style="width: 3em; text-align: center" AutoPostBack="true" OnSelectedIndexChanged="DDStore_SelectedIndexChanged">
                                                    <asp:ListItem Value="0">- ?-</asp:ListItem>
                                                </asp:DropDownList>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="BarCode (Scan)">
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtBarcode" runat="server" AutoPostBack="true" OnTextChanged="txtBarcode_TextChanged" Text='<%# Eval("BarCode") %>' oninput="handleBarcodeScan(this)" Style="width: 9em">></asp:TextBox>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Lot #" HeaderStyle-Width="4em">
                                            <ItemTemplate>
                                                <asp:Label ID="lblLotNum" runat="server" style="display:none" Text='<%# Eval("LotNumber") %>' ></asp:Label>
                                                <asp:DropDownList ID="DDlotNum" runat="server" OnSelectedIndexChanged="DDlotNum_SelectedIndexChanged" style="width:8.2em; text-align:center">
                                                    <%--<asp:ListItem Value="0">- Lot Number-</asp:ListItem>--%>
                                                </asp:DropDownList>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Unit">
                                            <ItemTemplate>
                                                <asp:Label ID="txtUnit" runat="server" Width="3em" Text='<%# Eval("Unit") %>'></asp:Label>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Qty" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtQty" runat="server" Width="6em" Style="text-align: center" Text='<%# Eval("Quantity" , "{0:N2}") %>' onkeydown='<%# "triggerSaveOnEnter(event, \"" + ((GridViewRow)Container).FindControl("lbtnLineSave").ClientID + "\")" %>'></asp:TextBox>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Use Qty *" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" HeaderStyle-ForeColor="Red">
                                        <ItemTemplate>
                                            <asp:TextBox ID="txtUseQty" runat="server" Width="6em" Style="text-align: center" Text='<%# Eval("LinePickQty" , "{0:N2}") %>' onkeydown='<%# "triggerSaveOnEnter(event, \"" + ((GridViewRow)Container).FindControl("lbtnLineSave").ClientID + "\")" %>'></asp:TextBox>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-Width="2em">
                                                <HeaderTemplate ><asp:Label ID="Label1" runat="server" Text="Done"></asp:Label><br />
                                                    <asp:CheckBox ID="chkSelectAll" runat="server" AutoPostBack="true" OnCheckedChanged="chkSelectAll_CheckedChanged" />
                                                </HeaderTemplate>
                                            <ItemTemplate>
                                                    <asp:CheckBox ID="chkCompl" runat="server" Checked='<%# Eval("PickComplete") != DBNull.Value && Convert.ToBoolean(Eval("PickComplete")) %>' AutoPostBack="true" OnCheckedChanged="chkComplete_CheckedChanged"/>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="lbtnLineSave" CommandArgument='<%# Eval("LineID") %>' CommandName="lbtnLineSave" runat="server" CssClass="fa fa-save buttonRed" ToolTip="Save Line To Job Card" OnClick="lbtnLineSave_Click" style="font-size:.85em"> </asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="lbtnComment" CommandArgument='<%# Eval("LineID") %>' CommandName="lbtnComment" runat="server" CssClass="fa fa-edit buttonRed" ToolTip="Add a line comment" OnClick="lbtnComment_Click" Style="color: #4282C1; font-size:.85em"> </asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="lbtnDeleteLine" CommandArgument='<%# Eval("LineID") %>' CommandName="lbtnDeleteLine" runat="server" CssClass="fa fa-ban" ToolTip="Delete Line" OnClick="lntnDeleteLine_Click" Style="color: red; font-size:.85em"> </asp:LinkButton>
                                                <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Delete Job Card Line?" Enabled="True" TargetControlID="lbtnDeleteLine"></cci:ConfirmButtonExtender>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                                </div>
                            <div class="10u 12u$(medium)">
                                <asp:Panel ID="PnlMsg" runat="server">
                                    <table style="width: 100%">
                                        <tr>
                                            <td>
                                                <h4>Job Card Message </h4>
                                            </td>
                                            <td>
                                                <h4>Packing Message </h4>
                                            </td>
                                            <td>
                                                <h4>Delivery Message </h4>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                <asp:TextBox ID="txtWMsg" runat="server" Rows="5" Columns="40" TextMode="MultiLine" Style="width: 100%; border:1px darkgreen solid"></asp:TextBox>
                                            </td>
                                            <td>
                                                <asp:TextBox ID="txtPMsg" runat="server" Rows="5" Columns="40" TextMode="MultiLine" Style="width: 100%; border:1px darkgreen solid"></asp:TextBox></td>
                                            <td>
                                                <asp:TextBox ID="txtDMsg" runat="server" Rows="5" Columns="40" TextMode="MultiLine" Style="width: 100%; border:1px darkgreen solid"></asp:TextBox></td>
                                        </tr>
                                    </table>
                                </asp:Panel>
                           </div>
                           
                            <div class="2u 12u$(medium)"><h4><asp:LinkButton ID="lbtnHist" runat="server" class="buttonC  icon fa-search-plus" OnClick="lbtnHist_Click" style="width:100%; color:#4282C1; margin:0.25em" ToolTip="Open Detailed View">&nbsp;Process Trail</asp:LinkButton></h4>
                                   <asp:GridView ID="GridHistLines" runat="server" AutoGenerateColumns="false" CssClass="gridview" RowStyle-Wrap="true" Style="font-size: .85em">
                                        <HeaderStyle CssClass="gridViewHeader" />
                                        <RowStyle CssClass="gridViewRow" />
                                        <AlternatingRowStyle CssClass="gridViewAltRow" />
                                        <FooterStyle CssClass="gridViewHeader" />
                                        <PagerStyle CssClass="gridViewPager" />
                                        <Columns>
                                            <asp:BoundField HeaderText="Date & Time" DataField="MoveDate" ReadOnly="True" ItemStyle-Width="12em" />
                                             <asp:BoundField HeaderText="Process" DataField="ToProcess" ReadOnly="True" />
                                        </Columns>
                                    </asp:GridView>
                            </div>
                            </div>
                        <div class="row 150%">
                            <div class="12u 12u$(medium)" style="text-align: center">
                                <asp:Panel ID="PnlButtons" runat="server">
                                <asp:LinkButton ID="lbtnDelJC" CssClass="icon fa-ban buttonTransparent" runat="server" ForeColor="Red" ToolTip="Delete Job Card" OnClick="lbtnDelJC_Click" Style="margin-right: 2em"> Delete</asp:LinkButton>
                                 <cci:ConfirmButtonExtender ID="ConfirmButtonExtender2" runat="server" ConfirmText="WARNING - Delete this job card? Are you sure?" Enabled="True" TargetControlID="lbtnDelJC"></cci:ConfirmButtonExtender>
                                <asp:LinkButton ID="lbtnJCPrint" runat="server" class="buttonRed icon fa-print" OnClick="lbtnPrintJC_Click" Style="margin-right: 2em">&nbsp;Print Preview</asp:LinkButton>
                                <asp:LinkButton ID="LbtnSaveEdits" runat="server" OnClick="LbtnSaveEdits_Click" CssClass=" icon fa-edit buttonSage" Style="margin-right: 2em" ToolTip="Save Edits"> Save Edits</asp:LinkButton>
                                <asp:LinkButton ID="LbtnJCSave" runat="server" OnClick="LbtnJCSave_Click" CssClass=" icon fa-save buttonRed" ToolTip="Save Job Card as Sales Order" style="display:inline-block"> Close off Job Card As Complete</asp:LinkButton>
                                <cci:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" ConfirmText="Confirm: Mark Job Card as complete?" Enabled="True" TargetControlID="LbtnJCSave"></cci:ConfirmButtonExtender>
                                <br /><br /><br />
                                    </asp:Panel>
                            </div>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>

                <asp:LinkButton ID="LinkButton2" runat="server" Style="display: none">LinkButton</asp:LinkButton>
                <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton2"></cci:ModalPopupExtender>
                <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                    <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" Style="float: right" ToolTip="Cancel"> </asp:LinkButton>
                    <div class="HellowWorldPopup">
                        <div id="Div4" class="PopupHeader">
                            <h2>Add message</h2>
                            <asp:Label ID="lblSender" runat="server" Text="Label" Style="display: none"></asp:Label><asp:Label ID="lblLineid" runat="server" Text="Label" Style="display: none"></asp:Label>
                        </div>
                        <div class="PopupBody">
                            <asp:TextBox ID="txtMsgBody" runat="server" Rows="5" Columns="40" TextMode="MultiLine"></asp:TextBox>
                        </div>
                        <div class="Controls">
                            <input id="btnCancel5" type="button" class="fa fa-times-circle" value="" runat="server" style="display: none" />
                            <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display: none" />
                            <asp:LinkButton ID="btnSaveComment" runat="server" CssClass="buttonCancel" OnClick="btnSaveComment_Click">OK</asp:LinkButton>
                        </div>
                    </div>
                </asp:Panel>
                <asp:LinkButton ID="LinkButton3" runat="server" Style="display: none">LinkButton</asp:LinkButton>
                <cci:ModalPopupExtender ID="ModalPopupExtender1" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="lbtnCancel2" Drag="true" OkControlID="lbtnOK2" PopupControlID="PnlKit" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton3"></cci:ModalPopupExtender>
                <asp:Panel ID="PnlKit" runat="server" Style="display: none" DefaultButton="lbtnOKKit">
                    <asp:LinkButton ID="lbtnCancel2" runat="server" CssClass="fa fa-times" Style="float: right" ToolTip="Cancel"> </asp:LinkButton>
                    <div class="HellowWorldPopup">
                        <div id="DivKit" class="PopupHeader">
                            <h3>Create from Kit:
                                <asp:Label ID="lblKitCode" runat="server" Text=""></asp:Label></h3>
                        </div>
                        <div class="PopupBody">
                            <h5>How many Kits are to be manufactured?</h5>
                            <asp:TextBox ID="txtKitCount" runat="server" Style="text-align: center; width: 3em"></asp:TextBox>
                            <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtKitCount" FilterType="Numbers" ValidChars="" />
                        </div>
                        <div class="Controls">
                            <input id="lbtnOK2" type="button" class="buttonYellow" value="OK" runat="server" style="display: none" />
                            <asp:LinkButton ID="lbtnOKKit" runat="server" CssClass="buttonCancel" OnClick="lbtnOKKit_Click">Go Create Kits</asp:LinkButton><br />
                            <br />
                        </div>
                    </div>
                </asp:Panel>

                <asp:LinkButton ID="LinkButton1" runat="server" Style="display: none">LinkButton</asp:LinkButton>
                <cci:ModalPopupExtender ID="ModalPopupExtender2" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="lbtnCancel2B" Drag="true" OkControlID="lbtnOK2B" PopupControlID="PnlBundles" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton1"></cci:ModalPopupExtender>
                <asp:Panel ID="PnlBundles" runat="server" Style="display: none" DefaultButton="lbtnOKBundle">
                    <asp:LinkButton ID="lbtnCancel2B" runat="server" CssClass="fa fa-times" Style="float: right" ToolTip="Cancel"> </asp:LinkButton>
                    <div class="HellowWorldPopup">
                        <div id="DivBundle" class="PopupHeader">
                            <h3>Assemble from Bundle:
                                <asp:Label ID="lblBundleCode" runat="server" Text=""></asp:Label></h3>
                        </div>
                        <div class="PopupBody">
                            <h5>How many Bundles?</h5>
                            <asp:TextBox ID="txtBundleCount" runat="server" Style="text-align: center; width: 3em"></asp:TextBox>
                            <cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender1" runat="server" TargetControlID="txtBundleCount" FilterType="Numbers" ValidChars="" />
                        </div>
                        <div class="Controls" style="text-align:center">
                            <input id="lbtnOK2B" type="button" class="buttonYellow" value="OK" runat="server" style="display: none" />
                            <asp:LinkButton ID="lbtnOKBundle" runat="server" CssClass="buttonCancel" OnClick="lbtnOKBundle_Click">Create</asp:LinkButton><br />
                            <br />
                        </div>
                    </div>
                </asp:Panel>

                 <asp:LinkButton ID="LinkButton4" runat="server" Style="display: none">LinkButton</asp:LinkButton>
                 <cci:ModalPopupExtender ID="ModalPopupExtender3" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="lbtnCancel5B" Drag="true" OkControlID="lbtnOK5B" PopupControlID="PnLIssue" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton4"></cci:ModalPopupExtender>
                 <asp:Panel ID="PnLIssue" runat="server" Style="display: none" DefaultButton="lbtnOKBundle">
                     <asp:LinkButton ID="lbtnCancel5B" runat="server" CssClass="fa fa-times" Style="float: right" ToolTip="Cancel"> </asp:LinkButton>
                     <div class="HellowWorldPopup">
                         <div id="DivIssueTo" class="PopupHeader">
                             <h3>Issue To
                                 <asp:Label ID="Label2" runat="server" Text=""></asp:Label></h3>
                         </div>
                         <div class="PopupBody">
                             <table style="margin:auto">
                                 <tr >
                                     <td> Role: </td>
                                     <td style="padding-bottom:1em"><asp:DropDownList ID="DDJCRoles" runat="server" Width="150px"></asp:DropDownList></td>        
                                 </tr>
                                 <tr>
                                     <td> Department:</td>
                                     <td> <asp:DropDownList ID="DDept" runat="server" Width="150px"></asp:DropDownList></td>
                                 </tr>
                             </table>
                         </div>
                         <div class="Controls" style="text-align:center">
                             <input id="lbtnOK5B" type="button" class="buttonYellow" value="OK" runat="server" style="display: none" />
                             <asp:LinkButton ID="lbtnOKIssue" runat="server" CssClass="buttonCancel" OnClick="lbtnOKIssue_Click">Issue</asp:LinkButton><br />
                             <br />
                         </div>
                     </div>
                 </asp:Panel>
            </div>
        </div>
    </form>
    <script type="text/javascript">
        function handleBarcodeScan(barcodeTextBox) {
            setTimeout(function () {
                var currentRow = barcodeTextBox.closest('tr');
                var unitTextBox = currentRow.querySelector("[id*='txtUnit']");
                if (unitTextBox) {
                    unitTextBox.focus();
                }
            }, 100); // Adjust the delay if needed    
        }
</script>
    <script type="text/javascript">
        function triggerSaveOnEnter(event, saveButtonId) {
            if (event.key === "Enter") {
                event.preventDefault(); // Prevent the default form submission
                document.getElementById(saveButtonId).click(); // Trigger the click event on the save button
            }
        }
</script>
</body>
</html>
