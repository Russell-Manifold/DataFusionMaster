<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TransferSlip.aspx.cs" Inherits="SBMS.TransferSlip" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Stock Transfer</title>
<link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
<link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
<link rel="stylesheet" href="assets/css/main.css" />
<link href="https://cdnjs.cloudflare.com/ajax/libs/select2/4.0.13/css/select2.min.css" rel="stylesheet" />
<script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
<script src="https://cdnjs.cloudflare.com/ajax/libs/select2/4.0.13/js/select2.min.js"></script>
<script type="text/javascript">
    // Enhance all ddlGridItem dropdowns in the GridView with Select2 and a search delay
    function initializeGridTrfLinesSelect2() {
        $("select[id$='ddlGridItem']").select2({
            minimumInputLength: 1,
            placeholder: "- Select -",
            allowClear: true,
            width: 'style',
            dropdownAutoWidth: true,
            delay: 1250, // 1.25 s delay before searching
            maximumSelectionLength: 100,
            matcher: function (params, data) {
                if ($.trim(params.term) === '') return data;
                var term = params.term.toLowerCase();
                if (data.text && data.text.toLowerCase().indexOf(term) > -1 ||
                    data.id && data.id.toLowerCase().indexOf(term) > -1) {
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
        // Ensure dropdown width matches the control width
        $("select[id$='ddlGridItem']").on('select2:open', function () {
            var dropdown = $('.select2-container--open .select2-dropdown');
            dropdown.css('min-width', $(this).outerWidth() + 'px');
        });
    }
    $(document).ready(function () {
        initializeGridTrfLinesSelect2();
    });
    // Reinitialize after UpdatePanel partial postback
    if (typeof (Sys) !== 'undefined') {
        var prm = Sys.WebForms.PageRequestManager.getInstance();
        prm.add_endRequest(function () {
            initializeGridTrfLinesSelect2();
        });
    }
</script>
<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script> 
</head>
<body>
    <form id="form1" runat="server">
          <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
 <div class="content">
     <div class="container">
         <div class="row 150%">
             <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website">
                 <img src="images/logo.png" style="float: left" class="logoImg" /></a></div>
             <div class="8u 12u$(medium)">
                 <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" OnClick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                 <asp:LinkButton ID="lbtnTrfsSOs" runat="server" class="buttonC icon fa-arrow-left" PostBackUrl="~/TransferHeaders.aspx">&nbsp;Open Transfers</asp:LinkButton>
             </div>
             <div class="2u 12u$(medium)">
                 <asp:Image ID="imgCoImg" runat="server" Style="float: right" class="logoImg" /></div>
         </div>
         <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
             <ProgressTemplate>
                 <div style="position: fixed; text-align: center; height: 100%; width: 100%; top: 0; right: 0; left: 0; z-index: 9999999; background-color: #000000; opacity: 0.5;">
                     <asp:Image ID="imgUpdateProgress" runat="server" ImageUrl="~/images/tenorwait.gif" AlternateText="Loading ..." ToolTip="Loading ..." Style="padding: 10px; padding-top: 15%; border-radius: 1.5em" />
                 </div>
             </ProgressTemplate>
         </asp:UpdateProgress>
         <asp:UpdatePanel ID="UpdatePanel1" runat="server">
             <ContentTemplate>
                 <div class="row 150%">
                     <div class="12u 12u$(medium)">
                         <h2>Stock Transfer Between Warehouses: ID=<asp:Label ID="lblDocNum" runat="server" Text=""></asp:Label><asp:Label ID="lblDocID" runat="server" Text=""></asp:Label></h2>
                         <asp:Label ID="lblError" runat="server" ForeColor="Red" />
                         <table style="margin:auto">
                             <tr>
                                <td colspan="2"></td>
                                <td style="text-align:left">From Warehouse:</td>
                                <td><asp:DropDownList ID="ddlFromWarehouse" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlFromWarehouse_SelectedIndexChanged" style="width:10em; margin-top:0.25em"/></td>
                                <td>To Warehouse:</td>
                                <td><asp:DropDownList ID="ddlToWarehouse" runat="server" style="width:10em; margin-top:0.25em" /></td>
                                <td>Status</td>
                                 <td><asp:DropDownList ID="DDStatus" runat="server" style="width:10em" Enabled="false">
                                     <asp:ListItem>New</asp:ListItem>
                                     <asp:ListItem>Started</asp:ListItem>
                                     <asp:ListItem>Complete</asp:ListItem>
                                     <asp:ListItem>Deleted</asp:ListItem>
                                     </asp:DropDownList></td>
                            </tr>
                             <tr>
                                 <td></td>
                                 <td>Reference</td>
                                 <td colspan="2" style="text-align:right"><asp:TextBox ID="txtxTrfRef" runat="server" style="width:20em; margin-top:0.25em"></asp:TextBox></td>
                                 <td>Date</td>
                                 <td><asp:TextBox ID="txtDate" runat="server" style="width:10em;"></asp:TextBox>
                                     <cci:CalendarExtender ID="CalendarExtender1" runat="server" Enabled="True" TargetControlID="txtDate" Format="dd MMM yyyy"></cci:CalendarExtender>
                                 </td>
                                 <td>Complete_Date</td>
                                 <td><asp:TextBox ID="txtCompleteDate" runat="server" style="width:10em;" Enabled="false"></asp:TextBox></td>
                             </tr>
                             
                             <tr>
                                <td colspan="2"></td>
                                <td>Notes:</td>
                                <td colspan="5"><asp:TextBox ID="txtNotes" runat="server" style="width:100%; margin-top:0.25em" TextMode="MultiLine"></asp:TextBox></td>
                            </tr>
                         </table>
                     </div>
                 </div>

                 <div class="row 150%">
                     <div class="12u 12u$(medium)">
                         <asp:GridView ID="GridTrfLines" runat="server" AutoGenerateColumns="false" CssClass="gridviewS" OnRowDataBound="GridTrfLines_RowDataBound">
                             <HeaderStyle CssClass="gridViewHeaderS" />
                             <RowStyle CssClass="gridViewRowS" />
                             <AlternatingRowStyle CssClass="gridViewAltRowS" />
                             <Columns>
                                 <asp:TemplateField HeaderText="Select Item">
                                     <ItemTemplate>
                                         <asp:DropDownList ID="ddlGridItem" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlGridItem_SelectedIndexChanged"></asp:DropDownList>
                                     </ItemTemplate>
                                 </asp:TemplateField>
                                 <asp:TemplateField HeaderText="Lot Number" ItemStyle-Width="5em">
                                    <ItemTemplate>
                                        <asp:DropDownList ID="DDlotNum" runat="server" Style="width: 8em; text-align: center" AutoPostBack="true" OnSelectedIndexChanged="DDlotNum_SelectedIndexChanged">
                                            <asp:ListItem Value="0">- Lot Number-</asp:ListItem>
                                        </asp:DropDownList>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                 <asp:TemplateField HeaderText="On Hand" ItemStyle-Width="10em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                    <ItemTemplate>
                                        <asp:Label ID="lblAvailQty" runat="server" style="text-align:center"></asp:Label>
                                        
                                    </ItemTemplate>
                                     </asp:TemplateField>
                                 <asp:TemplateField HeaderText="Trf_Qty" ItemStyle-Width="6em" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                     <ItemTemplate>
                                         <asp:TextBox ID="txtQty" runat="server" Text='<%# Eval("TrfOutQty") %>' style="text-align:center"></asp:TextBox>
                                     </ItemTemplate>
                                 </asp:TemplateField>
                                 
                                  <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="2em" >
                                     <ItemTemplate>
                                          <asp:LinkButton ID="lbtnLineSave" CommandArgument='<%# Eval("TrfLID") %>' CommandName="lbtnLineSave" runat="server" CssClass="fa fa-save buttonRed" ToolTip="Save Line" OnClick="lbtnLineSave_Click"> </asp:LinkButton>
                                     </ItemTemplate>
                                 </asp:TemplateField>
                                  <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="2em" >
                                     <ItemTemplate>
                                          <asp:LinkButton ID="lbtnDeleteLine" CommandArgument='<%# Eval("TrfLID") %>' CommandName="lbtnDeleteLine" runat="server" CssClass="fa fa-ban" ToolTip="Delete Line" style="color:red" OnClick="lbtnDeleteLine_Click"> </asp:LinkButton>
                                         <cci:ConfirmButtonExtender ID="lbtnIssue_ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Delete Line?" Enabled="True" TargetControlID="lbtnDeleteLine"></cci:ConfirmButtonExtender>
                                     </ItemTemplate>
                                 </asp:TemplateField>
                             </Columns>
                         </asp:GridView>
                     </div>
                     <div class="12u 12u$(medium)" style="text-align: center">
                         <asp:Panel ID="PnlButtons" runat="server">
                             <asp:Label ID="lblErr" runat="server" Text="" Style="margin: auto; color: red"></asp:Label><br />
                             <asp:LinkButton ID="lbtnDelTrf" CssClass="icon fa-ban buttonTransparent" runat="server" ForeColor="Red" ToolTip="Delete Transfer Slip" OnClick="lbtnDelTrf_Click" Style="margin-right: 2em"> Delete</asp:LinkButton>
                             <cci:ConfirmButtonExtender ID="ConfirmButtonExtender2" runat="server" ConfirmText="WARNING - Delete this Transfer? Are you sure?" Enabled="True" TargetControlID="lbtnDelTrf"></cci:ConfirmButtonExtender>
                             <asp:LinkButton ID="lbtnPrintDN" runat="server" class="buttonRed icon fa-print" Width="200px" OnClick="lbtnPrintDN_Click" >&nbsp;Print Preview</asp:LinkButton>
                             <asp:LinkButton ID="LbtnSaveEdits" runat="server" OnClick="LbtnSaveEdits_Click" CssClass=" icon fa-edit buttonSage" ToolTip="Save Edits"> Save Edits</asp:LinkButton>      
                             <asp:LinkButton ID="lbtnStart" runat="server" class="buttonIndex icon fa-upload" Width="220px" OnClick="lbtnStart_Click" ToolTip="Start Transfer">&nbsp;Start Transfer</asp:LinkButton>
                             <cci:ConfirmButtonExtender ID="ConfirmButtonExtender3" runat="server" ConfirmText="Confirm, Start Item Transfer?" Enabled="True" TargetControlID="lbtnStart"></cci:ConfirmButtonExtender>  
                             <asp:LinkButton ID="lbtnCompleteTrf" runat="server" class="buttonSage icon fa-download" Width="220px" OnClick="lbtnCompleteTrf_Click" ToolTip="Complete Transfer">&nbsp;Complete Transfer</asp:LinkButton>
                             <cci:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" ConfirmText="Confirm, Complete Item Transfer?" Enabled="True" TargetControlID="lbtnCompleteTrf"></cci:ConfirmButtonExtender>    
                         </asp:Panel>
                    </div>
                 </div>
             </ContentTemplate>
         </asp:UpdatePanel>
     </div>
 </div>
    </form>
</body>
</html>
