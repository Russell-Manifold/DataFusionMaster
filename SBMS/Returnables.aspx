<%-- Returnable items: pallets, crates, drums, cylinders - anything that goes out with a
     delivery and is expected back. Log a return (or an opening balance) against a customer,
     and see who is holding what. Data access is in Classes/Returnables.cs (raw SQL; the
     tables are outside the EF model - SQL/Add_Returnables.sql).
     AsyncTimeout: a return posts a stock adjustment to Sage; async pages default to 45s. --%>
<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="Returnables.aspx.cs" Inherits="SBMS.ReturnablesPage" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Returnables</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="prologue/assets/css/main.css" />
    <%-- In the HEAD, not after the form: this page posts back in full (no UpdatePanel), so the
         message script the server writes runs while the page is still loading. Loaded after
         the form, SweetAlert did not exist yet and every message silently failed to show. --%>
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    <style>
        .rt-wrap   { max-width:1100px; margin:auto; text-align:left; }
        .rt-sec    { color:#e68a00; font-weight:600; margin:1.6em 0 .4em; border-bottom:1px solid #dfe6eb; padding-bottom:.2em; }
        .rt-form td { padding:.25em .6em .25em 0; vertical-align:middle; }
        .rt-form input[type=text] { height:2.5em; }
        /* Date pickers: same look as the rest of the form (aliceblue, rounded, blue focus), not the browser default. */
        .rt-form input[type=date] { -webkit-appearance:none; appearance:none; background:aliceblue; color:#333; border:1px solid #cfd8de; border-radius:.7em; padding:.3em .8em; height:2.5em; font:inherit; font-size:.95em; }
        .rt-form input[type=date]:focus { outline:none; border-color:#4282C1; box-shadow:0 0 0 2px rgba(66,130,193,.25); }
        .rt-form input[type=date]::-webkit-calendar-picker-indicator { cursor:pointer; opacity:.55; padding:.15em; border-radius:.3em; }
        .rt-form input[type=date]::-webkit-calendar-picker-indicator:hover { opacity:1; background:#dde9f5; }
        /* No fixed height on dropdowns: the site stylesheet sets select line-height to 2.5em,
           and a 2em box cut the text in half. optiondd is the look the other screens use. */
        .rt-form select { height:auto; }
        .rt-grid   { width:100%; border-collapse:collapse; font-size:.92em; }
        .rt-grid th { background:#f1f5f8; text-align:left; padding:.35em .5em; border-bottom:1px solid #cfd8de; }
        .rt-grid td { padding:.3em .5em; border-bottom:1px solid #e6ecf0; }
        .rt-num    { text-align:right !important; }
        .rt-out    { font-weight:700; color:#b02a2a; }
        .rt-hint   { font-size:.85em; color:#777; }
        /* Two tabs: the daily work (Returns) and the once-off list of returnable items (Setup). */
        .rt-tabs   { margin-top:1.2em; border-bottom:2px solid #4282C1; }
        .rt-tabs button { background:#eef3f7; color:#4282C1; border:1px solid #cfd8de; border-bottom:none; border-radius:.4em .4em 0 0;
                          padding:.45em 1.2em; margin-right:.3em; font-size:1em; cursor:pointer; }
        .rt-tabs button.rt-on { background:#4282C1; color:#fff; border-color:#4282C1; }
        .rt-setup  { background:#f7f9fb; border:1px solid #dfe6eb; border-top:none; padding:.2em 1em 1em; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div id="header">
            <div class="top">
                <div id="logo">
                    <asp:Label ID="lblUsername" runat="server" Text="" Font-Size="Small" style="padding-top:0em; float:left"></asp:Label>
                    <asp:LinkButton ID="lbtnLogOut" runat="server" style="font-size:0.9em; float:right" OnClick="lbtnLogOut_Click"> Log Out</asp:LinkButton>
                </div>
                <nav id="nav">
                    <ul>
                        <li><asp:LinkButton ID="lbtnDash" runat="server" CssClass="buttonM" OnClick="lbtnDash_Click" ToolTip="Return to main dashboard">Dashboard</asp:LinkButton></li>
                        <li><asp:LinkButton ID="lbtnStockControl" runat="server" CssClass="buttonM" OnClick="lbtnStockControl_Click" ToolTip="Back to Stock Control">Stock Control</asp:LinkButton></li>
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
                            <asp:Image ID="imgCoImg" runat="server" style="float:right" class="logoImg" />
                            <h3 style="padding-top:2em; line-height:1em">Returnables</h3>
                        </div>
                    </div>

                    <div class="rt-wrap">
                        <asp:Panel ID="pnlNotReady" runat="server" Visible="false" style="color:#b02a2a; padding:1em 0">
                            Returnables is not set up on this database yet. Run <b>Add_Returnables.sql</b> first.
                        </asp:Panel>

                        <asp:Panel ID="pnlMain" runat="server">
                            <%-- Said up front: this screen is NOT a credit note. --%>
                            <div style="background:#fff6e5; border-left:3px solid #e69500; color:#7a4b00; padding:.6em .9em; margin-top:1em; font-size:.92em">
                                <b>For no-charge items that are expected back</b> &ndash; pallets, crates, drums, cylinders.<br />
                                This is <b>not a credit note</b>. Nothing is credited to the customer and no invoice is changed; it only records that the items came back and puts them into stock.
                                To credit a customer for goods they were charged for, raise a credit note in Sage.
                            </div>
                            <div class="rt-tabs">
                                <button type="button" id="rtBtn-returns" onclick="rtShowTab('returns')">Returns</button>
                                <button type="button" id="rtBtn-setup" onclick="rtShowTab('setup')">Setup: returnable items</button>
                            </div>
                            <%-- Which tab is showing; posted back so it survives a save or an Add. --%>
                            <asp:HiddenField ID="hfTab" runat="server" ClientIDMode="Static" Value="returns" />

                            <%-- ═════════ SETUP tab: which items are returnable ═════════ --%>
                            <div id="rtTab-setup" class="rt-setup" style="display:none">
                            <div class="rt-sec" id="rtItems">Returnable items</div>
                            <span class="rt-hint">Deliveries of an item are counted from the day it is added here. What customers already hold goes in as an opening balance.</span>
                            <table class="rt-form">
                                <tr>
                                    <td>Item code</td>
                                    <td><asp:TextBox ID="txtItemCode" runat="server" Width="200px" MaxLength="50"></asp:TextBox></td>
                                    <td><asp:LinkButton ID="lbtnAddItem" runat="server" CssClass="fa fa-plus-square buttonC" OnClick="lbtnAddItem_Click"> Add</asp:LinkButton></td>
                                </tr>
                            </table>
                            <asp:GridView ID="GridItems" runat="server" AutoGenerateColumns="false" CssClass="rt-grid" GridLines="None" DataKeyNames="ItemID" OnRowCommand="GridItems_RowCommand" EmptyDataText="No returnable items yet. Add the item code of your pallet, crate or cylinder above.">
                                <Columns>
                                    <asp:BoundField DataField="ItemCode" HeaderText="Code" />
                                    <asp:BoundField DataField="ItemDescription" HeaderText="Item" />
                                    <asp:BoundField DataField="Since" HeaderText="Counted from" DataFormatString="{0:dd MMM yyyy}" />
                                    <asp:TemplateField HeaderText="">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="lbtnRemove" runat="server" CommandName="RemoveItem" CommandArgument='<%# Eval("ItemID") %>' ForeColor="Red"
                                                OnClientClick="return confirm('Stop tracking this item as returnable? Its history is kept and comes back if you add it again, but deliveries are then counted from the new date.');">Remove</asp:LinkButton>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>

                            </div>

                            <%-- ═════════ RETURNS tab: the daily work ═════════ --%>
                            <div id="rtTab-returns">
                            <asp:Panel ID="pnlNoItems" runat="server" Visible="false" style="border:1px solid #e69500; border-radius:.3em; padding:.6em .9em; margin-top:1em">
                                No items are marked as returnable yet. <a href="#" onclick="rtShowTab('setup'); return false;">Add your pallet, crate or cylinder on the Setup tab</a> before logging a return.
                            </asp:Panel>

                            <%-- ═════════ Log a return / opening balance ═════════ --%>
                            <div class="rt-sec">Log a return of no-charge items</div>
                            <table class="rt-form">
                                <tr>
                                    <td>Type</td>
                                    <td>
                                        <asp:DropDownList ID="DDType" runat="server" Width="300px" CssClass="optiondd" AutoPostBack="true" OnSelectedIndexChanged="DDType_SelectedIndexChanged">
                                            <asp:ListItem Value="R">Return - no-charge items came back</asp:ListItem>
                                            <asp:ListItem Value="O">Opening balance - already with the customer</asp:ListItem>
                                        </asp:DropDownList>
                                    </td>
                                    <td>Date</td>
                                    <td><asp:TextBox ID="txtDate" runat="server" TextMode="Date" Width="160px"></asp:TextBox></td>
                                </tr>
                                <tr>
                                    <td>Customer</td>
                                    <td><asp:DropDownList ID="DDCustomer" runat="server" Width="300px" CssClass="optiondd"></asp:DropDownList></td>
                                    <td>Item</td>
                                    <td><asp:DropDownList ID="DDItem" runat="server" Width="300px" CssClass="optiondd"></asp:DropDownList></td>
                                </tr>
                                <tr>
                                    <td>Quantity</td>
                                    <td>
                                        <asp:TextBox ID="txtQty" runat="server" Width="100px" style="text-align:center"></asp:TextBox>
                                        <cci:FilteredTextBoxExtender ID="ftbeQty" runat="server" TargetControlID="txtQty" FilterType="Custom, Numbers" ValidChars="." />
                                    </td>
                                    <td><asp:Label ID="lblStore" runat="server" Text="Received into store"></asp:Label></td>
                                    <td><asp:DropDownList ID="DDStore" runat="server" Width="160px" CssClass="optiondd"></asp:DropDownList></td>
                                </tr>
                                <tr>
                                    <td>Reference</td>
                                    <td colspan="3"><asp:TextBox ID="txtRef" runat="server" Width="420px" MaxLength="100" placeholder="Delivery note number, vehicle registration, who returned them"></asp:TextBox></td>
                                </tr>
                                <tr>
                                    <td></td>
                                    <td colspan="3">
                                        <asp:LinkButton ID="lbtnSave" runat="server" CssClass="fa fa-save buttonC" OnClick="lbtnSave_Click"> Save</asp:LinkButton>
                                        <cci:ConfirmButtonExtender ID="cbeSave" runat="server" ConfirmText="Save this entry?" Enabled="True" TargetControlID="lbtnSave"></cci:ConfirmButtonExtender>
                                        <span class="rt-hint" style="margin-left:1em"><asp:Label ID="lblTypeHint" runat="server" Text=""></asp:Label></span>
                                    </td>
                                </tr>
                            </table>

                            <%-- ═════════ Who is holding what ═════════ --%>
                            <div class="rt-sec">Outstanding by customer</div>
                            <asp:CheckBox ID="chkShowZero" runat="server" Text=" Show customers with nothing outstanding" AutoPostBack="true" OnCheckedChanged="chkShowZero_CheckedChanged" />
                            <asp:GridView ID="GridOut" runat="server" AutoGenerateColumns="false" CssClass="rt-grid" GridLines="None" EmptyDataText="Nothing is outstanding.">
                                <Columns>
                                    <asp:BoundField DataField="CustomerName" HeaderText="Customer" />
                                    <asp:BoundField DataField="ItemCode" HeaderText="Code" />
                                    <asp:BoundField DataField="ItemDescription" HeaderText="Item" />
                                    <asp:BoundField DataField="Opening" HeaderText="Opening" DataFormatString="{0:0.##}" ItemStyle-CssClass="rt-num" HeaderStyle-CssClass="rt-num" />
                                    <asp:BoundField DataField="Delivered" HeaderText="Delivered" DataFormatString="{0:0.##}" ItemStyle-CssClass="rt-num" HeaderStyle-CssClass="rt-num" />
                                    <asp:BoundField DataField="Returned" HeaderText="Returned" DataFormatString="{0:0.##}" ItemStyle-CssClass="rt-num" HeaderStyle-CssClass="rt-num" />
                                    <asp:BoundField DataField="Outstanding" HeaderText="Still out" DataFormatString="{0:0.##}" ItemStyle-CssClass="rt-num rt-out" HeaderStyle-CssClass="rt-num" />
                                    <asp:BoundField DataField="LastDelivered" HeaderText="Last delivered" DataFormatString="{0:dd MMM yyyy}" />
                                    <asp:BoundField DataField="LastReturned" HeaderText="Last returned" DataFormatString="{0:dd MMM yyyy}" />
                                </Columns>
                            </asp:GridView>

                            <%-- ═════════ Recent entries ═════════ --%>
                            <div class="rt-sec">Recent returns and opening balances</div>
                            <asp:GridView ID="GridRecent" runat="server" AutoGenerateColumns="false" CssClass="rt-grid" GridLines="None" EmptyDataText="Nothing logged yet.">
                                <Columns>
                                    <asp:BoundField DataField="MoveDate" HeaderText="Date" DataFormatString="{0:dd MMM yyyy}" />
                                    <asp:BoundField DataField="MoveType" HeaderText="Type" />
                                    <asp:BoundField DataField="CustomerName" HeaderText="Customer" />
                                    <asp:BoundField DataField="ItemCode" HeaderText="Code" />
                                    <asp:BoundField DataField="ItemDescription" HeaderText="Item" />
                                    <asp:BoundField DataField="Qty" HeaderText="Qty" DataFormatString="{0:0.##}" ItemStyle-CssClass="rt-num" HeaderStyle-CssClass="rt-num" />
                                    <asp:BoundField DataField="Reference" HeaderText="Reference" />
                                </Columns>
                            </asp:GridView>
                            </div>
                        </asp:Panel>
                        <script type="text/javascript">
                            function rtShowTab(t) {
                                var hf = document.getElementById('hfTab');
                                if (!hf) return;
                                hf.value = t;
                                ['returns', 'setup'].forEach(function (n) {
                                    var pane = document.getElementById('rtTab-' + n), btn = document.getElementById('rtBtn-' + n);
                                    if (pane) pane.style.display = (n === t) ? '' : 'none';
                                    if (btn) btn.className = (n === t) ? 'rt-on' : '';
                                });
                            }
                            (function () { var hf = document.getElementById('hfTab'); rtShowTab(hf && hf.value === 'setup' ? 'setup' : 'returns'); })();
                        </script>
                    </div>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
