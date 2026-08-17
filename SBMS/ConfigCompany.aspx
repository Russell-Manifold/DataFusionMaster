<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="ConfigCompany.aspx.cs" Inherits="SBMS.ConfigCompany" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Company</title>
        <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
        <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <style>
        /* Client-side tabs: every tab's controls stay in the DOM, so the single
           Save posts them all. Switching tabs never posts back. */
        .cfg-tabnav { display:flex; gap:.35rem; flex-wrap:wrap; border-bottom:2px solid #ddd; margin:0 0 1rem 0; }
        .cfg-tabnav button { border:1px solid #ddd; border-bottom:none; background:#f4f4f4; padding:.55rem 1.2rem;
                             font-family:inherit; font-size:.95em; font-weight:600; color:#3d4449;
                             border-radius:7px 7px 0 0; cursor:pointer; }
        .cfg-tabnav button:hover { background:#fff; color:#ed9625; }
        .cfg-tabnav button.active { background:#fff; color:#ed9625; border-color:#ddd; }
        .cfg-tab { display:none; }
        .cfg-tab.active { display:block; }
        .cfg-sec { text-align:left; color:#ed9625; font-weight:700; padding:.2em 0 .1em; }
        /* Clean one-setting-per-row form (Lots & Packs) */
        .cfg-form { width:100%; border-collapse:collapse; }
        .cfg-form td { padding:.4rem .5rem; vertical-align:top; text-align:left; }
        .cfg-form td.cfg-lbl { width:22em; font-weight:600; color:#3d4449; }
        .cfg-help { font-size:.8em; color:#777; margin-top:2px; max-width:42em; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
         <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="content">
            <div class="container">
                <div class="row 150%">
                     <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>
                    <div class="8u 12u$(medium)">
                          <asp:LinkButton ID="LbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click" >&nbsp;&nbsp;</asp:LinkButton>
                          <asp:LinkButton ID="LbtnConfig" runat="server" class="buttonC icon fa-gears" PostBackUrl="~/ConfigMaster.aspx" > &nbsp;Settings</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton>
                        <br />
                        <h2 style="padding-top:0; line-height:1em">Company Details</h2>
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>

                <div class="row 150%">
                     <div class="2u 12u$(medium)">&nbsp</div>
                     <div class="8u 12u$(medium)" style="text-align:center">

                        <asp:HiddenField ID="hfActiveTab" runat="server" ClientIDMode="Static" />
                        <div class="cfg-tabnav">
                            <button type="button" data-tab="general"  onclick="cfgShowTab('general')">General</button>
                            <button type="button" data-tab="lots" onclick="cfgShowTab('lots')">Lots, Packs &amp; Bins</button>
                            <asp:PlaceHolder ID="phMobileTabBtn" runat="server"><button type="button" data-tab="mobile" onclick="cfgShowTab('mobile')">Mobile Picking</button></asp:PlaceHolder>
                            <button type="button" data-tab="manf"     onclick="cfgShowTab('manf')">Manufacturing</button>
                        </div>

                        <%-- ═══════════════════ GENERAL ═══════════════════ --%>
                        <div id="cfgtab-general" class="cfg-tab">
                           <table style="width:100%; text-align:left">
                               <tr>
                                   <td>Company Name</td>
                                   <td><asp:Label ID="lblCoName" runat="server" Text=""></asp:Label></td>
                                   <td>Sage Company ID</td>
                                   <td><asp:Label ID="lblCoID" runat="server" Text=""></asp:Label></td>
                               </tr>
                               <tr><td colspan="4"><hr /></td></tr>
                               <tr>
                                   <td>Contact Person</td>
                                   <td><asp:TextBox ID="txtContPerson" runat="server"></asp:TextBox></td>
                                   <td>Contact email</td>
                                   <td><asp:TextBox ID="txtContemail" runat="server"></asp:TextBox></td>
                               </tr>
                                <tr>
                                   <td>Tel 1</td>
                                   <td><asp:TextBox ID="TextBox3" runat="server"></asp:TextBox></td>
                                   <td>Tel 2</td>
                                   <td><asp:TextBox ID="TextBox4" runat="server"></asp:TextBox></td>
                               </tr>
                               <tr><td colspan="4"><hr /></td></tr>
                               <tr>
                                 <td>Item Quantity - decimal places</td>
                                 <td><asp:DropDownList ID="DDecPlaces" runat="server" Width="50px" style="text-align:center">
                                     <asp:ListItem>0</asp:ListItem>
                                     <asp:ListItem>1</asp:ListItem>
                                     <asp:ListItem>2</asp:ListItem>
                                     <asp:ListItem>3</asp:ListItem>
                                     <asp:ListItem>4</asp:ListItem>
                                     </asp:DropDownList></td>
                              </tr>
                               <tr><td colspan="4"><hr /></td></tr>
                               <tr><td colspan="4" class="cfg-sec">Sage Integration</td></tr>
                                  <tr>
                                   <td>Auto Update Sage on Picking Complete</td>
                                   <td>
                                       <asp:CheckBox ID="chkPSAuto" runat="server" />
                                       <cci:BalloonPopupExtender ID="BalloonPopupExtender2" TargetControlID="chkPSAuto" UseShadow="true"
                                           DisplayOnMouseOver ="true" Position="BottomRight" BalloonPopupControlID="pnlPopPickComplete" BalloonStyle="Rectangle"
                                            runat="server"  />
                                         <asp:Panel ID="pnlPopPickComplete" runat="server" style="font-size:small">
                                            Select this option if you want to auto update Sage with a message of "Ready To Invoice", as soon as picking of a picking slip is complete. <br /><br /> Use this option if a different user generates Tax Invoices.
                                        </asp:Panel>
                                    </td>
                                        <td>Auto Generate Sage Tax Invoices</td>
                                      <td>
                                          <asp:CheckBox ID="chkTaxInvAuto" runat="server" />
                                          <cci:BalloonPopupExtender ID="BalloonPopupExtender1" TargetControlID="chkTaxInvAuto" UseShadow="true"
                                                DisplayOnMouseOver ="true" Position="BottomRight" BalloonPopupControlID="pnlPopTaxInvComplete" BalloonStyle="Rectangle"
                                                 runat="server" />
                                              <asp:Panel ID="pnlPopTaxInvComplete" runat="server" style="font-size:small">
                                                 Select this option if you want to auto generate a Tax Invoice in Sage, as soon as picking of a picking slip is marked as complete.
                                             </asp:Panel>
                                      </td>
                             </tr>
                             <tr>
                                    <td>Use Produce Weights (for deliveries, shipping etc)</td>
                                     <td><asp:CheckBox ID="chkweight" runat="server" /></td>
                                    <td>Sage field name used for Unit weight</td>
                                   <td><asp:TextBox ID="txtSageWght" runat="server" placeholder="eg: NumericUserField1"></asp:TextBox></td>
                             </tr>
                               <tr><td colspan="4"><hr /></td></tr>
                               <tr><td colspan="4" class="cfg-sec">Modules</td></tr>
                               <tr>
                                   <td>Module 2 Active</td>
                                   <td><asp:CheckBox ID="chkMod2" runat="server"/>
                                       <cci:BalloonPopupExtender ID="BalloonPopupExtender5" TargetControlID="chkMod2" UseShadow="true" DisplayOnMouseOver ="true" Position="BottomRight" BalloonPopupControlID="pnlJC" BalloonStyle="Rectangle"  runat="server" />
                                            <asp:Panel ID="pnlJC" runat="server" style="font-size:small">
                                              Module 2 is applicable if you require Sales forcasting and Job Cards. Contact us if you want it switched on or off.
                                           </asp:Panel>
                                   </td>
                                   <td>Module 3 Active</td>
                                    <td><asp:CheckBox ID="chkMod3" runat="server" />
                                        <cci:BalloonPopupExtender ID="BalloonPopupExtender6" TargetControlID="chkMod3" UseShadow="true" DisplayOnMouseOver ="true" Position="BottomRight" BalloonPopupControlID="pnlBOM" BalloonStyle="Rectangle"  runat="server" />
                                              <asp:Panel ID="pnlBOM" runat="server" style="font-size:small">
                                                Module 3 contains the requirements for Works Orders, prodution recording, BOM and Kits management. Contact us if you want it switched on or off.
                                             </asp:Panel>
                                    </td>
                                </tr>
                               <tr>
                                   <td>Mobile Module Active</td>
                                   <td colspan="3"><asp:CheckBox ID="chkMobileModule" runat="server"/>
                                       <span style="font-size:small;color:#666;">&nbsp;&nbsp;Grants the Mobile Picking tab &amp; the scanner (mobile) app for this company.</span></td>
                               </tr>
                               <tr><td colspan="4"><hr /></td></tr>
                                <tr>
                                     <td>Send Notifications</td>
                                     <td><asp:CheckBox ID="chkNotifs" runat="server"/></td>
                                     <td>Testing Mode<br />
                                         <span style="font-size:.8em">In Testing Mode, integration <br /> with Sage accounting is ONE WAY only. <br /> No data is sent TO Sage, <br />but all data FROM Sage is current and rereshed on demand.</span>
                                     </td>
                                     <td><asp:CheckBox ID="chkUAT" runat="server" Enabled="false" /></td>
                                 </tr>
                               <tr><td colspan="4"><hr /></td></tr>
                               <tr><td colspan="4" class="cfg-sec">Company Image</td></tr>
                                <tr>
                                    <td colspan="4" style="text-align:center">
                                        <asp:FileUpload ID="fileUpload" runat="server" style="font-size:1.2em" />
                                            <asp:LinkButton ID="btnUpload" runat="server" OnClick="btnUpload_Click" CssClass="buttonC icon fa-upload" >Upload Image</asp:LinkButton>
                                            <asp:Label ID="lblMessage" runat="server" ForeColor="Red" />
                                        <h4>Image limitations</h4>
                                        <ul>
                                            <li>* Only .png files allowed</li>
                                            <li>* File size limit = 200kb</li>
                                            <li><span style="font-size:0.7em">(With some browsers, you may need to clear your browser history after successful upload, for you new image to display)</span> </li>
                                        </ul>
                                    </td>
                                </tr>
                                <tr>
                                   <td style="vertical-align:top">Current Image</td>
                                   <td colspan="3"><asp:Image ID="imgCoImgD" runat="server" Width="200px" /></td>
                               </tr>
                           </table>
                        </div>

                        <%-- ═══════════════════ LOTS & PACKS ═══════════════════ --%>
                        <div id="cfgtab-lots" class="cfg-tab">
                           <table class="cfg-form">
                                <tr><td colspan="2" class="cfg-sec">Lot Tracking</td></tr>
                                <tr>
                                    <td class="cfg-lbl">Use Lot Tracking</td>
                                    <td><asp:CheckBox ID="chkLotTrack" runat="server" OnCheckedChanged="chkLotTrack_CheckedChanged" AutoPostBack="true"/>
                                        <div class="cfg-help">Track lot numbers for raw materials with a shelf life, and see which lots were allocated to which orders.</div>
                                    </td>
                                </tr>
                                <tr>
                                    <td class="cfg-lbl">Lot Tracking Advanced</td>
                                    <td><asp:CheckBox ID="chkLotTrackAdd" runat="server"/></td>
                                </tr>
                                <tr>
                                    <td class="cfg-lbl">System-Generated Lot Numbers</td>
                                    <td><asp:CheckBox ID="chkSysLot" runat="server" />
                                        <div class="cfg-help">On = the app auto-creates lot numbers (scanners can receive). Off = users enter their own lot numbers (receiving is web-only).</div>
                                    </td>
                                </tr>
                                <tr><td colspan="2" class="cfg-sec" style="padding-top:1rem">Pack Sizes</td></tr>
                                <tr>
                                    <td class="cfg-lbl">Use Pack Sizes</td>
                                    <td><asp:CheckBox ID="chkUsePacks" runat="server"/>
                                        <div class="cfg-help">Sell in multiple pack sizes &mdash; enter a pack code on picking slips and the correct quantity populates.</div>
                                    </td>
                                </tr>
                                <tr><td colspan="2" class="cfg-sec" style="padding-top:1rem">Bin / Location Naming</td></tr>
                                <tr>
                                    <td class="cfg-lbl">Bin ID segments</td>
                                    <td><asp:TextBox ID="txtBinSegments" runat="server" Width="60%" placeholder="Warehouse,Aisle,Row,Bin" />
                                        <div class="cfg-help">Comma-separated, in order, max 4 (e.g. Warehouse,Aisle,Row,Bin). Blank = free-text bins.</div>
                                    </td>
                                </tr>
                                <tr>
                                    <td class="cfg-lbl">Segment separator</td>
                                    <td><asp:TextBox ID="txtBinDelim" runat="server" Width="3em" MaxLength="3" Text="-" />
                                        <div class="cfg-help">e.g. <b>-</b> &rarr; WH1-A03-R2-B05 &nbsp;&nbsp;(Max Length = 15 chars)</div>
                                    </td>
                                </tr>
                           </table>
                        </div>

                        <%-- ═══════════════════ MOBILE PICKING (hidden unless the company has the Mobile Module) ═══════════════════ --%>
                        <asp:PlaceHolder ID="phMobileTab" runat="server">
                        <div id="cfgtab-mobile" class="cfg-tab">
                           <table style="width:100%; text-align:left">
                               <tr><td colspan="4" class="cfg-sec">Scanner / Mobile Receiving Modes</td></tr>
                               <tr>
                                   <td>1. Count &amp; Check<br /><span style="font-size:small;color:#666;">Scanner counts, web posts the receipt</span></td>
                                   <td><asp:CheckBox ID="chkScanCount" runat="server" /></td>
                                   <td>2. Direct Receive<br /><span style="font-size:small;color:#666;">Scanner receives straight into stock</span></td>
                                   <td><asp:CheckBox ID="chkScanReceive" runat="server" /></td>
                               </tr>
                               <tr>
                                   <td>3. Put-Away<br /><span style="font-size:small;color:#666;">Scanner relocates received stock</span></td>
                                   <td><asp:CheckBox ID="chkScanPutAway" runat="server" /></td>
                                   <td colspan="2" style="font-size:small;color:#666;">
                                       Each switch shows / hides its tile on the mobile (scanner) dashboard.
                                   </td>
                               </tr>
                                <tr><td colspan="4"><hr /></td></tr>
                               <tr><td colspan="4" class="cfg-sec">Picking Options</td></tr>
                                <tr>
                                   <td>LPN Boxing in Mobile Picking</td>
                                   <td><asp:DropDownList ID="DDLPNMode" runat="server" ToolTip="License Plate Number workflow on the mobile picking screen. Off: no LPN scanning. One box: scan a box label, picks are packed into it. Label every unit: scan a sticker then the stock item, one pair per unit.">
                                       <asp:ListItem Value="off">Off</asp:ListItem>
                                       <asp:ListItem Value="box">One box, many items</asp:ListItem>
                                       <asp:ListItem Value="unit">Label every unit</asp:ListItem>
                                   </asp:DropDownList></td>
                                  </tr>
                                <tr>
                                   <td>Pick by Bin (Mobile)</td>
                                   <td><asp:CheckBox ID="chkPickByBin" runat="server" ToolTip="For warehouses that use Stores as bins. Mobile pickers take each line from the bin(s) that hold stock (splitting across bins) instead of choosing one store for the whole slip. Off = the usual 'Select Picking Store' flow." /></td>
                                  </tr>
                                <tr>
                                   <td>Use Picking Slip Tracking</td>
                                   <td><asp:CheckBox ID="chkPickSlip" runat="server"/>
                                       <cci:BalloonPopupExtender ID="BalloonPopupExtender7" TargetControlID="chkPickSlip" UseShadow="true" DisplayOnMouseOver ="true" Position="BottomRight" BalloonPopupControlID="PnlPSTrack" BalloonStyle="Rectangle"  runat="server" />
                                            <asp:Panel ID="PnlPSTrack" runat="server" style="font-size:small">
                                             If your company does warehousing or straight picking of goods from stock without the need to task lot number, this is a useful tool to switch on.
                                           </asp:Panel>
                                   </td>
                                  </tr>
                           </table>
                        </div>
                        </asp:PlaceHolder>

                        <%-- ═══════════════════ MANUFACTURING ═══════════════════ --%>
                        <div id="cfgtab-manf" class="cfg-tab">
                           <table style="width:100%; text-align:left">
                               <tr>
                                   <td>Use Auto Manufacture</td>
                                    <td><asp:CheckBox ID="chkAutoManf" runat="server" OnCheckedChanged="chkAutoManf_CheckedChanged" AutoPostBack="true" />
                                        <cci:BalloonPopupExtender ID="BalloonPopupExtender4" TargetControlID="chkAutoManf" UseShadow="true"
                                              DisplayOnMouseOver ="true" Position="BottomRight" BalloonPopupControlID="pnlAutoManf" BalloonStyle="Rectangle"
                                               runat="server" />
                                            <asp:Panel ID="pnlAutoManf" runat="server" style="font-size:small">
                                              Auto Manufacture fills every component on a works order in one click and adjusts your stock accordingly. Choose the store to fulfil from on the works order itself. It cannot be used with lot tracking, because lots have to be allocated by hand.
                                           </asp:Panel>
                                    </td>
                               </tr>
                                    <tr>
                                        <td colspan="4">
                                            <div id="pnlAutoManfConfirm">
                                                <span style="color:red">Switching on Auto Manufacture switches Lot Tracking off.</span><br />
                                                <asp:CheckBox ID="chkAutoManfConf" runat="server" Text="I understand, continue" style="float:right" />
                                            </div>
                                        </td>
                                    </tr>
                               <tr><td colspan="4"><hr /></td></tr>
                                <tr>
                                     <td>Show Manufacturing Costs On Works Orders</td>
                                     <td><asp:CheckBox ID="chkManfCosts" runat="server"/></td>
                                 </tr>
                           </table>
                        </div>

                        <%-- Save is OUTSIDE the tabs - always visible, saves every tab at once. --%>
                        <table style="width:100%; text-align:left">
                            <tr><td colspan="4"><hr /></td></tr>
                            <tr>
                                <td colspan="4" style="text-align:right">
                                    <asp:LinkButton ID="lbtnSave" runat="server" CssClass="icon fa-save buttonRed" OnClick="lbtnSave_Click"> SAVE</asp:LinkButton>
                                </td>
                            </tr>
                        </table>

                        </div>
                     <div class="2u 12u$(medium)">&nbsp</div>
                     </div>
                </div>
        </div>
                <script type="text/javascript">
                    function cfgShowTab(name) {
                        var tabs = document.querySelectorAll('.cfg-tab');
                        for (var i = 0; i < tabs.length; i++) tabs[i].classList.toggle('active', tabs[i].id === 'cfgtab-' + name);
                        var btns = document.querySelectorAll('.cfg-tabnav button');
                        for (var j = 0; j < btns.length; j++) btns[j].classList.toggle('active', btns[j].getAttribute('data-tab') === name);
                        var hf = document.getElementById('hfActiveTab');
                        if (hf) hf.value = name;
                    }
                    $(document).ready(function () {
                        // Restore the active tab (survives the AutoPostBacks from Lot Tracking / Auto Manufacture).
                        var hf = document.getElementById('hfActiveTab');
                        var target = hf && hf.value ? hf.value : 'general';
                        if (!document.getElementById('cfgtab-' + target)) target = 'general'; // tab may be hidden (e.g. Mobile without the module)
                        cfgShowTab(target);

                        // Auto-Manufacture confirmation slide (behaviour unchanged).
                        $('#<%= chkAutoManf.ClientID %>').change(function () {
                            if ($(this).is(':checked')) { $('#pnlAutoManfConfirm').slideDown(); }
                            else { $('#pnlAutoManfConfirm').slideUp(); $('#<%= chkAutoManfConf.ClientID %>').prop('checked', false); }
                        });
                        if (!$('#<%= chkAutoManf.ClientID %>').is(':checked')) { $('#pnlAutoManfConfirm').hide(); }
                    });
                </script>
             <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    </form>
</body>
</html>
