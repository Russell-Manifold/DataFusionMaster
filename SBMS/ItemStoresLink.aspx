<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ItemStoresLink.aspx.cs" Inherits="SBMS.ItemStoresLink" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>ItemStoresLink</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
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
                        <h3 style="padding-top:0; line-height:1em">Items Master List <br />
                        <span style="font-size:.6em">The top 1000 rows are returned. Use the filter feature to define your search.</span></h3>
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
                <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel2">
          <ProgressTemplate>
              <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
              <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." style="padding: 10px; padding-top:15%; border-radius:1.5em" />
              </div>
            </ProgressTemplate>
      </asp:UpdateProgress> 
            <asp:UpdatePanel ID="UpdatePanel2" runat="server">
                <ContentTemplate>    
                        <div class="row 150%">
                         <div class="1u 12u$(medium)" style="text-align:center">&nbsp;</div>
                            <div class="4u 12u$(medium)">
                                <div style="display:none">
                                <asp:Label ID="lblDir" runat="server" Text=""></asp:Label>
                                </div> 
                               <h3>Unlinked Items</h3>
                                <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnfind">
                                   Find Item <asp:TextBox ID="txtfind" runat="server" style="margin-bottom:.5em" placeholder="Code/Description/Category"></asp:TextBox><asp:LinkButton ID="lbtnfind" runat="server" CssClass="fa fa-search buttonC" OnClick="lbtnfind_Click"></asp:LinkButton>
                                </asp:Panel> 
                                <asp:GridView ID="GridItems" runat="server" AutoGenerateColumns="false" CssClass="gridview" AllowSorting="true" OnSorting="GridItems_Sorting" ShowFooter="True">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                 <Columns>
                                        <asp:BoundField DataField="ID" ReadOnly="True" ItemStyle-Font-Size="0.01em" ItemStyle-ForeColor="White"/>
                                        <asp:BoundField HeaderText="Category" DataField="CategoryDescript" ReadOnly="True"  SortExpression="CategoryDescript"/>
                                        <asp:BoundField HeaderText="Code" DataField="Code" ReadOnly="True"  SortExpression="Code"/>
                                        <asp:BoundField HeaderText="Description" DataField="Description" ReadOnly="True"  SortExpression="Description"/>       
                                     <asp:TemplateField>
                                         <HeaderTemplate>
                                                 Select All <br /> <asp:CheckBox ID="chkSelectAll" runat="server" AutoPostBack="true" OnCheckedChanged="chkSelectAll_CheckedChanged"/>
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkSelect" runat="server" Checked="false" Text=" "/>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                              </div> 
                           <div class="2u 12u$(medium)" style="text-align:center">
                               <h3>Link To Store</h3>
                               <asp:DropDownList ID="DDStoreTo" runat="server" Width="200px" AutoPostBack="true" OnSelectedIndexChanged="DDStoreTo_SelectedIndexChanged"></asp:DropDownList><br /><br />
                               <asp:LinkButton ID="lbtnUpdateYes" runat="server" OnClick="lbtnUpdateYes_Click" CssClass="fa fa-sync-alt buttonRed"> Save Link</asp:LinkButton>
                           </div>   
                            <div class="4u 12u$(medium)" style="text-align:center">
                                <h3>Linked Items</h3><br /><br />
                                <asp:GridView ID="GridLinked" runat="server" AutoGenerateColumns="false" CssClass="gridview" AllowSorting="true" OnSorting="GridItems_Sorting" ShowFooter="True">
                                    <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <PagerStyle CssClass="gridViewPager" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                 <Columns>
                                     <asp:BoundField HeaderText="Category" DataField="CategoryDescript" ReadOnly="True" />
                                        <asp:BoundField HeaderText="Code" DataField="Code" ReadOnly="True"/>
                                        <asp:BoundField HeaderText="Description" DataField="Description" ReadOnly="True" />       
                                    </Columns>
                                </asp:GridView>
                                </div>
                              <div class="1u 12u$(medium)" style="text-align:center">&nbsp;</div>
                      </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>
        </div>
    </form>
</body>
</html>
