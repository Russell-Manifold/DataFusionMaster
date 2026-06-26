<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="NotificationsM.aspx.cs"
         Inherits="SBMS.NotificationsM" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Notifications</title>
    <link rel="shortcut icon" href="../images/datafusionicon.ico" type="image/x-icon" />
    <link rel="stylesheet" href="../SBMSMobile/css/main.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/main.css") %>" />
    <link rel="stylesheet" href="../SBMSMobile/css/mobile-ui.css<%= SBMS.Classes.Ver.Css("~/SBMSMobile/css/mobile-ui.css") %>" />
    <!-- PWA -->
    <link rel="manifest" href="../SBMSMobile/manifest.json" />
    <meta name="theme-color" content="#4282C1" />
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
    <meta name="apple-mobile-web-app-title" content="Data Fusion" />
    <link rel="apple-touch-icon" href="../SBMSMobile/icons/icon-192.png" />
    <meta name="format-detection" content="telephone=no" />
    <style>
        /* base + tokens inherited from mobile-ui.css */

        /* Notification cards */
        .notif-list  { padding: .5em .6em 5.4em .6em; margin-top: 3.2em; }

        .notif-card  {
            background: var(--mob-surface);
            border-radius: var(--mob-radius-lg);
            box-shadow: var(--mob-shadow-card);
            border: 0.5px solid var(--mob-border);
            margin-bottom: .7em;
            display: flex;
            align-items: flex-start;
            gap: .75em;
            padding: .9em 1em;
            transition: opacity .3s;
        }
        .notif-card.read { opacity: .45; }

        .notif-icon {
            flex: 0 0 2.2em;
            width: 2.2em; height: 2.2em;
            border-radius: 50%;
            background: var(--mob-brand);
            color: #fff;
            display: flex; align-items: center; justify-content: center;
            font-size: 1.1em;
        }
        .notif-icon.read { background: #aaa; }

        .notif-body  { flex: 1 1 auto; }
        .notif-msg   { font-size: .95em; color: var(--mob-ink-strong); line-height: 1.4; }
        .notif-time  { font-size: .75em; color: var(--mob-text-faint); margin-top: .25em; }

        .notif-mark  {
            flex: 0 0 auto;
            background: none;
            border: 1px solid var(--mob-brand);
            color: var(--mob-brand);
            border-radius: var(--mob-radius-sm);
            padding: .45em .75em;
            font-size: .8em;
            cursor: pointer;
            white-space: nowrap;
            transition: background .15s, color .15s, box-shadow .15s;
        }
        .notif-mark:active { background: var(--mob-brand); color: #fff; }
        .notif-mark:focus-visible { outline: none; box-shadow: var(--mob-focus); }

        .notif-empty {
            text-align: center;
            padding: 4em 1em;
            color: #bbb;
            font-size: .95em;
        }

        /* Refresh / Mark-all bar */
        .notif-toolbar {
            position: fixed;
            bottom: 0; left: 0; right: 0;
            background: var(--mob-surface);
            border-top: 1px solid var(--mob-divider);
            box-shadow: var(--mob-shadow-panel);
            display: flex;
            gap: .5em;
            padding: .6em .8em;
            z-index: 100;
        }
        .notif-toolbar button {
            flex: 1;
            height: 3.1em;
            border: none;
            border-radius: var(--mob-radius-md);
            font-size: .95em;
            font-weight: 700;
            letter-spacing: .01em;
            cursor: pointer;
            transition: background .15s, box-shadow .15s;
        }
        .notif-toolbar button:focus-visible { outline: none; box-shadow: var(--mob-focus); }
        .btn-refresh  { background: var(--mob-brand); color: #fff; }
        .btn-markall  { background: #e2e3e5; color: #333; }
        .btn-refresh:active { background: var(--mob-brand-dark); }
        .btn-markall:active { background: #ccc; }

        /* Spinner */
        .notif-spinner {
            display: none;
            text-align: center;
            padding: 2em;
            color: #888;
            font-size: .9em;
        }
        .notif-spinner.active { display: block; }
    </style>
</head>
<body>
<form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" />

    <%-- Top bar --%>
    <div class="mob-topbar">
        <span class="mob-topbar-title">&#128276; Notifications
            <span id="spnBadge" style="display:none;background:#e44c65;color:#fff;border-radius:1em;
                  font-size:.7em;padding:.1em .55em;margin-left:.4em;vertical-align:middle;"></span>
        </span>
        <div style="display:flex;align-items:center;gap:.8em;">
            <asp:Label ID="lblUsername" runat="server" style="color:#ccc;font-size:.8em;" />
            <asp:LinkButton ID="lbtnHome" runat="server" OnClick="lbtnHome_Click"
                CssClass="mob-topbar-logout">&#127968;</asp:LinkButton>
        </div>
    </div>

    <%-- Notification list (populated by JS) --%>
    <div class="notif-list">
        <div id="notifSpinner" class="notif-spinner">Loading&hellip;</div>
        <div id="notifContainer"></div>
        <div id="notifEmpty" class="notif-empty" style="display:none;">
            &#10003;&nbsp;You&rsquo;re all caught up!
        </div>
    </div>

    <%-- Bottom toolbar --%>
    <div class="notif-toolbar">
        <button type="button" class="btn-refresh" onclick="loadNotifications()">&#8635; Refresh</button>
        <button type="button" class="btn-markall" onclick="markAllRead()">&#10003; Mark all read</button>
    </div>
</form>

<script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
<script>
    // Point at the existing desktop WebMethod endpoints
    var WM_BASE = '../Notifications.aspx/';

    function loadNotifications() {
        var spinner   = document.getElementById('notifSpinner');
        var container = document.getElementById('notifContainer');
        var empty     = document.getElementById('notifEmpty');
        var badge     = document.getElementById('spnBadge');

        spinner.classList.add('active');
        container.innerHTML = '';
        empty.style.display = 'none';

        $.ajax({
            type: 'POST',
            url: WM_BASE + 'GetNewNotifications',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (resp) {
                spinner.classList.remove('active');
                var list = resp.d || [];
                if (list.length === 0) {
                    empty.style.display = 'block';
                    badge.style.display = 'none';
                    return;
                }
                badge.textContent = list.length;
                badge.style.display = 'inline';
                list.forEach(function (n) { container.appendChild(buildCard(n)); });
            },
            error: function () {
                spinner.classList.remove('active');
                empty.textContent = 'Could not load notifications.';
                empty.style.display = 'block';
            }
        });
    }

    function buildCard(n) {
        var card = document.createElement('div');
        card.className = 'notif-card';
        card.id = 'notif-' + n.Id;

        card.innerHTML =
            '<div class="notif-icon">&#128276;</div>' +
            '<div class="notif-body">' +
                '<div class="notif-msg">' + escHtml(n.Message) + '</div>' +
            '</div>' +
            '<button type="button" class="notif-mark" onclick="markRead(' + n.Id + ')">Done</button>';
        return card;
    }

    function markRead(id) {
        $.ajax({
            type: 'POST',
            url: WM_BASE + 'MarkMessageAsRead',
            data: JSON.stringify({ messageId: id }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function () {
                var card = document.getElementById('notif-' + id);
                if (card) {
                    card.classList.add('read');
                    card.querySelector('.notif-mark').disabled = true;
                    card.querySelector('.notif-icon').classList.add('read');
                }
                updateBadge();
            }
        });
    }

    function markAllRead() {
        var cards = document.querySelectorAll('.notif-card:not(.read)');
        cards.forEach(function (card) {
            var id = parseInt(card.id.replace('notif-', ''));
            markRead(id);
        });
    }

    function updateBadge() {
        var unread = document.querySelectorAll('.notif-card:not(.read)').length;
        var badge  = document.getElementById('spnBadge');
        if (unread === 0) {
            badge.style.display = 'none';
            document.getElementById('notifEmpty').style.display = 'block';
        } else {
            badge.textContent = unread;
        }
    }

    function escHtml(str) {
        var d = document.createElement('div');
        d.appendChild(document.createTextNode(str));
        return d.innerHTML;
    }

    // Auto-refresh every 60 s (matches desktop behaviour)
    setInterval(loadNotifications, 60000);
    $(document).ready(function () { loadNotifications(); });
</script>
</body>
</html>
