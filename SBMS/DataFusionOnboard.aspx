<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="DataFusionOnboard.aspx.cs" Inherits="SBMS.DataFusionOnboard" %>
<%@ Register Src="~/CommonScripts.ascx" TagPrefix="uc" TagName="CommonScripts" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Data Fusion Onboard</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <link href="lib/toastr/toastr.min.css" rel="stylesheet" />
   <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script  type="text/javascript" src="lib/toastr/toastr.min.js"></script>
    <script type="text/javascript" src="scripts/notifications.js"></script>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true"></asp:ScriptManager>
         <uc:CommonScripts ID="CommonScripts" runat="server" />
        <div class="content">
            <div class="container">             
                <div class="row 150%">
                   <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>
                            <div class="8u 12u$(medium)">
                                <h2>Onboarding</h2>
                                <h4>Creating a Profile</h4>
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
                    <Triggers>
                        <%-- Full postback: Next may redirect off-site to Sage ID (OAuth 2.0) --%>
                        <asp:PostBackTrigger ControlID="lbtnNext" />
                    </Triggers>
                    <ContentTemplate>
                 <div class="row 150%">
                     <div class="3u 12u$(medium)">&nbsp;</div>
                        <div class="6u 12u$(medium)" style="text-align:center; padding-bottom:7em">
                            <asp:Panel ID="PnlPrimary" runat="server">
                            <table style="width:100%; text-align:left">
                                <tr>
                                    <td>Company Name <span style="font-size:1.5em; color:red">*</span></td>
                                    <td><asp:TextBox ID="txtCoName" runat="server" style="width:100%"></asp:TextBox></td>
                                </tr>
                                <tr>
                                    <td>Primary Contact Person (Your Name) <span style="font-size:1.5em; color:red">*</span></td>
                                    <td><asp:TextBox ID="txtContact" runat="server" style="width:100%"></asp:TextBox></td>
                                </tr>
                                 <tr>
                                     <td>Sage Accounting Login Name <span style="font-size:1.5em; color:red">*</span></td>
                                     <td><asp:TextBox ID="txtsagemail" runat="server" style="width:100%"></asp:TextBox></td>
                                 </tr>
                                <tr id="trSagePwd">
                                    <td>Sage Accounting Password <span style="font-size:1.5em; color:red">*</span></td>
                                    <td><asp:TextBox ID="txtSagePwd" runat="server" style="width:100%" TextMode="Password"></asp:TextBox></td>
                                </tr>
                                <%-- OAuth 2.0 hidden pending Sage app registration + UAT. Basic auth is the only option for now. --%>
                                <tr id="trSageAuth" runat="server" visible="false">
                                    <td>Sage Login Method</td>
                                    <td>
                                        <asp:DropDownList ID="DDSageAuth" runat="server" style="width:100%" ToolTip="How you sign in to Sage. OAuth 2.0 = you log in with a Sage Account (id.sage.com / 2FA).">
                                            <asp:ListItem Value="0" Text="Basic auth (Sage password)" Selected="True" />
                                            <asp:ListItem Value="1" Text="OAuth 2.0 (Sage Account)" />
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                                <tr id="trSageAuthNote" runat="server" visible="false">
                                    <td colspan="2" style="font-size:0.85em; text-align:right; color:#b35c00; background:#fff6e5; border-left:3px solid #e69500; padding:0.6em 0.8em; margin-bottom:1em">
                                        <b>Which one do I pick?</b> If Sage asks you for a code from your phone or an authenticator app when you log in,
                                        you have 2FA switched on &ndash; choose <b>OAuth 2.0 (Sage Account)</b>. If you log in with just your email and
                                        password, choose <b>Basic auth (Sage password)</b>.
                                    </td>
                                </tr>
                                <tr>
                                    <td>Billing Address1</td>
                                    <td><asp:TextBox ID="txtAdd1" runat="server" style="width:100%"></asp:TextBox></td>
                                </tr>
                                <tr>
                                    <td>Billing Address 2</td>
                                    <td><asp:TextBox ID="txtAdd2" runat="server" style="width:100%"></asp:TextBox></td>
                                </tr>
                                <tr>
                                    <td>Billing Address3</td>
                                    <td><asp:TextBox ID="txtAdd3" runat="server" style="width:100%"></asp:TextBox></td>
                                </tr>
                                <tr>
                                    <td>Billing Address Post Code</td>
                                    <td><asp:TextBox ID="txtAdd4" runat="server" style="width:100%"></asp:TextBox></td>
                                </tr>
                                <tr> 
                                    <td colspan="2" style="text-align:right"><asp:LinkButton ID="lbtnNext" runat="server" CssClass="icon fa-chevron-right buttonRed" OnClick="lbtnNext_Click"> Next</asp:LinkButton></td>
                                </tr>
                            </table>
                            </asp:Panel>
                              <asp:Panel ID="pnlTroubleshoot" runat="server" style="display:none">
                                  <table>
                                      <tr>
                                          <td><h2>Troubleshooting errors:</h2></td>
                                      </tr>
                                      <tr>
                                          <td>If you received a pop-up message saying "Unauthorised", it means the credentials provided do not have access to the "Add-On's" feature in Sage Accounting.<br /><br />
                                              Please follow the screenshot images below to authorise access as required.
                                          </td>
                                      </tr>
                                      <tr>
                                          <td>
                                              <a href="images/sbcacreds1.png" target="_blank"><img src="images\sbcacreds1.png" style="width:60em; max-width:90%" /></a>
                                              <br />
                                             <a href="images/sbcacreds2.png" target="_blank"><img src="images\sbcacreds2.png" style="width:60em; max-width:90%" /></a>
                                          </td>
                                      </tr>
                                      <tr>
                                          <td>If you received a different message, please mail us and let us know. </td>
                                      </tr>
                                  </table>
                                </asp:Panel>
                             <asp:Panel ID="PnlSelectCompany" runat="server" style="display:none">
                                 <table style="width:100%; text-align:left">
                                    <tr>
                                        <td width="200">Select Sage Company <span style="font-size:1.5em; color:red">*</span></td>
                                        <td><asp:DropDownList ID="DDCompany" runat="server" style="width:100%"></asp:DropDownList></td>
                                    </tr>
                                     <tr>
                                            <td width="200">Sage Consultant R Number</td>
                                            <td><asp:TextBox ID="txtConsultRNum" runat="server" style="width:100px"></asp:TextBox></td>
                                        </tr>
                                     <tr>
                                        <td>User Testing Mode</td>
                                        <td><asp:CheckBox ID="chkUAT" runat="server" Checked="true" Enabled="false" /></td>
                                    </tr>
                                      <tr>
                                            <td colspan="2" style="padding:2em"><h4>Warning:</h4> On initial enrollment, User Testing Mode is enabled by default. <br /> While in this mode, NO DATA is sent back to Sage Accounting.<br />All incoming data (PO's, SO's etc) will be in sync with Sage Accounting, but records in Sage will NOT BE UPDATED OR ADDED when transactions are completed in My Data Fusion.
                                                <br />
                                                This feature has been included to allow for user familiarisation and training.
                                                <br /> <br /> 
                                                It is advisable to start your learning in User Testing mode and "Go Live" when all users are fully trained. 
                                                <br /><br /> 
                                                Stock levels in My Data Fusion can easily be re-set synchronised with Sage Accounting on "Go Live".
                                                <br /><br /> 
                                                    This feature can be switch off in the settings features.
                                            </td>
                                        </tr>
                                     <tr>
                                         <td>I understand and accept the warning. <span style="font-size:1.5em; color:red">*</span></td>
                                         <td><asp:CheckBox ID="chkWarning" runat="server" /></td>
                                     </tr>
                                     <tr>
                                         <td><asp:LinkButton ID="lbtnBack2" runat="server" CssClass="icon fa-chevron-left buttonC" OnClick="lbtnBack2_Click" > Back</asp:LinkButton></td>
                                         <td style="text-align:right"><asp:LinkButton ID="lbtnNext2" runat="server" CssClass="icon fa-chevron-right buttonRed" OnClick="lbtnNext2_Click"> Next</asp:LinkButton></td>
                                     </tr>
                                     </table>
                                 </asp:Panel>
                            <asp:Panel ID="PnlAdds" runat="server" style="display:none">
                                <table style="width:100%; text-align:left">
                                   <tr>
                                       <td colspan="2" style="text-align:center">Additional Information <br />(These settings can be edited through the setting feature)<br /><br /></td>
                                   </tr>
                                    <tr>
                                       <td>Use Module 2 (Job Cards, Forecasting + more)</td>
                                       <td><asp:CheckBox ID="chkMod2" runat="server" Checked="false" Enabled="false" /></td>
                                   </tr>
                                    <tr>
                                       <td>Use Module 3 (BOM's, Kit's, Manufacturing + more)</td>
                                       <td><asp:CheckBox ID="chkMod3" runat="server" Checked="true" Enabled="false" /></td>
                                   </tr>
                                     <tr>
                                           <td>Use Lot Tracking</td>
                                           <td><asp:CheckBox ID="chkLotTrack" runat="server" /></td>
                                       </tr>
                                    <tr>
                                         <td>Unit Decimal Places</td>
                                         <td><asp:TextBox ID="txtDecPlaces" runat="server" TextMode="Number" Width="45">0</asp:TextBox></td>
                                     </tr>
                                    <tr>
                                       <td><asp:LinkButton ID="lbtnBack3" runat="server" CssClass="icon fa-chevron-left buttonC" OnClick="lbtnBack3_Click" > Back</asp:LinkButton></td>
                                        <td style="text-align:right"><asp:LinkButton ID="lbtnSave" runat="server" CssClass="icon fa-save buttonRed" OnClick="lbtnSave_Click" style="float:right"> Create Profile</asp:LinkButton></td>
                                    </tr>
                                    <tr>
                                        <td colspan="2">
                                            <asp:LinkButton ID="lblerr" runat="server" ForeColor="red"></asp:LinkButton></td>
                                    </tr>
                                    </table>
                                </asp:Panel>
                            <asp:Panel ID="PnlSuccess" runat="server" style="display:none">
                                <h2>Profile Successfully Created</h2>
                                <p>You have been sent an email with a verification code and an activation link.</p>
                                <p>Please copy the code, and follow the link to complete the enrolment process and activate your profile.</p>
                                <p>Once activated, the relevant data will be synchronised with Sage Online and you are ready to go.</p>
                                <p>You can close this tab/window.</p>
                                </asp:Panel>
                        </ div> 
                     <div class="3u 12u$(medium)">&nbsp;</div>
                    </div>
                    </ContentTemplate>
                    </asp:UpdatePanel>
                </div>
             <script type="text/javascript">
                 // OAuth = the user signs in at Sage, so the Sage password box is not needed: hide it.
                 // pageLoad re-runs after every UpdatePanel postback, so the row stays in step.
                 function toggleSagePwd() {
                     var dd = document.getElementById('<%= DDSageAuth.ClientID %>');
                     var tr = document.getElementById('trSagePwd');
                     if (dd && tr) tr.style.display = dd.value === '1' ? 'none' : '';
                 }
                 function pageLoad() {
                     var dd = document.getElementById('<%= DDSageAuth.ClientID %>');
                     if (dd) dd.onchange = toggleSagePwd;
                     toggleSagePwd();
                 }
             </script>
             <section id="footer" class="wrapper">
                     <a href="https://mydatafusion.online/learningCenter.aspx" class="button special icon fa-lightbulb" target="_blank"> Learn more from the Learning Hub >></a>
                 </section>
        </div>
    </form>
</body>
</html>
