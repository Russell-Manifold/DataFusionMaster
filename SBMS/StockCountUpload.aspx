<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="StockCountUpload.aspx.cs" Inherits="SBMS.StockCountUpload" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Count Upload</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <%--<link rel="stylesheet" href="assets/css/main.css" />--%>
    <link rel="stylesheet" href="prologue/assets/css/main.css" />
</head>
<body>
    <form id="form1" runat="server" enctype="multipart/form-data">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div id="header">
            <div class="top">
                <!-- Logo -->
                <div id="logo">
                    <asp:Label ID="lblUsername" runat="server" Text="" Font-Size="Small" Style="padding-top: 0em; float: left"></asp:Label>
                    <asp:LinkButton ID="lbtnLogOut" runat="server" Style="font-size: 0.9em; float: right" OnClick="lbtnLogOut_Click"> Log Out</asp:LinkButton>
                </div>
                <!-- Nav -->
                <nav id="nav">
                    <ul>
                        <li>
                            <asp:LinkButton ID="imgdash" runat="server" CssClass="buttonM" OnClick="imgdash_Click" ToolTip="Return to main dashboard">Dashboard</asp:LinkButton></li>
                        <li>
                            <asp:LinkButton ID="imgbRec" runat="server" CssClass="buttonM" OnClick="imgbRec_Click" ToolTip="Receive from Purchase Orders, allocate lot numbers">Purchase Orders</asp:LinkButton></li>
                        <li>
                            <asp:LinkButton ID="ibtnPickSlips" runat="server" CssClass="buttonM" OnClick="ibtnPickSlips_Click" ToolTip="View Sales Orders and picking slips, fulfill orders">Sales Orders</asp:LinkButton></li>
                        <li>
                            <asp:LinkButton ID="ibtnPickTrack" runat="server" CssClass="buttonM" OnClick="ibtnPickTrack_Click" ToolTip="Track all picking slips in a simple drag and drop process ">Picking Slip Tracking</asp:LinkButton></li>
                        <li>
                            <asp:LinkButton ID="ibtnStckCtl" runat="server" CssClass="buttonM" ToolTip="Inter Store Transfers, Stock adjustments, Stock Takes, Reporting" OnClick="ibtnStckCtl_Click">Stock Control</asp:LinkButton></li>
                        <li>______________</li>

                        <li>
                            <asp:LinkButton ID="ibtnFCasts" runat="server" CssClass="buttonM" OnClick="ibtnFCasts_Click" ToolTip="Ensure greater stock accuracy with sales forecasting"> Sales Forecasts</asp:LinkButton></li>
                        <li>
                            <asp:LinkButton ID="ibtnmrp" runat="server" CssClass="buttonM" OnClick="ibtnmrp_Click" ToolTip="View finished good demands based on PO's, Sales Orders, Forecasts etc"> Finished Goods Demands</asp:LinkButton></li>
                        <li>
                            <asp:LinkButton ID="ibtnJobTrack" runat="server" CssClass="buttonM" OnClick="ibtnJobTrack_Click" ToolTip="Track all Job cards, simply drag and drop their change in status"> Job Card Tracking</asp:LinkButton></li>
                        <li>______________</li>
                        <li>
                            <asp:LinkButton ID="ibtmWorksOrders" runat="server" CssClass="buttonM" OnClick="ibtmWorksOrders_Click" ToolTip="Create and view works orders for manufacturing or production"> Works Orders</asp:LinkButton></li>
                        <li>
                            <asp:LinkButton ID="ibtmWOrdMgment" runat="server" CssClass="buttonM" OnClick="ibtmWOrdMgment_Click" ToolTip="Allocate raw materials to works orders and update stock levels on order completion."> Works Order Fulfillment</asp:LinkButton></li>
                        <li>
                            <asp:LinkButton ID="ibtnRMD" runat="server" CssClass="buttonM" OnClick="ibtnRMD_Click" ToolTip="View RMD based on PO's, Sales orders, Works Orders."> Raw Materials Demands</asp:LinkButton></li>
                        <li>______________</li>
                        <li><a href="https://mydatafusion.online/learningCenter.aspx" class="buttonM icon fa-lightbulb" target="_blank">Learning Hub >></a></li>
                    </ul>
                </nav>
            </div>
        </div>
        <div id="main">
            <div class="content">
                <div class="container">
                    <div class="row 150%">
                        <div class="col-12 col-12-wide" style="text-align: center">
                            <a href="https://mydatafusion.online" title="My Data Fusion website">
                                <img src="images/Logo.png" style="border-radius: 0.25em; float: left" class="logoImg" /></a>
                             <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC fa fa-home" onclick="lbtnHome_Click" style="float:left">&nbsp;</asp:LinkButton>
                            <asp:LinkButton ID="lbtnBack" runat="server" class="buttonC fa fa-arrow-left" style="float:left" onclick="lbtnBack_Click">&nbsp;Stock Counts</asp:LinkButton>
                            <asp:Image ID="imgCoImg" runat="server" Style="float: right" class="logoImg" />
                            <h3 style="padding-top: 1.5em; line-height: 1em">Upload Count Workbook</h3>
                            <h4>
                                <asp:Label ID="lblerr" runat="server" Text=" " ForeColor="Red">&nbsp;</asp:Label></h4>
                        </div>
                    </div>
                    <div class="row 150%">
                        <div class="col-2 col-12-normal">
                        </div>
                        <div class="col-10 col-12-normal" style="text-align: center">
                            <div style="width: 98%; overflow: auto">
                                <div style="border: 2px dashed #aaa; border-radius: 0.5em; padding: 3em 2em; margin: 2em auto; max-width: 600px; background: #fafafa; text-align: center;">
                                    <p style="margin: 0 0 1em 0; font-size: 1.1em; color: #666;">Select your completed count sheet</p>
                                    <asp:FileUpload ID="fileUpload" runat="server" accept=".xlsx" />
                                    <br />
                                    <br />
                                    <asp:Button ID="btnProcess" runat="server" Text="Upload & Process"
                                        OnClick="btnProcess_Click" CssClass="buttonIndex" />
                                </div>

                                <asp:Panel ID="pnlResults" runat="server" Visible="false"
                                    Style="max-width: 700px; margin: 1em auto; text-align: left">
                                    <h4>Upload Results</h4>
                                    <asp:Label ID="lblCountInfo" runat="server"
                                        Style="display: block; margin-bottom: 0.5em; font-weight: bold"></asp:Label>
                                    <asp:Label ID="lblUploadResult" runat="server"></asp:Label>
                                </asp:Panel>
                                
                             
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        </form>
</body>
</html>
