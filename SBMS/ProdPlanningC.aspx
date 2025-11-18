<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ProdPlanningC.aspx.cs" Inherits="SBMS.ProdPlanningC" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Production Planning</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <meta name="viewport" content="width=device-width, initial-scale=1"/>
        <link href="assets/css/main.css" rel="stylesheet" />
        <noscript><link rel="stylesheet" href="assets/css/noscript.css" /></noscript>
        <script  type="text/javascript" src="lib/jquery/dist/jquery.js"></script>
        <script  type="text/javascript" src="lib/jquery/dist/jquery.min.js"></script>
        <script  type="text/javascript" src="lib/jqueryui/jquery-ui.min.js"></script>
        <script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/PapaParse/4.1.2/papaparse.min.js"></script>

         <script  type="text/javascript" src="https://cdn.plot.ly/plotly-basic-latest.min.js"></script>
         <script  type="text/javascript" src="pivot/pivot.js"></script>
        <script  type="text/javascript" src="pivot/plotly_renderers.js"></script>
         <link href="pivot/pivot.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
      <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="content">
                    <div class="container">
                        <div class="row 150%">
                            <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>
                            <div class="8u 12u$(medium)" style="padding-left:8em">
                                <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" onclick="lbtnHome_Click"></asp:LinkButton>
                                <asp:LinkButton ID="lbtnRefresh" runat="server" CssClass="icon fa-refresh buttonTransparent" OnClick="lbtnRefresh_Click" ToolTip="Refresh Items opening balances with Sage"> Refresh</asp:LinkButton>
                                 <asp:LinkButton ID="lbtnPrint" runat="server" CssClass="icon fa-pencil-square buttonTransparent" PostBackUrl="~/Production.aspx" ToolTip="Print"> Production</asp:LinkButton>
                                <asp:LinkButton ID="LinkButton1" runat="server" CssClass="icon fa-archive buttonTransparent" PostBackUrl="~/ProductionRMD.aspx" ToolTip="Print"> Raw Materials Requirments</asp:LinkButton>
                                <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" Style="float: right;" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                                <br />
                                <h3 style="padding-top: 0; line-height: 1em">MRP: Finished Items Demands & Production Planning</h3>
                                <span id="btnProdPlan" class="icon fa-arrow-circle-left" onclick="openNav()" style="font-size:1em;cursor:pointer; color:#4282C1" title="Open Production Plan Windows to Add/Update."> &#9776; Update Production Plan.</span> 
                                <asp:CheckBox ID="chkJC" runat="server" style="float:right" Text=" " Checked="true" /><asp:Label ID="Label1" runat="server" Text="Exclude Job Cards" style="float:right"></asp:Label>
                            </div>
                            <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                        <div class="row 150%">
                            <div class="12u 12u$(medium)">
                                         <div id="mySidenav" class="sidenav" style="width:340px">
                                  <a href="javascript:void(0)" class="closebtn" onclick="closeNav()">&times</a>
                                     <asp:LinkButton ID="lbtnDwnLToExcel" runat="server" class="icon fa-print xlbtn" ToolTip="Download Production Plan." OnClick="lbtnDwnLToExcel_Click">&nbsp;Excel</asp:LinkButton><br />
                                             <h2>Production Planning</h2>
                                 <asp:GridView ID="GridPPLines" runat="server" AutoGenerateColumns="false" CssClass="gridview" RowStyle-Wrap="true" OnRowDataBound="GridPPLines_RowDataBound" style="font-size:1em" >
                                    <HeaderStyle CssClass="gridViewHeader" />
                                            <RowStyle CssClass="gridViewRow" />
                                            <AlternatingRowStyle CssClass="gridViewAltRow" />
                                            <FooterStyle CssClass="gridViewHeader" />
                                            <PagerStyle CssClass="gridViewPager" />
                                            <Columns>
                                                 <asp:TemplateField HeaderText="Date" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="6em">
                                                    <ItemTemplate>
                                                        <asp:TextBox ID="txtDate" runat="server" Width="6em" style="text-align:center"  Text='<%# String.Format("{0:dd MMM yy}",  Eval("PlanDate"))%>' ></asp:TextBox>
                                                        <cci:CalendarExtender ID="CalendarExtender1" runat="server" Enabled="True" TargetControlID="txtDate" Format="dd MMM yy"></cci:CalendarExtender>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="Item" ItemStyle-Width="5em">
                                                    <ItemTemplate>
                                                        <asp:DropDownList ID="DDItemCode" runat="server" style="width:5em">
                                                          <asp:ListItem Value="0">-Item Code-</asp:ListItem>
                                                          </asp:DropDownList>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                 <asp:TemplateField HeaderText="Qty" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" ItemStyle-Width="4em">
                                                    <ItemTemplate>
                                                        <asp:TextBox ID="txtQty" runat="server" Width="4em" style="text-align:center" Text='<%# String.Format("{0:N1}" , Eval("PlanQuantity")) %>' onkeydown='<%# "triggerSaveOnEnter(event, \"" + ((GridViewRow)Container).FindControl("lbtnLineSave").ClientID + "\")" %>'></asp:TextBox>
                                                        <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtQty" FilterType="Custom, Numbers" ValidChars="." />
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                 
                                                <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="1em" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnLineSave" CommandArgument='<%# Eval("LineID") %>' CommandName="lbtnLineSave" runat="server" CssClass="fa fa-save" style="font-size:1em" ToolTip="Save Production Plan line" OnClick="lbtnLineSave_Click"> </asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="1em" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnDeleteLine" CommandArgument='<%# Eval("LineID") %>' CommandName="lbtnLineSave" runat="server" CssClass="fa fa-ban" style="font-size:1em; color:red" ToolTip="Delete Prod Plan Line" OnClick="lbtnDeleteLine_Click"> </asp:LinkButton>
                                                         <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Delete this line? Are you sure?" Enabled="True" TargetControlID="lbtnDeleteLine"></cci:ConfirmButtonExtender>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                 <asp:BoundField HeaderText="ID" DataField="LineID" ReadOnly="True"  />
                                            </Columns>
                                     </asp:GridView>
                            </div>
                            <div id="pivotoutput" style="overflow:auto;"></div>      
                                </div>
                        </div>
                       <asp:Literal ID="Literal1" runat="server"></asp:Literal>
                    </div>
</div>
            </div>
    </form>
    <script>
        function openNav() {
            document.getElementById("mySidenav").style.width = "340px";
            document.getElementById("btnProdPlan").style.display = "none";
        }

        function closeNav() {
            document.getElementById("mySidenav").style.width = "0";
            document.getElementById("btnProdPlan").style.display = "inline-block";
        }
    </script>
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
