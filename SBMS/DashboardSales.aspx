<%@ Page Language="C#" Async="true" AsyncTimeout="600"  AutoEventWireup="true" CodeBehind="DashboardSales.aspx.cs" Inherits="SBMS.DashboardSales" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<%@ Register Src="~/CommonScripts.ascx" TagPrefix="uc" TagName="CommonScripts" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
             <!-- Google Tag Manager -->
        <script>(function (w, d, s, l, i) {
                w[l] = w[l] || []; w[l].push({
                    'gtm.start':
                        new Date().getTime(), event: 'gtm.js'
                }); var f = d.getElementsByTagName(s)[0],
                    j = d.createElement(s), dl = l != 'dataLayer' ? '&l=' + l : ''; j.async = true; j.src =
                        'https://www.googletagmanager.com/gtm.js?id=' + i + dl; f.parentNode.insertBefore(j, f);
            })(window, document, 'script', 'dataLayer', 'GTM-TMWFZ356');</script>
        <!-- End Google Tag Manager -->
    <title>Insights</title>
    <meta name="description" content="Access your personalized dashboard in My Data Insights to explore business analytics, financial reports, and AI-powered insights from Sage Accounting data." />
    <meta name="keywords" content="SageCloudDIReporting, business dashboard, Sage Accounting, financial reports, management reporting, business insights, data analytics" />
    <meta name="robots" content="index, follow" />
    <link rel="canonical" href="https://mydatainsights.online/za/SageCloudDIReporting.aspx" />
    <link id="Link3" runat="server" rel="shortcut icon" href="images/BusIntelligenceImg.png" type="image/x-icon" />
	<link id="Link4" runat="server" rel="icon" href="images/BusIntelligenceImg.png" type="image/ico" />
    
    <%--<link href="niceadmin/assets/img/favicon.png" rel="icon"/>
<link href="niceadmin/assets/img/apple-touch-icon.png" rel="apple-touch-icon"/>--%>

<!-- Google Fonts -->
<link href="https://fonts.gstatic.com" rel="preconnect"/>
<link href="https://fonts.googleapis.com/css?family=Open+Sans:300,300i,400,400i,600,600i,700,700i|Nunito:300,300i,400,400i,600,600i,700,700i|Poppins:300,300i,400,400i,500,500i,600,600i,700,700i" rel="stylesheet"/>

<!-- Vendor CSS Files -->
<link href="niceadmin/assets/vendor/bootstrap/css/bootstrap.min.css" rel="stylesheet"/>
<link href="niceadmin/assets/vendor/bootstrap-icons/bootstrap-icons.css" rel="stylesheet"/>
<link href="niceadmin/assets/vendor/boxicons/css/boxicons.min.css" rel="stylesheet"/>
<link href="niceadmin/assets/vendor/quill/quill.snow.css" rel="stylesheet"/>
<link href="niceadmin/assets/vendor/quill/quill.bubble.css" rel="stylesheet"/>
<link href="niceadmin/assets/vendor/remixicon/remixicon.css" rel="stylesheet"/>
<link href="niceadmin/assets/vendor/simple-datatables/style.css" rel="stylesheet"/>

<!-- Template Main CSS File -->
<link href="niceadmin/assets/css/style.css" rel="stylesheet"/>

    <script type="application/ld+json">
        {
          "@context": "http://schema.org",
          "@type": "WebPage",
          "name": "SageCloudDIReporting - My Data Insights Dashboard",
          "url": "https://mydatainsights.online/za/SageCloudDIReporting.aspx",
          "description": "Access your personalized dashboard in My Data Insights for real-time business analytics, reporting, and AI-driven insights from Sage Accounting data."
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
                <!-- Google Tag Manager (noscript) -->
        <noscript><iframe src="https://www.googletagmanager.com/ns.html?id=GTM-TMWFZ356"
        height="0" width="0" style="display:none;visibility:hidden"></iframe></noscript>
        <!-- End Google Tag Manager (noscript) -->
      
                <header id="header" class="header fixed-top d-flex align-items-center">
                    <div class="d-flex align-items-center justify-content-between">
                        <a href="https://mydatainsights.online" target="_blank" class="logo d-flex align-items-center">
                            <img src="images/logo2.png" alt="" style="height: 2em" /></a>
                        <i class="bi bi-list toggle-sidebar-btn"></i>
                    </div>
                    <!-- End Logo -->

                    <nav class="header-nav ms-auto">
                        <ul class="d-flex align-items-center">
                            <li class="nav-item d-block d-lg-none">
                                <a class="nav-link nav-icon search-bar-toggle " href="#">
                                    <i class="bi bi-search"></i>
                                </a>
                            </li>
                            <!-- End Search Icon-->
                            <li class="nav-item dropdown pe-3">
                                <a class="nav-link nav-profile d-flex align-items-center pe-0" href="#" data-bs-toggle="dropdown">
                                    <span class="d-none d-md-block dropdown-toggle ps-2">
                                        <asp:Label ID="lblUserName" runat="server" Text=""></asp:Label></span>
                                </a>
                                <!-- End Profile Iamge Icon -->

                                <ul class="dropdown-menu dropdown-menu-end dropdown-menu-arrow profile">
                                    <li class="dropdown-header"></li>
                                    <li>
                                        <a class="dropdown-item d-flex align-items-center" href="#">
                                            <span>
                                                <asp:LinkButton ID="btnLogOut" runat="server" CssClass="bi bi-box-arrow-right">Sign Out</asp:LinkButton></span>
                                        </a>
                                    </li>
                                </ul>
                                <!-- End Profile Dropdown Items -->
                            </li>
                            <!-- End Profile Nav -->
                        </ul>
                    </nav>
                    <!-- End Icons Navigation -->
                </header>
                <!-- End Header -->
                <!-- ======= Sidebar ======= -->
                <aside id="sidebar" class="sidebar">
                    <%--  --%>
                    <ul class="sidebar-nav" id="sidebar-nav">
                       <li class="nav-item">
                            <h5><asp:Label ID="lblCoName" runat="server" Text=""></asp:Label></h5>
                           <hr />
                        </li>
                          <li class="nav-item"><asp:LinkButton ID="imgdash" runat="server" CssClass="buttonM" ToolTip="Return to main dashboard">Dashboard</asp:LinkButton></li>
                        <li class="nav-item"><asp:LinkButton ID="imgbRec" runat="server" CssClass="buttonM" ToolTip="Receive from Purchase Orders, allocate lot numbers">Purchase Orders</asp:LinkButton></li>
                        <li class="nav-item"><asp:LinkButton ID="ibtnPickSlips" runat="server" CssClass="buttonM"  ToolTip="View Sales Orders and picking slips, fulfill orders" >Sales Orders</asp:LinkButton></li>
                        <li class="nav-item"><asp:LinkButton ID="ibtnPickTrack" runat="server" CssClass="buttonM"  ToolTip="Track all picking slips in a simple drag and drop process " >Picking Slip Tracking</asp:LinkButton></li>
                        <li class="nav-item"><asp:LinkButton ID="ibtnStckCtl" runat="server" CssClass="buttonM" ToolTip="Inter Store Transfers, Stock adjustments, Stock Takes, Reporting" >Stock Control</asp:LinkButton></li>
                        <li class="nav-item">______________</li>
	       
                        <li class="nav-item"><asp:LinkButton ID="ibtnFCasts" runat="server" CssClass="buttonM" ToolTip="Ensure greater stock accuracy with sales forecasting"> Sales Forecasts</asp:LinkButton></li>
                        <li class="nav-item"><asp:LinkButton ID="ibtnmrp" runat="server" CssClass="buttonM" ToolTip="View finished good demands based on PO's, Sales Orders, Forecasts etc" > Finished Goods Demands</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnJobTrack" runat="server" CssClass="buttonM"  ToolTip="Track all Job cards, simply drag and drop their change in status"> Job Card Tracking</asp:LinkButton></li>
                        <li class="nav-item">______________</li>
                        <li class="nav-item"><asp:LinkButton ID="ibtmWorksOrders" runat="server" CssClass="buttonM" ToolTip="Create and view works orders for manufacturing or production" > Works Orders</asp:LinkButton></li>		
                        <li class="nav-item"><asp:LinkButton ID="ibtmWOrdMgment" runat="server" CssClass="buttonM" ToolTip="Allocate raw materials to works orders and update stock levels on order completion." > Works Order Fulfilllment</asp:LinkButton></li>		
                        <li class="nav-item"><asp:LinkButton ID="ibtnRMD" runat="server" CssClass="buttonM" ToolTip="View RMD based on PO's, Sales orders, Works Orders." > Raw Materials Demands</asp:LinkButton></li>
                        <li>______________</li>
                            <li><a href="https://mydatafusion.online/learningCenter.aspx" class="buttonM icon fa-lightbulb" target="_blank"> Learning Hub >></a></li>
                    </ul>

                </aside>
                <!-- End Sidebar-->
                <main id="main" class="main">
                       <div class="pagetitle">
                                <nav>
                                <ol class="breadcrumb">
                                    <li class="breadcrumb-item"><a href="Dashboard.aspx" class="bi bi-arrow-left" style="color:blue; font-weight:600">Home</a></li>
                                    <li class="breadcrumb-item active">Sales Dashboard</li>
                                </ol>
                            </nav>        
                    <div class="row">
                                      <div class="col-lg-8" >
                                        <div class="row" style="text-align:center;">
                                            <div class="col-lg-1 col-md-6">                                           
                                                Months<br />
                                                <asp:DropDownList ID="dMths" runat="server" class="form-control" AutoPostBack="true" OnSelectedIndexChanged="dMths_SelectedIndexChanged">
                                                    <asp:ListItem>1</asp:ListItem>
                                                    <asp:ListItem>2</asp:ListItem>
                                                    <asp:ListItem Selected="True">3</asp:ListItem>
                                                    <asp:ListItem>4</asp:ListItem>
                                                    <asp:ListItem>6</asp:ListItem>
                                                    <asp:ListItem>12</asp:ListItem>
                                                    <asp:ListItem>18</asp:ListItem>
                                                    <asp:ListItem>24</asp:ListItem>
                                                    <asp:ListItem>48</asp:ListItem>
                                                    <asp:ListItem>60</asp:ListItem>
                                                </asp:DropDownList>
                                               </div>
                                            <div class="col-lg-3 col-md-6">
                                                Customer<br />
                                                <asp:DropDownList ID="ddCust" runat="server" class="form-control" AutoPostBack="true" OnSelectedIndexChanged="dMths_SelectedIndexChanged">
                                                    
                                                </asp:DropDownList>
                                            </div>
                                            <div class="col-lg-3 col-md-6">
                                                   Sales Rep<br />
                                                    <asp:DropDownList ID="ddRep" runat="server" class="form-control" AutoPostBack="true" OnSelectedIndexChanged="dMths_SelectedIndexChanged">
                                                        
                                                    </asp:DropDownList>
                                                </div>
                                            <div class="col-lg-3 col-md-6">
                                            Category<br />
                                            <asp:DropDownList ID="ddcustCateg" runat="server" class="form-control" AutoPostBack="true" OnSelectedIndexChanged="dMths_SelectedIndexChanged">
                                               
                                            </asp:DropDownList>
                                        </div>
                                             <div class="col-lg-2 col-md-6" >  
                                                 <asp:LinkButton ID="lbtnDownload" runat="server" CssClass="bi bi-download btn btn-primary" style="margin-top:1em"  ToolTip="Download source data to excel" OnClick="lbtnDownload_Click">Download</asp:LinkButton>  
                                             </div>
                                        </div>
                                    </div>
                                </div>
                        </div>
                    <!-- End Page Title -->
                   <section class="section dashboard">
                            <div class="row">
                                <!-- Left side columns -->
                                <div class="col-lg-8">
                                    <div class="row">
                                        <!-- Sales Card -->
                                        <div class="col-xxl-4 col-md-6">
                                            <div class="card info-card sales-card">
                                                 <div class="card-body">
                                                    <h5 class="card-title">Sales Count</h5>
                                                    <div class="d-flex align-items-center">
                                                        <div class="card-icon rounded-circle d-flex align-items-center justify-content-center">
                                                            <i class="bi bi-cart"></i>
                                                        </div>
                                                        <div class="ps-3">
                                                            <h6 id="lblSalesQty" runat="server"></h6>
                                                            <span id="lblSalesPrevQty" runat="server" class="text-success small pt-1 fw-bold" style="color:red"></span><span id="lblSalesDiff" runat="server" class="text-muted small pt-2 ps-1"></span>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                        <!-- End Sales Card -->

                                        <!-- Revenue Card -->
                                        <div class="col-xxl-4 col-md-6">
                                            <div class="card info-card revenue-card">
                                                <div class="card-body">
                                                    <h5 class="card-title">Revenue</h5>
                                                    <div class="d-flex align-items-center">
                                                        <div class="card-icon rounded-circle d-flex align-items-center justify-content-center">
                                                            <i class="bi bi-speedometer"></i>
                                                        </div>
                                                        <div class="ps-3">
                                                            <h6 id="lblSalesRev" runat="server"></h6>
                                                            <span id="lblSalesPrevRev" runat="server" class="text-success small pt-1 fw-bold">8%</span> <span class="text-muted small pt-2 ps-1"></span>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                        <!-- End Revenue Card -->

                                        <!-- Customers Card -->
                                        <div class="col-xxl-4 col-xl-12">
                                            <div class="card info-card sales-card">
                                                <div class="card-body">
                                                    <h5 class="card-title">Customers</h5>
                                                    <div class="d-flex align-items-center">
                                                        <div class="card-icon rounded-circle d-flex align-items-center justify-content-center">
                                                            <i class="bi bi-people"></i>
                                                        </div>
                                                        <div class="ps-3">
                                                            <h6 id="lblCustCount" runat="server"></h6>
                                                            <span id="lblcustCountdiff" runat="server" class="text-danger small pt-1 fw-bold">12%</span> <span class="text-muted small pt-2 ps-1"></span>
                                                        </div>
                                                    </div>
                                                </div>
                                                </div>
                                            </div>                                 
                                        <!-- End Customers Card -->

                                        <!-- Reports -->
                                        <div class="col-12">
                                            <div class="card">
                                                <div class="card-body">
                                                     <div class="filter">
                                                             <a class="icon" href="#" data-bs-toggle="dropdown"><i class="bi bi-three-dots-vertical"></i></a>
                                                             <ul class="dropdown-menu dropdown-menu-end dropdown-menu-arrow">
                                                                 <li class="dropdown-header text-start">
                                                                     <button id="toggleViewBtn" type="button" class="buttonCancel bi bi-three-dots-vertical">Table</button>
                                                                 </li>
                                                             </ul>
                                                         </div>
                                                    <h5 class="card-title">Reports/<span id="lblRepMonths" runat="server"></span></h5>
                                                    <!-- Line Chart -->
                                                    <div id="chartContainer">
                                                        <div id="reportsChart"></div>
                                                     </div>
                                                    <div id="tableContainer" style="display: none;">
                                                        <table id="salesDataTable" class="table">
                                                            <thead>
                                                                <tr>
                                                                    <th>Month</th>
                                                                    <th>Total Sales</th>
                                                                    <th>Total Revenue (K)</th>
                                                                    <th>Customer Count</th>
                                                                </tr>
                                                            </thead>
                                                            <tbody id="salesDataTbody">
                                                                <!-- Data rows will be dynamically added here -->
                                                            </tbody>
                                                        </table>
                                                    </div>

                                                    <!-- End Line Chart -->
                                                </div>
                                            </div>
                                        </div>
                                        <!-- End Reports -->

                                        <!-- Recent Sales -->
                                        <div class="col-12">
                                            <div class="card recent-sales overflow-auto">
                                                 <div class="card-body">
                                                    <h5 class="card-title">Top Customers</h5>
                                                               <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnSearch" style="padding-left:1.75em">
                                                                <asp:LinkButton ID="lbtnClear" runat="server" OnClick="lbtnClear_Click" ToolTip="Clear Filters" Font-Bold="true">&nbsp;X&nbsp;</asp:LinkButton><asp:TextBox ID="txtFind" runat="server" placeholder="Customer"></asp:TextBox><asp:LinkButton ID="lbtnSearch" runat="server" CssClass="bi bi-search" OnClick="lbtnSearch_Click" ToolTip="Search / filter">&nbsp;</asp:LinkButton>
                                                             </asp:Panel>
                                                             <asp:GridView ID="customerSalesGridView" runat="server" CssClass="gridview" AutoGenerateColumns="False" AllowPaging="true" OnPageIndexChanging="customerSalesGridView_PageIndexChanging" PageSize="10" AllowSorting="true" OnSorting="customerSalesGridView_Sorting" OnSelectedIndexChanged="customerSalesGridView_SelectedIndexChanged">
                                                                 <HeaderStyle CssClass="gridViewHeader" />
                                                                 <RowStyle CssClass="gridViewRow" />
                                                                 <AlternatingRowStyle CssClass="gridViewAltRow" />
                                                                 <PagerStyle CssClass="gridViewPager" />
                                                                 <Columns>
                                                                    <asp:TemplateField SortExpression="CustomerName" HeaderText="Customer">
                                                                        <ItemTemplate>
                                                                            <asp:LinkButton ID="lbtnCust" runat="server" CommandArgument='<%# Eval("Customer_ID") %>' Text='<%# Eval("CustomerName") %>' ForeColor="Blue" Font-Bold="true">
                                                                            </asp:LinkButton>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                     <asp:BoundField DataField="NumberOfOrders" HeaderText="# of Invoices" SortExpression="NumberOfOrders" ItemStyle-HorizontalAlign="Center" />
                                                                     <asp:BoundField DataField="TotalOrderValue" HeaderText="Invoices Value" SortExpression="TotalOrderValue" DataFormatString="{0:N2}" HeaderStyle-HorizontalAlign="Right"  ItemStyle-HorizontalAlign="Right" ItemStyle-Width="8em" />
                                                                 </Columns>
                                                             </asp:GridView>
                                                    </div>
                                            </div>
                                        </div>
                                        <!-- End Recent Sales -->

                                        <!-- Top Selling -->
                                        <div class="col-12">
                                            <div class="card top-selling overflow-auto">
                                                  <div class="card-body pb-0">
                                                   <h5 class="card-title">Categories</h5>
                                                   <div id="categoryChart"></div>
                                                </div>
                                            </div>
                                        </div>
                                        <!-- End Top Selling -->

                                    </div>
                                </div>
                                <!-- End Left side columns -->

                                <!-- Right side columns -->
                                <div class="col-lg-4">
                            
                                    <!-- Sales Rep Contributions -->
                                                <div class="card">
                                                    <div class="filter">
                                                        <a class="icon" href="#" data-bs-toggle="dropdown"><i class="bi bi-three-dots-vertical"></i></a>
                                                        <ul class="dropdown-menu dropdown-menu-end dropdown-menu-arrow">
                                                            <li class="dropdown-header text-start">
                                                                <h6>Show</h6>
                                                            </li>
                                                            <li><button id="toggleViewBtnR" type="button" class="buttonCancel">Table</button></li>
                                                        </ul>
                                                    </div>

                                                    <div class="card-body pb-0">
                                                        <h5 class="card-title">Sales Rep Contribution (% of Total Sales)</h5>
                                                        <div id="chartContainerR">
                                                            <div id="repsPieChart" style="height: 400px;"></div>
                                                        </div>
                                                        <div id="tableContainerR" style="display: none;">
                                                            <table id="salesRepTable" class="table" style="width:100%">
                                                                <thead>
                                                                    <tr>
                                                                        <th>Sales Rep</th>
                                                                        <th>% of Total Sales</th>
                                                                    </tr>
                                                                </thead>
                                                                <tbody id="salesRepTbody">
                                                                    <!-- Table rows will be dynamically added here -->
                                                                </tbody>
                                                            </table>
                                                        </div>
                                                    </div>
                                                </div>

                                    <!-- End Sales Rep Contributions -->
                                    <div class="card">
                                        <div class="card-body pb-0">
                                          <h5 class="card-title">Sales Reps </h5>
                                            <div style="overflow:auto; height:38em">
                                            <asp:GridView ID="GridReps" runat="server" CssClass="gridview" AutoGenerateColumns="False" AllowSorting="true" OnSorting="GridReps_Sorting" OnSelectedIndexChanged="GridReps_SelectedIndexChanged" AllowPaging="true" OnPageIndexChanging="GridReps_PageIndexChanging" PageSize="14">
                                                    <HeaderStyle CssClass="gridViewHeader" />
                                                    <RowStyle CssClass="gridViewRow" />
                                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                                    <PagerStyle CssClass="gridViewPager" />
                                                    <Columns>
                                                       <asp:TemplateField SortExpression="SalesRep" HeaderText="Sales Rep">
                                                           <ItemTemplate>
                                                               <asp:LinkButton ID="lbSalesRep" runat="server" CommandArgument='<%# Eval("SalesRep") %>' Text='<%# Eval("SalesRep") %>' ForeColor="Blue" Font-Bold="true">
                                                               </asp:LinkButton>
                                                           </ItemTemplate>
                                                       </asp:TemplateField>
                                                        <asp:BoundField DataField="NumberOfOrders" HeaderText="# of Invoices" SortExpression="NumberOfOrders" ItemStyle-HorizontalAlign="Center" />
                                                        <asp:BoundField DataField="TotalOrderValue" HeaderText="Invoices Value" SortExpression="TotalOrderValue" DataFormatString="{0:N2}" HeaderStyle-HorizontalAlign="Right"  ItemStyle-HorizontalAlign="Right" ItemStyle-Width="8em" />
                                                    </Columns>
                                                </asp:GridView>  
                                                </div>
                                            </div>
                                        </div>
                                </div>
                                <!-- End Right side columns -->
                            </div>
                        </section>
                </main>
                <!-- End #main -->

        <!-- ======= Footer ======= -->
<footer id="footer" class="footer">
  <div class="copyright">
    &copy; Copyright 2024&nbsp;<strong>Syncflo (Pty) Ltd</strong><span>&nbsp;Assisted by : NiceAdmin</span>.  All Rights Reserved
  </div>
  <div class="credits">
    <!-- All the links in the footer should remain intact. -->
    <!-- You can delete the links only if you purchased the pro version. -->
    <!-- Licensing information: https://bootstrapmade.com/license/ -->
  </div>
</footer><!-- End Footer -->

<a href="#" class="back-to-top d-flex align-items-center justify-content-center"><i class="bi bi-arrow-up-short"></i></a>

<script>
    function showDiv() {
        document.getElementById('myHiddenDiv').style.display = "";
        document.getElementById('myHiddenDiv2').style.display = "";
        document.getElementById('Div1').style.display = "none";
        document.getElementById('CoSelectDiv').style.display = "none";
        document.getElementById('CoHDiv').style.display = "none";
        document.getElementById('btnUpdate').style.display = "none";
        setTimeout('document.images["myAnimatedImage"].src="images/tenorwait.gif"', 200);
    }
</script>

<!-- Vendor JS Files -->
<script src="https://cdn.jsdelivr.net/npm/apexcharts"></script>
<script src="niceadmin/assets/vendor/apexcharts/apexcharts.min.js"></script>
<script src="niceadmin/assets/vendor/bootstrap/js/bootstrap.bundle.min.js"></script>
<script src="niceadmin/assets/vendor/chart.js/chart.umd.js"></script>
<script src="niceadmin/assets/vendor/echarts/echarts.min.js"></script>
<script src="niceadmin/assets/vendor/quill/quill.js"></script>
<script src="niceadmin/assets/vendor/simple-datatables/simple-datatables.js"></script>
<script src="niceadmin/assets/vendor/tinymce/tinymce.min.js"></script>
<script src="niceadmin/assets/vendor/php-email-form/validate.js"></script>

            <!-- Template Main JS File -->
            <script src="niceadmin/assets/js/main.js"></script>

        <asp:Literal ID="chartDataLiteral" runat="server"></asp:Literal>
        <asp:Literal ID="repChartDataLiteral" runat="server"></asp:Literal>
        <asp:Literal ID="categoryChartDataLiteral" runat="server"></asp:Literal>
                    
        <script src="js/dash-sales.js"></script>
        <script src="js/dash-salesReps.js"></script>
        <script src="js/categoryChart.js"></script>

<div style="display:none">
    <asp:Label ID="lblSort" runat="server" Text=""></asp:Label><asp:Label ID="lblDir" runat="server" Text=""></asp:Label>
</div>
    </form>
    <script type="application/javascript">
        document.addEventListener("DOMContentLoaded", function () {
            const breadcrumbs = document.createElement("nav");
            breadcrumbs.className = "breadcrumbs";
            const breadcrumbLinks = [
                { name: "Home", url: "https://mydatainsights.online/za/index.aspx" },
                { name: "Dashboard", url: "https://mydatainsights.online/za/SageCloudDIReporting.aspx" }
            ];

            breadcrumbLinks.forEach(function (link, index) {
                const breadcrumbItem = document.createElement("a");
                breadcrumbItem.href = link.url;
                breadcrumbItem.textContent = link.name;
                if (index < breadcrumbLinks.length - 1) {
                    breadcrumbItem.textContent += " > ";  // Add separator for all but the last item
                }
                breadcrumbs.appendChild(breadcrumbItem);
            });
            // Append breadcrumbs to the page
            document.body.insertBefore(breadcrumbs, document.body.firstChild);
        });
    </script>

</body>
       
</html>
