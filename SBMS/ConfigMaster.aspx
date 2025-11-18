<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ConfigMaster.aspx.cs" Inherits="SBMS.ConfigMaster" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Settings</title>
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
                          <asp:LinkButton ID="LbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click" >&nbsp;&nbsp</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton>
                        <br />
                        <h2 style="padding-top:0; line-height:1em">Settings, Configuration and Master Files</h2>                       
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
                      <div class="4u 12u$(medium)">&nbsp;</div>
                     <div class="4u 12u$(medium)" style="text-align:center">
                             <h4>Master Files Management</h4>
                            <ul>
                                <li><asp:LinkButton ID="lbtnItems" runat="server" CssClass="buttonRed" style="width:100%" ToolTip="Add/Edit Item additional details" PostBackUrl="~/ItemsHeaders.aspx" >Items</asp:LinkButton></li>
                                <li><asp:LinkButton ID="lbtnStores" runat="server" CssClass="buttonRed" style="width:100%" PostBackUrl="~/StoresMaster.aspx" ToolTip="Add/Edit Stores" >Multi Stores</asp:LinkButton></li>
                                <li><asp:LinkButton ID="lbtnItemStore" runat="server" CssClass="buttonRed" style="width:100%" PostBackUrl="~/ItemStoresLink.aspx" ToolTip="Bulk link items to stores">Items<-->Stores Link</asp:LinkButton></li>
                                <li><asp:LinkButton ID="lbtnPickProc" runat="server" CssClass="buttonRed" style="width:100%" ToolTip="Add/Edit Picking, Job Card and Production Processes" PostBackUrl="~/ConfigProcesses.aspx" >Internal Processes</asp:LinkButton></li>
                                <li><asp:LinkButton ID="lbtnRoles" runat="server" CssClass="buttonRed" style="width:100%" PostBackUrl="~/ConfigRoles.aspx" ToolTip="Add/Edit Roles master file">Roles (Job Descriptions)</asp:LinkButton></li>
                                <li><asp:LinkButton ID="lbtnUsers" runat="server" CssClass="buttonRed" style="width:100%" PostBackUrl="~/ConfigUsers.aspx" ToolTip="Add/Edit Users, linked roles etc">Users</asp:LinkButton></li>
                                <li><asp:LinkButton ID="lbtnDeliv" runat="server" CssClass="buttonRed" style="width:100%" PostBackUrl="~/ConfigDelivery.aspx" ToolTip="Add/Edit Modes of delivery">Delivery Methods</asp:LinkButton></li>
                                <li><asp:LinkButton ID="lbtnAccts" runat="server" CssClass="buttonRed" style="width:100%" PostBackUrl="~/ConfigAccounts.aspx" ToolTip="Select GL Accounts which can be used for Sales Orders, Picking slips and Job Cards">GL Account Access</asp:LinkButton></li>
                                <li><asp:LinkButton ID="lbtnCompany" runat="server" CssClass="buttonRed" style="width:100%" PostBackUrl="~/ConfigCompany.aspx" ToolTip="Configure Company Master Details">Company Master</asp:LinkButton></li>
                            </ul>                     
                            <hr />
                         <ul>
                             <li><asp:LinkButton ID="lbtnBundle" runat="server" CssClass="buttonRed" style="width:100%" PostBackUrl="~/BundleHeaders.aspx" ToolTip ="View Sage Accounting Bundles" >View Bundles</asp:LinkButton></li>
                             <li><asp:LinkButton ID="lbtnBOM" runat="server" CssClass="buttonRed" style="width:100%" PostBackUrl="~/BOMHeaders.aspx" ToolTip ="Create and Edit Bills Of Materials" >B O M's</asp:LinkButton></li>
                             <li><asp:LinkButton ID="lbtnKit" runat="server" CssClass="buttonRed" style="width:100%" PostBackUrl="~/KitHeaders.aspx" ToolTip ="Create and Edit Kits" >Kits</asp:LinkButton></li>  
                             <li><asp:LinkButton ID="lbtnOpenBals" runat="server" CssClass="buttonRed" style="width:100%" PostBackUrl="~/ItemQOHSync.aspx" ToolTip="Import opening balances to specific stores">Import Opening Balances From Sage To Stores</asp:LinkButton></li>
                         </ul>   
                            <hr />
                            <%--<asp:LinkButton ID="lbtnSync" runat="server" CssClass="buttonRed" PostBackUrl="~/MigrateAndUpload.aspx" ToolTip="Upload data from Sage 50C into My Data Fustion" BackColor="LightBlue">Migrate From Sage 50C</asp:LinkButton>--%>
                            </div>
                       <div class="4u 12u$(medium)">&nbsp;</div>
                     </div>
                     </ContentTemplate>
                 </asp:UpdatePanel>
                </div>
        </div>
    </form>
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</body>
</html>
