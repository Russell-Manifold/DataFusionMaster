<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="Reporting.aspx.cs" Inherits="SBMS.Reporting" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Reporting</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
</head>
<body>
    <form id="form1" runat="server">
         <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="content">
            <div class="container">  
                <div class="row 150%">
                            <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>
                            <div class="8u 12u$(medium)">
                                <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" Onclick ="lbtnHome_Click"></asp:LinkButton>
                                <asp:LinkButton ID="LinkButton1" runat="server" CssClass="icon fa-chevron-left buttonTransparent" PostBackUrl="~/ProdPlanning.aspx" ToolTip="Print"> Production Planning</asp:LinkButton>
                                <asp:LinkButton ID="lbtnPrint" runat="server" CssClass="icon fa-archive buttonTransparent" PostBackUrl="~/ProductionRMD.aspx" ToolTip="Print"> Raw Materials Requirements</asp:LinkButton>
                                <asp:LinkButton ID="lbtnHistory" runat="server" class="buttonTransparent icon fa-download" ToolTip="Download Production.">&nbsp;Excel</asp:LinkButton>
                                <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" Style="float: right;" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                                <br />
                             </div>
                            <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                        <div class="row 150%">
                          <div class="12u 12u$(medium)">   
                              <iframe src="http://localhost:55060/SageCloudDIReportingC.aspx?userid=69543d0e-d8b4-416f-920f-9e7ec8458835"  class="iframe" height="900" ></iframe>

                             </div>
                            </div>
                    </div>
                </div>
            </div>
    </form>
     <script type="text/javascript">
        function triggerSaveOnEnter(event, saveButtonId) {
            if (event.key === "Enter") {
                event.preventDefault(); // Prevent the default form submission
                document.getElementById(saveButtonId).click(); // Trigger the click event on the save button
            }
        }
</script>
</body>

</html>
