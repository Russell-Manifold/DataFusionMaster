/* Serial number capture for the Receiving line modal.
 *
 * A handheld scanner is just a keyboard: it types the barcode and presses Enter. So the
 * serials are collected CLIENT-SIDE into a hidden field and posted once when the line is
 * saved. Doing it with AutoPostBack instead would cost a round-trip per unit - the operator
 * would wait a second between scans and the scanner would outrun the page.
 *
 * The Use By date is captured WITH each unit, not once for the line. Change the date
 * part-way through and the units scanned after it take the new one, so a single receipt can
 * hold stock that expires on different days. Nothing can be captured without a date.
 *
 * Stored in the hidden field as SERIAL~DATE|SERIAL~DATE|...
 */
(function () {
    "use strict";

    var PAIR = "|", SEP = "~";

    function ids() {
        return {
            box:   document.getElementById("txtSerialScan"),
            date:  document.getElementById("txtUseBy"),
            store: document.getElementById("hfSerials"),
            list:  document.getElementById("serialList"),
            count: document.getElementById("serialCount"),
            qty:   document.getElementById("txtQtyReceive")
        };
    }

    /** Captured units as {serial, date} - date exactly as the operator typed it. */
    function values(el) {
        if (!el || !el.value) return [];
        return el.value.split(PAIR).filter(function (v) { return v.length > 0; })
            .map(function (v) {
                var at = v.indexOf(SEP);
                return at < 0 ? { serial: v, date: "" }
                              : { serial: v.substring(0, at), date: v.substring(at + 1) };
            });
    }

    function store(el, list) {
        el.value = list.map(function (u) { return u.serial + SEP + u.date; }).join(PAIR);
    }

    function expected(el) {
        var n = el ? parseFloat(el.value) : 0;
        return isNaN(n) ? 0 : n;
    }

    function render() {
        var c = ids();
        if (!c.store || !c.list) return;

        var list = values(c.store);
        c.list.innerHTML = "";
        list.forEach(function (unit, i) {
            var li = document.createElement("li");
            li.className = "serial-row";
            li.innerHTML = '<span class="serial-idx">' + (i + 1) + '</span>' +
                           '<span class="serial-val"></span>' +
                           '<span class="serial-exp"></span>' +
                           '<button type="button" class="serial-del" data-i="' + i + '" title="Remove">&times;</button>';
            // textContent, never innerHTML: a scanned string is not to be trusted as markup
            li.querySelector(".serial-val").textContent = unit.serial;
            li.querySelector(".serial-exp").textContent = unit.date;
            c.list.appendChild(li);
        });

        if (c.count) {
            var want = expected(c.qty);
            c.count.textContent = want > 0
                ? list.length + " of " + want + " captured"
                : list.length + " captured";
            c.count.className = (want > 0 && list.length === want) ? "serial-count ok" : "serial-count";
        }
    }

    function flash(msg) {
        var c = ids();
        if (!c.count) return;
        c.count.textContent = msg;
        c.count.className = "serial-count warn";
        setTimeout(render, 2200);
    }

    function add(raw) {
        var c = ids();
        var serial = (raw || "").trim().toUpperCase();
        if (!serial) return;

        // No date, no capture. A unit received without an expiry can never be caught by
        // FEFO or the expiry report, so it must not be possible to create one.
        var date = c.date ? (c.date.value || "").trim() : "";
        if (!date) {
            flash("Enter the Use By Date before scanning");
            if (c.date) c.date.focus();
            return;
        }

        var list = values(c.store);
        if (list.some(function (u) { return u.serial === serial; })) {
            flash(serial + " already captured");
            return;
        }
        var want = expected(c.qty);
        if (want > 0 && list.length >= want) {
            flash("Quantity is " + want + " - remove one before adding another");
            return;
        }

        list.push({ serial: serial, date: date });
        store(c.store, list);
        render();
    }

    function wire() {
        var c = ids();
        if (!c.box || c.box.dataset.wired === "1") return;
        c.box.dataset.wired = "1";

        c.box.addEventListener("keydown", function (e) {
            if (e.key !== "Enter") return;
            e.preventDefault();          // the scanner's Enter must not submit the page
            add(c.box.value);
            c.box.value = "";
            c.box.focus();               // without this the next scan lands nowhere
        });

        // Typed entry: leaving the box also commits what is in it.
        c.box.addEventListener("blur", function () {
            if (c.box.value.trim().length > 0) { add(c.box.value); c.box.value = ""; }
        });

        if (c.list && c.list.dataset.wired !== "1") {
            c.list.dataset.wired = "1";
            c.list.addEventListener("click", function (e) {
                var btn = e.target.closest(".serial-del");
                if (!btn) return;
                var list = values(c.store);
                list.splice(parseInt(btn.dataset.i, 10), 1);
                store(c.store, list);
                render();
                c.box.focus();
            });
        }

        var clear = document.getElementById("btnSerialClear");
        if (clear && clear.dataset.wired !== "1") {
            clear.dataset.wired = "1";
            clear.addEventListener("click", function () {
                c.store.value = "";
                render();
                c.box.focus();
            });
        }

        if (c.qty && c.qty.dataset.serialWired !== "1") {
            c.qty.dataset.serialWired = "1";
            c.qty.addEventListener("change", render);
        }

        // Straight to the date: nothing can be captured until it is filled in.
        render();
        if (c.date && !c.date.value) c.date.focus(); else c.box.focus();
    }

    document.addEventListener("DOMContentLoaded", wire);

    // The modal lives inside an UpdatePanel, so it is re-rendered on every partial postback.
    if (typeof Sys !== "undefined" && Sys.WebForms && Sys.WebForms.PageRequestManager) {
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(wire);
    }
})();
