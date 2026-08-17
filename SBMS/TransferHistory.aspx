<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="TransferHistory.aspx.cs" Inherits="SBMS.TransferHistory" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Transfer History</title>
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
                                <asp:LinkButton ID="lbtnHist" runat="server" class="buttonC icon fa-wpforms" PostBackUrl="~/TransferSelect.aspx" >&nbsp;Item Movement Select</asp:LinkButton>
                                <asp:LinkButton ID="lbtndwnload" runat="server" class="buttonC icon fa-download" OnClick="lbtndwnload_Click"  >&nbsp;Excel</asp:LinkButton>
                                <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right;" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                                <h3 style="padding-top:0; line-height:1em">Items Movement History</h3>
                             </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
                <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                    <ContentTemplate>
                        <div class="row 150%">
                             <div class="12u 12u$(medium)" style="text-align:center">
                             <h4><asp:Label ID="lblerr" runat="server" Text=" " ForeColor="Red"></asp:Label></h4>   
                             </div>
                            <div class="1u 12u$(medium)">&nbsp;</div>
                              <div class="10u 12u$(medium)" style="text-align:center">
                                <asp:GridView ID="GridHistory" runat="server" CssClass="gridview" AutoGenerateColumns="false" Width="100%" AllowPaging="true" PageSize="50" OnPageIndexChanging="GridHistory_PageIndexChanging" AllowCustomPaging="False" ShowFooter="True">
                                     <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="5" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <Columns>
                                        <asp:BoundField HeaderText="Code" DataField="ItemCode" ReadOnly="True" ItemStyle-Width="8em" SortExpression="ItemCode"/>
                                        <asp:BoundField HeaderText="Description" DataField="ItemDescription" ReadOnly="True"  ItemStyle-HorizontalAlign="Left" SortExpression="ItemDescription" />
                                        <asp:BoundField HeaderText="Lot Num" DataField="LotNumber" ReadOnly="True" ItemStyle-Width="8em" SortExpression="LotNumber" />
                                        <asp:BoundField HeaderText="Type" DataField="TransactionType" ReadOnly="True" ItemStyle-Width="8em" SortExpression="TransactionType" />
                                        <asp:BoundField HeaderText="Document" DataField="DocNum" ReadOnly="True" ItemStyle-Width="8em" SortExpression="DocNum"/>
                                        <asp:BoundField HeaderText="Qty" DataField="Qty" ReadOnly="True" ItemStyle-Width="8em" SortExpression="Qty" DataFormatString="{0:N2}"/>
                                        <asp:BoundField HeaderText="From" DataField="FromStore" ReadOnly="True" ItemStyle-Width="5em" SortExpression="FromStore"/>
                                        <asp:BoundField HeaderText="To" DataField="ToStore" ReadOnly="True" ItemStyle-Width="5em" SortExpression="ToStore"/>
                                        <asp:BoundField HeaderText="Date" DataField="TransactionDate" ReadOnly="True" ItemStyle-Width="8em" SortExpression="TransactionDate" DataFormatString="{0:dd MMM yyyy}" HtmlEncode="false" />
                                        <asp:BoundField HeaderText="By" DataField="TrfBy" ReadOnly="True" SortExpression="TrfBy"/>             
                                    </Columns>                                
                                </asp:GridView>
                            </div>
                            <div class="1u 12u$(medium)">&nbsp;</div>   
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
        </div>
    </form>
</body>
</html>
