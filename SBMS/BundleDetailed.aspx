<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="BundleDetailed.aspx.cs" Inherits="SBMS.BundleDetailed" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Bundle Details</title>
    <link rel="stylesheet" href="assets/css/main.css" />
        <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
        <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
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
                        <asp:LinkButton ID="LinkButton1" runat="server" class="buttonC icon fa-angle-double-left" PostBackUrl="~/BundleHeaders.aspx">&nbsp;Bundles</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <h3 style="padding-top:0; line-height:1em">Bundle Details<br />
                            <span style="font-size:.6em; text-align:center">(Bundles editable within Sage Accounting)</span>
                        </h3>  
                        
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
                <div class="row 150%">
                      <div class="2u 12u$(medium)">&nbsp;</div>    
                    <div class="8u 12u$(medium)">
                        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                            <ContentTemplate>
                                 <asp:Panel ID="Panel1" runat="server">
                                    <table style="width: 100%">
                                        <tr>
                                            <td style="width:150px">Bundle Code:</td>
                                            <td style="width:150px"><asp:Label ID="lblFGCode" runat="server" Text=""></asp:Label></td>           
                                            <td><asp:Label ID="lblFGDescript" runat="server" Text=""></asp:Label></td>       
                                            <td></td>
                                        </tr>
                                    </table>  
                                    <hr />
                                </asp:Panel>
                                <h3>Bundle Lines</h3>
                                <asp:GridView ID="GridBundleLines" runat="server" AutoGenerateColumns="false" CssClass="gridview" >
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <Columns>
                                       <asp:BoundField HeaderText="Code" DataField="BLCode" ReadOnly="True" ItemStyle-Width="10em"  />
                                       <asp:BoundField HeaderText="Description" DataField="BLDescription" ReadOnly="True"  />
                                       <asp:BoundField HeaderText="Quantity" DataField="BLQuantity" ReadOnly="True" DataFormatString="{0:N4}" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="6em" />
                                       <asp:BoundField HeaderText="Av Unit Cost" DataField="BLAverageCost" ReadOnly="True" DataFormatString="{0:N4}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" ItemStyle-Width="10em" />        
                                    </Columns>
                                </asp:GridView>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                         </ div> 
                    <div class="2u 12u$(medium)">&nbsp;</div>    
                    </div>
                </div>
        </div>
    </form>
</body>
</html>
