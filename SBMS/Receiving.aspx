<%--
    AsyncTimeout: an async page defaults to 45 SECONDS. Web.config's
    executionTimeout does not apply to async pages - it is a separate setting.
    Finalising a receipt makes up to 3 sequential Sage calls PER LINE
    (LoadOneItem + two ItemAdjustments) on top of the supplier invoice, so a
    10-line receipt is already ~30 calls. Past 45s ASP.NET aborted the request
    mid-pipeline, after the supplier invoice had posted but before the header
    resolved - leaving DocHeader.RecStatus stuck on 1 and the PO permanently
    blocked from further receiving.
--%>
<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="Receiving.aspx.cs" Inherits="SBMS.Receiving" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Receiving</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <script src="assets/js/buttonDisable.js"></script>
     <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</head>
<body>
    <form id="form1" runat="server">
         <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
         <asp:HiddenField ID="hfPartialOverride" runat="server" Value="" />
         <asp:HiddenField ID="hfPrintScope" runat="server" Value="" />
        <div class="content">
            <asp:UpdatePanel ID="UpdatePanel2" runat="server">
                <ContentTemplate>
                <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel2">
                    <ProgressTemplate>
                        <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.7;">
                            <div style="position: relative; top: 40%; background: white; padding: 20px; border-radius: 10px; display: inline-block;">
                                <asp:Image ID="imgUpdateProgress2" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Processing ..." />
                                <br />
                                <strong>Processing: Please Wait</strong>
                            </div>
                        </div>
                    </ProgressTemplate>
                </asp:UpdateProgress>
                    <div class="container">  
                 <div class="row 150%">
                    <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>        
                    <div class="8u 12u$(medium)">
                        <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="lbtnPOs" runat="server" class="buttonC icon fa-arrow-left" PostBackUrl="~/OSPurchaseOrders.aspx" >&nbsp;OS Purchase Orders</asp:LinkButton>                        
                        <h3 style="padding-top:0; line-height:1em; width:100%">Receiving </h3>    
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>

                <div class="row 150%">
                    <div class="1u 12u$(medium)">&nbsp;</div>
                    <div class="10u 12u$(medium)">
                        <table style="width:100%">
                            <tr>
                                <td colspan="4"><h6><asp:LinkButton ID="lbtnAtt" runat="server" class="buttonTransparent icon fa-paperclip" ToolTip="Attachments" OnClick="lbtnAtt_Click"></asp:LinkButton> Purchase Order Details</h6></td>
                            </tr>
                            <tr>
                                <td style="width:10em">Supplier </td>
                                <td><asp:TextBox ID="txtSuppName" runat="server" style="width:90%" ReadOnly="true"></asp:TextBox><asp:Label ID="lblSupplierID" runat="server" Text="" style="display:none"></asp:Label></td>
                                <td style="width:10em">PO Date </td>
                                <td><asp:TextBox ID="txtPODate" runat="server" ReadOnly="true"></asp:TextBox></td>
                            </tr>
                            <tr>
                                <td>Document No</td>
                                <td><asp:TextBox ID="txtDocNum" runat="server" ReadOnly="true"></asp:TextBox><asp:Label ID="lblDocID" runat="server" Text="" style="display:none"></asp:Label></td>
                                 <td>Reference</td>
                                <td><asp:TextBox ID="txtRef" runat="server" style="width:95%"></asp:TextBox></td>
                           </tr>
                            <tr>
                                <td>Message</td>
                                <td colspan="3"><asp:TextBox ID="txtMsg" runat="server" ReadOnly="true" style="width:98%" TextMode="MultiLine"></asp:TextBox></td>
                           </tr>
                            </table>
                        <table style="width:100%">
                            <tr>
                                <td></td>
                            <td colspan="7" style="text-align:center"><asp:Label ID="lblRefreshSummary" runat="server" Text="" style="color:#b35900; font-size:1em; display:none"></asp:Label></td>
                            </tr>
                            <tr>
                                <td></td>
                            <td colspan="7" style="text-align:center"><asp:Label ID="lblSaveStatus" runat="server" Text="" style="color:red; font-size:1.2em"></asp:Label></td>
                            </tr>
                           <tr>
                               <td  colspan="8" style="text-align:center"><h4>Receiving Details <span style="font-size:0.7em">**(DN Num OR Invoice Num required)</span></h4></td>
                           </tr>
                            <tr>
                                <td style="width:10em">D/N Num **</td>
                                <td><asp:TextBox ID="txtDNNum" runat="server" ></asp:TextBox></td>
                                <td><asp:Label ID="lblExRate" runat="server" Text="Exch Rate" ></asp:Label> </td>
                                <td><asp:TextBox ID="txtExRate" runat="server" BorderColor="red" BorderStyle="solid" BorderWidth="1px"></asp:TextBox><cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender4" runat="server" TargetControlID="txtExRate" FilterType="Custom, Numbers" ValidChars="." /></td>
                                <td>&nbsp;</td>
                                <td><asp:Label ID="Label1" runat="server" Text="Receiving Complete"></asp:Label><asp:CheckBox ID="chkReceiveComplete" runat="server" Text=" " Enabled="false"/></td>
                                <td>&nbsp;</td>
                                <td></td>
                            </tr>
                            <tr>
                            <tr>
                                <td>Inv Num **</td>
                                <td><asp:TextBox ID="txtInvNum" runat="server" ></asp:TextBox></td>
                                <td>Receive Date </td>
                                <td><asp:TextBox ID="txtRecDate" runat="server"></asp:TextBox>
                                    <cci:CalendarExtender ID="CalendarExtender1" runat="server" Enabled="True" TargetControlID="txtRecDate" Format="dd MMM yyyy"></cci:CalendarExtender>
                                </td>
                                <td>&nbsp;</td>
                                <td>Received By: <asp:Label ID="lblRecBy" runat="server" Text=" "></asp:Label></td>
                                <td>&nbsp;</td>
                                <td style="padding-right:1em"><asp:LinkButton ID="lbtnRecAll" runat="server" style="float:right; font-size:1em; margin:.25em; text-align:right;" CssClass="icon fa-list buttonRed" > Select All</asp:LinkButton></td>
                            </tr>
                        </table>                         
                        </div>
                    <div class="1u 12u$(medium)">&nbsp;</div>
                   </div>
                 <div class="row 150%">
                     <div class="1u 12u$(medium)">&nbsp;</div>
                     <div class="10u 12u$(medium)">   
                          <asp:GridView ID="GridPOLines" runat="server" AutoGenerateColumns="false" CssClass="gridview" RowStyle-Wrap="true" OnRowDataBound="GridPOLines_RowDataBound" ShowFooter="true" OnSelectedIndexChanged="GridPOLines_SelectedIndexChanged" >
                                    <HeaderStyle CssClass="gridViewHeader" />
                                            <RowStyle CssClass="gridViewRow" />
                                            <AlternatingRowStyle CssClass="gridViewAltRow" />
                                            <FooterStyle CssClass="gridViewHeader" />
                                            <PagerStyle CssClass="gridViewPager" />
                                            <Columns>
                                                <asp:BoundField DataField="LineID" ReadOnly="True" ItemStyle-Width="0em" ItemStyle-Font-Size="0.001em" ItemStyle-ForeColor="Transparent" />
                                                <asp:TemplateField HeaderText="Item_Code" ItemStyle-Width="5em" >
                                                    <ItemTemplate >
                                                        <asp:LinkButton ID="lbtnItmC" CommandArgument='<%# Eval("LineID")%>' CommandName="lbtnItmC" runat="server" Text='<%# Eval("ItemCode")%>' ToolTip="Receive this item" style="color:#4A82AB; font-weight:600; width:100%" OnClick="lbtnItmC_Click" ></asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:BoundField HeaderText="Description" DataField="ItemDescription" ReadOnly="True"  />
                                                <asp:BoundField HeaderText="Unit" DataField="Unit" ReadOnly="True" ItemStyle-Width="2em"  />
                                                <asp:BoundField HeaderText="Order_Qty" DataField="Quantity" ReadOnly="True" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="7em"/>    
                                                <asp:BoundField HeaderText="Qty_Left" DataField="QtyLeft" ReadOnly="True" ItemStyle-HorizontalAlign="Center"  HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="7em"/>
                                                <asp:BoundField HeaderText="Excl_Price" DataField="UnitPriceExclusive" ReadOnly="True" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right"  />
                                                <asp:BoundField HeaderText="PO Total" DataField="Total" ReadOnly="True" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right"  />    
                                                <asp:BoundField HeaderText="Receive_Qty" DataField="ReceiveQty" ReadOnly="True" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="7em" FooterStyle-HorizontalAlign="Center" />
                                                <asp:BoundField HeaderText="Rejects" DataField="RejectQty" ReadOnly="True" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="7em" ItemStyle-ForeColor="#B00000" HeaderStyle-ForeColor="#B00000" />
                                                <asp:TemplateField HeaderText="To_Receive" ItemStyle-Width="7em" ItemStyle-HorizontalAlign="Center">
                                                    <ItemTemplate>
                                                        <asp:CheckBox ID="chkUnloadComplete" runat="server" Checked='<%# Eval("ToReceive") %>' Enabled="false" Text=" " />
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:BoundField HeaderText="To_Store" DataField="StoreCode" ReadOnly="True" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="3em" />    
                                                <asp:BoundField HeaderText="LotNumber" DataField="LotNumber" ReadOnly="True" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" /> 
                                                <asp:TemplateField HeaderText="" ItemStyle-Width="1em">
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="lbtnLotNumAdd" runat="server" CssClass="buttonTransparent icon fa-plus" style="font-size:.8em; margin:-1em" CommandArgument='<%# Eval("LineID") %>' ToolTip="Fulfull this line item with more than one Lot Number" OnClick="lbtnLotNumAdd_Click" Visible="false"></asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:BoundField HeaderText="Receive_Total" DataField="ReceiveTotal" ReadOnly="True" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right"  />   
                                                <asp:BoundField DataField="QtyVar" ReadOnly="True" />
                                                <asp:BoundField DataField="ItemType" ReadOnly="True" />
                                            </Columns>
                           </asp:GridView>
                          <asp:Panel ID="PnlAddCosts" runat="server" style="float:left; margin-top:.25em; margin-right:1em; max-width:70%;">
                           <asp:LinkButton ID="LbtnAddCosts" runat="server" style="font-size:.9em; float:left" CssClass="icon fa-plus-square buttonCancel" ToolTip="Add Additional costs" >&nbsp;Estimated Addtional Costs</asp:LinkButton>
                              <asp:GridView ID="GridAddCosts" runat="server" AutoGenerateColumns="false" CssClass="gridview" showFooter="true" style="width:100%" OnRowDataBound="GridAddCosts_RowDataBound">
                              <HeaderStyle CssClass="gridViewHeader" />
                              <RowStyle CssClass="gridViewRow" />
                              <AlternatingRowStyle CssClass="gridViewAltRow" />
                              <FooterStyle CssClass="gridViewHeader" />
                              <PagerStyle CssClass="gridViewPager" />
                              <Columns>
                                <asp:BoundField HeaderText="Supplier" DataField="SupplierName" ReadOnly="True"  />
                                <asp:BoundField HeaderText="Item" DataField="Message" ReadOnly="True" />
                                <asp:BoundField HeaderText="Value (excl)" DataField="Exclusive" ReadOnly="True" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right"  />
                                  <asp:TemplateField ItemStyle-HorizontalAlign="Right">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="lbtnDeleteLine" CommandArgument='<%# Eval("ACID") %>' CommandName="lbtnDeleteLine" runat="server" CssClass="fa fa-ban" ToolTip="Delete Line" OnClick="lntnDeleteLine_Click" Style="color: red; font-size:.85em"> </asp:LinkButton>
                                            <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Delete Additional Cost Line?" Enabled="True" TargetControlID="lbtnDeleteLine"></cci:ConfirmButtonExtender>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                              </Columns>
                          </asp:GridView>
                             <asp:Panel ID="PnlAddCostNote" runat="server" style="font-size:0.8em">Note:The total value of ESTIMATED additional costs will be</br> automatically split and allocated to each PO line.
                                  <asp:LinkButton ID="lbtnAddCostInfo" runat="server" CssClass="fa fa-info-circle buttonTransparent" ToolTip="More info" OnClientClick="return false;"></asp:LinkButton>
                                  <cci:BalloonPopupExtender ID="BalloonAddCostInfo" TargetControlID="lbtnAddCostInfo" UseShadow="true"
                                      DisplayOnMouseOver="true" Position="BottomRight" BalloonPopupControlID="pnlAddCostInfo" BalloonStyle="Rectangle" runat="server" />
                                  <asp:Panel ID="pnlAddCostInfo" runat="server" style="font-size:small">
                                      A Supplier Adjustment will be generated in Sage Accounting for each additional cost line.
                                      <p>It is advisable to capture estimated costs to a temp/holding account. <br />On receipt of actual supporting documentation from suppliers, <br /> process the supplier invoice(s) and then Journal Credit <br />the temp/holding account to ensure accounts balance</p>
                                  </asp:Panel>
                              </asp:Panel>
                           </asp:Panel>
                          
                         <asp:Panel ID="PnlTotals" runat="server" style="float:right; border:1px gray solid; margin-top:1em; margin-right:1em;">
                             <table>
                                  <tr>
                                     <td colspan="2"><hr /></td>
                                 </tr>
                                 <tr>
                                     <td>Total Exclusive</td>
                                       <td><asp:Label ID="lblSubTotal" runat="server" Text="" Width="150px" style="text-align:right"></asp:Label></td>
                                 </tr>
                                 <tr>
                                     <td colspan="2"><hr /></td>
                                 </tr>
                                 <tr>
                                     <td>Total Vat</td>
                                       <td><asp:Label ID="lblTotVat" runat="server" Text="" Width="150px" style="text-align:right"></asp:Label></td>
                                 </tr>
                                  <tr>
                                     <td colspan="2"><hr /></td>
                                 </tr>
                                 <tr>
                                     <td>Total</td>
                                       <td><asp:Label ID="lblTotal" runat="server" Text="" Width="150px" style="text-align:right"></asp:Label></td>
                                 </tr>
                                  <tr>
                                     <td colspan="2"><hr /></td>
                                 </tr>
                             </table>
                         </asp:Panel>

                          <asp:Panel ID="PnlServices" runat="server" style="display:none" >
                          <h4 >Service Items Detected</h4>
                        <span>Would you like to combine the total value of the invoice, including service item costs, and distribute the costs across the inventory items based on their average price?</span> 
                        <asp:RadioButtonList ID="RBAllocateCosts" runat="server" RepeatDirection="Horizontal" style="margin:auto;">
                            <asp:ListItem Selected="True" Value="1">Yes, combine and allocate costs</asp:ListItem>
                            <asp:ListItem Value="2">No, keep costs separate</asp:ListItem>
                         </asp:RadioButtonList>    
                          </asp:Panel>
                             </div>
                     <div class="1u 12u$(medium)">&nbsp;</div>
                       </div>
               <div class="row 150%"> 
                    <div class="1u 12u$(medium)">&nbsp;</div>
                    <div class="8u 12u$(medium)" style="text-align:center">
                        <asp:LinkButton ID="lbtnReset" runat="server" style="color:red" CssClass="icon fa-ban buttonTransparent" OnClick="lbtnReset_Click" >&nbsp;Reset Receiving</asp:LinkButton></td>
                         <cci:ConfirmButtonExtender ID="lbtnReset_ConfirmButtonExtender1" runat="server" ConfirmText="You are about to reset receiving of this PO? Are you sure?" Enabled="True" TargetControlID="lbtnReset"></cci:ConfirmButtonExtender>          
                        <asp:LinkButton ID="lbtnPrintRN" runat="server" class="buttonRed icon fa-print" OnClick="lbtnPrintRN_Click">&nbsp;Print Preview</asp:LinkButton>
                        <asp:LinkButton ID="lbtnSaveClose" runat="server" style="font-size:1em" CssClass="icon fa-save buttonRed" OnClick="lbtnSaveClose_Click" ToolTip="Save PO status"
                            OnClientClick="var y=document.getElementById('RBpoStatus_0'); if (y && y.checked) return confirm('Complete without additional receiving? The outstanding balance will NOT be received.'); return true;">&nbsp;Save</asp:LinkButton>
                        <asp:LinkButton ID="lbtnReceiveFinish" runat="server" style="font-size:1em" CssClass="icon fa-save buttonSage" OnClick="lbtnReceiveFinish_Click"
                            ToolTip="Receive all marked items and generate a Good Received Note" 
                            OnClientClick="disableReceiveButtons('<%= lbtnReset.ClientID %>', '<%= lbtnPrintRN.ClientID %>', '<%= lbtnReceiveFinish.ClientID %>', 'Receive & Generate GRN); return true;" >
                            &nbsp;Receive & Generate GRN</asp:LinkButton>
                        <cci:ConfirmButtonExtender ID="lbtnRecAll_ConfirmButtonExtender" runat="server" ConfirmText="Confirm - Receive all items as shown?" Enabled="True" TargetControlID="lbtnReceiveFinish"></cci:ConfirmButtonExtender>
                          
                        <div style="float:right">
                        <strong>Receiving complete?</strong>
                         <asp:RadioButtonList ID="RBpoStatus" runat="server" RepeatDirection="Horizontal" AutoPostBack="true" OnSelectedIndexChanged="RBpoStatus_SelectedIndexChanged">
                             <asp:ListItem Value="0">Yes</asp:ListItem>
                             <asp:ListItem Value="1" Selected="True">No</asp:ListItem>
                        </asp:RadioButtonList>
                            </div>
                         <div style="font-size:.8em; text-align:center; color:orange">Receiving a PO will generate an "Unpaid" Supplier invoice in Sage. Verification and payment processing is to be completed using Sage.</div>
                    </div>
                    <div class="2u 12u$(medium)" style="text-align:left"><br />

                       </div>
                    <div class="1u 12u$(medium)">&nbsp;</div>
                </div>
            <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="lbtnRecAll"></cci:ModalPopupExtender>
            <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                        <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                        <div class="HellowWorldPopup">
                            <div id="Div4" class="PopupHeader">
                                <h2>Receive All Items</h2>
                            </div>
                            <div class="PopupBody">
                                Into which store?<br />
                            <asp:DropDownList ID="DDStore" runat="server" style="text-align:center"></asp:DropDownList>
                            </div>
                            <div class="Controls">
                                <input id="btnCancel5" type="button" class="fa fa-times-circle" value="" runat="server" style="display:none"/>
                                <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                <asp:LinkButton ID="btnApprovYes" runat="server" CssClass="buttonSage" OnClick="lbtnRecAll_Click" >OK</asp:LinkButton>
                            </div>
                        </div>
                    </asp:Panel>
                
           <cci:ModalPopupExtender ID="ModalPopupExtender2" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="lbtnACClose" Drag="true" OkControlID="btnAdCSave" PopupControlID="PnlAddCost" PopupDragHandleControlID="PopupHeader" TargetControlID="LbtnAddCosts"></cci:ModalPopupExtender>
           <asp:Panel ID="PnlAddCost" runat="server" Style="display: none">
                        <asp:LinkButton ID="lbtnACClose" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                        <div class="HellowWorldPopup">
                            <div id="Div45" class="PopupHeader">
                                <h4>Estimated Additional Costs.</h4>
                            </div>
                            <div class="PopupBody" style="margin-top:1em">
                               <table style="text-align:left; width:400px; margin:auto">
                                   <tr>
                                       <td> Supplier:</td>
                                       <td><asp:DropDownList ID="DDSupplier" runat="server" style="width:12em"> </asp:DropDownList></td>
                                   </tr>
                                   <tr>
                                       <td>GL Account:</td>
                                       <td><asp:DropDownList ID="DDAcctList" runat="server" style="width:12em"></asp:DropDownList></td>
                                   </tr>
                                   <tr>
                                       <td colspan="2">Additional Costs Reason</td>
                                   </tr>
                                 <tr>
                                    <td colspan="2"><asp:TextBox ID="txtAddCostsReason" runat="server" TextMode="MultiLine" Rows="3" Columns="45" style="text-align:left;" MaxLength="100"></asp:TextBox></td>
                                </tr>
                                   <tr>
                                       <td>Total (Ex Vat) Value</td>
                                       <td><asp:TextBox ID="txtAddCosts" runat="server" style="text-align:center; width:12em"></asp:TextBox><cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender1" runat="server" TargetControlID="txtAddCosts" FilterType="Custom, Numbers" ValidChars="." /> </td>
                                   </tr>
                                   <tr>
                                       <td>Vat Type: </td>
                                       <td>  <asp:DropDownList ID="DDVat" runat="server" style="width:12em"></asp:DropDownList></td>
                                   </tr>
                               </table>
                            </div>
                            <div class="Controls">
                                <input id="btnAdCSave" type="button" value="OK" runat="server" style="display:none"/>
                                <asp:LinkButton ID="lbtnAdCSave" runat="server" CssClass="buttonSage" OnClick="lbtnAdCSave_Click" >OK</asp:LinkButton>
                            </div>
                        </div>
                    </asp:Panel>
                    <asp:UpdateProgress ID="UpdateProgress2" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
                        <ProgressTemplate>
                            <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.7;">
                                <div style="position: relative; top: 40%; background: white; padding: 20px; border-radius: 10px; display: inline-block;">
                                    <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Processing ..." />
                                    <br />
                                    <strong>Processing Transfers - Please Wait</strong>
                                    <br />
                                    <span style="font-size: 12px; color: #666;">Do not close or refresh the page</span>
                                </div>
                            </div>
                        </ProgressTemplate>
                    </asp:UpdateProgress>
            <asp:LinkButton ID="LinkButton2" runat="server" style="display:none">LinkButton</asp:LinkButton>
           <cci:ModalPopupExtender ID="ModalPopupExtender1" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="lbtnCancelP" Drag="true" OkControlID="lbtnReceiveA" PopupControlID="PnlReceive" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton2"></cci:ModalPopupExtender>                           
            <asp:Panel ID="PnlReceive" runat="server" Style="display: none" DefaultButton="lbtnReceive">        
                                    <div class="HellowWorldPopup rec-modal" style="text-align:center">
                                         <asp:LinkButton ID="lbtnCancelP" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" OnClick="lbtnCancelP_Click" > </asp:LinkButton>
                                        <div id="Div5" class="PopupHeader">
                                            <h3>Update Line Item Receiving</h3>
                                        </div>
                                        <div class="PopupBody">
                                            <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                                              <ContentTemplate>       
                                            <table class="rec-frame">
                                                <tr>
                                                    <td style="vertical-align:top"><asp:Label ID="lblItemdescr" runat="server" Text=""></asp:Label>
                                                        <asp:Label ID="lblLineID" runat="server" Text="" style="display:none"></asp:Label>
                                                    </td>
                                                    <td style="text-align:left"><asp:CheckBox ID="chkEdit" runat="server" Text="Editing" Enabled="false" Visible="false" /><br /><asp:CheckBox ID="chkAddLotNum" runat="server" Text="Add Lot Number" Enabled="false" Visible="false" /></td>
                                                </tr>
                                                 <tr><td colspan="2"><hr /></td></tr>
                                                <tr>
                                                     <td class="rec-lbl">Ordered Qty</td>
                                                    <td><asp:textbox id="txtordqty" runat="server" readonly="true" Enabled="false" style="width:6em; text-align:center" ClientIDMode="Static" Text="" onblur="startCalc()" TabIndex="99"></asp:textbox></td>  <%--onblur="startCalc()"--%>   
                                                 </tr> 
                                                <tr>
                                                    <td class="rec-lbl">Received Qty</td>
                                                    <%-- Variation sits ON this row: it is calculated from the two quantities
                                                         above it, cannot be typed into, and does not deserve a row of its own. --%>
                                                    <td><asp:textbox id="txtQtyReceive" runat="server" style="width:6em; text-align:center" Text="" onblur="startCalc()" ClientIDMode="Static"  TabIndex="0"></asp:textbox><%--onblur="startCalc()"--%>
                                                            <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtQtyReceive" FilterType="Custom, Numbers" ValidChars="." />
                                                            <span style="margin-left:1em; color:#7a8b98">Variation</span>
                                                            <asp:textbox id="txtBalQty" runat="server" style="width:5em; text-align:center; color:red; border:none; background:transparent; font-weight:600" Text="" ClientIDMode="Static" ReadOnly="true" TabIndex="-1"></asp:textbox></td>
                                                </tr>
                                                 <tr>
                                                    <td class="rec-lbl">Into Store</td>
                                                    <td><asp:DropDownList ID="DDStoreEdit" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDStoreEdit_SelectedIndexChanged" style="width:10em; text-align:center" TabIndex="1"></asp:DropDownList></td>
                                                </tr>
                                               </table> 
                                                  <asp:Panel ID="PnlLotTracking" runat="server" CssClass="rec-panel">
                                                <table class="rec-frame">
                                                    <tr>
                                                        <td class="rec-lbl"><asp:Label ID="lblLotNumH" runat="server" Text="Lot Number"></asp:Label></td>
                                                        <td class="rec-val"><asp:TextBox ID="lblLotNum" runat="server" CssClass="rec-input" OnTextChanged="lblLotNum_TextChanged" MaxLength="50" onkeyup="countCharacters(this)" onblur="convertToUpper(this)"></asp:TextBox>
                                                            <cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender3" runat="server" TargetControlID="lblLotNum" FilterType="Numbers, UppercaseLetters, LowercaseLetters, Custom" ValidChars=".-/\:*" />
                                                            <asp:HiddenField ID="hfOriginalLotNumber" runat="server" />
                                                            <div class="rec-hint"><asp:Label ID="lblCharacterCount" runat="server" Text="Remaining: 50"></asp:Label> of 50 &mdash; letters, numbers and - / \ : * only</div>
                                                        </td>
                                                    </tr>
                                               </table>
                                                <%-- Serial capture. Only shown for serial-tracked items; the lot box above
                                                     then holds the supplier BATCH that these serials belong to. --%>
                                                <%-- Use By is LOT driven: one date, every serial in the lot takes it. --%>
                                                <asp:Panel ID="PnlUseBy" runat="server" CssClass="rec-panel" style="display:none">
                                                    <table class="rec-frame">
                                                        <tr>
                                                            <td class="rec-lbl"><asp:Label ID="Label4" runat="server" Text="Use By Date"></asp:Label></td>
                                                            <td class="rec-val">
                                                                <asp:TextBox ID="txtUseBy" runat="server" ClientIDMode="Static" CssClass="rec-input"></asp:TextBox>
                                                                <cci:CalendarExtender ID="CalendarExtender2" runat="server" Enabled="True" TargetControlID="txtUseBy" Format="dd MMM yyyy"></cci:CalendarExtender>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </asp:Panel>
                                                <%-- Two columns. LEFT: the count and the captured list. RIGHT: the ways to
                                                     capture - scan, or import a file. --%>
                                                <asp:Panel ID="PnlSerialCapture" runat="server" CssClass="rec-panel" style="display:none">
                                                    <table class="rec-frame">
                                                        <tr>
                                                            <td class="rec-lbl"><span class="rec-panel-head">Serial Numbers</span></td>
                                                            <td class="rec-val">
                                                                <asp:TextBox ID="txtSerialScan" runat="server" ClientIDMode="Static" CssClass="rec-input"
                                                                     placeholder="Scan/Type Serial : Enter" MaxLength="50"
                                                                     autocomplete="off" autocorrect="off" autocapitalize="off"></asp:TextBox>
                                                                <asp:HiddenField ID="hfSerials" runat="server" ClientIDMode="Static" />
                                                            </td>
                                                        </tr>
                                                        <tr>
                                                            <td class="rec-lbl">
                                                                <span id="serialCount" class="serial-count">0 captured</span>
                                                                &nbsp;<button type="button" id="btnSerialClear" class="button2" style="font-size:.75em">Clear</button>
                                                                <ul id="serialList" class="serial-list"></ul>
                                                            </td>
                                                            <td class="rec-val">
                                                                <div class="rec-import">
                                                                    <div class="rec-hint"  style="padding-bottom:2em">Or import .csv / .xlsx &mdash; columns <b>Lot Number</b>, <b>Serial</b>. One lot per file.</div>
                                                                    <asp:FileUpload ID="fuSerials" runat="server" />
                                                                    <asp:LinkButton ID="lbtnImportSerials" runat="server" CssClass="buttonSage icon fa-upload"
                                                                        OnClick="lbtnImportSerials_Click"> Import</asp:LinkButton>
                                                                </div>
                                                                <div class="rec-hint" style="margin-top:.5em">One serial per unit. The line cannot be saved until the count matches the quantity received.</div>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </asp:Panel>
                                                        <asp:Panel ID="PnlLotAdditions" runat="server" CssClass="rec-panel">
                                                            <table style="width:100%">
                                                                <tr>
                                                                    <td class="rec-field" style="text-align:center">
                                                                        <asp:Label ID="lblLotAddHead" runat="server" Text="Lot Note (optional)" CssClass="rec-panel-head"></asp:Label>
                                                                        <asp:Label ID="Label2" runat="server" Text="" Visible="false"></asp:Label>
                                                                        <asp:TextBox ID="txtLotNote" runat="server" CssClass="rec-input" MaxLength="250"
                                                                            placeholder="Max 250 characters"></asp:TextBox>
                                                                        <asp:HiddenField ID="HiddenField1" runat="server" />
                                                                    </td>
                                                                </tr>
                                                                <tr>
                                                                    <td style="padding:0"><asp:TextBox ID="txtNumPieces" runat="server" style="width:10em; text-align:center; display:none"></asp:TextBox>
                                                                        <cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender2" runat="server" TargetControlID="txtNumPieces" FilterType="Custom, Numbers" ValidChars="." />
                                                                    </td>
                                                                </tr>
                                                            </table>
                                                        </asp:Panel>
                                                      </asp:Panel>
                                              <%--<table style="width:100%">
                                                <tr>
                                                    <td colspan="3" style="text-align:center;"><span id="errpop" runat="server" style="color:red" ></span></td>
                                                </tr>
                                                   <tr>
                                                     <td colspan="3" style="text-align:center;"><asp:CheckBox ID="chkAccept" runat="server" Text="I accept the variation, continue"  /></td>
                                                 </tr>
                                            </table>--%>
                                                  </ContentTemplate>
                                            </asp:UpdatePanel>
                                        </div>
                                        <div class="Controls">
                                            <input id="lbtnCancelA" type="button" class="button2" value="CANCEL" runat="server" style="display:none" />
                                            <input id="lbtnReceiveA" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                            <asp:LinkButton ID="lbtnReceive" runat="server" CssClass="icon fa-upload buttonSage" OnClick="lbtnReceive_Click" > Save Line</asp:LinkButton>                                            
                                        </div>
                                    </div>      
                         </asp:Panel>
                </div>
                    <script type="text/javascript">
                        function countCharacters(textbox) {
                            var maxLength = 50;
                            if (textbox.value.length > maxLength) {
                                textbox.value = textbox.value.substring(0, maxLength);
                            }
                            var length = textbox.value.length;
                            var remaining = maxLength - length;
                            var label = document.getElementById('<%= lblCharacterCount.ClientID %>');
                            label.innerHTML = "Remaining: " + remaining;
                        }
                    </script>
                     </ContentTemplate>
                <Triggers>
                    <%-- Re-opening a completed PO redirects, so RBpoStatus must do a FULL
                         postback - a Response.Redirect is swallowed by a partial postback. --%>
                    <asp:PostBackTrigger ControlID="RBpoStatus" />
                    <%-- Print does CreatePDF then Response.Redirect to the viewer; a full postback
                         keeps the redirect from being swallowed by the partial postback. --%>
                    <asp:PostBackTrigger ControlID="lbtnPrintRN" />
                    <%-- A FileUpload posts nothing on an async postback, so the import must be a
                         full postback. The handler re-shows the modal afterwards. --%>
                    <asp:PostBackTrigger ControlID="lbtnImportSerials" />
                </Triggers>
            </asp:UpdatePanel>

            <asp:LinkButton ID="LinkButton1" runat="server" Style="display: none">LinkButton</asp:LinkButton>
            <cci:ModalPopupExtender ID="ModalPopupExtender3" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="lbtnAttCancel" Drag="true" OkControlID="lbtnAppYes" PopupControlID="PnlUpload" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton1"></cci:ModalPopupExtender>
            <asp:Panel ID="PnlUpload" runat="server" Style="display: none">
                <asp:LinkButton ID="lbtnAttCancel" runat="server" CssClass="fa fa-times" Style="float: right" ToolTip="Cancel"> </asp:LinkButton>
                <div class="HellowWorldPopup">
                    <div id="Div55" class="PopupHeader">
                        <h2>Attachments</h2>
                    </div>
                    <div class="PopupBody" style="min-height: 300px; padding: 2em">
                        <asp:GridView ID="GridAtts" runat="server" AutoGenerateColumns="false" CssClass="gridview">
                            <HeaderStyle CssClass="gridViewHeader" />
                            <RowStyle CssClass="gridViewRow" />
                            <AlternatingRowStyle CssClass="gridViewAltRow" />
                            <FooterStyle CssClass="gridViewHeader" />
                            <PagerStyle CssClass="gridViewPager" />
                            <Columns>
                                <asp:BoundField HeaderText="Name" DataField="AttName" ReadOnly="True" />
                                <asp:TemplateField ItemStyle-HorizontalAlign="Right">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lbtnDownload" CommandArgument='<%# Eval("AttGUID") %>' CommandName='<%# Eval("AttName") %>' runat="server" CssClass="buttonC fa fa-download" ToolTip="Download Document" OnClick="lbtnDownload_Click"> </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                        <asp:Label ID="lblMessage" runat="server" ForeColor="Red" />
                    </div>
                    <div class="Controls">
                        <span>Choose file to upload</span><br />
                        <asp:LinkButton ID="lbtnUpload" runat="server" CssClass="icon fa-upload buttonSage" OnClick="lbtnUpload_Click" >Upload</asp:LinkButton>
                        <asp:FileUpload ID="fuAttachment" runat="server" CssClass="fileUploadControl" ToolTip="Choose file to upload"/>
                        <asp:LinkButton ID="lbtnAppYes" runat="server" CssClass="buttonRed">Close</asp:LinkButton>
                    </div>
                </div>
            </asp:Panel>

            </div>
    </form>
        <script type="text/javascript">
            function startCalc() {
                var Oqty = document.getElementById("txtordqty").value;
                var OrdQty = parseFloat(Oqty.replace(/[\s,]/g, ""));  // remove spaces and commas

                var RQty = document.getElementById("txtQtyReceive").value;
                var RecQty = parseFloat(RQty.replace(/[\s,]/g, ""));  // remove spaces and commas

                // Blank boxes parse to NaN, and "Variation NaN" was showing on screen the
                // moment the quantity was cleared. Treat missing as zero.
                if (isNaN(OrdQty)) OrdQty = 0;
                if (isNaN(RecQty)) RecQty = 0;

                var TotS = OrdQty - RecQty;
                document.getElementById("txtBalQty").value = TotS;

                document.getElementById("txtNumPieces").value = RecQty;

                var maxqty = OrdQty * 1.05;
                var minqty = OrdQty * 0.95;

                //if (RecQty >= minqty && RecQty <= maxqty) {
                //    document.getElementById("errpop").innerHTML = "";
                //    //document.getElementById("lbtnReceive").style.display = "inline-block";
                //    document.getElementById("chkAccept").style.display = "none";
                //} else {
                //    document.getElementById("errpop").innerHTML = "Warning - Quantity variation of 5% or more being received";
                //    document.getElementById("chkAccept").style.display = "inline-block";
                //    //document.getElementById("lbtnReceive").style.display = "none"; // Hide receive button in this case
                //}
            }
        </script>
        <%--<script type="text/javascript">
        function countCharacters(textbox) {
            var maxLength = 50;
            if (textbox.value.length > maxLength) {
                textbox.value = textbox.value.substring(0, maxLength);
            }
            var length = textbox.value.length;
            var remaining = maxLength - length;
            var label = document.getElementById('<%= lblCharacterCount.ClientID %>');
            label.innerHTML = "(Characters remaining: " + remaining + ")";
        }
    </script>--%>
    <script>
        function convertToUpper(textBox) {
            textBox.value = textBox.value.toUpperCase();
        }
    </script>
    <style>
        /* The receive-line modal only. .HellowWorldPopup is shared by every popup in the
           app - widening or resizing it there would move all of them - so the width and
           font size live here, on a class only this modal carries.
           Was font-size:.8em inline (about 11px); .95em is roughly two points larger. */
        .rec-modal { width:640px; max-width:95vw; margin:auto; font-size:.95em; }

        /* The body scrolls, the modal does not grow past the window. Without this the
           panels simply stacked and a serial line ran off the bottom of the screen. */
        .rec-modal .PopupBody { max-height:74vh; overflow-y:auto; overflow-x:hidden; padding-right:.35em; }

        /* The shared .Controls style is padding:2em + margin:2em - about 130px of empty
           space above Save Line on its own. Scoped down here, not changed globally. */
        .rec-modal .Controls { padding:.5em; margin:.4em 0 0; min-height:0; }

        /* The header block and the quantity table are tighter too - default cell padding
           on six rows is most of the space above the first panel. */
        .rec-modal .PopupHeader h3 { margin:.2em 0; }
        .rec-modal table td { padding:.1em .3em; }

        /* Panels inside the receive-line modal - one look for both. */
        .rec-panel      { margin:0 0 .45em; padding:.5em .8em; background:#f4f7f9;
                          border:1px solid #cfd8df; border-radius:.4em; }
        .rec-panel-head { font-weight:700; color:#4282C1; margin-bottom:.25em; }
        .rec-field      { padding:.15em 0; }
        /* Label left, input right on one line - stacked they cost twice the height for
           the same information. The label column is fixed so the three panels line up. */
        .rec-lbl        { width:11em; min-width:11em; text-align:left; font-weight:600; padding-right:.6em; white-space:nowrap; vertical-align:top; }
        .rec-val        { text-align:left; vertical-align:top; }
        /* Every table in the modal shares the same frame, so the input column starts at the
           same x in all of them. */
        .rec-frame      { margin:auto; text-align:left; width:400px; border-collapse:collapse; }
        .rec-frame td   { padding:.15em .3em; }
        /* The plain quantity boxes at the top are unstyled inputs; give them the same look. */
        .rec-frame input[type=text], .rec-frame select { height:1.9em; }

        /* The panels supply the padding; inputs simply fill the space inside them, so every
           box in the modal is the same width whatever container it happens to sit in. */
        .rec-input      { width:14em; box-sizing:border-box; text-align:left; padding-left:.4em; }
        /* Import block - a plain stacked column. It was a flex row with flex:1/min-width:0 on
           the file input, which inside a narrow cell shrank it to nothing: "the upload has gone". */
        .rec-import          { border-top:1px solid #dfe6eb; margin-top:.5em; padding-top:.45em; }
        .rec-import input[type=file] { display:block; width:100%; max-width:100%; box-sizing:border-box; font-size:.85em; }
        .rec-import .buttonSage      { display:block; width:fit-content; margin-left:auto; margin-top:.35em; padding:.3em .7em; color:#fff; white-space:nowrap; }
        .rec-hint       { font-size:.8em; color:#7a8b98; margin-top:.1em; line-height:1.25; }
        /* Same width as the inputs above it, so the panel reads as one column. */
        .serial-list { list-style:none; width:100%; max-width:100%; margin:.3em 0 0; padding:0;
                       max-height:7.5em; overflow-y:auto;
                       text-align:left; border:1px solid #cfd8df; border-radius:.3em; background:#fff; }
        .serial-row  { display:flex; align-items:center; gap:.4em; padding:.25em .5em;
                       border-bottom:1px solid #eef2f5; font-size:.9em; }
        .serial-row:last-child { border-bottom:none; }
        .serial-idx  { min-width:2em; color:#8a99a6; text-align:right; }
        .serial-val  { flex:1; font-weight:600; }
        .serial-exp  { color:#5a6b78; white-space:nowrap; text-align:right; font-size:.9em; }
        .serial-del  { border:none; background:none; color:#c0392b; font-size:1.1em;
                       cursor:pointer; line-height:1; }
        .serial-count      { font-size:.85em; font-weight:600; color:#5a6b78; }
        .serial-count.ok   { color:#1e8449; }
        .serial-count.warn { color:#c0392b; }
    </style>
    <script src="<%= ResolveUrl("~/scripts/serialCapture.js") %><%= SBMS.Classes.Ver.Js("~/scripts/serialCapture.js") %>"></script>
    
</body>
</html>
