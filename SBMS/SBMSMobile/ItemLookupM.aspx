<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ItemLookupM.aspx.cs" Inherits="SBMS.ItemLookupM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Item Lookup</title>
    <link rel="shortcut icon" href="../images/datafusionicon.ico" type="image/x-icon" />
    <link rel="stylesheet" href="css/main.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/main.css") %>" />
    <link rel="stylesheet" href="css/mobile-ui.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/mobile-ui.css") %>" />
    <link rel="manifest" href="../SBMSMobile/manifest.json" />
    <meta name="theme-color" content="#4282C1" />
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
    <meta name="apple-mobile-web-app-title" content="Data Fusion" />
    <link rel="apple-touch-icon" href="../SBMSMobile/icons/icon-192.png" />
    <meta name="format-detection" content="telephone=no" />
    <style>
        .il-box { background: var(--mob-surface); border-radius: var(--mob-radius-lg); box-shadow: var(--mob-shadow-card);
                  border: 0.5px solid var(--mob-border); margin: .6em; padding: .9em 1em; }
        .il-box h4 { margin: 0 0 .2em 0; font-size: 1em; font-weight: 700; color: var(--mob-ink); }
        .il-meta { font-size: .84em; color: var(--mob-text-muted); }
        .il-total { font-size: 1.1em; font-weight: 700; margin-top: .4em; }
        .il-row { display: flex; justify-content: space-between; gap: .6em; font-size: .9em; padding: .45em 0; border-top: 1px solid var(--mob-divider); }
        .il-row span:last-child { font-weight: 700; white-space: nowrap; }
        .il-neg { color: var(--mob-danger-ink); }
    </style>
</head>
<body>
<form id="form1" runat="server">
    <svg class="mob-ico-defs" aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg">
        <symbol id="i-house" viewBox="0 0 24 24"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><path d="M9 22V12h6v10"/></symbol>
        <symbol id="i-search" viewBox="0 0 24 24"><circle cx="11" cy="11" r="8"/><path d="M21 21l-4.3-4.3"/></symbol>
    </svg>

    <div class="mob-topbar">
        <div class="mob-topbar-left">
            <asp:LinkButton ID="lbtnHome" runat="server" OnClick="lbtnHome_Click" CssClass="mob-topbar-back">&#8592; Back</asp:LinkButton>
        </div>
        <span class="mob-topbar-title"><svg class="mob-ico" aria-hidden="true"><use href="#i-search"/></svg>Item Lookup</span>
        <div class="mob-topbar-right">
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click" CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <asp:Panel ID="pnlFilter" runat="server" DefaultButton="lbtnFind" CssClass="mob-filterbar">
        <asp:TextBox ID="txtFind" runat="server" placeholder="Scan barcode or type code / description" autocomplete="off" />
        <asp:LinkButton ID="lbtnFind" runat="server" OnClick="lbtnFind_Click" CssClass="mob-find-btn" title="Find" aria-label="Find"><svg class="mob-ico" aria-hidden="true"><use href="#i-search"/></svg></asp:LinkButton>
    </asp:Panel>

    <asp:Label ID="lblFeedback" runat="server" CssClass="mob-feedback" />

    <%-- Several items match: tap one --%>
    <div class="mob-cardlist">
        <asp:Repeater ID="rptMatches" runat="server">
            <ItemTemplate>
                <div class="mob-card">
                    <asp:LinkButton ID="lbtnPick" runat="server" CssClass="mob-card-btn" CommandArgument='<%# Eval("Code") %>' OnClick="lbtnPick_Click">
                        <div class="mob-card-row"><span class="mob-po-num"><%#: Eval("Code") %></span></div>
                        <div class="mob-supplier"><%#: Eval("Description") %></div>
                    </asp:LinkButton>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>

    <%-- One item: stock on hand per store / bin (and lot) --%>
    <asp:Panel ID="pnlItem" runat="server" Visible="false" CssClass="il-box">
        <h4><asp:Label ID="lblCode" runat="server" /></h4>
        <div class="il-meta"><asp:Label ID="lblDescr" runat="server" /></div>
        <div class="il-total">On hand: <asp:Label ID="lblTotal" runat="server" /></div>
        <asp:Repeater ID="rptStock" runat="server">
            <ItemTemplate>
                <div class="il-row">
                    <span><%#: Eval("Where") %></span>
                    <span class='<%# (decimal)Eval("Qty") < 0 ? "il-neg" : "" %>'><%#: Eval("QtyText") %></span>
                </div>
            </ItemTemplate>
        </asp:Repeater>
        <asp:Label ID="lblNoStock" runat="server" CssClass="il-meta" Visible="false" Text="No stock on hand in any store." />
    </asp:Panel>
</form>
<script>
    // Ready for the next scan.
    (function () { var t = document.getElementById('txtFind'); if (t) { t.focus(); t.select(); } })();
</script>
</body>
</html>
