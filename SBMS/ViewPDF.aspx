<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="ViewPDF.aspx.cs" Inherits="SBMS.ViewPDF" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>PDF View</title>
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
                            <div class="2u 12u$(medium)"><img src="images/logo.png" style="float: left" class="logoImg" /></div>
                            <div class="8u 12u$(medium)">
                                <asp:LinkButton ID="lbtnBack" runat="server" class="buttonC icon fa-chevron-circle-left" OnClick="lbtnBack_Click"> Back</asp:LinkButton>
                               <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" Style="float: right;" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                                <br />
                                <h3 style="padding-top: 0; line-height: 1em"><asp:Label ID="lblPDFHeader" runat="server" Text=""></asp:Label></h3>
                            </div>
                            <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /> </div>
                         </div>  
                
                <div class="row 150%">
                    <div class="1u 12u$(medium)">&nbsp;</div>
                    <div class="10u 12u$(medium)">
                            <div id="pnlViewDoc" runat="server" class="holder">
                                <iframe id="pnlpdfview" runat="server" class="doc" src="~/assets/images/redflag.png"></iframe>
                                 <asp:Literal ID="ltEmbed" runat="server" />
                                <div style="display:none"><asp:Label ID="lblOrigDocGuid" runat="server" Text=""></asp:Label><asp:Label ID="dcrid" runat="server" Text=""></asp:Label></div>
                            </div>
                         </ div> 
                    </div>
                <div class="1u 12u$(medium)">&nbsp;</div>
                </div>
        </div>
    </form>
</body>
</html>
