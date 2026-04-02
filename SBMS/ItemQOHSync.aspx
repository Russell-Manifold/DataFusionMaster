<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ItemQOHSync.aspx.cs" Inherits="SBMS.ItemQOHSync" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Items</title>
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
                        <asp:LinkButton ID="LbtnConfig" runat="server" class="buttonC icon fa-gears" PostBackUrl="~/ConfigMaster.aspx" > &nbsp;Settings</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <h3 style="padding-top:1em; line-height:1em">Import Opening Balances To Stores</h3>                    
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>       
                  <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
                            <ProgressTemplate>
                                <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
                             <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." style="padding: 10px; padding-top:15%; border-radius:1.5em" />
                                </div>
                              </ProgressTemplate>
                        </asp:UpdateProgress> 
                  <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                            <ContentTemplate>    
                                    <div class="row 150%">                   
                                        <div class="6u 12u$(medium)">
                              <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnfind">
                                 <table  style="width:100%">
                                     <tr>
                                         <td>Find</td>
                                         <td><asp:TextBox ID="txtfind" runat="server" placeholder="Description/Category"></asp:TextBox><asp:LinkButton ID="lbtnfind" runat="server" CssClass="fa fa-search buttonC" OnClick="lbtnfind_Click"  ></asp:LinkButton></td>
                                          <td style="text-align:left"><asp:CheckBox ID="chkZeroOnly" runat="server" Text="Limit to Sage Zero Balances ONLY" AutoPostBack="true" OnCheckedChanged="chkzero_CheckedChanged" style="font-size:0.8em; width:15em" /></td>
                                     </tr>
                                     <tr>
                                         <td colspan="2"><asp:CheckBox ID="chkzero" runat="server" Text="Show Zero Balances" AutoPostBack="true" OnCheckedChanged="chkzero_CheckedChanged" style="font-size:0.8em; width:15em" /></td>
                                        <td colspan="2" style="text-align:left"><asp:CheckBox ID="chkDiscrep" runat="server" Text="Show Mis-matched Only" AutoPostBack="true" OnCheckedChanged="chkzero_CheckedChanged" style="font-size:0.8em" /></td>
                                    </tr>
                                 </table>      
                              </asp:Panel>
                               <asp:GridView ID="GridItems" runat="server" AutoGenerateColumns="false" CssClass="gridview" AllowSorting="true" OnSorting="GridItems_Sorting" OnRowDataBound="GridItems_RowDataBound">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <Columns>
                                        <asp:BoundField HeaderText="Code" DataField="Code" ReadOnly="True" SortExpression="Code" />
                                        <asp:BoundField HeaderText="Description" DataField="Description" ReadOnly="True" SortExpression="Description" />
                                        <asp:BoundField HeaderText="Category" DataField="CategoryDescript" ReadOnly="True" SortExpression="CategoryDescript" />
                                        <asp:BoundField HeaderText="On Hand (Sage)" DataField="QuantityOnHand" ReadOnly="True" ItemStyle-Width="6em"  HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" DataFormatString="{0:N2}" SortExpression="QuantityOnHand"/>
                                        <asp:BoundField HeaderText="On Hand (Data Fusion)" DataField="TotQOH_MDF" ReadOnly="True" ItemStyle-Width="8em"  HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" DataFormatString="{0:N2}" />
                                        <asp:TemplateField HeaderText="Select" ItemStyle-Width="6em" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center">
                                            <HeaderTemplate>
                                                 Select All <br /> <asp:CheckBox ID="chkSelectAll" runat="server" AutoPostBack="true" OnCheckedChanged="chkSelectAll_CheckedChanged"/>
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkSelect" runat="server" Checked="false" Text=" " />
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                   </div> 
                    <div class="6u 12u$(medium)" style="text-align:center">
                        <h2>Move to Store</h2>
                        <asp:DropDownList ID="DDStoreTo" runat="server" Width="200px"></asp:DropDownList><br /><br />
                        <asp:LinkButton ID="lbtnUpdateYes" runat="server" OnClick="lbtnUpdateYes_Click" CssClass="fa fa-sync-alt buttonRed"> Update Opening Balances Now</asp:LinkButton>
                        <cci:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" ConfirmText="Update Item Opening Balances in Selected Store - WARNING ** - This action will remove all previous records of this item and set this quantity as an opening balance?" Enabled="True" TargetControlID="lbtnUpdateYes"></cci:ConfirmButtonExtender>
                    </div>    
                 </div>
                         </ContentTemplate>
                      <Triggers>
                                <asp:PostBackTrigger ControlID="lbtnUpdateYes" />
                            </Triggers>
                     </asp:UpdatePanel>
                    </div>  
                        <section id="footer" class="wrapper">
                            <asp:LinkButton ID="lbtmDLUpdate" runat="server"  OnClick="lbtmDLUpdate_Click"><></asp:LinkButton>
                            <cci:ConfirmButtonExtender ID="ConfirmButtonExtender3" runat="server" ConfirmText="Delete All Stock, are you sure? Warning - This is irreversable !!"  Enabled="True" TargetControlID="lbtmDLUpdate"></cci:ConfirmButtonExtender>
                    </section>
        </div>
    </form>
     <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</body>
</html>
