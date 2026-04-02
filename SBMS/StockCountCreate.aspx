<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="StockCountCreate.aspx.cs" Inherits="SBMS.StockCountCreate" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Stock Counts</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <script type="text/javascript">
    function selectAllCheckboxes(selectAllCheckbox) {
        var gridView = document.getElementById('<%= GridItemsSelect.ClientID %>');
        var checkboxes = gridView.getElementsByTagName('input');
        for (var i = 0; i < checkboxes.length; i++) {
            if (checkboxes[i].type == 'checkbox' && checkboxes[i] != selectAllCheckbox) {
                checkboxes[i].checked = selectAllCheckbox.checked;
            }
        }
    }
</script>
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
                               <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" Style="float: right;" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                                <br />
                                <h3 style="padding-top: 0; line-height: 1em">New Stock Count</h3>
                            </div>
                            <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /> </div>
                         </div>  
                            <div class="row 150%">
                                <div class="1u 12u$(medium)" style="text-align:center">&nbsp</div>      
                                <div class="10u 12u$(medium)" style="text-align:center">
                                      <h3>Details</h3>
                                            <table>
                                                <tr>
                                                    <td>Reference *</td>
                                                    <td>
                                                        <asp:TextBox ID="txtRef" runat="server" placeholder="Reference"></asp:TextBox></td>
                                                    <td>Created Date</td>
                                                    <td><asp:Label ID="lblDate" runat="server" Text=""></asp:Label>&nbsp;</td>
                                                    <td>Created By<asp:Label ID="CntID" runat="server" Text="" style="display:none"></asp:Label></td>
                                                    <td><asp:Label ID="lblCreatedBy" runat="server" Text=""></asp:Label></td>
                                                    <td>
                                                        <asp:Label ID="lblCountID" runat="server" Text=""></asp:Label></td>
                                                    <td></td>
                                                </tr>
                                                <tr>
                                                    <td colspan="6"><hr /></td>
                                                </tr>
                                            <tr>
                                                <td></td>
                                                <td></td>    
                                                <td>Category</td>
                                                    <td style="text-align:left"> <asp:DropDownList ID="DDCateg" runat="server" style="width:10em" AutoPostBack="true" OnSelectedIndexChanged="DDCateg_SelectedIndexChanged"></asp:DropDownList></td>
                                                    <td style="padding-left:2em">Filter</td>
                                                    <td>
                                                        <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnSearch">
                                                             <asp:TextBox ID="txtFilter" runat="server" style="width:10em" placeholder="Code/Description"></asp:TextBox><asp:LinkButton ID="lbtnSearch" runat="server" CssClass="icon fa-search buttonC" ToolTip="Search" OnClick="lbtnSearch_Click"></asp:LinkButton>
                                                        </asp:Panel>
                                                       </td>
                                                    <td style="padding-left:2em">Store</td>
                                                    <td><asp:DropDownList ID="DDStore" runat="server" style="width:10em; float:left" AutoPostBack="true" OnSelectedIndexChanged="DDStore_SelectedIndexChanged"></asp:DropDownList></td>
                                                </tr>    
                                            </table>
                                           <hr />    
                                    </div>
                         <div class="1u 12u$(medium)" style="text-align:center">&nbsp</div>  
                                </div>
                
                            <div class="row 150%">    
                                 <div class="1u 12u$(medium)" style="text-align:center">&nbsp</div>  
                                 <div class="4u 12u$(medium)" style="text-align:center">     
                                     <h3>Select Items <asp:LinkButton ID="lbtnAddSelected" runat="server" style="float:right; font-size:.8em" CssClass="icon fa-plus-square buttonRed" OnClick="lbtnAddSelected_Click"> Add Selected</asp:LinkButton></h3>
                                            <asp:GridView ID="GridItemsSelect" runat="server" AutoGenerateColumns="false" CssClass="gridview" AllowSorting="true" OnSorting="GridCntLines_Sorting" OnRowDataBound="GridCntLines_RowDataBound" >
                                              <HeaderStyle CssClass="gridViewHeader" />
                                              <FooterStyle CssClass="gridViewHeader" />
                                              <RowStyle CssClass="gridViewRow" />
                                              <AlternatingRowStyle CssClass="gridViewAltRow" />
                                              <PagerStyle CssClass="gridViewPager" />
                                              <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="5" />
                                              <Columns>
                                                  <asp:BoundField DataField="ItemID" ReadOnly="True" />
                                                  <asp:BoundField DataField="CategoryDescript" ReadOnly="True" HeaderText="Category" SortExpression="CategoryDescript" />
                                                  <asp:BoundField DataField="Code" ReadOnly="True" HeaderText="Code" SortExpression="Code" />
                                                  <asp:BoundField DataField="Description" ReadOnly="True" HeaderText="Item"  SortExpression="Description"/>
                                                  <asp:BoundField DataField="QOH" ReadOnly="True" HeaderText="QOH"  SortExpression="QOH"/>
                                                  <asp:BoundField DataField="StoreCode" ReadOnly="True" HeaderText="Store"  SortExpression="StoreCode"/>
                                                      <asp:TemplateField HeaderText="Select All" ItemStyle-Width="5em">
                                                          <HeaderTemplate >
                                                                <asp:CheckBox ID="chkSelectAll" runat="server" onclick="selectAllCheckboxes(this);" />
                                                            </HeaderTemplate>
                                                          <ItemTemplate>
                                                              <asp:CheckBox ID="chkSelect" runat="server" />
                                                          </ItemTemplate>
                                                      </asp:TemplateField>
                                              </Columns>
                                          </asp:GridView>
                                     </div>
                                <div class="2u 12u$(medium)" style="text-align:center">&nbsp</div>  
                                      <div class="4u 12u$(medium)" style="text-align:center">       
                                        <h3>Selected Items</h3>
                                          <asp:GridView ID="GridCntLines" runat="server" AutoGenerateColumns="false" CssClass="gridview" AllowSorting="true" OnSorting="GridCntLines_Sorting" OnRowDataBound="GridCntLines_RowDataBound" >
                                                <HeaderStyle CssClass="gridViewHeader" />
                                                <FooterStyle CssClass="gridViewHeader" />
                                                <RowStyle CssClass="gridViewRow" />
                                                <AlternatingRowStyle CssClass="gridViewAltRow" />
                                                <PagerStyle CssClass="gridViewPager" />
                                                <PagerSettings Visible="true" Mode="Numeric" PageButtonCount="5" />
                                                <Columns>
                                                    <asp:BoundField DataField="CountID" ReadOnly="True" />      
                                                    <asp:BoundField DataField="ItemCode" ReadOnly="True" HeaderText="Code" SortExpression="ItemCode" />
                                                    <asp:BoundField DataField="ItemDescription" ReadOnly="True" HeaderText="Item"  SortExpression="ItemCode"/>
                                                    <%--<asp:BoundField DataField="StoreCode" ReadOnly="True" HeaderText="Store"  SortExpression="StoreCode"/>--%>
                                                    <%--<asp:BoundField DataField="LotNumber" ReadOnly="True" HeaderText="Lot Number"  SortExpression="LotNumber"/>--%>
                                                </Columns>
                                            </asp:GridView>
                                     </ div> 
                                <div class="1u 12u$(medium)" style="text-align:center">&nbsp</div>  
                              </div>
                    </div>
                </div>
    </form>
</body>
</html>
