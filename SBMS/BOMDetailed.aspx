<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="BOMDetailed.aspx.cs" Inherits="SBMS.BOMDetailed" %>
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
                        <asp:LinkButton ID="lbtnCopy" runat="server" class="buttonC icon fa-copy" OnClick="LbtnCopy_Click">&nbsp;Copy BOM</asp:LinkButton>
                        <asp:LinkButton ID="lbtnBack" runat="server" class="buttonC icon fa-align-left" PostBackUrl="~/ItemsHeaders.aspx" >&nbsp;Items</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="lbtnDownload" runat="server" class="buttonC icon fa-download" style="float:right" OnClick="lbtnDownload_Click" ToolTip="Download BOM to Excel">&nbsp;Excel</asp:LinkButton><br />
                        <h3 style="padding-top:0; line-height:1em">Bill Of Materials Master</h3>                    
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
                <div class="row 150%">
                      <div class="2u 12u$(medium)">&nbsp;</div>    
                    <div class="8u 12u$(medium)">
                             <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
                                    <ProgressTemplate>
                                        <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
                                           <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." style="padding: 10px; padding-top:15%; border-radius:1.5em" />
                                        </div>
                                      </ProgressTemplate>
                                </asp:UpdateProgress> 
                        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                            <ContentTemplate>
                                <div style="display:none">
                                <asp:Label ID="lblDir" runat="server" Text=""></asp:Label>
                                </div> 
                                <asp:Panel ID="Panel1" runat="server">
                                    <table style="width: 100%">
                                        <tr>
                                            <td style="width:12em">BOM Code:</td>
                                            <td style="width:6em"><asp:TextBox ID="lblBOMCode" runat="server" Text="" MaxLength="20"></asp:TextBox></td>           
                                            <td><asp:TextBox ID="lblBOMDescipt" runat="server" Text="" style="width:300px" MaxLength="50"></asp:TextBox></td> 
                                            <td style="text-align:left"><asp:CheckBox ID="chkActive" runat="server" Text="BOM is Active" /></td>
                                        </tr>
                                        <tr>
                                            <td>Finished Good Code:</td>
                                            <td><asp:Label ID="lblFGCode" runat="server" Text=""></asp:Label><asp:Label ID="lblFGID" runat="server" Text="" style="display:none"></asp:Label></td>           
                                            <td><asp:Label ID="lblFGDescript" runat="server" Text=""></asp:Label></td>           
                                        </tr>
                                    </table>  
                                    <hr />
                                </asp:Panel>
                                <h3>BOM Lines</h3>
                                <asp:GridView ID="GridBOMLines" runat="server" AutoGenerateColumns="false" CssClass="gridviewS"  OnRowDataBound="GridBOMLines_RowDataBound" ShowFooter="true" AllowSorting="true" OnSorting="GridBOMLines_Sorting"  >
                                    <HeaderStyle CssClass="gridViewHeaderS" />
                                    <FooterStyle CssClass="gridViewHeaderS" />
                                    <RowStyle CssClass="gridViewRowS" />
                                    <AlternatingRowStyle CssClass="gridViewAltRowS" />
                                    <FooterStyle CssClass="gridViewHeaderS" />
                                    <Columns>
                                        <asp:BoundField DataField="BLID"  />
                                         <asp:TemplateField HeaderText="Line Item Code" ItemStyle-Width="10em" SortExpression="ItemCode" >
                                            <ItemTemplate >
                                                <asp:DropDownList ID="DDItemCode" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDItemCode_SelectedIndexChanged" Width="100px">
                                                          <asp:ListItem Value="0">-Item Code-</asp:ListItem>
                                                          </asp:DropDownList>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Item Description" DataField="Description" ReadOnly="True" SortExpression="Description"  />      
                                        <asp:TemplateField HeaderText="RM Qty" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" FooterStyle-HorizontalAlign="Center">
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtBOMQty" runat="server" Width="6em" style="text-align:center" Text='<%# Eval("RMQty") %>'  onkeydown='<%# "triggerSaveOnEnter(event, \"" + ((GridViewRow)Container).FindControl("lbtnLineSave").ClientID + "\")" %>'></asp:TextBox>
                                                <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtBOMQty" FilterType="Custom, Numbers" ValidChars="." />
                                            </ItemTemplate>
                                            </asp:TemplateField>
                                         <asp:BoundField HeaderText="UOM" DataField="BomUnit" ReadOnly="True" ItemStyle-Width="2em" ItemStyle-HorizontalAlign="Center"  />
                                        <asp:BoundField HeaderText="Unit Cost" DataField="AvCost" ReadOnly="True" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" FooterStyle-HorizontalAlign="Right" ItemStyle-Width="6em" />
                                        <asp:BoundField HeaderText="Cost" DataField="AvRMCost" ReadOnly="True" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" FooterStyle-HorizontalAlign="Right" ItemStyle-Width="6em" />
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnLineSave" CommandArgument='<%# Eval("BLID") %>' CommandName="lbtnLineSave" runat="server" CssClass="fa fa-save buttonRed" ToolTip="Save BOM Line" OnClick="lbtnLineSave_Click"> </asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnDeleteLine" CommandArgument='<%# Eval("BLID") %>' CommandName="lbtnDeleteLine" runat="server" CssClass="fa fa-ban" ToolTip="Delete Line" OnClick="lbtnDeleteLine_Click" style="color:red"> </asp:LinkButton>
                                                        <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Delete BOM Line?" Enabled="True" TargetControlID="lbtnDeleteLine"></cci:ConfirmButtonExtender>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                                 <asp:Panel ID="Panel3" runat="server" style="float:left; margin-top:1em">
                                         <table style="width:35em; font-size:0.8em">
                                             <tr>
                                                <td>This BOM Cost: </td>
                                                 <td style="text-align:right"><asp:Label ID="lblNewBOMCost" runat="server" Text=""></asp:Label></td> 
                                             </tr>    
                                             <tr>
                                                     <td>Current Sage Item Cost: </td>
                                                     <td  style="text-align:right"><asp:Label ID="lblItemCost" runat="server" Text=""></asp:Label></td> 
                                                  </tr>
                                             
                                             <tr>
                                                 <td>Current Sage Selling Price:</td>
                                                 <td style="text-align:right"><asp:Label ID="lblSageSell" runat="server" Text=""></asp:Label></td> 
                                             </tr>
                                             <tr>
                                                <td>Current GP% (This BOM Cost vs Sage Selling Price):</td>
                                                <td style="text-align:right"><asp:Label ID="lblCurrGP" runat="server" Text=""></asp:Label></td> 
                                            </tr>
                                          <tr>
                                              <td colspan="2"><hr /></td>
                                          </tr>
                                                 <tr>
                                                <td>Required GP%:</td>
                                                <td style="text-align:right"><asp:TextBox ID="txtNewGP" runat="server" Width="70" style="text-align:right; font-size:1em" oninput="calculateNewSell()" AutoPostBack="false">1.00</asp:TextBox>
                                                    <cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender3" runat="server" TargetControlID="txtNewGP" FilterType="Custom, Numbers" ValidChars="." />
                                                </td> 
                                            </tr>
                                             <tr>
                                            <td>New Selling Price:</td>
                                            <td style="text-align:right"><asp:TextBox ID="txtNewSell" runat="server" Width="70" style="text-align:right; font-size:1em">1.00</asp:TextBox>
                                                <cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender4" runat="server" TargetControlID="txtNewSell" FilterType="Custom, Numbers" ValidChars="." />
                                            </td> 
                                        </tr>
                                         </table>
                                     </asp:Panel>
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
                                </asp:Panel><br /><br /><br />
                                <asp:Panel ID="Pane32" runat="server">
                                <div style="width:100%; text-align:center; margin-top:7em ">
                                     <asp:LinkButton ID="lbtnSBCAUpdate" CssClass="icon fa-upload buttonIndex" runat="server" ToolTip="Update Sage with this BOM cost and new selling price?" OnClick="lbtnSBCAUpdate_Click" style="float:left"> Update Sage</asp:LinkButton>
                                    <cci:ConfirmButtonExtender ID="lbtnSBCAUpdate_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Update Sage Accounting Average Cost?" Enabled="True" TargetControlID="lbtnSBCAUpdate"></cci:ConfirmButtonExtender>
                                    <cci:ConfirmButtonExtender ID="lbtnDeleteBom_ConfirmButtonExtender1" runat="server" ConfirmText="Delete this BOM? Are you sure?" Enabled="True" TargetControlID="lbtnDeleteBom"></cci:ConfirmButtonExtender>
                                    <asp:LinkButton ID="lbtnDeleteBom" runat="server" style="color:red; font-size:.8em; padding-right:2em; float:right" CssClass="icon fa-ban button" OnClick="lbtnDeleteBom_Click" > Delete BOM</asp:LinkButton>
                                    <asp:LinkButton ID="LbtnSaveBOM" runat="server" style="font-size:1em" CssClass="icon fa-save buttonSage" OnClick="LbtnSaveBOM_Click"> SAVE BOM</asp:LinkButton>
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
     <script type="text/javascript">
         window.onload = function () {
             calculateNewSell();
         };
     </script>
     <script type="text/javascript">
        function calculateNewSell() {
            // Get the client IDs for the controls
            var costLabel = document.getElementById('<%= lblNewBOMCost.ClientID %>');
            var gpInput = document.getElementById('<%= txtNewGP.ClientID %>');
            var sellInput = document.getElementById('<%= txtNewSell.ClientID %>');

            if (!costLabel || !gpInput || !sellInput) return;

            // Get values
            var cost = parseFloat(costLabel.innerText.replace(/,/g, ''));
            var gp = parseFloat(gpInput.value.replace(/,/g, ''));

            if (isNaN(cost) || isNaN(gp) || gp >= 100) {
                sellInput.value = '';
                return;
            }

            var newSell = cost / (1 - (gp / 100));
            if (isFinite(newSell)) {
                sellInput.value = newSell.toFixed(2);
            } else {
                sellInput.value = '';
            }
        }
    </script>
     <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</body>
</html>
