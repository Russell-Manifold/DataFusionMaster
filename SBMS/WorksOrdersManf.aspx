<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="WorksOrdersManf.aspx.cs" Inherits="SBMS.WorksOrdersManf" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Manufacture</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <script src="js/cost-calculation.js" type="text/javascript"></script>
     <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script> <!-- Simple button disable script -->
    <script type="text/javascript">
        function disableButtonsDuringProcess() {
            // Disable all buttons with specific classes
            var buttons = document.querySelectorAll('.buttonC, .buttonRed, .buttonSage, .buttonIndex, .fa-print, .fa-save, .fa-upload');
            buttons.forEach(function (button) {
                button.disabled = true;
                button.style.opacity = '0.5';
                button.style.pointerEvents = 'none';
                // Do NOT overwrite onclick to avoid breaking ConfirmButtonExtender
                // button.onclick = function () { return false; };
            });

            // Change the update button text
            var updateBtn = document.getElementById('<%= LbtnUpdateWO.ClientID %>');
            if (updateBtn) {
                updateBtn.innerHTML = '<i class="fa fa-spinner fa-spin"></i> Processing...';
            }
            
            // Prevent navigation
            window.onbeforeunload = function() {
                return "Processing in progress. Please wait...";
            };
        }
        
        function enableButtonsAfterProcess() {
            // Re-enable all buttons
            var buttons = document.querySelectorAll('.buttonC, .buttonRed, .buttonSage, .buttonIndex, .fa-print, .fa-save, .fa-upload');
            buttons.forEach(function(button) {
                button.disabled = false;
                button.style.opacity = '1';
                button.style.pointerEvents = 'auto';
                // Do NOT overwrite onclick to avoid breaking ConfirmButtonExtender
                // button.onclick = null;
            });
            
            // Restore update button text
            var updateBtn = document.getElementById('<%= LbtnUpdateWO.ClientID %>');
            if (updateBtn) {
                updateBtn.innerHTML = '<i class="fa fa-upload"></i> Transfer Items To Stores and Update Sage';
            }

            // Remove navigation prevention
            window.onbeforeunload = null;
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="content">
            <div class="container">
                <div class="row 150%">
                    <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>
                    <div class="8u 12u$(medium)">
                        <asp:LinkButton ID="LinkButton2" runat="server" class="buttonC icon fa-arrow-left" PostBackUrl="~/WorksOrdersManfHeaders.aspx" ToolTip="View all open works orders">&nbsp;Works Orders</asp:LinkButton>
                        <asp:LinkButton ID="lbtnMRPThis" runat="server" class="buttonC icon fa-book" ToolTip="View Materials Requirements for this Works Order." OnClick="lbtnMRPThis_Click">&nbsp;Raw Materials Allocations</asp:LinkButton>
                        <asp:LinkButton ID="lbtnCloseRemaining" runat="server" class="buttonC icon fa-flag-checkered" ToolTip="Close the outstanding balance and complete this Works Order." OnClick="lbtnCloseRemaining_Click" Visible="false">&nbsp;Close Remaining</asp:LinkButton>
                        <cci:ConfirmButtonExtender ID="cbeCloseRemaining" runat="server" ConfirmText="Close the remaining balance and complete this Works Order? This cannot be undone." Enabled="True" TargetControlID="lbtnCloseRemaining"></cci:ConfirmButtonExtender>
                        <br />
                        <h3 id="woheader" runat="server" style="padding-top: 1em; line-height: 1em"></h3>
                        <asp:Label ID="lblwoid" runat="server" Text="" style="display:none"></asp:Label>
                    </div>
                    <div class="2u 12u$(medium)">
                       <asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" />
                    </div>
                </div>
                <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
                    <ProgressTemplate>
                        <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.7;">
                            <div style="position: relative; top: 40%; background: white; padding: 20px; border-radius: 10px; display: inline-block;">
                                <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Processing ..." />
                                <br />
                                <strong>Processing - Please Wait</strong>
                                <br />
                                <span style="font-size: 12px; color: #666;">Do not close or refresh the page</span>
                            </div>
                        </div>
                    </ProgressTemplate>
                </asp:UpdateProgress>
            <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                <ContentTemplate>
                <div class="row 150%">
                    <div class="1u 12u$(medium)">&nbsp;</div>
                    <div class="10u 12u$(medium)">
                        <div style="display: none"><asp:Label ID="lblDir" runat="server" Text=""></asp:Label> </div>
                        <asp:Panel ID="Panel1" runat="server">
                            <table style="width: 100%">
                                <tr>
                                    <td>Customer:</td>
                                    <td>
                                        <asp:TextBox ID="lblFCCustName" runat="server" Text="" Enabled="false"></asp:TextBox></td>
                                    <td>Created Date:</td>
                                    <td>
                                        <asp:Label ID="lblcreatedDate" runat="server" Text="" Enabled="false"></asp:Label></td>
                                    <td>Due Date</td>
                                    <td>
                                        <asp:TextBox ID="txtDueDate" runat="server" Text="" Enabled="false"></asp:TextBox></td>
                                </tr>
                                <tr>
                                    <td>Reference:</td>
                                    <td>
                                        <asp:TextBox ID="lblFCRef" runat="server" Text="" Enabled="false"></asp:TextBox></td>
                                    <td>Created By:</td>
                                    <td>
                                        <asp:TextBox ID="lblCreatedBy" runat="server" Text="" Enabled="false"></asp:TextBox></td>
                                    <td>Status</td>
                                    <td>
                                        <asp:DropDownList ID="DDStatus" runat="server" Style="width: 100%">
                                            <asp:ListItem>New</asp:ListItem>
                                            <asp:ListItem>Planned</asp:ListItem>
                                            <asp:ListItem>In Progress</asp:ListItem>
                                            <asp:ListItem>Partially Manufactured</asp:ListItem>
                                            <asp:ListItem>Complete</asp:ListItem>
                                        </asp:DropDownList></td>
                                </tr>
                                <tr>
                                    <td>Linked Document #:</td>
                                    <td>
                                        <asp:TextBox ID="txtLinkedDoc" runat="server" Text="" Enabled="false"></asp:TextBox></td>
                                    <td>Notes:</td>
                                    <td colspan="3">
                                        <asp:TextBox ID="txtwomsg" runat="server" Text="" Style="width: 100%" ></asp:TextBox></td>
                                </tr>
                            </table>
                        </asp:Panel>
                    </div>
                    <div class="1u 12u$(medium)">&nbsp;</div>
                </div>
                
                <div class="row 150%">
                    <div class="1u 12u$(medium)">&nbsp;</div>
                    <div class="10u 12u$(medium)">
                        <hr />
                        <h3>Works Order Details <asp:CheckBox ID="chkCompl" runat="server" Text="Complete" Enabled="false" Font-Size="Small" />
                            <asp:Panel ID="pnlDrawFrom" runat="server" Visible="false" style="display:inline-block; margin:auto; font-size:.55em; font-weight:normal; line-height:2.0em">
                                Auto-Manufacture draw components from:&nbsp;<asp:DropDownList ID="ddlDrawFrom" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlDrawFrom_SelectedIndexChanged" Font-Bold="true" Width="60px" ></asp:DropDownList>
                            </asp:Panel>
                        </h3>
                        <cci:Accordion ID="AccordionWOLines" runat="server" CssClass="accordion" 
                                HeaderCssClass="accordionHeader" 
                                ContentCssClass="accordionContent"    
                                FadeTransitions="true" 
                                TransitionDuration="250"
                                tooltip="Click to select">
                            </cci:Accordion>          
                    </div>

                    <div class="1u 12u$(medium)">&nbsp;</div>
                     </div>
                    <div class="row 150%">
                      <div class="1u 12u$(medium)">&nbsp;</div>
                    <div class="10u 12u$(medium)">   
                        <div id="pnlbtn" runat="server" style="width:100%; text-align:center; margin-top:2em"> 
                            <asp:Label ID="lblReccount" runat="server" Text=""></asp:Label>
                            <asp:LinkButton ID="lbtnResetLines" runat="server" CssClass="buttonRed icon fa-undo" Style="color:red; border:none" ToolTip="Undo splits: consolidate all incomplete lines back into one per item." OnClick="lbtnResetLines_Click" Visible="false">&nbsp;Reset Lines</asp:LinkButton>
                            <cci:ConfirmButtonExtender ID="cbeResetLines" runat="server" ConfirmText="Reset and consolidate all incomplete (un-manufactured) lines back into one per item? Completed batches are kept." Enabled="True" TargetControlID="lbtnResetLines"></cci:ConfirmButtonExtender>
                            <asp:LinkButton ID="lbtnWOPrint" runat="server" class="buttonRed icon fa-print" OnClick="lbtnWOPrint_Click" >&nbsp;Print Preview</asp:LinkButton>
                             <asp:LinkButton ID="LbtnSaveWO" runat="server" CssClass="icon fa-save buttonSage" OnClick="LbtnSaveWO_Click">&nbsp;Save Work Order</asp:LinkButton>
                            <asp:LinkButton ID="LbtnUpdateWO" runat="server" CssClass="icon fa-upload buttonIndex" 
                                    OnClick="LbtnUpdateWO_Click" 
                                    OnClientClick="disableButtonsDuringProcess(); return true;"
                                    ToolTip="Only completed lines can be transferred to stores and Quantity On Hand levels updated.">
                                    &nbsp;Transfer Items To Stores and Update Sage
                                </asp:LinkButton>
                            <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, update Sage and carry out all stock adjustments?" Enabled="True" TargetControlID="LbtnUpdateWO"></cci:ConfirmButtonExtender>
                        </div>
                     </div>
                        <div class="1u 12u$(medium)">&nbsp;</div>
                </div>
                <asp:LinkButton ID="LinkButton3" runat="server" Style="display: none">LinkButton</asp:LinkButton>
                <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton3"></cci:ModalPopupExtender>
                <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                    <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" Style="float: right" ToolTip="Cancel"> </asp:LinkButton>
                    <div class="HellowWorldPopup">
                        <div id="Div4" class="PopupHeader">
                            <h2>Add Comment</h2>
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

                <asp:LinkButton ID="LinkButton4" runat="server" Style="display: none">LinkButton</asp:LinkButton>
                <cci:ModalPopupExtender ID="ModalPopupExtender1" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="BomClose" Drag="true" OkControlID="BomOK" PopupControlID="PNLBom" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton4"></cci:ModalPopupExtender>
                <asp:Panel ID="PNLBom" runat="server" Style="display: none">
                    <asp:LinkButton ID="BomClose" runat="server" CssClass="fa fa-times" Style="float: right" ToolTip="Cancel"> </asp:LinkButton>
                    <div class="HellowWorldPopup">
                        <div id="Div44" class="PopupHeader">
                            <h2>Raw Materials Allocations</h2>
                        </div>
                        <div class="PopupBody" style="margin: 2em">
                            <h4>
                                <asp:Label ID="lblItem" runat="server" Text="Label"></asp:Label>
                                &nbsp; &nbsp;<asp:Label ID="lblQty" runat="server" Text="Label"></asp:Label>
                            </h4>
                            <asp:GridView ID="GridUseBom" runat="server" AutoGenerateColumns="false" CssClass="gridview" OnRowDataBound="GridUseBom_RowDataBound">
                                <HeaderStyle CssClass="gridViewHeader" />
                                <RowStyle CssClass="gridViewRow" />
                                <AlternatingRowStyle CssClass="gridViewAltRow" />
                                <Columns>
                                    <asp:BoundField HeaderText="Item" DataField="ItemCode" />
                                    <asp:BoundField HeaderText="Description" DataField="ItemDescription" />
                                    <asp:BoundField HeaderText="Quantity" DataField="Quantity" />
                                    <asp:BoundField HeaderText="Use Qty" DataField="UseQty" />
                                    <asp:BoundField HeaderText="Scrap" DataField="ScrapQty" />
                                    <asp:BoundField HeaderText="Lot_Number" DataField="LotNumber" />
                                    <asp:TemplateField HeaderText="On Hand" ItemStyle-HorizontalAlign="Center"><ItemTemplate><asp:Label ID="lblOnHand" runat="server"></asp:Label></ItemTemplate></asp:TemplateField>
                                    <asp:TemplateField HeaderText="Short" ItemStyle-HorizontalAlign="Center"><ItemTemplate><asp:Label ID="lblShort" runat="server"></asp:Label></ItemTemplate></asp:TemplateField>
                                </Columns>
                            </asp:GridView>
                        </div>
                        <div class="Controls">
                            <input id="Button1" type="button" class="fa fa-times-circle" value="" runat="server" style="display: none" />
                            <input id="BomOK" type="button" class="buttonYellow" value="OK" runat="server" style="display: none" />
                            <asp:LinkButton ID="lBtnSave" runat="server" CssClass="buttonCancel">OK</asp:LinkButton><br />
                        </div>
                    </div>
                </asp:Panel>

                <asp:LinkButton ID="LinkButton1" runat="server" Style="display: none">LinkButton</asp:LinkButton>
                <cci:ModalPopupExtender ID="ModalPopupExtender2" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="lbtnAddCancel" Drag="true" OkControlID="lbtnAddYes" PopupControlID="PnlAddLine" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton1"></cci:ModalPopupExtender>
                <asp:Panel ID="PnlAddLine" runat="server" Style="display: none">
                    <asp:LinkButton ID="lbtnAddCancel" runat="server" CssClass="fa fa-times" Style="float: right" ToolTip="Cancel"> </asp:LinkButton>
                    <div class="HellowWorldPopup">
                        <div id="Div43" class="PopupHeader">
                            <h2>Add Item To BOM</h2>
                        </div>
                        <div class="PopupBody" style="margin: 2em">
                            <h4>
                                <asp:Label ID="woLineID" runat="server" Text="" style="display:none"></asp:Label><asp:Label ID="WordID" runat="server" Text="" style="display:none"></asp:Label>
                               <table style="text-align:left">
                                   <tr>
                                       <td > Item Code:</td>
                                       <td><asp:DropDownList ID="ddlItemCode" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlItemCode_SelectedIndexChanged" Width="250px"></asp:DropDownList></td>
                                   </tr>
                                   <tr>
                                       <td colspan="2" style="padding:1em"><asp:Label ID="lblDescript" runat="server" Text="" style="text-align:left">&nbsp;</asp:Label></td>
                                   </tr>
                                   <tr>
                                       <td> Qty Required:</td>
                                       <td><asp:TextBox ID="txtqty" runat="server" style="width:150px; text-align:center">1</asp:TextBox></td>
                                       <cci:FilteredTextBoxExtender ID="ftbeP" runat="server" TargetControlID="txtqty" FilterType="Numbers,Custom" ValidChars="." />
                                   </tr>
                                   <tr>
                                       <td> Store:</td>
                                    <td><asp:DropDownList ID="DDItemAddStore" runat="server" Width="50px"></asp:DropDownList></td>
                                </tr>
                               </table>
                            </h4>                   
                        </div>
                        <div class="Controls">
                            <input id="lbtnAddYes" type="button" class="buttonYellow" value="OK" runat="server" style="display: none" />
                            <asp:LinkButton ID="lbtnAddYesM" runat="server" CssClass="icon fa-plus-circle buttonSage" OnClick="lbtnAddYesM_Click"> OK</asp:LinkButton>
                        </div>
                    </div>
                </asp:Panel>

                <asp:LinkButton ID="LinkButton5" runat="server" Style="display: none">LinkButton</asp:LinkButton>
                    <cci:ModalPopupExtender ID="ModalPopupExtender3" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnAddLotCancel" Drag="true" OkControlID="btnLotAdd" PopupControlID="PanelAddLot" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton5"></cci:ModalPopupExtender>
                    <asp:Panel ID="PanelAddLot" runat="server" Style="display: none">
                        <asp:LinkButton ID="btnAddLotCancel" runat="server" CssClass="fa fa-times" Style="float: right" ToolTip="Cancel"> </asp:LinkButton>
                        <div class="HellowWorldPopup">
                            <div id="Div41" class="PopupHeader">
                                <h2>Add Lot Number</h2>
                                <h3><asp:Label ID="lblAddLotItem" runat="server" Text=""></asp:Label></h3>
                                <asp:Label ID="lblRowid" runat="server" Text="Label" Style="display: none"></asp:Label><asp:Label ID="lblItemD" runat="server" Text="Label" Style="display: none"></asp:Label>
                            </div>
                            <div class="PopupBody">
                                <asp:TextBox ID="txtLotNum" runat="server"></asp:TextBox>
                            </div>
                            <div class="Controls">
                                <input id="btnLotAdd" type="button" class="buttonYellow" value="OK" runat="server" style="display: none" />
                                <asp:LinkButton ID="btnLotAddL" runat="server" CssClass="buttonCancel icon fa-save" OnClick="btnLotAddL_Click"> Save</asp:LinkButton>
                            </div>
                        </div>
                    </asp:Panel>

                <asp:LinkButton ID="LinkButtonPM" runat="server" Style="display: none">LinkButton</asp:LinkButton>
                <cci:ModalPopupExtender ID="ModalPopupPartManf" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnPMCancel" Drag="true" OkControlID="btnPMOk" PopupControlID="PnlPartManf" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButtonPM"></cci:ModalPopupExtender>
                <asp:Panel ID="PnlPartManf" runat="server" Style="display: none">
                    <asp:LinkButton ID="btnPMClose" runat="server" CssClass="fa fa-times" Style="float: right" ToolTip="Cancel"> </asp:LinkButton>
                    <div class="HellowWorldPopup">
                        <div id="DivPM" class="PopupHeader">
                            <h2>Part Manufacture</h2>
                        </div>
                        <div class="PopupBody" style="margin: 2em">
                            <h4><asp:Label ID="lblPMItem" runat="server" Text=""></asp:Label></h4>
                            <asp:Label ID="lblPMOpenLineId" runat="server" Text="" Style="display: none"></asp:Label>
                            <table style="text-align: left">
                                <tr>
                                    <td>Remaining to produce:&nbsp;</td>
                                    <td><asp:Label ID="lblPMRemaining" runat="server" Text="" Font-Bold="true"></asp:Label></td>
                                </tr>
                                <tr>
                                    <td>Quantity to make now:&nbsp;</td>
                                    <td>
                                        <asp:TextBox ID="txtPartQty" runat="server" Style="text-align: center; width: 120px"></asp:TextBox>
                                        <cci:FilteredTextBoxExtender ID="ftbePM" runat="server" TargetControlID="txtPartQty" FilterType="Numbers,Custom" ValidChars="." />
                                    </td>
                                </tr>
                            </table>
                            <p style="font-size: 0.8em; color: #888">Entering the full remaining amount (or more) keeps it as one line. <br /> Splitting resets any quantities already allocated on this line.</p>
                        </div>
                        <div class="Controls">
                            <input id="btnPMCancel" type="button" class="fa fa-times-circle" value="" runat="server" style="display: none" />
                            <input id="btnPMOk" type="button" value="OK" runat="server" style="display: none" />
                            <asp:LinkButton ID="btnPartManfSave" runat="server" CssClass="buttonSage icon fa-cut" OnClick="btnPartManfSave_Click"> Log Part Quantity</asp:LinkButton>
                        </div>
                    </div>
                </asp:Panel>
        </ContentTemplate>
    </asp:UpdatePanel>
                </div>
            </div>
    </form>
    <script>
        function validateQuantityInput(event, input) {
            const key = event.key;
            const currentValue = input.value;

            // Allow numbers, one dot, and control keys (e.g., backspace)
            if (!/^\d$/.test(key) && key !== "." && !["Backspace", "Delete", "ArrowLeft", "ArrowRight"].includes(key)) {
                return false;
            }

            // Prevent multiple dots
            if (key === "." && currentValue.includes(".")) {
                return false;
            }
            return true;
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
    <script type="text/javascript">
        function calculateNewSell(txtNewGPId, lblThisCostId, txtNewSellId) {
            var txtNewGP = document.getElementById(txtNewGPId);
            var lblThisCost = document.getElementById(lblThisCostId);
            var txtNewSell = document.getElementById(txtNewSellId);

            if (!lblThisCost || !txtNewSell || !txtNewGP) return;

            var cost = parseFloat(lblThisCost.textContent || lblThisCost.innerText);
            var gp = parseFloat(txtNewGP.value);

            if (isNaN(cost) || isNaN(gp) || gp >= 100) {
                txtNewSell.value = '';
                return;
            }

            var newSell = cost / (1 - (gp / 100));
            txtNewSell.value = newSell.toFixed(2);
        }
    </script>
     <script src="<%= ResolveUrl("~/js/cost-calculation.js") %>"></script>
     <script src="<%= ResolveUrl("~/scripts/itemSearch.js") %>"></script>
</body>
</html>
