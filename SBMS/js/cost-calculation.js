// Works Order Cost Calculation - GUARANTEED WORKING VERSION
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', initialize);

    if (typeof Sys !== 'undefined') {
        var prm = Sys.WebForms.PageRequestManager.getInstance();
        prm.add_endRequest(function () {
            setTimeout(initialize, 100);
        });
    }

    function initialize() {
        var inputs = document.querySelectorAll('.use-qty-input');
        inputs.forEach(function (input) {
            input.removeEventListener('input', handleQuantityChange);
            input.removeEventListener('change', handleQuantityChange);
            input.addEventListener('input', handleQuantityChange);
            input.addEventListener('change', handleQuantityChange);
        });
        recalculateAll();
    }

    function handleQuantityChange(e) {
        calculateUnitCost(e.target);
    }

    function calculateUnitCost(inputElement) {
        // Get the line ID from the input
        var lineId = inputElement.getAttribute('data-line-id');
        if (!lineId) return;

        // Find ALL inputs with this line ID
        var allInputs = document.querySelectorAll('.use-qty-input[data-line-id="' + lineId + '"]');
        if (allInputs.length === 0) return;

        var totalMaterialCost = 0;

        // Calculate total cost for this line
        for (var i = 0; i < allInputs.length; i++) {
            var input = allInputs[i];
            var row = input.closest('tr');
            if (!row) continue;

            var costElement = row.querySelector('.unit-cost-label');
            if (!costElement) continue;

            var unitCostText = costElement.textContent || '0';
            var unitCost = parseFloat(unitCostText.replace(/[^\d.-]/g, '')) || 0;
            var useQty = parseFloat(input.value) || 0;

            totalMaterialCost += useQty * unitCost;
        }

        // Get line quantity from hidden field
        var lineQuantity = 1;
        var hiddenQtyField = document.getElementById('HiddenLineQty_' + lineId);
        if (!hiddenQtyField) {
            // Try alternative selector
            hiddenQtyField = document.querySelector('input[id*="HiddenLineQty_' + lineId + '"]');
        }
        if (hiddenQtyField) {
            lineQuantity = parseFloat(hiddenQtyField.value) || 1;
        }

        // Store total cost in hidden field
        var hiddenTotalCost = document.getElementById('HiddenTotalCost_' + lineId);
        if (!hiddenTotalCost) {
            hiddenTotalCost = document.querySelector('input[id*="HiddenTotalCost_' + lineId + '"]');
        }
        if (hiddenTotalCost) {
            hiddenTotalCost.value = totalMaterialCost.toFixed(2);
        }

        // Calculate unit cost
        var unitCost = lineQuantity > 0 ? totalMaterialCost / lineQuantity : 0;

        // Update the display label - DIRECT ACCESS BY ID
        var displayLabel = document.getElementById('lblThisCost_' + lineId);
        if (!displayLabel) {
            // Try alternative selector
            displayLabel = document.querySelector('[id*="lblThisCost_' + lineId + '"]');
        }
        if (displayLabel) {
            displayLabel.textContent = unitCost.toFixed(2);
        }
    }

    function recalculateAll() {
        var inputs = document.querySelectorAll('.use-qty-input');
        var processedLineIds = new Set();

        inputs.forEach(function (input) {
            var lineId = input.getAttribute('data-line-id');
            if (lineId && !processedLineIds.has(lineId)) {
                processedLineIds.add(lineId);
                calculateUnitCost(input);
            }
        });
    }

    // Expose functions
    window.calculateLineCost = calculateUnitCost;

    window.WorksOrderCalculator = {
        recalculateAll: recalculateAll,
        refresh: initialize
    };

})();