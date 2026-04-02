<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="BOMCreate.aspx.cs" Inherits="SBMS.BOMCreate" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Bill Of Materials</title>
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
                        <asp:LinkButton ID="LinkButton1" runat="server" class="buttonC icon fa-angle-double-left" PostBackUrl="~/BOMHeaders.aspx">&nbsp;BOMs</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <h3 style="padding-top:0; line-height:1em">Bill Of Materials Master</h3>                    
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /> </div>
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
                                           <td rowspan="2"><asp:LinkButton ID="lbtnDeleteBom" runat="server" style="float:left; color:red; font-size:.8em" CssClass="icon fa-ban button" OnClick="lbtnDeleteBom_Click1" > Delete BOM</asp:LinkButton>
                                               <cci:ConfirmButtonExtender ID="lbtnDeleteBom_ConfirmButtonExtender1" runat="server" ConfirmText="Delete this BOM? Are you sure?" Enabled="True" TargetControlID="lbtnDeleteBom"></cci:ConfirmButtonExtender>
                                           </td>
                                            <td>BOM Code:</td>
                                            <td><asp:TextBox ID="txtBomCode" runat="server" Width="80px" MaxLength="20"></asp:TextBox></td>       
                                            <td> <asp:TextBox ID="txtBomDescript" runat="server" Style="width: 100%" MaxLength="50"></asp:TextBox></td> 
                                            <td rowspan="2" style="padding:2em; vertical-align:top"><asp:LinkButton ID="LbtnSaveBOM" runat="server" style="float:right; font-size:1em;" CssClass="icon fa-save buttonIndex" OnClick="LbtnSaveBOM_Click"> SAVE BOM</asp:LinkButton>
                                           </td>
                                        </tr>
                                        <tr>
                                            <td>Finished Good Code:</td>
                                            <td><asp:DropDownList ID="DDFGCode" runat="server" Width="80px" AutoPostBack="true" OnSelectedIndexChanged="DDFGCode_SelectedIndexChanged"></asp:DropDownList></td>           
                                            <td><asp:Label ID="lblFGDescript" runat="server" Text="" ></asp:Label></td>           
                                        </tr>
                                    </table>  
                                    <hr />
                                </asp:Panel>
                                <h3>BOM Lines</h3>
                                <asp:GridView ID="GridBOMLines" runat="server" AutoGenerateColumns="false" CssClass="gridview"  ToolTip="Open BOM" OnRowDataBound="GridBOMLines_RowDataBound" ShowFooter="true" >
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <Columns>
                                        <asp:BoundField DataField="BLID"  />
                                         <asp:TemplateField HeaderText="Line Item Code" ItemStyle-Width="10em" >
                                            <ItemTemplate >
                                                <asp:DropDownList ID="DDItemCode" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDItemCode_SelectedIndexChanged" Width="100px">
                                                          <asp:ListItem Value="0">-Item Code-</asp:ListItem>
                                                          </asp:DropDownList>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Item Description" DataField="Description" ReadOnly="True"  />
                                        <asp:TemplateField HeaderText="RM Qty" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" FooterStyle-HorizontalAlign="Center">
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtBOMQty" runat="server" Width="6em" style="text-align:center" Text='<%# Eval("RMQty") %>'  onkeydown='<%# "triggerSaveOnEnter(event, \"" + ((GridViewRow)Container).FindControl("lbtnLineSave").ClientID + "\")" %>'></asp:TextBox>
                                                <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtBOMQty" FilterType="Custom, Numbers" ValidChars="." />
                                            </ItemTemplate>
                                            </asp:TemplateField>
                                        <asp:BoundField HeaderText="UOM" DataField="BomUnit" ReadOnly="True" ItemStyle-Width="2em" ItemStyle-HorizontalAlign="Center"  />
                                        <asp:BoundField HeaderText="Av Unit Cost" DataField="AvCost" ReadOnly="True" DataFormatString="{0:N4}" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" FooterStyle-HorizontalAlign="Right" ItemStyle-Width="6em" />
                                        <asp:BoundField HeaderText="Av Cost" DataField="AvRMCost" ReadOnly="True" DataFormatString="{0:N4}" ItemStyle-HorizontalAlign="Right" FooterStyle-HorizontalAlign="Right" ItemStyle-Width="6em" />
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnLineSave" CommandArgument='<%# Eval("BLID") %>' CommandName="lbtnLineSave" runat="server" CssClass="fa fa-save buttonRed" ToolTip="Save BOM Line" OnClick="lbtnLineSave_Click"> </asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnDeleteLine" CommandArgument='<%# Eval("BLID") %>' CommandName="lbtnDeleteLine" runat="server" CssClass="fa fa-ban" ToolTip="Delete Line" OnClick="lbtnDeleteLine_Click" style="color:red"> </asp:LinkButton>
                                                        <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Delete Job Card Line?" Enabled="True" TargetControlID="lbtnDeleteLine"></cci:ConfirmButtonExtender>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                                <asp:Panel ID="Panel2" runat="server" style="float:right; margin-right:5em">
                                    <table>
                                        <tr>
                                            <td>Additional Costs 1&nbsp;</td>
                                            <td><asp:TextBox ID="txtAdd1" runat="server" Width="100px" style="text-align:right" OnTextChanged="txtAdd1_TextChanged" AutoPostBack="true"></asp:TextBox>
                                                <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtAdd1" FilterType="Custom, Numbers" ValidChars="." />
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>Additional Costs 2&nbsp;</td>
                                            <td><asp:TextBox ID="txtAdd2" runat="server" Width="100px" style="text-align:right" OnTextChanged="txtAdd1_TextChanged" AutoPostBack="true"></asp:TextBox></td>
                                            <cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender1" runat="server" TargetControlID="txtAdd2" FilterType="Custom, Numbers" ValidChars="." />
                                        </tr>
                                        <tr>
                                            <td>Additional Costs 3&nbsp;</td>
                                            <td><asp:TextBox ID="txtAdd3" runat="server" Width="100px" style="text-align:right" OnTextChanged="txtAdd1_TextChanged" AutoPostBack="true"></asp:TextBox></td>
                                            <cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender2" runat="server" TargetControlID="txtAdd3" FilterType="Custom, Numbers" ValidChars="." />
                                        </tr>
                                    <tr>
                                        <td>Additional Costs</td>
                                        <td><asp:TextBox ID="txtTotCost" runat="server" Width="100px" ReadOnly="true" style="text-align:right"></asp:TextBox></td>
                                    </tr>      
                                    </table>
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
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</body>
</html>
