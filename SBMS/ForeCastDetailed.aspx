<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ForeCastDetailed.aspx.cs" Inherits="SBMS.ForeCastDetailed" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Forecast Details</title>
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
                    <%--    <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click" >&nbsp;&nbsp;</asp:LinkButton>--%>
                        <asp:LinkButton ID="LinkButton1" runat="server" class="buttonC icon fa-arrow-left" PostBackUrl="~/ForeCastHeaders.aspx" >&nbsp;Forecasts</asp:LinkButton>
                        <%--<asp:LinkButton ID="LinkButton2" runat="server" class="buttonTransparent icon fa-edit" PostBackUrl="~/WorksOrdersHeaders.aspx" ToolTip="View all open works orders">&nbsp;Works Orders</asp:LinkButton>--%>
                        <%--<asp:LinkButton ID="lbtnMRP" runat="server" class="buttonTransparent icon fa-align-justify" PostBackUrl="~/FGDemands.aspx" >&nbsp;MRP: Finished Goods</asp:LinkButton>
                         <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />--%>
                        <h3 style="padding-top:0; line-height:1em">Forecast Details</h3>                    
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
                <div class="row 150%">
                      <div class="2u 12u$(medium)">&nbsp;</div>    
                    <div class="8u 12u$(medium)">
                        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                            <ContentTemplate>
                                <div style="display:none">
                                <asp:Label ID="lblDir" runat="server" Text=""></asp:Label>
                                </div> 
                                <asp:Panel ID="Panel1" runat="server">
                                    <table style="width: 100%">
                                        <tr>
                                           <td rowspan="2">
                                           </td>
                                            <td>Customer:</td>
                                            <td><asp:TextBox ID="lblFCCustName" runat="server" Text=""></asp:TextBox></td>           
                                            <td>Created Date:</td>
                                            <td><asp:Label ID="lblcreatedDate" runat="server" Text=""></asp:Label></td> 
                                            <td rowspan="2"><asp:LinkButton ID="LbtnSaveFC" runat="server" style="float:right; font-size:1em" CssClass="icon fa-save buttonC" OnClick="LbtnSaveFC_Click"> SAVE FORECAST</asp:LinkButton>
                                           </td>
                                        </tr>
                                        <tr>
                                            <td>Reference:</td>
                                            <td><asp:TextBox ID="lblFCRef" runat="server" Text=""></asp:TextBox></td>    
                                            <td>Created By:</td>
                                            <td><asp:TextBox ID="lblCreatedBy" runat="server" Text=""></asp:TextBox></td>           
                                        </tr>
                                    </table>  
                                    <hr />
                                </asp:Panel>
                                <h3>Forecast Lines</h3>
                                <asp:GridView ID="GridFCLines" runat="server" AutoGenerateColumns="false" CssClass="gridview"  ToolTip="Open BOM" OnRowDataBound="GridFCLines_RowDataBound" ShowFooter="true" >
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <Columns>
                                        <asp:BoundField DataField="LineID"  />
                                         <asp:TemplateField HeaderText="Item Code" ItemStyle-Width="10em" >
                                            <ItemTemplate >
                                                <asp:DropDownList ID="DDItemCode" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDItemCode_SelectedIndexChanged" Width="100px">
                                                          <asp:ListItem Value="0">-Item Code-</asp:ListItem>
                                                          </asp:DropDownList>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Description" ItemStyle-Width="100%">
                                                    <ItemTemplate>
                                                         <asp:TextBox ID="txtDescription" runat="server" style="width:100%"  Text='<%# Eval("ItemDescription") %>' ></asp:TextBox><br />
                                                        <asp:Label ID="lblLineComment" runat="server" Text='<%# Eval("Comments") %>' style="font-size:.8em"></asp:Label>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Qty" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" FooterStyle-HorizontalAlign="Center">
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtQty" runat="server" Width="6em" style="text-align:center" Text='<%# Eval("Quantity") %>'  onkeydown='<%# "triggerSaveOnEnter(event, \"" + ((GridViewRow)Container).FindControl("lbtnLineSave").ClientID + "\")" %>'></asp:TextBox>
                                                <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtQty" FilterType="Custom, Numbers" ValidChars="." />
                                            </ItemTemplate>
                                            </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Due Date" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="7em" >
                                                    <ItemTemplate>
                                                        <asp:TextBox ID="txtLineDate" runat="server" Text='<%# Eval("DueDelDate")%>' style="width:6.5em"></asp:TextBox>
                                                        <cci:CalendarExtender ID="CalendarExtender1" runat="server" Enabled="True" TargetControlID="txtLineDate" Format="dd MMM yyyy"></cci:CalendarExtender>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnLineSave" CommandArgument='<%# Eval("LineID") %>' CommandName="lbtnLineSave" runat="server" CssClass="fa fa-save buttonRed" ToolTip="Save Forecast Line" OnClick="lbtnLineSave_Click"> </asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnComment" CommandArgument='<%# Eval("LineID") %>' CommandName="lbtnComment" runat="server" CssClass="fa fa-edit buttonRed" ToolTip="Add a line comment" OnClick="lbtnComment_Click" style="color:#4282C1"> </asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnDeleteLine" CommandArgument='<%# Eval("LineID") %>' CommandName="lbtnDeleteLine" runat="server" CssClass="fa fa-ban" ToolTip="Delete Line" OnClick="lbtnDeleteLine_Click" style="color:red"> </asp:LinkButton>
                                                        <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Delete Forecast Line?" Enabled="True" TargetControlID="lbtnDeleteLine"></cci:ConfirmButtonExtender>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
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
                            </ContentTemplate>
                        </asp:UpdatePanel>
                         </ div> 
                    <div class="2u 12u$(medium)">&nbsp;</div>    
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
