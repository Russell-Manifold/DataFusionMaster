<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="Traceability.aspx.cs" Inherits="SBMS.Traceability" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cci" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Traceability &amp; Recall</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <style>
        .trace-box   { background:#EAF3FA; border:1px solid #cfd8df; border-radius:.4em;
                       padding:.8em 1em; margin:0 auto 1em; max-width:60em; text-align:left; }
        .trace-fact  { display:inline-block; min-width:14em; margin:.15em 1.5em .15em 0; }
        .trace-fact b{ color:#4282C1; }
        .trace-warn  { color:#c0392b; font-weight:700; }
        .trace-ok    { color:#1e8449; font-weight:700; }
        .trace-head  { font-weight:700; color:#4282C1; margin:.8em 0 .3em; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="content">
            <div class="container">
                <div class="row 150%">
                    <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg"/></a></div>
                    <div class="8u 12u$(medium)">
                        <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" OnClick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="lbtnExcel" runat="server" class="buttonC icon fa-download" OnClick="lbtnExcel_Click">&nbsp;Excel</asp:LinkButton>
                        <asp:LinkButton ID="lbtnLogOut" runat="server" class="buttonTransparent icon fa-eject" style="float:right;" ToolTip="Log Out" OnClick="lbtnLogOut_Click">&nbsp;</asp:LinkButton><br />
                        <h3 style="padding-top:0; line-height:1em">Traceability &amp; Recall</h3>
                        <p style="margin-top:.2em; color:#666; font-size:.9em">
                            Enter a serial number, a supplier batch or a purchase order. A serial shows that unit's full history;
                            a batch or purchase order shows every unit under it and every customer who received one.</p>
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server" style="float:right" class="logoImg" /></div>
                </div>

                <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                    <ContentTemplate>
                        <div class="row 150%">
                            <div class="12u 12u$(medium)" style="text-align:center">
                                <asp:TextBox ID="txtSearch" runat="server" style="width:22em; text-align:center; font-size:1.05em"
                                     placeholder="Scan or type a serial, batch or PO number"
                                     autocomplete="off" autocorrect="off" autocapitalize="off"
                                     AutoPostBack="true" OnTextChanged="txtSearch_TextChanged"></asp:TextBox>
                                <asp:LinkButton ID="lbtnSearch" runat="server" CssClass="icon fa-search buttonSage" OnClick="lbtnSearch_Click">&nbsp;Search</asp:LinkButton>
                                <h4><asp:Label ID="lblErr" runat="server" ForeColor="Red"></asp:Label></h4>
                            </div>
                        </div>

                        <%-- What was found: the unit or the batch itself --%>
                        <asp:Panel ID="pnlSummary" runat="server" Visible="false">
                            <div class="row 150%">
                                <div class="12u 12u$(medium)">
                                    <div class="trace-box"><asp:Literal ID="litSummary" runat="server"></asp:Literal></div>
                                </div>
                            </div>
                        </asp:Panel>

                        <%-- Units: one row per serial (a batch search lists them all) --%>
                        <asp:Panel ID="pnlUnits" runat="server" Visible="false">
                            <div class="row 150%">
                                <div class="1u 12u$(medium)">&nbsp;</div>
                                <div class="10u 12u$(medium)" style="text-align:center">
                                    <div class="trace-head">Units</div>
                                    <asp:GridView ID="GridUnits" runat="server" CssClass="gridview" AutoGenerateColumns="false" Width="100%"
                                                  OnRowDataBound="GridUnits_RowDataBound">
                                        <HeaderStyle CssClass="gridViewHeader" />
                                        <RowStyle CssClass="gridViewRow" />
                                        <AlternatingRowStyle CssClass="gridViewAltRow" />
                                        <Columns>
                                            <asp:BoundField HeaderText="Serial" DataField="Serial" />
                                            <asp:BoundField HeaderText="Batch" DataField="Batch" />
                                            <asp:BoundField HeaderText="Item" DataField="ItemCode" />
                                            <asp:BoundField HeaderText="Description" DataField="ItemDescription" />
                                            <asp:BoundField HeaderText="Expires" DataField="UseByDate" DataFormatString="{0:dd MMM yyyy}" ItemStyle-HorizontalAlign="Center" />
                                            <asp:BoundField HeaderText="On Hand" DataField="OnHand" DataFormatString="{0:N0}" ItemStyle-HorizontalAlign="Center" />
                                            <asp:BoundField HeaderText="Status" DataField="Status" ItemStyle-HorizontalAlign="Center" />
                                        </Columns>
                                    </asp:GridView>
                                </div>
                                <div class="1u 12u$(medium)">&nbsp;</div>
                            </div>
                        </asp:Panel>

                        <%-- Every movement: in from the supplier, transfers, out to the customer --%>
                        <asp:Panel ID="pnlHistory" runat="server" Visible="false">
                            <div class="row 150%">
                                <div class="1u 12u$(medium)">&nbsp;</div>
                                <div class="10u 12u$(medium)" style="text-align:center">
                                    <div class="trace-head">Movement history</div>
                                    <asp:GridView ID="GridHistory" runat="server" CssClass="gridview" AutoGenerateColumns="false" Width="100%">
                                        <HeaderStyle CssClass="gridViewHeader" />
                                        <RowStyle CssClass="gridViewRow" />
                                        <AlternatingRowStyle CssClass="gridViewAltRow" />
                                        <Columns>
                                            <asp:BoundField HeaderText="Date" DataField="TransactionDate" DataFormatString="{0:dd MMM yyyy HH:mm}" />
                                            <asp:BoundField HeaderText="Serial" DataField="Serial" />
                                            <asp:BoundField HeaderText="Type" DataField="TypeDescr" />
                                            <asp:BoundField HeaderText="Reference" DataField="Reference" />
                                            <asp:BoundField HeaderText="Store" DataField="StoreCode" />
                                            <asp:BoundField HeaderText="Qty" DataField="Qty" DataFormatString="{0:N0}" ItemStyle-HorizontalAlign="Center" />
                                        </Columns>
                                    </asp:GridView>
                                </div>
                                <div class="1u 12u$(medium)">&nbsp;</div>
                            </div>
                        </asp:Panel>

                        <%-- The recall list: who received stock from this batch --%>
                        <asp:Panel ID="pnlCustomers" runat="server" Visible="false">
                            <div class="row 150%">
                                <div class="1u 12u$(medium)">&nbsp;</div>
                                <div class="10u 12u$(medium)" style="text-align:center">
                                    <div class="trace-head">Customers supplied &mdash; recall list</div>
                                    <asp:GridView ID="GridCustomers" runat="server" CssClass="gridview" AutoGenerateColumns="false" Width="100%">
                                        <HeaderStyle CssClass="gridViewHeader" />
                                        <RowStyle CssClass="gridViewRow" />
                                        <AlternatingRowStyle CssClass="gridViewAltRow" />
                                        <Columns>
                                            <asp:BoundField HeaderText="Customer" DataField="Customer" />
                                            <asp:BoundField HeaderText="Sage Document" DataField="Reference" />
                                            <asp:BoundField HeaderText="Date Supplied" DataField="Supplied" DataFormatString="{0:dd MMM yyyy}" />
                                            <asp:BoundField HeaderText="Units" DataField="Units" DataFormatString="{0:N0}" ItemStyle-HorizontalAlign="Center" />
                                            <asp:BoundField HeaderText="Serials" DataField="Serials" />
                                        </Columns>
                                    </asp:GridView>
                                </div>
                                <div class="1u 12u$(medium)">&nbsp;</div>
                            </div>
                        </asp:Panel>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
        </div>
    </form>
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</body>
</html>
