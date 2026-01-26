<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ConfigCompany.aspx.cs" Inherits="SBMS.ConfigCompany" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Company</title>
        <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
        <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
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
                           <table style="width:100%; text-align:left">
                               <tr>
                                   <td>Company Name</td>
                                   <td><asp:Label ID="lblCoName" runat="server" Text=""></asp:Label></td>
                                   <td>Sage Company ID</td>
                                   <td><asp:Label ID="lblCoID" runat="server" Text=""></asp:Label></td>
                               </tr>
                               <tr>
                                   <td colspan="4"><hr /></td>
                               </tr>
                               <%--<tr>
                                   <td>Unique License Key</td>
                                   <td colspan="3"><asp:Label ID="lblGUID" runat="server" Text="Label"></asp:Label></td>
                               </tr>
                                <tr>
                                   <td colspan="4"><hr /></td>
                               </tr>--%>
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
                               <tr>
                                   <td colspan="4"><hr /></td>
                               </tr>
                              <%-- <tr>
                                   <td>Sage Generic Login</td>
                                   <td><asp:TextBox ID="lblGenEmail" runat="server"></asp:TextBox></td>
                                   <td>Sage Generic Password</td>
                                   <td><asp:TextBox ID="lblGenPwd" runat="server"></asp:TextBox></td>
                               </tr>--%>
                                <tr>
                                 <td colspan="4"><hr /></td>
                             </tr> 
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
                               <tr>
                                    <td colspan="4"><hr /></td>
                                </tr>
                                  <tr>
                                   <td>
                                     Auto Update Sage on Picking Complete
                                 </td>
                                   <td>
                                       <asp:CheckBox ID="chkPSAuto" runat="server" />
                                       <cci:BalloonPopupExtender ID="BalloonPopupExtender2" TargetControlID="chkPSAuto" UseShadow="true"
                                           DisplayOnMouseOver ="true" Position="BottomRight" BalloonPopupControlID="pnlPopPickComplete" BalloonStyle="Rectangle"
                                            runat="server"  />
                                         <asp:Panel ID="pnlPopPickComplete" runat="server" style="font-size:small">
                                            Select this option if you want to auto update Sage with a message of "Ready To Invoice", as soon as picking of a picking slip is complete. <br /><br /> Use this option if a different user generates Tax Invoices.
                                        </asp:Panel>
                                    </td>
                                        <td>
                                        Auto Generate Sage Tax Invoices
                                    </td>
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
                                    <td colspan="4"><hr /></td>
                                </tr> 

                               <tr>
                                   <td>Use Lot Tracking</td>
                                    <td><asp:CheckBox ID="chkLotTrack" runat="server" OnCheckedChanged="chkLotTrack_CheckedChanged" AutoPostBack="true"/>
                                        <cci:BalloonPopupExtender ID="BalloonPopupExtender3" TargetControlID="chkLotTrack" UseShadow="true"
                                              DisplayOnMouseOver ="true" Position="BottomRight" BalloonPopupControlID="pnlLotTrack" BalloonStyle="Rectangle" 
                                               runat="server" />
                                            <asp:Panel ID="pnlLotTrack" runat="server" style="font-size:small">
                                              Use Lot Tracking if your raw materials have a shelf life and you want to be able to track which lot numbers are allocated to which orders.
                                           </asp:Panel>
                                    </td>
                                   <td>Lot Tracking Advanced</td>
                                  <td><asp:CheckBox ID="chkLotTrackAdd" runat="server"/></td>    
                                    </tr>     
                                        <tr>
                                        <td colspan="4"><hr /></td>
                                    </tr> 
                               <tr>
                                   <td>Use Auto Manufacture</td>
                                    <td><asp:CheckBox ID="chkAutoManf" runat="server" OnCheckedChanged="chkAutoManf_CheckedChanged" AutoPostBack="true" />
                                        <cci:BalloonPopupExtender ID="BalloonPopupExtender4" TargetControlID="chkAutoManf" UseShadow="true"
                                              DisplayOnMouseOver ="true" Position="BottomRight" BalloonPopupControlID="pnlAutoManf" BalloonStyle="Rectangle" 
                                               runat="server" />
                                            <asp:Panel ID="pnlAutoManf" runat="server" style="font-size:small">
                                              Auto manufacturing is useful if you use only 1 warehouse and do not require lot tracking.  Auto Manufacture will simplify and speed up the fulfullment of works orders and adjust your stock accordingly works orders can be auto fulfileld with 1 click.
                                           </asp:Panel>
                                    </td>
                               </tr>
                                    <tr>
                                        <td colspan="4"><hr /></td>
                                    </tr> 
                                    <tr>
                                        <td colspan="4">
                                            <div id="pnlAutoManfConfirm">
                                                <span style="color:red">For Auto Manufacture to function, Lot Tracking will be switched off, and only 1 store will be available. </span><br />
                                                <asp:CheckBox ID="chkAutoManfConf" runat="server" Text="I understand, continue" style="float:right" />
                                            </div>
                                        </td>
                                    </tr>  
                               
                               <tr>
                                    <td colspan="4"><hr /></td>
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
                               <tr>
                                     <td colspan="4"><hr /></td>
                                 </tr> 
                                <tr>
                                   <td>Use Pack Sizes</td>
                                    <td><asp:CheckBox ID="chkUsePacks" runat="server"/>
                                        <cci:BalloonPopupExtender ID="BalloonPopupExtender8" TargetControlID="chkUsePacks" UseShadow="true" DisplayOnMouseOver ="true" Position="BottomRight" BalloonPopupControlID="pnlPack" BalloonStyle="Rectangle"  runat="server" />
                                             <asp:Panel ID="pnlPack" runat="server" style="font-size:small">
                                              If you sell products in different pack sizes, this would be useful. On picking slips, you can enter a pack code and the correct quantity will populate.
                                            </asp:Panel>
                                    </td>
                                   <td>Use BarCodes</td>
                                    <td><asp:CheckBox ID="chkBarCodes" runat="server"/></td>
                                </tr> 
                               <tr>
                                   <td colspan="4"><hr /></td>
                               </tr> 
                               <tr>
                                   <td>Module 2 Active</td>
                                   <td><asp:CheckBox ID="chkMod2" runat="server"/>
                                       <cci:BalloonPopupExtender ID="BalloonPopupExtender5" TargetControlID="chkMod2" UseShadow="true" DisplayOnMouseOver ="true" Position="BottomRight" BalloonPopupControlID="pnlJC" BalloonStyle="Rectangle"  runat="server" />
                                            <asp:Panel ID="pnlJC" runat="server" style="font-size:small">
                                              Module 2 is applicable if you require Sales forcasting and Job Cards. Contact us if you want it switched on or off. 
                                           </asp:Panel>
                                   </td>
                                   </tr>
                               <tr>
                                    <td colspan="4"><hr /></td>
                                </tr> 
                               <tr>
                                   <td>Module 3 Active</td>
                                   <td><asp:CheckBox ID="chkMod3" runat="server" />
                                       <cci:BalloonPopupExtender ID="BalloonPopupExtender6" TargetControlID="chkMod3" UseShadow="true" DisplayOnMouseOver ="true" Position="BottomRight" BalloonPopupControlID="pnlBOM" BalloonStyle="Rectangle"  runat="server" />
                                             <asp:Panel ID="pnlBOM" runat="server" style="font-size:small">
                                               Module 3 contains the requirements for Works Orders, prodution recording, BOM and Kits management. Contact us if you want it switched on or off. 
                                            </asp:Panel>
                                   </td>
                                   <td>Show Manufacturing Costs On Works Orders</td>
                                   <td><asp:CheckBox ID="chkManfCosts" runat="server"/></td>
                               </tr>           
                                
                                <tr>
                                      <td colspan="4"><hr /></td>
                                  </tr> 
                                <tr>
                                     <td>Send Notifications</td>
                                     <td><asp:CheckBox ID="chkNotifs" runat="server"/></td>
                                     <td>Testing Mode<br />
                                         <span style="font-size:.8em">In Testing Mode, integration <br /> with Sage accounting is ONE WAY only. <br /> No data is sent TO Sage, <br />but all data FROM Sage is current and rereshed on demand.</span>
                                     </td>
                                     <td><asp:CheckBox ID="chkUAT" runat="server" Enabled="false" /></td>
                                 </tr>
                                <tr>
                                   <td colspan="4"><hr /></td>
                               </tr>
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
                                        <td colspan="4"><hr /></td>
                                    </tr>
                               <tr>
                                   <td style="vertical-align:top">Company Image</td>
                                   <td><asp:Image ID="imgCoImgD" runat="server" Width="200px" /></td>
                                   <td style="vertical-align:top"></td>
                                   <td><asp:LinkButton ID="lbtnSave" runat="server" style="float:right; vertical-align:bottom" CssClass="icon fa-save buttonRed" OnClick="lbtnSave_Click"> SAVE</asp:LinkButton></td>
                               </tr>
                           </table>        
                        </div>
                     <div class="2u 12u$(medium)">&nbsp</div>   
                     </div>
                </div>
        </div>
                <script type="text/javascript">
                        $(document).ready(function () {
                            // Attach change event
                            $('#<%= chkAutoManf.ClientID %>').change(function () {
                        if ($(this).is(':checked')) {
                            $('#pnlAutoManfConfirm').slideDown();  // Smooth show
                        } else {
                            $('#pnlAutoManfConfirm').slideUp();    // Smooth hide
                            $('#<%= chkAutoManfConf.ClientID %>').prop('checked', false); // Uncheck confirmation box
                        }
                    });

                    // Initial state on page load
                    if (!$('#<%= chkAutoManf.ClientID %>').is(':checked')) {
                        $('#pnlAutoManfConfirm').hide(); // Hide if not checked
                    }
                });
        </script>
             <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    </form>
</body>
</html>
