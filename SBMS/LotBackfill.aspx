<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="LotBackfill.aspx.cs" Inherits="SBMS.LotBackfill" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Lot Backfill (one-off)</title>
    <link rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <style>
        body { font-family: Segoe UI, Arial, sans-serif; font-size: 13px; margin: 2em; color: #222; }
        h1 { font-size: 1.3em; margin-bottom: .2em; }
        .warn { background: #fff4e5; border: 1px solid #e0a800; padding: .8em 1em; margin: 1em 0; max-width: 60em; }
        .status { background: #f4f6f8; border: 1px solid #ccc; padding: .8em 1em; margin: 1em 0; max-width: 60em; }
        .btn { padding: .6em 1.4em; margin-right: .6em; font-size: 1em; cursor: pointer; }
        .btn-apply { background: #c0392b; color: #fff; border: 0; }
        .btn-apply[disabled] { background: #bbb; cursor: not-allowed; }
        table.grid { border-collapse: collapse; margin-top: 1em; }
        table.grid th { background: #34495e; color: #fff; padding: .4em .7em; text-align: left; font-weight: 600; }
        table.grid td { border-bottom: 1px solid #e3e3e3; padding: .35em .7em; }
        table.grid tr:nth-child(even) td { background: #fafafa; }
        .num { text-align: right; }
        .skip { color: #c0392b; }
    </style>
</head>
<body>
<form id="form1" runat="server">

    <h1>Lot Backfill &mdash; one-off, ALL companies</h1>

    <div class="warn">
        <b>What this does.</b> Flags physical items as lot tracked on every company that uses lot
        tracking, then gives a lot number to stock that is currently sitting with none.
        <br /><br />
        History is <b>not</b> rewritten. Each repair is posted as a dated movement &mdash; the quantity
        out with no lot, the same quantity back in under a new <code>SYS-</code> lot, at the store's
        current average cost. Quantity and value are unchanged, so nothing goes to Sage.
        <br /><br />
        Balances with no cost available are <b>skipped</b>, not given a zero-cost lot.
        <br /><br />
        <b>Run this on a restored backup first.</b> Delete this page once it has been applied.
    </div>

    <asp:Button ID="btnPreview" runat="server" Text="Preview" CssClass="btn" OnClick="btnPreview_Click" />
    <asp:TextBox ID="txtConfirm" runat="server" placeholder="type CONFIRM" style="padding:.5em; width:11em;" />
    <asp:Button ID="btnApply" runat="server" Text="Apply" CssClass="btn btn-apply" OnClick="btnApply_Click" />

    <div class="status"><asp:Literal ID="lblStatus" runat="server" /></div>

    <asp:GridView ID="gvPlan" runat="server" AutoGenerateColumns="false" CssClass="grid" GridLines="None">
        <Columns>
            <asp:BoundField HeaderText="Company"   DataField="CompanyName" />
            <asp:BoundField HeaderText="Item"      DataField="ItemCode" />
            <asp:BoundField HeaderText="Store"     DataField="StoreCode" />
            <asp:BoundField HeaderText="Qty"       DataField="Qty"      DataFormatString="{0:N4}" ItemStyle-CssClass="num" HeaderStyle-CssClass="num" />
            <asp:BoundField HeaderText="Unit Cost" DataField="UnitCost" DataFormatString="{0:N4}" ItemStyle-CssClass="num" HeaderStyle-CssClass="num" />
            <asp:BoundField HeaderText="Old Lot"   DataField="OldLot" />
            <asp:BoundField HeaderText="New Lot"   DataField="NewLot" />
            <asp:TemplateField HeaderText="Note">
                <ItemTemplate><span class="skip"><%# Eval("Note") %></span></ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

</form>
</body>
</html>
