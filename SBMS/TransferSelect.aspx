<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="TransferSelect.aspx.cs" Inherits="SBMS.TransferSelect" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Transfer Select</title>
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
                                <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click" >&nbsp;&nbsp;</asp:LinkButton>
                                <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right;" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                                <h3 style="padding-top:0; line-height:1em">Items Movement History Report Select</h3>
                             </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                    </div>
                <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                    <ContentTemplate>
                        <div class="row 150%">
                             <div class="4u 12u$(medium)" style="text-align:center">&nbsp;</div>
                             <div class="4u 12u$(medium)" style="text-align:center">
                             <h4><asp:Label ID="lblerr" runat="server" Text=" " ForeColor="Red"></asp:Label></h4>   
                             <table style="width:100%; text-align:left">
                                 <tr>
                                     <td style="width:200px">Item</td>
                                     <td><asp:DropDownList ID="DDFromItem" runat="server" style="width:100%" ></asp:DropDownList>
                                     </td>
                                 </tr>
                                 <tr>
                                        <td colspan="2"><hr /></td>
                                    </tr>
                                <tr>
                                     <td>Category</td>
                                     <td><asp:DropDownList ID="DDCategory" runat="server"  style="width:100%"></asp:DropDownList>
                                     </td>
                                 </tr>
                                 <tr>
                                     <td colspan="2"><hr /></td>
                                 </tr>
                                <tr>
                                     <td>Out From Store</td>
                                     <td><asp:DropDownList ID="DDFrmStore" runat="server" Width="150px" ></asp:DropDownList>
                                     </td>
                                 </tr>
                                <tr>
                                     <td>Into Store</td>
                                     <td><asp:DropDownList ID="DDToStore" runat="server" Width="150px"></asp:DropDownList>
                                     </td>
                                 </tr>
                                 <tr>
                                        <td colspan="2"><hr /></td>
                                    </tr>
                                 <tr>
                                         <td>Transaction Type</td>
                                         <td><asp:DropDownList ID="DDType" runat="server" Width="150px"></asp:DropDownList></td>
                                </tr>
                                 <tr>
                                     <td colspan="2"><hr /></td>
                                 </tr>
                                 <tr>
                                 <td>From Date</td>
                                 <td><asp:TextBox ID="txtDtFrom" runat="server" Width="150px"></asp:TextBox>
                                     <cci:CalendarExtender ID="CalendarExtender1" runat="server" Enabled="True" TargetControlID="txtDtFrom" Format="dd MMM yyyy"></cci:CalendarExtender>
                                 </td>
                             </tr>
                             <tr>
                                 <td>To Date</td>
                                 <td><asp:TextBox ID="txtDtTo" runat="server" Width="150px"></asp:TextBox>
                                     <cci:CalendarExtender ID="CalendarExtender2" runat="server" Enabled="True" TargetControlID="txtDtTo" Format="dd MMM yyyy"></cci:CalendarExtender>
                                 </td>
                             </tr>
                             <tr>
                                  <td colspan="2"><hr /></td>
                              </tr>
                                 <tr>
                                     <td></td>
                                     <td><asp:LinkButton ID="lbtnViewTrf" runat="server" CssClass="buttonIndex" OnClick="lbtnViewTrf_Click">View</asp:LinkButton></td>     
                                 </tr>

                             </table>
                             
                             
                             
                             
                             </div>
                            

                             <div class="4u 12u$(medium)" style="text-align:center">&nbsp;</div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
        </div>
    </form>
</body>
</html>
