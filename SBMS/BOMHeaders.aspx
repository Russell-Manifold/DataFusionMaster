<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="BOMHeaders.aspx.cs" Inherits="SBMS.BOMHeaders" %>
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
                        <asp:LinkButton ID="LbtnConfig" runat="server" class="buttonC icon fa-gears" PostBackUrl="~/ConfigMaster.aspx" > &nbsp;Settings</asp:LinkButton>
                        <asp:LinkButton ID="lbtnBack" runat="server" class="buttonC icon fa-align-left" PostBackUrl="~/ItemsHeaders.aspx" >&nbsp;Items</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonC icon fa-eject" style="float:right" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <h3 style="padding-top:0; line-height:1em">Bill Of Materials (BOM's)</h3>                    
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
                                            <td style="text-align:left">Find (BOM Code) <asp:TextBox ID="txtfind" runat="server"></asp:TextBox><asp:LinkButton ID="lbtnfind" runat="server" CssClass="fa fa-search buttonC" OnClick="lbtnfind_Click"></asp:LinkButton></td>       
                                            <td style="text-align:right"><asp:LinkButton ID="lbtnCreateNew" runat="server" CssClass="icon fa-plus-circle buttonRed" ToolTip="Add New BOM" OnClick="lbtnCreateNew_Click"> Add Bill Of Materials</asp:LinkButton>
                                               <cci:ConfirmButtonExtender ID="lbtnCreateNew_ConfirmButtonExtender1" runat="server" ConfirmText="Create a new BOM, are you sure?" Enabled="True" TargetControlID="lbtnCreateNew"></cci:ConfirmButtonExtender>
                                           </td>            
                                        </tr>
                                    </table>  
                                    <br />
                                </asp:Panel>
                                <asp:GridView ID="GridBOM" runat="server" AutoGenerateColumns="false" CssClass="gridview"  ToolTip="Open BOM" OnRowDataBound="GridBOM_RowDataBound">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <Columns>
                                        <asp:BoundField DataField="BomHID"  />
                                        <asp:TemplateField HeaderText="BOM_Code" ItemStyle-Width="5em" >
                                            <ItemTemplate >
                                                <asp:LinkButton ID="lbtnBOM" CommandArgument='<%# Eval("BomHID")%>' CommandName="lbtnBOM" runat="server" Text='<%# Eval("BOMCode")%>' ToolTip="View Bill OF Materials" style="color:#4A82AB; font-weight:600; border:1px solid #4A82AB" width="120px" OnClick="lbtnBOM_Click" ></asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Bom Description" DataField="BomDescript" ReadOnly="True"  />
                                        <asp:BoundField HeaderText="FG Code" DataField="FGCode" ReadOnly="True"  />
                                        <asp:BoundField HeaderText="FG Description" DataField="FGDescript" ReadOnly="True"  />
                                        <asp:TemplateField ItemStyle-Width="3em" HeaderText="Active" >
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkActive" runat="server" Checked='<%# Eval("BomActive") %>' Enabled="false"/>        
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                            <ItemTemplate>
                                                 <asp:LinkButton ID="lbtnDeleteLine" CommandArgument='<%# Eval("BomHID") %>' CommandName="lbtnDeleteLine" runat="server" CssClass="fa fa-ban" ToolTip="Delete Line" OnClick="lbtnDeleteLine_Click" style="color:red"> </asp:LinkButton>
                                                <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Delete BOM?" Enabled="True" TargetControlID="lbtnDeleteLine"></cci:ConfirmButtonExtender>
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
