<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="SBMS.Login"  %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Data Fusion</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <%-- bump ?v= whenever df-theme.css / df-ui.js change, so browsers don't serve a stale copy --%>
    <link rel="stylesheet" href="assets/css/df-theme.css?v=13" />
     <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    <script src="assets/js/df-ui.js?v=13"></script>
</head>
<body class="df-page">
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true"></asp:ScriptManager>

        <div class="df-auth">
            <div class="df-auth-bar">
                <div class="df-auth-bar-spacer"></div>
                <button type="button" class="df-icon-btn" id="dfThemeToggle" title="Toggle dark mode" aria-label="Toggle dark mode"><i class="icon fa-moon-o" id="dfThemeIcon"></i></button>
                <asp:LinkButton ID="lbtnNewProfile" runat="server" CssClass="df-btn" ToolTip="Create a new profile, (No Credit Card details required)" OnClick="lbtnNewProfile_Click"><i class="icon fa-edit"></i>&nbsp;New Profile</asp:LinkButton>
            </div>

            <div class="df-auth-body">
                <div>
                    <div id='myHiddenDiv' runat="server" style='display: none' class="df-loading-overlay">
                        <div class="df-loading-box">
                            <img src="images/tenorwait.gif" id='myAnimatedImage' align='absmiddle' class="df-loading-spinner" />
                        </div>
                    </div>

                    <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnlogin" CssClass="df-auth-card">
                        <div class="df-auth-brand">
                            <a href="https://mydatafusion.online" title="My Data Fusion website">
                                <img src="images/logo.png" class="df-auth-logo" alt="Data Fusion" /></a>
                            <h2 class="df-auth-title">Log In<span class="df-auth-sub">Data Fusion by Syncflo</span></h2>
                        </div>

                        <div class="df-field">
                            <asp:Label runat="server" CssClass="df-label" AssociatedControlID="txtUsername" Text="Username"></asp:Label>
                            <asp:TextBox ID="txtUsername" runat="server" CssClass="df-input-full" placeholder="Sage Login Username"></asp:TextBox>
                        </div>

                        <div class="df-field">
                            <asp:Label runat="server" CssClass="df-label" AssociatedControlID="txtPwd" Text="Password"></asp:Label>
                            <asp:TextBox ID="txtPwd" runat="server" CssClass="df-input-full" TextMode="Password" placeholder="Sage Password"></asp:TextBox>
                        </div>

                        <div class="df-check-row">
                            <asp:CheckBox ID="chkRememberMe" runat="server" Text="Keep me logged in today" />
                        </div>

                        <asp:LinkButton ID="lbtnlogin" runat="server" OnClick="lbtnlogin_Click" CssClass="df-btn-block"><i class="icon fa-sign-in"></i>Login</asp:LinkButton>

                        <asp:Label ID="lblErr" runat="server" Text="" ForeColor="Red" CssClass="df-auth-msg"></asp:Label>
                        <a id="lnkSage" runat="server" visible="false" href="https://status.sage.com/" target="_blank" class="icon fa-chain-broken df-auth-msg" style="color:darkgreen">&nbsp;Click here to check the status of Sage. This could be caused by a Sage outage.</a>
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

                    <asp:Panel ID="PnlNewUser" runat="server" style="display:none" CssClass="df-auth-card df-auth-wide">
                        <asp:LinkButton ID="lbtnPnlNewclose" runat="server" CssClass="icon fa-close" style="float:right" OnClick="lbtnPnlNewclose_Click"></asp:LinkButton>
                        <h3 class="df-auth-title">Update complete: So whats been created?</h3>
                        <ul class="df-auth-list">
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
                     <h3 class="df-auth-title">So what hasn't happened?</h3>
                        <ul class="df-auth-list">
                            <li  class="icon fa-close"> Default Job Card workflow processes (where applicable) -  these are configurable in the settings.</li>
                            <li  class="icon fa-close"> BOM's and Kits have not been created..</li>
                        </ul>
                       <h5>** All the above are editable in the settings, unless otherwise specified.**</h5>
                        <br />
                        <h3 class="df-auth-title">What now?</h3>
                        <ul class="df-auth-list">
                                <li  class="icon fa-eye"> After logging in for the first time, please go to <img src="images/settings.PNG" height="30"/> and browse through all the various settings to familiaries yourself.</li>
                                <li  class="icon fa-eye"> View your Purchase Orders and Sales Orders.</li>
                                <li  class="icon fa-eye"> You may need to transfer items from the RM store >> Finished Goods Store for immediate picking</li>
                            </ul>

                         <h5>** Visit the Learning Hub for help in getting started.**</h5>
                        <asp:LinkButton ID="lbtnGoTo" runat="server" CssClass="df-btn-block" OnClick="lbtnPnlNewclose_Click"><i class="icon fa-chevron-circle-right"></i>Proceed to Log In</asp:LinkButton>
                    </asp:Panel>
                </div>
            </div>

            <div class="df-auth-footer">
                <a href="https://mydatafusion.online/learningCenter.aspx" class="df-btn" target="_blank"><i class="icon fa-lightbulb-o"></i>&nbsp;Learn more from the Learning Hub</a>
                <ul class="df-policy-links" style="margin-top:1rem">
                    <li><a href="documents/Privacy_Policy.pdf" target="_blank" title="View our Privacy Policy">Privacy Policy</a></li>
                    <li><a href="documents/Terms_and_Conditions.pdf" target="_blank" title="View our Terms and Conditions">T n C's</a></li>
                    <li><a href="documents/Cookie_Policy.pdf" target="_blank" title="View our Cookies Policy">Cookie Policy</a></li>
                    <li><a href="documents/POPIA_Policy.pdf" target="_blank" title="View our POPIA Policy">POPIA Policy</a></li>
                    <li><a href="documents/Disclaimer.pdf" target="_blank" title="View our Disclaimer">Disclaimer</a></li>
                    <li><a href="documents/Copyright_Notice.pdf" target="_blank" title="View our Copyright Notice">Copyright Notice</a></li>
                </ul>
                <div>&copy; Syncflo. Design: <a href="https://syncflo.co.za" target="_blank">Syncflo (Pty) Ltd</a> &amp; <a href="http://html5up.net" target="_blank">HTML5 UP</a></div>
            </div>

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
    </form>
    <script>
    function showDiv() {
        document.getElementById('myHiddenDiv').style.display = "";
        document.getElementById('PnlNewP').style.display = "none";
        setTimeout('document.images["myAnimatedImage"].src="images/tenorwait.gif"', 200);
    }
    </script>
</body>
</html>
