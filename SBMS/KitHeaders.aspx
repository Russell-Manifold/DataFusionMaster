<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="KitHeaders.aspx.cs" Inherits="SBMS.KitHeaders" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Kits</title>
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
                        <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="LbtnConfig" runat="server" class="buttonC icon fa-gears" PostBackUrl="~/ConfigMaster.aspx" > &nbsp;Settings</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <h3 style="padding-top:0; line-height:1em">Finished Goods Kits</h3>                    
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
                <div class="row 150%">
                      <div class="1u 12u$(medium)">&nbsp;</div>    
                    <div class="10u 12u$(medium)">
                        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                            <ContentTemplate>
                                <div style="display:none">
                                <asp:Label ID="lblDir" runat="server" Text=""></asp:Label>
                                </div> 
                                <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnfind">
                                    <table style="width: 100%">
                                        <tr>
                                           <td><asp:LinkButton ID="lbtnCreateNew" runat="server" CssClass=" fa fa-plus-circle buttonRed" ToolTip="Add New Kit" OnClick="lbtnCreateNew_Click"> Add New Kit</asp:LinkButton>
                                               <cci:ConfirmButtonExtender ID="lbtnCreateNew_ConfirmButtonExtender1" runat="server" ConfirmText="Create a new Kit, are you sure?" Enabled="True" TargetControlID="lbtnCreateNew"></cci:ConfirmButtonExtender>
                                           </td>
                                            <td style="text-align:right">Find Kit) <asp:TextBox ID="txtfind" runat="server"></asp:TextBox><asp:LinkButton ID="lbtnfind" runat="server" CssClass="fa fa-search buttonC"></asp:LinkButton></td>            
                                        </tr>
                                    </table>  
                                    <br />
                                </asp:Panel>
                                <asp:GridView ID="GridBOM" runat="server" AutoGenerateColumns="false" CssClass="gridview"  ToolTip="Open Kit" OnRowDataBound="GridBOM_RowDataBound">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <Columns>
                                        <asp:BoundField DataField="KitHID"  />
                                        <asp:TemplateField HeaderText="Item Code" ItemStyle-Width="6em" >
                                            <ItemTemplate >
                                                <asp:LinkButton ID="lbtnBOM" CommandArgument='<%# Eval("KitHID")%>' CommandName="lbtnBOM" runat="server" Text='<%# Eval("FGCode")%>' ToolTip="View Bill OF Materials" style="color:#4A82AB; font-weight:600; border:1px solid #4A82AB" width="120px" OnClick="lbtnBOM_Click" ></asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Description" DataField="FGDescript" ReadOnly="True"  />
                                        <asp:TemplateField ItemStyle-Width="3em" HeaderText="Active" >
                                        <ItemTemplate>
                                            <asp:CheckBox ID="chkActive" runat="server" Checked='<%# Eval("KitActive") %>' Enabled="false"/>        
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                        <ItemTemplate>
                                             <asp:LinkButton ID="lbtnDeleteLine" CommandArgument='<%# Eval("KitHID") %>' CommandName="lbtnDeleteLine" runat="server" CssClass="fa fa-ban" ToolTip="Delete Line" OnClick="lbtnDeleteLine_Click" style="color:red"> </asp:LinkButton>
                                            <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Delete Kit?" Enabled="True" TargetControlID="lbtnDeleteLine"></cci:ConfirmButtonExtender>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                         </ div> 
                    <div class="1u 12u$(medium)">&nbsp;</div>    
                    </div>
                </div>
        </div>
    </form>
</body>
</html>
