<%@ Page Language="C#" Async="true" AsyncTimeout="600" AutoEventWireup="true" CodeBehind="Helper.aspx.cs" Inherits="SBMS.Helper" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Data Fusion - Help</title>
    <link id="Link3" runat="server" rel="shortcut icon" href="images/datafusionicon.ico" type="image/x-icon" />
    <link rel="stylesheet" href="assets/css/main.css" />
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    <style>
        .help-wrapper { max-width: 900px; margin: 2em auto; padding: 0 1em; }
        .help-header { display: flex; align-items: center; gap: 1em; margin-bottom: 1.5em; }
        .help-header h3 { flex: 1; margin: 0; }
        .help-search-wrap { display: flex; gap: .5em; margin-bottom: 1.5em; }
        .help-search-wrap input[type=text] {
            flex: 1; height: 2.8em; border: 1.5px solid #ccc; border-radius: .5em;
            padding: 0 .75em; font-size: .95em; font-family: inherit;
        }
        .help-search-wrap input[type=text]:focus { border-color: #4282C1; outline: none; }
        .help-ask-btn {
            height: 2.8em; padding: 0 1.5em; background: #4282C1; color: #fff;
            border: none; border-radius: .5em; font-size: .95em; font-weight: 600;
            cursor: pointer; white-space: nowrap;
        }
        .help-ask-btn:hover { background: #356a9e; }
        .help-answer-box {
            background: #fff; border-radius: .6em; box-shadow: 0 2px 12px rgba(0,0,0,.08);
            padding: 1.5em; margin-bottom: 1.5em; min-height: 6em; line-height: 1.6;
        }
        .help-answer-box h4 { color: #4282C1; margin: 0 0 .5em 0; font-size: 1.1em; }
        .help-answer-box p { margin: .5em 0; }
        .help-answer-box ul, .help-answer-box ol { margin: .5em 0 .5em 1.5em; }
        .help-answer-box li { margin: .25em 0; }
        .help-answer-box strong { font-weight: 600; }
        .help-answer-empty { color: #aaa; text-align: center; padding: 2em; font-size: .95em; }
        .help-answer-loading { color: #888; text-align: center; padding: 2em; }
        .help-source { font-size: .75em; color: #aaa; margin-top: .5em; border-top: 1px solid #eee; padding-top: .5em; }
        .help-feedback { display: flex; gap: .5em; margin-top: .75em; }
        .help-feedback button {
            padding: .35em .8em; border: 1px solid #ddd; border-radius: .4em;
            background: #fff; cursor: pointer; font-size: .8em; color: #666;
        }
        .help-feedback button:hover { background: #f0f0f0; }
        .help-suggestions { display: flex; flex-wrap: wrap; gap: .4em; margin-top: .5em; }
        .help-suggestion {
            background: #e8f0fe; color: #4282C1; border: none;
            border-radius: 1em; padding: .35em .85em; font-size: .8em;
            cursor: pointer; font-family: inherit;
        }
        .help-suggestion:hover { background: #d0e0f8; }

        /* Admin panel */
        .help-admin { margin-top: 2em; }
        .help-admin-toggle {
            background: none; border: 1px dashed #ccc; color: #999;
            padding: .5em 1em; border-radius: .4em; cursor: pointer; font-size: .8em;
        }
        .help-admin-toggle:hover { border-color: #4282C1; color: #4282C1; }
        .help-admin-table { width: 100%; border-collapse: collapse; margin-top: .5em; font-size: .82em; }
        .help-admin-table th { background: #f5f6f8; padding: .5em .6em; text-align: left; border-bottom: 2px solid #ddd; font-weight: 600; }
        .help-admin-table td { padding: .4em .6em; border-bottom: 1px solid #eee; vertical-align: top; }
        .help-admin-table tr:hover td { background: #fafbfc; }
        .help-admin-table .q-col { max-width: 280px; }
        .help-admin-table .a-col { max-width: 350px; }
        .help-admin-btn {
            padding: .25em .6em; border-radius: .3em; border: 1px solid #ccc;
            background: #fff; cursor: pointer; font-size: .85em; white-space: nowrap;
        }
        .help-admin-btn.pub { color: #2d6a2d; border-color: #2d6a2d; }
        .help-admin-btn.pub:hover { background: #2d6a2d; color: #fff; }
        .help-admin-btn.del { color: #c00; border-color: #c00; }
        .help-admin-btn.del:hover { background: #c00; color: #fff; }

        /* Print */
        @media print {
            body * { visibility: hidden; }
            .help-print-area, .help-print-area * { visibility: visible; }
            .help-print-area {
                position: absolute; left: 0; top: 0; width: 100%;
                padding: 1em; font-size: 11pt; line-height: 1.5;
            }
            .help-print-area h4 { font-size: 14pt; margin-bottom: .5em; }
            .help-no-print { display: none !important; }
        }
    </style>
</head>
<body>
<form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" />

    <div class="content">
    <div class="container">

        <%-- Top bar row --%>
        <div class="row 150%">
            <div class="2u 12u$(medium)">
                <a href="https://mydatafusion.online" title="My Data Fusion website">
                    <img src="images/logo.png" style="float:left" class="logoImg" alt="Data Fusion" />
                </a>
            </div>
            <div class="8u 12u$(medium)" style="text-align:center;">
                <asp:LinkButton ID="lbtnBack" runat="server" CssClass="buttonC icon fa-arrow-left"
                    OnClick="lbtnHome_Click" style="float:left">&nbsp;Back to Dashboard</asp:LinkButton>
                <h3 style="display:inline-block; margin-left:1em;">&#128161; Help Assistant</h3>
            </div>
            <div class="2u 12u$(medium)">
                <asp:LinkButton ID="lbtnLogOut" runat="server" CssClass="buttonC icon fa-eject"
                    OnClick="lbtnLogOut_Click" style="float:right;" ToolTip="Log Out">&nbsp;Log Out&nbsp;</asp:LinkButton>
            </div>
        </div>

        <div class="row 150%">
            <div class="2u 12u$(medium)">&nbsp;</div>
            <div class="8u 12u$(medium)">
                <div class="help-wrapper">

                    <%-- Search / ask bar --%>
                    <div class="help-search-wrap">
                        <asp:TextBox ID="txtQuestion" runat="server"
                            placeholder="Ask a question about Data Fusion... e.g. 'How do I receive goods?'"
                            style="flex:1;height:2.8em;border:1.5px solid #ccc;border-radius:.5em;padding:0 .75em;font-size:.95em;" />
                        <asp:LinkButton ID="lbtnAsk" runat="server" OnClick="lbtnAsk_Click"
                            CssClass="help-ask-btn" style="text-decoration:none;display:flex;align-items:center;">
                            &#128640; Ask
                        </asp:LinkButton>
                    </div>

                    <%-- Answer display --%>
                    <asp:Panel ID="pnlAnswer" runat="server" CssClass="help-answer-box" Visible="false">
                        <div class="help-print-area">
                            <h4><asp:Label ID="lblAnswerQuestion" runat="server" /></h4>
                            <asp:Literal ID="litAnswer" runat="server" />
                        </div>
                        <div class="help-source">
                            <asp:Label ID="lblSource" runat="server" />
                        </div>
                        <div class="help-feedback help-no-print" style="justify-content:space-between;">
                            <span>
                                <button type="button" onclick="printAnswer()" style="font-weight:600;">&#128424; Print Answer</button>
                            </span>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlEmpty" runat="server" CssClass="help-answer-box" Visible="true">
                        <div class="help-answer-empty">
                            &#128161; Ask me anything about using Data Fusion.<br />
                            <span style="font-size:.85em;">e.g. How do I receive goods? How does stock counting work? What permissions do I need?</span>
                            <div class="help-suggestions" style="justify-content:center;margin-top:1em;">
                                <button type="button" class="help-suggestion" onclick="askSuggestion('How do I receive goods?')">Receiving goods</button>
                                <button type="button" class="help-suggestion" onclick="askSuggestion('How does stock counting work?')">Stock counting</button>
                                <button type="button" class="help-suggestion" onclick="askSuggestion('How do I pick items on a picking slip?')">Picking slips</button>
                                <button type="button" class="help-suggestion" onclick="askSuggestion('How do I transfer stock between stores?')">Transfers</button>
                            </div>
                        </div>
                    </asp:Panel>

                    <%-- Admin panel (super users only) --%>
                    <asp:Panel ID="pnlAdmin" runat="server" Visible="false" CssClass="help-admin">
                        <asp:LinkButton ID="lbtnToggleAdmin" runat="server"
                            CssClass="help-admin-toggle" OnClick="lbtnToggleAdmin_Click">
                            &#9881; Manage Help Articles
                        </asp:LinkButton>

                        <asp:Panel ID="pnlAdminContent" runat="server" Visible="false" style="margin-top:1em;">
                            <h5>Unpublished Answers (Review Queue)</h5>
                            <asp:Repeater ID="rptUnpublished" runat="server" OnItemCommand="rptUnpublished_ItemCommand">
                                <HeaderTemplate>
                                    <table class="help-admin-table">
                                        <tr><th>Question</th><th>Answer</th><th>Module</th><th style="width:5em;"></th></tr>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <tr>
                                        <td class="q-col"><%# Eval("Question") %></td>
                                        <td class="a-col"><%# Eval("Answer").ToString().Length > 200 ? Eval("Answer").ToString().Substring(0, 200) + "..." : Eval("Answer") %></td>
                                        <td><%# Eval("Module") %></td>
                                        <td style="white-space:nowrap;">
                                            <asp:LinkButton ID="lbtnPublish" runat="server"
                                                CssClass="help-admin-btn pub"
                                                CommandName="Publish"
                                                CommandArgument='<%# Eval("Id") %>'>Publish</asp:LinkButton>
                                            <asp:LinkButton ID="lbtnDelete" runat="server"
                                                CssClass="help-admin-btn del"
                                                CommandName="Delete"
                                                CommandArgument='<%# Eval("Id") %>'
                                                OnClientClick="return confirm('Delete this article?');">Del</asp:LinkButton>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                                <FooterTemplate>
                                    </table>
                                    <asp:Label ID="lblNoUnpub" runat="server" Visible="false"
                                        style="color:#aaa;font-size:.85em;">No articles awaiting review.</asp:Label>
                                </FooterTemplate>
                            </asp:Repeater>

                            <h5 style="margin-top:1.5em;">Published Library</h5>
                            <asp:Repeater ID="rptPublished" runat="server" OnItemCommand="rptPublished_ItemCommand">
                                <HeaderTemplate>
                                    <table class="help-admin-table">
                                        <tr><th>Question</th><th>Module</th><th>Uses</th><th style="width:5em;"></th></tr>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <tr>
                                        <td class="q-col"><%# Eval("Question") %></td>
                                        <td><%# Eval("Module") %></td>
                                        <td><%# Eval("UsageCount") %></td>
                                        <td>
                                            <asp:LinkButton ID="lbtnUnpublish" runat="server"
                                                CssClass="help-admin-btn" style="color:#e65100;border-color:#e65100;"
                                                CommandName="Unpublish"
                                                CommandArgument='<%# Eval("Id") %>'>Unpub</asp:LinkButton>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                                <FooterTemplate>
                                    </table>
                                </FooterTemplate>
                            </asp:Repeater>
                        </asp:Panel>
                    </asp:Panel>

                </div>
            </div>
            <div class="2u 12u$(medium)">&nbsp;</div>
        </div>

    </div>
    </div>
</form>

<script>
    function askSuggestion(q) {
        document.getElementById('<%= txtQuestion.ClientID %>').value = q;
        __doPostBack('<%= lbtnAsk.UniqueID %>', '');
    }

    // Set focus on question box
    window.onload = function () {
        var q = document.getElementById('<%= txtQuestion.ClientID %>');
        if (q) q.focus();
    };

    // On Enter key, click Ask
    document.getElementById('<%= txtQuestion.ClientID %>').addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            __doPostBack('<%= lbtnAsk.UniqueID %>', '');
        }
    });

    function printAnswer() {
        window.print();
    }
</script>
</body>
</html>
