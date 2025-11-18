<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="Production.aspx.cs" Inherits="SBMS.Production" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Production</title>
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
                                <h3 style="padding-top: 0; line-height: 1em">Record Production</h3>
                                <div style="text-align:center">
                                    <span style="font-size:.8em">(All completed Production is automatically saved in the INT Store until transfered out)</span><br />
                                Show <asp:DropDownList ID="ddProdPlanWeeks" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddProdPlanWeeks_SelectedIndexChanged" style="text-align:center">
                                    <asp:ListItem>- All -</asp:ListItem>
                                    <asp:ListItem>1</asp:ListItem>
                                    <asp:ListItem>2</asp:ListItem>
                                    <asp:ListItem>3</asp:ListItem>
                                    <asp:ListItem>4</asp:ListItem>
                                    <asp:ListItem>8</asp:ListItem>
                                </asp:DropDownList> Weeks
                            </div>
                             </div>
                            <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" /></div>
                        <div class="row 150%">
                          <div class="12u 12u$(medium)">   
                             
                              <asp:LinkButton ID="LbtnSaveProd" runat="server" style="float:right; font-size:1em; width:15em; margin-bottom:1em" CssClass="icon fa-save buttonIndex" OnClick="LbtnSaveProd_Click"  ToolTip="On saving production, all lines marked as complete will be transfered to INT Store, and the records removed from the production plan."> SAVE PRODUCTION</asp:LinkButton><br />
                                <cci:ConfirmButtonExtender ID="LbtnSaveProd_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Save All Complete Production Records." Enabled="True" TargetControlID="LbtnSaveProd"></cci:ConfirmButtonExtender>

                               <span>Output production will PULL raw materials from WIP, count the stock down and ADD Finished goods stock. (Oldest lot number RM's will automatically be consumed first)</span>
                                <asp:GridView ID="GridProdPlan" runat="server" AutoGenerateColumns="false" CssClass="gridview" RowStyle-Wrap="true" OnRowDataBound="GridProdPlan_RowDataBound" style="font-size:1em" >
                                    <HeaderStyle CssClass="gridViewHeader" />
                                            <RowStyle CssClass="gridViewRow" />
                                            <AlternatingRowStyle CssClass="gridViewAltRow" />
                                            <FooterStyle CssClass="gridViewHeader" />
                                            <PagerStyle CssClass="gridViewPager" />
                                            <Columns>
                                               <asp:BoundField HeaderText="Code" DataField="ItemCode" ReadOnly="True" ItemStyle-Width="5em"  />
                                               <asp:BoundField HeaderText="Description" DataField="ItemDescription" ReadOnly="True" ItemStyle-Width="20em"  />
                                               <asp:BoundField HeaderText="Plan Qty" DataField="PlanQuantity" ReadOnly="True" ItemStyle-Width="5em" DataFormatString="{0:N1}" ItemStyle-HorizontalAlign="Center" ItemStyle-BackColor="Wheat"  />
                                               <asp:BoundField HeaderText="Plan Date" DataField="PlanDate" ReadOnly="True" ItemStyle-Width="7em" DataFormatString="{0:dd MMM yyyy}"  />
                                              <asp:TemplateField HeaderText="Prod Date" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="6em">
                                                    <ItemTemplate>
                                                        <asp:TextBox ID="txtDate" runat="server" Width="6em" style="text-align:center"  Text='<%# String.Format("{0:dd MMM yy}",  Eval("ActualDate"))%>' ></asp:TextBox>
                                                        <cci:CalendarExtender ID="CalendarExtender1" runat="server" Enabled="True" TargetControlID="txtDate" Format="dd MMM yy"></cci:CalendarExtender>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="Qty" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="5em">
                                                    <ItemTemplate>
                                                        <asp:TextBox ID="txtActQty" runat="server" Width="5em" style="text-align:center" 
                                                Text='<%# String.Format("{0:N1}", Eval("ActualQuantity")) %>' onkeydown='<%# "triggerSaveOnEnter(event, \"" + ((GridViewRow)Container).FindControl("lbtnLineSave").ClientID + "\")" %>'></asp:TextBox>
                                                        <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtActQty" FilterType="Custom, Numbers" ValidChars="." />
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                 <asp:TemplateField HeaderText="Lot Number" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="11em">
                                                    <ItemTemplate>
                                                         <asp:TextBox ID="txtLotNum" runat="server" style="text-align:center; width:9em"  MaxLength="15"
                                                Text='<%# Eval("ProdLotNum") %>' onkeydown='<%# "triggerSaveOnEnter(event, \"" + ((GridViewRow)Container).FindControl("lbtnLineSave").ClientID + "\")" %>'></asp:TextBox><asp:LinkButton ID="lbtnLotGen" runat="server" CommandArgument='<%# Eval("LineID") %>' ToolTip="Generate Lot Number" CssClass="icon fa-gg-circle" OnClick="lbtnLotGen_Click"></asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                  <asp:TemplateField HeaderText="Comments" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                                    <ItemTemplate>
                                                         <asp:TextBox ID="txtComments" runat="server" style="text-align:left; width:100%"  MaxLength="400"
                                                Text='<%# Eval("Comments") %>' onkeydown='<%# "triggerSaveOnEnter(event, \"" + ((GridViewRow)Container).FindControl("lbtnLineSave").ClientID + "\")" %>'></asp:TextBox>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                
                                                <asp:TemplateField HeaderText="Reject" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="4em">
                                                    <ItemTemplate>
                                                         <asp:TextBox ID="txtReject" runat="server" style="text-align:center; width:4em"  Text='<%# String.Format("{0:N1}",  Eval("RejectQty")) %>' ></asp:TextBox>
                                                        <cci:FilteredTextBoxExtender ID="ftbe2" runat="server" TargetControlID="txtReject" FilterType="Custom, Numbers" ValidChars="." />
                                                    </ItemTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Compl" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                                    <ItemTemplate>
                                                        <asp:CheckBox ID="chkComplete" runat="server" Text=" " Checked='<%# Eval("ProdComplete") %>' />
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="3em" >
                                                    <ItemTemplate>
                                                         <asp:LinkButton ID="lbtnLineSave" CommandArgument='<%# Eval("LineID") %>' CommandName="lbtnLineSave" runat="server" CssClass="fa fa-save buttonRed" ToolTip="Save Production" OnClick="lbtnLineSave_Click"> </asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                
                                            </Columns>
                           </asp:GridView>       
                        </ div> 
                    </div>
                </div>
                </div>
        </div>
        <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="lbtnCancel" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="lbtnHistory"></cci:ModalPopupExtender>
            <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                        <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                        <div class="HellowWorldPopup">
                            <div id="Div4" class="PopupHeader">
                                <h3>Download Production</h3>
                            </div>
                            <div class="PopupBody" style="text-align:center">
                                From Date <asp:TextBox ID="txtFromDate" runat="server" style="text-align:center" ></asp:TextBox><br />
                                <cci:CalendarExtender ID="CalendarExtender2" runat="server" Enabled="True" TargetControlID="txtFromDate" Format="dd MMM yy"></cci:CalendarExtender>
                                <h4>Include</h4>
                                <asp:RadioButtonList ID="RBIncl" runat="server" TextAlign="Right" RepeatDirection="Vertical" style="width:100%">
                                            <asp:ListItem Selected="True">- ALL - &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;</asp:ListItem>
                                            <asp:ListItem>Planned Only &nbsp;</asp:ListItem>
                                            <asp:ListItem>Complete Only</asp:ListItem>
                                            </asp:RadioButtonList>   
                               </div>
                            <div class="Controls">
                                <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                <asp:LinkButton ID="btnDownloadToExcel" runat="server" CssClass="icon fa-thumbs-up buttonCancel" OnClick="btnDownloadToExcel_Click"> Download</asp:LinkButton>
                            </div>
                        </div>
                    </asp:Panel>
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
