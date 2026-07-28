// Searchable item picker.
//
// Turns any <select class="item-search"> into a type-to-filter list WITHOUT changing
// the select itself - it keeps its name, its options and its AutoPostBack, so every
// existing SelectedValue / SelectedIndexChanged handler carries on working untouched.
// We simply intercept the click, show a filter panel, and write the choice back to the
// select before firing its normal change event.
//
// Matching is substring, case-insensitive, and multi-keyword: the query is split on
// spaces and EVERY word must appear somewhere in the option text. So with options
// rendered as "CODE - Description", a user can find 5L Dishwash by typing the code,
// any word of the description, or "dish 5l" in either order.

(function () {
    'use strict';

    var PANEL_ID = 'itemSearchPanel';
    var current = null;          // the select currently being edited

    function panel() {
        var p = document.getElementById(PANEL_ID);
        if (p) return p;

        p = document.createElement('div');
        p.id = PANEL_ID;
        p.style.cssText =
            'position:fixed;z-index:100000;display:none;background:#fff;border:1px solid #4A82AB;' +
            'border-radius:.4em;box-shadow:0 6px 24px rgba(0,0,0,.25);font-size:.85em;' +
            'min-width:22em;max-width:34em;overflow:hidden';

        var box = document.createElement('input');
        box.type = 'text';
        box.setAttribute('autocomplete', 'off');
        box.placeholder = 'Type a code, description or keyword...';
        box.style.cssText =
            'width:100%;box-sizing:border-box;border:0;border-bottom:1px solid #ddd;' +
            'padding:.55em .7em;outline:none;font-size:1em';
        p.appendChild(box);

        var list = document.createElement('div');
        list.style.cssText = 'max-height:16em;overflow-y:auto';
        p.appendChild(list);

        p._box = box;
        p._list = list;

        box.addEventListener('input', function () { render(box.value); });
        box.addEventListener('keydown', onKey);

        document.body.appendChild(p);
        return p;
    }

    function options() {
        // Skip the placeholder rows ("Select", "-?-", "-Item Code-") - they carry value 0
        // or empty, and the user picks those by pressing Escape, not by searching for them.
        return Array.prototype.slice.call(current.options).filter(function (o) {
            return o.value !== '' && o.value !== '0';
        });
    }

    // The "nothing selected" option ("Select", "-Select Item-", ...). It is kept OUT of the
    // search results, so without an explicit way back to it a selection could never be undone.
    function placeholder() {
        return Array.prototype.slice.call(current.options).filter(function (o) {
            return o.value === '' || o.value === '0';
        })[0];
    }

    function addRow(text, value, muted) {
        var row = document.createElement('div');
        row.textContent = text;
        row.dataset.value = value;
        row.style.cssText = 'padding:.45em .7em;cursor:pointer;white-space:nowrap;' +
                            'overflow:hidden;text-overflow:ellipsis' +
                            (muted ? ';color:#777;font-style:italic;border-bottom:1px solid #eee' : '');
        row.dataset.baseColor = muted ? '#777' : '';   // restored when the highlight comes off
        row.addEventListener('mouseenter', function () { clearMarks(); mark(row, true); });
        row.addEventListener('mousedown', function (ev) { ev.preventDefault(); choose(value); });
        panel()._list.appendChild(row);
        return row;
    }

    function render(query) {
        var p = panel(), list = p._list;
        var words = (query || '').toLowerCase().split(/\s+/).filter(function (w) { return w.length; });

        var matches = options().filter(function (o) {
            var t = o.text.toLowerCase();
            return words.every(function (w) { return t.indexOf(w) !== -1; });
        });

        list.innerHTML = '';

        // Offer the way back to "nothing selected", but only when something IS selected.
        var ph = placeholder();
        var cleared = null;
        if (ph && current.value !== ph.value) cleared = addRow('Clear selection', ph.value, true);

        if (!matches.length) {
            var none = document.createElement('div');
            none.textContent = 'No matching items';
            none.style.cssText = 'padding:.6em .7em;color:#999';
            list.appendChild(none);
        } else {
            matches.slice(0, 300).forEach(function (o) { addRow(o.text, o.value, false); });
        }

        // Highlight the first real match; fall back to Clear when nothing matched.
        var first = matches.length
            ? list.children[cleared ? 1 : 0]
            : cleared;
        if (first) mark(first, true);
    }

    function mark(row, on) {
        row.style.background = on ? '#4A82AB' : '';
        row.style.color = on ? '#fff' : (row.dataset.baseColor || '');
        if (on) {
            row.dataset.sel = '1';
            if (row.scrollIntoViewIfNeeded) row.scrollIntoViewIfNeeded();
            else row.scrollIntoView({ block: 'nearest' });
        } else {
            delete row.dataset.sel;
        }
    }

    function clearMarks() {
        Array.prototype.forEach.call(panel()._list.children, function (r) { mark(r, false); });
    }

    function selected() {
        return panel()._list.querySelector('[data-sel="1"]');
    }

    function onKey(e) {
        var sel = selected();
        if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
            e.preventDefault();
            if (!sel) return;
            var next = e.key === 'ArrowDown' ? sel.nextElementSibling : sel.previousElementSibling;
            if (next && next.dataset.value !== undefined) { mark(sel, false); mark(next, true); }
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (sel) choose(sel.dataset.value);
        } else if (e.key === 'Escape') {
            e.preventDefault();
            close();
        }
    }

    function choose(value) {
        var sel = current;
        close();
        if (!sel) return;
        if (sel.value === value) return;               // no change -> no postback, same as a native select
        sel.value = value;
        // Fire the select's own change event so ASP.NET AutoPostBack behaves exactly as
        // it would have on a native pick.
        sel.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function close() {
        var p = panel();
        p.style.display = 'none';
        current = null;
    }

    function open(sel) {
        current = sel;
        var p = panel(), r = sel.getBoundingClientRect();

        p.style.display = 'block';
        p.style.left = Math.min(r.left, window.innerWidth - p.offsetWidth - 12) + 'px';

        // Flip above the field when there isn't room below it.
        var below = window.innerHeight - r.bottom;
        if (below < p.offsetHeight + 12 && r.top > below) {
            p.style.top = Math.max(4, r.top - p.offsetHeight - 4) + 'px';
        } else {
            p.style.top = (r.bottom + 4) + 'px';
        }

        p._box.value = '';
        render('');
        p._box.focus();
    }

    function enhance(sel) {
        if (sel.dataset.searchable === '1') return;
        sel.dataset.searchable = '1';
        sel.title = 'Click to search by code, description or keyword';

        // Intercept before the native dropdown opens.
        sel.addEventListener('mousedown', function (e) { e.preventDefault(); open(sel); });
        sel.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ' || e.key === 'ArrowDown') { e.preventDefault(); open(sel); }
        });
    }

    function scan() {
        Array.prototype.forEach.call(document.querySelectorAll('select.item-search'), enhance);
    }

    document.addEventListener('DOMContentLoaded', scan);
    document.addEventListener('mousedown', function (e) {
        var p = document.getElementById(PANEL_ID);
        if (current && p && !p.contains(e.target) && e.target !== current) close();
    });
    window.addEventListener('resize', close);
    // Grids and accordions re-render on partial postbacks - re-scan for new rows.
    // ScriptManager renders inside the body, so Sys may not exist yet if this file was
    // pulled in from <head>; try now and again on load, and only hook once.
    var hooked = false;
    function hookPartialPostbacks() {
        if (hooked) return;
        if (window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager) {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(scan);
            hooked = true;
        }
    }
    hookPartialPostbacks();
    window.addEventListener('load', function () { hookPartialPostbacks(); scan(); });
    scan();
})();
