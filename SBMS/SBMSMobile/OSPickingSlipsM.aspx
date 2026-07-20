<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="OSPickingSlipsM.aspx.cs" Inherits="SBMS.OSPickingSlipsM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Picking Slips</title>
    <link rel="shortcut icon" href="../images/datafusionicon.ico" type="image/x-icon" />
    <link rel="stylesheet" href="css/main.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/main.css") %>" />
    <link rel="stylesheet" href="css/mobile-ui.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/mobile-ui.css") %>" />
    <!-- PWA -->
    <link rel="manifest" href="../SBMSMobile/manifest.json" />
    <meta name="theme-color" content="#4282C1" />
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
    <meta name="apple-mobile-web-app-title" content="Data Fusion" />
    <link rel="apple-touch-icon" href="../SBMSMobile/icons/icon-192.png" />
    <meta name="format-detection" content="telephone=no" />
    <style>
        .mob-so-ref { font-size: .78em; color: var(--mob-text-faint); margin-top: .15em; }
    </style>
</head>
<body>
<form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" />
    <%-- Icon library (outline, 2px, rounded) — referenced via <use href="#i-…"> --%>
    <svg class="mob-ico-defs" aria-hidden="true" focusable="false" xmlns="http://www.w3.org/2000/svg">
        <symbol id="i-house" viewBox="0 0 24 24"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><path d="M9 22V12h6v10"/></symbol>
        <symbol id="i-clipboard" viewBox="0 0 24 24"><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/><rect x="8" y="2" width="8" height="4" rx="1"/><path d="M9 12l2 2 4-4"/></symbol>
        <symbol id="i-search" viewBox="0 0 24 24"><circle cx="11" cy="11" r="8"/><path d="M21 21l-4.3-4.3"/></symbol>
    </svg>


    <%-- Loading overlay --%>
    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="upMain">
        <ProgressTemplate>
            <div style="position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:999;display:flex;align-items:center;justify-content:center;color:#fff;font-size:1.1em;">
                Loading&hellip;
            </div>
        </ProgressTemplate>
    </asp:UpdateProgress>

    <%-- Top bar --%>
    <div class="mob-topbar">
        <div class="mob-topbar-left">
            <asp:LinkButton ID="lbtnBack" runat="server" OnClick="lbtnBack_Click" CssClass="mob-topbar-back">&#8592; Back</asp:LinkButton>
            <asp:LinkButton ID="lbtnHome" runat="server" OnClick="lbtnHome_Click" CssClass="mob-topbar-icon" title="Home" aria-label="Home"><svg class="mob-ico" aria-hidden="true"><use href="#i-house"/></svg></asp:LinkButton>
        </div>
        <span class="mob-topbar-title"><svg class="mob-ico" aria-hidden="true"><use href="#i-clipboard"/></svg>Picking Slips</span>
        <div class="mob-topbar-right">
            <asp:Label ID="lblUsername" runat="server" CssClass="mob-topbar-user" style="display:none;" />
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click" CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>

        <%-- Filter bar --%>
        <asp:Panel ID="pnlFilter" runat="server" DefaultButton="lbtnFind" CssClass="mob-filterbar">
            <asp:TextBox ID="txtfind" runat="server" placeholder="PS # / Customer / SO #" />
            <asp:LinkButton ID="lbtnFind" runat="server" OnClick="lbtnFind_Click" CssClass="mob-find-btn" title="Find" aria-label="Find"><svg class="mob-ico" aria-hidden="true"><use href="#i-search"/></svg></asp:LinkButton>
            <asp:DropDownList ID="ddStatus" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddStatus_SelectedIndexChanged" />
            <asp:LinkButton ID="lbtnSort" runat="server" OnClick="lbtnSort_Click" CssClass="mob-sort-btn" title="Sort by document number">&#8593;</asp:LinkButton>
        </asp:Panel>

        <%-- Card list --%>
        <div class="mob-cardlist">
            <asp:Repeater ID="rptSlips" runat="server" OnItemDataBound="rptSlips_ItemDataBound">
                <ItemTemplate>
                    <div class="mob-card">
                        <asp:LinkButton ID="lbtnPS" runat="server" CssClass="mob-card-btn"
                                        CommandArgument='<%# Eval("PSID") %>'
                                        OnClick="lbtnPS_Click">
                            <div class="mob-card-row">
                                <span class="mob-po-num"><%# Eval("PSIntNumber") %></span>
                                <span class="mob-badge" id="spnBadge" runat="server"></span>
                            </div>
                            <div class="mob-supplier"><%# Eval("CustSupName") %></div>
                            <div class="mob-meta">
                                <span class="mob-so-ref">SO: <%# Eval("DocumentNumber") %></span>
                                <span>Due: <%# Eval("DueDelDate", "{0:dd MMM yyyy}") %></span>
                            </div>
                        </asp:LinkButton>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <asp:Label ID="lblEmpty" runat="server" CssClass="mob-empty"
                Visible="false" Text="No active picking slips found." />
        </div>

    </ContentTemplate>
    </asp:UpdatePanel>
</form>
<script src="../SBMSMobile/js/sweetalert2.all.min.js"></script>
<script>
    // Hide URL bar on mobile (best-effort; full hiding requires installing as PWA)
    if (!window.matchMedia('(display-mode: standalone)').matches) {
        window.addEventListener('load', function () {
            document.body.style.height = (window.screen.height + 50) + 'px';
            setTimeout(function () { window.scrollTo(0, 1); }, 50);
            setTimeout(function () { document.body.style.height = ''; }, 600);
        });
    }
</script>
</body>
</html>
