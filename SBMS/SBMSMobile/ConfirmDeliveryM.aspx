<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ConfirmDeliveryM.aspx.cs" Inherits="SBMS.ConfirmDeliveryM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Confirm Delivery</title>
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
        .mob-so-ref { font-size: .78em; color: var(--mob-text-faint); margin-top: .15em; }
        .cd-box { background: var(--mob-surface); border-radius: var(--mob-radius-lg); box-shadow: var(--mob-shadow-card);
                  border: 0.5px solid var(--mob-border); margin: .6em; padding: .9em 1em; }
        .cd-box h4 { margin: 0 0 .4em 0; font-size: 1em; font-weight: 700; color: var(--mob-ink); }
        .cd-meta { font-size: .84em; color: var(--mob-text-muted); line-height: 1.5; }
        .cd-line { display: flex; justify-content: space-between; gap: .6em; font-size: .84em; padding: .35em 0; border-top: 1px solid var(--mob-divider); }
        .cd-line span:last-child { font-weight: 700; white-space: nowrap; }
        .cd-box .mob-panel-buttons { margin-top: .4em; }
    </style>
</head>
<body>
<form id="form1" runat="server">
    <svg class="mob-ico-defs" aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg">
        <symbol id="i-house" viewBox="0 0 24 24"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><path d="M9 22V12h6v10"/></symbol>
        <symbol id="i-truck" viewBox="0 0 24 24"><path d="M1 3h15v13H1z"/><path d="M16 8h4l3 3v5h-7V8z"/><circle cx="5.5" cy="18.5" r="2.5"/><circle cx="18.5" cy="18.5" r="2.5"/></symbol>
        <symbol id="i-search" viewBox="0 0 24 24"><circle cx="11" cy="11" r="8"/><path d="M21 21l-4.3-4.3"/></symbol>
    </svg>

    <%-- Top bar --%>
    <div class="mob-topbar">
        <div class="mob-topbar-left">
            <asp:LinkButton ID="lbtnBack" runat="server" OnClick="lbtnBack_Click" CssClass="mob-topbar-back">&#8592; Back</asp:LinkButton>
            <asp:LinkButton ID="lbtnHome" runat="server" OnClick="lbtnHome_Click" CssClass="mob-topbar-icon" title="Home" aria-label="Home"><svg class="mob-ico" aria-hidden="true"><use href="#i-house"/></svg></asp:LinkButton>
        </div>
        <span class="mob-topbar-title"><svg class="mob-ico" aria-hidden="true"><use href="#i-truck"/></svg>Confirm Delivery</span>
        <div class="mob-topbar-right">
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click" CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <asp:Label ID="lblFeedback" runat="server" CssClass="mob-feedback" />

    <%-- List: closed-off deliveries not yet confirmed --%>
    <asp:Panel ID="pnlList" runat="server">
        <asp:Panel ID="pnlFilter" runat="server" DefaultButton="lbtnFind" CssClass="mob-filterbar">
            <asp:TextBox ID="txtFind" runat="server" placeholder="PS # / Customer / SO #" />
            <asp:LinkButton ID="lbtnFind" runat="server" OnClick="lbtnFind_Click" CssClass="mob-find-btn" title="Find" aria-label="Find"><svg class="mob-ico" aria-hidden="true"><use href="#i-search"/></svg></asp:LinkButton>
        </asp:Panel>
        <div class="mob-cardlist">
            <asp:Repeater ID="rptDeliveries" runat="server">
                <ItemTemplate>
                    <div class="mob-card">
                        <asp:LinkButton ID="lbtnOpen" runat="server" CssClass="mob-card-btn"
                                        CommandArgument='<%# Eval("PSID") %>' OnClick="lbtnOpen_Click">
                            <div class="mob-card-row">
                                <span class="mob-po-num"><%#: Eval("PSIntNumber") %></span>
                                <span class="mob-badge mob-badge-invoiced"><%# Eval("Delivered", "{0:dd MMM}") %></span>
                            </div>
                            <div class="mob-supplier"><%#: Eval("Customer") %></div>
                            <div class="mob-meta">
                                <span class="mob-so-ref">SO: <%#: Eval("SalesOrder") %></span>
                                <span><%#: Eval("POD") %></span>
                            </div>
                        </asp:LinkButton>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
            <asp:Label ID="lblEmpty" runat="server" CssClass="mob-empty" Visible="false"
                Text="No deliveries waiting to be confirmed (last 60 days)." />
        </div>
    </asp:Panel>

    <%-- One delivery: who received it, and a comment --%>
    <asp:Panel ID="pnlDetail" runat="server" Visible="false">
        <asp:HiddenField ID="hfPSID" runat="server" />
        <asp:HiddenField ID="hfActionToken" runat="server" />
        <div class="cd-box">
            <h4><asp:Label ID="lblSlip" runat="server" /></h4>
            <div class="cd-meta"><asp:Literal ID="litMeta" runat="server" /></div>
        </div>
        <div class="cd-box">
            <asp:Repeater ID="rptLines" runat="server">
                <ItemTemplate>
                    <div class="cd-line"><span><%#: Eval("ItemCode") %> - <%#: Eval("Description") %></span><span><%#: Eval("Qty") %></span></div>
                </ItemTemplate>
            </asp:Repeater>
        </div>
        <div class="cd-box">
            <div class="mob-panel-row">
                <label for="txtReceivedBy">Received by (name)</label>
                <asp:TextBox ID="txtReceivedBy" runat="server" MaxLength="100" />
            </div>
            <div class="mob-panel-row">
                <label for="txtComment">Comment</label>
                <asp:TextBox ID="txtComment" runat="server" MaxLength="500" />
            </div>
            <asp:Label ID="lblError" runat="server" CssClass="mob-panel-error" Visible="false" />
            <div class="mob-panel-buttons">
                <asp:LinkButton ID="lbtnCancel" runat="server" OnClick="lbtnCancel_Click" CssClass="mob-btn-cancel">Cancel</asp:LinkButton>
                <asp:LinkButton ID="lbtnConfirm" runat="server" OnClick="lbtnConfirm_Click" CssClass="mob-btn-confirm"
                    OnClientClick="this.style.pointerEvents='none'; this.innerText='Saving...';">Confirm delivery</asp:LinkButton>
            </div>
        </div>
    </asp:Panel>
</form>
</body>
</html>
