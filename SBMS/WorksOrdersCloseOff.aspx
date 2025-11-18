<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="WorksOrdersCloseOff.aspx.cs" Inherits="SBMS.WorksOrdersCloseOff" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Manufacture Update</title>
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
                        <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" OnClick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="LinkButton2" runat="server" class="buttonC icon fa-edit" PostBackUrl="~/WorksOrdersManfHeaders.aspx" ToolTip="View all open works orders">&nbsp;Works Orders</asp:LinkButton>
                        <asp:LinkButton ID="lbtnMRPThis" runat="server" class="buttonC icon fa-book" ToolTip="View Materials Requirements for this Works Order." OnClick="lbtnMRPThis_Click">&nbsp;Raw Materials Allocations</asp:LinkButton>
                        <asp:LinkButton ID="lbtnMRP" runat="server" class="buttonC icon fa-align-justify" PostBackUrl="~/FGDemands.aspx">&nbsp;MRP: Raw Materials</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" Style="float: right" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <br />
                        <h3 id="woheader" runat="server" style="padding-top: 0; line-height: 1em"></h3>
                    </div>
                    <div class="2u 12u$(medium)">
                       <asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" />
                    </div>
                </div>
                <div class="row 150%">
                    <div class="1u 12u$(medium)">&nbsp;</div>
                    <div class="10u 12u$(medium)">
                        <div style="display: none">
                            <asp:Label ID="lblDir" runat="server" Text=""></asp:Label>
                        </div>
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
                                        <asp:DropDownList ID="DDStatus" runat="server" Style="width: 100%" Enabled="false">
                                        </asp:DropDownList></td>
                                </tr>
                                <tr>
                                    <td>Linked Document #:</td>
                                    <td>
                                        <asp:TextBox ID="txtLinkedDoc" runat="server" Text="" Enabled="false"></asp:TextBox></td>
                                    <td>Notes:</td>
                                    <td colspan="3">
                                        <asp:TextBox ID="txtwomsg" runat="server" Text="" Style="width: 100%"></asp:TextBox></td>
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
                        <h3>Works Order Details<br /><span style="font-size:small">(Select row to allocate raw materials)</span></h3>
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
                            <asp:LinkButton ID="lbtnWOPrint" runat="server" class="buttonRed icon fa-print" OnClick="lbtnWOPrint_Click" >&nbsp;Print Preview</asp:LinkButton>
                             <asp:LinkButton ID="LbtnSaveWO" runat="server" CssClass="icon fa-save buttonSage" OnClick="LbtnSaveWO_Click">&nbsp;Save Work Order</asp:LinkButton>
                            <asp:LinkButton ID="LbtnUpdateWO" runat="server" CssClass="icon fa-upload buttonIndex" OnClick="LbtnUpdateWO_Click" ToolTip="Only completed lines can be transferred to stores and Quantity On Hand levels updated.">&nbsp;Transfer Items To Stores and Update Sage</asp:LinkButton>
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
                            <asp:GridView ID="GridUseBom" runat="server" AutoGenerateColumns="false" CssClass="gridview">
                                <HeaderStyle CssClass="gridViewHeader" />
                                <RowStyle CssClass="gridViewRow" />
                                <AlternatingRowStyle CssClass="gridViewAltRow" />
                                <Columns>
                                    <asp:BoundField HeaderText="Item" DataField="ItemCode" />
                                    <asp:BoundField HeaderText="Description" DataField="ItemDescription" />
                                    <asp:BoundField HeaderText="Quantity" DataField="Quantity" />
                                    <asp:BoundField HeaderText="Lot_Number" DataField="LotNumber" />
                                    
                                </Columns>
                            </asp:GridView>
                        </div>
                        <div class="Controls">
                            <input id="Button1" type="button" class="fa fa-times-circle" value="" runat="server" style="display: none" />
                            <input id="BomOK" type="button" class="buttonYellow" value="OK" runat="server" style="display: none" />
                            <asp:LinkButton ID="lBtnSave" runat="server" CssClass="buttonCancel">OK</asp:LinkButton>
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
                                Item Code:<br /><asp:DropDownList ID="ddlItemCode" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlItemCode_SelectedIndexChanged"></asp:DropDownList><br /><br />
                                <asp:Label ID="lblDescript" runat="server" Text=""></asp:Label><br /><br />
                                Qty:<br />
                                <asp:TextBox ID="txtAddQty" runat="server" style="width:4em; text-align:center" ></asp:TextBox>
                            </h4>
                                   
                        </div>
                        <div class="Controls">
                            <input id="lbtnAddYes" type="button" class="buttonYellow" value="OK" runat="server" style="display: none" />
                            <asp:LinkButton ID="lbtnAddYesM" runat="server" CssClass="icon fa-save buttonSage" OnClick="lbtnAddYesM_Click">OK</asp:LinkButton>
                        </div>
                    </div>
                </asp:Panel>

                <asp:LinkButton ID="LinkButton5" runat="server" Style="display: none">LinkButton</asp:LinkButton>
                    <cci:ModalPopupExtender ID="ModalPopupExtender3" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnAddLotCancel" Drag="true" OkControlID="btnLotAdd" PopupControlID="PanelAddLot" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton4"></cci:ModalPopupExtender>
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
</body>
</html>
