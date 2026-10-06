<%-- Job Card GP Analysis: what each completed job earned and what it cost, including the
     stock drawn that the customer never saw. Queries are in Classes/JobCardGP.cs. --%>
<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="JobCardGP.aspx.cs" Inherits="SBMS.JobCardGPPage" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Job Card GP Analysis</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="prologue/assets/css/main.css" />
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    <style>
        .jg-wrap   { max-width:1200px; margin:auto; text-align:left; }
        .jg-sec    { color:#e68a00; font-weight:600; margin:1.4em 0 .4em; border-bottom:1px solid #dfe6eb; padding-bottom:.2em; }
        .jg-form td { padding:.25em .6em .25em 0; vertical-align:middle; }
        .jg-form input[type=text] { height:2.5em; }
        /* Date pickers: same look as the rest of the form (aliceblue, rounded, blue focus), not the browser default. */
        .jg-form input[type=date] { -webkit-appearance:none; appearance:none; background:aliceblue; color:#333; border:1px solid #cfd8de; border-radius:.7em; padding:.3em .8em; height:2.5em; font:inherit; font-size:.95em; }
        .jg-form input[type=date]:focus { outline:none; border-color:#4282C1; box-shadow:0 0 0 2px rgba(66,130,193,.25); }
        .jg-form input[type=date]::-webkit-calendar-picker-indicator { cursor:pointer; opacity:.55; padding:.15em; border-radius:.3em; }
        .jg-form input[type=date]::-webkit-calendar-picker-indicator:hover { opacity:1; background:#dde9f5; }
        .jg-grid   { width:100%; border-collapse:collapse; font-size:.9em; }
        .jg-grid th { background:#f1f5f8; text-align:left; padding:.35em .5em; border-bottom:1px solid #cfd8de; }
        .jg-grid td { padding:.3em .5em; border-bottom:1px solid #e6ecf0; }
        .jg-num    { text-align:right !important; }
        .jg-hint   { font-size:.85em; color:#777; }
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
                            <h3 style="padding-top:2em; line-height:1em">Job Card GP Analysis</h3>
                        </div>
                    </div>

                    <div class="jg-wrap">
                        <asp:Panel ID="pnlNotReady" runat="server" Visible="false" style="color:#b02a2a; padding:1em 0">
                            This report is not set up on this database yet. Run <b>Add_JobCardHideLines.sql</b> first.
                        </asp:Panel>

                        <asp:Panel ID="pnlMain" runat="server">
                            <table class="jg-form">
                                <tr>
                                    <td>Jobs completed from</td>
                                    <td><asp:TextBox ID="txtFrom" runat="server" TextMode="Date" Width="160px"></asp:TextBox></td>
                                    <td>to</td>
                                    <td><asp:TextBox ID="txtTo" runat="server" TextMode="Date" Width="160px"></asp:TextBox></td>
                                    <td>Search</td>
                                    <td><asp:TextBox ID="txtSearch" runat="server" Width="220px" MaxLength="100" placeholder="Job card, sales order or customer" ToolTip="Any part of the job card number, sales order number or customer name"></asp:TextBox></td>
                                    <td><asp:LinkButton ID="lbtnShow" runat="server" CssClass="fa fa-search buttonC" OnClick="lbtnShow_Click"> Show</asp:LinkButton></td>
                                </tr>
                            </table>
                            <span class="jg-hint">Cost is everything drawn from stock on the job card, at the cost it left the store at, whether or not the customer saw the line. Materials only: labour and bought-in services are not included.</span>

                            <div class="jg-sec">Jobs</div>
                            <asp:GridView ID="GridJobs" runat="server" AutoGenerateColumns="false" CssClass="jg-grid" GridLines="None" DataKeyNames="JCID"
                                OnRowCommand="GridJobs_RowCommand" ShowFooter="true" EmptyDataText="No completed job cards in this period.">
                                <Columns>
                                    <asp:BoundField DataField="CompleteDate" HeaderText="Completed" DataFormatString="{0:dd MMM yyyy}" />
                                    <asp:BoundField DataField="JCNumber" HeaderText="Job card" />
                                    <asp:BoundField DataField="SONumber" HeaderText="Sales order" />
                                    <asp:BoundField DataField="Customer" HeaderText="Customer" />
                                    <asp:BoundField DataField="JCSummary" HeaderText="Summary" />
                                    <asp:BoundField DataField="AddedLines" HeaderText="Added lines" />
                                    <asp:BoundField DataField="Revenue" HeaderText="Revenue" DataFormatString="{0:N2}" ItemStyle-CssClass="jg-num" HeaderStyle-CssClass="jg-num" FooterStyle-CssClass="jg-num" />
                                    <asp:BoundField DataField="OrderCost" HeaderText="Cost: order lines" DataFormatString="{0:N2}" ItemStyle-CssClass="jg-num" HeaderStyle-CssClass="jg-num" FooterStyle-CssClass="jg-num" />
                                    <asp:BoundField DataField="AddedCost" HeaderText="Cost: added lines" DataFormatString="{0:N2}" ItemStyle-CssClass="jg-num" HeaderStyle-CssClass="jg-num" FooterStyle-CssClass="jg-num" />
                                    <asp:BoundField DataField="GP" HeaderText="GP" DataFormatString="{0:N2}" ItemStyle-CssClass="jg-num" HeaderStyle-CssClass="jg-num" FooterStyle-CssClass="jg-num" />
                                    <asp:BoundField DataField="Margin" HeaderText="Margin" DataFormatString="{0:P1}" ItemStyle-CssClass="jg-num" HeaderStyle-CssClass="jg-num" FooterStyle-CssClass="jg-num" />
                                    <asp:TemplateField HeaderText="">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="lbtnComp" runat="server" CommandName="Components" CommandArgument='<%# Eval("JCID") %>'>Components</asp:LinkButton>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>

                            <asp:Panel ID="pnlComp" runat="server" Visible="false">
                                <div class="jg-sec"><asp:Label ID="lblCompTitle" runat="server" Text=""></asp:Label></div>
                                <asp:GridView ID="GridComp" runat="server" AutoGenerateColumns="false" CssClass="jg-grid" GridLines="None" EmptyDataText="Nothing was drawn from stock on this job card.">
                                    <Columns>
                                        <asp:BoundField DataField="ItemCode" HeaderText="Code" />
                                        <asp:BoundField DataField="ItemDescription" HeaderText="Item" />
                                        <asp:BoundField DataField="Source" HeaderText="Line" />
                                        <asp:BoundField DataField="Qty" HeaderText="Qty drawn" DataFormatString="{0:0.####}" ItemStyle-CssClass="jg-num" HeaderStyle-CssClass="jg-num" />
                                        <asp:BoundField DataField="Cost" HeaderText="Cost" DataFormatString="{0:N2}" ItemStyle-CssClass="jg-num" HeaderStyle-CssClass="jg-num" />
                                    </Columns>
                                </asp:GridView>
                            </asp:Panel>
                        </asp:Panel>
                    </div>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
