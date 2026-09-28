<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="LabelPrint.aspx.cs" Inherits="SBMS.LabelPrint" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Print Labels</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link id="Link4" runat="server" rel="icon" href="images/datafusionicon.ico" type="image/ico" />
    <link rel="stylesheet" href="assets/css/main.css" />
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="content">
            <div class="container">
                <div class="row 150%">
                    <div class="2u 12u$(medium)"><a href="https://mydatafusion.online" title="My Data Fusion website"><img src="images/logo.png" style="float:left" class="logoImg" /></a></div>
                    <div class="8u 12u$(medium)">
                        <asp:LinkButton ID="lbtnHome" runat="server" class="buttonC icon fa-home" OnClick="lbtnHome_Click">&nbsp;&nbsp;</asp:LinkButton>
                        <asp:LinkButton ID="lbtnBack" runat="server" class="buttonC icon fa-arrow-left" OnClick="lbtnBack_Click">&nbsp;Back to Item</asp:LinkButton>
                        <h3 style="padding-top:0; line-height:1em">Print Labels: <asp:Label ID="lblItemCode" runat="server" Text=""></asp:Label></h3>
                    </div>
                    <div class="2u 12u$(medium)"><asp:Image ID="imgCoImg" runat="server" style="float:right" class="logoImg" /></div>
                </div>
                <div class="row 150%">
                    <div class="3u 12u$(medium)">&nbsp;</div>
                    <div class="6u 12u$(medium)">
                        <h4>What to print</h4>
                        <label>Barcode</label>
                        <asp:DropDownList ID="ddValue" runat="server"></asp:DropDownList>
                        <label>Or type a value (overrides the barcode above)</label>
                        <asp:TextBox ID="txtCustom" runat="server" MaxLength="80"></asp:TextBox>
                        <label>Number of labels</label>
                        <asp:TextBox ID="txtQty" runat="server" TextMode="Number" Text="1" min="1" max="1000"></asp:TextBox>
                        <asp:CheckBox ID="chkShowText" runat="server" Text="Print the numbers under the barcode" Checked="true" />

                        <h4 style="margin-top:1.5em">Label layout <small>(remembered on this PC)</small></h4>
                        <label>Label width (mm)</label>
                        <asp:TextBox ID="txtWidth" runat="server" TextMode="Number" Text="100" min="10" max="300" step="0.1"></asp:TextBox>
                        <label>Label height (mm)</label>
                        <asp:TextBox ID="txtHeight" runat="server" TextMode="Number" Text="50" min="5" max="300" step="0.1"></asp:TextBox>
                        <label>Labels across the roll</label>
                        <asp:DropDownList ID="ddAcross" runat="server">
                            <asp:ListItem Value="1" Text="1 across" Selected="True" />
                            <asp:ListItem Value="2" Text="2 across" />
                            <asp:ListItem Value="3" Text="3 across" />
                        </asp:DropDownList>
                        <label>Gap between labels across (mm)</label>
                        <asp:TextBox ID="txtGap" runat="server" TextMode="Number" Text="3" min="0" max="50" step="0.1"></asp:TextBox>

                        <br />
                        <asp:Label ID="lblMsg" runat="server" ForeColor="#C0392B"></asp:Label><br />
                        <asp:LinkButton ID="lbtnPrint" runat="server" class="buttonC icon fa-print" OnClick="lbtnPrint_Click"
                            OnClientClick="saveLayout(); document.forms[0].target='_blank'; setTimeout(function(){ document.forms[0].target=''; }, 500);">&nbsp;Create Labels</asp:LinkButton>
                    </div>
                    <div class="3u 12u$(medium)">&nbsp;</div>
                </div>
            </div>
        </div>
    </form>
    <script>
        // Layout is per PC (it follows the roll loaded in that PC's printer), so it lives in the browser.
        var layoutKey = 'sbmsLabelLayout';
        var layoutIds = ['<%= txtWidth.ClientID %>', '<%= txtHeight.ClientID %>', '<%= ddAcross.ClientID %>', '<%= txtGap.ClientID %>', '<%= chkShowText.ClientID %>'];
        function saveLayout() {
            try {
                var s = {};
                layoutIds.forEach(function (id) { var el = document.getElementById(id); s[id] = el.type === 'checkbox' ? el.checked : el.value; });
                localStorage.setItem(layoutKey, JSON.stringify(s));
            } catch (e) { }
        }
        (function loadLayout() {
            try {
                var s = JSON.parse(localStorage.getItem(layoutKey) || '{}');
                layoutIds.forEach(function (id) {
                    if (!(id in s)) return;
                    var el = document.getElementById(id);
                    if (el.type === 'checkbox') el.checked = s[id]; else el.value = s[id];
                });
            } catch (e) { }
        })();
    </script>
</body>
</html>
