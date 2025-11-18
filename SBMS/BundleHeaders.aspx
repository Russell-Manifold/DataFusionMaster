<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="BundleHeaders.aspx.cs" Inherits="SBMS.BundleHeaders" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Bundles</title>
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
                        <h3 style="padding-top:0; line-height:1em">Bundles <span style="font-size:.6em; text-align:center">(Bundles editable within Sage Accounting)</span></h3>                    
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
                                           <td></td>
                                            <td style="text-align:right">Find Bundle <asp:TextBox ID="txtfind" runat="server"></asp:TextBox><asp:LinkButton ID="lbtnfind" runat="server" CssClass="fa fa-search"></asp:LinkButton></td>            
                                        </tr>
                                    </table>  
                                    <br />
                                </asp:Panel>
                                <asp:GridView ID="GridBundles" runat="server" AutoGenerateColumns="false" CssClass="gridview"  ToolTip="Open Bundle" OnRowDataBound="GridBundles_RowDataBound">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <Columns>
                                        <asp:BoundField DataField="BundID"  />
                                        <asp:TemplateField HeaderText="Bundle Code" ItemStyle-Width="12em" >
                                            <ItemTemplate >
                                                <asp:LinkButton ID="lbtnBundle" CommandArgument='<%# Eval("BundCode")%>' CommandName="lbtnBundle" runat="server" Text='<%# Eval("BundCode")%>' ToolTip="View Bundle" style="color:#4A82AB; font-weight:600" OnClick="lbtnBundle_Click" ></asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Description" DataField="BundDescription" ReadOnly="True"  />
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
