<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="KitDetailed.aspx.cs" Inherits="SBMS.KitDetailed" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Kits Details</title>
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
                        <asp:LinkButton ID="LinkButton1" runat="server" class="buttonC icon fa-angle-double-left" PostBackUrl="~/KitHeaders.aspx">&nbsp;Kits</asp:LinkButton>
                        <asp:LinkButton ID="lbtnCopy" runat="server" class="buttonC icon fa-copy" OnClick="LbtnCopy_Click">&nbsp;Copy Kit</asp:LinkButton>
                        <asp:LinkButton ID="lbtnBack" runat="server" class="buttonC icon fa-align-left" PostBackUrl="~/ItemsHeaders.aspx" >&nbsp;Items List</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <h3 style="padding-top:0; line-height:1em">Kit Details</h3>                    
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
                                           <td style="width:8em"><asp:LinkButton ID="lbtnDeleteBom" runat="server" style="float:left; color:red; font-size:.8em" CssClass="icon fa-ban button" OnClick="lbtnDeleteBom_Click1" > Delete Kit</asp:LinkButton>
                                               <cci:ConfirmButtonExtender ID="lbtnDeleteBom_ConfirmButtonExtender1" runat="server" ConfirmText="Delete this Kit? Are you sure?" Enabled="True" TargetControlID="lbtnDeleteBom"></cci:ConfirmButtonExtender>
                                           </td>
                                           <td style="width:12em">Finished Good Code:</td>
                                            <td style="text-align:left"><asp:Label ID="lblFGCode" runat="server" Text=""></asp:Label><asp:Label ID="lblFGID" runat="server" Text="" style="display:none"></asp:Label></td>           
                                            <td style="text-align:left"><asp:Label ID="lblFGDescript" runat="server" Text=""></asp:Label></td>       
                                            <td ><asp:LinkButton ID="LbtnSaveBOM" runat="server" style="float:right; font-size:1em" CssClass="icon fa-save buttonRed" OnClick="LbtnSaveBOM_Click"> SAVE KIT</asp:LinkButton>
                                           </td>
                                        </tr>
                                    </table>  
                                    <hr />
                                </asp:Panel>
                                <h3>Kit Components</h3>
                                <asp:GridView ID="GridKitLines" runat="server" AutoGenerateColumns="false" CssClass="gridviewS"  ToolTip="Open Kit" OnRowDataBound="GridKitLines_RowDataBound" ShowFooter="true" >
                                    <HeaderStyle CssClass="gridViewHeaderS" />
                                    <FooterStyle CssClass="gridViewHeaderS" />
                                    <RowStyle CssClass="gridViewRowS" />
                                    <AlternatingRowStyle CssClass="gridViewAltRowS" />
                                    <FooterStyle CssClass="gridviewfooterS" />
                                    <Columns>
                                        <asp:BoundField DataField="KLID"  />
                                         <asp:TemplateField HeaderText="Line Item Code" ItemStyle-Width="10em" >
                                            <ItemTemplate >
                                                <asp:DropDownList ID="DDItemCode" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDItemCode_SelectedIndexChanged" Width="100px">
                                                          <asp:ListItem Value="0">-Item Code-</asp:ListItem>
                                                          </asp:DropDownList>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Item Description" DataField="Description" ReadOnly="True"  />
                                        <asp:TemplateField HeaderText="FG Qty" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" FooterStyle-HorizontalAlign="Center">
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtBOMQty" runat="server" Width="6em" style="text-align:center" Text='<%# Eval("FGQty") %>'  onkeydown='<%# "triggerSaveOnEnter(event, \"" + ((GridViewRow)Container).FindControl("lbtnLineSave").ClientID + "\")" %>'></asp:TextBox>
                                                <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtBOMQty" FilterType="Custom, Numbers" ValidChars="." />
                                            </ItemTemplate>
                                            </asp:TemplateField>
                                        <asp:BoundField HeaderText="Av Unit Cost" DataField="AvCost" ReadOnly="True" DataFormatString="{0:N4}" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" FooterStyle-HorizontalAlign="Right" ItemStyle-Width="10em" />
                                        <asp:BoundField HeaderText="Av Cost" DataField="AvRMCost" ReadOnly="True" DataFormatString="{0:N4}" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" FooterStyle-HorizontalAlign="Right" ItemStyle-Width="8em" />
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnLineSave" CommandArgument='<%# Eval("KLID") %>' CommandName="lbtnLineSave" runat="server" CssClass="fa fa-save buttonRed" ToolTip="Save Kit Line" OnClick="lbtnLineSave_Click"> </asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnDeleteLine" CommandArgument='<%# Eval("KLID") %>' CommandName="lbtnDeleteLine" runat="server" CssClass="fa fa-ban" ToolTip="Delete Line" OnClick="lbtnDeleteLine_Click" style="color:red"> </asp:LinkButton>
                                                        <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Delete Kit Line?" Enabled="True" TargetControlID="lbtnDeleteLine"></cci:ConfirmButtonExtender>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                                <asp:Panel ID="Panel3" runat="server" style="float:left; margin-top:2em">
                                    <table style="width:20em">
                                            <tr>
                                                <td>SBCA Item Cost Price: </td>
                                                <td  style="text-align:right"><asp:Label ID="lblItemCost" runat="server" Text=""></asp:Label></td> 
                                             </tr>
                                        <tr>
                                            <td>This Kit Total Cost Price: </td>
                                             <td style="text-align:right"><asp:Label ID="lblNewKitCost" runat="server" Text=""></asp:Label></td> 
                                         </tr>
                                        
                                        <tr>
                                            <td colspan="2"><br /> <asp:LinkButton ID="lbtnSBCAUpdate" CssClass="icon fa-upload buttonIndex" runat="server" ToolTip="Update Sage with this unit cost?" OnClick="lbtnSBCAUpdate_Click">Update SBCA Average Unit Cost</asp:LinkButton></td>
                                            <cci:ConfirmButtonExtender ID="lbtnSBCAUpdate_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Update Sage Accounting Average Cost?" Enabled="True" TargetControlID="lbtnSBCAUpdate"></cci:ConfirmButtonExtender>
                                        </tr>
                                        </table>
                                </asp:Panel>

                                <asp:Panel ID="Panel2" runat="server" style="float:right; margin-right:6.3em">
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
</body>
</html>
