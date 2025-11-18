function confirmUnlink(message) {
    return confirm(message);
}

function updateCheckboxes() {
    var chkF = document.getElementById(chkFClientID);
    var chkB = document.getElementById(chkBClientID);
    var chkK = document.getElementById(chkKClientID);
    var chkIsFromBom = document.getElementById(chkIsFromBomClientID);
    var chkIsFromKit = document.getElementById(chkIsFromKitClientID);
    var lbtnBOM = document.getElementById(lbtnBOMClientID);
    var lbtnKit = document.getElementById(lbtnKitClientID);

    try {
        // Check if chkIsFromKit was just unchecked
        if (chkIsFromKit) {
            if (!chkIsFromKit.checked && chkIsFromKit.wasChecked) {
                if (!confirmUnlink("You are about to unlink this item from a kit, are you sure?")) {
                    chkIsFromKit.checked = true; // Re-check if user cancels the action
                    return;
                } else {
                    // Trigger server-side method if confirmed
                    __doPostBack(chkIsFromKit.id, 'UnlinkKit');
                }
            }
            chkIsFromKit.wasChecked = chkIsFromKit.checked; // Store the current state
        }
    } catch { }

    // Check if chkIsFromBom was just unchecked
    try {
        if (chkIsFromBom) {
            if (!chkIsFromBom.checked && chkIsFromBom.wasChecked) {
                if (!confirmUnlink("You are about to unlink this item from a BOM, are you sure?")) {
                    chkIsFromBom.checked = true; // Re-check if user cancels the action
                    return;
                } else {
                    // Trigger server-side method if confirmed
                    __doPostBack(chkIsFromBom.id, 'UnlinkBom');
                }
            }
            chkIsFromBom.wasChecked = chkIsFromBom.checked; // Store the current state
        }
    } catch { }

    try {
        // Main checkbox logic for chkB and chkIsFromBom
        if (chkB && chkIsFromBom) {
            // Prevent chkB and chkIsFromBom from being checked together
            if (chkB.checked) {
                chkIsFromBom.checked = false;
                chkIsFromBom.disabled = true;
            } else if (chkIsFromBom.checked) {
                chkB.checked = false;
                chkB.disabled = true;
            } else {
                // If neither is checked, enable both as long as chkF is checked
                if (chkF && chkF.checked) {
                    chkB.disabled = false;
                    chkIsFromBom.disabled = false;
                }
            }
        }

        // Main checkbox logic for chkK and chkIsFromKit
        if (chkK && chkIsFromKit) {
            // Prevent chkK and chkIsFromKit from being checked together
            if (chkK.checked) {
                chkIsFromKit.checked = false;
                chkIsFromKit.disabled = true;
            } else if (chkIsFromKit.checked) {
                chkK.checked = false;
                chkK.disabled = true;
            } else {
                // If neither is checked, enable both as long as chkF is checked
                if (chkF && chkF.checked) {
                    chkK.disabled = false;
                    chkIsFromKit.disabled = false;
                }
            }
        }

        // Handle display of link buttons
        if (lbtnBOM) {
            lbtnBOM.style.display = chkIsFromBom && chkIsFromBom.checked ? 'inline-block' : 'none';
        }
        if (lbtnKit) {
            lbtnKit.style.display = chkIsFromKit && chkIsFromKit.checked ? 'inline-block' : 'none';
        }

        // If chkF is unchecked, disable appropriate checkboxes
        if (chkF && !chkF.checked) {
            if (chkIsFromBom) {
                chkIsFromBom.disabled = true;
                chkIsFromBom.checked = false;
            }
            if (chkIsFromKit) {
                chkIsFromKit.disabled = true;
                chkIsFromKit.checked = false;
            }
            if (lbtnBOM) lbtnBOM.style.display = 'none';
            if (lbtnKit) lbtnKit.style.display = 'none';
        }
    } catch { }
}

document.addEventListener('DOMContentLoaded', function () {
    // Attach the update function to all checkboxes
    var checkboxIds = [chkFClientID, chkBClientID, chkKClientID, chkIsFromBomClientID, chkIsFromKitClientID];
    checkboxIds.forEach(function(id) {
        var checkbox = document.getElementById(id);
        if (checkbox) {
            checkbox.addEventListener('change', updateCheckboxes);
        }
    });

    // Store initial state
    var chkIsFromKit = document.getElementById(chkIsFromKitClientID);
    if (chkIsFromKit) chkIsFromKit.wasChecked = chkIsFromKit.checked;
    var chkIsFromBom = document.getElementById(chkIsFromBomClientID);
    if (chkIsFromBom) chkIsFromBom.wasChecked = chkIsFromBom.checked;

    // Initial check to set the correct state
    updateCheckboxes();
});


