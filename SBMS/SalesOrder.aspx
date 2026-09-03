<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="SalesOrder.aspx.cs" Inherits="SBMS.SalesOrder" ClientIDMode="Static" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Sales Order</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <%-- bump ?v= whenever df-theme.css / df-ui.js change, so browsers don't serve a stale copy --%>
    <link rel="stylesheet" href="assets/css/df-theme.css?v=13" />
          <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    <script src="assets/js/df-ui.js?v=13"></script>
    </head>
<body class="df-page df-mod-sales">
    <form id="form1" runat="server">
         <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>

            <div id="divOverlay" class="df-loading-overlay" style="display: none;">
                <div class="df-loading-box">
                    <img src="images/tenorwait.gif" class="df-loading-spinner" alt="Loading..." />
                </div>
            </div>
           <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel2">
            <ProgressTemplate>
                <div class="df-loading-overlay">
                    <div class="df-loading-box">
                        <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." CssClass="df-loading-spinner" />
                    </div>
                </div>
              </ProgressTemplate>
        </asp:UpdateProgress>

        <asp:UpdatePanel ID="UpdatePanel2" runat="server">
     <ContentTemplate>

        <div class="df-shell" id="dfShell">
            <div class="df-main">
                <header class="df-topbar">
                    <a href="https://mydatafusion.online" title="My Data Fusion website" target="_blank"><img src="images/logo.png" class="df-brand-logo" alt="Data Fusion" /></a>
                    <asp:LinkButton ID="lbtnHome" runat="server" CssClass="df-icon-btn" onclick="lbtnHome_Click" ToolTip="Dashboard" aria-label="Dashboard"><i class="icon fa-home"></i></asp:LinkButton>
                    <asp:LinkButton ID="lbtnSOs" runat="server" CssClass="df-btn" PostBackUrl="~/OSSalesOrders.aspx"><i class="icon fa-arrow-left"></i>&nbsp;Open Sales Orders</asp:LinkButton>
                    <div class="df-topbar-spacer"></div>
                    <button type="button" class="df-icon-btn" id="dfThemeToggle" title="Toggle dark mode" aria-label="Toggle dark mode"><i class="icon fa-moon-o" id="dfThemeIcon"></i></button>
                    <asp:Image ID="imgCoImg" runat="server" CssClass="df-co-logo" />
                </header>

                <main class="df-content">
                    <div class="df-page-head">
                        <h2 class="df-page-title"><i class="icon fa-shopping-cart df-page-icon"></i>Sales Order
                            <asp:Label ID="lblDocNum" runat="server" Text="" CssClass="df-count"></asp:Label>&nbsp;<asp:Label ID="lblSOStatus" runat="server" Text="" CssClass="df-count"></asp:Label></h2>
                        <asp:LinkButton ID="lbtnViewJC" runat="server" CssClass="df-btn" ToolTip="View Job Card" OnClick="lbtnJCNew_Click"><i class="icon fa-search-plus"></i>&nbsp;Job Card</asp:LinkButton>
                        <asp:LinkButton ID="lbtnViewPS" runat="server" CssClass="df-btn" ToolTip="View Picking Slip" OnClick="lbtnViewPS_Click"><i class="icon fa-search-plus"></i>&nbsp;Picking Slip</asp:LinkButton>
                        <asp:DropDownList ID="DDOptions" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDOptions_SelectedIndexChanged" CssClass="df-select df-select-sm">
                            <asp:ListItem>- Create -</asp:ListItem>
                             <asp:ListItem value = "1">Create Picking Slip</asp:ListItem>
                            <asp:ListItem Value="0">Create Job Card</asp:ListItem>
                            <asp:ListItem Value="2">Create Works Order</asp:ListItem>
                        </asp:DropDownList>
                        <asp:LinkButton ID="lbtnReload" runat="server" ToolTip="Reload Sales Order From Sage" CssClass="df-icon-btn" OnClick="lbtnReload_Click" aria-label="Reload from Sage"><i class="icon fa-refresh"></i></asp:LinkButton>
                        <cci:ConfirmButtonExtender ID="ConfirmButtonExtender1R" runat="server" ConfirmText="Confirm, reload this Sales Order?" Enabled="True" TargetControlID="lbtnReload"></cci:ConfirmButtonExtender>
                        <asp:LinkButton ID="lbtnDelSO" CssClass="df-btn df-btn-danger" runat="server" ToolTip="Delete Sales Order" OnClick="lbtnDelSO_Click"><i class="icon fa-ban"></i>&nbsp;Delete</asp:LinkButton>
                        <cci:ConfirmButtonExtender ID="ConfirmButtonExtender2" runat="server" ConfirmText="WARNING - Delete this Sales Order from Data Fusion? Are you sure?" Enabled="True" TargetControlID="lbtnDelSO"></cci:ConfirmButtonExtender>
                    </div>

                    <div class="df-card">
                        <asp:Label ID="lblDocID" runat="server" Text="" style="display:none"></asp:Label>
                        <table class="df-detail-table">
                            <tr>
                                <td style="width:10em">Customer </td>
                                <td><asp:TextBox ID="txtCustName" runat="server" ReadOnly="true"></asp:TextBox><asp:Label ID="lblCustID" runat="server" Text="" style="display:none"></asp:Label></td>
                                <td style="width:10em">Address</td>
                                <td><asp:TextBox ID="txtAddress1" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td style="width:8em">Picking Slip </td>
                                <td><asp:TextBox ID="txtPSNum" runat="server" ReadOnly="true" ></asp:TextBox></td>
                            </tr>
                            <tr>
                                <td>Due Date</td>
                                <td><asp:TextBox ID="txtPODate" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td>Doc Date</td>
                                <td><asp:TextBox ID="txtCaptDate" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td>Job Card</td>
                                <td><asp:TextBox ID="txtJCNum" runat="server" ReadOnly="true"></asp:TextBox></td>
                           </tr>
                            <tr >
                                <td>Reference</td>
                                <td><asp:TextBox ID="txtRef" runat="server" ReadOnly="true"></asp:TextBox><asp:Label ID="lblRepID" runat="server" Text="" style="display:none"></asp:Label></td>
                                <td>Address</td>
                                <td><asp:TextBox ID="txtAddress2" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td>Issued To</td>
                                <td><asp:TextBox ID="txtIssuedTo" runat="server" ReadOnly="true"></asp:TextBox></td>
                           </tr>
                            <tr>
                                <td>Sales Rep</td>
                                <td><asp:TextBox ID="txtRep" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td>Address</td>
                                <td><asp:TextBox ID="txtAddress3" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td>Status</td>
                                <td><asp:Label ID="lblStatus" runat="server" Text="" ReadOnly="true"></asp:Label></td>
                           </tr>
                            <tr>
                                <td></td>
                                <td></td>
                                <td>Address</td>
                                <td><asp:TextBox ID="txtAddress4" runat="server" ReadOnly="true"></asp:TextBox></td>
                                <td></td>
                                <td></td>
                           </tr>
                            </table>
                    </div>

                    <div class="df-card df-table-card">
                        <div class="df-table-wrap">
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
                        </div>
                    </div>

                    <div class="df-split">
                        <asp:Panel ID="PnlMsg" runat="server" CssClass="df-card">
                            <h3 class="df-card-title">Message</h3>
                            <asp:TextBox ID="txtMsg" runat="server" ReadOnly="true" Rows="5" TextMode="MultiLine" CssClass="df-input-full" Style="height:auto"></asp:TextBox>
                        </asp:Panel>

                        <asp:Panel ID="PnlTotals" runat="server" CssClass="df-card">
                            <h3 class="df-card-title">Totals</h3>
                            <table class="df-totals-table">
                                <tr>
                                    <td>Total Exclusive</td>
                                    <td><asp:Label ID="lblSubTotal" runat="server" Text=""></asp:Label></td>
                                </tr>
                                <tr>
                                    <td>Total Vat</td>
                                    <td><asp:Label ID="lblTotVat" runat="server" Text=""></asp:Label></td>
                                </tr>
                                <tr class="df-totals-row-strong">
                                    <td>Total</td>
                                    <td><asp:Label ID="lblTotal" runat="server" Text=""></asp:Label></td>
                                </tr>
                                <tr class="df-totals-row-strong">
                                    <td>Cost: <asp:Label ID="lblDocCost" runat="server" Text=""></asp:Label></td>
                                    <td>GP: <asp:Label ID="lblDocGP" runat="server" Text=""></asp:Label></td>
                                </tr>
                            </table>
                        </asp:Panel>
                    </div>

                    <asp:Panel ID="PnlButtons" runat="server" CssClass="df-card" Style="display: none">
                        <span id="lblspan" runat="server" visible="false"> Send No Charge Lines To Sage?<br /><asp:DropDownList ID="DDNCSelect" runat="server" CssClass="df-select df-select-sm">
                            <asp:ListItem Value="0">-Select-</asp:ListItem>
                            <asp:ListItem Value="1">Yes</asp:ListItem>
                            <asp:ListItem Value="2">No</asp:ListItem>
                      </asp:DropDownList><br /><br /></span>
                        <asp:Label ID="lblErr" runat="server" Text="" CssClass="df-auth-msg" style="color:red"></asp:Label>
                        <div class="df-toolbar">
                            <asp:LinkButton ID="lbtnPrintDN" runat="server" CssClass="df-btn" OnClick="lbtnPrintDN_Click" Style="display: none"><i class="icon fa-print"></i>&nbsp;Delivery Note</asp:LinkButton>
                            <asp:LinkButton ID="lbtnPost" runat="server" CssClass="df-btn df-btn-primary" OnClick="lbtnPost_Click" ToolTip="Updates Sales Order in Sage Accounting with picking details, Store, Lot number etc. Tax invoice to be generated In Sage Accounting."><i class="icon fa-upload"></i>&nbsp;Update Sage SO</asp:LinkButton>
                            <cci:ConfirmButtonExtender ID="lbtnpost_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Update this Sales Order in Sage?" Enabled="True" TargetControlID="lbtnPost"></cci:ConfirmButtonExtender>
                            <asp:LinkButton ID="lbtnTaxInv" runat="server" CssClass="df-btn df-btn-primary" OnClick="lbtnTaxInv_Click" ToolTip="Generate Tax invoice In Sage Accounting."><i class="icon fa-upload"></i>&nbsp;Create Sage Tax Invoice</asp:LinkButton>
                            <cci:ConfirmButtonExtender ID="ConfirmButtonExtender3" runat="server" ConfirmText="Confirm, Create a Tax Invoice in Sage?" Enabled="True" TargetControlID="lbtnTaxInv"></cci:ConfirmButtonExtender>
                            <asp:LinkButton ID="lbtnUndo" runat="server" CssClass="df-btn df-btn-danger" OnClick="lbtnUndo_Click"><i class="icon fa-undo"></i>&nbsp;Re-open for editing</asp:LinkButton>
                            <cci:ConfirmButtonExtender ID="lbtnUndo_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Re-open this Sales Order for editing?" Enabled="True" TargetControlID="lbtnUndo"></cci:ConfirmButtonExtender>
                        </div>
                    </asp:Panel>

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

                </main>
            </div>
        </div>

                    </ContentTemplate>
            </asp:UpdatePanel>
    </form>
    <script type="text/javascript">
    function showOverlayAndPostBack() {
        document.getElementById("divOverlay").style.display = "block";
        __doPostBack("GenerateInvoice", "");
    }
    </script>
</body>
</html>
