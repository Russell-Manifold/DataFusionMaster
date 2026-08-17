<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="ReceivingRecovery.aspx.cs" Inherits="SBMS.ReceivingRecovery" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Stuck Receipts</title>
    <link rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <style>
        body { font-family: Segoe UI, Arial, sans-serif; font-size: 13px; color: #222; margin: 2em; }
        h1 { font-size: 1.35em; margin-bottom: .1em; }
        .sub { color: #666; margin-bottom: 1.2em; }
        .warn { background: #fff4e5; border: 1px solid #e0a800; padding: .9em 1.1em; max-width: 62em; margin-bottom: 1.4em; line-height: 1.5; }
        .msg { padding: .7em 1em; max-width: 62em; margin-bottom: 1em; }
        .msg.ok  { background: #e8f5e9; border: 1px solid #66bb6a; }
        .msg.bad { background: #fdecea; border: 1px solid #e57373; }
        .none { background: #f4f6f8; border: 1px solid #ccc; padding: 1em; max-width: 62em; }
        table.gridview { border-collapse: collapse; width: 100%; max-width: 78em; }
        table.gridview th { background: #34495e; color: #fff; padding: .5em .7em; text-align: left; font-weight: 600; }
        table.gridview td { border-bottom: 1px solid #e3e3e3; padding: .45em .7em; vertical-align: middle; }
        table.gridview tr:nth-child(even) td { background: #fafafa; }
        .num { text-align: right; }
        .no  { color: #c0392b; font-weight: 600; }
        .yes { color: #2e7d32; font-weight: 600; }
        .btn { padding: .45em .9em; cursor: pointer; }
        .btn-rec { background: #2e7d32; color: #fff; border: 0; }
        .btn-non { background: #fff; color: #c0392b; border: 1px solid #c0392b; }
        input[type=text] { padding: .4em; width: 11em; }
        a.back { color: #34495e; }
    </style>
</head>
<body>
<form id="form1" runat="server">

    <h1>Stuck Receipts</h1>
    <div class="sub"><a href="Dashboard.aspx" class="back">&#8592; Dashboard</a></div>

    <div class="warn">
        <b>These receipts sent a supplier invoice to Sage but did not finish.</b>
        Because the invoice went across, the system blocked the purchase order to stop a
        second one being sent for the same goods.
        <br /><br />
        <b>Check Sage before releasing anything here.</b> Find the supplier invoice for that
        delivery and enter its number &mdash; that records it so nobody invoices it twice.
        Only use <i>No invoice was posted</i> if you have looked and there is genuinely
        nothing in Sage.
        <br /><br />
        <b>Stock received</b> tells you whether the goods already reached this system.
        If it says <span class="yes">Yes</span>, do not receive those lines again &mdash;
        carry on with whatever is still outstanding.
    </div>

    <asp:Label ID="lblMsg" runat="server" CssClass="msg" Visible="true" />

    <asp:Panel ID="pnlNone" runat="server" CssClass="none" Visible="false">
        No stuck receipts. Nothing to do here.
    </asp:Panel>

    <p>Stuck receipts: <b><asp:Label ID="lblCount" runat="server" Text="0" /></b></p>

    <asp:GridView ID="gvStuck" runat="server" AutoGenerateColumns="false" CssClass="gridview"
                  GridLines="None" OnRowCommand="gvStuck_RowCommand" DataKeyNames="DocID">
        <Columns>
            <asp:BoundField HeaderText="PO Number"        DataField="PONumber" />
            <asp:BoundField HeaderText="Supplier"         DataField="Supplier" />
            <asp:BoundField HeaderText="Value"            DataField="Total" DataFormatString="{0:N2}"
                            ItemStyle-CssClass="num" HeaderStyle-CssClass="num" />
            <asp:BoundField HeaderText="Date"             DataField="LastActivity" DataFormatString="{0:dd MMM yyyy HH:mm}" />
            <asp:BoundField HeaderText="Invoices on record" DataField="Recorded" />
            <asp:TemplateField HeaderText="Stock received">
                <ItemTemplate>
                    <span class='<%# Convert.ToString(Eval("StockIn")) == "Yes" ? "yes" : "no" %>'><%# Eval("StockIn") %></span>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Sage invoice number">
                <ItemTemplate>
                    <asp:TextBox ID="txtInvNum" runat="server" MaxLength="40" />
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Release">
                <ItemTemplate>
                    <asp:Button ID="btnRecord" runat="server" Text="Record &amp; Release" CssClass="btn btn-rec"
                                CommandName="RecordUnlock" CommandArgument='<%# Eval("DocID") %>' />
                    <asp:Button ID="btnNoInv" runat="server" Text="No invoice was posted" CssClass="btn btn-non"
                                CommandName="UnlockOnly" CommandArgument='<%# Eval("DocID") %>'
                                OnClientClick="return confirm('Only do this if you have checked Sage and there is NO invoice for this delivery. Releasing without recording one means the next receive will send a new invoice. Continue?');" />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

</form>
</body>
</html>
