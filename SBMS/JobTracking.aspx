<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="JobTracking.aspx.cs" Inherits="SBMS.JobTracking" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Job Card Tracking</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <!-- CSS styles -->
    <%--<link rel="stylesheet" href="assets/css/main.css" />--%>
     <link rel="stylesheet" href="prologue/assets/css/main.css" />
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js"></script>      
    <script type="text/javascript" src="js/kanbanscript.js"></script>
</head>
<body>
    <form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true"></asp:ScriptManager>
         <div id="header" >
     <div class="top">
         <!-- Logo -->
	            <div id="logo" >
		            <asp:Label ID="lblUsername" runat="server" Text="" Font-Size="Small" style="padding-top:0em; float:left"></asp:Label>
                    <asp:LinkButton ID="lbtnLogOut" runat="server" style="font-size:0.9em; float:right" OnClick="lbtnLogOut_Click"  > Log Out</asp:LinkButton>
	            </div>

         <!-- Nav -->
	            <nav id="nav" >
		            <ul >
                        <li><asp:LinkButton ID="imgdash" runat="server" CssClass="buttonM" OnClick="imgdash_Click" ToolTip="Return to main dashboard">Dashboard</asp:LinkButton></li>
                        <li><asp:LinkButton ID="imgbRec" runat="server" CssClass="buttonM" OnClick="imgbRec_Click" ToolTip="Receive from Purchase Orders, allocate lot numbers">Purchase Orders</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnPickSlips" runat="server" CssClass="buttonM" OnClick="ibtnPickSlips_Click" ToolTip="View Sales Orders and picking slips, fulfill orders" >Sales Orders</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnPickTrack" runat="server" CssClass="buttonM" OnClick="ibtnPickTrack_Click" ToolTip="Track all picking slips in a simple drag and drop process " >Picking Slip Tracking</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnStckCtl" runat="server" CssClass="buttonM" ToolTip="Inter Store Transfers, Stock adjustments, Stock Takes, Reporting" OnClick="ibtnStckCtl_Click" >Stock Control</asp:LinkButton></li>
                        <li>______________</li>
	       
                        <li><asp:LinkButton ID="ibtnFCasts" runat="server" CssClass="buttonM" OnClick="ibtnFCasts_Click" ToolTip="Ensure greater stock accuracy with sales forecasting"> Sales Forecasts</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnmrp" runat="server" CssClass="buttonM" OnClick="ibtnmrp_Click" ToolTip="View finished good demands based on PO's, Sales Orders, Forecasts etc" > Finished Goods Demands</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnJobTrack" runat="server" CssClass="buttonM" OnClick="ibtnJobTrack_Click" ToolTip="Track all Job cards, simply drag and drop their change in status"> Job Card Tracking</asp:LinkButton></li>
                        <li>______________</li>
                        <li><asp:LinkButton ID="ibtmWorksOrders" runat="server" CssClass="buttonM" OnClick="ibtmWorksOrders_Click" ToolTip="Create and view works orders for manufacturing or production" > Works Orders</asp:LinkButton></li>		
                        <li><asp:LinkButton ID="ibtmWOrdMgment" runat="server" CssClass="buttonM" OnClick="ibtmWOrdMgment_Click" ToolTip="Allocate raw materials to works orders and update stock levels on order completion." > Works Order Fulfillment</asp:LinkButton></li>		
                        <li><asp:LinkButton ID="ibtnRMD" runat="server" CssClass="buttonM" OnClick="ibtnRMD_Click" ToolTip="View RMD based on PO's, Sales orders, Works Orders." > Raw Materials Demands</asp:LinkButton></li>
                        <li>______________</li>
                            <li><a href="https://mydatafusion.online/learningCenter.aspx" class="buttonM icon fa-lightbulb" target="_blank"> Learning Hub >></a></li>
		            </ul>
	            </nav>
     </div>
 </div>
<div id="main">  
        <div class="content">
            <div class="container">
                  <div class="row 150%">
                       <div class="col-12 col-12-wide" style="text-align:center">
                           <a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/Logo.png" style="border-radius:0.25em; float:left" class="logoImg" /></a>
                             <asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" />
                             <h3 style="padding-top:2em; line-height:1em">Job Card Tracking</h3>
                         </div>
                      </div>
                  <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div class="row 150%">
                            <div class="col-12 col-12-wide" style="display: flex; justify-content: center;">
                                <div>
                                    <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnfind" style="text-align:center">
                                    <asp:TextBox ID="txtfind" runat="server" placeholder="Find Job Card Or Customer" style="text-align:center; width:17em; margin-bottom:1em"></asp:TextBox><asp:LinkButton ID="lbtnfind" runat="server" CssClass="fa fa-search buttonC" OnClick="lbtnfind_Click"></asp:LinkButton>
                                    <asp:TextBox ID="txtDueDt" runat="server" placeholder="Due Date" style="text-align:center; width:17em; margin-bottom:1em"></asp:TextBox><asp:LinkButton ID="LinkButton1" runat="server" CssClass="fa fa-search buttonC" OnClick="lbtnfind_Click"></asp:LinkButton>
                                         <cci:CalendarExtender ID="CalendarExtender1" runat="server" Enabled="True" TargetControlID="txtDueDt" Format="dd MMM yyyy"></cci:CalendarExtender>
                                    </asp:Panel>
                                   <%-- <span style="text-align:left;"> From Process 1 ==> a WIP process : RM's removed from RM store and placed into WIP.</span>
                                    <span style="float:right">From WIP Process ==> Ready To Invoice : RM's removed from WIP + FG added to Co Store.</span>--%>
                                    <div id="kanbanboard" runat="server" class="kanbanboard" style="font-size:0.8em">
                                        <!-- Job columns will be dynamically added here -->
                                    </div>
                                </div>
                            </div>
                        </div>
                         </ContentTemplate>
                </asp:UpdatePanel>
                        <asp:Button ID="btnShowModal" runat="server" Text="Show Modal" Style="display: none;" />
                        <!-- Modal HTML -->
                            <div class="modal fade" id="quantityModal" tabindex="-1" role="dialog" aria-labelledby="quantityModalLabel" aria-hidden="true">
                                <div class="modal-dialog" role="document">
                                    <div class="modal-content">
                                        <div class="modal-header">
                                           <h4 class="modal-title" id="quantityModalHLabel">Move Items To New Workflow Process</h4>
                                           </div>
                                        <div class="modal-body">
                                            <asp:Panel ID="panelModal" runat="server" DefaultButton="btnSaveQuantity">
                                            <h5 class="modal-title" id="quantityModalLabel">Approved Quantity Sent</h5>
                                            <asp:TextBox ID="txtQuantity" runat="server" CssClass="form-control" placeholder="Approved" style="text-align:center; width:6em"></asp:TextBox>
                                            <h5 class="modal-title" id="quantityModalRLabel">Number of Rejects</h5>
                                            <asp:TextBox ID="txtRejQuantity" runat="server" CssClass="form-control" placeholder="Rejects" style="text-align:center;  width:6em"></asp:TextBox>
                                             <asp:HiddenField ID="hiddenJobId" runat="server" ClientIDMode="Static" />
                                             <asp:HiddenField ID="newWsID" runat="server" ClientIDMode="Static" />
                                                </asp:Panel>
                                        </div>
                                        <div class="modal-footer">
                                            <asp:LinkButton ID="btnSaveQuantity" runat="server" Text=" Save" CssClass="icon fa-save buttonSage" OnClick="btnSaveQuantity_Click"  />
                                            <asp:LinkButton ID="lbtnCancel" runat="server" class="buttonTransparent icon fa-times" OnClick="lbtnCancel_Click"> Cancel</asp:LinkButton>
                                        </div>
                                    </div>
                                </div>
                   </div>
            </div>
        </div>
    </div>
    </form>
     <!-- Place the script before the closing body tag -->
<script type="text/javascript">
    var basePath = "<%= ResolveUrl("~/") %>";  // Server-side variable
</script>

<script type="text/javascript">
    function openJobCard(Psid) {
        window.location.href = basePath + 'JobCard.aspx?docid=' + encodeURIComponent(Psid);
        return false;
    }
</script>
</body>
</html>
