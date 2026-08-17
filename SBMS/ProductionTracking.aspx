<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="ProductionTracking.aspx.cs" Inherits="SBMS.ProductionTracking" %>
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
    <script type="text/javascript" src="js/ProdPlankanbanscript.js"></script>
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
                        <asp:LinkButton ID="LinkButton1" runat="server" CssClass="icon fa-chevron-left buttonTransparent" PostBackUrl="~/ProdPlanning.aspx" ToolTip="Print"> Production Planning</asp:LinkButton>
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
                                    <span style="text-align:left"> From Process 1 ==> a WIP process : RM's removed from RM store and placed into WIP.</span>
                                   
                                     <span style="float:right">From WIP Process ==> Ready To Invoice : RM's removed from WIP + FG added to Co Store.</span>
                                    <div id="kanbanboard" runat="server" class="kanbanboard">
                                        <!-- Job columns will be dynamically added here -->
                                    </div>
                                </div>
                            </div>
                        </div>
                         </ContentTemplate>
                </asp:UpdatePanel>
                        <asp:Button ID="btnShowModal" runat="server" Text="Show Modal" Style="display: none;" />
                        <!-- Modal HTML -->
                            <div class="modal fade" id="quantityModal" tabindex="-1" role="dialog" aria-labelledby="quantityModalLabel" aria-hidden="true">
                                <div class="modal-dialog" role="document">
                                    <div class="modal-content">
                                        <div class="modal-header">
                                           <h4 class="modal-title" id="quantityModalHLabel">Move Items To New Workflow Process</h4>
                                           </div>
                                        <div class="modal-body">
                                            <h5 class="modal-title" id="quantityModalLabel">Approved Quantity Sent</h5>
                                            <asp:TextBox ID="txtQuantity" runat="server" CssClass="form-control" placeholder="Approved" style="text-align:center; width:6em"></asp:TextBox>
                                            <h5 class="modal-title" id="quantityModalRLabel">Number of Rejects</h5>
                                            <asp:TextBox ID="txtRejQuantity" runat="server" CssClass="form-control" placeholder="Rejects" style="text-align:center;  width:6em"></asp:TextBox>
                                             <asp:HiddenField ID="hiddenJobId" runat="server" ClientIDMode="Static" />
                                             <asp:HiddenField ID="newWsID" runat="server" ClientIDMode="Static" />
                                        </div>
                                        <div class="modal-footer">
                                            <asp:LinkButton ID="btnSaveQuantity" runat="server" Text=" Save" CssClass="buttonSage icon fa-save" OnClick="btnSaveQuantity_Click"  />
                                            <asp:LinkButton ID="lbtnCancel" runat="server" class="buttonTransparent icon fa-times" OnClick="lbtnCancel_Click"> Cancel</asp:LinkButton>
                                        </div>
                                    </div>
                                </div>
                   </div>
            </div>
        </div>
    </form>
</body>
</html>
