<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="ProductionTrackingStatic.aspx.cs" Inherits="SBMS.ProductionTrackingStatic" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Production Tracking</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <!-- CSS styles -->
    <link rel="stylesheet" href="assets/css/main.css" />
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js"></script>
    <script type="text/javascript" src="scripts/PickSlipRefresh.js"></script>
</head>
<body>
    <form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true"></asp:ScriptManager>
        <div class="content">
            <div class="container">
                <div class="row 150%">
                   <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>
                    <div class="8u 12u$(medium)" style="padding-left:8em">
                        <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click"></asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" Style="float: right;" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <br />
                        <h3 style="padding-top: 0; line-height: 1em"><span class="fa fa-boxes" style="text-decoration:none"></span>Production Tracking</h3>
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                </div>
                <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div class="row 150%">
                            <div class="12u 12u$(medium)" style="display: flex; justify-content: center;">
                                <div>
                                    <div id="kanbanboard" runat="server" class="kanbanboard">
                                       <script type="text/javascript">
                                        var kanbanboardClientID = '<%= kanbanboard.ClientID %>';
                                    </script>
                                    </div>
                                </div>
                            </div>
                        </div>
                         </ContentTemplate>
                </asp:UpdatePanel>                  
            </div>
        </div>
    </form>
</body>
</html>
