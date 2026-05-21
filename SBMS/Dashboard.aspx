<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="SBMS.Dashboard" %>
<%@ Register Src="~/CommonScripts.ascx" TagPrefix="uc" TagName="CommonScripts" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Data Fusion</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <link href="lib/toastr/toastr.min.css" rel="stylesheet" />
   <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script  type="text/javascript" src="lib/toastr/toastr.min.js"></script>
    <script type="text/javascript" src="scripts/notifications.js"></script>
     <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true"></asp:ScriptManager>
         <uc:CommonScripts ID="CommonScripts" runat="server" />
        <div class="content"> 
            <div class="container">             
                <div class="row 150%">
                   <div class="1u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>
                    <div class="2u 12u$(medium)" style="text-align:center">&nbsp;
                        <asp:Label ID="lblUserName" runat="server" Text="" style="font-size:.8em; padding-top:1em;"></asp:Label>
                        <asp:DropDownList ID="DDProgBoard" runat="server" CssClass="buttonC" AutoPostBack="true" OnSelectedIndexChanged="DDProgBoard_SelectedIndexChanged"   >
                                           <asp:ListItem> - Tracking Board - </asp:ListItem>
                                           <asp:ListItem>Picking Slips</asp:ListItem> 
                                           <asp:ListItem>Job Cards</asp:ListItem>
                                           <asp:ListItem>Production</asp:ListItem>                           
                                           </asp:DropDownList>    
                    </div>
                    <div class="5u 12u$(medium)">  
                     <div id="lbluat" runat="server" style="color:green; font-size:0.6em; text-align:center">WARNING - Your profile is in <strong  style="color:red">User Acceptance Testing (UAT)</strong> status. <br /> This means data will come FROM SBCA, but NO data will be sent to or updated in SBCA. <br /> When you are ready, please email us with a request to activate full 2 way integration.</div>
                    <h4 style="padding-top:1em">Data Fusion <span style="font-size:.5em" >By Syncflo</span> for <asp:Label ID="lblCoName" runat="server" Text=""></asp:Label></h4> 
                    </div>
                     <div class="2u 12u$(medium)">
                          <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonC icon fa-eject" OnClick="lbtnLogOut_Click" style="float:right;" ToolTip="Log Out">&nbsp;Log Out&nbsp;</asp:LinkButton>
                            <asp:LinkButton ID="lbtnAdmin" runat="server" class="buttonC icon fa-gears" style="float:right; margin-right:1em" ToolTip="Config, Settings and Master file management" PostBackUrl="~/ConfigMaster.aspx">&nbsp;&nbsp;</asp:LinkButton>
                     </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" />
                        <asp:CheckBox ID="chkMobile" runat="server" Text="Mobile" AutoPostBack="true" style="float:right;" OnCheckedChanged="chkMobile_CheckedChanged"/>
                    </div>
                    </div>
                        <div class="row 150%">
                            <div class="2u 12u$(medium)">&nbsp;</div>
                            <div class="8u 12u$(medium)" style="text-align: center">    
                                <div id='myHiddenDiv' runat="server" style='display: none'>
                                         <div style="padding: .5em;">
                                         <img src="images/tenorwait.gif" id='myAnimatedImage' align='absmiddle' class="funkygif" style="border-radius:.5em"  />
                                        </div>
                                 </div>
                                <asp:Panel ID="pnlButtons" runat="server">  
                                    <h5>Module 1 - Multi Stores, Receiving, Item Transfers & Picking Slip Management,
                                        <br />
                                        Stock Counts, Lot/Batch Tracking & Item Movement Reporting</h5>
                                    <asp:LinkButton ID="imgbRec" runat="server" CssClass="button big" OnClick="imgbRec_Click" OnClientClick="showDiv()" ToolTip ="See outstanding Purchase Orders and carry out receiving process."><img src="images/receiving.png" alt="Click Here" class="image fit" /></asp:LinkButton>
                                    <asp:LinkButton ID="ibtnPickSlips" runat="server" CssClass="button big" OnClick="ibtnPickSlips_Click" ToolTip="See all open sales orders, and thier associated Picking slips or Job cards. Process you Sales Orders from here." OnClientClick="showDiv()" ><img src="images/PickSlips.png" alt="Click Here" class="image fit" /></asp:LinkButton>
                                    <asp:LinkButton ID="ibtnPickTrack" runat="server" CssClass="button big" OnClick="ibtnPickTrack_Click" ToolTip="Track and move all open Picking Slips."><img src="images/Picksliptrack.png" alt="Click Here" class="image fit" /></asp:LinkButton>
                                    <asp:LinkButton ID="ibtnStckCtl" runat="server" CssClass="button big" OnClick="ibtnStckCtl_Click" ToolTip="Stock Transfers, Adjustments, Reports & Stock counts."><img src="images/StockCounts.png" alt="Click Here" class="image fit" /></asp:LinkButton>
                                    <asp:LinkButton ID="ibtnmrp2" runat="server" CssClass="button big" OnClick="ibtnmrp2_Click" ToolTip="View finished goods materials requirements based on purchase orders, sales orders, stock balances, minimum stock etc."><img src="images/fgmrp.png" alt="Click Here" class="image fit" /></asp:LinkButton>
                                    <asp:Panel ID="PnlForecast" runat="server">
                                        <h5 id="headmod2" runat="server">Module 2: Planning, Job Cards, Kits & Finished Goods Demands</h5>
                                        <asp:LinkButton ID="ibtnFCasts" runat="server" CssClass="button big" OnClick="ibtnFCasts_Click" ToolTip="Add/Edit Sales Forecasts"><img src="images/Forecasta.png" alt="Click Here"class="image fit" /></asp:LinkButton>
                                        <asp:LinkButton ID="ibtnmrp" runat="server" CssClass="button big" OnClick="ibtnmrp2_Click" ToolTip="View finished goods materials requirements based on purchase orders, sales orders, stock balances, minimum stock etc."><img src="images/fgmrp.png" alt="Click Here" class="image fit"/></asp:LinkButton>
                                        <asp:LinkButton ID="ibtnJobTrack" runat="server" CssClass="button big" OnClick="ibtnJobTrack_Click" ToolTip="View all open job cards and complete job card workflows."><img src="images/JobTrack.png" alt="Click Here" class="image fit" /></asp:LinkButton>
                                    </asp:Panel>
                                    <asp:Panel ID="PnlProduction" runat="server">
                                        <h5>Module 3: Manufacturing, BOM's and Raw Materials Planning (MRP)</h5>
                                        <asp:LinkButton ID="ibtmWorksOrders" runat="server" CssClass="button big" OnClick="ibtmWorksOrders_Click" ToolTip="View all current manufacturing or production works orders."><img src="images/worksord.png" alt="Click Here" class="image fit"" /></asp:LinkButton>
                                        <asp:LinkButton ID="ibtmWOrdMgment" runat="server" CssClass="button big" OnClick="ibtmWOrdMgment_Click" ToolTip="Record manufacturing/production and allocate raw materials."><img src="images/WOrdFilling.png" alt="Click Here" class="image fit" /></asp:LinkButton>
                                        <asp:LinkButton ID="ibtnRMD" runat="server" CssClass="button big" OnClick="ibtnRMD_Click" ToolTip="View and analyse raw materials demands based on current works orders."><img src="images/rmMrp.png" alt="Click Here" class="image fit" /></asp:LinkButton>
                                    </asp:Panel>
                                    <div class="ticker-tape-wrapper">
                                          <div class="ticker-tape">
                                            <asp:Label ID="lblWarn" runat="server" Text=""></asp:Label>
                                          </div>
                                        </div>
                                 </asp:Panel>
                            </div>
                            <div class="2u 12u$(medium)" style="text-align:left; font-size:xx-small">
                                <h3>Whats new?</h3>
                                <ul>
                                    <li><a href="https://mydatafusion.online/learning.aspx?id=IopFk85Yeeo" target="_blank" title="See clip"> >> 14 April 2026 - Converting products to different items codes or UOM's.</a></li>
                                    <li><a href="https://mydatafusion.online/learning.aspx?id=xLpoTuzEKqM" target="_blank" title="See clip"> >> Receiving goods purchased in foreign currency.</a></li>
                                    <li><a href="https://mydatafusion.online/learning.aspx?id=xLpoTuzEKqM" target="_blank" title="See clip"> >> Allocate multiple lot numbers when receiving a single item code.</a></li>
                                    <li><a href="https://mydatafusion.online/learning.aspx?id=xLpoTuzEKqM" target="_blank" title="See clip"> >> Consume multiple lot numbers when fulfilling a works order.</a></li>
                                </ul>

                            </div>
                        </div>
                        </div>
            <section id="footer" class="wrapper">
                 <a href="SBMSMobile/DashboardM.aspx" class="button icon fa-mobile" target="_blank">Mobile</a>
                <a href="https://mydatafusion.online/learningCenter.aspx" class="button special icon fa-lightbulb" target="_blank"> Learn more from the Learning Hub >></a>
            </section>
                </div>

        <%-- Floating Help Button — always visible at bottom-right --%>
        <asp:LinkButton ID="lbtnHelper" runat="server" OnClick="lbtnHelper_Click"
            CssClass="floating-help-btn" ToolTip="Get help using Data Fusion">
            <span class="floating-help-icon">?</span>
            <span class="floating-help-label">Help</span>
        </asp:LinkButton>

        <style>
            .floating-help-btn {
                display: inline-flex;
                align-items: center;
                gap: .5em;
                background: #4282C1;
                color: #fff;
                padding: .7em 1.1em;
                border-radius: 2em;
                text-decoration: none;
                font-weight: 600;
                font-size: 1.05em;
                box-shadow: 0 4px 18px rgba(66,130,193,.45);
                cursor: pointer;
                position: fixed;
                bottom: 3.5rem;
                right: 1.5rem;
                z-index: 9999;
                transition: box-shadow .2s, transform .15s, background .2s;
            }
            .floating-help-btn:hover {
                background: #356a9e;
                box-shadow: 0 6px 22px rgba(66,130,193,.6);
                transform: translateY(-1px);
            }
            .floating-help-icon {
                display: inline-flex;
                align-items: center;
                justify-content: center;
                width: 1.6em; height: 1.6em;
                background: rgba(255,255,255,.25);
                border-radius: 50%;
                font-size: 1.15em;
                font-weight: 700;
            }
            .floating-help-label { font-size: .9em; }

            @media screen and (max-width: 736px) {
                .floating-help-btn {
                    bottom: 2.2rem;
                    right: 1rem;
                    padding: .55em .9em;
                    font-size: .95em;
                }
            }
            @media screen and (max-width: 480px) {
                .floating-help-btn {
                    bottom: 1.8rem;
                    right: .75rem;
                    padding: .45em .75em;
                    font-size: .85em;
                }
                .floating-help-label { display: none; }
            }
        </style>
        <script>
            function showDiv() {
                document.getElementById('myHiddenDiv').style.display = "";
                document.getElementById('pnlButtons').style.display = "none";
                setTimeout('document.images["myAnimatedImage"].src="images/tenorwait.gif"', 200);
            }
        </script>
    </form>
</body>
</html>
