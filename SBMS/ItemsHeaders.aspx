<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="ItemsHeaders.aspx.cs" Inherits="SBMS.ItemsHeaders" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Items</title>
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
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="lbtndwnload" runat="server" class="buttonC icon fa-download" OnClick="lbtndwnload_Click" style="float:right;margin-bottom:.5em" ToolTip="Download to excel">&nbsp;Excel</asp:LinkButton>
                        <br />
                        <h3 style="padding-top:0; line-height:1em">Items Master List</h3>
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
                <div class="row 150%">
                     <div class="1u 12u$(medium)">&nbsp;</div>
                    <div class="10u 12u$(medium)">
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
                                <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnfind">
                                   Find Item <asp:TextBox ID="txtfind" runat="server" style="margin-bottom:.5em; width:20em" placeholder="Code/Description/Category"></asp:TextBox><asp:LinkButton ID="lbtnfind" runat="server" CssClass="fa fa-search buttonC" OnClick="lbtnfind_Click"></asp:LinkButton>
                                    <asp:Label ID="lblReccount" runat="server" Text="" style="padding-left:3em"></asp:Label>  
                                    <asp:CheckBox ID="chkZero" runat="server" Text="Hide_Zero_On_Hand" style="float:right; font-size:small;" AutoPostBack="true" OnCheckedChanged="chkZero_CheckedChanged" Checked="true" />
                                   <asp:CheckBox ID="chkService" runat="server" Text="Show_Service_Items" style="float:right; font-size:small" AutoPostBack="true" OnCheckedChanged="chkZero_CheckedChanged" Checked="false" />
                                </asp:Panel> 
                            <asp:GridView ID="GridItems" runat="server" AutoGenerateColumns="false" CssClass="gridview" AllowSorting="true" OnSorting="GridItems_Sorting" OnRowDataBound="GridItems_RowDataBound" AllowPaging="true" PageSize="100" OnPageIndexChanging="GridItems_PageIndexChanging"  >
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                 <Columns>
                                        <asp:TemplateField HeaderText="Item Code" ItemStyle-Width="10em" SortExpression="Code">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="lbtnBOM" CommandArgument='<%# Eval("ID") %>' CommandName="lbtnBOM" runat="server"
                                                    Text='<%# Eval("Code") %>' ToolTip="View Item" style="color:#4A82AB; font-weight:600; border:1px solid #4A82AB; width:8em; min-width:8em; max-width:10em"
                                                    OnClick="lbtnBOM_Click"></asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Description" DataField="Description" ReadOnly="True"  SortExpression="Description"/>
                                        <asp:BoundField HeaderText="On Hand (Sage)" DataField="QuantityOnHand" ReadOnly="True" ItemStyle-Width="6em"  HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" DataFormatString="{0:N2}" SortExpression="QuantityOnHand" />
                                        <asp:BoundField HeaderText="On Hand" DataField="TotQOH_MDF" ReadOnly="True" ItemStyle-Width="6em"  HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" DataFormatString="{0:N2}" SortExpression="TotQOH_MDF" />
                                        <asp:BoundField HeaderText="Qty Picked" DataField="TotQPicked" ReadOnly="True" ItemStyle-Width="6em"  HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" DataFormatString="{0:N2}" SortExpression="TotQPicked" />   
                                        <asp:BoundField HeaderText="Qty On Job Cards" DataField="TotQJCard" ReadOnly="True" ItemStyle-Width="6em"  HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" DataFormatString="{0:N2}" SortExpression="TotQPicked" />   
                                      <asp:BoundField HeaderText="Min Level" DataField="ReorderLevel" ReadOnly="True" ItemStyle-Width="6em"  HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" DataFormatString="{0:N2}" SortExpression="ReorderLevel" />
                                     <asp:TemplateField HeaderText="Is Physical" ItemStyle-Width="6em" HeaderStyle-HorizontalAlign="Center"
                                            ItemStyle-HorizontalAlign="Center">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkIsPhysical" runat="server" Checked='<%# Eval("Physical") %>' Text=" " Enabled="false" />
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                          <asp:TemplateField HeaderText="Lot Tracked" ItemStyle-Width="6em" HeaderStyle-HorizontalAlign="Center"
                                               ItemStyle-HorizontalAlign="Center">
                                               <ItemTemplate>
                                                   <asp:CheckBox ID="chkIsLotTracked" runat="server" Checked='<%# Eval("IsLotTracked") %>' Text=" " Enabled="false" />
                                               </ItemTemplate>
                                           </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Finished Item" ItemStyle-Width="6em" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" SortExpression="IsFinishedGoods">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkIsFG" runat="server" Checked='<%# Eval("IsFinishedGoods") %>' Text=" " Enabled="false" />
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="From BOM" ItemStyle-Width="6em" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" SortExpression="IsFromBOM">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkIsFromBOM" runat="server" Checked='<%# Eval("IsFromBOM") %>' Text=" " AutoPostBack="true" OnCheckedChanged="chkIsFromBOM_CheckedChanged"/>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="From KIT" ItemStyle-Width="6em" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" SortExpression="IsFromKit">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkIsFromKit" runat="server" Checked='<%# Eval("IsFromKit") %>' Text=" " AutoPostBack="true" OnCheckedChanged="chkIsFromKit_CheckedChanged" />
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="BOM Component" ItemStyle-Width="6em" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" SortExpression="IsBOMComponent">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkIsBom" runat="server" Checked='<%# Eval("IsBOMComponent") %>' Text=" " AutoPostBack="true" OnCheckedChanged="chkIsBom_CheckedChanged" />
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Kit Component" ItemStyle-Width="6em" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" SortExpression="IsKitComponent">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkIskit" runat="server" Checked='<%# Eval("IsKitComponent") %>' Text=" " AutoPostBack="true" OnCheckedChanged="chkIskit_CheckedChanged" />
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                            </ContentTemplate>
                            <Triggers>
                                <asp:PostBackTrigger ControlID="lbtndwnload" />
                            </Triggers>
                        </asp:UpdatePanel>
                         </ div> 
                    <div class="1u 12u$(medium)">&nbsp;</div>    
                    </div>
                </div>
        </div>
    </form>
</body>
</html>
