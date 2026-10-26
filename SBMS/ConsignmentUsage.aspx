<%-- Consignment month end: pick the consignment location, count what the customer holds,
     check the statement, create the Sage sales order for what was used. Arithmetic and SQL in
     Classes/ConsignmentUsage.cs (raw SQL; tables from SQL/Add_ConsignmentUsage.sql).
     AsyncTimeout: creating the order posts to Sage. --%>
<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="ConsignmentUsage.aspx.cs" Inherits="SBMS.ConsignmentUsagePage" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Consignment Stock</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="prologue/assets/css/main.css" />
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    <style>
        .cu-wrap   { max-width:1150px; margin:auto; text-align:left; }
        .cu-sec    { color:#e68a00; font-weight:600; margin:1.6em 0 .4em; border-bottom:1px solid #dfe6eb; padding-bottom:.2em; }
        .cu-form td { padding:.25em .6em .25em 0; vertical-align:middle; }
        .cu-form input[type=date] { -webkit-appearance:none; appearance:none; background:aliceblue; color:#333; border:1px solid #cfd8de; border-radius:.7em; padding:.3em .8em; height:2.5em; font:inherit; font-size:.95em; }
        .cu-form select { height:auto; }
        .cu-grid   { width:100%; border-collapse:collapse; font-size:.92em; }
        .cu-grid th { background:#f1f5f8; text-align:right; padding:.35em .5em; border-bottom:1px solid #cfd8de; }
        .cu-grid th:first-child, .cu-grid th:nth-child(2) { text-align:left; }
        .cu-grid td { padding:.3em .5em; border-bottom:1px solid #e6ecf0; text-align:right; }
        .cu-grid td:first-child, .cu-grid td:nth-child(2) { text-align:left; }
        .cu-grid input[type=text] { width:6em; text-align:right; height:2.2em; padding:.2em .4em; }
        .cu-used   { font-weight:700; }
        .cu-neg    { font-weight:700; color:#b02a2a; }
        .cu-hint   { font-size:.85em; color:#777; }
        .cu-info   { background:#eef5fb; border-left:3px solid #4282C1; padding:.6em .9em; margin:.6em 0; font-size:.92em; }
        .cu-warn   { background:#fff6e5; border-left:3px solid #e69500; color:#7a4b00; padding:.6em .9em; margin:.6em 0; font-size:.92em; }
        .cu-done   { background:#eaf7ea; border-left:3px solid #2e8b57; color:#1f5e3a; padding:.6em .9em; margin:.6em 0; font-size:.92em; }
        .cu-btns a { margin-right:.6em; }
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
                            <h3 style="padding-top:2em; line-height:1em">Consignment Stock - Month End</h3>
                        </div>
                    </div>
                    <div class="cu-wrap">
                        <asp:Panel ID="pnlNotReady" runat="server" Visible="false" style="color:#b02a2a; padding:1em 0">
                            Consignment usage is not set up on this database yet. Run <b>Add_ConsignmentUsage.sql</b> first.
                        </asp:Panel>
                        <asp:Panel ID="pnlNoStores" runat="server" Visible="false" class="cu-warn">
                            No consignment locations yet. Settings &gt; Location Master: pick the Sage customer on the location that sits at their premises, then come back here.
                        </asp:Panel>
                        <asp:Panel ID="pnlMain" runat="server">
                            <div class="cu-sec">1. Which customer location, and the date you counted</div>
                            <table class="cu-form">
                                <tr>
                                    <td>Consignment location</td>
                                    <td><asp:DropDownList ID="DDStore" runat="server" Width="360px" CssClass="optiondd" AutoPostBack="true" OnSelectedIndexChanged="DDStore_SelectedIndexChanged"></asp:DropDownList></td>
                                    <td>Count date</td>
                                    <td><asp:TextBox ID="txtCountDate" runat="server" TextMode="Date" Width="160px"></asp:TextBox></td>
                                    <td><asp:LinkButton ID="lbtnStart" runat="server" CssClass="fa fa-play-circle buttonC" OnClick="lbtnStart_Click" ToolTip="Read what the location holds and open the count"> Start month end</asp:LinkButton></td>
                                </tr>
                            </table>
                            <asp:HiddenField ID="hfUsageID" runat="server" Value="" />

                            <asp:Panel ID="pnlRun" runat="server" Visible="false">
                                <div class="cu-sec">2. Count, check, and create the order</div>
                                <asp:Label ID="lblRunInfo" runat="server" CssClass="cu-info" style="display:block"></asp:Label>
                                <asp:Label ID="lblDone" runat="server" CssClass="cu-done" style="display:block" Visible="false"></asp:Label>
                                <span class="cu-hint">On hand = what the location should hold now (opening + delivered + other). Type the counted quantity; Used = on hand &minus; counted. Price is the item's selling price, change it per line if needed. Save counts before printing or creating the order.</span>
                                <asp:GridView ID="GridLines" runat="server" AutoGenerateColumns="false" CssClass="cu-grid" GridLines="None" DataKeyNames="LineID" OnRowDataBound="GridLines_RowDataBound"
                                    EmptyDataText="Nothing has been delivered to this location yet, so there is nothing to count.">
                                    <Columns>
                                        <asp:BoundField DataField="ItemCode" HeaderText="Code" />
                                        <asp:BoundField DataField="ItemDescription" HeaderText="Item" />
                                        <asp:BoundField DataField="Opening" HeaderText="Opening" DataFormatString="{0:N2}" />
                                        <asp:BoundField DataField="Delivered" HeaderText="Delivered" DataFormatString="{0:N2}" />
                                        <asp:BoundField DataField="OtherMoves" HeaderText="Other" DataFormatString="{0:N2}" />
                                        <asp:BoundField DataField="OnHand" HeaderText="On hand" DataFormatString="{0:N2}" />
                                        <asp:TemplateField HeaderText="Counted">
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtCounted" runat="server" Text='<%# Eval("Counted") == null ? "" : string.Format("{0:0.##}", Eval("Counted")) %>'></asp:TextBox>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Used">
                                            <ItemTemplate><asp:Label ID="lblUsed" runat="server" Text='<%# string.Format("{0:N2}", Eval("Used")) %>'></asp:Label></ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Price (excl)">
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtPrice" runat="server" Text='<%# string.Format("{0:0.####}", Eval("UnitPrice")) %>'></asp:TextBox>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField DataField="LineExcl" HeaderText="Value (excl)" DataFormatString="{0:N2}" />
                                    </Columns>
                                </asp:GridView>
                                <p style="text-align:right; margin:.4em 0"><b>Total used (excl): <asp:Label ID="lblTotal" runat="server" Text="0.00"></asp:Label></b></p>
                                <div class="cu-btns">
                                    <asp:LinkButton ID="lbtnSave" runat="server" CssClass="fa fa-save buttonC" OnClick="lbtnSave_Click"> Save counts</asp:LinkButton>
                                    <asp:LinkButton ID="lbtnStatement" runat="server" CssClass="fa fa-print buttonC" OnClick="lbtnStatement_Click" ToolTip="Print the usage statement to agree with the customer"> Print statement</asp:LinkButton>
                                    <asp:LinkButton ID="lbtnCreateSO" runat="server" CssClass="fa fa-file-invoice buttonC" OnClick="lbtnCreateSO_Click" ToolTip="Raise the Sage sales order for the used quantities and take them out of the location"
                                        OnClientClick="return confirm('Create the Sage sales order for the used quantities and take them out of this location? Check the counts and prices first - this cannot be undone here.');"> Create Sales Order</asp:LinkButton>
                                    <asp:LinkButton ID="lbtnDelete" runat="server" CssClass="fa fa-trash buttonCancel" OnClick="lbtnDelete_Click" ForeColor="Red"
                                        OnClientClick="return confirm('Discard this count? Nothing has been sent to Sage.');"> Discard</asp:LinkButton>
                                </div>
                            </asp:Panel>

                            <div class="cu-sec">Previous month ends</div>
                            <asp:GridView ID="GridHistory" runat="server" AutoGenerateColumns="false" CssClass="cu-grid" GridLines="None" DataKeyNames="UsageID" OnRowCommand="GridHistory_RowCommand"
                                EmptyDataText="No month ends yet.">
                                <Columns>
                                    <asp:TemplateField HeaderText="Location">
                                        <ItemTemplate><asp:LinkButton ID="lbtnOpen" runat="server" CommandName="OpenRun" CommandArgument='<%# Eval("UsageID") %>' Text='<%# Eval("StoreDescript") %>'></asp:LinkButton></ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField DataField="CustomerName" HeaderText="Customer" />
                                    <asp:BoundField DataField="CountDate" HeaderText="Count date" DataFormatString="{0:dd MMM yyyy}" />
                                    <asp:BoundField DataField="Status" HeaderText="Status" />
                                    <asp:BoundField DataField="SageSONumber" HeaderText="Sales order" />
                                    <asp:BoundField DataField="TotalExcl" HeaderText="Used (excl)" DataFormatString="{0:N2}" />
                                </Columns>
                            </asp:GridView>
                        </asp:Panel>
                    </div>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
