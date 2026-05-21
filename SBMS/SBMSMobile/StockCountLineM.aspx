<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="StockCountLineM.aspx.cs" Inherits="SBMS.StockCountLineM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Count Stock</title>
    <link rel="shortcut icon" href="../images/datafusionicon.ico" type="image/x-icon" />
    <link rel="stylesheet" href="css/main.css" />
    <link rel="stylesheet" href="css/mobile-ui.css" />
    <link rel="manifest" href="../SBMSMobile/manifest.json" />
    <meta name="theme-color" content="#4282C1" />
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
    <meta name="apple-mobile-web-app-title" content="Data Fusion" />
    <link rel="apple-touch-icon" href="../SBMSMobile/icons/icon-192.png" />
    <meta name="format-detection" content="telephone=no" />
    <style>
        /* ── Calculator overlay ────────────────────────────────────────── */
        .calc-overlay {
            position: fixed; inset: 0; background: #f5f6f8; z-index: 500;
            display: flex; flex-direction: column;
        }
        .calc-header {
            background: #4282C1; color: #fff; padding: .7rem 1rem;
            display: flex; align-items: center; gap: .5rem;
        }
        .calc-item-code {
            font-size: 1.1em; font-weight: 700; flex: 1;
        }
        .calc-item-desc {
            font-size: .82em; opacity: .9;
        }
        .calc-qty-hint {
            background: rgba(255,255,255,.2); color: #fff;
            padding: .15em .6em; border-radius: .4em; font-size: .78em;
        }
        .calc-body {
            flex: 1; display: flex; flex-direction: column; padding: .8rem;
            overflow-y: auto;
        }
        .calc-expr-display {
            background: #fff; border: 2px solid #4282C1; border-radius: .6em;
            padding: .8rem; margin-bottom: .6rem; min-height: 5em;
            display: flex; flex-direction: column; justify-content: center;
        }
        .calc-expr-text {
            font-size: 1.4em; font-weight: 600; color: #333;
            word-break: break-all; line-height: 1.3;
        }
        .calc-total-text {
            font-size: 2em; font-weight: 700; color: #4282C1;
            text-align: right; margin-top: .2em;
        }
        .calc-pad {
            display: grid; grid-template-columns: 1fr 1fr 1fr 1fr;
            gap: .5rem;
        }
        .calc-btn {
            height: 3.6em; border: none; border-radius: .5em;
            font-size: 1.15em; font-weight: 600; cursor: pointer;
            background: #fff; color: #333;
            box-shadow: 0 1px 3px rgba(0,0,0,.1);
            -webkit-tap-highlight-color: transparent;
            transition: background .1s;
        }
        .calc-btn:active { background: #e0e0e0; }
        .calc-btn--op {
            background: #e8f0fe; color: #4282C1;
        }
        .calc-btn--op:active { background: #d0e0f8; }
        .calc-btn--clear {
            background: #fce4e4; color: #c00;
        }
        .calc-btn--del {
            background: #fff3e0; color: #e65100;
        }
        .calc-btn--save {
            background: #4caf50; color: #fff; grid-column: span 2;
        }
        .calc-btn--cancel {
            background: #eee; color: #666; grid-column: span 2;
        }

        /* ── Store badge in top bar ──────────────────────────────────── */
        .mob-store-badge {
            background: rgba(255,255,255,.2); color: #fff;
            padding: .15em .55em; border-radius: .4em;
            font-size: .7em; font-weight: 600;
        }

        /* ── Count expression in line card ───────────────────────────── */
        .mob-count-expr {
            font-size: .75em; color: #888; margin-top: .15em;
            font-style: italic;
        }
        .mob-barcode-chips {
            display: flex; flex-wrap: wrap; gap: .25em; margin-top: .3em;
        }
        .mob-barcode-chip {
            background: #f0f4f8; color: #555; font-size: .68em;
            padding: .1em .45em; border-radius: .35em; border: 1px solid #ddd;
            white-space: nowrap;
        }
        .mob-barcode-chip-qty {
            color: #4282C1; font-weight: 600;
        }

        /* ── Compact line cards (override mobile-ui.css) ─────────────── */
        .mob-linelist { padding: .25em .4em 5em .4em; }
        .mob-linecard { margin-bottom: .35em; }
        .mob-card-main { padding: .45em .6em .4em .7em !important; }
        .mob-item-code  { font-size: .88em; }
        .mob-item-descr { font-size: .74em; margin-top: .1em; }
        .mob-qty-row { margin-top: .25em; gap: .25em; }
        .mob-qty-badge { font-size: .65em; padding: .15em .5em; }
        .mob-barcode-chips { margin-top: .15em; gap: .15em; }
        .mob-barcode-chip { font-size: .62em; padding: .08em .35em; }
        .mob-count-expr { font-size: .68em; margin-top: .08em; }
        .mob-saved-badge { font-size: .62em; padding: .15em .5em; margin-left: .3em; }
        .mob-action-btn { width: 2.8em; font-size: 1.3em; }
    </style>
</head>
<body>
<form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" />

    <%-- Loading overlay --%>
    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="upMain">
        <ProgressTemplate>
            <div style="position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:999;
                        display:flex;align-items:center;justify-content:center;color:#fff;font-size:1.1em;">
                Loading&hellip;
            </div>
        </ProgressTemplate>
    </asp:UpdateProgress>

    <%-- Top bar --%>
    <div class="mob-topbar">
        <div class="mob-topbar-left">
            <asp:LinkButton ID="lbtnBack" runat="server" OnClick="lbtnBack_Click" CssClass="mob-topbar-back">&#8592; Back</asp:LinkButton>
            <asp:LinkButton ID="lbtnHome" runat="server" OnClick="lbtnHome_Click" CssClass="mob-topbar-icon" title="Home">&#127968;</asp:LinkButton>
        </div>
        <span class="mob-topbar-title">
            &#128202; <asp:Label ID="lblCountRefTop" runat="server" />
            <span class="mob-store-badge"><asp:Label ID="lblStoreBadge" runat="server" /></span>
        </span>
        <div class="mob-topbar-right">
            <asp:Label ID="lblUsername" runat="server" style="display:none;" />
            <asp:LinkButton ID="lbtnLogOut" runat="server" OnClick="lbtnLogOut_Click" CssClass="mob-topbar-logout">Log Out</asp:LinkButton>
        </div>
    </div>

    <asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>

        <%-- Count header --%>
        <div class="mob-doc-header">
            <div class="mob-doc-header-main">
                <span class="mob-doc-num"><asp:Label ID="lblCountRef" runat="server" /></span>
                <span class="mob-doc-secondary" style="margin-left:.5em;">
                    <asp:Label ID="lblCountStatus" runat="server" />
                </span>
            </div>
            <div class="mob-doc-meta">
                <span class="mob-doc-meta-item">Date: <asp:Label ID="lblCountDate" runat="server" /></span>
                <span class="mob-doc-meta-item">Store: <strong><asp:Label ID="lblStoreName" runat="server" /></strong></span>
                <span class="mob-doc-badge"><asp:Label ID="lblLineCount" runat="server" /> remaining</span>
            </div>
        </div>

        <%-- Scan bar (shown when calculator is hidden) --%>
        <asp:Panel ID="pnlScanBar" runat="server" CssClass="mob-scan-bar">
            <div class="mob-scan-wrap">
                <asp:TextBox ID="txtBarcode" runat="server" placeholder="&#128247; Scan barcode to count&hellip;"
                    AutoPostBack="true" OnTextChanged="txtBarcode_TextChanged"
                    autocomplete="off" autocorrect="off" autocapitalize="off"
                    style="font-size:16px;" />
                <asp:LinkButton ID="lbtnClearScan" runat="server" OnClick="lbtnClearScan_Click"
                    CssClass="mob-scan-clear" title="Clear">&#10005;</asp:LinkButton>
            </div>
        </asp:Panel>

        <%-- Scan feedback --%>
        <asp:Label ID="lblScanFeedback" runat="server" />

        <%-- ================================================================ --%>
        <%-- FULL-SCREEN CALCULATOR OVERLAY --%>
        <%-- ================================================================ --%>
        <asp:Panel ID="pnlCalculator" runat="server" Visible="false" CssClass="calc-overlay">

            <div class="calc-header">
                <div style="flex:1;">
                    <div class="calc-item-code">
                        <asp:Label ID="lblCalcItemCode" runat="server" />
                        <span style="font-size:.7em;opacity:.8;margin-left:.4em;">
                            <asp:Label ID="lblCalcRound" runat="server" />
                        </span>
                    </div>
                    <div class="calc-item-desc">
                        <asp:Label ID="lblCalcItemDesc" runat="server" />
                    </div>
                </div>
                <asp:Label ID="lblCalcQtyHint" runat="server" CssClass="calc-qty-hint" Visible="false" />
            </div>

            <div class="calc-body">
                <div class="calc-expr-display">
                    <div class="calc-expr-text" id="calcExprText">&nbsp;</div>
                    <div class="calc-total-text" id="calcTotalText">= 0</div>
                </div>

                <div class="calc-pad">
                    <button type="button" class="calc-btn" onclick="calcDigit('7')">7</button>
                    <button type="button" class="calc-btn" onclick="calcDigit('8')">8</button>
                    <button type="button" class="calc-btn" onclick="calcDigit('9')">9</button>
                    <button type="button" class="calc-btn calc-btn--clear" onclick="calcClear()">C</button>

                    <button type="button" class="calc-btn" onclick="calcDigit('4')">4</button>
                    <button type="button" class="calc-btn" onclick="calcDigit('5')">5</button>
                    <button type="button" class="calc-btn" onclick="calcDigit('6')">6</button>
                    <button type="button" class="calc-btn calc-btn--del" onclick="calcDelete()">&#9003;</button>

                    <button type="button" class="calc-btn" onclick="calcDigit('1')">1</button>
                    <button type="button" class="calc-btn" onclick="calcDigit('2')">2</button>
                    <button type="button" class="calc-btn" onclick="calcDigit('3')">3</button>
                    <button type="button" class="calc-btn calc-btn--op" onclick="calcOp('+')">+</button>

                    <button type="button" class="calc-btn" onclick="calcDigit('0')" style="grid-column:span 2;">0</button>
                    <button type="button" class="calc-btn calc-btn--op" onclick="calcOp('-')">&minus;</button>

                    <asp:LinkButton ID="lbtnCalcCancel" runat="server" OnClick="lbtnCalcCancel_Click"
                        CssClass="calc-btn calc-btn--cancel" style="text-decoration:none;display:flex;align-items:center;justify-content:center;">
                        Cancel
                    </asp:LinkButton>
                    <asp:LinkButton ID="lbtnCalcSave" runat="server" OnClick="lbtnCalcSave_Click"
                        CssClass="calc-btn calc-btn--save" style="text-decoration:none;display:flex;align-items:center;justify-content:center;"
                        OnClientClick="return prepareCalcSave();">
                        &#10003; Save
                    </asp:LinkButton>
                </div>
            </div>

            <asp:HiddenField ID="hfCalcCtLineID" runat="server" />
            <asp:HiddenField ID="hfCalcExpression" runat="server" />
            <asp:HiddenField ID="hfCalcTotal" runat="server" />
            <asp:HiddenField ID="hfCalcInitQty" runat="server" />
            <asp:HiddenField ID="hfCalcRoundText" runat="server" />
        </asp:Panel>

        <%-- Close-off panel --%>
        <asp:Panel ID="pnlCloseOff" runat="server" Visible="false" CssClass="mob-action-panel">
            <h4>&#x1F4CB; Close Off Stock Count</h4>
            <p style="font-size:.82em;color:#555;margin:0 0 .7em 0;line-height:1.4;">
                All lines for this store have been counted. Confirm to close off the entire stock count.
            </p>
            <asp:Label ID="lblCloseOffError" runat="server" CssClass="mob-panel-error" Visible="false" />
            <div class="mob-panel-buttons">
                <asp:LinkButton ID="lbtnCancelCloseOff" runat="server"
                    OnClick="lbtnCancelCloseOff_Click"
                    CssClass="mob-btn-cancel">Cancel</asp:LinkButton>
                <asp:LinkButton ID="lbtnConfirmCloseOff" runat="server"
                    OnClick="lbtnConfirmCloseOff_Click"
                    CssClass="mob-btn-confirm"
                    OnClientClick="return confirmCloseOff();">&#10003; Confirm Close Off</asp:LinkButton>
            </div>
        </asp:Panel>

        <%-- Filter bar (category + search) --%>
        <div class="mob-filterbar" style="border-top:none;padding-top:0;">
            <asp:DropDownList ID="ddCategory" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddCategory_SelectedIndexChanged"
                style="flex:1;min-width:0;height:2.5em;border:1px solid #ccc;border-radius:.4em;padding:0 .3em;font-size:.85em;" />
            <asp:TextBox ID="txtSearch" runat="server" placeholder="Search&hellip;"
                style="flex:1;min-width:0;height:2.5em;border:1px solid #ccc;border-radius:.4em;padding:0 .4em;font-size:.85em;" />
            <asp:LinkButton ID="lbtnSearch" runat="server" OnClick="lbtnSearch_Click"
                CssClass="mob-find-btn">&#128269;</asp:LinkButton>
        </div>

        <%-- Line cards --%>
        <div class="mob-linelist">
            <asp:Repeater ID="rptLines" runat="server"
                OnItemDataBound="rptLines_ItemDataBound"
                OnItemCommand="rptLines_ItemCommand">
                <ItemTemplate>
                    <div class='mob-linecard<%# (long)Eval("CtLineID") == MatchedLineID ? " matched" : (Eval("LineFinished") as bool? == true ? " mob-linecard-finished" : "") %>'
                         id="card_<%# Eval("CtLineID") %>">
                        <div class="mob-linecard-body">

                            <div class="mob-card-main">
                                <div class="mob-item-code">
                                    <%# Eval("ItemCode") %>
                                    <asp:Label ID="lblCountedBadge" runat="server"
                                        CssClass="mob-saved-badge" Visible="false">&#10003; Counted</asp:Label>
                                    <asp:Label ID="lblCountAgainBadge" runat="server"
                                        CssClass="mob-saved-badge" Visible="false" style="background:#fff3e0;color:#e65100;">
                                        &#8635; Count Again
                                    </asp:Label>
                                    <asp:Label ID="lblFinalCountBadge" runat="server"
                                        CssClass="mob-saved-badge" Visible="false" style="background:#fce4ec;color:#880e4f;">
                                        &#9878; Final Count
                                    </asp:Label>
                                </div>
                                <div class="mob-item-descr"><%# Eval("ItemDescription") %></div>
                                <div class="mob-item-unit" style="font-size:.75em;color:#666;margin-top:.15em;">
                                    <%# Eval("CategoryDescript") %>
                                </div>
                                <asp:Label ID="lblBarcodes" runat="server" CssClass="mob-barcode-chips" />

                                <%-- Count 1 info (visible when first count done) --%>
                                <asp:Panel ID="pnlCount1Info" runat="server" Visible="false" style="margin-top:.3em;">
                                    <div class="mob-qty-row">
                                        <span class="mob-qty-badge" style="background:#e8f0fe;color:#1565c0;">
                                            Count 1:&nbsp;<asp:Label ID="lblCount1Qty" runat="server" />
                                        </span>
                                    </div>
                                </asp:Panel>

                                <%-- Count 2 info (visible when second count done) --%>
                                <asp:Panel ID="pnlCount2Info" runat="server" Visible="false" style="margin-top:.2em;">
                                    <div class="mob-qty-row">
                                        <span class="mob-qty-badge" style="background:#f3e5f5;color:#7b1fa2;">
                                            Count 2:&nbsp;<asp:Label ID="lblCount2Qty" runat="server" />
                                        </span>
                                    </div>
                                </asp:Panel>

                                <%-- Final count expression --%>
                                <asp:Label ID="lblCountExpr" runat="server" CssClass="mob-count-expr" />

                                <asp:HiddenField ID="hfCtLineID" runat="server" Value='<%# Eval("CtLineID") %>' />
                            </div>

                            <asp:LinkButton ID="lbtnRecount" runat="server"
                                CssClass="mob-action-btn"
                                CommandName="Recount"
                                CommandArgument='<%# Eval("CtLineID") %>'
                                OnClick="lbtnRecount_Click">&#9998;</asp:LinkButton>

                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <asp:Label ID="lblEmpty" runat="server" CssClass="mob-empty"
                Visible="false" Text="No items found for this store." />
        </div>

    </ContentTemplate>
    </asp:UpdatePanel>

    <%-- Bottom toolbar --%>
    <div class="mob-toolbar">
        <asp:LinkButton ID="lbtnCloseOff" runat="server" OnClick="lbtnCloseOff_Click"
            CssClass="mob-btn-primary">&#x2B06; Close Off</asp:LinkButton>
    </div>
</form>

<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
<script>
    // ── Calculator JS ──────────────────────────────────────────────────────
    var calcExpr = "";

    function calcInit(initQty) {
        calcExpr = initQty > 0 ? initQty.toString() : "";
        renderCalc();
    }

    function calcDigit(d) {
        // If the expression ends with a number, append. Otherwise start new number.
        if (calcExpr.length > 0 && !isNaN(calcExpr.charAt(calcExpr.length - 1))) {
            // Check if last "number" has a decimal already, prevent double
            calcExpr += d;
        } else {
            calcExpr += d;
        }
        renderCalc();
    }

    function calcOp(op) {
        if (calcExpr.length === 0) return;
        var last = calcExpr.charAt(calcExpr.length - 1);
        if (last === '+' || last === '-') {
            // Replace last operator
            calcExpr = calcExpr.slice(0, -1) + op;
        } else {
            calcExpr += op;
        }
        renderCalc();
    }

    function calcDelete() {
        calcExpr = calcExpr.slice(0, -1);
        renderCalc();
    }

    function calcClear() {
        calcExpr = "";
        renderCalc();
    }

    function evalExpr(expr) {
        if (!expr) return 0;
        // Strip anything that isn't digits, +, -, ., or space
        var sanitized = expr.replace(/[^0-9+\-.\s]/g, '');
        if (!sanitized) return 0;
        try {
            // Use Function constructor — safe because we sanitized to only arithmetic chars
            var result = new Function('return (' + sanitized + ')')();
            return isNaN(result) || !isFinite(result) ? 0 : result;
        } catch (e) {
            return 0;
        }
    }

    function renderCalc() {
        var exprEl = document.getElementById('calcExprText');
        var totalEl = document.getElementById('calcTotalText');
        if (exprEl) exprEl.innerHTML = calcExpr || '&nbsp;';
        if (totalEl) {
            var total = evalExpr(calcExpr);
            totalEl.textContent = '= ' + total;
        }
    }

    function prepareCalcSave() {
        var hfExpr  = document.getElementById('<%= hfCalcExpression.ClientID %>');
        var hfTotal = document.getElementById('<%= hfCalcTotal.ClientID %>');
        if (hfExpr)  hfExpr.value  = calcExpr;
        if (hfTotal) hfTotal.value = evalExpr(calcExpr);
        return true;
    }

    // ── Focus & scroll ─────────────────────────────────────────────────────
    function focusScanBox() {
        var b = document.getElementById('<%= txtBarcode.ClientID %>');
        if (b) b.focus();
    }

    var prm = Sys && Sys.WebForms && Sys.WebForms.PageRequestManager.getInstance();
    if (prm) {
        prm.add_endRequest(function () {
            var calc = document.getElementById('<%= pnlCalculator.ClientID %>');
            if (calc && calc.style.display !== 'none' && calc.offsetParent !== null) {
                // Calculator is visible — initialise with init qty
                var hfInit = document.getElementById('<%= hfCalcInitQty.ClientID %>');
                if (hfInit && hfInit.value) {
                    calcInit(parseInt(hfInit.value) || 0);
                }
            } else {
                focusScanBox();
            }
            var matched = document.querySelector('.mob-linecard.matched');
            if (matched) matched.scrollIntoView({ behavior: 'smooth', block: 'center' });
        });
    }

    window.onload = function () {
        var calc = document.getElementById('<%= pnlCalculator.ClientID %>');
        if (!calc || calc.style.display === 'none' || calc.offsetParent === null) {
            focusScanBox();
        }
        if (!window.matchMedia('(display-mode: standalone)').matches) {
            document.body.style.height = (window.screen.height + 50) + 'px';
            setTimeout(function () { window.scrollTo(0, 1); }, 50);
            setTimeout(function () { document.body.style.height = ''; }, 600);
        }
    };

    function confirmCloseOff() {
        return confirm('Are you sure you want to Close Off this Stock Count? This cannot be undone.');
    }
</script>
</body>
</html>
