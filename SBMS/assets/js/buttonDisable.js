// assets/js/buttonDisable.js
function disableReceiveButtons(resetId, printId, finishId, finishText) {
    // Disable all receive-related buttons
    var buttons = [resetId, printId, finishId];

    buttons.forEach(function (buttonId) {
        var button = document.getElementById(buttonId);
        if (button) {
            button.disabled = true;
            button.style.opacity = '0.5';
            button.style.pointerEvents = 'none';
            button.onclick = function () { return false; };
        }
    });

    // Change the finish button text to show processing
    var finishBtn = document.getElementById(finishId);
    if (finishBtn) {
        finishBtn.innerHTML = '<i class="fa fa-spinner fa-spin"></i> Processing...';
    }

    // Prevent navigation
    window.onbeforeunload = function () {
        return "Receive process in progress. Please wait...";
    };
}

function enableReceiveButtons(resetId, printId, finishId, finishText) {
    // Re-enable all receive-related buttons
    var buttons = [resetId, printId, finishId];

    buttons.forEach(function (buttonId) {
        var button = document.getElementById(buttonId);
        if (button) {
            button.disabled = false;
            button.style.opacity = '1';
            button.style.pointerEvents = 'auto';
            button.onclick = null;
        }
    });

    // Restore finish button text
    var finishBtn = document.getElementById(finishId);
    if (finishBtn) {
        finishBtn.innerHTML = finishText;
    }

    // Remove navigation prevention
    window.onbeforeunload = null;
}