<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ItemEdit.aspx.cs" Inherits="SBMS.ItemEdit" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Edit Item</title>
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
                         <asp:LinkButton ID="lbtnBack" runat="server" class="buttonC icon fa-align-left" PostBackUrl="~/ItemsHeaders.aspx" >&nbsp;Items</asp:LinkButton>
                        <asp:LinkButton ID="lbtnBOM" runat="server" class="buttonC icon fa-book" onclick="lbtnBOM_Click" >&nbsp;View Bill Of Materials (BOM)</asp:LinkButton> 
                        <asp:LinkButton ID="LbtnKit" runat="server" class="buttonC icon fa-bookmark" onclick="LbtnKit_Click">&nbsp;View Kit</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" onclick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <h3 style="padding-top:0; line-height:1em">Item Master</h3>                    
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
                <div class="row 150%">
                      <div class="1u 12u$(medium)">&nbsp;</div>    
                    <div class="10u 12u$(medium)">
                        <table style="width:100%; font-size:1.25em">
                            <tr>
                                <td>Item Code: <asp:Label ID="lblItemCode" runat="server" Text=""></asp:Label></td>
                                <td style="vertical-align:top; text-align:right">Item Description:&nbsp; </td>
                                <td colspan="2" ><asp:Label ID="txtDescription" runat="server" style="width:100%"></asp:Label></td>
                            </tr>
                         </table>
                        <br /><br />
                        <cci:Accordion ID="Accordion1" runat="server" SelectedIndex="0" ContentCssClass="accordionContent"
                          headercssclass="accordionHeader" headerselectedcssclass="accordionHeaderSelected" AutoSize="None" FadeTransitions="true" TransitionDuration="450" FramesPerSecond="40" RequireOpenedPane="true" >
                         <Panes>
                             <cci:AccordionPane runat="server">
                                  <Header>
                                          1) Item Basic Details
                                    </Header>
                                 <Content>
                                    <table style="width:50em; margin:auto">
                                        <tr>
                                            <td style="width:50%; text-align:left">
                                                <asp:CheckBox ID="chkPhysical" runat="server" text=" Physical" Enabled="false"/><br />
                                                   <asp:CheckBox ID="chkisFinished" runat="server" text="  Is Finished Item "/><br />
                                                <asp:CheckBox ID="chkIsFromBom" runat="server" text=" Is Made Up From A BOM "/><br />
                                                   <asp:CheckBox ID="chkisBom" runat="server" text="  Is a BOM Component"/><br />
                                                 <asp:CheckBox ID="chkIsFromKit" runat="server" text=" Is Made Up From A Kit "/><br />
                                                   <asp:CheckBox ID="chkisKit" runat="server" text=" Is a Kit Component "/><br />        
                                            </td>
                                            <td style="text-align:right; vertical-align:top">
                                                <asp:Label ID="LbLIsTracked" runat="server" Text="Item is Lot Tracked"></asp:Label><asp:CheckBox ID="chkIsTracked" runat="server" text=" " Enabled="false"/><br /><br />
                                                Minimum Level <asp:TextBox ID="txtReOrdQty" runat="server" Width="60px">0</asp:TextBox>
                                                <cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender1" runat="server" TargetControlID="txtReOrdQty" FilterType="Custom, Numbers" ValidChars="." />
                                            </td>
                                        </tr>
                                    </table>
                                     
                                    </Content>
                                 </cci:AccordionPane>     
                             <cci:AccordionPane runat="server">
                                  <Header>
                                          2) Linked Stores
                                    </Header>
                                 <Content>
                                     <div style="width:50em; margin:auto">
                                          <asp:CheckBoxList ID="chkStores" runat="server" Width="100%" style="text-align:left"></asp:CheckBoxList>
                                     </div>
                                     
                                    </Content>
                                 </cci:AccordionPane>
                              <cci:AccordionPane runat="server">
                                  <Header>
                                          3) Qty On Hand By Store
                                    </Header>
                                 <Content>
                                    <asp:GridView ID="GridQOHByStore" runat="server" AutoGenerateColumns="false" Width="100%" CssClass="gridview" ShowFooter="true">
                                        <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                         <Columns>
                                              <asp:BoundField HeaderText="Store" DataField="StoreCode" ReadOnly="True" />
                                              <asp:BoundField HeaderText="Qty On Hand" DataField="QOH" ReadOnly="True" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" FooterStyle-HorizontalAlign="Right" />
                                             </Columns>
                                        </asp:GridView>
                                    </Content>
                                 </cci:AccordionPane>
                              <cci:AccordionPane runat="server">
                                  <Header>
                                          4) Bar Codes
                                    </Header>
                                 <Content>
                                    <table style="margin:auto">
                                       <tr>
                                           <td>Barcode</td>
                                           <td><asp:Label ID="lblBCodeQty" runat="server" Text="Qty of items represented"></asp:Label></td>
                                       </tr>
                                        <tr>
                                            <td><asp:TextBox ID="txtBarcode1" runat="server" style="width:25em" placeholder="Barcode 1"></asp:TextBox></td>
                                            <td><asp:TextBox ID="txtBQty1" runat="server" style="width:5em; text-align:center" TextMode="Number" placeholder="Qty 1"></asp:TextBox></td>
                                        </tr>
                                        <tr>
                                            <td><asp:TextBox ID="txtBarcode2" runat="server" style="width:25em" placeholder="Barcode 2"></asp:TextBox></td>
                                            <td><asp:TextBox ID="txtBQty2" runat="server" style="width:5em; text-align:center" TextMode="Number" placeholder="Qty 2"></asp:TextBox></td>
                                        </tr>
                                    
                                        <tr>
                                            <td><asp:TextBox ID="txtBarcode3" runat="server" style="width:25em" placeholder="Barcode 3"></asp:TextBox></td>
                                            <td><asp:TextBox ID="txtBQty3" runat="server" style="width:5em; text-align:center" TextMode="Number" placeholder="Qty 3"></asp:TextBox></td>
                                        </tr>
                                    </table> 
                                     <br />
                                     <br />
                                    </Content>
                                 </cci:AccordionPane>
                             <cci:AccordionPane runat="server">
                            <Header>
                                    5) User Defined Fields
                              </Header>
                           <Content>
                              <h4>(Edits only permitted in Sage Accounting)</h4>
                               <asp:TextBox ID="TxtUDF1" runat="server" style="width:15em" placeholder="User Text 1" Enabled="false"></asp:TextBox><br />
                              <asp:TextBox ID="TxtUDF2" runat="server" style="width:15em" placeholder="User Text 2" Enabled="false"></asp:TextBox><br />
                              <asp:TextBox ID="TxtUDF3" runat="server" style="width:15em" placeholder="User Text 3" Enabled="false"></asp:TextBox><br />
                               <hr />
                               <asp:TextBox ID="txtUDFN1" runat="server" style="width:15em" placeholder="User Number 1" Enabled="false"></asp:TextBox><br />
                                <asp:TextBox ID="txtUDFN2" runat="server" style="width:15em" placeholder="User Number 2" Enabled="false"></asp:TextBox><br />
                                <asp:TextBox ID="txtUDFN3" runat="server" style="width:15em" placeholder="User Number 3" Enabled="false"></asp:TextBox><br />
                                 <hr />
                              </Content>
                           </cci:AccordionPane>
                             </Panes>
                            </cci:Accordion>
                        <br />
                        <asp:LinkButton ID="lbtnSaveItem" runat="server" CssClass="icon fa-save buttonRed" OnClick="lbtnSaveItem_Click" style="width:100%; font-size:1.2em" >&nbsp;Save Changes</asp:LinkButton>
                  
                        <h2>Item Movement History</h2>
                        Store <asp:DropDownList ID="ddStore" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddStore_SelectedIndexChanged" CssClass="optiondd"></asp:DropDownList>
                            <asp:GridView ID="GridItemTrans" runat="server" AutoGenerateColumns="false" CssClass="gridview"  ShowHeaderWhenEmpty ="true" ShowFooter="true" OnRowDataBound="GridItemTrans_RowDataBound" >
                            <HeaderStyle CssClass="gridViewHeader" />
                            <FooterStyle CssClass="gridViewHeader" />
                            <RowStyle CssClass="gridViewRow" />
                            <AlternatingRowStyle CssClass="gridViewAltRow" />
                              <Columns>
                                <asp:BoundField DataField="TransactionType" HeaderText="Type" ReadOnly="True" />
                                <asp:BoundField DataField="Document" ReadOnly="True" HeaderText="Document" />
                                <asp:BoundField DataField="LotNumber" ReadOnly="True" HeaderText="Lot Number"/>
                                <asp:BoundField DataField="Qty" ReadOnly="True" HeaderText="Qty" DataFormatString="{0:N2}" />
                                <asp:BoundField DataField="TotalLineValExcl" ReadOnly="True" HeaderText="Line Value" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" FooterStyle-HorizontalAlign="Right" DataFormatString="{0:N2}" />
                                  <asp:BoundField DataField="Store" ReadOnly="True" HeaderText="Store" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                 <asp:BoundField DataField="TransactionDate" ReadOnly="True" HeaderText="Date" DataFormatString="{0:dd MMM}" />
                                 <asp:BoundField DataField="ByRole" ReadOnly="True" HeaderText="By"/>
                                 <asp:BoundField DataField="TransactionReference" ReadOnly="True" HeaderText="Reference" />       
                            </Columns>
                        </asp:GridView>
                    </ div> 
                    <div class="1u 12u$(medium)">&nbsp;</div>    
                    </div>
                </div>
        </div>
    </form>
</body>
</html>
