<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="ItemStkAdjustment.aspx.cs" Inherits="SBMS.ItemStkAdjustment" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Item Adjustment</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <%--<link rel="stylesheet" href="assets/css/main.css" />--%>
        <link rel="stylesheet" href="prologue/assets/css/main.css" />
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
		            <ul >
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
                                   <h3 style="padding-top:2em; line-height:1em">Item Adjustment</h3>
                               </div>       
                         </div> 
                <div class="row 150%">
                    <div class="col-3 col-12-wide">&nbsp;</div>
                        <div class="col-6 col-12-wide">
                                 <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
                                                <ProgressTemplate>
                                                    <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
                                                       <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." style="padding: 10px; padding-top:15%; border-radius:1.5em" />
                                                    </div>
                                                  </ProgressTemplate>
                                            </asp:UpdateProgress> 
                            <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                                <ContentTemplate>
                                  <span style="color:red; font-size:small">* All fields are compulsory *</span>
                                    <table>
                                <tr>
                                    <td>Item <span style="color:red; font-size:large;">*</span></td>
                                    <td>
                                        <asp:DropDownList ID="DDItemList" runat="server"                                       
                                                AutoPostBack="true" 
                                                OnSelectedIndexChanged="DDItemList_SelectedIndexChanged"
                                                CssClass="js-filterable-select optiondd"
                                                MaxLength="100"
                                                CaseSensitive="false"
                                                AutoComplete="true"
                                                Style="max-width:25em; max-width:90%; width: 100%;" >
                                                <asp:ListItem Text="- Select -" Value="" />
                                            </asp:DropDownList>
                                            <asp:HiddenField ID="hdnAllItems" runat="server" />
                                    </td>
                                </tr>
                                        <tr>
                                            <td>Store <span style="color:red; font-size:large">*</span></td>
                                            <td><asp:DropDownList ID="DDStore" runat="server" AutoPostBack="true" style="width:150px;" CssClass="optiondd"></asp:DropDownList><br /></td>
                                        </tr>
                                 <tr>
                                    <td>Lot_Number <span style="color:red; font-size:large">*</span></td>
                                    <td><asp:DropDownList ID="DDLotNum" runat="server" Width="150" CssClass="optiondd"></asp:DropDownList>
                                        <asp:LinkButton ID="lbtnAddLot" runat="server" ToolTip="Add New Lot Number" CssClass="fa fa-plus-square"></asp:LinkButton></td>
                                        <tr>
                                    <td>Qty_To_Adjust <span style="color:red; font-size:large">*</span></td>
                                    <td><asp:TextBox ID="txtAdjQty" runat="server" Width="150" style="text-align:center;font-size:1em; margin-bottom:0.25em; padding-top:0.4em" >1</asp:TextBox><br /></td>
                                        <cci:FilteredTextBoxExtender ID="ftbe" runat="server" TargetControlID="txtAdjQty" FilterType="Custom, Numbers" ValidChars="." />
                                </tr>
                                </tr>
                                     <tr>
                                         <td>Average Cost<span style="color:red; font-size:large">*</span></td>
                                            <td>
                                                <asp:TextBox ID="txtAvCost" runat="server" Width="150" style="text-align:center; font-size:1em" onchange="confirmAvCostChange(this)">0</asp:TextBox><br />
                                                <span id="costConfirmation" style="color:orange; font-size:0.8em; display:none;">You have changed the Average Cost, please make sure you do it with caution.</span>
                                            </td>
                                            <cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender1" runat="server" TargetControlID="txtAvCost" FilterType="Custom, Numbers" ValidChars="." />
                                     </tr>
                                     <tr>
                                    <td>Add_Or_Remove <span style="color:red; font-size:large">*</span></td>
                                    <td>
                                        <asp:DropDownList ID="DDInOut" runat="server" Width="150" CssClass="optiondd">
                                            <asp:ListItem>- Select -</asp:ListItem>
                                            <asp:ListItem Value="0">Add</asp:ListItem>
                                            <asp:ListItem Value="1">Remove</asp:ListItem>
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                                
                                <tr>
                                    <td>Adjust_Sage <span style="color:red; font-size:large">*</span></td>
                                    <td>
                                        <asp:DropDownList ID="DDAdjYesNo" runat="server" Width="150" CssClass="optiondd">
                                            <asp:ListItem>- Select -</asp:ListItem>
                                            <asp:ListItem Value="0">Yes</asp:ListItem>
                                            <asp:ListItem Value="1">No</asp:ListItem>
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                                        <tr>
                                            <td style="vertical-align:top">Reason <span style="color:red; font-size:large">*</span></td>
                                            <td>
                                            <asp:TextBox ID="txtAdjReason" runat="server" TextMode="MultiLine" Columns="40" Rows="5" MaxLength="100"></asp:TextBox></td>
                                        </tr>
                                <tr>
                                    <td>&nbsp;</td>
                                    <td>
                                        <asp:LinkButton ID="lbtnSave" runat="server" OnClick="lbtnSave_Click" CssClass="fa fa-save buttonC"> Complete Adjustment</asp:LinkButton>
                                        <cci:ConfirmButtonExtender ID="lbtnSave_ConfirmButtonExtender1" runat="server" ConfirmText="Complete Stock Adjustment? Are Your Sure?" Enabled="True" TargetControlID="lbtnSave"></cci:ConfirmButtonExtender>
                                        <asp:Label ID="lblerr" runat="server" Text="" ForeColor="red"></asp:Label>
                                    </td>
                                </tr>
                            </table>
                                    <cci:ModalPopupExtender ID="Button25_ModalPopupExtender" runat="server" BackgroundCssClass="ModalPopupBG" CancelControlID="btnCancel5" Drag="true" OkControlID="btnOkay5" PopupControlID="PnlConf" PopupDragHandleControlID="PopupHeader" TargetControlID="lbtnAddLot"></cci:ModalPopupExtender>
                                        <asp:Panel ID="PnlConf" runat="server" Style="display: none">
                                                    <asp:LinkButton ID="lbtnCancel" runat="server" CssClass="fa fa-times" style="float:right" ToolTip="Cancel" > </asp:LinkButton>
                                                    <div class="HellowWorldPopup">
                                                        <div id="Div4" class="PopupHeader">
                                                            <h2>New Lot Number</h2>
                                                                   <asp:LinkButton ID="lbtnNewLot" runat="server" CssClass="icon fa-plus-square" OnClick="lbtnNewLot_Click"> Create New</asp:LinkButton><br /><br>
                                                        </div>
                                                        <div class="PopupBody">     
                                                                       <asp:TextBox ID="lblLotNum" runat="server" style="width:80%; margin-left:10%; margin-right:10%; text-align:center" OnTextChanged="lblLotNum_TextChanged" MaxLength="50" onkeyup="countCharacters(this)" onblur="convertToUpper(this)" ></asp:TextBox><br />
                                                                               <cci:FilteredTextBoxExtender ID="FilteredTextBoxExtender3" runat="server" TargetControlID="lblLotNum" FilterType="Numbers, UppercaseLetters, LowercaseLetters, Custom" ValidChars=".-/\:*" />
                                                                               <asp:HiddenField ID="hfOriginalLotNumber" runat="server" />
                                                                               <span style="font-size:.8em">(Max 50 Characters:  <asp:Label ID="lblCharacterCount" runat="server" Text="Remaining: 50"></asp:Label> )</span><br />
                                                                               <span style="font-size:.8em">(Limited to:- Numbers, UppercaseLetters and ONLY these special characters  - / \ : * )</span>
                                                                       <asp:Label ID="LotNumCheck" runat="server" ForeColor="Red" Text=""></asp:Label>
                                                        </div>
                                                        <div class="Controls">
                                                            <input id="btnCancel5" type="button" class="fa fa-times-circle" value="No" runat="server" style="display:none"/>
                                                            <input id="btnOkay5" type="button" class="buttonYellow" value="OK" runat="server" style="display:none"/>
                                                            <asp:LinkButton ID="btnAddNewLot" runat="server" CssClass="icon fa-thumbs-up buttonRed" OnClick="btnAddNewLot_Click"  > Save New Lot Number</asp:LinkButton>
                                                        </div>
                                                    </div>
                                                </asp:Panel>
                                    <script type="text/javascript">
                                        function countCharacters(textbox) {
                                            var maxLength = 50;
                                            if (textbox.value.length > maxLength) {
                                                textbox.value = textbox.value.substring(0, maxLength);
                                            }
                                            var length = textbox.value.length;
                                            var remaining = maxLength - length;
                                            var label = document.getElementById('<%= lblCharacterCount.ClientID %>');
                                            label.innerHTML = "Remaining: " + remaining;
                                        }
                                    </script>
                                </ContentTemplate>
                            </asp:UpdatePanel>
                         </ div> 
                    <div class="col-3 col-12-wide">&nbsp;</div>
                    </div>
                </div>
             
        </div>
    </div>
    </form>
    <script type="text/javascript">
        function confirmAvCostChange(textbox) {
            var confirmSpan = document.getElementById('costConfirmation');
            var originalValue = textbox.defaultValue;
            var newValue = textbox.value;
            
            if (originalValue !== newValue) {
                confirmSpan.style.display = 'inline';
                setTimeout(function() {
                    confirmSpan.style.display = 'none';
                }, 3000); // Hide after 3 seconds
            }
            textbox.defaultValue = newValue;
        }
    </script>
    <script>
        function convertToUpper(textBox) {
            textBox.value = textBox.value.toUpperCase();
        }
    </script>
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    <script type="text/javascript">
        $(document).ready(function () {
            var allItems = JSON.parse($('#<%= hdnAllItems.ClientID %>').val());
        var select = $('.js-filterable-select');

        // Convert to Select2 with local data and enhanced features
        select.select2({
            data: allItems,
            minimumInputLength: 1,
            placeholder: "- Select -",
            allowClear: true,
            width: 'style',
            dropdownAutoWidth: true,
            // Add search delay to wait for user to finish typing
            delay: 1250, // 1.25 s delay before searching
            // Show more results
            maximumSelectionLength: 100,

            // Better matching - search in both code and description
            matcher: function (params, data) {
                // If no search term, return all
                if ($.trim(params.term) === '') return data;

                var term = params.term.toLowerCase();
                // Search in both code and text
                if (data.text.toLowerCase().indexOf(term) > -1 ||
                    data.id.toLowerCase().indexOf(term) > -1) {
                    return data;
                }
                return null;
            },

            // Better template to show both code and description clearly
            templateResult: function (item) {
                if (!item.id) return item.text;

                var $result = $('<span></span>');
                // If it's a search result, highlight the match
                if (item.text && item.text.includes(' - ')) {
                    var parts = item.text.split(' - ');
                    $result.append('<strong style="font-weight: bold;">' + parts[0] + '</strong> - ' + parts[1]);
                } else {
                    $result.text(item.text);
                }
                return $result;
            },

            // Show full text in selection
            templateSelection: function (item) {
                return item.text;
            }
        });

        // Handle AutoPostBack when an item is selected
        select.on('select2:select', function (e) {
            // Only trigger postback if an actual item was selected (not clearing)
            if (e.params.data && e.params.data.id !== '') {
                // Small delay to ensure the dropdown value is set before postback
                setTimeout(function () {
                    __doPostBack('<%= DDItemList.ClientID %>', '');
            }, 100);
        }
    });

        // Ensure dropdown width matches the control width
        select.on('select2:open', function () {
            var dropdown = $('.select2-container--open .select2-dropdown');
            dropdown.css('min-width', $(this).outerWidth() + 'px');
        });
    });

        // Reinitialize Select2 after ASP.NET postback (if using UpdatePanel)
        if (typeof (Sys) !== 'undefined') {
            var prm = Sys.WebForms.PageRequestManager.getInstance();
            prm.add_endRequest(function () {
                var allItems = JSON.parse($('#<%= hdnAllItems.ClientID %>').val());
        $('.js-filterable-select').select2({
            data: allItems,
            minimumInputLength: 1,
            placeholder: "- Select -",
            allowClear: true,
            width: 'style',
            delay: 1250,
            maximumSelectionLength: 100,
            matcher: function (params, data) {
                if ($.trim(params.term) === '') return data;
                var term = params.term.toLowerCase();
                if (data.text.toLowerCase().indexOf(term) > -1 ||
                    data.id.toLowerCase().indexOf(term) > -1) {
                    return data;
                }
                return null;
            },
            templateResult: function (item) {
                if (!item.id) return item.text;
                var $result = $('<span></span>');
                if (item.text && item.text.includes(' - ')) {
                    var parts = item.text.split(' - ');
                    $result.append('<strong style="font-weight: bold;">' + parts[0] + '</strong> - ' + parts[1]);
                } else {
                    $result.text(item.text);
                }
                return $result;
            },
            templateSelection: function (item) {
                return item.text;
            }
        });
    });
        }
    </script>
    <link href="https://cdnjs.cloudflare.com/ajax/libs/select2/4.0.13/css/select2.min.css" rel="stylesheet" />
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/select2/4.0.13/js/select2.min.js"></script>
</body>
</html>
