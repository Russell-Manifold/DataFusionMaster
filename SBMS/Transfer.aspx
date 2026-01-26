<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="Transfer.aspx.cs" Inherits="SBMS.Transfer" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Transfer</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <%--<link rel="stylesheet" href="assets/css/main.css" />--%>
        <link rel="stylesheet" href="prologue/assets/css/main.css" />
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script> 
</head>
<body>
    <form id="form1" runat="server">
         <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        	<div id="header" >
     <div class="top">
         <!-- Logo -->
	            <div id="logo" >
		            <asp:Label ID="lblUsername" runat="server" Text="" Font-Size="Small" style="padding-top:0em; float:left"></asp:Label>   
                    <asp:LinkButton ID="lbtnLogOut" runat="server" style="font-size:0.9em; float:right" OnClick="lbtnLogOut_Click"  > Log Out</asp:LinkButton>  
	            </div>
         <!-- Nav -->
	            <nav id="nav" >
		            <ul>
                        <li><asp:LinkButton ID="imgdash" runat="server" CssClass="buttonM" OnClick="imgdash_Click" ToolTip="Return to main dashboard">Dashboard</asp:LinkButton></li>
                     <li><asp:LinkButton ID="imgbRec" runat="server" CssClass="buttonM" OnClick="imgbRec_Click" ToolTip="Receive from Purchase Orders, allocate lot numbers">Purchase Orders</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnPickSlips" runat="server" CssClass="buttonM" OnClick="ibtnPickSlips_Click" ToolTip="View Sales Orders and picking slips, fulfill orders" >Sales Orders</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnPickTrack" runat="server" CssClass="buttonM" OnClick="ibtnPickTrack_Click" ToolTip="Track all picking slips in a simple drag and drop process " >Picking Slip Tracking</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnStckCtl" runat="server" CssClass="buttonM" ToolTip="Inter Store Transfers, Stock adjustments, Stock Takes, Reporting" OnClick="ibtnStckCtl_Click" >Stock Control</asp:LinkButton></li>
                        <li>______________</li>
	       
                        <li><asp:LinkButton ID="ibtnFCasts" runat="server" CssClass="buttonM" OnClick="ibtnFCasts_Click" ToolTip="Ensure greater stock accuracy with sales forecasting"> Sales Forecasts</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnmrp" runat="server" CssClass="buttonM" OnClick="ibtnmrp_Click" ToolTip="View finished good demands based on PO's, Sales Orders, Forecasts etc" > Finished Goods Demands</asp:LinkButton></li>
                        <li><asp:LinkButton ID="ibtnJobTrack" runat="server" CssClass="buttonM" OnClick="ibtnJobTrack_Click" ToolTip="Track all Job cards, simply drag and drop their change in status"> Job Card Tracking</asp:LinkButton></li>
                        <li>______________</li>
                        <li><asp:LinkButton ID="ibtmWorksOrders" runat="server" CssClass="buttonM" OnClick="ibtmWorksOrders_Click" ToolTip="Create and view works orders for manufacturing or production" > Works Orders</asp:LinkButton></li>		
                        <li><asp:LinkButton ID="ibtmWOrdMgment" runat="server" CssClass="buttonM" OnClick="ibtmWOrdMgment_Click" ToolTip="Allocate raw materials to works orders and update stock levels on order completion." > Works Order Fulfillment</asp:LinkButton></li>		
                        <li><asp:LinkButton ID="ibtnRMD" runat="server" CssClass="buttonM" OnClick="ibtnRMD_Click" ToolTip="View RMD based on PO's, Sales orders, Works Orders." > Raw Materials Demands</asp:LinkButton></li>
                        <li>______________</li>
                            <li><a href="https://mydatafusion.online/learningCenter.aspx" class="buttonM icon fa-lightbulb" target="_blank"> Learning Hub >></a></li>
		            </ul>
	            </nav>
     </div>
 </div>
        <div id="main">        
        <div class="content">
            <div class="container">    
                 <div class="row 150%">
                      <div class="col-12 col-12-wide" style="text-align:center">
                            <a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/Logo.png" style="border-radius:0.25em; float:left" class="logoImg" /></a>
                                <asp:Image ID="imgCoImg" runat="server"  style="float:right" class="logoImg" />  
                            <h3 style="padding-top:2em; line-height:1em">Inter Store Transfer</h3>
                        </div>
                     </div>
                <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
                      <ProgressTemplate>
                          <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
                         <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." style="padding: 10px; padding-top:15%; border-radius:1.5em" />
                          </div>
                        </ProgressTemplate>
                  </asp:UpdateProgress> 
                <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                    <ContentTemplate>
                        <div class="row 150%">
                             <div class="col-12 col-12-normal" style="text-align:center">
                             <h4><asp:Label ID="lblerr" runat="server" Text=" " ForeColor="Red">&nbsp;</asp:Label></h4>   
                             </div>
                            <div class="col-6 col-12-mobile" style="text-align:center">
                                <h3 style="padding-top: 0; line-height: 1em">1) FROM Store&nbsp;&nbsp;</h3>
                                Store <asp:DropDownList ID="DDStoreFrom" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DDStoreFrom_SelectedIndexChanged" Width="200px" CssClass="optiondd"></asp:DropDownList>
                                <asp:Panel ID="Panel1" runat="server" DefaultButton="lbtnfind">
                                     <br />Find Item <asp:TextBox ID="txtFindFrom" runat="server" Width="200px" placeholder="Code / Description" style="font-size:1em"></asp:TextBox><asp:LinkButton ID="lbtnfind" runat="server" CssClass="fa fa-search buttonC" OnClick="lbtnfind_Click" ></asp:LinkButton><br /><br />
                                     </asp:Panel>
                                <asp:GridView ID="GridFromItems" runat="server" CssClass="gridview" AutoGenerateColumns="false" Width="100%" OnRowDataBound="GridFromItems_RowDataBound">
                                     <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <Columns>
                                         <asp:BoundField DataField="TransID" />
                                        <asp:BoundField HeaderText="Code" DataField="ItemCode" ReadOnly="True" ItemStyle-Width="8em"/>
                                        <asp:BoundField HeaderText="Description" DataField="ItemDescription" ReadOnly="True"  ItemStyle-HorizontalAlign="Left" />
                                         <asp:TemplateField HeaderText="Lot_Num" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                            <ItemTemplate >
                                                <asp:LinkButton ID="lbtnTrf" CommandArgument='<%# Eval("TransID")%>' CommandName="lbtnTrf"  runat="server" Text='<%# Eval("LotNumber")%>' ToolTip="Select Lot Number to Transfer" style="color:#4A82AB; font-weight:600; min-height:1.2em; min-width:14em" OnClick="lbtnTrf_Click" ></asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField HeaderText="Store" DataField="StoreCode" ReadOnly="True" ItemStyle-Width="8em" />
                                        <asp:BoundField HeaderText="Q O H" DataField="QOH" ReadOnly="True" ItemStyle-Width="5em" ItemStyle-HorizontalAlign="Right" />
                                    </Columns>                                
                                </asp:GridView>
                            </div>
                            <div class="col-6 col-12-mobile" style="text-align:center">
                                <h3 style="padding-top: 0; line-height: 1em;">2) TO Store</h3>
                                <asp:DropDownList ID="DDStoreTo" runat="server" Width="200px" AutoPostBack="true" OnSelectedIndexChanged="DDStoreTo_SelectedIndexChanged" CssClass="optiondd"></asp:DropDownList>
                                <h5 style="padding-top:3.3em">Current On Hand</h5>
                                <asp:GridView ID="GridToItems" runat="server" CssClass="gridview" AutoGenerateColumns="false" Width="100%" OnRowDataBound="GridToItems_RowDataBound">
                                     <HeaderStyle CssClass="gridViewHeader" />
                                    <FooterStyle CssClass="gridViewHeader" />
                                    <RowStyle CssClass="gridViewRow" />
                                    <AlternatingRowStyle CssClass="gridViewAltRow" />
                                    <Columns>
                                         <asp:BoundField DataField="TransID" />
                                        <asp:BoundField HeaderText="Code" DataField="ItemCode" ReadOnly="True" ItemStyle-Width="8em"/>
                                        <asp:BoundField HeaderText="Description" DataField="ItemDescription" ReadOnly="True"  ItemStyle-HorizontalAlign="Left" />
                                        <asp:BoundField HeaderText="Lot Num" DataField="LotNumber" ReadOnly="True" ItemStyle-Width="8em" />
                                        <asp:BoundField HeaderText="Store" DataField="StoreCode" ReadOnly="True" ItemStyle-Width="8em" />
                                        <asp:BoundField HeaderText="Q O H" DataField="QOH" ReadOnly="True" ItemStyle-Width="5em" ItemStyle-HorizontalAlign="Right"/>
                                    </Columns>                                
                                </asp:GridView>
                            </div>
                            </div>
                            <%-------------------------------------------------------------------------------------------------%>
                            <asp:LinkButton ID="LinkButton1" runat="server" style="display:none"></asp:LinkButton>
                             <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="LinkButton1"></cci:ModalPopupExtender>
                            <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                                <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                                <div class="HellowWorldPopup">
                                    <div id="Div4" class="PopupHeader">
                                        <h3>Item Transfer</h3>
                                        <h4><asp:Label ID="lblItem" runat="server" Text=""></asp:Label><asp:Label ID="TransID" runat="server" Text="" style="display:none"></asp:Label></h4>
                                    </div>
                                    <div class="PopupBody">
                                       Qty to Transfer 
                                        <br />
                                            <asp:TextBox ID="txtQtyToTrf" runat="server" style="text-align:center"></asp:TextBox>
                                        <br /> (Max = <asp:Label ID="lblMax" runat="server" Text=""></asp:Label>)
                                            <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtQtyToTrf" FilterType="Custom, Numbers" ValidChars="." />
                                        <br /><br /><h3>From <asp:Label ID="lblFromStore" runat="server" Text=""></asp:Label> --> <asp:Label ID="lblToStore" runat="server" Text=""></asp:Label> Store</h3>
                                        <hr />
                                        <asp:Panel ID="Panel2" runat="server" style="display:none">
                                            Transfer Additional Costs
                                        <hr />
                                            Additional Costs Reason<br />
                                            <asp:TextBox ID="txtAddCostsReason" runat="server" TextMode="MultiLine" Rows="3" Columns="45" Style="text-align: left;" MaxLength="100"></asp:TextBox><br />
                                            Total (Ex Vat) Value Of Additional Cost(s)
                                            <br />
                                            <asp:TextBox ID="txtTrfAddCosts" runat="server" Style="text-align: center"></asp:TextBox><br />
                                            <br />
                                            <cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender1" runat="server" TargetControlID="txtTrfAddCosts" FilterType="Custom, Numbers" ValidChars="." />
                                            <asp:CheckBox ID="chkSageUpdate" runat="server" Text="Update item average cost in Sage?" />
                                            <hr />
                                        </asp:Panel>
                                    </div>
                                    <div class="Controls">
                                        <input id="btnCancel5" type="button" class="fa fa-times-circle" value="" runat="server" style="display:none"/>
                                        <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                        <asp:LinkButton ID="btnApprovYes" runat="server" CssClass="buttonC fa fa-thumbs-up" OnClick="btnApprovYes_Click" >&nbsp;OK&nbsp;</asp:LinkButton>
                                    </div>
                                </div>
                    </asp:Panel>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
        </div>
            </div>

    </form>
</body>
</html>
