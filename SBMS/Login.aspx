<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="SBMS.Login"  %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Data Fusion</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    <style>
        /* Login page responsive overrides */
        /* No header bar on this page, so remove the space reserved for it */
        #page-wrapper {
            padding-top: 0;
        }
        .login-logo-row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            flex-wrap: wrap;
            padding: 0.15em 0;
        }
        /* Pull the login form's heading up tight against the logo row */
        .login-panel h2 {
            margin-top: 0.25em;
        }
        .login-logo-row .logoImg {
            float: none !important;
            max-height: 5em;
            max-width: 7em;
        }
        .login-logo-row .lbtnNewProfile-wrap {
            flex: 1;
            text-align: right;
        }
        .login-input {
            width: 100%;
            max-width: 22em;
            box-sizing: border-box;
        }
        .login-panel {
            padding: 1em;
        }
        @media screen and (max-width: 736px) {
            .login-logo-row {
                justify-content: center;
                gap: 0.5em;
            }
            .login-logo-row .lbtnNewProfile-wrap {
                width: 100%;
                text-align: center;
                margin-top: 0.5em;
            }
            .login-input {
                max-width: 100%;
                font-size: 1.1em;
                padding: 0.4em;
                height: auto;
                min-height: 2.6em;
            }
            .login-panel {
                padding: 0.5em 0.25em;
            }
            .login-col-center {
                padding-bottom: 2em !important;
            }
            .HellowWorldPopup {
                min-width: 0 !important;
                width: 95vw !important;
                max-width: 98vw !important;
                box-sizing: border-box;
            }
        }
        @media screen and (max-width: 480px) {
            .login-input {
                font-size: 1.2em;
                min-height: 3em;
            }
        }

        /* Login page: drastically shrink footer on mobile so the form is usable */
        @media screen and (max-width: 736px) {
            .login-footer {
                padding: 0.25em 0.25em !important;
                font-size: 0.5em !important;
                line-height: 1.2 !important;
            }
            .login-footer .button.special {
                font-size: 0.9em;
                padding: 0.3em 0.8em;
            }
            .login-footer #copyright ul:first-of-type {
                display: none;
            }
            .login-footer #copyright ul:last-of-type {
                margin-top: 0.15em;
            }
        }
        @media screen and (max-width: 480px) {
            .login-footer {
                padding: 0.15em 0.15em !important;
                font-size: 0.45em !important;
            }
            .login-footer #copyright ul:first-of-type {
                display: none;
            }
            .login-footer #copyright ul:last-of-type {
                margin-top: 0.1em;
            }
        }

        /* General size reduction for the whole login page on phones */
        @media screen and (max-width: 736px) {
            html { font-size: 13px !important; }
            body, input, select, textarea, button, .button { font-size: 1rem !important; }
            .login-panel h2 { font-size: 1.8rem; line-height: 1.2; }
            .login-input { min-height: 2.6em; padding: .45em .6em; }
            .buttonSage { font-size: 1rem; padding: .55em 1.2em; }
            .login-panel { font-size: 1rem; }
        }
        @media screen and (max-width: 480px) {
            html { font-size: 12px !important; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true"></asp:ScriptManager>
       <div id="page-wrapper">
        <div class="content">
           <div class="container"> 
                <div class="row 150%">
                            <div class="12u 12u$(medium)">
                                <div class="login-logo-row">
                                    <a href="https://mydatafusion.online" title="My Data Fusion website">
                                        <img src="images/logo.png" class="logoImg" />
                                    </a>
                                    <div class="lbtnNewProfile-wrap">
                                        <asp:LinkButton ID="lbtnNewProfile" runat="server" class="buttonC icon fa-edit" style="font-size:small" ToolTip="Create a new profile, (No Credit Card details required)" OnClick="lbtnNewProfile_Click">&nbsp;New Profile&nbsp;</asp:LinkButton>
                                    </div>
                                </div>
                            </div>
                    </div>
                 <div class="row 150%">
                     <div class="4u 12u$(medium)">&nbsp;<asp:HiddenField ID="hfScreenWidth" runat="server" /></div>
                        <div class="4u 12u$(medium) login-col-center" style="text-align:center; padding-bottom:9em">
                            <div id='myHiddenDiv' runat="server" style='display: none'>
                                    <div style="padding: .5em;">
                                    <img src="images/tenorwait.gif" id='myAnimatedImage' align='absmiddle' class="funkygif" style="border-radius:.5em"  />
                                   </div>
                            </div>
                            <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnlogin" CssClass="login-panel">
                                
                                <h2>Data Fusion <span style="font-size:.5em" >By Syncflo</span><br />Log In</h2>
                                Username: <br /><asp:TextBox ID="txtUsername" runat="server" CssClass="login-input" placeholder="Sage Login Username"></asp:TextBox><br />
                                Password: <br /><asp:TextBox ID="txtPwd" runat="server" CssClass="login-input" TextMode="Password" placeholder="Sage Password"></asp:TextBox><br />
                                <br /><asp:CheckBox ID="chkRememberMe" runat="server" Text="Keep me logged in today" /><br/>
                                <asp:LinkButton ID="lbtnlogin" runat="server" OnClick="lbtnlogin_Click" CssClass="icon fa-door-open buttonSage" >Login</asp:LinkButton><br />
                                <asp:Label ID="lblErr" runat="server" Text="" ForeColor="Red"></asp:Label><br /><br />
                                <a id="lnkSage" runat="server" visible="false" href="https://status.sage.com/" target="_blank" style="color:darkgreen" class="icon fa-chain-broken">Click here to check the status of Sage, <br /> This could be caused by a Sage outage.</a>
                            </asp:Panel>

                               <asp:Panel ID="PnlNewP" runat="server" Style="display: none">
                                <asp:LinkButton ID="lbtnCancelP" runat="server" CssClass="fa fa-times" Style="float: right" ToolTip="Cancel" OnClick="lbtnCancel_Click"> </asp:LinkButton>
                                <div class="HellowWorldPopup">
                                    <div id="Div44" class="PopupHeader">
                                        <h4>Your First Time Here - Welcome<asp:Label ID="lblTpe" runat="server" Text=""></asp:Label></h4>
                                    </div>
                                    <div class="PopupBody" style="text-align: center">
                                        <p>Before you start, lets get all your basic details,<br /> accounts and items synchronised with Sage Accounting.<br /> It will take a few minutes, but will be worth the wait.</p>
                                        <p>Should we proceed?</p>       
                                    </div>
                                    <div class="Controls">
                                        <input id="Button1" type="button" class="fa fa-times-circle" value="" runat="server" style="display: none" />
                                        <input id="btnOkayP" type="button" class="buttonYellow" value="OK" runat="server" style="display: none" />
                                        <asp:LinkButton ID="btnSaveYes" runat="server" CssClass="icon fa-save buttonCancel" OnClick="btnSaveYes_Click"  OnClientClick="showDiv()"> Yes, do it.</asp:LinkButton><br />
                                    </div>
                                </div>
                            </asp:Panel>                 
                            <asp:Panel ID="PnlNewUser" runat="server" style="display:none" >
                                <asp:LinkButton ID="lbtnPnlNewclose" runat="server" CssClass="icon fa-close" style="float:right" OnClick="lbtnPnlNewclose_Click"></asp:LinkButton>
                                <h3>Update complete: So whats been created? </h3>
                                <ul style="text-align:left; font-size:.8em">
                                    <li class="icon fa-check"> 4 default stores have been created.</li>
                                     <li class="icon fa-check"> Stock items, balances and pricing have been imported from Sage Accounting.</li>
                                     <li class="icon fa-check"> All items have been linked to all stores.</li>
                                    <li class="icon fa-check"> Items opening balance transactions completed.</li>
                                    <li class="icon fa-check"> Un-Invoiced Purchase Orders synchronised </li>
                                    <li  class="icon fa-check"> Un-Invoiced Sales Orders synchronised</li>
                                    <li class="icon fa-check"> 3 User roles, including Super User have been created</li>
                                    <li class="icon fa-check"> A default list of picking slip processes created.</li>
                                    <li class="icon fa-check"> A default lot number has been allocated to each item (if applicable).</li>
                                    <li class="icon fa-check"> Bundles have been imported from Sage Accounting.</li>
                                    <li class="icon fa-check"> GL Account codes (only) imported from Sage Accounting.</li>
                                </ul>
                             <h3>So what hasn't happened? </h3>
                                <ul style="text-align:left; font-size:.8em">
                                    <li  class="icon fa-close"> Default Job Card workflow processes (where applicable) -  these are configurable in the settings.</li>
                                    <li  class="icon fa-close"> BOM's and Kits have not been created..</li>
                                </ul>
                               <h5>** All the above are editable in the settings, unless otherwise specified.**</h5>
                                <br /><br />
                                <h3>What now?</h3>
                                <ul style="text-align:left; font-size:.8em">
                                        <li  class="icon fa-eye"> After logging in for the first time, please go to <img src="images/settings.PNG" height="30"/> and browse through all the various settings to familiaries yourself.</li>
                                        <li  class="icon fa-eye"> View your Purchase Orders and Sales Orders.</li>
                                        <li  class="icon fa-eye"> You may need to transfer items from the RM store >> Finished Goods Store for immediate picking</li>
                                    </ul>

                                 <h5>** Visit the Learning Hub for help in getting started.**</h5>
                                <asp:LinkButton ID="lbtnGoTo" runat="server" CssClass="icon fa-chevron-circle-right buttonSage" OnClick="lbtnPnlNewclose_Click"> Proceed to Log In</asp:LinkButton>
                            </asp:Panel>
                         </ div> 
                     <div class="4u 12u$(medium)">&nbsp;</div>
                    </div> 
                </div>
            <section id="footer" class="wrapper login-footer">
                <a href="https://mydatafusion.online/learningCenter.aspx" class="button special icon fa-lightbulb" target="_blank"> Learn more from the Learning Hub >></a>
            <div id="copyright">
				<ul class="copyright">
					<li><a href="documents/Privacy_Policy.pdf" target="_blank" title="View our Privacy Policy">Privacy Policy </a></li>
					<li><a href="documents/Terms_and_Conditions.pdf" target="_blank" title="View our Terms and Conditions">T n C's</a></li>
					<li><a href="documents/Cookie_Policy.pdf" target="_blank" title="View our Cookies Policy">Cookie Policy</a></li>
					<li><a href="documents/POPIA_Policy.pdf" target="_blank" title="View our POPIA Policy">POPIA Policy</a></li>
					<li><a href="documents/Disclaimer.pdf" target="_blank" title="View our Disclaimer">Disclaimer</a></li>
					<li><a href="documents/Copyright_Notice.pdf" target="_blank" title="View our Copyright Notice">Copyright Notice</a></li>
				</ul>
				<ul>
					<li>&copy; Syncflo. Design: <a href="https://syncflo.co.za" target="_blank">Syncflo (Pty) Ltd</a> & <a href="http://html5up.net" target="_blank">HTML5 UP</a></li>
				</ul>
                </div>
                </section>
            
            <asp:LinkButton ID="LinkButton1" runat="server" style="display:none">LinkButton</asp:LinkButton>    
            <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton1"></cci:ModalPopupExtender>
            <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                        <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                        <div class="HellowWorldPopup">
                            <div id="Div4" class="PopupHeader">
                                <h2>Multiple Login Detected.</h2>
                            </div>
                            <div class="PopupBody">
                                You are logged in, in a different session. <br />Log out and start a new session here? 
                            </div>
                            <div class="Controls">
                                <input id="btnCancel5" type="button" class="fa fa-times-circle" value="No" runat="server" style="display:none"/>
                                <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                    <asp:LinkButton ID="btnCancel" runat="server" CssClass="buttonRed"  > No, cancel this log in</asp:LinkButton>
                                <asp:LinkButton ID="btnLogUserOut" runat="server" CssClass="icon fa-thumbs-up buttonSage" OnClick="btnLogUserOut_Click"  > Yes, do it.</asp:LinkButton>
                            </div>
                        </div>
                    </asp:Panel>
          
            
            <asp:LinkButton ID="LinkButton2" runat="server" style="display:none">LinkButton</asp:LinkButton>    
                <cci:ModalPopupExtender ID="ModalSelectCompany" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="Button2" Drag="true" OkControlID="Button3" PopupControlID="PnlCompany" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton2"></cci:ModalPopupExtender>
                <asp:Panel ID="PnlCompany" runat="server" Style="display: none">
                            <asp:LinkButton ID="LinkButton3" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                            <div class="HellowWorldPopup">
                                <div id="Div424" class="PopupHeader">
                                    <h2>Select Company</h2>
                                </div>
                                <div class="PopupBody">
                                    You have multiple companies linked to your profile, <br /> please select the one required. <br />
                                    <asp:DropDownList ID="DDCompanyList" runat="server"></asp:DropDownList><br />
                                    <asp:Label ID="lblMCErr" runat="server" Text="" ForeColor="Red"></asp:Label>
                                </div>
                                <div class="Controls">
                                    <input id="Button2" type="button" class="fa fa-times-circle" value="No" runat="server" style="display:none"/>
                                    <input id="Button3" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                        <asp:LinkButton ID="LinkCoNo" runat="server" CssClass="buttonRed"  > No, cancel this log in</asp:LinkButton>
                                    <asp:LinkButton ID="LinkCoYes" runat="server" CssClass="icon fa-thumbs-up buttonSage" OnClick="LinkCoYes_Click"> Continue - Log In</asp:LinkButton>
                                </div>
                            </div>
                        </asp:Panel>
        </div>
           </div>
    </form>
    <script>
    function showDiv() {
        document.getElementById('myHiddenDiv').style.display = "";
        document.getElementById('PnlNewP').style.display = "none";
        setTimeout('document.images["myAnimatedImage"].src="images/tenorwait.gif"', 200);
    }
    </script>
    <script>
        window.onload = function () {
            document.getElementById('<%= hfScreenWidth.ClientID %>').value = screen.width;
        };
</script>
</body>
</html>

