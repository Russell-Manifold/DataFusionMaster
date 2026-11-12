<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="FGDemands.aspx.cs" Inherits="SBMS.FGDemands" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Item Demands</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <meta name="viewport" content="width=device-width, initial-scale=1"/>
        <%--<link href="assets/css/main.css" rel="stylesheet" />--%>
       <link rel="stylesheet" href="prologue/assets/css/main.css" />
        <noscript><link rel="stylesheet" href="assets/css/noscript.css" /></noscript>
        <script  type="text/javascript" src="lib/jquery/dist/jquery.js"></script>
        <script  type="text/javascript" src="lib/jquery/dist/jquery.min.js"></script>
        <script  type="text/javascript" src="lib/jqueryui/jquery-ui.min.js"></script>
        <link rel="stylesheet" href="prologue/assets/css/timeline.css" />
  </head>
<body>
    <form id="form1" runat="server">
      <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
          	<div id="header" >
       <div class="top">
           <!-- Logo -->
            <div id="logo" >
	            <asp:Label ID="lblUsername" runat="server" Text="" Font-Size="Small" style="padding-top:0em; float:left"></asp:Label>   
                   <asp:LinkButton ID="lbtnLogOut" runat="server" style="font-size:0.9em; float:right" OnClick="lbtnLogOut_Click" CssClass="icon fa-door-open"  > Log Out</asp:LinkButton>         
            </div>
           <!-- Nav -->
            <nav id="nav" >
	            <ul>
                    <li><asp:LinkButton ID="imgdash" runat="server" CssClass="buttonM" OnClick="imgdash_Click" ToolTip="Return to main dashboard">Dashboard</asp:LinkButton></li>  
                    <li><asp:LinkButton ID="imgbRec" runat="server" CssClass="buttonM" OnClick="imgbRec_Click" ToolTip="Receive from Purchase Orders, allocate lot numbers">Purchase Orders</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnPickSlips" runat="server" CssClass="buttonM" OnClick="ibtnPickSlips_Click" ToolTip="View Sales Orders and picking slips, fulfill orders" >Sales Orders</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnPickTrack" runat="server" CssClass="buttonM" OnClick="ibtnPickTrack_Click" ToolTip="Track all picking slips in a simple drag and drop process " >Picking Slip Tracking</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnStckCtl" runat="server" CssClass="buttonM" ToolTip="Inter Store Transfers, Stock adjustments, Stock Takes, Reporting" OnClick="ibtnStckCtl_Click" >Stock Control</asp:LinkButton></li>
                        <li>______________</li>
	       
                        <li><asp:LinkButton ID="ibtnFCasts" runat="server" CssClass="buttonM" OnClick="ibtnFCasts_Click" ToolTip="Ensure greater stock accuracy with sales forecasting"> Sales Forecasts</asp:LinkButton></li>
                        <asp:LinkButton ID="ibtnmrp" runat="server" CssClass="buttonM" OnClick="ibtnmrp_Click" ToolTip="What will run short, week by week, for finished goods and raw materials" Visible="false"></asp:LinkButton>
                        <li><asp:LinkButton ID="ibtnJobTrack" runat="server" CssClass="buttonM" OnClick="ibtnJobTrack_Click" ToolTip="Track all Job cards, simply drag and drop their change in status"> Job Card Tracking</asp:LinkButton></li>
                        <li>______________</li>
                        <li><asp:LinkButton ID="ibtmWorksOrders" runat="server" CssClass="buttonM" OnClick="ibtmWorksOrders_Click" ToolTip="Create and view works orders for manufacturing or production" > Works Orders</asp:LinkButton></li>		
                        <li><asp:LinkButton ID="ibtmWOrdMgment" runat="server" CssClass="buttonM" OnClick="ibtmWOrdMgment_Click" ToolTip="Allocate raw materials to works orders and update stock levels on order completion." > Works Order Fulfillment</asp:LinkButton></li>		
                        <asp:LinkButton ID="ibtnRMD" runat="server" CssClass="buttonM" OnClick="ibtnRMD_Click" ToolTip="View RMD based on PO's, Sales orders, Works Orders." Visible="false"></asp:LinkButton>
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
                                <h3 style="padding-top:2em; line-height:1em">Item Demands</h3>
                                <p class="fuse-hint">Stock left at the end of each week, after everything due in and out. Click an item to see why.</p>
                            </div>
                            </div>
                        <div class="stl-toolbar">
                            <span id="stlSummary" class="fuse-hint"></span>
                            <span class="stl-spacer"></span>
                            <asp:PlaceHolder ID="phShow" runat="server">
                                <label for="stlShow" style="display:inline; margin:0">Show</label>
                                <select id="stlShow" runat="server" ClientIDMode="Static">
                                    <option value="">All</option>
                                    <option value="g">Finished goods</option>
                                    <option value="f">Made from a BOM</option>
                                    <option value="r">BOM components</option>
                                </select>
                            </asp:PlaceHolder>
                            <label for="stlCat" style="display:inline; margin:0">Category</label>
                            <select id="stlCat" runat="server" ClientIDMode="Static"></select>
                            <input type="checkbox" id="stlShort" runat="server" ClientIDMode="Static" /><label for="stlShort" style="margin:0">Short only</label>
                            <label for="txtUpTo" style="display:inline; margin:0">Up to</label>
                            <asp:TextBox ID="txtUpTo" runat="server" TextMode="Date" AutoPostBack="true" OnTextChanged="txtUpTo_TextChanged" ClientIDMode="Static" ToolTip="Leave out everything due after this date"></asp:TextBox>
                            <input type="text" id="stlFind" runat="server" ClientIDMode="Static" placeholder="Find" aria-label="Find an item" autocomplete="off" />
                            <asp:LinkButton ID="lbtnDownload" runat="server" class="buttonC fa fa-download" OnClick="lbtnDownload_Click" ToolTip="Download every item and its weekly stock to Excel" style="font-size:1em">&nbsp;Download</asp:LinkButton>
                        </div>
                        <asp:Literal ID="litTimeline" runat="server" EnableViewState="false"></asp:Literal>
                       </div>
                    </div>
</div>
    <script type="text/javascript">
        (function () {
            var table = document.getElementById('stlTable');
            var find = document.getElementById('stlFind');
            var show = document.getElementById('stlShow');
            var cat = document.getElementById('stlCat');
            var shortOnly = document.getElementById('stlShort');
            var summary = document.getElementById('stlSummary');
            if (!table) { if (find) find.disabled = true; if (show) show.disabled = true; if (cat) cat.disabled = true; if (shortOnly) shortOnly.disabled = true; return; }

            table.addEventListener('click', function (e) {
                var row = e.target.closest('tr.stl-row');
                if (!row) return;
                e.preventDefault();
                var open = row.classList.toggle('stl-open');
                row.nextElementSibling.hidden = !open;
                row.querySelector('.stl-toggle').setAttribute('aria-expanded', open ? 'true' : 'false');
            });

            // Enter must not submit the form: the list is already filtered as you type.
            find.addEventListener('keydown', function (e) { if (e.key === 'Enter') e.preventDefault(); });
            function applyFilters() {
                var term = find.value.trim().toLowerCase();
                var type = show ? show.value : '';
                var category = cat ? cat.value.trim().toLowerCase() : '';
                var shortCount = 0, lowCount = 0, total = 0;
                table.querySelectorAll('tr.stl-row').forEach(function (row) {
                    var match = row.getAttribute('data-find').indexOf(term) !== -1
                        && row.getAttribute('data-type').indexOf(type) !== -1
                        && (category === '' || row.getAttribute('data-cat') === category)
                        && (!shortOnly || !shortOnly.checked || row.getAttribute('data-attn') === '0');
                    row.hidden = !match;
                    row.nextElementSibling.hidden = !match || !row.classList.contains('stl-open');
                    if (!match) return;
                    total++;
                    var attention = row.getAttribute('data-attn');
                    if (attention === '0') shortCount++;
                    else if (attention === '1') lowCount++;
                });
                summary.innerHTML = '<span class="stl-sum--short">' + shortCount + ' short</span>, <span class="stl-sum--low">' + lowCount
                    + ' below minimum</span>, of ' + total + (total === 1 ? ' item' : ' items');
            }

            find.addEventListener('input', applyFilters);
            if (show) show.addEventListener('change', applyFilters);
            if (cat) cat.addEventListener('change', applyFilters);
            if (shortOnly) shortOnly.addEventListener('change', applyFilters);
            applyFilters();
        })();
    </script>
    </form>
</body>
</html>
