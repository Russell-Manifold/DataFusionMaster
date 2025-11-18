<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="PickingSlipTrackStatic.aspx.cs" Inherits="SBMS.PickingSlipTrackStatic" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Picking Slip Tracking</title>
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
                    <div class="2u 12u$(medium)">
                        <img src="images/logo.png" style="float: left" class="logoImg" />
                    </div>
                    <div class="8u 12u$(medium)" style="padding-left:8em">
                        <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" Style="float: right;" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <br />
                        <h3 style="padding-top: 0; line-height: 1em"><span class="icon fa-list-alt" style="text-decoration:none"></span>Picking Slip Tracking</h3>
                       
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                </div>

                <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div class="row 150%">
                            <div class="12u 12u$(medium)" style="display: flex; justify-content: center;">
                                <div>
                                    <asp:Panel ID="Panel1" runat="server" style="text-align:center; overflow:auto">                        
                                    <div id="kanbanboard" runat="server" class="kanbanboard">
                                     <script type="text/javascript">
                                        var kanbanboardClientID = '<%= kanbanboard.ClientID %>';
                                    </script>
                                    </div>
                                        </asp:Panel>
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
