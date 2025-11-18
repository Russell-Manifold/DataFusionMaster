<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="DataFusionOnboardFinish.aspx.cs" Inherits="SBMS.DataFusionOnboardFinish" %>
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
                                <h4>Verifying You Profile</h4>
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
                        <div class="4u 12u$(medium)" style="text-align:center; padding-bottom:7em">                          
                                <p>To complete your profile, please enter your unique ID.</p>
                                    <asp:TextBox ID="txtguid" runat="server" style="width:100%; text-align:center"></asp:TextBox>
                                <hr />
                            <div style="font-size:0.8em">
                                <h3>My Data Fusion – User Agreement & Disclaimer</h3>
                             <p>By signing up to use My Data Fusion ("the Platform"), you acknowledge and agree to the following terms:</p>

                                <p><h4>1. Data Accuracy & Liability</h4> <br />
                                    My Data Fusion integrates with Sage Accounting (Online) to facilitate multiple stores, inventory control, manufacturing processes, and reporting. While we endeavor to ensure that data is accurately transferred between My Data Fusion and Sage Accounting, we do not and cannot guarantee the accuracy, completeness, or reliability of any data within either system.</p>

                                    Syncflo (Pty) Ltd, the owner and operator of My Data Fusion, cannot be held liable for:<br />
                                    Any discrepancies, errors, or inaccuracies in data, regardless of their origin.<br />
                                    Any financial, operational, or business decisions made based on data within My Data Fusion or Sage Accounting.<br />
                                    Any delays, system failures, or interruptions that may impact data synchronization.<br />
                                    It is the user’s responsibility to verify data integrity and take appropriate measures to ensure compliance with their own financial and operational requirements.</p>

                                    <p><h4>2. Data Security & Privacy</h4>
                                    My Data Fusion is committed to securing your data through industry-standard security protocols, including:<br />
                                    Encryption of data during transmission and storage.<br />
                                    Access control measures to protect against unauthorized access.<br />
                                    Regular security updates to maintain system integrity.</br></br>
                                    However, no system is completely immune to risks. By using My Data Fusion, you acknowledge that Syncflo (Pty) Ltd is not responsible for unauthorized access, breaches, or data loss due to circumstances beyond our control. Users are encouraged to implement their own security measures, including strong passwords and role-based access controls.</p>

                                    <p><h4>3. Acceptance of Terms </h4> 
                                    By proceeding with registration and using My Data Fusion, you confirm that you have read, understood, and agreed to this disclaimer. If you do not agree with these terms, you should not use the platform.</p>
                            <br />
                                <div style="text-align:center; width:100%">
                             I Agree : <span style="font-size:1.5em; color:red">*</span> <asp:RadioButtonList ID="rbagree" runat="server" style="margin:auto">
                                  <asp:ListItem>Yes</asp:ListItem><asp:ListItem>No</asp:ListItem>
                                      </asp:RadioButtonList>
                                    </div>
                              <asp:LinkButton ID="lbtnSave" runat="server" CssClass="icon fa-save buttonRed" OnClick="lbtnSave_Click" style="float:right"> Complete Profile</asp:LinkButton>
                                <asp:Label ID="lblSuccess" runat="server" Text="" ForeColor="Red" Font-Size="Large"></asp:Label><br />
                              <asp:LinkButton ID="lbtnlogin" runat="server" CssClass="icon fa-door-open buttonRed" PostBackUrl="~/Login.aspx" style="display:none"> Go To Login</asp:LinkButton>
                                </div>
                        </ div> 
                     <div class="4u 12u$(medium)">&nbsp;</div>
                    </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>
        </div>
    </form>
</body>
</html>
