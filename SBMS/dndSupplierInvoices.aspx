<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="dndSupplierInvoices.aspx.cs" Inherits="SBMS.dndSupplierInvoices" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Supplier Invoice Drop</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
     <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
     <link href="https://cdnjs.cloudflare.com/ajax/libs/toastr.js/latest/toastr.min.css" rel="stylesheet"/>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/toastr.js/latest/toastr.min.js"></script>
    <script type="text/javascript" src="assets/js/upload.js"></script>
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
                                   <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right;" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;Log Out</asp:LinkButton>
                                     <br /><br /><h2>Drop Supplier Invoices .csv's</h2>
                             </div>
                    <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>
                    </div>  
                 <div class="row 150%">      
                     <div class="3u 12u$(medium)" style="text-align:center">&nbsp;
                         <asp:HiddenField ID="hiddenFilePath" runat="server" />
                     </div>
                     <div class="6u 12u$(medium)" style="text-align:center">
                  <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
                            <ProgressTemplate>
                                <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
                               <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." style="padding: 10px; padding-top:15%; border-radius:1.5em" />
                                </div>
                              </ProgressTemplate>
                        </asp:UpdateProgress> 
                         <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                    <ContentTemplate>


                     <cci:Accordion ID="Accordion1" runat="server" SelectedIndex="0" ContentCssClass="accordionContent"  headercssclass="accordionHeader" headerselectedcssclass="accordionHeaderSelected" AutoSize="None" FadeTransitions="true" TransitionDuration="450" FramesPerSecond="40" RequireOpenedPane="true" >
                         <Panes>
                             <cci:AccordionPane runat="server">
                                  <Header>
                                          1) Select Supplier
                                    </Header>
                                 <Content>
                                     <asp:DropDownList ID="DDSuppList" runat="server" Width="200px">
                                         <asp:ListItem>Supplier 1</asp:ListItem>
                                         <asp:ListItem>Supplier 2</asp:ListItem>
                                         <asp:ListItem>Supplier 3</asp:ListItem>
                                     </asp:DropDownList>
                                     
                                     
                                     
                                     <h4>Drop Sage .csv(s)</h4>
                             <div id="drop_zone" ondrop="drop(event)" ondragover="allowDrop(event)" style="width: 100%; height: 100px; border: 2px dashed #ccc; text-align: center; line-height: 100px;">
                                Drop folder here
                            </div>
                             <ul>
                                 <li>.zip files only</li>
                                 <li>Maximin size = 1GB</li>
                             </ul>
                             <span>Uploaded File</span><br />
                             <span style="font-size:1.3em; color:orangered" id="lblFileName">No file uploaded</span>
                               </Content>
                             </cci:AccordionPane>
                          <cci:AccordionPane runat="server">
                                <Header>
                                       2) View Uploaded Transaction(s) 
                                    </Header>
                                    <Content>
                                        <span style="font-size:.8em; color:red">*** We working on it ***</span>
                                        <%--<ul>
                                            <li><asp:LinkButton ID="lbtnMStore" runat="server" CssClass=" icon fa-chevron-circle-up buttonTransparent" style="text-align:left"  Width="300" OnClick="lbtnMStore_Click"> 1) Multi Stores Master</asp:LinkButton></li>   
                                            <li><asp:LinkButton ID="lbtnStoreLink" runat="server" CssClass=" icon fa-chevron-circle-up buttonTransparent" style="text-align:left"  Width="300" OnClick="lbtnStoreLink_Click" > 2) Item <--> Store Link</asp:LinkButton></li>
                                            <li><hr /></li>
                                            <li><span style="font-size:.8em">Select last valid Period in Sage50C to import Stock balances per store. <br /> <asp:DropDownList ID="DDPeriod" runat="server" style="text-align:center; width:4em">
                                                <asp:ListItem>1</asp:ListItem>
                                                <asp:ListItem>2</asp:ListItem>
                                                <asp:ListItem>3</asp:ListItem>
                                                <asp:ListItem>4</asp:ListItem>
                                                <asp:ListItem>5</asp:ListItem>
                                                <asp:ListItem>6</asp:ListItem>
                                                <asp:ListItem>7</asp:ListItem>
                                                <asp:ListItem>8</asp:ListItem>
                                                <asp:ListItem>9</asp:ListItem>
                                                <asp:ListItem>10</asp:ListItem>
                                                <asp:ListItem>11</asp:ListItem>
                                                <asp:ListItem>12</asp:ListItem>
                                                <asp:ListItem>13</asp:ListItem>
                                            </asp:DropDownList> </span></li>
                                            <li><asp:LinkButton ID="lbtnOpenBal" runat="server" CssClass=" icon fa-chevron-circle-up buttonTransparent" style="text-align:left"  Width="300" OnClick="lbtnOpenBal_Click" > 3) Stores Opening Balances</asp:LinkButton></li>
                                            <%--<li><asp:LinkButton ID="LinkButton6" runat="server" CssClass=" icon fa-chevron-circle-up buttonTransparent" style="text-align:left"  Width="300"> 4) Sage 50C Kits as SBCA Bundles</asp:LinkButton></li>--%>
                                            <%--<li><asp:LinkButton ID="LinkButton7" runat="server" CssClass=" icon fa-chevron-circle-up buttonTransparent" style="text-align:left"  Width="250"> BOMs</asp:LinkButton></li>--%>
                                            </ul>
                                       <%-- <cci:ConfirmButtonExtender ID="lbtnMStore_ConfirmButtonExtender" runat="server" ConfirmText="Confirm - Import Multi Stores - Are you sure?" Enabled="True" TargetControlID="lbtnMStore"></cci:ConfirmButtonExtender>
                                        <cci:ConfirmButtonExtender ID="lbtnStoreLink_ConfirmButtonExtender" runat="server" ConfirmText="Confirm - Migrate Items<-->Stores Links - Are you sure?" Enabled="True" TargetControlID="lbtnStoreLink"></cci:ConfirmButtonExtender>
                                         <cci:ConfirmButtonExtender ID="lbtnOpenBal_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm - Update Opening Stock On Hand levels  - Are you sure?" Enabled="True" TargetControlID="lbtnOpenBal"></cci:ConfirmButtonExtender>--%>
                                        </Content>
                         </cci:AccordionPane>
                             <cci:AccordionPane runat="server">
                                <Header>
                                       3) Upload To Sage Accounting 
                                    </Header>
                                    <Content>
                                        <span style="font-size:.8em; color:red">*** We working on it ***</span>
                                        <%--<ul>
                                            <li><asp:LinkButton ID="lbtnMStore" runat="server" CssClass=" icon fa-chevron-circle-up buttonTransparent" style="text-align:left"  Width="300" OnClick="lbtnMStore_Click"> 1) Multi Stores Master</asp:LinkButton></li>   
                                            <li><asp:LinkButton ID="lbtnStoreLink" runat="server" CssClass=" icon fa-chevron-circle-up buttonTransparent" style="text-align:left"  Width="300" OnClick="lbtnStoreLink_Click" > 2) Item <--> Store Link</asp:LinkButton></li>
                                            <li><hr /></li>
                                            <li><span style="font-size:.8em">Select last valid Period in Sage50C to import Stock balances per store. <br /> <asp:DropDownList ID="DDPeriod" runat="server" style="text-align:center; width:4em">
                                                <asp:ListItem>1</asp:ListItem>
                                                <asp:ListItem>2</asp:ListItem>
                                                <asp:ListItem>3</asp:ListItem>
                                                <asp:ListItem>4</asp:ListItem>
                                                <asp:ListItem>5</asp:ListItem>
                                                <asp:ListItem>6</asp:ListItem>
                                                <asp:ListItem>7</asp:ListItem>
                                                <asp:ListItem>8</asp:ListItem>
                                                <asp:ListItem>9</asp:ListItem>
                                                <asp:ListItem>10</asp:ListItem>
                                                <asp:ListItem>11</asp:ListItem>
                                                <asp:ListItem>12</asp:ListItem>
                                                <asp:ListItem>13</asp:ListItem>
                                            </asp:DropDownList> </span></li>
                                            <li><asp:LinkButton ID="lbtnOpenBal" runat="server" CssClass=" icon fa-chevron-circle-up buttonTransparent" style="text-align:left"  Width="300" OnClick="lbtnOpenBal_Click" > 3) Stores Opening Balances</asp:LinkButton></li>
                                            <%--<li><asp:LinkButton ID="LinkButton6" runat="server" CssClass=" icon fa-chevron-circle-up buttonTransparent" style="text-align:left"  Width="300"> 4) Sage 50C Kits as SBCA Bundles</asp:LinkButton></li>--%>
                                            <%--<li><asp:LinkButton ID="LinkButton7" runat="server" CssClass=" icon fa-chevron-circle-up buttonTransparent" style="text-align:left"  Width="250"> BOMs</asp:LinkButton></li>--%>
                                            </ul>
                                       <%-- <cci:ConfirmButtonExtender ID="lbtnMStore_ConfirmButtonExtender" runat="server" ConfirmText="Confirm - Import Multi Stores - Are you sure?" Enabled="True" TargetControlID="lbtnMStore"></cci:ConfirmButtonExtender>
                                        <cci:ConfirmButtonExtender ID="lbtnStoreLink_ConfirmButtonExtender" runat="server" ConfirmText="Confirm - Migrate Items<-->Stores Links - Are you sure?" Enabled="True" TargetControlID="lbtnStoreLink"></cci:ConfirmButtonExtender>
                                         <cci:ConfirmButtonExtender ID="lbtnOpenBal_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm - Update Opening Stock On Hand levels  - Are you sure?" Enabled="True" TargetControlID="lbtnOpenBal"></cci:ConfirmButtonExtender>--%>
                                        </Content>
                         </cci:AccordionPane>
                             </Panes>
                     </cci:Accordion>
                       
                        <asp:LinkButton ID="LinkButton1" runat="server" style="display:none">LinkButton</asp:LinkButton>
                        <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton1"></cci:ModalPopupExtender>
                        <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                                    <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                                    <div class="HellowWorldPopup">
                                        <div id="Div4" class="PopupHeader">
                                            <h2>Transactions complete</h2>
                                        </div>
                                        <div class="PopupBody">
                                          <asp:Label ID="lblYes" runat="server" Text=""></asp:Label><br />
                                          <asp:Label ID="lblNo" runat="server" Text=""></asp:Label>
                                        </div>
                                        <div class="Controls">
                                            <input id="btnCancel5" type="button" class="fa fa-times-circle" value="" runat="server" style="display:none"/>
                                            <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                            <asp:LinkButton ID="btnApprovYes" runat="server" CssClass="buttonSage" >OK</asp:LinkButton>
                                        </div>
                                    </div>
                                </asp:Panel>

                    </ContentTemplate>
                </asp:UpdatePanel>
                    </div>
                       <div class="3u 12u$(medium)" style="text-align:center">&nbsp;</div>
                            </div>
                </div>
        </div>
    </form>
</body>
</html>
