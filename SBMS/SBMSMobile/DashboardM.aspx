<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="DashboardM.aspx.cs" Inherits="SBMS.DashboardM" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Data Fusion</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="../images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="../images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="css/main.css" />
    <link href="../lib/toastr/toastr.min.css" rel="stylesheet" />
   <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script  type="text/javascript" src="../lib/toastr/toastr.min.js"></script>
    <script type="text/javascript" src="../scripts/notifications.js"></script>
</head>
<body>
    <form id="form1" runat="server">
    <div class="content"> 
     <div class="container">             
         <div class="row 150%">
             <div class="12u 12u$(medium)">
                 <a href="https://mydatafusion.online" title="My Data Fusion website"><img src="../images/logo.png" style="float:left" class="logoImg"/></a><br />
             </div>
             <div class="12u 12u$(medium)">
                <h3>Data Fusion Mobile</h3>
              </div>
             </div>
             <div class="row 150%">
             <div class="4u 12u$(medium)" style="text-align: center">&nbsp;</div>
             <div class="4u 12u$(medium)" style="text-align: center">
              <asp:LinkButton ID="imgbRec" runat="server" CssClass="button" OnClick="imgbRec_Click" ToolTip ="See outstanding Purchase Orders and carry out receiving process."><img src="../images/receivingM.png" alt="Click Here" class="image fit" /></asp:LinkButton>
              <asp:LinkButton ID="ibtnPickSlips" runat="server" CssClass="button" OnClick="ibtnPickSlips_Click"  ToolTip="See all open sales orders, and thier associated Picking slips or Job cards. Process you Sales Orders from here." ><img src="../images/PickSlipsM.png" alt="Click Here" class="image fit" /></asp:LinkButton>
                 <asp:LinkButton ID="lbtnBinTransfer" runat="server" CssClass="button" OnClick="ibtnPickSlips_Click"  ToolTip="See all open sales orders, and thier associated Picking slips or Job cards. Process you Sales Orders from here." ><img src="../images/BinTrfM.png" alt="Click Here" class="image fit" /></asp:LinkButton>
              <asp:LinkButton ID="ibtnStockMove" runat="server" CssClass="button" OnClick="ibtnPickSlips_Click"  ToolTip="See all open sales orders, and thier associated Picking slips or Job cards. Process you Sales Orders from here." ><img src="../images/WHTrf.png" alt="Click Here" class="image fit" /></asp:LinkButton>
              <asp:LinkButton ID="ibtnStockCount" runat="server" CssClass="button" OnClick="ibtnPickSlips_Click"  ToolTip="See all open sales orders, and thier associated Picking slips or Job cards. Process you Sales Orders from here." ><img src="../images/StockCountsM.png" alt="Click Here" class="image fit" /></asp:LinkButton>
             </div>
             <div class="4u 12u$(medium)" style="text-align: center">&nbsp;</div>
             </div>
         </div>
            </div>

        <div id="pwa-install-prompt" style="display: none; position: fixed; bottom: 20px; right: 20px; background: #3367D6; color: white; padding: 10px 15px; border-radius: 5px; z-index: 1000;">
    <p>Install our app for better experience!</p>
    <button onclick="installPWA()" style="background: white; color: #3367D6; border: none; padding: 5px 10px; border-radius: 3px; margin-right: 10px;">Install</button>
    <button onclick="document.getElementById('pwa-install-prompt').style.display='none'" style="background: transparent; color: white; border: 1px solid white; padding: 5px 10px; border-radius: 3px;">Later</button>
</div>

<script>
    // Show install prompt when available
    window.addEventListener('beforeinstallprompt', (e) => {
        e.preventDefault();
        deferredPrompt = e;
        document.getElementById('pwa-install-prompt').style.display = 'block';
    });
</script>

             </form>
</body>

    <script>
// PWA Install Prompt
let deferredPrompt;
const installButton = document.getElementById('installButton'); // Add this button to your page

window.addEventListener('beforeinstallprompt', (e) => {
    // Prevent Chrome 67 and earlier from automatically showing the prompt
    e.preventDefault();
    // Stash the event so it can be triggered later
    deferredPrompt = e;
    // Show your install button
    if(installButton) installButton.style.display = 'block';
});

// Installation handler
function installPWA() {
    if(deferredPrompt) {
        deferredPrompt.prompt();
        deferredPrompt.userChoice.then((choiceResult) => {
            if (choiceResult.outcome === 'accepted') {
                console.log('User installed the PWA');
            }
            deferredPrompt = null;
        });
    }
}

// Hide address bar on mobile
window.addEventListener('load', function() {
    setTimeout(function() {
        window.scrollTo(0, 1);
    }, 0);
});
    </script>
</html>
