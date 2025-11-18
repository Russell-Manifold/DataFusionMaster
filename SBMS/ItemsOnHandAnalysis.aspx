<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ItemsOnHandAnalysis.aspx.cs" Inherits="SBMS.ItemsOnHandAnalysis" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Items Analysis</title>
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
                        <asp:LinkButton ID="lbtnQOH" runat="server" class="buttonC icon fa-codepen" PostBackUrl="~/ItemsHeaders.aspx" >&nbsp;Items</asp:LinkButton>
                         <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <h3 style="padding-top:0; line-height:1em">Items Analysis</h3>                    
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
                                <asp:Panel ID="Panel1" runat="server" DefaultButton="btnFilter"> 
                                    Find&nbsp;
                                <asp:TextBox ID="txtItemCode" runat="server" placeholder="Item Code"></asp:TextBox>
                                <asp:TextBox ID="txtStoreCode" runat="server" placeholder="Store Code"></asp:TextBox> 
                                <asp:LinkButton ID="btnFilter" runat="server" Text=" " OnClick="btnFilter_Click" CssClass="icon fa-search buttonC" />
                                </asp:Panel>
                                <asp:GridView ID="GridItems" runat="server" AutoGenerateColumns="false" CssClass="gridview"
                                    AllowSorting="true" AllowPaging="true" PageSize="50" OnSorting="GridItems_Sorting" OnPageIndexChanging="GridItems_PageIndexChanging" OnRowDataBound="GridItems_RowDataBound" ShowFooter="true">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <Columns>        
                                        <asp:BoundField HeaderText="ItemCode" DataField="ItemCode" ReadOnly="True" SortExpression="ItemCode" />
                                        <asp:BoundField HeaderText="Description" DataField="ItemDescription" ReadOnly="True" SortExpression="ItemDescription" />
                                        <asp:BoundField HeaderText="QOH (Sage)" DataField="TotalOnHand" ReadOnly="True" ItemStyle-Width="9em"  HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" SortExpression="TotalOnHand" DataFormatString ="{0:N2}" />
                                        <asp:BoundField HeaderText="Store" DataField="StoreCode" ReadOnly="True"  ItemStyle-Width="5em" SortExpression="StoreCode" />
                                        <asp:BoundField HeaderText="QOH" DataField="QOH" ReadOnly="True" SortExpression="QOH" ItemStyle-Width="5em" ItemStyle-HorizontalAlign="Center" FooterStyle-HorizontalAlign="Center" DataFormatString ="{0:N2}"/>
                                        <asp:BoundField HeaderText="LotNumber" DataField="LotNumber" ReadOnly="True" SortExpression="LotNumber" ItemStyle-Width="8em"/>
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
