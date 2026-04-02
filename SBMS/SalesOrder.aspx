<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="SalesOrder.aspx.cs" Inherits="SBMS.SalesOrder" ClientIDMode="Static" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Sales Order</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
          <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    </head>
<body>
    <form id="form1" runat="server">
         <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="content">
            <div id="divOverlay" style="display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background-color: #000; opacity: 0.5; z-index: 9999999; text-align: center;">
                <img src="images/tenorwait.gif"
                    style="margin-top: 15%; border-radius: 1.5em;"
                    alt="Loading..." />
            </div>
           <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel2">
            <ProgressTemplate>
                <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
               <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." style="padding: 10px; padding-top:15%; border-radius:1.5em" />
                </div>
              </ProgressTemplate>
        </asp:UpdateProgress> 
            <asp:UpdatePanel ID="UpdatePanel2" runat="server">
     <ContentTemplate> 
                <div class="container">          
                    <div class="row 150%">
                    <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>        
                    <div class="8u 12u$(medium)">
                        <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="lbtnSOs" runat="server" class="buttonC icon fa-arrow-left" PostBackUrl="~/OSSalesOrders.aspx">&nbsp;Open Sales Orders</asp:LinkButton>
                        <asp:DropDownList ID="DDOptions" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDOptions_SelectedIndexChanged" CssClass="buttonC" Height="2.35em">
                            <asp:ListItem>- Create -</asp:ListItem>
                             <asp:ListItem value = "1">Create Picking Slip</asp:ListItem>
                            <asp:ListItem Value="0">Create Job Card</asp:ListItem>
                            <asp:ListItem Value="2">Create Works Order</asp:ListItem>
                        </asp:DropDownList>
                            <asp:LinkButton ID="lbtnReload" runat="server" ToolTip="Reload Sales Order From Sage" CssClass="icon fa-refresh" OnClick="lbtnReload_Click" style="float:right; font-size:0.8em" >&nbsp;</asp:LinkButton>
                                <cci:ConfirmButtonExtender ID="ConfirmButtonExtender1R" runat="server" ConfirmText="Confirm, reload this Sales Order?" Enabled="True" TargetControlID="lbtnReload"></cci:ConfirmButtonExtender>
                        <h2 style="padding-top:0; line-height:1em">Sales Order</h2>                       
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
  
                <div class="row 150%">
                    <div class="1u 12u$(medium)">&nbsp;</div>
                    <div class="10u 12u$(medium)">
                        <table style="width:100%">  
                            <tr>
                                <td colspan="6">
                                     <asp:LinkButton ID="lbtnDelSO" CssClass="icon fa-ban buttonTransparent" runat="server" ForeColor="Red" ToolTip="Delete Sales Order" OnClick="lbtnDelSO_Click" Style="margin-right: 2em; float:right"> Delete</asp:LinkButton>
                                    <cci:ConfirmButtonExtender ID="ConfirmButtonExtender2" runat="server" ConfirmText="WARNING - Delete this Sales Order from Data Fusion? Are you sure?" Enabled="True" TargetControlID="lbtnDelSO"></cci:ConfirmButtonExtender>
                                    <h6>Sales Order Details: <asp:Label ID="lblDocNum" runat="server" Text=""></asp:Label>&nbsp;<asp:Label ID="lblSOStatus" runat="server" Text=""></asp:Label></h6>
                                    <asp:Label ID="lblDocID" runat="server" Text="" style="display:none"></asp:Label>
                                </td>
                            </tr>
                            <tr>
                                <td style="width:10em">Customer </td>
                                <td><asp:TextBox ID="txtCustName" runat="server" style="width:95%" ReadOnly="true"></asp:TextBox><asp:Label ID="lblCustID" runat="server" Text="" style="display:none"></asp:Label></td>
                                <td style="width:10em">Address</td>
                                <td><asp:TextBox ID="txtAddress1" runat="server" style="width:95%" ReadOnly="true"></asp:TextBox></td>
                                <td>Picking Slip</td>
                                <td><asp:TextBox ID="txtPSNum" runat="server" ReadOnly="true" ></asp:TextBox><asp:LinkButton ID="lbtnViewPS" runat="server" CssClass="buttonRed icon fa-search" ToolTip="View Picking Slip" OnClick="lbtnViewPS_Click"></asp:LinkButton></td>
                            </tr>
                            <tr>
                                <td>Due Date</td>
                                <td style="padding-top: 2px"><asp:TextBox ID="txtPODate" runat="server" ReadOnly="true"></asp:TextBox>&nbsp;<span style="float:right; padding-right:5%"> Doc Date<asp:TextBox ID="txtCaptDate" runat="server" ReadOnly="true"></asp:TextBox></span></td>
                                 <td></td>
                                <td><asp:TextBox ID="txtAddress2" runat="server" style="width:95%" ReadOnly="true"></asp:TextBox></td>
                                <td>Job Card </td>
                                <td><asp:TextBox ID="txtJCNum" runat="server" ReadOnly="true"></asp:TextBox><asp:LinkButton ID="lbtnViewJC" runat="server" CssClass="buttonRed icon fa-search" ToolTip="View Job Card" OnClick="lbtnJCNew_Click"></asp:LinkButton></td>
                           </tr>
                            <tr >
                                <td>Reference</td>
                                <td style="padding-top: 2px"><asp:TextBox ID="txtRef" runat="server" style="width:95%" ReadOnly="true"></asp:TextBox><asp:Label ID="lblRepID" runat="server" Text="" style="display:none"></asp:Label></td>
                                <td></td>
                                <td><asp:TextBox ID="txtAddress3" runat="server" style="width:95%" ReadOnly="true"></asp:TextBox></td>
                                <td>Issued To</td>
                                <td><asp:TextBox ID="txtIssuedTo" runat="server" ReadOnly="true"></asp:TextBox></td>
                           </tr>
                            <tr>
                                <td>Sales Rep</td>
                                <td style="padding-top: 2px"><asp:TextBox ID="txtRep" runat="server" style="width:95%" ReadOnly="true"></asp:TextBox></td>
                                <td></td>
                                <td style="padding-top: 2px"><asp:TextBox ID="txtAddress4" runat="server" style="width:95%" ReadOnly="true"></asp:TextBox></td>
                                <td>Status</td>
                                <td style="padding-top: 2px"><asp:Label ID="lblStatus" runat="server" Text="" ReadOnly="true"></asp:Label></td>
                           </tr>
                            </table> 
                        <hr />
                        </div>
                    <div class="1u 12u$(medium)">&nbsp;</div>
                   </div>
                 <div class="row 150%">
                     <div class="1u 12u$(medium)">&nbsp;</div>
                     <div class="10u 12u$(medium)">
                          <asp:GridView ID="GridPOLines" runat="server" AutoGenerateColumns="false" CssClass="gridview" RowStyle-Wrap="true"  ShowFooter="true" OnRowDataBound="GridPOLines_RowDataBound" >
                                    <HeaderStyle CssClass="gridViewHeader" />
                                            <RowStyle CssClass="gridViewRow" />
                                            <AlternatingRowStyle CssClass="gridViewAltRow" />
                                            <FooterStyle CssClass="gridViewHeader" />
                                            <PagerStyle CssClass="gridViewPager" />
                                            <Columns>
                                                <asp:BoundField DataField="LineID" ReadOnly="True" ItemStyle-Width="0em" ItemStyle-Font-Size="0.01em" ItemStyle-ForeColor="Transparent" />
                                                <asp:BoundField HeaderText="ItemCode" DataField="ItemCode" ReadOnly="True" ItemStyle-Width="8em"  />
                                                <asp:BoundField HeaderText="Description" DataField="ItemDescription" ReadOnly="True"  />
                                                <asp:BoundField HeaderText="Unit" DataField="Unit" ReadOnly="True" ItemStyle-Width="3em"  />
                                                <asp:BoundField HeaderText="Order Qty" DataField="Quantity" ReadOnly="True" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center"  ItemStyle-Width="6em" />    
                                                <asp:BoundField HeaderText="Pick Qty" DataField="ReceiveQty" ReadOnly="True" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center"  ItemStyle-Width="6em" />    
                                                <asp:BoundField HeaderText="Lot No" DataField="LotNumber" ReadOnly="True" ItemStyle-Width="8em"  />
                                                <asp:BoundField HeaderText="Excl_Price" DataField="UnitPriceExclusive" ReadOnly="True" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" ItemStyle-Width="8em" />
                                                <asp:BoundField HeaderText="Disc%" DataField="DiscountPercentage" ReadOnly="True" DataFormatString="{0:p}" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="5em"   />
                                                <asp:BoundField HeaderText="Discount" DataField="Discount" ReadOnly="True" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" ItemStyle-Width="5em"  />
                                                <asp:BoundField HeaderText="SO Total" DataField="Total" ReadOnly="True" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" ItemStyle-Width="10em" />        
                                            </Columns>
                           </asp:GridView>
                         <asp:Panel ID="PnlMsg" runat="server" Style="max-width: 48%; vertical-align: top; float: left">
                             <h5>Message</h5>
                             <asp:TextBox ID="txtMsg" runat="server" ReadOnly="true" Rows="5" Columns="50" TextMode="MultiLine"></asp:TextBox>
                         </asp:Panel>

                         <asp:Panel ID="PnlTotals" runat="server" style="float:right; border:1px gray solid; margin-top:1em; max-width:48%;margin-right:1em; font-size:0.8em">
                             <table>
                                  <tr>
                                     <td colspan="4"><hr /></td>
                                 </tr>
                                 <tr>
                                     <td>Total Exclusive</td>
                                       <td><asp:Label ID="lblSubTotal" runat="server" Text="" Width="150px" style="text-align:right"></asp:Label></td>
                                 </tr>
                                 <tr>
                                     <td colspan="4"><hr /></td>
                                 </tr>
                                 <tr>
                                      <td>Total Vat</td>
                                       <td><asp:Label ID="lblTotVat" runat="server" Text="" Width="150px" style="text-align:right"></asp:Label></td>
                                 </tr>
                                  <tr>
                                     <td colspan="4"><hr /></td>
                                 </tr>
                                 <tr>
                                     <td>Total</td>
                                       <td><asp:Label ID="lblTotal" runat="server" Text="" Width="150px" style="text-align:right"></asp:Label></td>
                                 </tr>
                                  <tr>
                                     <td colspan="2"><hr /></td>
                                 </tr>
                                 <tr>
                                    <td style="text-align:left">Cost: <asp:Label ID="lblDocCost" runat="server" Text=""></asp:Label></td>
                                      <td style="text-align:right">GP: <asp:Label ID="lblDocGP" runat="server" Text=""></asp:Label></td>
                                </tr>
                             </table>
                         </asp:Panel>
                         </div>
                     <div class="1u 12u$(medium)">&nbsp;</div>
                     
                     <div class="12u 12u$(medium)" style="text-align: center">
                         <asp:Panel ID="PnlButtons" runat="server" Style="display: none">
                             <span id="lblspan" runat="server" tooltip="Select NO for the stock entries of 0.00 lines to be made by item adjustments in SAGE, and not reflect on the Sales Order or Tax Invoice" >'No Charge'  (0.00) lines detected. Add them to the Sage Sales Order?
                                 <asp:DropDownList ID="DDNCSelect" runat="server" Width="60px" >
                                 <asp:ListItem Value="1">Yes</asp:ListItem>
                                 <asp:ListItem Value="2">No</asp:ListItem>
                           </asp:DropDownList><br />
                                 <span style="font-size:x-small">(Selecting NO :-  The item entries of 0.00 lines to be made by item adjustments in SAGE, and not reflect on the Sales Order or Tax Invoice)</span>
                             </span><br />
                             <asp:Label ID="lblErr" runat="server" Text="" style="margin:auto; color:red"></asp:Label><br />
                             <asp:LinkButton ID="lbtnPrintDN" runat="server" class="buttonRed icon fa-print" Width="200px" OnClick="lbtnPrintDN_Click" Style="display: none">&nbsp;Delivery Note</asp:LinkButton>
                             <asp:LinkButton ID="lbtnPost" runat="server" class="buttonIndex icon fa-upload" Width="220px" OnClick="lbtnPost_Click" ToolTip="Updates Sales Order in Sage Accounting with picking details, Store, Lot number etc. Tax invoice to be generated In Sage Accounting.">&nbsp;Update Sage SO</asp:LinkButton>
                             <cci:ConfirmButtonExtender ID="lbtnpost_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Update this Sales Order in Sage?" Enabled="True" TargetControlID="lbtnPost"></cci:ConfirmButtonExtender>
                             <asp:LinkButton ID="lbtnTaxInv" runat="server" class="buttonIndex icon fa-upload" Width="220px" OnClick="lbtnTaxInv_Click" ToolTip="Generate Tax invoice In Sage Accounting.">&nbsp;Create Sage Tax Invoice</asp:LinkButton>
                             <cci:ConfirmButtonExtender ID="ConfirmButtonExtender3" runat="server" ConfirmText="Confirm, Create a Tax Invoice in Sage?" Enabled="True" TargetControlID="lbtnTaxInv"></cci:ConfirmButtonExtender>
                             <asp:LinkButton ID="lbtnUndo" runat="server" class="buttonRed icon fa-undo" Width="200px" OnClick="lbtnUndo_Click">&nbsp;Re-open for editing</asp:LinkButton>
                             <cci:ConfirmButtonExtender ID="lbtnUndo_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Re-open this Sales Order for editing?" Enabled="True" TargetControlID="lbtnUndo"></cci:ConfirmButtonExtender>
                         </asp:Panel>
                     </div>
                </div>      
                <asp:LinkButton ID="LinkButton2" runat="server"></asp:LinkButton>      
                 <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                        <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                        <div class="HellowWorldPopup">
                            <div id="Div4" class="PopupHeader">
                                <h4>Confirm, Open new <asp:Label ID="lblTpe" runat="server" Text=""></asp:Label></h4>
                                <asp:Label ID="lblSender" runat="server" Text="Label" style="display:none"></asp:Label><br />
                                From Store:<br /><asp:DropDownList ID="DDStoreH" runat="server"></asp:DropDownList>
                            </div>
                            <div class="PopupBody">
                             <div id="pnlJCref" runat="server">
                                <h5>Enter Internal Job Card Reference</h5>
                                <asp:TextBox ID="txtMsgBody" runat="server" style="text-align:center" ></asp:TextBox><br /><br />
                                 <asp:LinkButton ID="lbtnAutoCreate" runat="server" OnClick="lbtnAutoCreate_Click" CssClass="fa fa-plus-circle buttonRed">Auto-create a Number</asp:LinkButton>
                                 </div>
                            </div>
                            <div class="Controls">
                                <input id="btnCancel5" type="button" class="fa fa-times-circle" value="" runat="server" style="display:none"/>
                                <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                <asp:LinkButton ID="btnSaveConfirm" runat="server" CssClass="buttonSage icon fa-thumbs-up" OnClick="btnSaveConfirm_Click">Yes, Do it.</asp:LinkButton><br />
                            </div>
                        </div>
                    </asp:Panel> 
          <cci:ModalPopupExtender ID="Button2551_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton2"></cci:ModalPopupExtender>
                </div>
                    </ContentTemplate>
            </asp:UpdatePanel>
                    </div>
    </form>
    <script type="text/javascript">
    function showOverlayAndPostBack() {
        document.getElementById("divOverlay").style.display = "block";
        __doPostBack("GenerateInvoice", "");
    }
    </script>
</body>
</html>
