<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="WorksOrdersManfDetailed.aspx.cs" Inherits="SBMS.WorksOrdersManfDetailed" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Manufacture Details</title>
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
                        <asp:LinkButton ID="LinkButton2" runat="server" class="buttonC icon fa-edit" PostBackUrl="~/WorksOrdersHeaders.aspx" ToolTip="View all open works orders">&nbsp;Works Orders</asp:LinkButton>
                        <asp:LinkButton ID="lbtnMRPThis" runat="server" class="buttonC icon fa-book" ToolTip="View Materials Requirements for this Works Order." OnClick="lbtnMRPThis_Click">&nbsp;RMD: This Works Order</asp:LinkButton>
                        <asp:LinkButton ID="lbtnMRP" runat="server" class="buttonC icon fa-align-justify" PostBackUrl="~/FGDemands.aspx" >&nbsp;MRP: Raw Materials</asp:LinkButton>
                         <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br /><br />
                        <h3 id="woheader" runat="server" style="padding-top:0; line-height:1em"></h3>                    
                    </div>
                    <div class="2u 12u$(medium)"><img src="images/SageAcctLogo.jpg" style="float:right" class="logoImg"  />   </div>
                    </div>
                <div class="row 150%">
                      <div class="1u 12u$(medium)">&nbsp;</div>    
                    <div class="10u 12u$(medium)">
                        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                            <ContentTemplate>
                                <div style="display:none">
                                <asp:Label ID="lblDir" runat="server" Text=""></asp:Label>
                                </div> 
                                <asp:Panel ID="Panel1" runat="server">
                                    <table style="width: 100%">
                                        <tr>
                                            <td>Customer:</td>
                                            <td>
                                                <asp:TextBox ID="lblFCCustName" runat="server" Text=""></asp:TextBox></td>           
                                            <td>Created Date:</td>
                                            <td><asp:Label ID="lblcreatedDate" runat="server" Text=""></asp:Label></td> 
                                            <td>Due Date</td>
                                            <td><asp:TextBox ID="txtDueDate" runat="server" Text=""></asp:TextBox></td>
                                        </tr>
                                        <cci:CalendarExtender ID="CalendarExtender1" runat="server" Enabled="True" TargetControlID="txtDueDate" Format="dd MMM yyyy"></cci:CalendarExtender>
                                        <tr>
                                            <td>Reference:</td>
                                            <td><asp:TextBox ID="lblFCRef" runat="server" Text=""></asp:TextBox></td>    
                                            <td>Created By:</td>
                                            <td><asp:TextBox ID="lblCreatedBy" runat="server" Text=""></asp:TextBox></td>       
                                            <td>Status</td>
                                            <td><asp:DropDownList ID="DDStatus" runat="server" style="width:100%">
                                                </asp:DropDownList></td>
                                        </tr>
                                    <tr>
                                    <td>Linked Document #:</td>
                                    <td><asp:TextBox ID="txtLinkedDoc" runat="server" Text=""></asp:TextBox></td>    
                                    <td>Notes:</td>
                                    <td colspan="3"><asp:TextBox ID="txtwomsg" runat="server" Text="" style="width:100%"></asp:TextBox></td>           
                                </tr>
                                    
                                    </table>  
                                    <hr />
                                </asp:Panel>
                                <h3>Works Order Lines</h3>
                                <asp:GridView ID="GridWOLines" runat="server" AutoGenerateColumns="false" CssClass="gridview"  ToolTip="Open BOM" OnRowDataBound="GridWOLines_RowDataBound" ShowFooter="true" >
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <Columns>
                                        <asp:BoundField DataField="LineID"  />
                                         <asp:TemplateField HeaderText="Item/Kit/BOM" ItemStyle-Width="10em" >
                                                <ItemTemplate >
                                                    <asp:DropDownList ID="DDBOMKIT" runat="server" Width="100px" Enabled="false">
                                                              <asp:ListItem Value="">- Select -</asp:ListItem>
                                                              <asp:ListItem Value="1">Item</asp:ListItem>
                                                              <asp:ListItem Value="2">From BOM</asp:ListItem>
                                                              <asp:ListItem Value="3">From KIT</asp:ListItem>
                                                              </asp:DropDownList>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Item Code" ItemStyle-Width="10em" >
                                            <ItemTemplate >
                                                <asp:DropDownList ID="DDItemCode" runat="server" AutoPostBack="true"  Width="100px" Enabled="false">
                                                          <asp:ListItem Value="0">-Item Code-</asp:ListItem>
                                                          </asp:DropDownList>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Description" ItemStyle-Width="100%">
                                                    <ItemTemplate>
                                                         <asp:TextBox ID="txtDescription" runat="server" style="width:100%"  Text='<%# Eval("ItemDescription") %>' Enabled="false" ></asp:TextBox><br />
                                                        <asp:Label ID="lblLineComment" runat="server" Text='<%# Eval("Comments") %>' style="font-size:.8em"></asp:Label>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Qty" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" FooterStyle-HorizontalAlign="Center">
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtQty" runat="server" Width="6em" style="text-align:center" Text='<%# Eval("Quantity") %>'  Enabled="false"></asp:TextBox>
                                            </ItemTemplate>
                                            </asp:TemplateField>
                                         
                                            <asp:TemplateField HeaderText="BOM" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnShowBOM" CommandArgument='<%# Eval("LineID") %>' CommandName="lbtnShowBOM" runat="server"  CssClass="fa fa-book buttonRed" ToolTip="Show Raw Materials Requirements" OnClick="lbtnShowBOM_Click" style="color:#4282C1" > </asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnComment" CommandArgument='<%# Eval("LineID") %>' CommandName="lbtnComment" runat="server" CssClass="fa fa-edit buttonRed" ToolTip="Add a line comment" OnClick="lbtnComment_Click" style="color:#4282C1"> </asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                         <asp:TemplateField HeaderText="Lot Number" ItemStyle-Width="8em">
                                             <ItemTemplate>
                                                  <asp:TextBox ID="txtLotNum" runat="server" style="width:8em"  Text='<%# Eval("LotNumber") %>' ></asp:TextBox> 
                                             </ItemTemplate>
                                         </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Complete">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkComplete" runat="server" Enabled="false" Text=" " />
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                                <div style="text-align:center">
                                    <asp:LinkButton ID="lbtnWOPrint" runat="server" class="buttonRed icon fa-print" OnClick="lbtnWOPrint_Click" style="margin-right:2em">&nbsp;Print Preview</asp:LinkButton>
                                    <asp:LinkButton ID="LbtnSaveWO" runat="server" style="font-size:1em; margin-top:2em; margin-right:2em;" CssClass="icon fa-save buttonSage" OnClick="LbtnSaveWO_Click"> Save</asp:LinkButton>
                                    <asp:LinkButton ID="lbtnAutoManf" runat="server" style="font-size:1em; margin-top:2em;" CssClass="icon fa-circle-notch buttonRed" OnClick="lbtnAutoManf_Click"> Manufacture/Convert</asp:LinkButton>
                                </div>

                                 <asp:LinkButton ID="LinkButton3" runat="server" style="display:none">LinkButton</asp:LinkButton>
                                <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton3"></cci:ModalPopupExtender>
                                    <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                                                <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                                                <div class="HellowWorldPopup">
                                                    <div id="Div4" class="PopupHeader">
                                                        <h2>Add Comment</h2><asp:Label ID="lblSender" runat="server" Text="Label" style="display:none"></asp:Label><asp:Label ID="lblLineid" runat="server" Text="Label" style="display:none"></asp:Label>
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

                                <asp:LinkButton ID="LinkButton4" runat="server" style="display:none">LinkButton</asp:LinkButton>
                                <cci:ModalPopupExtender ID="ModalPopupExtender1" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="BomClose" Drag="true" OkControlID="BomOK" PopupControlID="PNLBom" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton4"></cci:ModalPopupExtender>
                                    <asp:Panel ID="PNLBom" runat="server" Style="display: none">
                                                <asp:LinkButton ID="BomClose" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                                                <div class="HellowWorldPopup">
                                                    <div id="Div44" class="PopupHeader">
                                                        <h2>Raw Materials Requirements</h2>
                                                    </div>
                                                    <div class="PopupBody" style="margin:2em">
                                                        <h4><asp:Label ID="lblItem" runat="server" Text="Label"></asp:Label>
                                                      &nbsp; &nbsp;<asp:Label ID="lblQty" runat="server" Text="Label"></asp:Label> </h4>
                                                        <asp:GridView ID="GridUseBom" runat="server" AutoGenerateColumns="false" CssClass="gridview" >
                                                            <HeaderStyle CssClass="gridViewHeader" />
                                                            <RowStyle CssClass="gridViewRow" />
                                                            <AlternatingRowStyle CssClass="gridViewAltRow" />
                                                                <Columns>
                                                                    <asp:BoundField HeaderText="Item" DataField="ItemCode"  />
                                                                    <asp:BoundField HeaderText="Description" DataField="ItemDescription"  />
                                                                    <asp:TemplateField HeaderText="Qty" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" FooterStyle-HorizontalAlign="Center">
                                                                    <ItemTemplate>
                                                                        <asp:TextBox ID="txtMQty" runat="server" Width="6em" style="text-align:center" Text='<%# Eval("Quantity") %>'  onkeydown='<%# "triggerSaveOnEnter(event, \"" + ((GridViewRow)Container).FindControl("lbtnLineMSave").ClientID + "\")" %>'></asp:TextBox>
                                                                        <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtMQty" FilterType="Custom, Numbers" ValidChars="." />
                                                                    </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                                                        <ItemTemplate>
                                                                             <asp:LinkButton ID="lbtnLineMSave" CommandArgument='<%# Eval("LineID") %>' CommandName="lbtnLineSave" runat="server" CssClass="fa fa-save buttonRed" ToolTip="Save Line" OnClick="lbtnMLineSave_Click"> </asp:LinkButton>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                 </Columns>
                                                            </asp:GridView>
                                                           </div> 
                                                    <div class="Controls">
                                                        <input id="Button1" type="button" class="fa fa-times-circle" value="" runat="server" style="display:none"/>
                                                        <input id="BomOK" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                                        <asp:LinkButton ID="lBtnSave" runat="server" CssClass="buttonCancel" >OK</asp:LinkButton>
                                                    </div>
                                                </div>
                                            </asp:Panel>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                         </ div> 
                    <div class="1u 12u$(medium)">&nbsp;</div>    
                    </div>
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
</body>
</html>
